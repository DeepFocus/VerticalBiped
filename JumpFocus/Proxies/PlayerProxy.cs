﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using JumpFocus.DAL;
using JumpFocus.Models;
using JumpFocus.Models.API;
using JumpFocus.Repositories;

namespace JumpFocus.Proxies
{
    public class PlayerProxy
    {
        private const string CacheKey = "PlayerList";
        private readonly ObjectCache _cache;
        private readonly TwitterRepository  _twitterRepo;
        private readonly string _twitterScreenName;

        public PlayerProxy(ObjectCache cache, string twitterApiKey, string twitterApiSecret, string twitterScreenName)
        {
            _cache = cache;
            _twitterRepo = new TwitterRepository(twitterApiKey, twitterApiSecret);
            _twitterScreenName = twitterScreenName;
        }

        public async Task<IEnumerable<Player>> GetAllPlayers()
        {
            var result = _cache.Get(CacheKey) as IEnumerable<Player>;

            if (null != result)
            {
                return result;
            }

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

            var dbRepo = new JumpFocusContext();
            var dbUserTwitterIds = from p in dbRepo.Players
                where p.Name != null && p.Name.Trim() != string.Empty
                select p.TwitterId;
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
                    }
                }
                ids.RemoveAll(users.Contains);
                dbRepo.SaveChanges();
            }
            result = dbRepo.Players.ToList();
            _cache.Set(CacheKey, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }
    }
}
