using Bartergora2._0.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Commerce;

namespace TMDInvestments.Context
{
    public sealed class BartergoraDBContext : DbContext
    {

        public DbSet<Bartergora2._0.Models.Category> Categories { get; set; }
        public DbSet<Advertiser> Advertisers { get; set; }
        public DbSet<Manufacture> Manufactures { get; set; }
        public DbSet<CustomField> CustomFields { get; set; }
        public DbSet<Bartergora2._0.Models.Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<SubCategory> SubCategories { get; set; }
        public DbSet<SubCategoriesToCategories> SubCategoriesToCategories { get; set; }
        public DbSet<ProductsToSubCategories> ProductsToSubCategories { get; set; }
        public DbSet<BarergoraSearch> BarergoraSearch { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfiguration config = builder.Build();
            optionsBuilder.UseSqlServer(config["ConnectionStrings2:connection"], sqlServerOptions => {sqlServerOptions.CommandTimeout(600);
                sqlServerOptions.EnableRetryOnFailure(
                       maxRetryCount: 5,
                       maxRetryDelay: System.TimeSpan.FromSeconds(30),
                       errorNumbersToAdd: null);
            });
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<BarergoraSearch>().HasNoKey();
            modelBuilder.Entity<CategoriesWithDescription>().HasNoKey();
        }
    }
}
