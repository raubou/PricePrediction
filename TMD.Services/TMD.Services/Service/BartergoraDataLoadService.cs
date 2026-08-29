using Azure;
using Bartergora2._0.Models;
using HtmlAgilityPack;
using log4net.Repository.Hierarchy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using NewsAggregator.models.returnModel;
using Org.BouncyCastle.Crypto.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using TMDInvestment.Models;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Commerce;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TMD.Services.Service
{
    public sealed class BartergoraDataLoadService
    {
        BartergoraDBContext? ctx2 = null;
        MLPricePredict? ml = null;
        StackCommerceService? stackCommerceService = null;
        RakutenService? rakutenService = null;
        CommissionJunctionService? commissionJunctionService = null;
        IHttpClientFactory? _httpClientFactory;
        ILogger? _logger;
        Bartergora2._0.Models.ProductsToSubCategories productsToSubCategories = null;
        Bartergora2._0.Models.Advertiser advertiser = null;
        Bartergora2._0.Models.Product products = null;
        Bartergora2._0.Models.ProductImage productImage = null;
        public BartergoraDataLoadService(ILogger? logger, IHttpClientFactory httpClientFactory)
        {
            ctx2 = new BartergoraDBContext();
            ml = new MLPricePredict();
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            stackCommerceService = new StackCommerceService(_logger, _httpClientFactory);
            rakutenService = new RakutenService(_logger, _httpClientFactory, false);
            commissionJunctionService = new CommissionJunctionService(_logger, _httpClientFactory, false);
        }
        public void LoadData()
        {
            // Implement data loading logic here
            IEnumerable<CategoriesWithDescription> categories = null;

            ctx2.Database.ExecuteSqlRaw("EXEC [TMD].[TruncateAllTables]");
            
            categories = ctx2.Database.SqlQueryRaw<CategoriesWithDescription>("EXEC [tmd].[GetCategoriesWithDescription]");
            List<ModelInput> categoryResults = new List<ModelInput>();
            foreach (var item in categories)
            {
                categoryResults.Add(new ModelInput { SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName, KeyWords = item.KeyWords });
            }
            var predEngine = ml.InitTextCategoryPredict(categoryResults);

            LoadCJData(predEngine, categoryResults);
            //LoadStackCommerceData(predEngine, categoryResults);
            LoadRakutenData(predEngine, categoryResults);
        }

        private void LoadCJData(PredictionEngine<ModelInput, ModelOutput> predEngine, List<ModelInput> categoryResults)
        {
            var response = commissionJunctionService.GetProductsGraphQL();

            ml.predictCategory(predEngine, categoryResults, ref response);

            foreach (var item in response.products.ResultList)
            {
                if(string.IsNullOrEmpty(item.SubCategoryName))
                    continue;
                if (!Uri.TryCreate(item.linkCode.imageUrl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;
                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == item.advertiserName.ToLower().Trim() && x.AdvertiserKey == Convert.ToInt32(item.advertiserId)).FirstOrDefault();
                if (advertiser == null)
                {
                    advertiser = new Advertiser() { AdvertiserName = item.advertiserName, AdvertiserKey = Convert.ToInt32(item.advertiserId), DateCreated = DateTime.Now, Deleted = false, IsActive = true };
                    ctx2.Advertisers.Add(advertiser);
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                products = ctx2.Products.Where(x => x.AdvertiserId == advertiser.AdvertiserId && x.ProductName == item.title && x.Description == item.description).FirstOrDefault();
                if (products == null)
                {
                    products = new Bartergora2._0.Models.Product()
                    {
                        Source = "Commission Junction",
                        AdvertiserId = advertiser.AdvertiserId,
                        ProductName = item.title,
                        Description = item.description,
                        Price = Convert.ToDecimal(item?.price?.amount ?? "0"),
                        SalePrice = Convert.ToDecimal(item?.salePrice?.amount ?? "0"),
                        //SKU = item.sku,
                        //UPC = item.upccode,
                        linkURL = item.linkCode.clickUrl,
                        DateCreated = DateTime.Now,
                        Deleted = false
                    };
                    ctx2.Products.Add(products);
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == item.linkCode.imageUrl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = item.linkCode.imageUrl, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductImages.Add(productImage);
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productsToSubCategories = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == item.SubCategoryId && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategories == null)
                {
                    productsToSubCategories = new ProductsToSubCategories() { SubCategoryId = item.SubCategoryId, ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductsToSubCategories.Add(productsToSubCategories);
                    ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
        }
        private void LoadRakutenData(PredictionEngine<ModelInput, ModelOutput> predEngine, List<ModelInput> categoryResults)
        {
            dynamic error = null;
            var merchants = RakutenService.LoadMerchants(_logger);
            RakutenModel products = null;
            List<RakutenModel> productsList = new List<RakutenModel>();
 
            int pageCount = 1;
            foreach (var merchant in merchants.Items)
            {
                products = rakutenService.GetRakutenProducts(1,/*token,*/ merchant, ref error);
                var totalMatches = Convert.ToInt32(products.TotalMatches);
                var totalPages = /*(Convert.ToInt32(products.TotalPages) > maxPages) ? maxPages :*/ Convert.ToInt32(products.TotalPages);
                if (totalPages > 0)
                {
                    LoadRakutenCategories(predEngine, categoryResults, products.items);
                    productsList.Add(products);
                    for (int i = 2; i < totalPages; i++)
                    {
                        products = rakutenService.GetRakutenProducts(i, /*token,*/ merchant, ref error);
                        if (products == null || products.items == null || products.items.Count == 0)
                        {
                            continue;
                        }
                        LoadRakutenCategories(predEngine, categoryResults, products.items);
                        products.items = products.items.Where(x => x.SubCategoryName != null).ToList();
                        productsList.Add(products);
                        System.Threading.Thread.Sleep(250);
                    }
                }
                System.Threading.Thread.Sleep(250);
            }
        }

        private void LoadStackCommerceData(PredictionEngine<ModelInput, ModelOutput> predEngine, List<ModelInput> categoryResults)
        {
            Bartergora2._0.Models.ProductsToSubCategories productsToSubCategory = null;
            Bartergora2._0.Models.Advertiser advertiser = null;
            Bartergora2._0.Models.Product products = null;
            Bartergora2._0.Models.ProductImage productImage = null;
            decimal price = 0m;
            string imageUrl = string.Empty;

            SyndicationFeed feed = stackCommerceService.GetFileFeed();

            ml.predictCategory(predEngine, categoryResults, ref feed);

            foreach (SyndicationItem item in feed.Items)
            {
                Console.WriteLine($"Title: {item.Title.Text}");
                Console.WriteLine($"Summary: {item.Summary?.Text}");

                bool abstractDigit = false;
                string digitsOnly = "";
                foreach (char c in item.Title.Text)
                {
                    if (abstractDigit == true && char.IsDigit(c))
                    {
                        digitsOnly += c;
                    }
                    else
                    {
                        abstractDigit = false;
                    }
                    if (c == '$')
                    {
                        abstractDigit = true;
                        digitsOnly = "";
                    }
                }
                price = Convert.ToDecimal(digitsOnly);
                //}
                //catch (Exception ex)
                //{
                //    continue;
                //}

                var doc = new HtmlDocument();
                doc.LoadHtml(item.Summary?.Text); // or item.Content
                                                  // Find the first <img> tag
                var imgNode = doc.DocumentNode.SelectSingleNode("//img");
                if (imgNode != null)
                {
                    imageUrl = imgNode.GetAttributeValue("src", "");
                    // Process the imageUrl (e.g., download, display)
                }
                Console.WriteLine($"Link: {item.Links[0].Uri}");
                Console.WriteLine("----------------------------");

                if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;

                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == "Stack Commerce" && x.AdvertiserKey == 999).FirstOrDefault();
                if (advertiser == null)
                {
                    advertiser = new Advertiser() { AdvertiserName = "Stack Commerce", AdvertiserKey = 999, DateCreated = DateTime.Now, Deleted = false, IsActive = true };
                    ctx2.Advertisers.Add(advertiser);
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                products = ctx2.Products.Where(x => x.Source == "Stack Commerce" && x.ProductName == item.Title.Text && x.Description == item.Summary.Text).FirstOrDefault();
                if (products == null)
                {
                    products = new Bartergora2._0.Models.Product()
                    {
                        Source = "Stack Commerce",
                        AdvertiserId = advertiser.AdvertiserId,
                        ProductName = item.Title.Text,
                        Description = item.Summary?.Text,
                        Price = price,
                        SalePrice = 0,
                        SKU = "0",
                        UPC = "0",
                        linkURL = item.Links[0].Uri.ToString(),
                        DateCreated = DateTime.Now,
                        Deleted = false
                    };
                    ctx2.Products.Add(products);
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == imageUrl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = imageUrl, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductImages.Add(productImage);
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                var label = (item.Categories[0].Label ?? "0").Trim();
                productsToSubCategory = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == Convert.ToInt32(label) && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategory == null)
                {
                    productsToSubCategory = new ProductsToSubCategories() { SubCategoryId = Convert.ToInt32(label), ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductsToSubCategories.Add(productsToSubCategory);
                    ctx2.Entry(productsToSubCategory).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productsToSubCategory).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
            }
        }

        private void LoadRakutenCategories(PredictionEngine<ModelInput, ModelOutput> predEngine, List<ModelInput> categoryResults, IEnumerable<RakutenModelItems> items)
        {
            ml.predictCategory(predEngine, categoryResults, ref items);

            foreach (var item in items)
            {
                if (!Uri.TryCreate(item.imageurl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;
                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == item.merchantname.ToLower().Trim() && x.AdvertiserKey == Convert.ToInt32(item.mid)).FirstOrDefault();
                if (advertiser == null)
                {
                    advertiser = new Advertiser() { AdvertiserName = item.merchantname, AdvertiserKey = Convert.ToInt32(item.mid), DateCreated = DateTime.Now, Deleted = false, IsActive = true };
                    ctx2.Advertisers.Add(advertiser);
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                products = ctx2.Products.Where(x => x.AdvertiserId == advertiser.AdvertiserId && x.ProductName == item.productname && x.Description == item.description[0].secondary).FirstOrDefault();
                if (products == null)
                {
                    products = new Bartergora2._0.Models.Product()
                    {
                        Source = "Rakuten",
                        AdvertiserId = advertiser.AdvertiserId,
                        ProductName = item.productname,
                        Description = item.description[0]._short,
                        Price = Convert.ToDecimal(item.price),
                        SalePrice = Convert.ToDecimal(item.saleprice),
                        SKU = item.sku,
                        UPC = item.upccode,
                        linkURL = item.linkurl,
                        DateCreated = DateTime.Now,
                        Deleted = false
                    };
                    ctx2.Products.Add(products);
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == item.imageurl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = item.imageurl, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductImages.Add(productImage);
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }
                productsToSubCategories = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == item.SubCategoryId && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategories == null)
                {
                    productsToSubCategories = new ProductsToSubCategories() { SubCategoryId = item.SubCategoryId, ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    ctx2.ProductsToSubCategories.Add(productsToSubCategories);
                    ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                    ctx2.SaveChanges();
                    ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                }

            }
        }

    }
}
