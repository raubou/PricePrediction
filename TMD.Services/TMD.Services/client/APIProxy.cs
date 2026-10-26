using MongoDB.Bson;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;
using TMDInvestments.Models;

namespace TMD.Services
{
    public class APIProxy<T> //where T : class
    {
        public APIProxy()
        {
            
        }
        public static T Get(string url, ref dynamic error, HttpClient _client)
        {
            T results = default(T);
            //using (HttpClient _client = new HttpClient())
            //{
                HttpResponseMessage responseMsg = _client.GetAsync(url).Result;
                if (responseMsg.IsSuccessStatusCode)
                {
                    results = JsonSerializer.DeserializeAsync<T>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                }
                else
                {
                    try
                    {
                        error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                    }
                    catch (System.Exception)
                    {
                        error = responseMsg.Content.ReadAsStringAsync().Result;
                    }
                }
                //_client.Dispose();
            //}
            return results;
        }
        public static T Get(string url, string token, ref dynamic error, HttpClient _client)
        {
            T results = default(T);
            //using (HttpClient _client = new HttpClient())
            //{
                _client.DefaultRequestHeaders.Authorization
                         = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage responseMsg = _client.GetAsync(url).Result;
                if(responseMsg.IsSuccessStatusCode)
                {
                    results = JsonSerializer.DeserializeAsync<T>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                }
                else
                {
                    try
                    {
                        error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                    }
                    catch (System.Exception)
                    {
                        error = responseMsg.Content.ReadAsStringAsync().Result;
                    }
                }
                //_client.Dispose();
            //}
            return results;
        }

        public static T Delete(string url, string token, ref dynamic error, HttpClient _client)
        {
            T results = default(T);
            //using (HttpClient _client = new HttpClient())
            //{
                _client.DefaultRequestHeaders.Authorization
                         = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage responseMsg = _client.DeleteAsync(url).Result;
                if (responseMsg.IsSuccessStatusCode)
                {
                    results = JsonSerializer.DeserializeAsync<T>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                }
                else
                {
                    try
                    {
                        error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                    }
                    catch (System.Exception)
                    {
                        error = responseMsg.Content.ReadAsStringAsync().Result;
                    }
                }
                //_client.Dispose();
            //}
            return results;
        }

        public static T Post(string url, HttpContent _content, ref dynamic error, HttpClient _client)
        {
            T results = default(T);
            //using (HttpClient _client = new HttpClient())
            //{
                HttpResponseMessage responseMsg = _client.PostAsync(url, _content).Result;
                if (responseMsg.IsSuccessStatusCode)
                {
                    results = JsonSerializer.DeserializeAsync<T>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                }
                else
                {
                    try
                    {
                        error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                    }
                    catch (System.Exception)
                    {
                        error = responseMsg.Content.ReadAsStringAsync().Result;
                    }
                }
                //_client.Dispose();
            //}
            return results;
        }

        public static T Post(string url, string token, HttpContent _content, ref dynamic error, HttpClient _client)
        {
            T results = default(T);
            //using (HttpClient _client = new HttpClient())
            //{
                _client.DefaultRequestHeaders.Authorization
                    = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage responseMsg = _client.PostAsync(url, _content).Result;
                if (responseMsg.IsSuccessStatusCode)
                {
                    results = JsonSerializer.DeserializeAsync<T>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                }
                else
                {
                    try
                    {
                        error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                    }
                    catch (System.Exception)
                    {
                        error = responseMsg.Content.ReadAsStringAsync().Result;
                    }
                }
                //_client.Dispose();
            //}
            return results;
        }

        public static T Put(string url, string token, HttpContent _content, ref dynamic error, HttpClient _client)
        {
            T results = default(T);
            //using (HttpClient _client = new HttpClient())
            //{
                _client.DefaultRequestHeaders.Authorization
                    = new AuthenticationHeaderValue("Bearer", token);
                HttpResponseMessage responseMsg = _client.PutAsync(url, _content).Result;
                if (responseMsg.IsSuccessStatusCode)
                {
                    results = JsonSerializer.DeserializeAsync<T>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                }
                else
                {
                    try
                    {
                        error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                    }
                    catch (System.Exception)
                    {
                        error = responseMsg.Content.ReadAsStringAsync().Result;
                    }
                }
                //_client.Dispose();
            //}
            return results;
        }

        public static T Send(HttpRequestMessage request, string accept, ref dynamic error, HttpClient _client)
        {
            T results = default(T);
            _client.DefaultRequestHeaders.Clear();
            //using (HttpClient _client = new HttpClient())
            //{
                if (accept != string.Empty)
                    _client.DefaultRequestHeaders
                      .Accept
                      .Add(new MediaTypeWithQualityHeaderValue(accept));
                HttpResponseMessage responseMsg = _client.SendAsync(request).Result;
                if (responseMsg.IsSuccessStatusCode)
                {
                    try
                    {
                        //results = JsonSerializer.DeserializeAsync<T>(responseMsg.Content.ReadAsStringAsync().Result, InitialSerializerOptions()).Result;
                        results = JsonSerializer.Deserialize<T>(responseMsg.Content.ReadAsStringAsync().Result);
                }
                    catch (System.Exception ex)
                    {
                        try
                        {
                            //dynamic response = new ExpandoObject(); 
                            var responseStream = responseMsg.Content.ReadAsStringAsync().Result;
                            //XDocument doc = XDocument.Parse(responseStream);
                            //var descendants = doc.Descendants().Elements();
                            //foreach (var link in links)
                            //{
                            //response.doc = doc;
                            //response = descendants;
                            //}
                            var serializer = new XmlSerializer(typeof(T));                            
                            //doc.Descendants("link").ToString();
                            //var links = doc.Descendants("links").Elements();
                            //StringReader reader = new StringReader(string.Concat(links.Select(x => x.ToString())));
                            StringReader reader = new StringReader(responseStream);
                            results = (T)serializer.Deserialize(reader);
                            //results = response;
                            //results = (T)descendants;
                            //results = (T)doc;
                        }
                        catch (System.Exception ex2)
                        {
                            //
                            try
                            {
                                dynamic response = new ExpandoObject();
                                var responseStream = responseMsg.Content.ReadAsStringAsync().Result;
                                XDocument doc = XDocument.Parse(responseStream);
                                var descendants = doc.Descendants().Elements();
                                response.doc = doc;
                                results = (T)response;
                            }
                            catch (System.Exception ex3)
                            {
                                error = ex3.Source + ": " + ex3.StackTrace + ": " + ex3.Message;//responseMsg.Content.ReadAsStringAsync().Result;
                            }
                        }
                        
                    }                    
                }
                else
                {
                    try
                    {
                        error = JsonSerializer.DeserializeAsync<dynamic>(responseMsg.Content.ReadAsStreamAsync().Result, InitialSerializerOptions()).Result;
                    }
                    catch (System.Exception)
                    {
                        error = responseMsg.Content.ReadAsStringAsync().Result;
                    }                    
                }
                //_client.Dispose();
            //}
            return results;
        }

        public static string SendString(HttpRequestMessage request, ref dynamic error, HttpClient _client)
        {
            string results = string.Empty;
            //using (HttpClient _client = new HttpClient())
            //{
                _client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));
                HttpResponseMessage responseMsg = _client.SendAsync(request).Result;
                if (responseMsg.IsSuccessStatusCode)
                {
                    results = responseMsg.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    error = responseMsg.Content.ReadAsStringAsync().Result;
                }
                //_client.Dispose();
            //}
            return results;
        }

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
