using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NewsAggregator.helpers
{
    public sealed class HttpHelper<T>
    {
        // HttpClient is intended to be instantiated once per application, rather than per-use. See Remarks.
        //private static readonly HttpClient client;
        //private static readonly HttpRequestMessage request;
        //static HttpHelper()
        //{
        //    client = new HttpClient();
        //}
        public static T SendRequest(HttpMethod method, string url, string Authorization, Dictionary<string, string> headers,string body, ref string error)
        {
            T results = default(T);
            ByteArrayContent byteArrayContent = null;
            using (HttpClient client = new HttpClient())
            {
                //StringContent content = new StringContent(body, Encoding.UTF8);
                var request = new HttpRequestMessage(method, url);
                //request.Headers.Add("User-Agent", "TMDInvestment/1.0");

                // Call asynchronous network methods in a try/catch block to handle exceptions.            
                if (body != null && body.Trim() != string.Empty && method == HttpMethod.Post)
                {
                    byteArrayContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
                    //content = new StringContent(body, Encoding.UTF8);
                    request.Content = byteArrayContent;
                    //request.Content = content;
                }
                //request.Headers.Clear();
                //client.DefaultRequestHeaders.Clear();
                foreach (var item in headers)
                {
                    if (item.Key == "Authorization")
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Authorization, item.Value);
                    }
                    else if (item.Key == "Content-Type")
                    {
                        byteArrayContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue(item.Value);
                        //content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(item.Value);
                        request.Headers.Add("Accept", item.Value);
                    }
                    else
                    {
                        request.Headers.Add(item.Key, item.Value);
                    }
                }

                try
                {
                    HttpResponseMessage response = client.SendAsync(request).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            var responseStream = response.Content.ReadAsStreamAsync().Result;
                            results = JsonSerializer.DeserializeAsync<T>(responseStream).Result;
                        }
                        catch (Exception)
                        {
                            var responseStream = response.Content.ReadAsStringAsync().Result;
                            //error = responseStream;
                            var serializer = new XmlSerializer(typeof(T));
                            StringReader reader = new StringReader(responseStream);
                            results = (T)serializer.Deserialize(reader);
                        }
                    }
                    else
                    {
                        var responseStream = response.Content.ReadAsStringAsync().Result;
                        error = responseStream;
                    }

                }
                catch (HttpRequestException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            //client.Dispose();
            return results;
        }
        //public static async Task<Stream> SendRequest(HttpMethod method, string url, Dictionary<string, string> headers, string body)
        //{
        //    Stream results = null;
        //    //ByteArrayContent byteArrayContent = null;
        //    using (HttpClient client = new HttpClient())
        //    {
        //        StringContent content = new StringContent(body, Encoding.UTF8);
        //        var request = new HttpRequestMessage(method, url);
        //        //request.Headers.Add("User-Agent", "TMDInvestment/1.0");

        //        // Call asynchronous network methods in a try/catch block to handle exceptions.            
        //        //if (body != null && body.Trim() != string.Empty && method == HttpMethod.Post)
        //        //{
        //        //byteArrayContent = new ByteArrayContent(body);
        //        //content = new StringContent(body, Encoding.UTF8);
        //        //request.Content = byteArrayContent;
        //        request.Content = content;
        //        //}            
        //        //request.Headers.Clear();
        //        //client.DefaultRequestHeaders.Clear();
        //        foreach (var item in headers)
        //        {
        //            //if (item.Key == "Authorization")
        //            //{
        //            //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Authorization, item.Value);
        //            //}
        //            //else if (item.Key == "Content-Type")
        //            if (item.Key == "Content-Type")
        //            {
        //                //byteArrayContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue(item.Value);
        //                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(item.Value);
        //                request.Headers.Add("Accept", item.Value);
        //            }
        //            else
        //            {
        //                request.Headers.Add(item.Key, item.Value);
        //            }
        //        }

        //        //try
        //        //{
        //            HttpResponseMessage response = await client.SendAsync(request);
        //            if (response.IsSuccessStatusCode)
        //            {
        //                //try
        //                //{
        //                var responseStream = await response.Content.ReadAsStreamAsync();
        //                //    results = await JsonSerializer.DeserializeAsync<T>(responseStream);
        //                //}
        //                //catch (Exception)
        //                //{
        //                //var responseStream = await response.Content.ReadAsStringAsync();
        //                results = responseStream;
        //                //    var serializer = new XmlSerializer(typeof(T));
        //                //    StringReader reader = new StringReader(responseStream);
        //                //    results = (T)serializer.Deserialize(reader);
        //                //}

        //            }

        //        //}
        //        //catch (HttpRequestException ex)
        //        //{
        //        //    throw ex;
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    throw ex;
        //        //}
        //    }
        //    //client.Dispose();
        //    return results;
        //}

        public static T Send(HttpRequestMessage request, ref dynamic error)
        {
            T results = default(T);
            HttpClientHandler handler = new HttpClientHandler();
            handler.AllowAutoRedirect = true;
            handler.UseCookies = true;
            handler.MaxAutomaticRedirections = 1000;
            handler.Credentials = CredentialCache.DefaultCredentials;
            using (HttpClient _client = new HttpClient(handler))
            {
                HttpResponseMessage responseMsg = _client.SendAsync(request).Result;
                if (responseMsg.IsSuccessStatusCode)
                {
                    results = JsonSerializer.DeserializeAsync<T>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                }
                else
                {
                    //try
                    //{
                    //    error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                    //}
                    //catch (System.Exception)
                    //{
                        error = responseMsg.Content.ReadAsStringAsync().Result;
                    //}
                }
                _client.Dispose();
            }
            return results;
        }

        //public static string ReadAsString(HttpRequestMessage request, ref dynamic error)
        //{
        //    string results = default(string);
        //    HttpClientHandler handler = new HttpClientHandler();
        //    handler.AllowAutoRedirect = true;
        //    handler.UseCookies = true;
        //    handler.MaxAutomaticRedirections = 1000;
        //    handler.Credentials = CredentialCache.DefaultCredentials;
        //    using (HttpClient _client = new HttpClient(handler))
        //    {
        //        HttpResponseMessage responseMsg = _client.SendAsync(request).Result;
        //        if (responseMsg.IsSuccessStatusCode)
        //        {
        //            results = responseMsg.Content.ReadAsStringAsync().Result;
        //        }
        //        else
        //        {
        //            //try
        //            //{
        //            //    error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
        //            //}
        //            //catch (System.Exception)
        //            //{
        //            error = responseMsg.Content.ReadAsStringAsync().Result;
        //            //}
        //        }
        //        _client.Dispose();
        //    }
        //    return results;
        //}
        //private HttpRequestMessage GetAuthenicatedRequest(HttpRequestMessage request, dynamic model, ref dynamic error)
        //{
        //    JsonSerializerOptions options = InitialSerializerOptions();
        //    //JsonSerializerOptions options = new JsonSerializerOptions
        //    //{
        //    //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        //    //    AllowTrailingCommas = true,
        //    //    PropertyNameCaseInsensitive = true,
        //    //    IgnoreNullValues = true,
        //    //    IgnoreReadOnlyProperties = true,
        //    //    MaxDepth = 128,
        //    //    WriteIndented = false,
        //    //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //    //    ReadCommentHandling = JsonCommentHandling.Disallow
        //    //};

        //    string jsonString = string.Empty;
        //    //if (model != null && ((JsonElement)model).GetType() != null)
        //    try
        //    {
        //        if (((JsonElement)model).GetType() != null)
        //            jsonString = JsonSerializer.Serialize(model, options);
        //    }
        //    catch (Exception)
        //    {

        //    }

        //    //CoinBaseAuthenicator.CB_ACCESS_TIMESTAMP = GetTimeStamp(ref error);
        //    //CoinBaseAuthenicator.CB_ACCESS_SIGN = CoinBaseAuthenicator.GenerateSignature(CoinBaseAuthenicator.CB_ACCESS_TIMESTAMP, method, url, jsonString, _apiSecret);
        //    //HttpRequestMessage request = new HttpRequestMessage(method, url);
        //    //request.Headers.Add("CB-ACCESS-KEY", CoinBaseAuthenicator.CB_ACCESS_KEY);
        //    //request.Headers.Add("CB-ACCESS-PASSPHRASE", CoinBaseAuthenicator.CB_ACCESS_PASSPHRASE);
        //    //request.Headers.Add("CB-ACCESS-TIMESTAMP", CoinBaseAuthenicator.CB_ACCESS_TIMESTAMP);
        //    //request.Headers.Add("CB-ACCESS-SIGN", CoinBaseAuthenicator.CB_ACCESS_SIGN);
        //    //request.Headers.Add("User-Agent", "TMDInvestment/1.0");
        //    //request.Headers.Add("Accept", "application/json");
        //    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        //    return request;
        //}

        private static JsonSerializerOptions InitialSerializerOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            options.PropertyNameCaseInsensitive = true;
            options.IgnoreNullValues = true;
            options.IgnoreReadOnlyProperties = true;
            options.MaxDepth = 128;
            options.WriteIndented = true;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            return options;
        }
    }
}
