using Jose;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Coinbase.AdvancedTrade
{
    /// <summary>
    /// Provides functionality for generating JWT tokens.
    /// </summary>
    public sealed class JwtTokenGenerator2
    {
        static Random random = new Random();
        public static string GenerateJwt(string name, string secret, string uri, string method)
        {
            string secretParse = parseKey(secret);
            var privateKeyBytes = Convert.FromBase64String(secretParse); // Assuming PEM is base64 encoded
            var key = ECDsa.Create();
            key.ImportECPrivateKey(privateKeyBytes, out _);

            var payload = new Dictionary<string, object>
             {
                 { "sub", name },
                 { "iss", "coinbase-cloud" },
                 { "nbf", Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                 { "exp", Convert.ToInt64((DateTime.UtcNow.AddMinutes(1) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) },
                 //{ "uri", uri }
             };

            string request_host = "api.coinbase.com";
            string request_path = uri != null && uri.Contains("?") ? uri.Substring(0, uri.IndexOf('?')) : uri;


            if (method != null && request_path != null)
            {
                payload.Add("uri", $"{method} {request_host}{request_path}");
            }

            var extraHeaders = new Dictionary<string, object>
             {
                 { "kid", name },
                 // add nonce to prevent replay attacks with a random 10 digit number
                 { "nonce", randomHex(10) },
                 { "typ", "JWT"}
             };

            var encodedToken = JWT.Encode(payload, key, JwsAlgorithm.ES256, extraHeaders);

            // print token
            Console.WriteLine(encodedToken);
            return encodedToken;
        }
        static bool isTokenValid(string token, string tokenId, string secret)
        {
            if (token == null)
                return false;

            var key = ECDsa.Create();
            key?.ImportECPrivateKey(Convert.FromBase64String(secret), out _);

            var securityKey = new ECDsaSecurityKey(key) { KeyId = tokenId };

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }
        static string parseKey(string key)
        {
            List<string> keyLines = new List<string>();
            keyLines.AddRange(key.Split('\n', StringSplitOptions.RemoveEmptyEntries));

            keyLines.RemoveAt(0);
            keyLines.RemoveAt(keyLines.Count - 1);

            return String.Join("", keyLines);
        }


        static string randomHex(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        static string CallApiGET(string url, string bearerToken = "")
        {
            using (var client = new HttpClient())
            {

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    if (bearerToken != "")
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
                    var response = client.Send(request);

                    if (response != null)
                        return response.Content.ReadAsStringAsync().Result;
                    else
                        return "";

                }
            }
        }
    }
}
