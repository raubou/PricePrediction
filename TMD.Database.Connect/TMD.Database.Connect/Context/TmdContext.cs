using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.IO;
//using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using TMDInvestments.Models;


namespace TMDInvestments.Context
{
    public class TmdContext : DbContext
    {
        public TmdContext()
        {
            
        }
        public DbSet<RulesEngine>? RulesEngine { get; set; }
        //public DbSet<StockTracker>? StockTracker { get; set; }
        public DbSet<StockTracker2>? StockTracker2 { get; set; }
        public DbSet<StockList>? StockList { get; set; }
        public DbSet<Earning>? Earning { get; set; }
        public DbSet<IPO>? IPO { get; set; }
        public DbSet<Token>? Token { get; set; }
        public DbSet<UserRoles>? UserRoles { get; set; }
        public DbSet<Users>? Users { get; set; }
        public DbSet<CryptoList> CryptoList { get; set; }
        //public DbSet<CryptoTracker> CryptoTracker { get; set; }
        public DbSet<CryptoTracker2> CryptoTracker2 { get; set; }
        public DbSet<Balance> Balance { get; set; }
        public DbSet<Exceptions> Exceptions { get; set; }

        public DbSet<KalshiMarketPosition> KalshiMarketPositions { get; set; }

        //protected void OnModelCreatingPartial(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<ProjectSearch>().HasNoKey();
        //}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CryptoTracker2>().Property(x => x.LastPrice).HasPrecision(18, 5);
            modelBuilder.Entity<CryptoTracker2>().Property(x => x.PricePurchased).HasPrecision(18, 5);
            modelBuilder.Entity<CryptoTracker2>().Property(x => x.PriceSold).HasPrecision(18, 5);
            modelBuilder.Entity<CryptoTracker2>().Property(x => x.CurrentBalance).HasPrecision(18, 5);
            modelBuilder.Entity<CryptoTracker2>().Property(x => x.Amount).HasPrecision(18, 5);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connection = string.Empty;
            try
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                var connectionString = configuration.GetSection("ConnectionStrings");
                connection = connectionString.GetSection("connection").Value;
            }
            catch (System.Exception ex)
            {
                connection = Environment.GetEnvironmentVariable("SqlConnectionString");
            }

            optionsBuilder.UseSqlServer(connection
                ,
                options =>
                {
                    options.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: System.TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null); options.CommandTimeout(600);
                }

                )
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }
    }
}
