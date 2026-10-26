using Bartergora2._0.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using NewsAggregator.models.returnModel;
using System.ServiceModel.Syndication;
using TMDInvestments.Context;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Commerce;

namespace TMD.Services.Service
{
    public sealed class BartergoraDataLoadService
    {
        BartergoraDBContext? ctx2 = null;
        MLPricePredict? ml = null;
        StackCommerceService? stackCommerceService = null;
        RakutenService? rakutenService = null;
        CommissionJunctionService? commissionJunctionService = null;

        ImpactService? impactService = null;
        IHttpClientFactory? _httpClientFactory;
        ILogger? _logger;
        Bartergora2._0.Models.ProductsToSubCategories productsToSubCategories = null;
        Bartergora2._0.Models.Advertiser advertiser = null;
        Bartergora2._0.Models.Product products = null;
        Bartergora2._0.Models.ProductImage productImage = null;
        private readonly object _lockObject = new object();
        public BartergoraDataLoadService(ILogger? logger, IHttpClientFactory httpClientFactory)
        {
            ctx2 = new BartergoraDBContext();
            ml = new MLPricePredict();
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            stackCommerceService = new StackCommerceService(_logger, _httpClientFactory);
            rakutenService = new RakutenService(_logger, _httpClientFactory, false);
            commissionJunctionService = new CommissionJunctionService(_logger, _httpClientFactory, false);
            impactService = new ImpactService(_logger, _httpClientFactory.CreateClient(), false);
        }
        public void LoadData()
        {            
            // Implement data loading logic here
            IEnumerable<CategoriesWithDescription>? categories = null;

            ctx2?.Database.ExecuteSqlRaw("EXEC [TMD].[TruncateAllTables]");
            
            categories = ctx2?.Database?.SqlQueryRaw<CategoriesWithDescription>("EXEC [tmd].[GetCategoriesWithDescription]");

            List<ProductInput> categoryResults = new List<ProductInput>();
            foreach (var item in categories)
            {
                categoryResults.Add(new ProductInput { CategoryId = item.CategoryId, CategoryName = item.CategoryName, CategoryDescription = item.CategoryDescription, SubCategoryId = item.SubCategoryId, SubCategoryName = item.SubCategoryName, SubCategoryDefinition = item.SubCategoryDefinition, SubCategoryOther = item.SubCategoryOther });
            }
            var predEngine = ml.InitTextCategoryPredict5(categoryResults);

            Task task1 = new Task(() => LoadCJData(predEngine, categoryResults));
            Task task2 = new Task(() => LoadStackCommerceData(predEngine, categoryResults));
            Task task3 = new Task(() => LoadRakutenData(predEngine, categoryResults));
            Task task4 = new Task(() => LoadImactRadiusData(predEngine, categoryResults));
            task1.Start();
            task2.Start();
            task3.Start();
            task4.Start();
            Task.WaitAll(task1, task2, task3, task4);
        }

        //private void LoadCJData(PredictionEngine<ModelInput, ModelOutput> predEngine, List<ModelInput> categoryResults)
        private void LoadCJData(PredictionEngine<ProductInput, ProductPrediction> predEngine, List<ProductInput> categoryResults)
        {
            var response = commissionJunctionService.GetProductsGraphQL();

            ml.predictCategory(predEngine, categoryResults, ref response);

            foreach (var item in response.products.ResultList)
            {
                //if(string.IsNullOrEmpty(item.SubCategoryName))
                //    continue;
                if (!Uri.TryCreate(item.linkCode.imageUrl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;
                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == item.advertiserName.ToLower().Trim() /*&& x.AdvertiserKey == Convert.ToInt64(item.advertiserId)*/).FirstOrDefault();
                if (advertiser == null)
                {
                    advertiser = new Advertiser() { AdvertiserName = item.advertiserName, AdvertiserKey = Convert.ToInt64(item.advertiserId), DateCreated = DateTime.Now, Deleted = false, IsActive = true };
                    lock (_lockObject)
                    {
                        ctx2.Advertisers.Add(advertiser);
                        ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
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
                    lock (_lockObject)
                    {
                        ctx2.Products.Add(products);
                        ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == item.linkCode.imageUrl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = item.linkCode.imageUrl, DateCreated = DateTime.Now, Deleted = false };
                    lock (_lockObject)
                    {
                        ctx2.ProductImages.Add(productImage);
                        ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                productsToSubCategories = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == item.SubCategoryId && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategories == null)
                {
                    productsToSubCategories = new ProductsToSubCategories() { SubCategoryId = item.SubCategoryId, ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    lock (_lockObject)
                    {
                        ctx2.ProductsToSubCategories.Add(productsToSubCategories);
                        ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
            }
        }

        private void LoadRakutenData(PredictionEngine<ProductInput, ProductPrediction> predEngine, List<ProductInput> categoryResults)
        {
            dynamic error = null;
            var merchants = RakutenService.LoadMerchants(_logger);
            RakutenModel products = null;
            List<RakutenModel> productsList = new List<RakutenModel>();
 
            int pageCount = 1;
            foreach (var merchant in merchants.Items)
            {
                products = rakutenService.GetRakutenProducts(1,/*token,*/ merchant, ref error);
                if (products == null || products.items == null || products.items.Count == 0)
                {
                    continue;
                }
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
                        //products.items = products.items.Where(x => x.SubCategoryName != null).ToList();
                        productsList.Add(products);
                        System.Threading.Thread.Sleep(250);
                    }
                }
                System.Threading.Thread.Sleep(250);
            }
            System.Threading.Thread.Sleep(250);
        }


        private void LoadStackCommerceData(PredictionEngine<ProductInput, ProductPrediction> predEngine, List<ProductInput> categoryResults)
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
                    lock (_lockObject)
                    {
                        ctx2.Advertisers.Add(advertiser);
                        ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
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
                    lock (_lockObject)
                    {
                        ctx2.Products.Add(products);
                        ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == imageUrl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = imageUrl, DateCreated = DateTime.Now, Deleted = false };
                    lock (_lockObject)
                    {
                        ctx2.ProductImages.Add(productImage);
                        ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                var label = (item?.Categories[0]?.Label ?? "0").Trim();
                productsToSubCategory = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == Convert.ToInt32(label) && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategory == null)
                {
                    productsToSubCategory = new ProductsToSubCategories() { SubCategoryId = Convert.ToInt32(label), ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    lock (_lockObject)
                    {
                        ctx2.ProductsToSubCategories.Add(productsToSubCategory);
                        ctx2.Entry(productsToSubCategory).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(productsToSubCategory).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
            }
        }
        private void LoadImactRadiusData(PredictionEngine<ProductInput, ProductPrediction> predEngine, List<ProductInput> categoryResults)
        {
            var response = impactService.GetProducts();
            IEnumerable<CatalogItems> catalogItems = response?.ads;
            ml.predictCategory(predEngine, categoryResults, ref catalogItems);
            foreach (var item in catalogItems)
            {
                if (!Uri.TryCreate(item.ImageUrl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;
                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == item.Manufacturer.ToLower().Trim() /*&& x.AdvertiserKey == Convert.ToInt32(item.CatalogItemId)*/).FirstOrDefault();
                if (advertiser == null)
                {
                    lock (_lockObject)
                    {
                        advertiser = new Advertiser() { AdvertiserName = item.Manufacturer, AdvertiserKey = Convert.ToInt32(item.CatalogItemId), DateCreated = DateTime.Now, Deleted = false, IsActive = true };
                        ctx2.Advertisers.Add(advertiser);
                        ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                products = ctx2.Products.Where(x => x.AdvertiserId == advertiser.AdvertiserId && x.ProductName == item.Name && x.Description == item.Description).FirstOrDefault();
                if (products == null)
                {
                    products = new Bartergora2._0.Models.Product()
                    {
                        Source = "Impact",
                        AdvertiserId = advertiser.AdvertiserId,
                        ProductName = item.Name,
                        Description = item.Description,
                        Price = Convert.ToDecimal(item.OriginalPrice),
                        SalePrice = Convert.ToDecimal(item.CurrentPrice),
                        SKU = item.Mpn,
                        UPC = item.Gtin,
                        linkURL = item.Url,
                        DateCreated = DateTime.Now,
                        Deleted = false
                    };
                    lock (_lockObject)
                    {
                        ctx2.Products.Add(products);
                        ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == item.ImageUrl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = item.ImageUrl, DateCreated = DateTime.Now, Deleted = false };
                    lock (_lockObject)
                    {
                        ctx2.ProductImages.Add(productImage);
                        ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                productsToSubCategories = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == item.SubCategoryId && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategories == null)
                {
                    productsToSubCategories = new ProductsToSubCategories() { SubCategoryId = item.SubCategoryId, ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    lock (_lockObject)
                    {
                        ctx2.ProductsToSubCategories.Add(productsToSubCategories);
                        ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }

            }
        }
        //private void LoadRakutenCategories(PredictionEngine<ModelInput, ModelOutput> predEngine, List<ModelInput> categoryResults, IEnumerable<RakutenModelItems> items)
        private void LoadRakutenCategories(PredictionEngine<ProductInput, ProductPrediction> predEngine, List<ProductInput> categoryResults, IEnumerable<RakutenModelItems> items)
        {
            ml.predictCategory(predEngine, categoryResults, ref items);

            foreach (var item in items)
            {
                if (!Uri.TryCreate(item.imageurl, UriKind.Absolute, out Uri uriResult)
                    || !(uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                    continue;
                advertiser = ctx2.Advertisers.Where(x => x.AdvertiserName.ToLower().Trim() == item.merchantname.ToLower().Trim() /*&& x.AdvertiserKey == Convert.ToInt32(item.mid)*/).FirstOrDefault();
                if (advertiser == null)
                {
                    advertiser = new Advertiser() { AdvertiserName = item.merchantname, AdvertiserKey = Convert.ToInt32(item.mid), DateCreated = DateTime.Now, Deleted = false, IsActive = true };
                    lock (_lockObject)
                    {
                        ctx2.Advertisers.Add(advertiser);
                        ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(advertiser).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
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
                    lock (_lockObject)
                    {
                        ctx2.Products.Add(products);
                        ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(products).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                productImage = ctx2.ProductImages.Where(x => x.ProductId == products.ProductId && x.ImageUrl == item.imageurl).FirstOrDefault();
                if (productImage == null)
                {
                    productImage = new ProductImage() { ProductId = products.ProductId, ImageUrl = item.imageurl, DateCreated = DateTime.Now, Deleted = false };
                    lock (_lockObject)
                    {
                        ctx2.ProductImages.Add(productImage);
                        ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(productImage).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }
                productsToSubCategories = ctx2.ProductsToSubCategories.Where(x => x.SubCategoryId == item.SubCategoryId && x.ProductId == products.ProductId).FirstOrDefault();
                if (productsToSubCategories == null)
                {
                    productsToSubCategories = new ProductsToSubCategories() { SubCategoryId = item.SubCategoryId, ProductId = products.ProductId, DateCreated = DateTime.Now, Deleted = false };
                    lock (_lockObject)
                    {
                        ctx2.ProductsToSubCategories.Add(productsToSubCategories);
                        ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Added;
                        ctx2.SaveChanges();
                        ctx2.Entry(productsToSubCategories).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                }

            }
        }

    }
}
