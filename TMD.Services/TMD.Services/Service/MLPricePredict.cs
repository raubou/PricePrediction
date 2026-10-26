using Microsoft.ML;
using Microsoft.ML.TorchSharp;
using Microsoft.ML.TorchSharp.NasBert;
using Microsoft.ML.Transforms.Text;


//using Microsoft.ML.TorchSharp;
using Microsoft.ML.Transforms.TimeSeries;
using NewsAggregator.models.returnModel;
using Org.BouncyCastle.Crypto.Engines;
using System.ServiceModel.Syndication;
using System.Text.Json.Nodes;
using TMDInvestment.Models;
using TMDInvestments.Models;
using TMDInvestments.Models.Models.Polygon;

namespace TMD.Services.Service
{
    public sealed class MLPricePredict
    {
        private int horizon = 3;
        private int horizonCnt = 1;
        public MLPricePredict()
        {
        }
        public void Calculate(List<CandleItems> inputs, ref JsonObject results)
        {
            try
            {
                var mlContext = new MLContext();
                IDataView trainDataVeiw = mlContext.Data.LoadFromEnumerable<CandleItems>(inputs);

                var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: "ForecastedPriceDifference",
                    inputColumnName: "close",
                    windowSize: /*(inputs.Count <= 14) ? (int)Math.Round((decimal)inputs.Count / 2, 0) :*/ horizon,
                    seriesLength: 30,//21,
                    trainSize: /*365*/inputs.Count,
                    horizon: horizon,
                    confidenceLevel: 0.96f,
                    confidenceLowerBoundColumn: "LowerBoundPriceDifference",
                    confidenceUpperBoundColumn: "UpperBoundPriceDifference");

                var forecaster = forecastingPipeline.Fit(trainDataVeiw);
                var forecastEngine = forecaster.CreateTimeSeriesEngine<CandleItems, MLModelResults>(mlContext);
                var prediction = forecastEngine.Predict();
                horizonCnt = 1;
                foreach (var item in prediction.UpperBoundPriceDifference)
                {
                    results.Add($"Upper Bound Price Difference {horizonCnt}", item);
                    horizonCnt++;
                }
                horizonCnt = 1;
                foreach (var item in prediction.LowerBoundPriceDifference)
                {
                    results.Add($"Lower Bound Price Difference {horizonCnt}", item);
                    horizonCnt++;
                }
                horizonCnt = 1;
                foreach (var item in prediction.ForecastedPriceDifference)
                {
                    results.Add($"Forecast Bound Price Difference {horizonCnt}", item);
                    horizonCnt++;
                }
                results.Add($"Upper Bound Price Difference Average", prediction.UpperBoundPriceDifference.Average(x => x));
                results.Add($"Lower Bound Price Difference Average", prediction.LowerBoundPriceDifference.Average(x => x));
                results.Add($"Forecast Bound Price Difference Average", prediction.ForecastedPriceDifference.Average(x => x));
            }
            catch (Exception)
            {
                results.Add($"Upper Bound Price Difference Average", 0);

                results.Add($"Lower Bound Price Difference Average", 0);

                results.Add($"Forecast Bound Price Difference Average", 0);
            }

        }
        public void Calculate(List<Coinbase.AdvancedTrade.Models.Public.PublicCandle> inputs, ref JsonObject results)
        {
            try
            {
                var mlContext = new MLContext();
                IDataView trainDataVeiw = mlContext.Data.LoadFromEnumerable<Coinbase.AdvancedTrade.Models.Public.PublicCandle>(inputs);

                var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: "ForecastedPriceDifference",
                    inputColumnName: "Close",
                    windowSize: /*(inputs.Count <= 14) ? (int)Math.Round((decimal)inputs.Count / 2, 0) :*/ horizon,
                    seriesLength: 30,//21,
                    trainSize: /*365*/inputs.Count,
                    horizon: horizon,
                    confidenceLevel: 0.96f,
                    confidenceLowerBoundColumn: "LowerBoundPriceDifference",
                    confidenceUpperBoundColumn: "UpperBoundPriceDifference");

                var forecaster = forecastingPipeline.Fit(trainDataVeiw);
                var forecastEngine = forecaster.CreateTimeSeriesEngine<Coinbase.AdvancedTrade.Models.Public.PublicCandle, MLModelResults>(mlContext);
                var prediction = forecastEngine.Predict();
                horizonCnt = 1;
                foreach (var item in prediction.UpperBoundPriceDifference)
                {
                    results.Add($"Upper Bound Price Difference {horizonCnt}", item);
                    horizonCnt++;
                }
                horizonCnt = 1;
                foreach (var item in prediction.LowerBoundPriceDifference)
                {
                    results.Add($"Lower Bound Price Difference {horizonCnt}", item);
                    horizonCnt++;
                }
                horizonCnt = 1;
                foreach (var item in prediction.ForecastedPriceDifference)
                {
                    results.Add($"Forecast Bound Price Difference {horizonCnt}", item);
                    horizonCnt++;
                }
                results.Add($"Upper Bound Price Difference Average", prediction.UpperBoundPriceDifference.Average(x => x));
                results.Add($"Lower Bound Price Difference Average", prediction.LowerBoundPriceDifference.Average(x => x));
                results.Add($"Forecast Bound Price Difference Average", prediction.ForecastedPriceDifference.Average(x => x));
            }
            catch (Exception)
            {
                results.Add($"Upper Bound Price Difference Average", 0);

                results.Add($"Lower Bound Price Difference Average", 0);

                results.Add($"Forecast Bound Price Difference Average", 0);
            }
        }
        /// <summary>
        /// Main Category prediction function. It trains a model using the provided categories and then creates a prediction engine that can be used
        /// to predict the category of new items based on their text (e.g., Category or description). The model is trained using a text featurization
        /// step followed by a multiclass classification trainer. The predicted category is then mapped back to its original string value for easier
        /// interpretation.
        /// </summary>
        /// <param name="categories"></param>
        /// <returns></returns>
        //public PredictionEngine<ModelInput, ModelOutput> InitTextCategoryPredict(IEnumerable<ModelInput> categories)
        //{
        //    // Initialize MLContext
        //    var _mlContext = new MLContext(seed: 0)
        //    {
        //        FallbackToCpu = true,
        //        GpuDeviceId = 0
        //    }
        //        ;

        //    // 1. Load data
        //    var existingCategoryData = _mlContext.Data.LoadFromEnumerable(categories/*, schema*/);

        //    // 2. Build pipeline

        //    // var pipeline = /*_mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "PredictedLabel", inputColumnName: "Title");*/
        //    ///*//.Append(*/_mlContext.Transforms.Text.FeaturizeText(inputColumnName: "Title", outputColumnName: "PredictedLabel");/*)*/
        //    //.Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
        //    //.Append(_mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: "PredictedLabel"));

        //    //var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ModelInput.SubCategoryName))
        //    //    .Append(_mlContext.MulticlassClassification.Trainers.TextClassification(
        //    //        labelColumnName: "Label",
        //    //        sentence1ColumnName: nameof(ModelOutput.SubCategoryName)))
        //    //    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        //    // 1. Define the data-processing pipeline
        //    //var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ModelInput.KeyWords))
        //    //    .Append(_mlContext.Transforms.Text.FeaturizeText("Features", nameof(ModelInput.KeyWords)))
        //    var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ModelInput.SubCategoryName))
        //        .Append(_mlContext.Transforms.Text.FeaturizeText("Features", nameof(ModelInput.SubCategoryName)))
        //        //.Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
        //        .Append(_mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy("Label", "Features"))
        //        //.Append(_mlContext.MulticlassClassification.Trainers.LightGbm("Label", "Features")) //maybe
        //        //.Append(_mlContext.MulticlassClassification.Trainers.NaiveBayes("Label", "Features"))
        //        .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        //    var model = pipeline.Fit(existingCategoryData);

        //    // Create a prediction engine for making predictions
        //    var predEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
        //    return predEngine;
        //}
        //public void predictCategory(PredictionEngine<ModelInput, ModelOutput> predEngine, IEnumerable<ModelInput> categories, ref CJModel results)
        public void predictCategory(PredictionEngine<ProductInput, ProductPrediction> predEngine, IEnumerable<ProductInput> categories, ref CJModel results)
        {
            // Test the trained model with new data
            //string word = string.Empty;
            foreach (var item in results.products.ResultList)
            {
                //var titles = item?.title?.Split(' ');
                //var wordArray = item?.itemListName?.Split('>');
                //if (wordArray?.Length >= 2)
                //    word = wordArray[1].Trim();
                //else
                //    word = item.title;
                ProductPrediction? result = null;
                //foreach (var title in titles)
                //{
                //    //var input = new ModelInput { KeyWords = item.title };
                //    var input = new ProductInput { Category = title, Description = word };
                //    result = predEngine.Predict(input);

                //    Console.WriteLine($"Text: {input.Category}");
                //    Console.WriteLine($"Prediction: {result.PredictedCategory}");
                //}
                var input = new ProductInput { SubCategoryName = item?.title /*+ " " + item?.description/*, Description = word */};

                result = predEngine.Predict(input);
                var category = categories.Where(x => x.SubCategoryName == result.PredictedLabel).FirstOrDefault();
                if (category != null)
                {
                    item.SubCategoryId = category.SubCategoryId;
                    item.SubCategoryName = category.SubCategoryName;
                }
                //}
            }
        }
        public void predictCategory(PredictionEngine<ProductInput, ProductPrediction> predEngine, IEnumerable<ProductInput> categories, ref IEnumerable<RakutenModelItems> results)
        {
            // Test the trained model with new data
            foreach (var item in results)
            {
                //var wordArray = item.productname.Split(' ');
                //foreach (var word in wordArray)
                //{
                var input = new ProductInput { SubCategoryName = item?.productname /*+ " " + item?.description[0]._short/*, Description = item.description[0]._short */ };
                //var input = new ProductInput { KeyWords = item.productname/*, Description = item.description[0]._short */ };
                //var input = new ProductInput { SubCategoryName = word/*, Description = item.description[0]._short */ };
                var result = predEngine.Predict(input);

                Console.WriteLine($"Text: {input.SubCategoryName}");
                Console.WriteLine($"Prediction: {result.PredictedLabel}");
                //var category = categories.Where(x => x.KeyWords == result.PredictedLabel).FirstOrDefault();
                var category = categories.Where(x => x.SubCategoryName == result.PredictedLabel).FirstOrDefault();
                //var category = categories.Where(x => (x.SubCategoryName == item?.categories[0].secondary || x.SubCategoryName.Contains(item?.categories[0].secondary))).FirstOrDefault();
                if (category != null)
                {
                    item.SubCategoryId = category.SubCategoryId;
                    item.SubCategoryName = category.SubCategoryName;
                }
                //}   
            }
        }
        public void predictCategory(PredictionEngine<ProductInput, ProductPrediction> predEngine, IEnumerable<ProductInput> categories, ref SyndicationFeed results)
        {
            // Test the trained model with new data
            foreach (var item in results.Items)
            {
                var input = new ProductInput { SubCategoryName = item.Title.Text/*, Description = item.Summary.Text */ };
                var result = predEngine.Predict(input);

                Console.WriteLine($"Text: {input.SubCategoryName}");
                Console.WriteLine($"Prediction: {result.PredictedLabel}");
                var category = categories.Where(x => x.SubCategoryName == result.PredictedLabel).FirstOrDefault();
                if (category != null)
                {
                    item.Categories.Add(new SyndicationCategory(category.SubCategoryName, null, category.SubCategoryId.ToString()));
                }
            }
        }
        public void predictCategory(PredictionEngine<ProductInput, ProductPrediction> predEngine, IEnumerable<ProductInput> categories, ref IEnumerable<CatalogItems> results)
        {
            // Test the trained model with new data
            foreach (var item in results)
            {
                //var wordArray = item.productname.Split(' ');
                //foreach (var word in wordArray)
                //{
                var input = new ProductInput { SubCategoryName = item?.Name /*+ " " + item?.Description/*, Description = item.description[0]._short */ };
                //var input = new ProductInput { KeyWords = item.productname/*, Description = item.description[0]._short */ };
                //var input = new ProductInput { SubCategoryName = word/*, Description = item.description[0]._short */ };
                var result = predEngine.Predict(input);

                Console.WriteLine($"Text: {input.SubCategoryName}");
                Console.WriteLine($"Prediction: {result.PredictedLabel}");
                //var category = categories.Where(x => x.KeyWords == result.PredictedLabel).FirstOrDefault();
                var category = categories.Where(x => x.SubCategoryName == result.PredictedLabel).FirstOrDefault();
                //var category = categories.Where(x => (x.SubCategoryName == item?.Category || x.SubCategoryName.Contains(item?.Category))).FirstOrDefault();
                if (category != null)
                {
                    item.SubCategoryId = category.SubCategoryId;
                    item.SubCategoryName = category.SubCategoryName;
                }
                //}   
            }
        }
        public PredictionEngine<ProductInput, ProductPrediction> InitTextCategoryPredict2(IEnumerable<ProductInput> categories)
        {
            // 1. Initialize MLContext (the entry point for all ML.NET operations)
            var mlContext = new MLContext(seed: 1);

            // 2. Mock some sample product training data adhering to a taxonomy
            //    var trainingData = new List<ProductInput>
            //{
            //    new() { Category = "Wireless Mechanical Keyboard", Description = "RGB backlit 87-key typing keyboard", TaxonomyCategory = "Electronics > Computers > Peripherals" },
            //    new() { Category = "Gaming Laser Mouse", Description = "High DPI wired desktop precision pointer", TaxonomyCategory = "Electronics > Computers > Peripherals" },
            //    new() { Category = "Running Shoes Air Max", Description = "Lightweight breathable cross-training sneakers", TaxonomyCategory = "Apparel > Clothing > Shoes" },
            //    new() { Category = "Waterproof Winter Parka", Description = "Heavy insulated hooded coat for freezing weather", TaxonomyCategory = "Apparel > Clothing > Outerwear" }
            //};

            IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(categories);

            // 3. Define the Data Preparation & Feature Extraction Pipeline
            var pipeline =
                // Step A: Convert the text label (string taxonomy) into a numerical key ML models require
                mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ProductInput.SubCategoryName))

                // Step B: Featurize text strings into raw numeric vector weights (TF-IDF tokens)
                .Append(mlContext.Transforms.Text.FeaturizeText("CategoryFeatures", nameof(ProductInput.SubCategoryName)))
                .Append(mlContext.Transforms.Text.FeaturizeText("DescFeatures", nameof(ProductInput.SubCategoryName)))

                // Step C: Concatenate multiple numeric token vectors into a unified column named "Features"
                .Append(mlContext.Transforms.Concatenate("Features", "TitleFeatures", "DescFeatures"))

                // Step D: Append a Multiclass Classifier (SDCA Maximum Entropy is great for quick text tasks)
                .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))

                // Step E: Convert the numerical predicted key back to the original taxonomy string path
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // 4. Train the Model
            Console.WriteLine("Training taxonomy classification model...");
            var trainedModel = pipeline.Fit(trainingDataView);

            // 5. Create a Prediction Engine for isolated single-item executions
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ProductInput, ProductPrediction>(trainedModel);

            // 6. Test an unknown product variant
            var newProduct = new ProductInput
            {
                SubCategoryName = "Ergonomic Office Trackball",
                //Description = "Comfortable desktop thumb-controlled pointer"
            };

            var prediction = predictionEngine.Predict(newProduct);

            // 7. Output Results
            Console.WriteLine("\n--- Product Inference Results ---");
            Console.WriteLine($"Product Category: {newProduct.SubCategoryName}");
            Console.WriteLine($"Predicted Taxonomy Hierarchy: {prediction.PredictedLabel}");

            return predictionEngine;
        }
        public PredictionEngine<ProductInput, ProductPrediction> InitTextCategoryPredict3(IEnumerable<ProductInput> categories)
        {
            // 1. Initialize MLContext (the entry point for all ML.NET operations)
            var mlContext = new MLContext(seed: 1);

            // 2. Mock some sample product training data adhering to a taxonomy
            //    var trainingData = new List<ProductInput>
            //{
            //    new() { Category = "Wireless Mechanical Keyboard", Description = "RGB backlit 87-key typing keyboard", TaxonomyCategory = "Electronics > Computers > Peripherals" },
            //    new() { Category = "Gaming Laser Mouse", Description = "High DPI wired desktop precision pointer", TaxonomyCategory = "Electronics > Computers > Peripherals" },
            //    new() { Category = "Running Shoes Air Max", Description = "Lightweight breathable cross-training sneakers", TaxonomyCategory = "Apparel > Clothing > Shoes" },
            //    new() { Category = "Waterproof Winter Parka", Description = "Heavy insulated hooded coat for freezing weather", TaxonomyCategory = "Apparel > Clothing > Outerwear" }
            //};

            IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(categories);

            // 1. Convert taxonomy string categories to numeric ML keys
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ProductInput.SubCategoryName))

                // 2. Featurize Category -> Outpuing a known-size Vector of Single
                .Append(mlContext.Transforms.Text.FeaturizeText("CategoryFeatures", nameof(ProductInput.SubCategoryName)))

                //// 3. Featurize Description -> Outputing a known-size Vector of Single
                //.Append(mlContext.Transforms.Text.FeaturizeText("DescFeatures", nameof(ProductInput.Description)))

                // 4. Safely concatenate the structurally identical vector types into "Features"
                //.Append(mlContext.Transforms.Concatenate("Features", "CategoryFeatures", "DescFeatures"))
                .Append(mlContext.Transforms.Concatenate("Features", "CategoryFeatures"))

                // 5. Append your trainer
                .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))

                // 6. Convert predicted keys back to your Taxonomy String path
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // 4. Train the Model
            Console.WriteLine("Training taxonomy classification model...");
            var trainedModel = pipeline.Fit(trainingDataView);

            // 5. Create a Prediction Engine for isolated single-item executions
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ProductInput, ProductPrediction>(trainedModel);

            // 6. Test an unknown product variant
            var newProduct = new ProductInput
            {
                SubCategoryName = "Ergonomic Office Trackball",
                //Description = "Comfortable desktop thumb-controlled pointer"
            };

            var prediction = predictionEngine.Predict(newProduct);

            // 7. Output Results
            Console.WriteLine("\n--- Product Inference Results ---");
            Console.WriteLine($"Product Category: {newProduct.SubCategoryName}");
            Console.WriteLine($"Predicted Taxonomy Hierarchy: {prediction.PredictedLabel}");

            return predictionEngine;
        }
        public PredictionEngine<ProductInput, ProductPrediction> InitTextCategoryPredict4(IEnumerable<ProductInput> categories)
        {
            // 1. Initialize MLContext (the entry point for all ML.NET operations)
            var mlContext = new MLContext(seed: 1);

            // 2. Mock some sample product training data adhering to a taxonomy
            //    var trainingData = new List<ProductInput>
            //{
            //    new() { Category = "Wireless Mechanical Keyboard", Description = "RGB backlit 87-key typing keyboard", TaxonomyCategory = "Electronics > Computers > Peripherals" },
            //    new() { Category = "Gaming Laser Mouse", Description = "High DPI wired desktop precision pointer", TaxonomyCategory = "Electronics > Computers > Peripherals" },
            //    new() { Category = "Running Shoes Air Max", Description = "Lightweight breathable cross-training sneakers", TaxonomyCategory = "Apparel > Clothing > Shoes" },
            //    new() { Category = "Waterproof Winter Parka", Description = "Heavy insulated hooded coat for freezing weather", TaxonomyCategory = "Apparel > Clothing > Outerwear" }
            //};

            IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(categories);

            // 1. Convert taxonomy string categories to numeric ML keys
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ProductInput.SubCategoryName))

                // Step A: Combine both text features into a single metadata block string
                //.Append(mlContext.Transforms.Concatenate("CombinedText", nameof(ProductInput.SubCategoryName)/*, nameof(ProductInput.SubCategoryDefinition)*/))

                // Step B: Featurize the single text column directly into your finalized "Features" target
                //.Append(mlContext.Transforms.Text.FeaturizeText("Features", "CombinedText"))
                .Append(mlContext.Transforms.Text.FeaturizeText("Features", inputColumnName: nameof(ModelInput.SubCategoryName)))

                //// Step D: Filter out common words like "the", "is", "a"
                //.Append(mlContext.Transforms.Text.RemoveDefaultStopWords("CleanTokens", "Features"))

                //// Step E: Convert the text tokens into a numeric vector (N-grams) that trainers accept
                //.Append(mlContext.Transforms.Conversion.MapValueToKey("Keys", "CleanTokens"))
                //.Append(mlContext.Transforms.Text.ProduceNgrams("Features", "Keys"))

                // Step C: Train the model
                .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // 4. Train the Model
            Console.WriteLine("Training taxonomy classification model...");
            var trainedModel = pipeline.Fit(trainingDataView);

            // 5. Create a Prediction Engine for isolated single-item executions
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ProductInput, ProductPrediction>(trainedModel);

            //// 6. Test an unknown product variant
            //var newProduct = new ProductInput
            //{
            //    Category = "Ergonomic Office Trackball",
            //    Description = "Comfortable desktop thumb-controlled pointer"
            //};

            //var prediction = predictionEngine.Predict(newProduct);

            //// 7. Output Results
            //Console.WriteLine("\n--- Product Inference Results ---");
            //Console.WriteLine($"Product Category: {newProduct.Category}");
            //Console.WriteLine($"Predicted Taxonomy Hierarchy: {prediction.PredictedCategory}");

            return predictionEngine;
        }
        public PredictionEngine<ProductInput, ProductPrediction> InitTextCategoryPredict5(IEnumerable<ProductInput> categories)
        //public PredictionEngine<ModelInput, ModelOutput> InitTextCategoryPredict5(IEnumerable<ProductInput> categories)
        {
            // 2. Initialize MLContext
            var mlContext = new MLContext(seed: 0);

            // Use the GPU if available, otherwise fall back to CPU
            //mlContext.GpuDeviceId = 0;
            //mlContext.FallbackToCpu = true;
            // 3. Create sample data
            //var sampleData = new[] {
            //    new ModelInput { Description = "The laptop screen is flickering and won't turn on.", SubCategoryName = "Hardware" },
            //    new ModelInput { Description = "I forgot my password and need a reset link.", SubCategoryName = "Accounts" },
            //    new ModelInput { Description = "The Wi-Fi drops connection every 10 minutes.", SubCategoryName = "Network" },
            //    new ModelInput { Description = "My mouse pointer is frozen on the display.", SubCategoryName = "Hardware" },
            //    new ModelInput { Description = "Cannot log into the portal with my credentials.", SubCategoryName = "Accounts" }
            //};

            var trainingData = mlContext.Data.LoadFromEnumerable(categories);

            // 4. Build the training pipeline
            // Step A: Map the text 'Category' into numeric keys (Labels)
            // Step B: Use FeaturizeText to convert 'Description' into a numerical vector named 'Features'
            // Step C: Append a multi-class trainer
            // Step D: Map the predicted numeric keys back to the original string category
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(ProductInput.SubCategoryName))
                .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "SubCategoryName", inputColumnName: nameof(ProductInput.SubCategoryName)))
                //.AppendCacheCheckpoint(mlContext)
                .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "SubCategoryDefinition", inputColumnName: nameof(ProductInput.SubCategoryDefinition)))
                .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "SubCategoryOther", inputColumnName: nameof(ProductInput.SubCategoryOther)))
                .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "CategoryName", inputColumnName: nameof(ProductInput.CategoryName)))
                .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "CategoryDescription", inputColumnName: nameof(ProductInput.CategoryDescription)))
                // Step C: Concatenate multiple numeric token vectors into a unified column named "Features"
                .Append(mlContext.Transforms.Concatenate("Features", "SubCategoryName", "SubCategoryDefinition", "SubCategoryOther", "CategoryName", "CategoryDescription"))
                .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                //.Append(mlContext.MulticlassClassification.Trainers.TextClassification(
                //labelColumnName: "Label",
                //sentence1ColumnName: nameof(ProductInput.SubCategoryOther),
                //batchSize: 32,
                //maxEpochs: 5,
                //architecture: BertArchitecture.Roberta))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // 5. Train the model
            Console.WriteLine("Training the model...");
            var model = pipeline.Fit(trainingData);

            // 6. Test with a single prediction
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ProductInput, ProductPrediction>(model);

            //var newIssue = new ProductInput
            //{
            //    KeyWords = "Our office router is blinking red and the internet is completely down."
            //};

            //var prediction = predictionEngine.Predict(newIssue);

            //// 7. Output results
            //Console.WriteLine($"\nKeyWords: {newIssue.KeyWords}");
            //Console.WriteLine($"Predicted Category: {prediction.PredictedLabel}");
            return predictionEngine;
        }
    }
}
