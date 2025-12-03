using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System.Data.SqlTypes;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Polygon;


namespace TMD.Services
{
    public sealed class HttpClientServices<P> where P : new() //where T : class
    {
        private readonly IHttpClientFactory? _factory;
        private HttpClient _httpClient;
        private readonly ILogger? _logger;
        private readonly dynamic? _settings;
        HttpResponseMessage? results;
        public HttpClientServices(IHttpClientFactory factory, dynamic settings, ILogger logger) {
            _factory = factory;
            //_httpClient = (_factory != null) ?_factory.CreateClient() : new HttpClient();
            if(_factory == null)
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(7), // Recreate every 15 minutes

                };

                _httpClient = new HttpClient(handler); 
            }
            else
            {
                _httpClient = _factory.CreateClient();
            }

            _settings = settings;
            _logger = logger;
            _httpClient.DefaultRequestVersion = HttpVersion.Version20;
            _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        }
        public HttpClientServices(IHttpClientFactory factory, ILogger logger)
        {
            _factory = factory;
            //_httpClient = (_factory != null) ?_factory.CreateClient() : new HttpClient();
            if (_factory == null)
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(7), // Recreate every 15 minutes

                };

                _httpClient = new HttpClient(handler);
            }
            else
            {
                _httpClient = _factory.CreateClient();
            }

            _logger = logger;
            _httpClient.DefaultRequestVersion = HttpVersion.Version20;
            _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        }
        public P? Get(string baseUrl, string url, string authorization, Dictionary<string, object> additionalHeaders)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
            var request = GetAuthorization(ref _httpClient, HttpMethod.Get, url, authorization, additionalHeaders, "", "");
            results = _httpClient
                .SendAsync(request).Result;
            //.GetAsync($"{url}").Result;
            if (results.StatusCode != System.Net.HttpStatusCode.OK)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.StatusCode = results.StatusCode;
                try
                {
                    error.errors.Details = results.Content.ReadFromJsonAsync<dynamic>().Result;
                }
                catch (Exception)
                {
                    error.errors.Details = results.Content.ReadAsStringAsync().Result;
                }
                error = JsonSerializer.SerializeToElement(error);
                return error;
            }
            //try
            //{
                return results.Content.ReadFromJsonAsync<P?>().Result;
            //}
            //catch (Exception)
            //{
            //    dynamic output = new ExpandoObject();
            //    output.Results = results.Content.ReadAsStringAsync().Result;
            //    return output;
            //}
            
        }
        public dynamic Get(string baseUrl, string url, string authorization)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
            var request = GetAuthorization(ref _httpClient, HttpMethod.Get, url, authorization, null, "", "");
            results = _httpClient
                .SendAsync(request).Result;
            //.GetAsync($"{url}").Result;
            if (results.StatusCode != System.Net.HttpStatusCode.OK)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.StatusCode = results.StatusCode;
                try
                {
                    error.errors.Details = results.Content.ReadFromJsonAsync<dynamic>().Result;
                    //return default(P);
                    return error;
                }
                catch (Exception)
                {
                    error.errors.Details = results.Content.ReadAsStringAsync().Result;
                    //return default(P);
                    return error;
                }
                error = JsonSerializer.SerializeToElement(error);
                return error;
            }
            try
            {
                return XElement.Parse(results.Content.ReadAsStringAsync().Result);
                //XmlSerializer serializer = new XmlSerializer(typeof(EtradeAccountInfo));
                //using var stringReader = new StringReader(results.Content.ReadAsStringAsync().Result);
                //P deserializedObject = (P)serializer.Deserialize(stringReader);
                //return deserializedObject;
            }
            catch (Exception)
            {
                //return results.Content.ReadAsStringAsync().Result;
                return default(P);
            }
        }
        public dynamic Get(string url)
        {
            //_httpClient.BaseAddress = new Uri(baseUrl);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
            //var request = GetAuthorization(ref _httpClient, HttpMethod.Get, url, authorization, null, "", "");
            results = _httpClient
                .SendAsync(request).Result;
            //.GetAsync($"{url}").Result;
            if (results.StatusCode != System.Net.HttpStatusCode.OK)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.StatusCode = results.StatusCode;
                try
                {
                    error.errors.Details = results.Content.ReadFromJsonAsync<dynamic>().Result;
                    //return default(P);
                    return error;
                }
                catch (Exception)
                {
                    error.errors.Details = results.Content.ReadAsStringAsync().Result;
                    //return default(P);
                    return error;
                }
                error = JsonSerializer.SerializeToElement(error);
                return error;
            }
            try
            {
                return XElement.Parse(results.Content.ReadAsStringAsync().Result);
                //XmlSerializer serializer = new XmlSerializer(typeof(EtradeAccountInfo));
                //using var stringReader = new StringReader(results.Content.ReadAsStringAsync().Result);
                //P deserializedObject = (P)serializer.Deserialize(stringReader);
                //return deserializedObject;
            }
            catch (Exception)
            {
                //return results.Content.ReadAsStringAsync().Result;
                return default(P);
            }
        }
        public P? GetModel(string baseUrl, string url, string authorization)
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
            var request = GetAuthorization(ref _httpClient, HttpMethod.Get, url, authorization, null, "", "");
            results = _httpClient
                .SendAsync(request).Result;
            //.GetAsync($"{url}").Result;
            if (results.StatusCode != System.Net.HttpStatusCode.OK)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.StatusCode = results.StatusCode;
                try
                {
                    error.errors.Details = results.Content.ReadFromJsonAsync<dynamic>().Result;
                    return default(P);
                }
                catch (Exception)
                {
                    error.errors.Details = results.Content.ReadAsStringAsync().Result;
                    return default(P);
                }
                error = JsonSerializer.SerializeToElement(error);
                return error;
            }
            try
            {
                //return XElement.Parse(results.Content.ReadAsStringAsync().Result);
                XmlSerializer serializer = new XmlSerializer(typeof(EtradeAccountInfo));
                using var stringReader = new StringReader(results.Content.ReadAsStringAsync().Result);
                P deserializedObject = (P)serializer.Deserialize(stringReader);
                return deserializedObject;
            }
            catch (Exception)
            {
                //return results.Content.ReadAsStringAsync().Result;
                return default(P);
            }
        }
        public P Get(string url, string authorization, Dictionary<string, object> additionalHeaders)
        {
            //var output = Activator.CreateInstance<P>();
            //var output = new P();
            //string baseUrl = (_settings?.Url == null) ? _settings.sandBoxURL : _settings.Url;
            _httpClient.BaseAddress = new Uri($"https://{_settings?.Url}");
            var request = GetAuthorization(ref _httpClient, HttpMethod.Get, url, authorization, additionalHeaders, "", "");
            results = _httpClient
                .SendAsync(request).Result;
            //.GetAsync($"{url}").Result;
            if (results.StatusCode != System.Net.HttpStatusCode.OK)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.StatusCode = results.StatusCode;
                try
                {
                    error.errors.Details = results.Content.ReadFromJsonAsync<dynamic>().Result;
                }
                catch (Exception)
                {

                }
                error = JsonSerializer.SerializeToElement(error);
                return error;

                //PropertyInfo property = output.GetType().GetProperty("error");
                //if (property != null && property.CanWrite)
                //{
                //    property.SetValue(output, results.Content.ReadAsStringAsync().Result);
                //}
                //return output;
            }
            var a = results.Content.ReadAsStringAsync().Result;
            try
            {
                var response = results.Content.ReadFromJsonAsync<P>().Result;
                return response;
            }
            catch (Exception)
            {
                //PropertyInfo property = output.GetType().GetProperty("error");
                //if (property != null && property.CanWrite)
                //{
                //    property.SetValue(output, results.Content.ReadAsStringAsync().Result);
                //}
                //return output;
                return default(P);
            }
            //return 
        }
        public P? Post(string url, string payLoad, string authorization, Dictionary<string, object> additionalHeaders, string contentType)
        {
            _httpClient.BaseAddress = new Uri($"https://{_settings?.Url}");
            var request = GetAuthorization(ref _httpClient, HttpMethod.Post, url, authorization, additionalHeaders, payLoad, contentType);
            HttpContent content = new StringContent(payLoad, Encoding.UTF8, contentType);
            results = _httpClient
            .PostAsync($"{url}",content).Result;
            if (results.StatusCode != System.Net.HttpStatusCode.OK && results.StatusCode != HttpStatusCode.Created)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.StatusCode = results.StatusCode;
                try
                {
                    error.errors.Details = results.Content.ReadFromJsonAsync<dynamic>().Result;
                }
                catch (Exception)
                {

                }
                error = JsonSerializer.SerializeToElement(error);
                return error;
            }
            if(results.StatusCode != HttpStatusCode.Created)
                return results.Content.ReadFromJsonAsync<P?>().Result;
            else
            {
                dynamic output = new ExpandoObject();
                output.status = "Created";
                return output;
            }
        }
        public P? Post(string baseUrl, string url, string payLoad, string authorization, string contentType = "application/x-www-form-urlencoded")
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
            var request = GetAuthorization(ref _httpClient, HttpMethod.Post, url, authorization, null, payLoad, contentType);
            //HttpContent content = new StringContent(payLoad, Encoding.UTF8, "application/x-www-form-urlencoded");
            results = _httpClient
                .SendAsync(request).Result;
            //.PostAsync($"{url}", content).Result;
            if (results.StatusCode != System.Net.HttpStatusCode.OK && results.StatusCode != HttpStatusCode.Created)
            {
                dynamic error = new ExpandoObject();
                error.errors = new ExpandoObject();
                error.errors.StatusCode = results.StatusCode;
                try
                {
                    error.errors.Details = results.Content.ReadFromJsonAsync<dynamic>().Result;
                }
                catch (Exception)
                {
                    error.errors.Details = results.Content.ReadAsStringAsync().Result;
                }
                error = JsonSerializer.SerializeToElement(error);
                return error;
            }
            if (results.StatusCode == HttpStatusCode.OK)
                try
                {
                    return results.Content.ReadFromJsonAsync<P?>().Result;
                }
                catch (Exception)
                {
                    dynamic output;
                    output = XElement.Parse(results.Content.ReadAsStringAsync().Result);
                    return output;
                }
                
            else
            {
                dynamic output = new ExpandoObject();
                output.status = "Created";
                return output;
            }
        }
        private HttpRequestMessage GetAuthorization(ref HttpClient httpClient, HttpMethod method, string url, string authorization, Dictionary<string,object> additionalHeaders, string content, string mediaType) {
            //if (authorization != null)
        //        httpClient.DefaultRequestHeaders.Authorization = 
        //new AuthenticationHeaderValue("Authorization", authorization);
        if(!string.IsNullOrEmpty(authorization))
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);
            if (additionalHeaders != null) {
                foreach (var header in additionalHeaders) { 
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value.ToString());
                }
            }
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            if (content != null && method == HttpMethod.Post)
            {
                request.Content = new StringContent(content,
                                                    Encoding.UTF8,
                                                    mediaType);//CONTENT-TYPE header
            }
            return request;
        }
    }
}
