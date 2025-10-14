using TMDInvestments.Models;
using System.ServiceModel.Syndication;
using System;
using System.Text;
using System.Xml;
using System.IO;
using System.Xml.Linq;

namespace NewsAggregator.helpers
{
    public sealed class NewsHelpers
    {
        public static string GetNewsType(NewsSite newsSites, NewsType newsType)
        {
            string rssFeed = string.Empty;
            switch (newsSites)
            {
                case NewsSite.google:
                    //no image
                    rssFeed = (newsType == NewsType.TopStories) ?"https://news.google.com/rss?hl=en-US&gl=US&ceid=US:en" :
                     "https://www.dailymail.co.uk/tvshowbiz/index.rss";
                    break;
                case NewsSite.dailyMail:
                    //image
                    rssFeed = (newsType == NewsType.TopStories) ?"https://www.dailymail.co.uk/articles.rss" :
                        (newsType == NewsType.Travel) ? "https://www.dailymail.co.uk/travel/index.rss":
                     "https://www.dailymail.co.uk/tvshowbiz/index.rss";
                    break;
                case NewsSite.vox:
                    //image on client
                    rssFeed = (newsType == NewsType.TopStories) ? "https://www.vox.com/rss/index.xml" : "";
                    break;
                case NewsSite.NYDailyNews:
                    //no image
                    rssFeed = (newsType == NewsType.TopStories) ? "https://www.nydailynews.com/arcio/rss/category/news/national/?query=display_date:%5Bnow-2d+TO+now%5D+AND+revision.published:true&sort=display_date:desc#nt=instory-link":
                        "https://www.nydailynews.com/arcio/rss/category/snyde/?query=display_date:%5Bnow-2d+TO+now%5D+AND+revision.published:true&sort=display_date:desc#nt=instory-link";
                    break;
                case NewsSite.cnn:
                    //image
                    rssFeed = (newsType == NewsType.TopStories) ? "http://rss.cnn.com/rss/cnn_topstories.rss": 
                        "http://rss.cnn.com/rss/cnn_showbiz.rss";
                    break;
                case NewsSite.NYTimes:
                    //image
                    rssFeed = (newsType == NewsType.TopStories) ? "https://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml":
                        "https://rss.nytimes.com/services/xml/rss/nyt/Television.xml";
                    break;
                case NewsSite.NPR:
                    //image
                    rssFeed = (newsType == NewsType.TopStories) ? "https://feeds.npr.org/1001/rss.xml" :
                        "https://feeds.npr.org/1008/rss.xml";
                    break;
                case NewsSite.NYPost:
                    //image
                    rssFeed = (newsType == NewsType.TopStories) ? "https://nypost.com/feed/" :
                        "https://nypost.com/entertainment/feed/";
                    break;
                case NewsSite.CNet:
                    //image
                    rssFeed = (newsType == NewsType.TopStories) ? "https://www.cnet.com/rss/news/" :
                        "https://www.cnet.com/rss/reviews/";
                    break;
                case NewsSite.NewsAtlas:
                    //image
                    rssFeed = (newsType == NewsType.TopStories) ? "https://newatlas.com/index.rss" :
                        "https://newatlas.com/lifestyle/index.rss";
                    break;
                case NewsSite.HuffintonPost:
                    //image
                    rssFeed = (newsType == NewsType.TopStories) ? "https://www.huffpost.com/section/front-page/feed" :
                        "https://www.huffpost.com/dept/entertainment/feed";
                    break;
                case NewsSite.NYTimesBusiness:
                    rssFeed = (newsType == NewsType.TopStories) ? "https://rss.nytimes.com/services/xml/rss/nyt/Business.xml" :
                        "https://www.huffpost.com/dept/entertainment/feed";
                    break;
                case NewsSite.MarketWatch:
                    rssFeed = (newsType == NewsType.TopStories) ? "http://feeds.marketwatch.com/marketwatch/topstories/" :
                        "";
                    break;
                case NewsSite.WikiNews:
                    rssFeed = (newsType == NewsType.TopStories) ? "https://en.wikinews.org/w/index.php?title=Special:NewsFeed&feed=atom&categories=Published&notcategories=No%20publish%7CArchived%7CAutoArchived%7Cdisputed&namespace=0&count=30&hourcount=124&ordermethod=categoryadd&stablepages=only" :
                        "";
                    break;
                case NewsSite.Mashable:
                    rssFeed = (newsType == NewsType.TopStories) ? "https://mashable.com/feeds/rss/all" :
                        "https://mashable.com/feeds/rss/entertainment";
                    break;
                case NewsSite.Guardian:
                    rssFeed = (newsType == NewsType.TopStories) ? "https://www.theguardian.com/world/rss" : "";
                    break;
                case NewsSite.GuardianFinance:
                    rssFeed = (newsType == NewsType.TopStories) ? "https://www.theguardian.com/business/economics/rss" : "";
                    break;
                case NewsSite.DailyBeast:
                    rssFeed = (newsType == NewsType.TopStories) ? "https://feeds.thedailybeast.com/rss/articles" : "";
                    break;
                case NewsSite.TravelandTourWorld:
                    rssFeed = (newsType == NewsType.Travel) ? "http://feeds.feedburner.com/travelandtourworld/home" : "";
                    break;
            }
            return rssFeed;
        }

        //public static string AbstractImageURL(string content)
        //{
        //    string url = string.Empty;
        //    int index1 = content.IndexOf("src");
        //    int index2 = content.Substring((index1 + 5)).IndexOf("'");
        //    if (index2 > 0 && index1 > 0)
        //    {
        //        url = content.Substring((index1 + 5), (index2));
        //    }
        //    return url;
        //}
        //public static string AbstractRawContent(SyndicationContent synContent)
        //{
        //    TextSyndicationContent tsc = (TextSyndicationContent)synContent;            
        //    return tsc.Text.ToString().Trim().Replace("\n", "").Replace(System.Environment.NewLine, "");
        //}

        public static string AbstractContent(SyndicationContent synContent)
        {
            string content = string.Empty;
            if (synContent != null)
            {
                TextSyndicationContent tsc = (TextSyndicationContent)synContent;
                //XmlSyndicationContent tsc2 = (XmlSyndicationContent)synContent;
                string input = "<root>" + tsc.Text.ToString().Trim().Replace("\n", "").Replace(System.Environment.NewLine, "") + "</root>";
                byte[] ibyte = Encoding.ASCII.GetBytes(input);
                Stream stream = new MemoryStream(ibyte);
                XmlReader reader = XmlReader.Create(stream);
                string elementName = string.Empty;
                try
                {
                    while (reader.Read())
                    {
                        if(reader.NodeType == XmlNodeType.Element)
                            elementName = reader.Name.Trim();
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Text:
                                if(elementName.ToLower() != "figcaption")
                                    content += reader.Value;
                                break;
                            case XmlNodeType.Whitespace:
                                if (elementName.ToLower() != "figcaption")
                                    content += reader.Value;
                                break;
                        }
                    }
                }
                catch (Exception)
                {
                    //do logging
                }
            }
              return content;
        }

        //public static string GetImageFromContent(SyndicationContent synContent)
        //public static string GetImageFromContent(string synContent)
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
        //                        if(elementName.ToLower() == "img")
        //                        {
        //                            content = reader.GetAttribute("src");
        //                            break;
        //                        }
        //                    break;
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
        //public static string AbstractRawContent(SyndicationContent synContent)
        //{
        //    string content = string.Empty;
        //    if (synContent != null)
        //    {
        //        TextSyndicationContent tsc = (TextSyndicationContent)synContent;
        //        //XmlSyndicationContent tsc2 = (XmlSyndicationContent)synContent;
        //        //string input = "<root>" + tsc.Text.ToString().Trim().Replace("\n", "").Replace(System.Environment.NewLine, "") + "</root>";
        //        content = tsc.Text.ToString().Trim().Replace("\n", "").Replace(System.Environment.NewLine, "");
        //    }
        //    else
        //    {
        //        content = "";
        //    }
        //    return content;
        //}

        //private static string SerializeItem(SyndicationItem item)
        //{
        //    var output = new StringBuilder();
        //    var formatter = new Atom10ItemFormatter(item);

        //    using (var writer = XmlWriter.Create(output))
        //    {
        //        formatter.WriteTo(writer);
        //    }

        //    return output.ToString();
        //}
    }
}
