using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMD.Database.Connect
{
    public abstract class ClientBase
    {
        private IConfiguration Mondodb;
        private string _connection;
        public string _collection;
        private MongoClientSettings settings;
        private MongoClient client;
        public IMongoDatabase database;
        public ClientBase()
        {
            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                Mondodb = configuration.GetSection("Mondodb");
                if (Mondodb != null)
                {
                    _connection = Mondodb.GetSection("connection").Value;
                    settings = MongoClientSettings.FromConnectionString(_connection);
                    settings.ServerApi = new ServerApi(ServerApiVersion.V1);
                    client = new MongoClient(settings);
                    database = client.GetDatabase("TriMarkDirect");
                    //collection = database.GetCollection<RssModelItems>("TMD");
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        public void Dispose()
        {
            client = null;
            database = null;
            settings = null;
        }
    }
}
