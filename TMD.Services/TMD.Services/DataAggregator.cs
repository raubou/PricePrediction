using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Net;
using TMD.Database.Connect.Managers;
using TMD.Services.Service;
using TMDInvestment.Models;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.returnModel;

namespace TMD.Services
{
    public sealed class DataAggregator
    {
        private Manager? manager;
        private static IConfiguration? MongoDb;
        private static IConfiguration? connection;
        private ILogger? _log;
        private bool _isAzureFunction;
        string? conn = string.Empty;
        string? mongoConn = string.Empty;
        private static SqlConnection? _connection;
        private List<ImageFileObject>? imagefileObject;
        private NetworkCredential? credentials;
        public DataAggregator(ILogger? log, bool isAzureFunction)
        {
            _log = log;
            _isAzureFunction = isAzureFunction;
            if (!isAzureFunction)
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                MongoDb = configuration.GetSection("Mondodb");
                connection = configuration.GetSection("ConnectionStrings");
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
            _connection = new SqlConnection(conn);
            _connection.Open();
            //manager = new Manager(_connection, mongoConn, _log);
            manager = new Manager(conn, mongoConn, _log);
            imagefileObject = new List<ImageFileObject>();
        }
        public void LoadNewsData(HttpClient client)
        {
            NewsAggregatorService na = new NewsAggregatorService(_log, _isAzureFunction);
            _log?.LogInformation("Loading News Data");
            RssModel rssAll = new RssModel() { source = 4554, title = "News", items = new List<RssModelItems>() };
            credentials = new NetworkCredential("wikinewz", "Bitcomm104459");
            //DeleteImagesFromFTP("ftp://ftp.wikinewz.net/wwwroot/images/", credentials, _log);
            credentials = new NetworkCredential("tradergo", "Bitcomm104459");
            //DeleteImagesFromFTP("ftp://ftp.tradergo.com/wwwroot/images/", credentials, _log);
            manager?.DeleteNewsQueue();
            _log.LogInformation("Loading Finnhub News");
            na.GetFinnhubNewsFeed(_log, client, ref imagefileObject);
            rssAll.items.AddRange(na.GetFeed(NewsSite.vox, NewsType.TopStories, "home", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.NPR, NewsType.TopStories, "home", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.CNet, NewsType.TopStories, "home", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.WikiNews, NewsType.TopStories, "home", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.Mashable, NewsType.TopStories, "home", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.Guardian, NewsType.TopStories, "home", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.NewsAtlas, NewsType.TopStories, "home", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.MarketWatch, NewsType.TopStories, "finance", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.NYTimesBusiness, NewsType.TopStories, "finance", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.GuardianFinance, NewsType.TopStories, "finance", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.CNet, NewsType.Entertainment, "entertainment", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.dailyMail, NewsType.Entertainment, "entertainment", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.Mashable, NewsType.Entertainment, "entertainment", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.NewsAtlas, NewsType.Entertainment, "entertainment", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.DailyBeast, NewsType.TopStories, "home", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.dailyMail, NewsType.Travel, "travel", _log, client, ref imagefileObject).items);
            rssAll.items.AddRange(na.GetFeed(NewsSite.TravelandTourWorld, NewsType.Travel, "travel", _log, client, ref imagefileObject).items);
            //na.GetFeed(NewsSite.HuffintonPost, NewsType.TopStories, "home", _log, client);
            credentials = new NetworkCredential("wikinewz", "Bitcomm104459");
            //UploadImageToFTP("ftp://ftp.wikinewz.net/wwwroot/images/", imagefileObject, credentials, _log);
            credentials = new NetworkCredential("tradergo", "Bitcomm104459");
            //UploadImageToFTP("ftp://ftp.tradergo.com/wwwroot/images/", imagefileObject, credentials, _log);
            manager.AddBulk("[queue].[News]", rssAll.items);
            manager.TruncateNews();
            manager.MoveNewsQueue();
        }
        public void LoadNewsDataAsync(HttpClient client)
        {
            bool hasActiveThread = false;
            NewsAggregatorService? na = null;
            _log.LogInformation("Loading News Data");
            RssModel rssAll = new RssModel() { source = 4554, title = "News", items = new List<RssModelItems>() };
            credentials = new NetworkCredential("wikinewz", "Bitcomm104459");
            //DeleteImagesFromFTP("ftp://ftp.wikinewz.net/wwwroot/images/", credentials, _log);
            credentials = new NetworkCredential("tradergo", "Bitcomm104459");
            //DeleteImagesFromFTP("ftp://ftp.tradergo.com/wwwroot/images/", credentials, _log);
            manager.DeleteNewsQueue();
            Thread t1 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.vox, NewsType.TopStories, "home", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t1.IsBackground = false;
            t1.Name = "News 1";
            t1.Start();
            Thread t2 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.NPR, NewsType.TopStories, "home", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t2.IsBackground = false;
            t2.Name = "News 2";
            t2.Start();
            Thread t3 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.CNet, NewsType.TopStories, "home", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t3.IsBackground = false;
            t3.Name = "News 3";
            t3.Start();
            Thread t4 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.WikiNews, NewsType.TopStories, "home", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t4.IsBackground = false;
            t4.Name = "News 4";
            t4.Start();
            Thread t5 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.Mashable, NewsType.TopStories, "home", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t5.IsBackground = false;
            t5.Name = "News 5";
            t5.Start();
            Thread t6 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.Guardian, NewsType.TopStories, "home", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t6.IsBackground = false;
            t6.Name = "News 6";
            t6.Start();
            Thread t7 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.NewsAtlas, NewsType.TopStories, "home", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t7.IsBackground = false;
            t7.Name = "News 7";
            t7.Start();
            Thread t8 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.MarketWatch, NewsType.TopStories, "finance", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t8.IsBackground = false;
            t8.Name = "News 8";
            t8.Start();
            Thread t9 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.NYTimesBusiness, NewsType.TopStories, "finance", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t9.IsBackground = false;
            t9.Name = "News 9";
            t9.Start();
            Thread t10 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.GuardianFinance, NewsType.TopStories, "finance", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t10.IsBackground = false;
            t10.Name = "News 10";
            t10.Start();
            Thread t11 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.CNet, NewsType.Entertainment, "entertainment", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t11.IsBackground = false;
            t11.Name = "News 11";
            t11.Start();
            Thread t12 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.dailyMail, NewsType.Entertainment, "entertainment", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t12.IsBackground = false;
            t12.Name = "News 12";
            t12.Start();
            Thread t13 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.Mashable, NewsType.Entertainment, "entertainment", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t13.IsBackground = false;
            t13.Name = "News 13";
            t13.Start();
            Thread t14 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.NewsAtlas, NewsType.Entertainment, "entertainment", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t14.IsBackground = false;
            t14.Name = "News 14";
            t14.Start();
            Thread t15 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.DailyBeast, NewsType.TopStories, "home", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t15.IsBackground = false;
            t15.Name = "News 15";
            t15.Start();
            Thread t16 = new Thread(new ThreadStart(() => { na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.dailyMail, NewsType.Travel, "travel", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items); }));
            t16.IsBackground = false;
            t16.Name = "News 16";
            t16.Start();
            Thread t17 = new Thread(new ThreadStart(() => {
                na = new NewsAggregatorService(_log, _isAzureFunction); RssModel rss = na.GetFeed(NewsSite.TravelandTourWorld, NewsType.Travel, "travel", _log, client, ref imagefileObject); rssAll.items.AddRange(rss.items);
            }));
            t17.IsBackground = false;
            t17.Name = "News 17";
            t17.Start();

            do
            {
                hasActiveThread = false;
                //_log.LogInformation($"Thread {t2.Name} State {t2.ThreadState.ToString()}");
                if (t1.ThreadState != ThreadState.Stopped ||
                    t2.ThreadState != ThreadState.Stopped ||
                    t3.ThreadState != ThreadState.Stopped ||
                    t4.ThreadState != ThreadState.Stopped ||
                    t5.ThreadState != ThreadState.Stopped ||
                    t6.ThreadState != ThreadState.Stopped ||
                    t7.ThreadState != ThreadState.Stopped ||
                    t8.ThreadState != ThreadState.Stopped ||
                    t9.ThreadState != ThreadState.Stopped ||
                    t10.ThreadState != ThreadState.Stopped ||
                    t11.ThreadState != ThreadState.Stopped ||
                    t12.ThreadState != ThreadState.Stopped ||
                    t13.ThreadState != ThreadState.Stopped ||
                    t14.ThreadState != ThreadState.Stopped ||
                    t15.ThreadState != ThreadState.Stopped ||
                    t16.ThreadState != ThreadState.Stopped ||
                    t17.ThreadState != ThreadState.Stopped
                    )
                {
                    hasActiveThread = true;
                    _log.LogInformation("News thread is still running");
                }                    
                System.Threading.Thread.Sleep(200);
            } while (hasActiveThread);

            manager.AddBulk("[queue].[News]", rssAll.items);
            manager.TruncateNews();
            manager.MoveNewsQueue();
        }

        public void LoadAdData(HttpClient client)
        {
            int rakIndex = 1;
            RakutenService? rk = null;
            List<Category> categories = LoadCategory();
            ImpactService ipc = new ImpactService(_log, client, _isAzureFunction);
            CommissionJunctionService cj = new CommissionJunctionService(_log, client, _isAzureFunction);
            rk = new RakutenService(_log, client, _isAzureFunction);
            RssModel rss = new RssModel() { source = 5855, title = "Rakuten", items = new List<RssModelItems>() };
            manager.DeleteAdvertisingQueue();

            _log.LogInformation("Loading Rakuten Ads index ");
            MerchByAppStatusResponse merchants = RakutenService.LoadMerchants(_log);
            foreach (MerchantItem merchant in merchants.Items)
            {
                //foreach (var merchant in merchants.midlist.merchant)
                //{
                //while (rakIndex < 3000)
                //{
                ////Console.WriteLine("Loading Rakuten Ads " + merchant.merchantName);
                //Console.WriteLine("Loading Rakuten Ads index " + rakIndex);
                _log.LogInformation("Loading Rakuten Ads index " + rakIndex);
                rk = new RakutenService(_log, client, _isAzureFunction);
                //rk.GetProducts(categories, rakIndex);
                rk.GetProducts(merchant, categories, ref rss);
                rakIndex += 1;
                System.Threading.Thread.Sleep(1100);
            }


            if (rss.items.Count() > 0)
            {
                manager.AddBulk("[queue].[Advertising]", rss.items);
            }
            //rk.LoadAdvertising("rakuten");
            //System.Threading.Thread.Sleep(1000);
            //}

            //}

            ////Console.WriteLine("Loading Impact Ads");
            //_log.LogInformation("Loading Impact Ads");
            //ipc.GetProducts(categories);
            ////Console.WriteLine("Loading Commision Junction Ads");
            //_log.LogInformation("Loading Commision Junction Ads");
            //cj.GetProductsGraphQL(categories);
            //rk.fixContent();
            //manager.TruncateAdvertiser();
            manager.MoveAdvertisingQueue();
        }

        public void LoadAdDataAsync(HttpClient client)
        {
            int rakIndex = 0;
            RakutenService? rk = null;
            List<Category> categories = LoadCategory();
            ImpactService ipc = new ImpactService(_log, client, _isAzureFunction);
            CommissionJunctionService cj = new CommissionJunctionService(_log, client, _isAzureFunction);
            bool hasActiveThread1 = false;
            bool hasActiveThread2 = false;
            bool hasActiveThread3 = false;
            Thread[] t3 = new Thread[4000];
            RssModel rss = new RssModel() { source = 5855, title = "Rakuten", items = new List<RssModelItems>() };
            _log.LogInformation("Deleting Advertising Queue");
            manager.DeleteAdvertisingQueue();

            #region Impact Ads
            //try
            //{
            //    _log.LogInformation("Loading Impact Ads");
            //    Thread t1 = new Thread(new ThreadStart(() => ipc.GetProducts(categories)));
            //    t1.IsBackground = false;
            //    t1.Name = "Impact Ads";
            //    t1.Start();
            //    do
            //    {
            //        hasActiveThread2 = false;
            //        //_log.LogInformation($"Thread {t1.Name} State {t1.ThreadState.ToString()}");
            //        if (t1.ThreadState != ThreadState.Stopped)
            //            hasActiveThread2 = true;
            //        //System.Threading.Thread.Sleep(500);
            //    } while (hasActiveThread2);
            //}
            //catch (Exception ex)
            //{
            //    _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            //}
            #endregion

            #region Commission Junction Ads
            //try
            //{
            //    _log.LogInformation("Loading Commision Junction Ads");
            //    Thread t2 = new Thread(new ThreadStart(() => cj.GetProductsGraphQL(categories)));
            //    t2.IsBackground = false;
            //    t2.Name = "Commision Junction Ads";
            //    t2.Start();

            //    do
            //    {
            //        hasActiveThread3 = false;
            //        //_log.LogInformation($"Thread {t2.Name} State {t2.ThreadState.ToString()}");
            //        if (t2.ThreadState != ThreadState.Stopped)
            //            hasActiveThread3 = true;
            //        //System.Threading.Thread.Sleep(500);

            //    } while (hasActiveThread3);
            //}
            //catch (Exception ex)
            //{
            //    _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            //}
            #endregion

            #region Rakuten Ads
            try
            {
                _log.LogInformation("Loading Rakuten Ads index ");
                MerchByAppStatusResponse merchants = RakutenService.LoadMerchants(_log);
                foreach (MerchantItem merchant in merchants.Items)
                {
                    t3[rakIndex] = new Thread(new ThreadStart(() => { rk = new RakutenService(_log, client, _isAzureFunction); rk.GetProducts(merchant, categories, ref rss); }));
                    t3[rakIndex].Name = "RakutenService Index " + rakIndex.ToString();
                    t3[rakIndex].Priority = ThreadPriority.Highest;
                    t3[rakIndex].IsBackground = false;
                    t3[rakIndex].Start();
                    rakIndex += 1;
                    System.Threading.Thread.Sleep(1100);
                }

                do
                {
                    hasActiveThread1 = false;
                    foreach (var item in t3)
                    {
                        if (item != null)
                        {
                            //_log.LogInformation($"Thread {item.Name} State {item.ThreadState.ToString()}");
                            if (item.ThreadState != ThreadState.Stopped)
                                hasActiveThread1 = true;
                            //System.Threading.Thread.Sleep(500);                                                                             
                        }

                    }
                } while (hasActiveThread1);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Source + " : " + ex.StackTrace + " : " + ex.InnerException?.Message + " : " + ex.Message);
            }
            #endregion

            if (rss.items.Count() > 0)
            {
                manager.AddBulk("[queue].[Advertising]", rss.items);
                Thread.Sleep(1000);
            }

            //_log.LogInformation("Deleting Advertising Table");
            //manager.TruncateAdvertiser();
            _log.LogInformation("Moving from Queue to Advertising Table");
            manager.MoveAdvertisingQueue();
        }

        public void LoadStockData(HttpClient client)
        {
            TraderGoService tg = new TraderGoService(_log, client, _isAzureFunction);
            try
            {
                tg.StartRequests();
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
            }

        }

        public static string SetCategory(string title, List<Category> Category)
        {
            string strCategory = string.Empty;
            int count = 0;
            bool found = false;

            found = false;
            var foundCategory = new Category();
            foreach (var category in Category)
            {
                var keywords = category.Keywords.ToLower().Split(new char[] { ',' });
                foreach (var keyword in keywords)
                {
                    if (title.ToLower().Contains(keyword) || title.ToLower().Contains(category.Name))
                    {
                        found = true;
                        foundCategory.Id = category.Id;
                        foundCategory.Name = category.Name;
                        foundCategory.Keywords = category.Keywords;
                        break;
                    }
                }
                if (found)
                {
                    strCategory = foundCategory.Name;
                    count += 1;
                }
                else
                {

                }

            }
            return strCategory;
        }

        private List<Category> LoadCategory()
        {
            List<Category> Category = new List<Category>();
            Category.Add(new Category() { Id = 1, Name = "Clothing", Keywords = "dress, pant, pants, shirt, t-shirt, short, belt, cardigan, leggings, tee, jumper, jean, cotton, coat, pyjama, suit, panties, skirt, jacket, sweater, sweatshirt, v-neck, polo, overcoat, trouser, trousers, blouse, jumpsuit, jumpsuits, bra" });
            Category.Add(new Category() { Id = 2, Name = "Books", Keywords = "book, paperback, books, ebook, ebooks" });
            Category.Add(new Category() { Id = 3, Name = "Movies", Keywords = "dvd, Blue ray" });
            Category.Add(new Category() { Id = 4, Name = "Electronics", Keywords = "speaker, bluetooth" });
            Category.Add(new Category() { Id = 5, Name = "Computers", Keywords = "laptop, tablet, desktop, mouse, keyboard, surface, amd, intel, galaxy tab, monitor, notebook, display, sata, m.2, memory, drive, surface, amd, intel" });
            Category.Add(new Category() { Id = 6, Name = "Home", Keywords = "decor, Appliance, knives" });
            Category.Add(new Category() { Id = 7, Name = "Grocery", Keywords = "food" });
            Category.Add(new Category() { Id = 8, Name = "Beauty", Keywords = "nail, polish, beyond polish" });
            Category.Add(new Category() { Id = 9, Name = "Toys", Keywords = "dji, rc, flying, drone" });
            Category.Add(new Category() { Id = 10, Name = "Games", Keywords = "video games, xbox, playstation, nintendo, sega, headset" });
            Category.Add(new Category() { Id = 11, Name = "Accessories", Keywords = "bag, bags, shawl, scarf, tunic, sunglasses, sunglass, backpack" });
            Category.Add(new Category() { Id = 12, Name = "OutDoors", Keywords = "sports" });
            Category.Add(new Category() { Id = 13, Name = "Automotive", Keywords = "part, car" });
            Category.Add(new Category() { Id = 14, Name = "Shoes", Keywords = "heels, shoes, romper, loafer, boot, boots, asics, socks, sneaker, sneakers" });
            //Category.Add(new Category() { Id = 15, Name = "Appliance", Keywords = "" });
            //Category.Add(new Category() { Id = 16, Name = "Cameria", Keywords = "casio" });
            Category.Add(new Category() { Id = 17, Name = "Cell Phones", Keywords = "cell phone, cellular phone, smart phone" });
            Category.Add(new Category() { Id = 18, Name = "Audio", Keywords = "amp, Stereo, speaker" });
            //Category.Add(new Category() { Id = 19, Name = "Video Games", Keywords = "video games, xbox, ps, playstation, nintendo, sega" });
            Category.Add(new Category() { Id = 20, Name = "Music", Keywords = "dvd, cd, mp3, streaming" });
            Category.Add(new Category() { Id = 21, Name = "Wearables", Keywords = "wearos, wear os, android, watch, ios, iwatch, bud, wireless, headphone, earbud, anc" });
            Category.Add(new Category() { Id = 22, Name = "Sports", Keywords = "fitness" });
            Category.Add(new Category() { Id = 23, Name = "Fitness", Keywords = "sports" });
            Category.Add(new Category() { Id = 24, Name = "Furniture", Keywords = "home, bed, desk, cabnet, couch, table, sofa" });
            Category.Add(new Category() { Id = 25, Name = "Security", Keywords = "alarm" });
            Category.Add(new Category() { Id = 26, Name = "Wifi", Keywords = "router" });
            Category.Add(new Category() { Id = 27, Name = "Office", Keywords = "office funiture" });
            //Category.Add(new Category() { Id = 28, Name = "Recreation", Keywords = "" });
            Category.Add(new Category() { Id = 29, Name = "Tv", Keywords = "uhd, hdtv, tv, roku, fire tv" });
            //Category.Add(new Category() { Id = 30, Name = "RC Drones", Keywords = "" });
            //Category.Add(new Category() { Id = 31, Name = "Tablets", Keywords = "" });
            //Category.Add(new Category() { Id = 32, Name = "Laptop", Keywords = "" });
            return Category;
        }

        //public async Task DeleteImagesFromFTP(string root, NetworkCredential credentials, ILogger _log)
        //{
        //    //var credentials = new NetworkCredential("wikinewz", "Bitcomm104459");
        //    //FtpWebRequest listRequest = (FtpWebRequest)WebRequest.Create("ftp://ftp.wikinewz.net/wwwroot/images/");
        //    //FtpWebRequest listRequest = (FtpWebRequest)WebRequest.Create(root);
        //    FtpWebRequest listRequest = (FtpWebRequest)FtpWebRequest.Create(new Uri(root));
        //    listRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
        //    listRequest.Credentials = credentials;

        //    List<string> lines = new List<string>();

        //    using (FtpWebResponse listResponse = (FtpWebResponse)listRequest.GetResponse())
        //    using (Stream listStream = listResponse.GetResponseStream())
        //    using (StreamReader listReader = new StreamReader(listStream))
        //    {
        //        while (!listReader.EndOfStream)
        //        {
        //            lines.Add(listReader.ReadLine());
        //        }
        //    }

        //    foreach (string line in lines)
        //    {
        //        string[] tokens =
        //          line.Split(new[] { ' ' }, 9, StringSplitOptions.RemoveEmptyEntries);
        //        //string name = tokens[8];
        //        string name = tokens[3];
        //        string permissions = tokens[0];

        //        //string fileUrl = "ftp://ftp.wikinewz.net/wwwroot/images/" + name;

        //        if (permissions[0] == 'd')
        //        {
        //            //DeleteFtpDirectory(fileUrl + "/", credentials);
        //        }
        //        else
        //        {
        //            _log.LogInformation($"Deleting File {name}");
        //            FtpWebRequest deleteRequest = (FtpWebRequest)WebRequest.Create(root + name);
        //            deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
        //            deleteRequest.Credentials = credentials;
        //            deleteRequest.GetResponse();
        //        }
        //    }
        //}

        //public static async Task UploadImageToFTP(string root, List<ImageFileObject> fileObject, NetworkCredential credentials, ILogger _log)
        //{

        //    foreach (ImageFileObject files in fileObject)
        //    {
        //        try
        //        {
        //            //FtpWebRequest request = (FtpWebRequest)WebRequest.Create(root + files.FileName);
        //            FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(new Uri(root + files.FileName));
        //            request.Credentials = credentials;
        //            // By default KeepAlive is true, where the control connection is 
        //            // not closed after a command is executed.
        //            request.KeepAlive = false;

        //            // Set the data transfer type.
        //            request.UseBinary = true;

        //            // Set content length
        //            request.ContentLength = files.Image.Length;
        //            request.Method = WebRequestMethods.Ftp.UploadFile;
        //            Stream ftpstream = request.GetRequestStream();
        //            ftpstream.Write(files.Image, 0, files.Image.Length);
        //            ftpstream.Close();
        //            _log.LogInformation($"Upload File Complete, filename {files.FileName}");
        //        }
        //        catch (Exception ex)
        //        {
        //            _log.LogError("File upload " + files.FileName + " Failed " + ex.Source + " " + ex.StackTrace + " " + ex.InnerException?.Message + " " + ex.Message);
        //            try
        //            {
        //                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(new Uri(root + files.FileName));
        //                request.Credentials = credentials;
        //                // By default KeepAlive is true, where the control connection is 
        //                // not closed after a command is executed.
        //                request.KeepAlive = false;

        //                // Set the data transfer type.
        //                request.UseBinary = true;

        //                // Set content length
        //                request.ContentLength = files.Image.Length;
        //                request.Method = WebRequestMethods.Ftp.UploadFile;
        //                Stream ftpstream = request.GetRequestStream();
        //                ftpstream.Write(files.Image, 0, files.Image.Length);
        //                ftpstream.Close();
        //                _log.LogInformation($"Upload File Complete, filename {files.FileName}");
        //            }
        //            catch (Exception ex2)
        //            {
        //                _log.LogError("FIle upload " + files.FileName + " Failed " + ex2.Source + " " + ex2.StackTrace + " " + ex2.InnerException?.Message + " " + ex2.Message);
        //            }
        //        }               
        //    }
        //}
    }
}