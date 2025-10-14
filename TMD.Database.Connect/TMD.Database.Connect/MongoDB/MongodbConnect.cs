using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using TMDInvestments.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewsAggregator.models.returnModel;
using System.IO;
using MongoDB.Driver.Linq;
using NewsAggregator.helpers;
using System.Security.Cryptography;

namespace TMD.Database.Connect
{
    public sealed class MongodbConnect : IDisposable
    {
        private string _connection;
        public string _collection;
        private MongoClientSettings settings;
        private MongoClient client;
        public IMongoDatabase database;
        public MongodbConnect(string connection, string collection) //: base()
        {
            try
            {
                _collection = collection;
                _connection = connection;
                settings = MongoClientSettings.FromConnectionString(_connection);
                settings.ServerApi = new ServerApi(ServerApiVersion.V1);
                client = new MongoClient(settings);
                database = client.GetDatabase("TriMarkDirect");
            }
            catch (Exception ex)
            {

            }
        }

        public bool Save(RssModel rss)
        {
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(_collection);
            List<BsonDocument> doc = new List<BsonDocument>();
            foreach (var item in rss.items)
            {
                if(!string.IsNullOrEmpty(item.title) && !string.IsNullOrEmpty(item.image) && !string.IsNullOrEmpty(item.link))
                    doc.Add(new BsonDocument()
                    {{ "_source", rss.title },
                        { "title", item.title },
                         { "link",item.link },
                         { "price",(item.price ?? "") },
                         { "image",item.image },
                         { "pubDate",(item.pubDate) },
                         { "content",(item.content ?? "") },
                         { "source",(item.source ?? "") },
                         { "Category",(item.Category ?? "") },
                         { "Advertiser",(item.Advertiser ?? "") },
                         { "Banner",(item.Banner ?? "")},
                         { "DateCreated",item.DateCreated }
                });
            }

            try
            {
                collection.InsertMany(doc);
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("An error has occured in saving. " + ex.Message);
                return false;
            }            
        }

        public bool SaveItem(string source, RssModelItems item)
        {
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>(_collection);
            BsonDocument doc = new BsonDocument()
                    {{ "_source", source },
                        { "title", item.title },
                         { "link",item.link },
                         { "price",(item.price ?? "") },
                         { "image",item.image },
                         { "pubDate",(item.pubDate) },
                         { "content",(item.content ?? "") },
                         { "source",(item.source ?? "") },
                         { "Category",(item.Category ?? "") },
                         { "Advertiser",(item.Advertiser ?? "") },
                         { "Banner",(item.Banner ?? "")},
                         { "DateCreated",item.DateCreated }
                };
            try
            {
                collection.InsertOne(doc);
                return true;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("An error has occured in saving. " + ex.Message);
                return false;
            }
        }

        public RssModel Get(string source)
        {
            //List<RssModelItems> collection = null;
            IEnumerable<RssModelItems> collection = null;
            RssModel rss = new RssModel();
            if(!string.IsNullOrEmpty(source))
            {
                //collection = database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x._source == source).ToList();
                collection = database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x._source == source).ToEnumerable();
            }                
            else
            {
                //collection = database.GetCollection<RssModelItems>(_collection).AsQueryable().Sample(300).ToList().GroupBy(x => x.title).Select(group => group.First()).ToList();
                collection = database.GetCollection<RssModelItems>(_collection).AsQueryable().Sample(300).ToList().GroupBy(x => x.title).Select(group => group.First());
            }
            rss.items = collection.ToList();
            //foreach (var item in collection.AsQueryable())
            //{
            //    rss.items.Add(new RssModelItems()
            //    {
            //        _source = item._source,
            //        title = item.title,
            //        link = item.link,
            //        price = item.price,
            //        image = item.image,
            //        pubDate = item.pubDate,
            //        content = item.content,
            //        source = item.source,
            //        Category = item.Category,
            //        Advertiser = item.Advertiser,
            //        Banner = item.Banner,
            //        DateCreated = item.DateCreated
            //    }); ;
            //}
            return rss;
        }

        public RssModel GetAll(int source, string title)
        {
            RssModel rss = new RssModel() { source = source, title = title };
            //IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            IEnumerable<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection).AsQueryable();
            rss.items = collection.ToList();
            //foreach (var item in collection.AsQueryable())
            //{
            //    rss.items.Add(new RssModelItems()
            //    {
            //        _id = item._id,
            //        _source = item._source,
            //        title = item.title,
            //        link = item.link,
            //        price = item.price,
            //        image = item.image,
            //        pubDate = item.pubDate,
            //        content = item.content,
            //        source = item.source,
            //        Category = item.Category,
            //        Advertiser = item.Advertiser,
            //        Banner = item.Banner,
            //        DateCreated = item.DateCreated
            //    }); ;
            //}
            return rss;
        }

        public RssModel GetAdByTitleAndLink(string title, string link)
        {
            RssModel rss = new RssModel();
            var collection = database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x.title == title && x.link == link).ToEnumerable();
            rss.items = collection.ToList();
            return rss;
        }

        public RssModel GetAdByTitleLinkAndImage(string title, string link, string image)
        {
            RssModel rss = new RssModel();
            var collection = database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x.title == title && x.link == link && x.image == image).ToEnumerable();
            rss.items = collection.ToList();
            return rss;
        }

        public RssModel GetAdsByBanner(string banner)
        {
            RssModel rss = new RssModel();
            var collection = database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x.Banner == banner).ToEnumerable();
            rss.items = collection.ToList();
            return rss;
        }

        public void DeleteBySource(string source)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var filter = Builders<RssModelItems>.Filter.Eq("_source", source);
            var result = collection.DeleteMany(filter);
            //Console.WriteLine(result.DeletedCount + " Previous Deleted from " + source);
        }

        public void DeleteByCategory(string category)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var filter = Builders<RssModelItems>.Filter.Where(x => x.Category == category);
            var result = collection.DeleteMany(filter);
            //Console.WriteLine(result.DeletedCount + " Previous Deleted from Category " + category);
        }

        public void DeleteBySourceandCategory(string source, string category)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var filter = Builders<RssModelItems>.Filter.Where(x => x._source == source && x.Category == category);
            var result = collection.DeleteMany(filter);
            //Console.WriteLine(result.DeletedCount + " Previous Deleted from " + source + " Category " + category);
        }

        public void DeleteBySourceandAdvertiser(string source, string advertiser)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var filter = Builders<RssModelItems>.Filter.Where(x => x._source == source && x.Advertiser == advertiser);
            var result = collection.DeleteMany(filter);
            //Console.WriteLine(result.DeletedCount + " Deleted from " + source + " Advertiser " + advertiser);
        }

        public void DeleteByAdvertiser(string advertiser)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var emptyFilter = Builders<RssModelItems>.Filter.Eq("Advertiser", advertiser);
            var result = collection.DeleteMany(emptyFilter);
            //Console.WriteLine(result.DeletedCount + " Previous Deleted from " + advertiser);
        }

        public void DeleteAll()
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var emptyFilter = Builders<RssModelItems>.Filter.Empty;
            var result = collection.DeleteMany(emptyFilter);
            //Console.WriteLine(result.DeletedCount + " Previous Deleted ");
        }

        public void DeleteByTitleAndLink(string title, string link)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var filter = Builders<RssModelItems>.Filter.Where(x => x.title == title && x.link == link);
            var result = collection.DeleteMany(filter);
            //Console.WriteLine(result.DeletedCount + " Deleted Title " + title + " and Link " + link);
        }

        public bool DeleteByTitleLinkAndImageActiveAds(string title, string link, string image)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var filter = Builders<RssModelItems>.Filter.Where(x => x.title == title && x.link == link && x.image == image);
            var result = collection.DeleteMany(filter);          
            //Console.WriteLine(result.DeletedCount + " Deleted Title " + title + " and Link " + link);
            return result.IsAcknowledged;
        }

        public bool UpdateByTitleLinkAndImageActiveAds(string category, string banner, RssModelItems item)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var filter = Builders<RssModelItems>.Filter.Where(x => x.title == item.title && x.link == item.link && x.image == item.image);
            BsonDocument doc = new BsonDocument()
                    { { "_source", "Active Ads" },
                                { "title", item.title },
                                 { "link",item.link },
                                 { "price",item.price },
                                 { "image",item.image },
                                 { "pubDate",item.pubDate },
                                 { "content",item.content },
                                 { "source",item.source },
                                 { "Category",category },
                                 { "Advertiser",item.Advertiser },
                                 { "Banner",banner },
                                 { "DateCreated",item.DateCreated }
                        };
            var result = collection.UpdateOne(filter, doc);
            ////Console.WriteLine(result.DeletedCount + " Deleted Title " + title + " and Link " + link);
            return result.IsAcknowledged;
        }

        public void DeleteById(ObjectId Id)
        {
            IMongoCollection<RssModelItems> collection = database.GetCollection<RssModelItems>(_collection);
            var filter = Builders<RssModelItems>.Filter.Where(x => x._id == Id);
            var result = collection.DeleteMany(filter);
            //Console.WriteLine(result.DeletedCount + " Deleted object Id " + Id);
        }

        public RssModel Search(string text)
        {
            IEnumerable<RssModelItems> results;// = new List<RssModelItems>();
            RssModel rss = new RssModel() { title = "Ads Search", source = 23322 };
            //var command =
            //      new BsonDocument[] {
            //      new BsonDocument("$search", new BsonDocument {
            //        {
            //          "index",
            //          "AdsLoad"
            //        },
            //        {
            //          //"text",
            //          "autocomplete",
            //          new BsonDocument {
            //            {
            //              "query",
            //              text
            //            },
            //            {
            //              "path",
            //              "title"
            //            }
            //          }
            //        }
            //      })
            //    };

            //var command2 =
            //new BsonDocument[] {
            //      new BsonDocument("$search", new BsonDocument {
            //          { "index", "AdsLoad" },
            //          {
            //            "compound", new BsonDocument {
            //                {"should",new BsonDocument[] {
            //                    {new BsonDocument {
            //                        { "autocomplete", new BsonDocument {
            //                                    { "query", text }, { "path", "title" }
            //                                }
            //                            //    },
            //                            //{ new BsonDocument {
            //                            //    { "autocomplete", new BsonDocument {
            //                            //        { "query", text }, { "path", "title" }
            //                            //    }
            //                            //    }
            //                            //    }
            //                         }
            //                    }} // 186
            //                } } //185
            //            } //184
            //          } //183 
            //      }) //181
            //}; //180 

            //var command = BsonDocument.Parse("[{'$search': {'index': 'AdsLoad', 'compound': { 'should': [{'autocomplete': { 'query':'" + text + "', 'path': 'title' } }, {'autocomplete': {'query':'" + text + "','path': 'content'}}]}}}]");
            //var command = BsonDocument.Parse("{'$search': {'index': 'AdsLoad', 'compound': { 'should': [{'autocomplete': { 'query':'" + text + "', 'path': 'title' } }, {'autocomplete': {'query':'" + text + "','path': 'content'}}]}}}");
            //var command = new BsonDocument[] { BsonDocument.Parse("{'$search': {'index': 'AdsLoad', 'compound': { 'should': [{'autocomplete': { 'query':'" + text + "', 'path': 'title' } }, {'autocomplete': {'query':'" + text + "','path': 'content'}}]}}}") };
            //var command = new BsonDocument[] { BsonDocument.Parse("{'$search': {'index': 'AdsLoad', 'autocomplete': { 'query':'" + text + "', 'path': 'title' }}}") };
            var command = new BsonDocument[] { BsonDocument.Parse("{'$search': {'index': 'AdsLoad', 'autocomplete': { 'query':'" + text + "', 'path': 'title'}}}"), BsonDocument.Parse("{'$limit' : 300}") };

            //var results = database.RunCommand<List<RssModelItems>>(command);

            try
            {
                var collection = database.GetCollection<RssModelItems>(_collection);//.Distinct(x => x.title, new BsonDocument());
                //var option = new FindOptions() { Collation = new Collation("en", strength: CollationStrength.Primary) };

                results = collection.Aggregate<RssModelItems>(command)
                .ToList().GroupBy(x => x.title)
                .Select(group => group.First());//.ToList().Take(500).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Mongo DB Search Fail:", ex);
            }
            rss.items = results.ToList();
            return rss;
        }

        public RssModel GetByCategory(string category)
        {
            RssModel rss = new RssModel() { title = "News", source = 23322 };
            //var collection = database.GetCollection<RssModelItems>(_collection);
            //var filter = Builders<RssModelItems>.Filter.Eq("Category", category);
            //var results = collection.Find(filter)
            var results = database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x.Category == category).ToEnumerable();
            //.Skip(0)
            //.Limit(100)
            //.ToList();
            rss.items = results.ToList();
            return rss;
        }

        public RssModel GetByCategory(string category, int start, int length)
        {
            RssModel rss = new RssModel() { title = "News", source = 23322 };
            //var collection = database.GetCollection<RssModelItems>(_collection);
            //var filter = Builders<RssModelItems>.Filter.Eq("Category", category);
            //var results = collection.Find(filter)
            //var results = database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x.Category == category)
            var results = database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x.Category == category)
            .Skip(start)
            .Limit(length).ToList();
            //.ToList();
            if (results != null)
            results.Shuffle<RssModelItems>();
            //rss.items = results.Skip(start).Take(length).ToList();
            rss.items = results;
            return rss;
        }

        public bool Update(string title, string link, RssModel rss)
        {
            bool succeess = true;
            try
            {
                var collection = database.GetCollection<RssModelItems>(_collection);
                var filter = Builders<RssModelItems>.Filter.Where(x => x.title == title && x.link == link);
                foreach (var item in rss.items)
                {
                    BsonDocument doc = new BsonDocument()
                    { { "_source", rss.source },
                                { "title", item.title },
                                 { "link",item.link },
                                 { "price",item.price },
                                 { "image",item.image },
                                 { "pubDate",item.pubDate },
                                 { "content",item.content },
                                 { "source",item.source },
                                 { "Category",item.Category },
                                 { "Advertiser",item.Advertiser },
                                 { "Banner",item.Banner },
                                 { "DateCreated",item.DateCreated }
                        };
                        collection.UpdateOneAsync(filter, doc);
                }
            }
            catch (Exception ex)
            {
                succeess = false;
            }
            return succeess;
        }
        public RssModelItems FindDuplicateRecord(RssModelItems item)
        {
            return database.GetCollection<RssModelItems>(_collection).Aggregate().Match(x => x._id == item._id).FirstOrDefault();
        }

        public void Dispose()
        {
            settings = null;
            client = null;
            database = null;
        }
    }
}
