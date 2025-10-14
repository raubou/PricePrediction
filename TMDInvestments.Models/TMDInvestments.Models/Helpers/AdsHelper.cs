using NewsAggregator.models.returnModel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Xml.Serialization;

namespace NewsAggregator.helpers
{
    public class AdsHelper
    {
        //private static char ochar = (char)34; //double space
        //private static char ochar2 = (char)39; //single space
        //private static char ochar3 = (char)32; //single space        
        public static string error = string.Empty;
        public string CallLSLinks(string basicToken, string baseUrl, string userName, string password, string sid, string PageNum, string Type, string Keyword, ref int merchant)
        {
            string URL = string.Empty;
            RakutenModel response = null;            
            switch (Type)
            {
                case "productsearch":
                    URL = baseUrl + Type + "/1.0?pagenumber=" + PageNum + "&max=20";
                    if (Keyword != string.Empty)
                    {

                        URL += "&keyword=" + HttpUtility.UrlEncode(Keyword);

                    }
                    break;
                case "coupon":
                    URL = baseUrl + Type + "/1.0";
                    break;
                case "merchantsearch":
                    //type merchantsearch
                    URL = baseUrl + Type + "/1.0";
                    break;
            }
            GetRakuten(URL, baseUrl, basicToken, userName, password, sid, ref merchant, ref response);
            if(response != null)
            {
                using (var stringwriter = new StringWriter())
                {
                    var serializer = new XmlSerializer(response.GetType());
                    serializer.Serialize(stringwriter, response);
                    return stringwriter.ToString();
                }
            }
            return null;
        }

        private void GetRakuten(string URL, string baseUrl, string basicToken, string userName, string password, string sid, ref int merchant1, ref RakutenModel response)
        {
            TokenModel token;
            //RakutenModel response = null;
            RakutenMerchantModel merchants = null;
            Random rnd = new Random();
            token = GetLSToken(baseUrl, basicToken, userName, password, sid);
            if (token != null && token.access_token != null && token.access_token.Trim() != string.Empty)
            {
                merchants = GetMerchants(baseUrl, token);
                if(merchants != null && merchants.midlist.merchant.Count >= 0)
                {
                    int mIndex = rnd.Next(merchants.midlist.merchant.Count);
                    RakutenMerchantItems merchant = merchants.midlist.merchant[mIndex];
                    merchant1 = Convert.ToInt32(merchant.mid);
                    response = GetRakutenProducts(URL, token, merchant);
                    if (response.item == null || response.item.Count <= 0)
                    {
                        GetRakuten(URL, baseUrl, basicToken, userName, password, sid, ref merchant1, ref response);
                    }
                }
            }
            //return response;
        }
        private RakutenModel GetRakutenProducts(string URL, TokenModel token, RakutenMerchantItems merchant)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", token.access_token);
            return HttpHelper<RakutenModel>.SendRequest(HttpMethod.Get, URL + "&mid=" + merchant.mid, "Bearer", headers, "", ref error);
        }
        private TokenModel GetLSToken(string baseUrl, string basicToken, string userName, string password, string sid)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", basicToken);
            headers.Add("Content-Type", "application/x-www-form-urlencoded");
            //byte[] body = Encoding.ASCII.GetBytes("grant_type=password&username=" + userName + "&password=" + password + "&scope=" + sid);
            string body = "grant_type=password&username=" + userName + "&password=" + password + "&scope=" + sid;
            return HttpHelper<TokenModel>.SendRequest(HttpMethod.Post, baseUrl + "token", "Basic", headers, body, ref error);            
        }

        private static RakutenMerchantModel GetMerchants(string baseUrl, TokenModel token)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Authorization", token.access_token);
            return HttpHelper<RakutenMerchantModel>.SendRequest(HttpMethod.Get, baseUrl + "advertisersearch/1.0", "Bearer", headers, "", ref error);
        }
    }
}
