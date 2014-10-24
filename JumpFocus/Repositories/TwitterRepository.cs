using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly TwitterConfig _twitterConfig;
        private readonly string _baseUrl;

        public TwitterRepository(TwitterConfig twitterConfig, string baseUrl = "https://api.twitter.com")
        {
            _twitterConfig = twitterConfig;
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
        /// Returns a user if found, null if not
        /// </summary>
        /// <param name="screeName"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TwitterUser>> GetUsersLookup(string screeName)
        {
            var client = new RestClient(_baseUrl)
            {
                Authenticator = new TwitterAuthenticator(_twitterConfig)
            };
            var request = new RestRequest("1.1/users/lookup.json");
            request.AddParameter("screen_name", screeName);

            var response = await client.ExecuteTaskAsync<List<TwitterUser>>(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return response.Data;
            }
            return null;
        }

        /// <summary>
        /// Post a message on Twitter
        /// </summary>
        /// <param name="message"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public async Task<TwitterStatusesUpdateResponse> PostStatusUpdate(string message, string filePath)
        {
            var client = new RestClient("https://upload.twitter.com")
            {
                Authenticator = new TwitterAuthenticator(_twitterConfig)
            };
            
            var uploadRequest = new RestRequest("/1.1/media/upload.json", Method.POST);
            uploadRequest.AddFile("media", ReadToEnd(filePath), Path.GetFileName(filePath), "image/jpg");
            var uploadResponse = await client.ExecuteTaskAsync<TwitterUploadResponse>(uploadRequest);

            client = new RestClient(_baseUrl)
            {
                Authenticator = new TwitterAuthenticator(_twitterConfig)
            };

            var request = new RestRequest("1.1/statuses/update.json", Method.POST);
            request.AddParameter("status", message, ParameterType.QueryString);
            request.AddParameter("media_ids", uploadResponse.Data.media_id, ParameterType.QueryString);

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
            string base64Token = string.Format("{0}:{1}", _twitterConfig.ConsumerKey, _twitterConfig.ConsumerSecret).ToBase64();

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

        private byte[] ReadToEnd(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);
            long originalPosition = fileStream.Position;
            fileStream.Position = 0;

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = fileStream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = fileStream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                fileStream.Position = originalPosition;
            }
        }
    }
}
