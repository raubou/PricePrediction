using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMDInvestments.Models;
using static Microsoft.EntityFrameworkCore.Query.Internal.ExpressionTreeFuncletizer;

namespace TMD.Services
{
    public sealed class KalshiService
    {
        private readonly ILogger? _logger;
        //private readonly CoinBaseSettings? _settings;
        private readonly IHttpClientFactory? _factory;
        private readonly IConfiguration? Instance;
        private readonly KalshiSettings? kalshi;
        HttpClientServices<dynamic>? client = null;
        public KalshiService(ILogger? logger, IHttpClientFactory? factory)
        {
            _logger = logger;
            //_settings = settings;
            _factory = factory;
            string baseDir = AppContext.BaseDirectory;
            
            Instance = new ConfigurationBuilder()
                .SetBasePath(baseDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddEnvironmentVariables() // Optional: recommended for fallback configurations
                .Build();
            kalshi = Instance?.GetSection("kalshi")?.Get<KalshiSettings>();
        }
        public dynamic SendRequest(HttpMethod method, string path, string body, bool authenicate)
        {
            Dictionary<string, object>? header = null;
            dynamic? response = null;
            client = new HttpClientServices<dynamic>(_factory, _logger);
            switch (method.Method)
            {
                case "GET":
                    if (authenicate)
                    {
                        header = Authenicate(kalshi?.ApiKey, kalshi?.PrivateKey, method.ToString(), path);
                        response = client.Get(kalshi?.BaseUrl, path, null, header);
                    }
                    else
                    {
                        //header = Authenicate(kalshi?.ApiKey, kalshi?.PrivateKey, method, path);
                        response = client.Get(kalshi?.BaseUrl, path);
                    }
                    break;
                case "POST":
                case "DELETE":
                    if (authenicate)
                    {
                        header = Authenicate(kalshi?.ApiKey, kalshi?.PrivateKey, method.ToString(), path);
                        response = client.SendRequest(method, kalshi?.BaseUrl, path, body, null, header, "application/json");
                    }
                    else
                    {
                        //header = Authenicate(kalshi?.ApiKey, kalshi?.PrivateKey, method, path);
                        response = client.SendRequest2(method, kalshi?.BaseUrl, path, body, null, header, "application/json");
                    }
                    break;
            }

            // Implement the logic to send the request to Kalshi API using the authenticated request
            // You can use HttpClient or any other HTTP library to send the request
            // Return the response from the API
            return response; // Placeholder for actual response
        }
        private Dictionary<string, object> Authenicate(string APIKey,string private_key, string method, string path) {
            Dictionary<string, object> Header = new Dictionary<string, object>();
            //Strip query parameters from path before signing
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            path = path.Split('?')[0];
            // 1. Define the data to be signed
            //string message = "Secure data payload to verify authenticity.";
            string message = $"{timestamp}{method}{path}";
            byte[] dataToSign = Encoding.UTF8.GetBytes(message);

            byte[] signature;
            byte[] publicKeyBytes;

            // 2. Generate the RSA-PSS Signature using the Private Key
            using (RSA rsa = RSA.Create(2048)) // Minimum 2048-bit key size required for security
            {
                rsa.ImportFromPem(private_key.ToCharArray());
                // Sign the data using SHA256 and PSS padding
                signature = rsa.SignData(
                    dataToSign,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss
                );

                // Export public key to simulate sharing it with a third party
                publicKeyBytes = rsa.ExportRSAPublicKey();
            }

            //Console.WriteLine($"Signature generated successfully. Length: {signature.Length} bytes.");

            // 3. Verify the RSA-PSS Signature using only the Public Key
            //using (RSA rsaVerifier = RSA.Create())
            //{
            //    // Import the public key
            //    rsaVerifier.ImportRSAPublicKey(publicKeyBytes, out _);

            //    // Verify using the exact same Hash Algorithm and Padding Mode
            //    bool isSignatureValid = rsaVerifier.VerifyData(
            //        dataToSign,
            //        signature,
            //        HashAlgorithmName.SHA256,
            //        RSASignaturePadding.Pss
            //    );

            //    Console.WriteLine($"Is signature valid? {isSignatureValid}"); // Output: True
            //}
            Header.Add("KALSHI-ACCESS-KEY", APIKey);
            Header.Add("KALSHI-ACCESS-SIGNATURE", Convert.ToBase64String(signature));
            Header.Add("KALSHI-ACCESS-TIMESTAMP", timestamp);
            return Header;
        }
    }
}
