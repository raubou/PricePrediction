using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using TinyOAuth2;
using Microsoft.EntityFrameworkCore;
using TMDInvestments.Context;
using TMDInvestments.Models;

namespace TMD.Services.Service
{
    public sealed class ETradeService
    {
        private readonly ILogger? _logger;
        private readonly ETradeSettings? _settings;
        //private Polygon? _polygon;
        private readonly IHttpClientFactory? _factory;
        private Dictionary<string, object>? headers;
        HttpClientServices<dynamic> client;
        TinyOAuth tinyOAuth;
        TinyOAuthConfig tinyOAuthConfig;
        string requestToken;
        string requestTokenSecret;
        TmdContext _ctx;
        private static Random random = new Random();
        PolygonService? polygonService = null;
        //Helper? _helper = null;
        public ETradeService(ILogger logger, ETradeSettings settings, IHttpClientFactory? factory, IDbContextFactory<TmdContext> contextFactory) {
            _logger = logger;
            _settings = settings;
            _factory = factory;
            //this._ctx = ctx;
            //_helper = helper;
            tinyOAuthConfig = new TinyOAuthConfig
            {
                AccessTokenUrl = _settings.baseURL + "oauth/access_token",
                RequestTokenUrl = _settings.baseURL + "oauth/request_token",
                ConsumerKey = _settings.ConsumerKey,
                ConsumerSecret = _settings.ConsumerSecret,
                AuthorizeTokenUrl = _settings.authURL,
                CallbackUrl = "oob" //"https://127.0.0.1/EtradeAPICallBack"
            };
            tinyOAuth = new TinyOAuth(tinyOAuthConfig);
            //IConfigurationBuilder builder = new ConfigurationBuilder()
            //    .AddJsonFile("appsettings.json");
            //IConfigurationRoot config = builder.Build();
            //var ConnectionStrings = config.GetSection("ConnectionStrings").Get<ConnectionSettings>();
            //var services = new ServiceCollection();
            //services.AddDbContextFactory<TmdContext>(options =>
            //    options.UseSqlServer(ConnectionStrings.Connection));
            ////services.AddTransient<PricePrediction>();
            //var serviceProvider = services.BuildServiceProvider();
            //using (var scope = serviceProvider.CreateScope())
            //{
            //    var ctxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TmdContext>>();
            //    var context = ctxFactory.CreateDbContext();
            //    ctx = context;
            //}
            //ctx = new TmdContext();
            _ctx = contextFactory.CreateDbContext();
            var token = _ctx.Token.Where(x => x.TokenType == TokenType.ETrade).FirstOrDefault();
            if (token != null)
            {
                requestToken = System.Text.Encoding.Default.GetString(token.Access_Token);
                requestTokenSecret = System.Text.Encoding.Default.GetString(token.Refresh_Token);
            }
            //polygonService = new PolygonService(logger, _polygon, factory);
        }
        public dynamic Quote(string symbol, bool skipCalculation = false)
        {
            IndicesCalculatorV2 indicesCalculator = new IndicesCalculatorV2(_logger, _settings, _factory/*, _helper*/);
            dynamic output = null;
            string url = $"v1/market/quote/{symbol}";
            var param = "detailFlag=ALL&";
            //payLoad = GetHeaderPayload(url, "GET");
            var authorization = GetHeaderAuth(url, "GET", param);
            //url += "&" + payLoad;
            url += "?" + param;
            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            XElement results = client.Get(_settings.baseURL, url, authorization);
            if (!skipCalculation)
                //output = polygonService.GetIndicators(symbol, DateTime.Now, "day", 14, 12, 26, 9, results);
            output = indicesCalculator.CalculateV2(symbol, results);
            return output;
        }

        public dynamic Account()
        {
            string instType = "BROKERAGE";
            string url = $"v1/accounts/list";
            //payLoad = GetHeaderPayload(url, "GET");
            var authorization = GetHeaderAuth(url, "GET", null);
            //url += "?" + payLoad;
            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            dynamic accounts = client.Get(_settings.baseURL, url, authorization);
            var accountIdKey = ((XElement)accounts).Elements("Accounts").ToList()[0].Element("Account").Element("accountIdKey").Value;

            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            url = $"v1/accounts/{accountIdKey}/balance";//?instType={instType}&realTimeNAV=true";
            var param = $"instType={instType}&realTimeNAV=true";
            authorization = GetHeaderAuth(url, "GET", param);
            url += "?" + param;
            //url += "?instType={instType}&realTimeNAV=true";
            //payLoad = GetHeaderPayload(url, "GET");
            //url += "?" + payLoad;
            //url += "&" + payLoad;
            dynamic account = client.Get(_settings.baseURL, url, authorization);
            return account;
        }
        public EtradeAccountInfo2 AccountModel()
        {
            string instType = "BROKERAGE";
            string url = $"v1/accounts/list";
            //payLoad = GetHeaderPayload(url, "GET");
            var authorization = GetHeaderAuth(url, "GET", null);
            //url += "?" + payLoad;
            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            dynamic accounts = client.Get(_settings.baseURL, url, authorization);
            var accountIdKey = ((XElement)accounts).Elements("Accounts").ToList()[0].Element("Account").Element("accountIdKey").Value;

            HttpClientServices<EtradeAccountInfo2> client2 = new HttpClientServices<EtradeAccountInfo2>(_factory, _settings/*, _logger*/);
            url = $"v1/accounts/{accountIdKey}/balance";//?instType={instType}&realTimeNAV=true";
            var param = $"instType={instType}&realTimeNAV=true";
            authorization = GetHeaderAuth(url, "GET", param);
            url += "?" + param;
            //url += "?instType={instType}&realTimeNAV=true";
            //payLoad = GetHeaderPayload(url, "GET");
            //url += "?" + payLoad;
            //url += "&" + payLoad;
            dynamic account = client2.GetModel(_settings.baseURL, url, authorization);
            return account;
        }
        public XElement Portfolio()
        {
            string accountIdKey = string.Empty;
            string url = $"v1/accounts/list";
            //payLoad = GetHeaderPayload(url, "GET");
            var authorization = GetHeaderAuth(url, "GET", null);
            //url += "?" + payLoad;
            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            dynamic accounts = client.Get(_settings.baseURL, url, authorization);
            try
            {
                accountIdKey = ((XElement)accounts).Elements("Accounts").ToList()[0].Element("Account").Element("accountIdKey").Value;
            }
            catch (Exception ex)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.Details = ex.Message;
            }            

            url = $"v1/accounts/{accountIdKey}/portfolio";
            //payLoad = GetHeaderPayload(url, "GET");
            authorization = GetHeaderAuth(url, "GET");
            //url += "?" + payLoad;
            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            XElement portfolio = client.Get(_settings.baseURL, url, authorization);
            return portfolio;
        }
        public EtradeAccountInfo PortfolioModel()
        {
            string accountIdKey = string.Empty;
            string url = $"v1/accounts/list";
            //payLoad = GetHeaderPayload(url, "GET");
            var authorization = GetHeaderAuth(url, "GET", null);
            //url += "?" + payLoad;
            HttpClientServices<dynamic> client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            dynamic accounts = client.Get(_settings.baseURL, url, authorization);
            try
            {
                accountIdKey = ((XElement)accounts).Elements("Accounts").ToList()[0].Element("Account").Element("accountIdKey").Value;
            }
            catch (Exception ex)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.Details = ex.Message;
            }

            url = $"v1/accounts/{accountIdKey}/portfolio";
            //payLoad = GetHeaderPayload(url, "GET");
            authorization = GetHeaderAuth(url, "GET");
            //url += "?" + payLoad;
            HttpClientServices<EtradeAccountInfo> client2 = new HttpClientServices<EtradeAccountInfo>(_factory, _settings/*, _logger*/);
            EtradeAccountInfo portfolio = client2.GetModel(_settings.baseURL, url, authorization);
            return portfolio;
        }
        public dynamic Search(string search)
        {
            string url = $"v1/market/lookup/{search}";
            //payLoad = GetHeaderPayload(url, "GET");
            var authorization = GetHeaderAuth(url, "GET");
            //url += "?" + payLoad;
            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            dynamic results = client.Get(_settings.baseURL, url, authorization);
            return results;
        }
        public dynamic SetOrder(string symbol, string priceType, string limitPrice, int? qty, TradeDirection direction)
        {
            string orderAction = null;
            qty = Math.Abs((int)qty);
            switch (direction)
            {
                case TradeDirection.BUY:
                    orderAction = "BUY";
                    break;
                case TradeDirection.SELL:
                    orderAction = "SELL";
                    break;
                case TradeDirection.SELL_SHORT:
                    orderAction = "SELL_SHORT";
                    break;
                case TradeDirection.BUY_TO_COVER:
                    orderAction = "BUY_TO_COVER";
                    break;
            }
            string url = $"v1/accounts/list";
            var authorization = GetHeaderAuth(url, "GET", null);
            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            dynamic accounts = client.Get(_settings.baseURL, url, authorization);
            var accountIdKey = ((XElement)accounts).Elements("Accounts").ToList()[0].Element("Account").Element("accountIdKey").Value;

            dynamic previewOrder = PreviewOrder(accountIdKey, symbol, priceType, limitPrice, qty, orderAction, direction);
            if (previewOrder is not XElement)
                return previewOrder;
            var previewId = ((XElement)previewOrder)?.Element("PreviewIds")?.Element("previewId")?.Value?.ToString();
            dynamic results = PlaceOrder(accountIdKey, previewId, symbol, priceType, limitPrice, qty, orderAction, direction);
            return results;
        }
        private dynamic PreviewOrder(string accountIdKey, string symbol, string priceType, string limitPrice, int? qty, string orderAction, TradeDirection direction)
        {
            string url = $"v1/accounts/{accountIdKey}/orders/preview";
            var authorization = GetHeaderAuth(url, "POST", null);
            string payLoad = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<PreviewOrderRequest>" +
                   "<orderType>EQ</orderType>" +
                   "<Order>" +
                      "<allOrNone>true</allOrNone>" +
                      //"<priceType>MARKET</priceType>" +
                      "<priceType>" + priceType + "</priceType>" +
                      "<orderTerm>GOOD_FOR_DAY</orderTerm>" +
                      "<marketSession>REGULAR</marketSession>" +
                      "<stopPrice />";
            if (!string.IsNullOrEmpty(limitPrice))
            {
                payLoad += "<limitPrice>" + limitPrice + "</limitPrice>";
            }
            else
            {
                payLoad += "<limitPrice />";
            }

            payLoad += "<Instrument>" +
              "<Product>" +
                 "<securityType>EQ</securityType>" +
                 "<symbol>" + symbol + "</symbol>" +
              "</Product>" +
              "<orderAction>" + orderAction + "</orderAction>" +
              "<quantityType>QUANTITY</quantityType>" +
              "<quantity>" + qty + "</quantity>" +
           "</Instrument>" +
        "</Order>" +
        "<clientOrderId>" + RandomString(20) + "</clientOrderId>" +
     "</PreviewOrderRequest>";

            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            dynamic results = client.Post(_settings.baseURL, url, payLoad, authorization, "application/xml");
            return results;
        }

        private dynamic PlaceOrder(string accountIdKey, string previewId, string symbol, string priceType, string limitPrice, int? qty, string orderAction, TradeDirection direction)
        {
            string url = $"v1/accounts/{accountIdKey}/orders/place";
            var authorization = GetHeaderAuth(url, "POST", null);
            string payLoad = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<PlaceOrderRequest>" +
                "<orderType>EQ</orderType>" +
                "<clientOrderId>" + RandomString(20) + "</clientOrderId>" +
                "<PreviewIds>" +
                    "<previewId>" + previewId + "</previewId>" +
                //"<cashMargin>CASH</cashMargin>" +
                "</PreviewIds>" +
                "<Order>" +
                    "<allOrNone>true</allOrNone>" +
                    //"<accountId>" + accountIdKey + "</accountId>" +                    
                    //"<priceType>MARKET</priceType>" +
                    "<priceType>" + priceType + "</priceType>" +
                    "<orderTerm>GOOD_FOR_DAY</orderTerm>" +
                    "<marketSession>REGULAR</marketSession>" +
                    "<stopPrice />";
                    //"<limitPrice />" +//188.51</limitPrice>" +
                    if (!string.IsNullOrEmpty(limitPrice))
                    {
                        payLoad += "<limitPrice>" + limitPrice + "</limitPrice>";
                    }
                    else
                    {
                        payLoad += "<limitPrice />";
                    }

            payLoad += "<Instrument>" +
                        "<Product>" +
                        "<securityType>EQ</securityType>" +
                        "<symbol>" + symbol + "</symbol>" +
                        "</Product>" +
                        "<orderAction>" + orderAction + "</orderAction>" +
                        "<quantityType>QUANTITY</quantityType>" +
                        "<quantity>" + qty + "</quantity>" +
                    "</Instrument>" +
                "</Order>" +
            "</PlaceOrderRequest>";
            client = new HttpClientServices<dynamic>(_factory, _settings/*, _logger*/);
            dynamic results = client.Post(_settings.baseURL, url, payLoad, authorization, "application/xml");
            return results;
        }

        //consumer key, timestamp, nonce, signature method, and signature
        private string GetHeaderAuth(string url, string method, string param = null)
        {
            //Dictionary<string,string> headers = new Dictionary<string,string>();
            var nonce = GetNonce();
            var timeStamp = GetTimeStamp();

            var requestParameters = new List<string>
            {
                "oauth_consumer_key=" +  tinyOAuthConfig.ConsumerKey,
                "oauth_nonce=" + nonce,
                "oauth_signature_method=HMAC-SHA1",
                "oauth_timestamp=" + timeStamp,
                "oauth_token=" +  Uri.EscapeDataString(requestToken),
               
				 //"oauth_signature",
                
                
                //"oauth_version=1.0",
                //"oauth_verifier=" +  verifier,
            };
            if(param != null) 
                requestParameters.AddRange(param.Split('&'));

            requestParameters.Sort();

            //string sortedString = String.Join(",", requestParameters.OrderBy(x => x).ToList());

            var signatureBaseString = GetSignatureBaseString(method, _settings.baseURL + url, requestParameters, null);

            var signature = GetSignature(signatureBaseString, tinyOAuthConfig.ConsumerSecret, requestTokenSecret);

            string values = $"OAuth oauth_consumer_key=\"{Uri.EscapeDataString(tinyOAuthConfig.ConsumerKey)}\",oauth_nonce=\"{Uri.EscapeDataString(nonce)}\",oauth_signature_method=\"HMAC-SHA1\",oauth_timestamp=\"{timeStamp}\",oauth_token=\"{Uri.EscapeDataString(requestToken)}\",oauth_signature=\"{Uri.EscapeDataString(signature)}\"";
            //headers.Add("Authorization", values);
            return values;
        }
        private string GetHeaderPayload(string url, string method)
        {
            var nonce = GetNonce();
            var timeStamp = GetTimeStamp();

            var requestParameters = new List<string>
            {
                "oauth_consumer_key=" +  tinyOAuthConfig.ConsumerKey,
                "oauth_token=" +  Uri.EscapeDataString(requestToken),
                "oauth_signature_method=HMAC-SHA1",
				 //"oauth_signature",
                "oauth_timestamp=" + timeStamp,
                "oauth_nonce=" + nonce,
                //"oauth_version=1.0",
                //"oauth_verifier=" +  verifier,
            };

            requestParameters.Sort();

            // Appendix A.5.1. Generating Signature Base String
            var signatureBaseString = GetSignatureBaseString(method, _settings.baseURL + url, requestParameters);
            //signatureBaseString = signatureBaseString.Replace("_", "%5f");

            // Appendix A.5.2. Calculating Signature Value
            var signature = GetSignature(signatureBaseString, tinyOAuthConfig.ConsumerSecret, requestTokenSecret);

            var list = ConcatList(requestParameters, "&") + "&oauth_signature=" + Uri.EscapeDataString(signature);
            return list;
        }
        private string GetNonce()
        {
            var rand = new Random();
            var nonce = rand.Next(1000000000);
            return nonce.ToString();
        }

        private string GetTimeStamp()
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }
        private static string ConcatList(IEnumerable<string> source, string separator)
        {
            var sb = new StringBuilder();
            foreach (var s in source)
                if (sb.Length == 0)
                {
                    sb.Append(s);
                }
                else
                {
                    sb.Append(separator);
                    sb.Append(s);
                }
            return sb.ToString();
        }
        private string GetSignature(string signatureBaseString, string consumerSecret, string tokenSecret = null)
        {
            /*9.2.  HMAC-SHA1

			The HMAC-SHA1 signature method uses the HMAC-SHA1 signature algorithm as defined in [RFC2104] where the Signature Base String is the text and the key is the concatenated values (each first encoded per Parameter Encoding) of the Consumer Secret and Token Secret, separated by an '&' character (ASCII code 38) even if empty.
			*/

            var hmacsha1 = new HMACSHA1();

            var key = Uri.EscapeDataString(consumerSecret) + "&" + (string.IsNullOrEmpty(tokenSecret)
                          ? ""
                          : Uri.EscapeDataString(tokenSecret));
            hmacsha1.Key = Encoding.ASCII.GetBytes(key);

            var dataBuffer = Encoding.ASCII.GetBytes(signatureBaseString);
            var hashBytes = hmacsha1.ComputeHash(dataBuffer);

            return Convert.ToBase64String(hashBytes);

            // .NET Core implementation
            // var signingKey = string.Format("{0}&{1}", consumerSecret, !string.IsNullOrEmpty(requestTokenSecret) ? requestTokenSecret : "");
            // IBuffer keyMaterial = CryptographicBuffer.ConvertStringToBinary(signingKey, BinaryStringEncoding.Utf8);
            // MacAlgorithmProvider hmacSha1Provider = MacAlgorithmProvider.OpenAlgorithm("HMAC_SHA1");
            // CryptographicKey macKey = hmacSha1Provider.CreateKey(keyMaterial);
            // IBuffer dataToBeSigned = CryptographicBuffer.ConvertStringToBinary(signatureBaseString, BinaryStringEncoding.Utf8);
            // IBuffer signatureBuffer = CryptographicEngine.Sign(macKey, dataToBeSigned);
            // String signature = CryptographicBuffer.EncodeToBase64String(signatureBuffer);
            // return signature;
        }
        private string GetSignatureBaseString(string method, string url, List<string> requestParameters, string param = null)
        {
            // It's very important that we "normalize" the parameters, that is sort them:
            //9.1.1.Normalize Request Parameters

            //The request parameters are collected, sorted and concatenated into a normalized string:

            // * Parameters in the OAuth HTTP Authorization header excluding the realm parameter.
            // * Parameters in the HTTP POST request body(with a content - type of application / x - www - form - urlencoded).
            // * HTTP GET parameters added to the URLs in the query part(as defined by[RFC3986] section 3).
            var sortedList = new List<string>(requestParameters);
            sortedList.Sort();

            var requestParametersSortedString = ConcatList(sortedList, "&");

            // Url must be slightly reformatted because of:

            /*9.1.2. Construct Request URL

			The Signature Base String includes the request absolute URL, tying the signature to a specific endpoint. The URL used in the Signature Base String MUST include the scheme, authority, and path, and MUST exclude the query and fragment as defined by [RFC3986] section 3.

			If the absolute request URL is not available to the Service Provider (it is always available to the Consumer), it can be constructed by combining the scheme being used, the HTTP Host header, and the relative HTTP request URL. If the Host header is not available, the Service Provider SHOULD use the host name communicated to the Consumer in the documentation or other means.

			The Service Provider SHOULD document the form of URL used in the Signature Base String to avoid ambiguity due to URL normalization. Unless specified, URL scheme and authority MUST be lowercase and include the port number; http default port 80 and https default port 443 MUST be excluded.

			For example, the request:

							HTTP://Example.com:80/resource?id=123
			Is included in the Signature Base String as:

							http://example.com/resource
 */


            //url = ConstructRequestUrl(url);
            if (param != null)
            {
                return method.ToUpper() + "&" + Uri.EscapeDataString(url) + "&" + Uri.EscapeDataString(param + "&" +
                   requestParametersSortedString);
            }
            else
            {
                return method.ToUpper() + "&" + Uri.EscapeDataString(url) + "&" +
                       Uri.EscapeDataString(requestParametersSortedString);
            }
        }

        
        private string ConstructRequestUrl(string url)
        {
            var uri = new Uri(url, UriKind.Absolute);
            var normUrl = string.Format("{0}://{1}", uri.Scheme, uri.Host);
            if (!(uri.Scheme == "http" && uri.Port == 80 ||
                  uri.Scheme == "https" && uri.Port == 443))
                normUrl += ":" + uri.Port;

            normUrl += uri.AbsolutePath;

            return normUrl;
        }
        public RequestTokenInfo GetRequestToken()
        {
            // *** Check if we have saved tokens already, if not do the following: ***

            // Set up the basic config parameters
            string authorizationUrl = string.Empty;

            // Get the request token and request token secret
            var requestTokenInfo = tinyOAuth.GetRequestTokenAsync().Result;

            if (requestTokenInfo.RequestToken == null || requestTokenInfo.RequestTokenSecret == null)
                authorizationUrl = "error";
            else
                // Construct the authorization url
                //authorizationUrl = tinyOAuth.GetAuthorizationUrl(requestTokenInfo.RequestToken);
                authorizationUrl = tinyOAuthConfig.AuthorizeTokenUrl + "key=" + tinyOAuthConfig.ConsumerKey + "&token=" + requestTokenInfo.RequestToken;// https://us.etrade.com/e/t/etws/authorize?key=282683cc9e4b8fc81dea6bc687d46758&token=%2FiQRgQCRGPo7Xdk6G8QDSEzX0Jsy6sKNcULcDavAGgU%3D

            requestTokenInfo.AuthorizationUrl = authorizationUrl;

            // *** You will need to implement these methods yourself ***
            //await LaunchWebBrowserAsync(authorizationUrl); // Use Process.Start(authorizationUrl), LaunchUriAsync(new Uri(authorizationUrl)) etc...
            //var verificationCode = await InputVerificationCodeAsync(authorizationUrl);

            // *** Important: Do not run this code before visiting and completing the authorization url ***
            //var accessTokenInfo = await tinyOAuth.GetAccessTokenAsync(requestTokenInfo.RequestToken, requestTokenInfo.RequestTokenSecret, verificationCode);
            //var accessTokenInfo = await tinyOAuth.GetAccessTokenAsync(requestTokenInfo.RequestToken, requestTokenInfo.RequestTokenSecret);
            //return accessTokenInfo;
            return requestTokenInfo;
        }
        public async Task<AccessTokenInfo> GetAuthorizationToken(RequestTokenInfo requestTokenInfo, string verificationCode)
        {
            var accessTokenInfo = await tinyOAuth.GetAccessTokenAsync(requestTokenInfo.RequestToken, requestTokenInfo.RequestTokenSecret, verificationCode);
            return accessTokenInfo;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
