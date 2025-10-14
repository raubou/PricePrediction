using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using TinyOAuth2;
using TMDInvestments.Context;
using TMDInvestments.Models;
using WikiNews2._0.helpers;

namespace TMD.Services
{
    public class AuthenicationHelper
    {
        static TmdContext ctx = null;
        static AuthenicationHelper() {
            ctx = new TmdContext();
        }
        public static HttpRequestMessage GetAuthenicatedRequest(HttpMethod method, string url, Dictionary<string, string> header, string contentType, string body)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url) { Version = new Version(2, 0) };
            foreach (var item in header)
            {
                request.Headers.Add(item.Key, item.Value);
            }
            if (string.IsNullOrEmpty(contentType))
                request.Content = new StringContent(body, Encoding.UTF8);
            else
                request.Content = new StringContent(body, Encoding.UTF8, contentType);
            return request;
        }
        public void AddToken(AccessTokenInfo results, TokenType tokenType)
        {
            bool isNewToken = false;
            Token token = null;
            token = ctx.Token.Where(x => x.TokenType == tokenType).FirstOrDefault();
            if (token == null)
            {
                token = new Token();
                token.TokenType = tokenType;
                token.DateEntered = DateTime.Now;
                isNewToken = true;
            }
            else
            {
                token.DateUpdated = DateTime.Now;
            }
            token.Access_Token = System.Text.Encoding.UTF8.GetBytes(results.AccessToken.ToString());
            token.Refresh_Token = System.Text.Encoding.UTF8.GetBytes(results.AccessTokenSecret.ToString());
            token.DateExpires = DateTime.Now.AddDays(7);
            token.AuditId = 1;

            if (isNewToken)
            {
                ctx.Token.Add(token);
                ctx.Entry(token).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }
            else
            {
                ctx.Token.Attach(token);
                ctx.Entry(token).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            ctx.SaveChanges();
            ctx.Entry(token).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            //System.Threading.Thread.Sleep(5000);
        }

        public void AddToken(JsonElement results, TokenType tokenType)
        {
            bool isNewToken = false;
            Token token = null;
            token = ctx.Token.Where(x => x.TokenType == tokenType).FirstOrDefault();
            if (token == null)
            {
                token = new Token();
                token.TokenType = tokenType; 
                token.DateEntered = DateTime.Now;
                isNewToken = true;
            }
            else
            {
                token.DateUpdated = DateTime.Now;
            }
            token.Access_Token = System.Text.Encoding.UTF8.GetBytes(results.GetProperty("access_token").ToString());
            token.Refresh_Token = System.Text.Encoding.UTF8.GetBytes(results.GetProperty("refresh_token").ToString());
            token.DateExpires = DateTime.Now.AddSeconds(Convert.ToInt32(results.GetProperty("expires_in").ToString()));
            token.AuditId = 1;

            if (isNewToken)
            {
                ctx.Token.Add(token);
                ctx.Entry(token).State = Microsoft.EntityFrameworkCore.EntityState.Added;
            }
            else
            {
                ctx.Token.Attach(token);
                ctx.Entry(token).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
            ctx.SaveChanges();
            ctx.Entry(token).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            //System.Threading.Thread.Sleep(5000);
        }
        //private HttpRequestMessage GetAuthenicatedRequest(HttpMethod method, dynamic model, ILogger _log)
        //{
        //    JsonSerializerOptions options = new JsonSerializerOptions
        //    {
        //        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        //        AllowTrailingCommas = true,
        //        PropertyNameCaseInsensitive = true,
        //        //IgnoreNullValues = true,
        //        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        //        IgnoreReadOnlyProperties = true,
        //        MaxDepth = 128,
        //        WriteIndented = false,
        //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //        ReadCommentHandling = JsonCommentHandling.Disallow
        //    };

        //    string jsonString = string.Empty;
        //    try
        //    {
        //        if (model != null && ((JsonElement)model).GetType() != null)
        //            jsonString = JsonSerializer.Serialize(model, options);
        //    }
        //    catch (Exception ex)
        //    {
        //        //Console.ForegroundColor = ConsoleColor.Red;
        //        //Console.WriteLine("An error has occured in your application" + ex.Source + " : " + ex.StackTrace + " : " + ex.Message);

        //        if (ex.InnerException != null)
        //        {
        //            //Console.WriteLine(ex.InnerException);
        //            _log.LogError(ex.InnerException?.Message ?? "");
        //        }
        //        //Console.BackgroundColor = ConsoleColor.Black;
        //        //Console.ForegroundColor = ConsoleColor.White;
        //        ////Console.Read();
        //    }

        //    ImpactAuthenicator.base64 = ImpactAuthenicator.GenerateSignature(_accountSID, _token);
        //    string url = "Mediapartners/" + _accountSID + "/" + _actionVerb;
        //    HttpRequestMessage request = new HttpRequestMessage(method, _baseUrl + url) { Version = new Version(2, 0) };
        //    request.Headers.Add("Authorization", "Basic " + ImpactAuthenicator.base64);
        //    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        //    return request;
        //}

        //private static HttpRequestMessage GetAuthenicatedRequest(HttpMethod method, string url, Dictionary<string, string> header, string contentType, string body)
        //{
        //    HttpRequestMessage request = new HttpRequestMessage(method, _baseUrl + url) { Version = new Version(2, 0) };
        //    foreach (var item in header)
        //    {
        //        request.Headers.Add(item.Key, item.Value);
        //    }
        //    request.Content = new StringContent(body, Encoding.UTF8, contentType);
        //    return request;
        //}

        //private HttpRequestMessage GetAuthenicatedRequest(HttpMethod method, string keyWord, dynamic model)
        //{
        //    JsonSerializerOptions options = new JsonSerializerOptions
        //    {
        //        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        //        AllowTrailingCommas = true,
        //        PropertyNameCaseInsensitive = true,
        //        IgnoreNullValues = true,
        //        IgnoreReadOnlyProperties = true,
        //        MaxDepth = 128,
        //        WriteIndented = false,
        //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //        ReadCommentHandling = JsonCommentHandling.Disallow
        //    };

        //    string jsonString = string.Empty;
        //    try
        //    {
        //        if (model != null && ((JsonElement)model).GetType() != null)
        //            jsonString = JsonSerializer.Serialize(model, options);
        //    }
        //    catch (Exception)
        //    {

        //    }

        //    ShareaSaleAuthenicator.ut = DateTime.Now.ToUniversalTime().ToString("r");
        //    x_ShareASale_Authentication = ShareaSaleAuthenicator.GenerateSignature(_actionVerb);
        //    string url = "action=" + _actionVerb + "&affiliateId=" + _affiliateid + "&token=" + ShareaSaleAuthenicator.apiToken + "&version=" + _apiVersion + "&keyword=" + keyWord;
        //    HttpRequestMessage request = new HttpRequestMessage(method, _baseUrl + url);
        //    request.Headers.Add("x-ShareASale-Authentication", x_ShareASale_Authentication);
        //    request.Headers.Add("x-ShareASale-Date", ShareaSaleAuthenicator.ut);
        //    request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");
        //    return request;
        //}
    }
}
