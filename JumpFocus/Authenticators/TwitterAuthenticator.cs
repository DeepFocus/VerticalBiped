using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace JumpFocus.Authenticators
{
    class TwitterAuthenticator : IAuthenticator
    {
        private readonly string _accessToken;
        private readonly string _accessTokenSecret;
        private readonly string _consumerKey;
        private readonly string _consumerSecret;

        public TwitterAuthenticator(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
        {
            _accessToken = accessToken;
            _accessTokenSecret = accessTokenSecret;
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
        }

        /// <summary>
        /// Generates the authorization header for a twitter user request
        /// https://dev.twitter.com/oauth/overview/authorizing-requests
        /// </summary>
        /// <param name="client"></param>
        /// <param name="request"></param>
        public void Authenticate(IRestClient client, IRestRequest request)
        {
            var oauthParameters = new Dictionary<string, string>
            {
                {"oauth_consumer_key", _consumerKey},
                {"oauth_nonce", Guid.NewGuid().ToString().Replace("-", string.Empty)},
                {"oauth_signature_method", "HMAC-SHA1"},
                {"oauth_timestamp", ((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds).ToString()},
                {"oauth_token", _accessToken},
                {"oauth_version", "1.0"}
            };

            var parameters = from o in oauthParameters.Concat(request.Parameters.Select(p => new KeyValuePair<string, string>(p.Name, p.Value.ToString())))
                             orderby Uri.EscapeDataString(o.Key)
                             select string.Format("{0}={1}", Uri.EscapeDataString(o.Key), Uri.EscapeDataString(o.Value));

            string oauth = string.Format("{0}&{1}&{2}",
                request.Method.ToString().ToUpper(),
                Uri.EscapeDataString(new Uri(client.BuildUri(request).AbsoluteUri).GetLeftPart(UriPartial.Path)),
                Uri.EscapeDataString(string.Join("&", parameters)));

            string signingKey = string.Format("{0}&{1}",
                Uri.EscapeDataString(_consumerSecret),
                Uri.EscapeDataString(_accessTokenSecret));

            var encoding = Encoding.UTF8;
            var hmac = new HMACSHA1(encoding.GetBytes(signingKey));
            hmac.Initialize();
            string oauthSignature = Convert.ToBase64String(hmac.ComputeHash(encoding.GetBytes(oauth)));

            oauthParameters.Add("oauth_signature", oauthSignature);

            request.AddHeader("Authorization",
                string.Format("OAuth {0}",
                    string.Join(",",
                        oauthParameters.OrderBy(kvp => Uri.EscapeDataString(kvp.Key))
                        .Select(kvp => string.Format("{0}=\"{1}\"", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value))))));
        }
    }
}
