using Microsoft.ML;
using Microsoft.ML.TorchSharp;
using Microsoft.ML.Transforms.TimeSeries;
using System.Text.Json.Nodes;
using TMDInvestment.Models;
using TMDInvestments.Models;

namespace TMD.Services.Service
{
    public sealed class MLPricePredict
    {
        private int horizon = 10;
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
                results.Add($"Upper Bound Price Difference", 0);

                results.Add($"Lower Bound Price Difference", 0);

                results.Add($"Forecast Bound Price Difference", 0);
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
                results.Add($"Upper Bound Price Difference", 0);

                results.Add($"Lower Bound Price Difference", 0);

                results.Add($"Forecast Bound Price Difference", 0);
            }
        }
        public void TestTextCategoryTrainer()
        {
            // Initialize MLContext
            var mlContext = new MLContext();

            // Load your data into a strongly-typed collection
            var reviews = new[]
            {
                new { Text = "This is a bad steak", Sentiment = "Negative" },
                new { Text = "I really like this restaurant", Sentiment = "Positive" },
                new { Text = "The food was okay, but the service was slow", Sentiment = "Negative" },
                new { Text = "Amazing experience, definitely coming back!", Sentiment = "Positive" }
            };
            var reviewsDV = mlContext.Data.LoadFromEnumerable(reviews);

            // Define your training pipeline
            var pipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", "Sentiment")
                .Append(mlContext.MulticlassClassification.Trainers.TextClassification(/*numberOfClasses: 2,*/ sentence1ColumnName: "Text"))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel")); // Map the predicted key back to a string label

            // Train the model (Fit the pipeline to the data)
            var model = pipeline.Fit(reviewsDV);

            // Create a prediction engine for making predictions
            var predEngine = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);

            // Test the trained model with new data
            var input = new SentimentData { Text = "The meal was fantastic!" };
            var result = predEngine.Predict(input);

            Console.WriteLine($"Text: {input.Text}");
            Console.WriteLine($"Prediction: {result.PredictedLabel}");


        }

        //public void Calculat2(List<CandleItems> inputs, ref JsonObject results) 
        //{
        //// Inside Main method:
        //var mlContext = new MLContext();
        ////var data = new List<DataPoint>();
        //// ... Load data into 'data' list ...
        ////IDataView trainingData = mlContext.Data.LoadFromEnumerable(data);
        //IDataView trainingData = mlContext.Data.LoadFromEnumerable(inputs);

        //    // Define, train, and predict
        //    var pipeline = mlContext.Regression.Trainers.Sdca(labelColumnName: "Close");
        //var model = pipeline.Fit(trainingData);
        //var engine = mlContext.Model.CreatePredictionEngine<DataPoint, Prediction>(model);
        //var prediction = engine.Predict(new DataPoint { Features = new float[] { 1f, 2f, 3f, 4f } });
        //    // Run the model on test data set.
        //    var transformedTestData = model.Transform(testData);
        //    // Convert IDataView object to a list.
        //    var predictions = mlContext.Data.CreateEnumerable<Prediction>(
        //        transformedTestData, reuseRowObject: false).ToList();
        //    foreach (var p in predictions)
        //        Console.WriteLine($"Label: {p.Label:F3}, Prediction: {p.Score:F3}");
        //}
        //public void RegressionTrainer(List<CandleItems> inputs, float currentPrice)
        //{
        //    // Create MLContext
        //    MLContext mlContext = new MLContext();

        //    // Sample training data
        //    //    var housingData = new List<HousingData>
        //    //{
        //    //    new HousingData { Size = 600f, CurrentPrice = 170000f },
        //    //    new HousingData { Size = 1000f, CurrentPrice = 225000f },
        //    //    new HousingData { Size = 1200f, CurrentPrice = 250000f },
        //    //    new HousingData { Size = 800f, CurrentPrice = 200000f }
        //    //};

        //    // Load data into IDataView
        //    IDataView data = mlContext.Data.LoadFromEnumerable(inputs);

        //    // 3. Define the data preparation and training pipeline
        //    // Concatenate all features into a single "Features" column, which is required by ML.NET trainers
        //    var pipeline = mlContext.Transforms.Concatenate("Features", "close")
        //        // Append the SDCA regression trainer, specifying the label column name
        //        .Append(mlContext.Regression.Trainers.Sdca(labelColumnName: "CurrentPrice"));

        //    // 4. Train the model
        //    Console.WriteLine("Training the model...");
        //    ITransformer trainedModel = pipeline.Fit(data);
        //    Console.WriteLine("Model trained.");

        //    // 5. Make a prediction
        //    Console.WriteLine("Making a prediction...");
        //    // Create a prediction engine from the trained model
        //    var predictionEngine = mlContext.Model.CreatePredictionEngine<CandleItems, PricePrediction>(trainedModel);

        //    // Input data for prediction
        //    var StockPrice = new CandleItems { close = currentPrice };

        //    // Predict the price
        //    var prediction = predictionEngine.Predict(StockPrice);

        //    Console.WriteLine($"\nPredicted price for a {StockPrice.close} sq ft house: {prediction.Score:C}");
        //}
    }
    // 1. Define the input data model
    //public class StockData
    //{
    //    public float Size { get; set; }
    //    public float CurrentPrice { get; set; }
    //}
    // 2. Define the prediction output model
    //public class PricePrediction
    //{
    //    // ML.NET convention for the predicted value
    //    public float Score { get; set; }
    //}
    //Key Steps Explained
    //MLContext: This is the starting point for all ML.NET operations and serves as a catalog of components and operations.
    //Data Models: The input (HousingData) and output (PricePrediction) classes define the schema for your data. The output Score property is a standard ML.NET convention for regression results.
    //Load Data: LoadFromEnumerable converts your in-memory data collection into an IDataView, a format ML.NET can process.
    //Data Preparation:
    //mlContext.Transforms.Concatenate("Features", "Size"): This step is crucial.ML.NET trainers require a single vector column named "Features" containing all input features.
    //Trainer: mlContext.Regression.Trainers.Sdca() adds the Stochastic Dual Coordinate Ascent regression algorithm to the pipeline.
    //Training: The Fit() method trains the model using the prepared data.
    //Prediction: A PredictionEngine is created to make predictions on single instances of data using the Predict() method.
    //Microsoft Learn
    //Microsoft Learn
    // +6
    //For more detailed examples and documentation, refer to the Microsoft ML.NET documentation and the official machinelearning-samples GitHub repository.
    //Train and evaluate a model - ML.NET - Microsoft Learn
    //Dec 19, 2024 — // 1. Concatenate Size and Historical into a single feature vector output to a new column called Features // 2. Normalize Features...

    //Microsoft Learn

    //Save and load trained models - ML.NET | Microsoft Learn
    //Dec 19, 2024 — Although this sample uses a linear regression model, the same process applies to other ML.NET algorithms. ... HousingData[] housin...

    //Microsoft Learn
    //What is ML.NET and how does it work? - Microsoft
    //Dec 19, 2024 — Simple ML.NET app The code in the following snippet demonstrates the simplest ML.NET application. This example constructs a linear...

    //Microsoft Learn


    //}


    //    public void Calculat3()
    //    {
    //        // Initialize MLContext
    //        MLContext mlContext = new MLContext(seed: 0);

    //        // 3. Load or create sample data
    //        var data = new[]
    //        {
    //        new CarPriceData { Year = 2018, Horsepower = 200, Seats = 5, Price = 35000 },
    //        new CarPriceData { Year = 2015, Horsepower = 150, Seats = 4, Price = 22000 },
    //        new CarPriceData { Year = 2020, Horsepower = 300, Seats = 5, Price = 50000 },
    //        new CarPriceData { Year = 2010, Horsepower = 100, Seats = 4, Price = 10000 },
    //        new CarPriceData { Year = 2022, Horsepower = 350, Seats = 5, Price = 65000 }
    //    };
    //        IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(data);

    //        // 4. Define the data processing pipeline and select the FastTreeRegressionTrainer
    //        var pipeline = mlContext.Transforms.Concatenate("Features", "Year", "Horsepower", "Seats")
    //            .AppendCacheCheckpoint(mlContext) // Caching helps trainers that use multiple passes
    //            .Append(mlContext.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options
    //            {
    //                NumberOfLeaves = 10, // Max number of leaves in each tree
    //                NumberOfTrees = 100, // Total number of trees in the ensemble
    //                MinimumExampleCountPerLeaf = 1, // Minimal number of data points required to form a new leaf
    //                LearningRate = 0.2f,
    //                LabelColumnName = "Label",
    //                FeatureColumnName = "Features"
    //            }));

    //        // 5. Train the model
    //        Console.WriteLine("Training the FastTree regression model...");
    //        var model = pipeline.Fit(trainingDataView);
    //        Console.WriteLine("Model training complete.");

    //        // 6. Use the model for a prediction
    //        var predictionEngine = mlContext.Model.CreatePredictionEngine<CarPriceData, CarPricePrediction>(model);

    //        var sampleCar = new CarPriceData { Year = 2019, Horsepower = 220, Seats = 5 };
    //        var prediction = predictionEngine.Predict(sampleCar);

    //        Console.WriteLine($"\nSample Car Data: Year: {sampleCar.Year}, HP: {sampleCar.Horsepower}, Seats: {sampleCar.Seats}");
    //        Console.WriteLine($"Predicted Price (Score): {prediction.PredictedPrice:C}");
    //    }
    //    public void Calculat4()
    //    {
    //        // Set the random seed for deterministic results
    //        var mlContext = new MLContext(seed: 0);

    //        // 3. Load the data (using in-memory data for this example)
    //        var trainingData = mlContext.Data.LoadFromEnumerable(GenerateSampleData());

    //        // 4. Define the data processing pipeline and trainer
    //        // First, concatenate all features into a single "Features" column
    //        var pipeline = mlContext.Transforms.Concatenate("Features", new[] { "Size" })
    //            // Append the LightGbm Regression Trainer
    //            .Append(mlContext.Regression.Trainers.LightGbm(labelColumnName: "Price"));

    //        // Optionally, define trainer options for fine-tuning
    //        var options = new LightGbmRegressionTrainer.Options
    //        {
    //            LabelColumnName = "Price",
    //            FeatureColumnName = "Features",
    //            NumberOfLeaves = 50, // Example parameter tuning
    //            //MinimumDataPointsPerLeaf = 10,
    //            MinimumExampleCountPerLeaf = 10,
    //            LearningRate = 0.1,
    //            // ... other parameters from the [Microsoft docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.trainers.lightgbm.lightgbmregressiontrainer.options?view=ml-dotnet-preview)
    //        };
    //        // Use the options in the pipeline (replace the above .Append() with this)
    //        // var pipelineWithOptions = mlContext.Transforms.Concatenate("Features", new[] { "Size" })
    //        //    .Append(mlContext.Regression.Trainers.LightGbm(options));

    //        // 5. Train the model
    //        Console.WriteLine("Training the model...");
    //        var model = pipeline.Fit(trainingData);

    //        // 6. Make a prediction (demonstration)
    //        var predictionEngine = mlContext.Model.CreatePredictionEngine<HouseData, PricePrediction>(model);

    //        var house = new HouseData { Size = 1.5F }; // Example house size
    //        var prediction = predictionEngine.Predict(house);

    //        Console.WriteLine($"\nPredicted price for a house of size {house.Size} units: {prediction.PredictedPrice:C}");
    //    }
    //    // Helper method to generate some sample data
    //    private static IEnumerable<HouseData> GenerateSampleData()
    //    {
    //        return new[]
    //        {
    //        new HouseData { Size = 1.1F, Price = 1200F },
    //        new HouseData { Size = 1.2F, Price = 1300F },
    //        new HouseData { Size = 1.3F, Price = 1400F },
    //        new HouseData { Size = 1.4F, Price = 1500F },
    //        new HouseData { Size = 1.5F, Price = 1600F },
    //        new HouseData { Size = 1.6F, Price = 1700F },
    //        new HouseData { Size = 1.7F, Price = 1800F },
    //        new HouseData { Size = 1.8F, Price = 1900F },
    //        new HouseData { Size = 1.9F, Price = 2000F },
    //        new HouseData { Size = 2.0F, Price = 2100F }
    //    };
    //    }
    //}
    //// Define input/output classes
    //public class DataPoint
    //{
    //    public float Label { get; set; }
    //    [VectorType(4)] public float[] Features { get; set; }
    //}
    //public class Prediction { public float Score { get; set; } }
    //// 1. Define your data structure
    //public class CarPriceData
    //{
    //    [LoadColumn(0)]
    //    public float Year { get; set; }

    //    [LoadColumn(1)]
    //    public float Horsepower { get; set; }

    //    [LoadColumn(2)]
    //    public float Seats { get; set; }

    //    // This is the label (the value you want to predict)
    //    [LoadColumn(3), ColumnName("Label")]
    //    public float Price { get; set; }
    //}

    //// 2. Define the structure for predictions
    //public class CarPricePrediction
    //{
    //    // ML.NET mandates the output column name 'Score' for regression predictions
    //    [ColumnName("Score")]
    //    public float PredictedPrice { get; set; }
    //}
    //// 1. Define the data structure
    //public class HouseData
    //{
    //    [LoadColumn(0)]
    //    public float Size { get; set; }

    //    [LoadColumn(1)]
    //    public float Price { get; set; } // The label (target variable)
    //}

    //// 2. Define the prediction structure
    //public class PricePrediction
    //{
    //    [ColumnName("Score")]
    //    public float PredictedPrice { get; set; }
    //}

    //}
}
