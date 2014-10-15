﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using JumpFocus.Extensions;
using JumpFocus.Models.API;
using RestSharp;
using JumpFocus.Authenticators;

namespace JumpFocus.Repositories
{
    class TwitterRepository
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _accessToken;
        private readonly string _accessTokenSecret;
        private readonly string _baseUrl;

        public TwitterRepository(string apiKey, string apiSecret, string accessToken = "", string accessTokenSecret = "", string baseUrl = "https://api.twitter.com")
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _accessToken = accessToken;
            _accessTokenSecret = accessTokenSecret;
            _baseUrl = baseUrl;
        }

        /// <summary>
        /// Returns the followers id
        /// </summary>
        /// <param name="screenName"></param>
        /// <param name="cursor"></param>
        /// <param name="count">Max number of tweets to return (default to 100, max 200)</param>
        /// <returns></returns>
        public async Task<TwitterFollowersIds> GetFollowersIds(string screenName, long cursor = -1, int count = 5000)
        {

            var client = new RestClient(_baseUrl);

            if (await Authenticate(client))
            {
                var request = new RestRequest("1.1/followers/ids.json", Method.GET);
                request.AddParameter("screen_name", screenName);
                request.AddParameter("cursor", cursor);
                request.AddParameter("count", count);

                var response = await client.ExecuteTaskAsync<TwitterFollowersIds>(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response.Data;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a list of users
        /// </summary>
        /// <param name="userIds"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TwitterUser>> PostUsersLookup(IEnumerable<long> userIds)
        {
            var client = new RestClient(_baseUrl);

            if (await Authenticate(client))
            {
                var request = new RestRequest("1.1/users/lookup.json", Method.POST);
                request.AddParameter("user_id", string.Join(",", userIds));
                request.AddParameter("include_entities", false);

                var response = await client.ExecuteTaskAsync<List<TwitterUser>>(request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return response.Data;
                }
            }

            return null;
        }

        /// <summary>
        /// Post a message on Twitter
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<TwitterStatusesUpdateResponse> PostStatusesUpdate(string message)
        {
            var client = new RestClient(_baseUrl)
            {
                Authenticator = new TwitterAuthenticator(_apiKey, _apiSecret, _accessToken, _accessTokenSecret)
            };

            var request = new RestRequest("1.1/statuses/update.json", Method.POST);
            request.AddParameter("status", message, ParameterType.QueryString);

            var response = await client.ExecuteTaskAsync<TwitterStatusesUpdateResponse>(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Data;
            }

            return null;
        }

        /// <summary>
        /// Authenticates the client and returns true if successful, false if not
        /// see: https://dev.twitter.com/docs/auth/application-only-auth
        /// </summary>
        /// <param name="client">IRestClient to be authorized</param>
        private async Task<bool> Authenticate(IRestClient client)
        {
            string base64Token = string.Format("{0}:{1}", _apiKey, _apiSecret).ToBase64();

            client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(base64Token, "Basic");

            var request = new RestRequest("/oauth2/token", Method.POST);
            request.AddParameter("grant_type", "client_credentials");

            var response = await client.ExecuteTaskAsync<TwitterAuthenticationResponse>(request);

            if (response.StatusCode == HttpStatusCode.OK
                && response.Data.token_type.ToLower() == "bearer"
                && !string.IsNullOrWhiteSpace(response.Data.access_token))
            {
                client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(response.Data.access_token, "Bearer");
                return true;
            }

            return false;
        }
    }
}
