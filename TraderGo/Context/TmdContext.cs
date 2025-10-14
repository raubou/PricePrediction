using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using TMDInvestments.Models;

namespace TraderGo.Context
{
    public class TmdContext : DbContext
    {
        public TmdContext()
        {
            
        }
        public DbSet<RulesEngine>? RulesEngine { get; set; }
        public DbSet<StockTracker>? StockTracker { get; set; }
        public DbSet<Token>? Token { get; set; }
        public DbSet<UserRoles>? UserRoles { get; set; }
        public DbSet<Users>? Users { get; set; }

        //protected void OnModelCreatingPartial(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<ProjectSearch>().HasNoKey();
        //}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = configuration.GetSection("ConnectionStrings");
            optionsBuilder.UseSqlServer(connectionString.GetSection("connection").Value);
        }
    }
}
