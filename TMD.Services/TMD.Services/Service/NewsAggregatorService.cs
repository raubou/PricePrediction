using TMDInvestments.Models;
using NewsAggregator.helpers;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using WikiNews2._0.helpers;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using TMD.Database.Connect.Managers;
using Microsoft.Extensions.Logging;
using TMD.Services.Service;
using System.Data;
using TMD.Database.Connect.Helper;

namespace TMD.Services
{
    public sealed class NewsAggregatorService
    {
        public static Stopwatch timer = new Stopwatch();
        //private MongodbConnect _connect;
        private IConfiguration? MongoDb;
        private IConfiguration? connection;
        private Manager? manager;
        private ILogger? _log;
        string? conn = string.Empty;
        string? mongoConn = string.Empty;
        //private List<FileObject>? fileObject;
        //private NetworkCredential? credentials;
        dynamic? error;
        public NewsAggregatorService(ILogger? log, bool isAzureFunction)
        {
            _log = log;
            if (!isAzureFunction)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                MongoDb = configuration.GetSection("Mondodb");
                connection = configuration.GetSection("ConnectionStrings");
                //_connect = new MongodbConnect(MongoDb.GetSection("connection").Value, "NewsAggregator");
                conn = connection.GetSection("connection").Value;
                mongoConn = MongoDb.GetSection("connection").Value;
                //manager = new Manager(connection.GetSection("connection").Value, MongoDb.GetSection("connection").Value, _log);
            }
            else
            {
                conn = Environment.GetEnvironmentVariable("ConnectionString");
                mongoConn = Environment.GetEnvironmentVariable("MondodbConnection");
                //manager = new Manager(Environment.GetEnvironmentVariable("ConnectionString"), Environment.GetEnvironmentVariable("MondodbConnection"), _log);
            }
            //_connection = new SqlConnection(conn);
            manager = new Manager(conn, mongoConn, _log);
            //fileObject = new List<FileObject>();
        }
        public void GetFinnhubNewsFeed(ILogger _log, HttpClient client, ref List<ImageFileObject> imageFileObject)
        {
            timer.Start();
            _log.LogInformation("Loading Finnhub news");
            //RssModel rssAll = new RssModel() { source = 4554, title = "Finnhub news", items = new List<RssModelItems>() };
            //RssModel rss = new RssModel() { source = 34332, title = "Finnhub news", items = new List<RssModelItems>() };
            try
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                headers.Add("X-Finnhub-Token", TraderGoService.finnhub.finnhubKey.ToString());
                var news = GeneralHelper.GetRequest(headers, TraderGoService.finnhub.URL + TraderGoService.finnhub.GeneralNews, client, error);
                DataTable dt = new DataTable();
                GeneralHelper.AddColumnstoTable(new string[] { "Source", "Title", "Link", "Image", "ImageData", "Content", "Category", "PubDate" }, ref dt);
                foreach (var item in news?.EnumerateArray())
                {
                    DataRow dr = dt.NewRow();
                    _log.LogInformation($"Loading {item.GetProperty("headline").ToString()}");
                    try
                    {
                        dr["Source"] = item.GetProperty("source").ToString();
                        dr["Title"] = item.GetProperty("headline").ToString();
                        dr["Link"] = item.GetProperty("url").ToString();
                        dr["Image"] = item.GetProperty("image").ToString();
                        dr["Content"] = item.GetProperty("summary").ToString();
                        dr["Category"] = "finance";
                        var offset = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(item.GetProperty("datetime").ToString()));
                        dr["PubDate"] = Convert.ToDateTime(offset.LocalDateTime.ToString());
                        //var imageContent = client.GetByteArrayAsync(item.GetProperty("image").ToString()).Wait();
                        HttpResponseMessage response = client.GetAsync(item.GetProperty("image").ToString()).Result;
                        byte[] imageContent = response.Content.ReadAsByteArrayAsync().Result;
                        dr["ImageData"] = imageContent;
                        dt.Rows.Add(dr);
                        //imageFileObject.Add(new ImageFileObject() { Image = imageContent, FileName = item.GetProperty("image").ToString().Replace("https://", "").Replace("http://", "").Replace("&", "").Replace("?", "").Replace("=", "").Replace("=", "").Replace("/", "").Replace(",", "").Replace(".", "").Replace(" ", "").Trim() + ".png" });
                        imageFileObject.Add(new ImageFileObject() { Image = imageContent, FileName = (item.GetProperty("headline").ToString().Trim().Length > 15) ? item.GetProperty("headline").ToString().Trim().Substring(0, 15).Replace("https://", "").Replace("http://", "").Replace("&", "").Replace("?", "").Replace("=", "").Replace("/", "").Replace(",", "").Replace(".", "").Replace(":", "").Replace(" ", "").Replace("'", "").Replace("\"", "").Trim() + ".png" : item.GetProperty("headline").ToString().Trim().Replace("https://", "").Replace("http://", "").Replace("&", "").Replace("?", "").Replace("=", "").Replace("/", "").Replace(",", "").Replace(".", "").Replace(":", "").Replace("'", "").Replace(" ", "").Replace("'", "").Replace("\"", "").Trim() + ".png" });
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex.Message);
                    }
                }
                manager.AddBulk("[queue].[News]", dt.Select());
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Source + " " + ex.StackTrace + " " + ex.InnerException?.Message + " " + ex.Message);
            }
        }
        public RssModel GetFeed(NewsSite newsSite, NewsType newsType, string category, ILogger _log, HttpClient client, ref List<ImageFileObject> imageFileObject)
        {
            timer.Start();
            _log.LogInformation("Loading " + newsSite.ToString() + " " + newsType.ToString());
            //Console.WriteLine("Loading " + newsSite.ToString() + " " + newsType.ToString());
            RssModel rssAll = new RssModel() { source = 4554, title = newsSite.ToString(), items = new List<RssModelItems>() };
            RssModel rss = new RssModel() { source = 34332, title = newsSite.ToString(), items = new List<RssModelItems>() };
            try
            {
                rss = GetRSS(newsSite, newsType, client);
                _log.LogInformation("Found " + newsSite.ToString() + " " + newsType.ToString() + " " + rss.items.Count());
                //Console.WriteLine("Found " + newsSite.ToString() + " " + newsType.ToString() + " " + rss.items.Count());
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Source + " " + ex.StackTrace + " " + ex.Message);
                //Console.BackgroundColor = ConsoleColor.Red;
                //Console.WriteLine(ex.Source + " " + ex.StackTrace + " " + ex.Message);
                //Console.BackgroundColor = ConsoleColor.White;
            }

            try
            {
                foreach (var item in rss.items)
                {
                    item.Category = category;
                    DateTime pub = Convert.ToDateTime(item.pubDate);
                    TimeSpan duration = (DateTime.Now - pub);
                    HttpResponseMessage response = client.GetAsync(item.image).Result;
                    byte[] imageContent = response.Content.ReadAsByteArrayAsync().Result;
                    if (imageContent.Length > 0)
                    //if (ImageHelper.IsImageUrl(item.image.Replace("http:", "https:").Trim(), true, client) && item.link.Trim() != string.Empty && duration.Days <= 3)
                    {
                        item.DateCreated = DateTime.Now;
                        //UploadImageToFTP(item.ImageData, item.image.Replace("https://", "").Replace("http://", "").Replace("&", "").Replace("?", "").Replace("=", "").Replace("/", "").Replace(".", ""));
                        //imageFileObject.Add(new ImageFileObject() { Image = item.ImageData, FileName = item.image.Replace("https://", "").Replace("http://", "").Replace("&", "").Replace("?", "").Replace("=", "").Replace("=", "").Replace("/", "").Replace(",", "").Replace(".", "").Replace(" ", "").Trim() + ".png" });
                        imageFileObject.Add(new ImageFileObject() { Image = imageContent, FileName = (item.title.Trim().Length > 15) ? item.title.Trim().Substring(0, 15).Replace("https://", "").Replace("http://", "").Replace("&", "").Replace("?", "").Replace("=", "").Replace("/", "").Replace(",", "").Replace(".", "").Replace(":", "").Replace("'", "").Replace("`", "").Replace(" ", "").Replace("'", "").Replace("\"", "").Trim() + ".png" : item.title.Trim().Replace("https://", "").Replace("http://", "").Replace("&", "").Replace("?", "").Replace("=", "").Replace("/", "").Replace(",", "").Replace(".", "").Replace(":", "").Replace("'", "").Replace("`", "").Replace(" ", "").Replace("'", "").Replace("\"", "").Trim() + ".png" });
                        rssAll.items.Add(item);
                        _log.LogInformation($"adding image article {item.title.Trim()}");
                    }
                }

                _log.LogInformation("Loading " + newsSite.ToString() + " " + newsType.ToString() + " " + +rssAll.items.Count());
                //Console.WriteLine("Loading " + newsSite.ToString() + " " + newsType.ToString() + " " + + rssAll.items.Count());
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Source + " " + ex.StackTrace + " " + ex.Message);
                //Console.BackgroundColor = ConsoleColor.Red;
                //Console.WriteLine(ex.Source + " " + ex.StackTrace + " " + ex.Message);
                //Console.BackgroundColor = ConsoleColor.White;
            }
            //_connect.DeleteBySource("finance");
            //_connect.DeleteBySourceandCategory(newsSite.ToString(), category);
            //manager.DeleteNewsBySource(null, newsSite.ToString());
            if (rssAll.items.Count() > 0)
            {
                //Dictionary<string, dynamic> param = new Dictionary<string, dynamic>();
                //_connect.DeleteAll();
                //_connect.Save(rssAll);
                //manager.EditNews(rssAll);
            }
            else
            {
                _log.LogError("No items found from " + newsSite.ToString() + " " + newsType.ToString());
                //Console.WriteLine("No items found from " + newsSite.ToString() + " " + newsType.ToString());
            }
            timer.Stop();
            TimeSpan ts = timer.Elapsed;
            _log.LogInformation((nameof(NewsAggregatorService) + " Processing Time - " + ts.Hours + ":" + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + ":"));
            //Console.WriteLine(nameof(NewsAggregatorService) + " Processing Time - " + ts.Hours + ":" + ts.Minutes + ":" + ts.Seconds + ":" + ts.Milliseconds + ":");
            //System.Threading.Thread.Sleep(1000);
            return rssAll;
        }

        private RssModel GetRSS(NewsSite newsSites, NewsType newsType, HttpClient client)
        {
            //dynamic error = null; 
            RssModel rssModel = new RssModel() { items = new List<RssModelItems>() };
            XmlReader reader = null;
            string url = NewsHelpers.GetNewsType(newsSites, newsType);
            if (url.Trim() != "")
            {
                //XmlTextReader textReader = new XmlTextReader(url);
                try
                {
                    reader = XmlReader.Create(url);
                }
                catch (Exception ex)
                {
                    string mess = $"Exception: {ex.Source} {ex.StackTrace} {ex.InnerException?.Message} {ex.Message}";
                    //if(typeof(//Console))
                    //Console.WriteLine(mess);
                    //log.LogError(mess);
                    return rssModel;
                }
                //XDocument xDocument = XDocument.Load(url);
                //HttpRequestMessage mess = new HttpRequestMessage(HttpMethod.Get, url);
                //string xml = HttpHelper<string>.ReadAsString(mess, ref error);
                //var memory = new MemoryStream();
                //xDocument.Save(memory);
                //string xml = xDocument.ToString();
                //byte[] byteArray = Encoding.ASCII.GetBytes(xml);
                //MemoryStream stream = new MemoryStream(byteArray);
                //XmlReader reader = XmlReader.Create(stream);
                //string xmlText = Encoding.UTF8.GetString(memory.ToArray());
                //no index content can cause errors in the SyndicationFeed
                //XmlReader reader = RemoveNoIndexContent(memory);
                SyndicationFeed feed = new SyndicationFeed();
                XNamespace mediapfx = "http://search.yahoo.com/mrss/";
                //XNamespace mediapfx = "";
                Task.Run(() => feed = SyndicationFeed.Load(reader)).Wait();
                Rss20FeedFormatter _rsstofeed = null;
                Task.Run(() => { _rsstofeed = new Rss20FeedFormatter(feed); }).Wait();
                //Task.Run(() => {
                rssModel = new RssModel();
                rssModel.source = (int)newsSites;
                rssModel.title = _rsstofeed.Feed.Title.Text;
                rssModel.items = new List<RssModelItems>();
                foreach (var item in _rsstofeed.Feed.Items)
                {
                    try
                    {
                        //RssModelItems rssItem = new RssModelItems() { title = (item.Title != null) ? item.Title.Text : "", link = (item.Links.Count >= 1) ? item.Links[0].Uri.ToString() : "", image = (item.Links.Count > 1 && (item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".jpg" || item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".png" || item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".gif" || item.Links[1].MediaType == "image/jpeg")) ? item.Links[1].Uri.ToString() : "", pubDate = item.PublishDate.ToString(), source = (item.Summary != null) ? item.Summary.Text.Trim().Replace("\"", "'") : "",rawContent = (item.Content != null) ? ((TextSyndicationContent)item.Content).Text.Trim() : "" };
                        //RssModelItems rssItem = new RssModelItems() { title = (item.Title != null) ? item.Title.Text : "", link = (item.Links.Count >= 1) ? item.Links[0].Uri.ToString() : "", image = (item.Links.Count > 1 && (item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".jpg" || item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".png" || item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".gif" || item.Links[1].MediaType == "image/jpeg")) ? item.Links[1].Uri.ToString() : "", pubDate = (item.PublishDate.DateTime == DateTime.MinValue) ? item.LastUpdatedTime.DateTime : item.PublishDate.DateTime, source = (item.Summary != null) ? item.Summary.Text.Trim().Replace("\"", "'") : (item.Content != null) ? ((TextSyndicationContent)item.Content).Text.Trim() : "" };
                        RssModelItems rssItem = new RssModelItems() { title = (item.Title != null) ? item.Title.Text : "", link = (item.Links.Count >= 1) ? item.Links[0].Uri.ToString() : "", image = (item.Links.Count > 1 && (item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".jpg" || item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".png" || item.Links[1].Uri.ToString().Substring((item.Links[1].Uri.ToString().Length - 4), 4) == ".gif" || item.Links[1].MediaType == "image/jpeg")) ? item.Links[1].Uri.ToString() : "", pubDate = (item.PublishDate.DateTime == DateTime.MinValue) ? item.LastUpdatedTime.DateTime.ToString() : item.PublishDate.DateTime.ToString(), source = newsSites.ToString() };
                        rssItem.content = (item.Content == null) ? NewsHelpers.AbstractContent(item.Summary).Trim() : NewsHelpers.AbstractContent(item.Content).Trim();
                        //rssItem.rawContent = NewsHelpers.AbstractRawContent(item.Content);
                        var elements = item.ElementExtensions.Select(x => x.GetObject<XElement>());
                        foreach (var content in elements)
                        {
                            if (content.Attribute("medium") != null && content.Attribute("medium").Value == "image")
                            {
                                rssItem.image = content.Attribute("url").Value.Replace("http://", "https://");
                                rssItem.ImageData = client.GetByteArrayAsync(content.Attribute("url").Value.Replace("http://", "https://")).Result;
                                break;
                            }
                            else if (content.Name.LocalName == "encoded")
                            {
                                rssItem.image = ImageHelper.AbstractImageURL(content.Value.Trim());
                                rssItem.ImageData = client.GetByteArrayAsync(content.Value.Trim()).Result;
                            }
                            else if (content.Attribute("width") != null && Convert.ToInt32(content.Attribute("width").Value) > 140)
                            {
                                rssItem.image = content.Attribute("url").Value.Replace("http://", "https://");
                                rssItem.ImageData = client.GetByteArrayAsync(content.Attribute("url").Value.Replace("http://", "https://")).Result;
                                break;
                            }
                            else
                            {
                                foreach (var content2 in content.Descendants(mediapfx + "content"))
                                {
                                    if (content2.Attribute("medium") != null && content2.Attribute("medium").Value == "image")
                                    {
                                        rssItem.image = content2.Attribute("url").Value.Replace("http://", "https://");
                                        rssItem.ImageData = client.GetByteArrayAsync(content2.Attribute("url").Value.Replace("http://", "https://")).Result;
                                        break;
                                    }
                                }
                            }
                        }
                        //if image is till empty try last attempt in source or content
                        if (rssItem.image.Trim() == "")
                        {
                            //if (rssItem.content.Trim() != "")
                            //{
                            //    rssItem.image = NewsHelpers.AbstractImageURL(rssItem.content.Trim()).Replace("http://", "https://");
                            //}else
                            //if (rssItem.source.Trim() != "")
                            if (((TextSyndicationContent)item.Summary)?.Text != null && ((TextSyndicationContent)item.Summary)?.Text?.Trim() != "")
                            {
                                rssItem.image = ImageHelper.AbstractImageURL(((TextSyndicationContent)item.Summary).Text.Trim());
                                rssItem.ImageData = client.GetByteArrayAsync(ImageHelper.AbstractImageURL(((TextSyndicationContent)item.Summary).Text.Trim())).Result;
                            }
                            else if (((TextSyndicationContent)item.Content)?.Text != null && ((TextSyndicationContent)item.Content)?.Text?.Trim() != "")
                            {
                                rssItem.image = ImageHelper.AbstractImageURL(((TextSyndicationContent)item.Content).Text.Trim());
                                rssItem.ImageData = client.GetByteArrayAsync(ImageHelper.AbstractImageURL(((TextSyndicationContent)item.Content).Text.Trim())).Result;
                            }
                        }
                        //if image is till empty try last attempt in source or content
                        if (rssItem.image.Trim() == "")
                        {
                            rssItem.image = ImageHelper.GetImageFromContent((item.Summary != null) ? item.Summary.Text.Trim().Replace("\"", "'") : (item.Content != null) ? ((TextSyndicationContent)item.Content).Text.Trim() : "");
                            rssItem.ImageData = client.GetByteArrayAsync(ImageHelper.GetImageFromContent((item.Summary != null) ? item.Summary.Text.Trim().Replace("\"", "'") : (item.Content != null) ? ((TextSyndicationContent)item.Content).Text.Trim() : "")).Result;
                        }
                        if (!string.IsNullOrEmpty(rssItem.image.Trim()) && rssItem.ImageData != null)
                            //rssItem.parseSource = true;
                            rssModel.items.Add(rssItem);
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine($"An error has occured while abstracing the rss: {ex.Source} - {ex.StackTrace} - {ex.InnerException?.Message} - {ex.Message}");
                    }

                }
                //}).Wait();
            }
            return rssModel;
        }

    }
}