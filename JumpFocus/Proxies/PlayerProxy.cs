using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using JumpFocus.DAL;
using JumpFocus.Models;
using JumpFocus.Models.API;
using JumpFocus.Repositories;
using AutoMapper;

namespace JumpFocus.Proxies
{
    public class PlayerProxy
    {
        private List<Player> Players
        {
            get
            {
                return _cache.Get(CacheKey) as List<Player>;
            }
        }

        private const string CacheKey = "PlayerList";
        private readonly ObjectCache _cache;
        private readonly TwitterRepository _twitterRepo;
        private readonly string _twitterScreenName;

        public PlayerProxy(ObjectCache cache, TwitterConfig twitterConfig, string twitterScreenName)
        {
            _cache = cache;
            _twitterRepo = new TwitterRepository(twitterConfig);
            _twitterScreenName = twitterScreenName;
        }

        public async Task<Player> FindPlayer(string screenName)
        {
            var result = Players.FirstOrDefault(p => String.Equals(p.TwitterHandle, screenName, StringComparison.CurrentCultureIgnoreCase));

            if (null == result)
            {
                result = Players.Where(p => p.TwitterHandle.ToLower().StartsWith(screenName.ToLower())).OrderBy(p => p.TwitterHandle.Length).FirstOrDefault();

                if (null == result)
                {
                    var users = await _twitterRepo.GetUsersLookup(screenName);
                    if (null != users)
                    {
                        var dbRepo = new JumpFocusContext();
                        try
                        {
                            var players = Mapper.Map<List<Player>>(users);

                        dbRepo.Players.AddOrUpdate(players.ToArray());
                        Players.AddRange(players); //update cache

                        result = players.FirstOrDefault();
                        }
                        catch (Exception)
                        {
                            int i = 12;
                        }
                        await dbRepo.SaveChangesAsync();
                    }
                }
            }

            return result;
        }

        public async Task CacheWarmup()
        {
            var result = _cache.Get(CacheKey) as List<Player>;

            if (null != result)
            {
                return;
            }

            var ids = await GetAllTwitterIds();

            var dbRepo = new JumpFocusContext();
            result = await dbRepo.Players.ToListAsync();

            foreach (var player in result)
            {
                ids.Remove(player.TwitterId);
            }

            var dbUserTwitterIds = dbRepo.Players
                        .Where(x => x != null && x.Name != null && x.Name.Trim() != string.Empty)
                        .Select(x => x.TwitterId);
            ids.RemoveAll(dbUserTwitterIds.Contains);

            long[] users;
            while ((users = ids.Take(100).ToArray()).Length > 0) //100 is the twitter API limit
            {
                var players = await _twitterRepo.PostUsersLookup(users);
                if (null != players)
                {
                    foreach (var player in players)
                    {
                        var p = new Player
                        {
                            TwitterId = player.id,
                            Name = player.name,
                            TwitterHandle = player.screen_name,
                            TwitterPhoto = player.profile_image_url,
                            Created = DateTime.Now
                        };
                        dbRepo.Players.AddOrUpdate(p);
                        result.Add(p);
                    }
                }
                ids.RemoveAll(users.Contains);
                await dbRepo.SaveChangesAsync();

            }
            var cachePolicy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(15),
                RemovedCallback = CacheWarmup
            };
            _cache.Set(CacheKey, result, cachePolicy);
        }

        private void CacheWarmup(CacheEntryRemovedArguments arguments)
        {
            CacheWarmup();
        }

        private async Task<List<long>> GetAllTwitterIds()
        {
            var ids = new List<long>();
            var followersIds = new TwitterFollowersIds
            {
                next_cursor = -1 //initiallize the cursor to the default value
            };
            do
            {
                followersIds = await _twitterRepo.GetFollowersIds(_twitterScreenName, followersIds.next_cursor);
                if (null != followersIds)
                {
                    ids.AddRange(followersIds.ids);
                }
            } while (null != followersIds && followersIds.next_cursor > 0);
            return ids;
        }
    }
}
