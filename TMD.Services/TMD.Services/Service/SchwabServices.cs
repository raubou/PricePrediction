using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using TMD.Services;
using TMD.Services.Service;
using TMDInvestments.Context;
using TMDInvestments.Models;


namespace TMDInvestments.Services
{
    public class SchwabServices
    {
        private readonly ILogger? _logger;
        private SchwabSettings? _settings;
        private Polygon? _polygon;
        private TaAPISettings? _taapiSettings;
        private readonly IHttpClientFactory? _factory;
        Dictionary<string, object>? headers;
        Token? _token;
        TmdContext? ctx = null;
        int? fetchRetry = 0;
        HttpClientServices<dynamic>? client = null;
        PolygonService? polygonService = null;
        IndicesCalculatorV2 indicesCalculatorV2 = null;
        TaAPIService? taAPIService = null;
        //public SchwabServices() { }
        public SchwabServices(ILogger logger, SchwabSettings settings, Polygon polygon, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings;
            _polygon = polygon;
            _factory = factory;
            ctx = new TmdContext();
            _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
            polygonService = new PolygonService(logger, _polygon, _factory);
        }
        public SchwabServices(ILogger logger, SchwabSettings settings, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings;
            _factory = factory;
            ctx = new TmdContext();
            _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
        }
        public SchwabServices(ILogger logger, SchwabSettings settings,TaAPISettings taAPISettings, IHttpClientFactory factory)
        {
            _logger = logger;
            _settings = settings;
            _factory = factory;
            _taapiSettings = taAPISettings;
            ctx = new TmdContext();
            _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
            taAPIService = new TaAPIService(_logger, _taapiSettings, _factory);
        }

        public void FetchToken(string code,ref bool success, bool refreshToken = false)
        {
            AuthenicationHelper authenicationHelper = new AuthenicationHelper();
            int retry = 0;
            try
            {
            GetToken:
                if (retry == 3) {
                    success = false;
                    return;
                }
                headers = new Dictionary<string, object>();
                headers.Add("accept", "*/*");
                headers.Add("User-Agent", "TMDInvestments");
                string contentType = "application/x-www-form-urlencoded";
                string payLoad = (!refreshToken) ? $"grant_type=authorization_code&code={code}&redirect_uri=https://127.0.0.1/SchwabAPICallBack": $"grant_type=refresh_token&refresh_token={System.Text.Encoding.Default.GetString((_token?.Refresh_Token ?? new byte[1]))}";
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                var bytes = Encoding.UTF8.GetBytes(_settings.ApiKey + ":" + _settings.ApiSecret);
                JsonElement results = client.Post("v1/oauth/token", payLoad, $"Basic {Convert.ToBase64String(bytes)}", headers, contentType);
                if (results.TryGetProperty("error", out JsonElement value) || results.TryGetProperty("errors", out JsonElement value2))
                {
                    retry++;
                    goto GetToken;
                }
                else
                {                    
                    //Token token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                    //token = (token == null) ? new Token() : token;
                    //Microsoft.EntityFrameworkCore.EntityState state = (token.Id == null) ? Microsoft.EntityFrameworkCore.EntityState.Added : Microsoft.EntityFrameworkCore.EntityState.Modified;
                    //authenicationHelper.AddToken(results, state, TokenType.SchwabToken, ref token);
                    authenicationHelper.AddToken(results, TokenType.SchwabToken);
                    //message = "Retrieving token successful";
                }
                success = true;
            }
            catch (Exception ex)
            {
                //error = ex.Message;
                success = false;
            }
        }
        //https://api.schwabapi.com/marketdata/v1/movers/%24DJI?sort=PERCENT_CHANGE_UP&frequency=0
        
        public JsonObject[] GetMovers()
        {
            int cnt = 0;
            JsonElement result;// = null;
            JsonObject[] results = new JsonObject[1];
            var markets = new List<string>(){ "$DJI", "$COMPX", "$SPX","NYSE", "NASDAQ"};
            bool success = false;
            indicesCalculatorV2 = new IndicesCalculatorV2(_logger, _settings, _factory/*, true, false*/);
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "application/json");

            foreach (var item in markets)
            {
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                try
                {
                    //results = client.Get("marketdata/v1/movers/" + market + "?sort=PERCENT_CHANGE_UP&frequency=0", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    //results = client.Get("marketdata/v1/movers/" + market, $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    result = client.Get("marketdata/v1/movers/" + item, $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                }
                catch (Exception ex) //Call may error on onget call being called twice
                {
                    client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                    //results = client.Get("marketdata/v1/movers/" + market + "?sort=PERCENT_CHANGE_UP&frequency=0", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    //result = client.Get("marketdata/v1/movers/" + market, $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    result = client.Get("marketdata/v1/movers/" + item, $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    //return results;
                }
                if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("errors", out JsonElement value))
                {
                    if (cnt == 3)
                    {
                        return results;
                    }
                    cnt++;
                    FetchToken("", ref success, true);
                    //return GetMovers(market);
                    _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                    goto Retry;
                }
                //cnt = 0;
                int movers = result.GetProperty("screeners").GetArrayLength() + results.Length;
                System.Array.Resize(ref results, movers);   
                foreach (var mover in result.GetProperty("screeners").EnumerateArray())
                {
                    results[cnt] = new JsonObject();
                    results[cnt] = indicesCalculatorV2.CalculateV2(mover.GetProperty("symbol").ToString(), mover);
                    //results[cnt].Add("Symbol", mover.GetProperty("symbol").ToString());
                    //results[cnt].Add("Name", mover.GetProperty("description").ToString());
                    //results[cnt].Add("lastPrice", mover.GetProperty("lastPrice").ToString());
                    cnt++;
                }
                
            }
            return results;
        }
        public void GetAccountInfo(ref SchwabAccountInfo results)
        {
            JsonElement accountNumbers = new JsonElement();
            bool success = false;
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "*/*");
            client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
            try
            {
                accountNumbers = client.Get("trader/v1/accounts/accountNumbers", $"Bearer {System.Text.Encoding.Default.GetString((_token?.Access_Token ?? new byte[1]))}", headers);
            }
            catch (Exception ex) //Call may error on onget call being called twice
            {
                //accountNumbers = new JsonElement();
                //client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                //accountNumbers = client.Get("trader/v1/accounts/accountNumbers", $"Bearer {System.Text.Encoding.Default.GetString((_token?.Access_Token ?? new byte[1]))}", headers);
                //results = accountNumbers;
            }
            if (accountNumbers.ValueKind == JsonValueKind.Object &&  accountNumbers.TryGetProperty("errors", out JsonElement value)) 
            {                
                if (fetchRetry == 3)
                {
                    //results = accountNumbers;
                    //return;
                }
                else
                {
                    fetchRetry++;
                    //System.Threading.Thread.Sleep(6000); //Five Seconds.
                    FetchToken("", ref success, true);
                    //System.Threading.Thread.Sleep(6000); //Five Seconds.
                    //GetAccountInfo(ref results);
                    _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                    goto Retry;
                }
            }
            if (accountNumbers.ValueKind == JsonValueKind.Object && accountNumbers.TryGetProperty("errors", out JsonElement value2))
            {
                //results = accountNumbers;
                return;
            }
            else
            {
                HttpClientServices<SchwabAccountInfo> client = new HttpClientServices<SchwabAccountInfo>(_factory, _settings, _logger);
                try
                {
                    results = client.Get($"trader/v1/accounts/{accountNumbers[0].GetProperty("hashValue").ToString()}?fields=positions", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    //return;
                }
                catch (Exception ex)
                {
                    //results = new JsonElement();
                    //client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                    //results = client.Get($"trader/v1/accounts/{accountNumbers[0].GetProperty("hashValue").ToString()}?fields=positions", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    //results = results;
                    //return;
                }
            }           
            //return results;
        }

        public void GetAccountInfo(ref JsonElement results)
        {
            JsonElement accountNumbers = new JsonElement();
            bool success = false;
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "*/*");
            client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
            try
            {
                accountNumbers = client.Get("trader/v1/accounts/accountNumbers", $"Bearer {System.Text.Encoding.Default.GetString((_token?.Access_Token ?? new byte[1]))}", headers);
            }
            catch (Exception ex) //Call may error on onget call being called twice
            {
                //accountNumbers = new JsonElement();
                //client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                //accountNumbers = client.Get("trader/v1/accounts/accountNumbers", $"Bearer {System.Text.Encoding.Default.GetString((_token?.Access_Token ?? new byte[1]))}", headers);
                //results = accountNumbers;
            }
            if (accountNumbers.ValueKind == JsonValueKind.Object && accountNumbers.TryGetProperty("errors", out JsonElement value))
            {
                if (fetchRetry == 3)
                {
                    //results = accountNumbers;
                    //return;
                }
                else
                {
                    fetchRetry++;
                    //System.Threading.Thread.Sleep(6000); //Five Seconds.
                    FetchToken("", ref success, true);
                    //System.Threading.Thread.Sleep(6000); //Five Seconds.
                    //GetAccountInfo(ref results);
                    _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                    goto Retry;
                }
            }
            if (accountNumbers.ValueKind == JsonValueKind.Object && accountNumbers.TryGetProperty("errors", out JsonElement value2))
            {
                results = accountNumbers;
                return;
            }
            else
            {
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                try
                {
                    results = client.Get($"trader/v1/accounts/{accountNumbers[0].GetProperty("hashValue").ToString()}?fields=positions", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    return;
                }
                catch (Exception ex)
                {
                    results = new JsonElement();
                    client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                    results = client.Get($"trader/v1/accounts/{accountNumbers[0].GetProperty("hashValue").ToString()}?fields=positions", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                    //results = results;
                    return;
                }
            }
            //return results;
        }
        public dynamic GetOrders(string fromDate, string toDate)
        {
            int? fetchRetry = 0;
            dynamic? results = null;
            bool success = false;
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "application/json");
            client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
            //&status=PENDING_ACTIVATION
            try
            {
                results = client.Get($"trader/v1/orders?fromEnteredTime={fromDate}&toEnteredTime={toDate}", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
            }
            catch (Exception ex)
            {
                //client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                //results = client.Get($"trader/v1/orders?fromEnteredTime={fromDate}&toEnteredTime={toDate}", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                //return results;
            }
            if (results?.ValueKind == JsonValueKind.Object && results?.TryGetProperty("errors", out JsonElement value))
            {
                if (fetchRetry == 3)
                {
                    //results = accountNumbers;
                    return results;
                }
                else
                {
                    fetchRetry++;
                    //System.Threading.Thread.Sleep(6000); //Five Seconds.
                    FetchToken("", ref success, true);
                    //System.Threading.Thread.Sleep(6000); //Five Seconds.
                    //GetAccountInfo(ref results);
                    _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                    goto Retry;
                }
            }
            return results;
        }
        public dynamic SetOrder(string order)
        {
            dynamic accountNumbers = null;
            dynamic results = null;
            bool success = false;
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "*/*");
            //headers.Add("Content-Type", "application/json");
            client = new HttpClientServices<dynamic>(_factory, _settings, _logger);

            //{ "complexOrderStrategyType": "NONE", "orderType": "LIMIT", "session": "NORMAL", "price": "6.45",
            //"duration": "DAY", "orderStrategyType": "SINGLE", "orderLegCollection": [ { "instruction": "BUY_TO_OPEN",
            //"quantity": 10, "instrument": { "symbol": "XYZ 240315C00500000", "assetType": "OPTION" } } ]}

            //order = {
            //    "orderType" : "LIMIT",
            //    "session" : "NORMAL",
            //    "price": "160",
            //    "duration" : "DAY",
            //    "orderStrategyType" : "SINGLE",
            //    "orderLegCollection" : [{
            //                    "instruction" : "BUY",
            //        "quantity" : 1,
            //        "instrument" :{
            //                        "symbol" : "AAPL",
            //            "assetType" : "EQUITY"
            //        }
            //                }

            //    ]
            //}

            //{
            //  "session": "NORMAL",
            //  "duration": "DAY",
            //  "orderType": "MARKET",
            //  "cancelTime": "2024-11-17T14:47:18.796Z",
            //  "complexOrderStrategyType": "NONE",
            //  "quantity": 0,
            //  "filledQuantity": 0,
            //  "remainingQuantity": 0,
            //  "destinationLinkName": "string",
            //  "releaseTime": "2024-11-17T14:47:18.796Z",
            //  "stopPrice": 0,
            //  "stopPriceLinkBasis": "MANUAL",
            //  "stopPriceLinkType": "VALUE",
            //  "stopPriceOffset": 0,
            //  "stopType": "STANDARD",
            //  "priceLinkBasis": "MANUAL",
            //  "priceLinkType": "VALUE",
            //  "price": 0,
            //  "taxLotMethod": "FIFO",
            //  "orderLegCollection": [
            //    {
            //                    "orderLegType": "EQUITY",
            //      "legId": 0,
            //      "instrument": {
            //                        "cusip": "string",
            //        "symbol": "string",
            //        "description": "string",
            //        "instrumentId": 0,
            //        "netChange": 0,
            //        "type": "SWEEP_VEHICLE"
            //      },
            //      "instruction": "BUY",
            //      "positionEffect": "OPENING",
            //      "quantity": 0,
            //      "quantityType": "ALL_SHARES",
            //      "divCapGains": "REINVEST",
            //      "toSymbol": "string"
            //    }
            //  ],
            //  "activationPrice": 0,
            //  "specialInstruction": "ALL_OR_NONE",
            //  "orderStrategyType": "SINGLE",
            //  "orderId": 0,
            //  "cancelable": false,
            //  "editable": false,
            //  "status": "AWAITING_PARENT_ORDER",
            //  "enteredTime": "2024-11-17T14:47:18.796Z",
            //  "closeTime": "2024-11-17T14:47:18.796Z",
            //  "accountNumber": 0,
            //  "orderActivityCollection": [
            //    {
            //                    "activityType": "EXECUTION",
            //      "executionType": "FILL",
            //      "quantity": 0,
            //      "orderRemainingQuantity": 0,
            //      "executionLegs": [
            //        {
            //                        "legId": 0,
            //          "price": 0,
            //          "quantity": 0,
            //          "mismarkedQuantity": 0,
            //          "instrumentId": 0,
            //          "time": "2024-11-17T14:47:18.796Z"
            //        }
            //      ]
            //    }
            //  ],
            //  "replacingOrderCollection": [
            //    "string"
            //  ],
            //  "childOrderStrategies": [
            //    "string"
            //  ],
            //  "statusDescription": "string"
            //}
            //{"message": "A validation error occurred while processing the request."}
            try
            {
                accountNumbers = client.Get("trader/v1/accounts/accountNumbers", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
            }
            catch (Exception ex) //Call may error on onget call being called twice
            {
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                accountNumbers = client.Get("trader/v1/accounts/accountNumbers", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                return accountNumbers;
            }
            if (accountNumbers.ValueKind == JsonValueKind.Object && accountNumbers.TryGetProperty("errors", out JsonElement value))
            {
                if (fetchRetry == 3)
                {
                    return accountNumbers;
                }
                //fetchRetry++;
                FetchToken("", ref success, true);
                //return SetOrder(order);
                _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                goto Retry;
            }
            try
            {
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                string contentType = "application/json";
                results = client.Post($"trader/v1//accounts/{accountNumbers[0].GetProperty("hashValue").ToString()}/orders", order, $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers, contentType);
            }
            catch (Exception ex)
            {
                //client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                //results = client.Get($"trader/v1/orders?fromEnteredTime={fromDate}&toEnteredTime={toDate}", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                return results;
            }
            return results;
        }
        public dynamic Search(string name)
        {
            dynamic results;
            List<dynamic> output = new List<dynamic>();
            bool success = false;
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "application/json");
            client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
            try
            {//symbol-search, desc-search
                results = client.Get($"marketdata/v1/instruments?symbol={name}&projection=symbol-search", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                try
                {
                    var temp = JsonSerializer.Deserialize<List<dynamic>>(JsonSerializer.Serialize(results.GetProperty("instruments")));
                    output.AddRange(temp);
                }
                catch (Exception)
                {

                }                
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                results = client.Get($"marketdata/v1/instruments?symbol={name}&projection=desc-search", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                try
                {
                    var temp = JsonSerializer.Deserialize<List<dynamic>>(JsonSerializer.Serialize(results.GetProperty("instruments")));
                    output.AddRange(temp);
                }
                catch (Exception)
                {

                }
            }
            catch (Exception ex)
            {
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                results = client.Get($"marketdata/v1/instruments?symbol={name}&projection=symbol-search", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                try
                {
                    var temp = JsonSerializer.Deserialize<List<dynamic>>(JsonSerializer.Serialize(results.GetProperty("instruments")));
                    output.AddRange(temp);
                }
                catch (Exception)
                {

                }
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                results = client.Get($"marketdata/v1/instruments?symbol={name}&projection=desc-search", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                try
                {
                    var temp = JsonSerializer.Deserialize<List<dynamic>>(JsonSerializer.Serialize(results.GetProperty("instruments")));
                    output.AddRange(temp);
                }
                catch (Exception)
                {

                }
                return output;
            }

            if (results.ValueKind == JsonValueKind.Object && results.TryGetProperty("errors", out JsonElement value))
            {
                if (fetchRetry == 3)
                {
                    return results;
                }
                fetchRetry++;
                FetchToken("", ref success, true);
                _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                //return Search(name);
                goto Retry;
            }
            return output;
        }
        public dynamic GetQuote(string symbol,bool useV2 = true, bool skipCalculation = false)
        {
            int fetchRetry = 0;
            symbol = symbol.ToUpper();
            dynamic results = null;
            bool success = false;
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "application/json");
            IndicesCalculator indicesCalculator = new IndicesCalculator(_logger, _settings, _polygon, _factory);
            indicesCalculatorV2 = new IndicesCalculatorV2(_logger, _settings, _factory/*, true, false*/);
            client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
            try
            {
                results = client.Get($"marketdata/v1/{symbol}/quotes?fields=quote,fundamental,extended,reference,regular", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
            }
            catch (Exception ex)
            {
                //client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                //results = client.Get($"marketdata/v1/{symbol}/quotes?fields=quote,fundamental,extended,reference,regular", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                //return results;
            }
            if (results.ValueKind == JsonValueKind.Object && results.TryGetProperty("errors", out JsonElement value))
            {
                if (fetchRetry == 3)
                {
                    return results;
                }
                else
                {
                    fetchRetry++;
                    FetchToken("",ref success, true);
                    _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                    goto Retry;
                }
            }
            if (!skipCalculation)
                if (useV2)
                    results = indicesCalculatorV2.CalculateV2(symbol, results);

            return results;
        }
        public dynamic GetQuotes(string symbols, bool useV2 = true, bool skipCalculation = false)
        {
            int fetchRetry = 0;
            symbols = symbols.ToUpper();
            dynamic results = null;
            bool success = false;
            JsonElement value = new JsonElement();
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "application/json");
            IndicesCalculatorV2 indicesCalculator = new IndicesCalculatorV2(_logger, _settings, _factory/*, false, true*/);
            client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
            try
            {
                results = client.Get($"marketdata/v1/quotes?symbols={HttpUtility.UrlEncode(symbols)}&fields=quote,fundamental,extended,reference,regular", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
            }
            catch (Exception ex)
            {
                //client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                //results = client.Get($"marketdata/v1/{symbol}/quotes?fields=quote,fundamental,extended,reference,regular", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                //return results;
            }
            if (results.ValueKind == JsonValueKind.Object && results.TryGetProperty("errors", out value) && value.ToString().Contains("401"))
            {
                if (fetchRetry == 3)
                {
                    return results;
                }
                else
                {
                    fetchRetry++;
                    FetchToken("", ref success, true);
                    //System.Threading.Thread.Sleep(6000); //Five Seconds.
                    _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                    goto Retry;
                }
                //return GetQuote(symbol, skipCalculation);
            }
            int index = 0;
            var quotes = symbols.Split(',');
            JsonObject[] returnObj = new JsonObject[quotes.Length];
            //results = JsonObject.Create(results);
            //results = JsonSerializer.Deserialize<JsonObject>(results.ToString());
            foreach (var item in quotes)
            {
                try
                {
                    var quoteData = ((JsonElement)results).GetProperty(item.ToUpper());
                    //var quoteData = results[item.ToUpper()];
                    //results = JsonElement.ParseValue(quoteData.ToString());
                    var calObject = indicesCalculator.CalculateV2(item.ToUpper(), quoteData);
                    returnObj[index] = calObject;
                    index++;
                }
                catch (Exception ex)
                {

                }

            }
            //if (!skipCalculation)
            //    if (useV2)
            //        results = indicesCalculator.CalculateV2(symbol, results);
            //else
            //    results = indicesCalculator.calculate(results, symbol);
            return returnObj;
        }
        public dynamic GetPriceHistory(string symbol, string periodType, string period, string frequencyType, int frequency, DateTime? startDate, DateTime? endDate)
        {
            dynamic results;
            bool success = false;
            long? start = null;
            long? end = null;

            if (startDate.HasValue && endDate.HasValue)
            {
                start = new DateTimeOffset(startDate.Value.ToUniversalTime()).ToUnixTimeMilliseconds();
                end = new DateTimeOffset(endDate.Value.ToUniversalTime()).ToUnixTimeMilliseconds();
            }
        Retry:
            headers = new Dictionary<string, object>();
            headers.Add("accept", "application/json");
            if(_settings == null)
            {
                var builder = new ConfigurationBuilder();
                builder.SetBasePath(Directory.GetCurrentDirectory())
                       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                IConfiguration config = builder.Build();
                _settings = config.GetSection("SchwabAPI").Get<SchwabSettings>();
            }
            client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
            try
            {
                //if (periodType != "day")
                //{
                //    results = client.Get($"marketdata/v1/pricehistory?symbol={symbol}&periodType={periodType}&period={1}&frequencyType={frequencyType}&frequency={frequency}&startDate={start}&endDate={end}", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                //}
                //else
                //{
                   results = client.Get($"marketdata/v1/pricehistory?symbol={symbol}&periodType={periodType}&period={1}&frequencyType={frequencyType}&frequency={frequency}", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                //}
            }
            catch (Exception ex)
            {
                client = new HttpClientServices<dynamic>(_factory, _settings, _logger);
                //if (periodType != "day")
                //{
                //    results = client.Get($"marketdata/v1/pricehistory?symbol={symbol}&periodType={periodType}&period={1}&frequencyType={frequencyType}&frequency={frequency}&startDate={start}&endDate={end}", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                //}
                //else
                //{
                    results = client.Get($"marketdata/v1/pricehistory?symbol={symbol}&periodType={periodType}&period={1}&frequencyType={frequencyType}&frequency={frequency}", $"Bearer {System.Text.Encoding.Default.GetString(_token.Access_Token)}", headers);
                //}
                return results;
            }
            if (results.ValueKind == JsonValueKind.Object && results.TryGetProperty("errors", out JsonElement value))
            {
                if (fetchRetry == 3)
                {
                    return results;
                }
                fetchRetry++;
                FetchToken("", ref success, true);
                //return GetPriceHistory(symbol, periodType, period, frequencyType, frequency, startDate, endDate);
                _token = ctx.Token.Where(x => x.TokenType == TokenType.SchwabToken).FirstOrDefault();
                goto Retry;
            }
            //results = calculate(results);
            return results;
        }

    }
}
