using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
//using static System.Net.Mime.MediaTypeNames;
using System.Drawing;

namespace WikiNews2._0.helpers
{
    public sealed class ImageHelper
    {
        public static bool IsImageUrl(string image, bool checkurl, HttpClient client)
        {
            //if (System.IO.File.Exists(image))
            //{
            //    return true;
            //}
            if (string.IsNullOrEmpty(image))
                return false;
            if (checkurl)
            {
                //var client = new HttpClient();
                //client.Timeout = TimeSpan.FromSeconds(30);
                try
                {
                    var response = client.GetAsync(image, HttpCompletionOption.ResponseHeadersRead).Result;
                    if (!response.IsSuccessStatusCode)
                        return false;
                }
                catch (Exception ex)
                {
                    //client.Dispose();
                    //client = null;
                    return false;
                }
                //client.Dispose();
                //client = null;
            }
            return true;
        }

        //public static Stream GetImageFromContent(string synContent)
        //{
        //    string content = string.Empty;
        //    if (synContent != null)
        //    {
        //        //TextSyndicationContent tsc = (TextSyndicationContent)synContent;
        //        //XmlSyndicationContent tsc2 = (XmlSyndicationContent)synContent;
        //        //string input = "<root>" + tsc.Text.ToString().Trim().Replace("\n", "").Replace(System.Environment.NewLine, "") + "</root>";
        //        string input = "<root>" + synContent.Trim().Replace("\n", "").Replace(System.Environment.NewLine, "") + "</root>";
        //        byte[] ibyte = Encoding.ASCII.GetBytes(input);
        //        Stream stream = new MemoryStream(ibyte);
        //        XmlReader reader = XmlReader.Create(stream);
        //        string elementName = string.Empty;
        //        try
        //        {
        //            while (reader.Read())
        //            {
        //                if (reader.NodeType == XmlNodeType.Element)
        //                    elementName = reader.Name.Trim();
        //                switch (reader.NodeType)
        //                {
        //                    case XmlNodeType.Element:
        //                        if (elementName.ToLower() == "img")
        //                        {
        //                            content = reader.GetAttribute("src");
        //                            break;
        //                        }
        //                        break;
        //                }
        //                if (content.Trim() != string.Empty)
        //                    break;
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            //do logging
        //        }

        //    }
        //    return content;
        //}

        public static string GetImageFromContent(string synContent)
        {
            string content = string.Empty;
            if (synContent != null)
            {
                //TextSyndicationContent tsc = (TextSyndicationContent)synContent;
                //XmlSyndicationContent tsc2 = (XmlSyndicationContent)synContent;
                //string input = "<root>" + tsc.Text.ToString().Trim().Replace("\n", "").Replace(System.Environment.NewLine, "") + "</root>";
                string input = "<root>" + synContent.Trim().Replace("\n", "").Replace(System.Environment.NewLine, "") + "</root>";
                byte[] ibyte = Encoding.ASCII.GetBytes(input);
                Stream stream = new MemoryStream(ibyte);
                XmlReader reader = XmlReader.Create(stream);
                string elementName = string.Empty;
                try
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                            elementName = reader.Name.Trim();
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                if (elementName.ToLower() == "img")
                                {
                                    content = reader.GetAttribute("src");
                                    break;
                                }
                                break;
                        }
                        if (content.Trim() != string.Empty)
                            break;
                    }
                }
                catch (Exception)
                {
                    //do logging
                }

            }
            return content;
        }

        public static string AbstractImageURL(string content)
        {
            string url = string.Empty;
            int index1 = content.IndexOf("src");
            int index2 = content.Substring((index1 + 5)).IndexOf("'");
            //index2 = content.Substring((index1 + 6)).IndexOf("\"");
            //if (index2 > 0 && index1 > 0)
            if (index2 <= 0 || index1 <= 0)
            {
                index2 = content.Substring((index1 + 5)).IndexOf("\"");
                if (index2 > 0 && index1 > 0)
                {
                    url = content.Substring((index1 + 5), (index2));
                }
            }
            else
            {
                url = content.Substring((index1 + 5), (index2));
            }
            if (url.IndexOf("http") <= -1)// <= 0 && url.IndexOf("https") <= 0)
                url = "https:" + url;
            return url;
        }
    }
}
