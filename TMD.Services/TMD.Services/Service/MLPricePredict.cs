using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Trainers.LightGbm;
using Microsoft.ML.Transforms.TimeSeries;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TMDInvestment.Models;
using TMDInvestments.Models;

namespace TMD.Services.Service
{
    public sealed class MLPricePredict
    {
        public MLPricePredict()
        {
        }
        public void Calculate(List<CandleItems> inputs, ref JsonObject results)
        {
            try
            {
                //Create model
                //string symbol = "NBR";
                //Console.WriteLine("Symbol " + symbol);
                var mlContext = new MLContext();
                //var trainDataVeiw = mlContext.Data.LoadFromEnumerable(inputs);
                //var cacaface = StaticMethods.GetPriceHistory(symbol, services);
                IDataView trainDataVeiw = mlContext.Data.LoadFromEnumerable<CandleItems>(inputs);

                var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: "ForecastedPriceDiffrence",
                    inputColumnName: "close",
                    windowSize: (inputs.Count <= 14) ? (int)Math.Round((decimal)inputs.Count / 2, 0) : 7,
                    seriesLength: 21,
                    trainSize: inputs.Count,
                    horizon: 1,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: "LowerBoundPriceDiffrence",
                    confidenceUpperBoundColumn: "UpperBoundPriceDiffrence");

                var forecaster = forecastingPipeline.Fit(trainDataVeiw);
                //return forecaster.CreateTimeSeriesEngine<ModelInput, ModelOutput>(mlContext);
                var forecastEngine = forecaster.CreateTimeSeriesEngine<CandleItems, MLModelResults>(mlContext);
                var prediction = forecastEngine.Predict();
                foreach (var item in prediction.UpperBoundPriceDiffrence)
                {
                    //Console.WriteLine("Upper Bound " + item);
                    results.Add("Upper Bound Price Diffrence", item);
                }
                foreach (var item in prediction.LowerBoundPriceDiffrence)
                {
                    //Console.WriteLine("Lower Bound " + item);
                    results.Add("Lower Bound Price Diffrence", item);
                }
                foreach (var item in prediction.ForecastedPriceDiffrence)
                {
                    //Console.WriteLine("Forecast Bound " + item);
                    results.Add("Forecast Bound Price Diffrence", item);
                }
            }
            catch (Exception)
            {
                    results.Add("Upper Bound Price Diffrence", 0);

                    results.Add("Lower Bound Price Diffrence", 0);

                    results.Add("Forecast Bound Price Diffrence", 0);
            }

        }
        public void Calculate(List<Coinbase.AdvancedTrade.Models.Candle> inputs, ref JsonObject results)
        {
            try
            {
                //Create model
                //string symbol = "NBR";
                //Console.WriteLine("Symbol " + results[""].ToString());
                var mlContext = new MLContext();
                //var trainDataVeiw = mlContext.Data.LoadFromEnumerable(inputs);
                //var cacaface = StaticMethods.GetPriceHistory(symbol, services);
                IDataView trainDataVeiw = mlContext.Data.LoadFromEnumerable<Coinbase.AdvancedTrade.Models.Candle>(inputs);

                var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: "ForecastedPriceDiffrence",
                    inputColumnName: "Close",
                    windowSize: (inputs.Count <= 14) ? (int)Math.Round((decimal)inputs.Count / 2, 0) : 7,
                    seriesLength: 21,
                    trainSize: inputs.Count,
                    horizon: 1,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: "LowerBoundPriceDiffrence",
                    confidenceUpperBoundColumn: "UpperBoundPriceDiffrence");

                var forecaster = forecastingPipeline.Fit(trainDataVeiw);
                //return forecaster.CreateTimeSeriesEngine<ModelInput, ModelOutput>(mlContext);
                var forecastEngine = forecaster.CreateTimeSeriesEngine<Coinbase.AdvancedTrade.Models.Candle, MLModelResults>(mlContext);
                var prediction = forecastEngine.Predict();
                foreach (var item in prediction.UpperBoundPriceDiffrence)
                {
                    //Console.WriteLine("Upper Bound " + item);
                    results.Add("Upper Bound Price Diffrence", item);
                }
                foreach (var item in prediction.LowerBoundPriceDiffrence)
                {
                    //Console.WriteLine("Lower Bound " + item);
                    results.Add("Lower Bound Price Diffrence", item);
                }
                foreach (var item in prediction.ForecastedPriceDiffrence)
                {
                    //Console.WriteLine("Forecast Bound " + item);
                    results.Add("Forecast Bound Price Diffrence", item);
                }
            }
            catch (Exception)
            {
                results.Add("Upper Bound Price Diffrence", 0);

                results.Add("Lower Bound Price Diffrence", 0);

                results.Add("Forecast Bound Price Diffrence", 0);
            }
        }
        public void Calculat2() 
        {
        // Inside Main method:
        var mlContext = new MLContext();
        var data = new List<DataPoint>();
        // ... Load data into 'data' list ...
        IDataView trainingData = mlContext.Data.LoadFromEnumerable(data);

        // Define, train, and predict
        var pipeline = mlContext.Regression.Trainers.Sdca(labelColumnName: "Label");
        var model = pipeline.Fit(trainingData);
        var engine = mlContext.Model.CreatePredictionEngine<DataPoint, Prediction>(model);
        var prediction = engine.Predict(new DataPoint { Features = new float[] { 1f, 2f, 3f, 4f } });
        }
        public void Calculat3()
        {
            // Initialize MLContext
            MLContext mlContext = new MLContext(seed: 0);

            // 3. Load or create sample data
            var data = new[]
            {
            new CarPriceData { Year = 2018, Horsepower = 200, Seats = 5, Price = 35000 },
            new CarPriceData { Year = 2015, Horsepower = 150, Seats = 4, Price = 22000 },
            new CarPriceData { Year = 2020, Horsepower = 300, Seats = 5, Price = 50000 },
            new CarPriceData { Year = 2010, Horsepower = 100, Seats = 4, Price = 10000 },
            new CarPriceData { Year = 2022, Horsepower = 350, Seats = 5, Price = 65000 }
        };
            IDataView trainingDataView = mlContext.Data.LoadFromEnumerable(data);

            // 4. Define the data processing pipeline and select the FastTreeRegressionTrainer
            var pipeline = mlContext.Transforms.Concatenate("Features", "Year", "Horsepower", "Seats")
                .AppendCacheCheckpoint(mlContext) // Caching helps trainers that use multiple passes
                .Append(mlContext.Regression.Trainers.FastTree(new FastTreeRegressionTrainer.Options
                {
                    NumberOfLeaves = 10, // Max number of leaves in each tree
                    NumberOfTrees = 100, // Total number of trees in the ensemble
                    MinimumExampleCountPerLeaf = 1, // Minimal number of data points required to form a new leaf
                    LearningRate = 0.2f,
                    LabelColumnName = "Label",
                    FeatureColumnName = "Features"
                }));

            // 5. Train the model
            Console.WriteLine("Training the FastTree regression model...");
            var model = pipeline.Fit(trainingDataView);
            Console.WriteLine("Model training complete.");

            // 6. Use the model for a prediction
            var predictionEngine = mlContext.Model.CreatePredictionEngine<CarPriceData, CarPricePrediction>(model);

            var sampleCar = new CarPriceData { Year = 2019, Horsepower = 220, Seats = 5 };
            var prediction = predictionEngine.Predict(sampleCar);

            Console.WriteLine($"\nSample Car Data: Year: {sampleCar.Year}, HP: {sampleCar.Horsepower}, Seats: {sampleCar.Seats}");
            Console.WriteLine($"Predicted Price (Score): {prediction.PredictedPrice:C}");
        }
        public void Calculat4()
        {
            // Set the random seed for deterministic results
            var mlContext = new MLContext(seed: 0);

            // 3. Load the data (using in-memory data for this example)
            var trainingData = mlContext.Data.LoadFromEnumerable(GenerateSampleData());

            // 4. Define the data processing pipeline and trainer
            // First, concatenate all features into a single "Features" column
            var pipeline = mlContext.Transforms.Concatenate("Features", new[] { "Size" })
                // Append the LightGbm Regression Trainer
                .Append(mlContext.Regression.Trainers.LightGbm(labelColumnName: "Price"));

            // Optionally, define trainer options for fine-tuning
            var options = new LightGbmRegressionTrainer.Options
            {
                LabelColumnName = "Price",
                FeatureColumnName = "Features",
                NumberOfLeaves = 50, // Example parameter tuning
                //MinimumDataPointsPerLeaf = 10,
                MinimumExampleCountPerLeaf = 10,
                LearningRate = 0.1,
                // ... other parameters from the [Microsoft docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.trainers.lightgbm.lightgbmregressiontrainer.options?view=ml-dotnet-preview)
            };
            // Use the options in the pipeline (replace the above .Append() with this)
            // var pipelineWithOptions = mlContext.Transforms.Concatenate("Features", new[] { "Size" })
            //    .Append(mlContext.Regression.Trainers.LightGbm(options));

            // 5. Train the model
            Console.WriteLine("Training the model...");
            var model = pipeline.Fit(trainingData);

            // 6. Make a prediction (demonstration)
            var predictionEngine = mlContext.Model.CreatePredictionEngine<HouseData, PricePrediction>(model);

            var house = new HouseData { Size = 1.5F }; // Example house size
            var prediction = predictionEngine.Predict(house);

            Console.WriteLine($"\nPredicted price for a house of size {house.Size} units: {prediction.PredictedPrice:C}");
        }
        // Helper method to generate some sample data
        private static IEnumerable<HouseData> GenerateSampleData()
        {
            return new[]
            {
            new HouseData { Size = 1.1F, Price = 1200F },
            new HouseData { Size = 1.2F, Price = 1300F },
            new HouseData { Size = 1.3F, Price = 1400F },
            new HouseData { Size = 1.4F, Price = 1500F },
            new HouseData { Size = 1.5F, Price = 1600F },
            new HouseData { Size = 1.6F, Price = 1700F },
            new HouseData { Size = 1.7F, Price = 1800F },
            new HouseData { Size = 1.8F, Price = 1900F },
            new HouseData { Size = 1.9F, Price = 2000F },
            new HouseData { Size = 2.0F, Price = 2100F }
        };
        }
    }
    // Define input/output classes
    public class DataPoint
    {
        public float Label { get; set; }
        [VectorType(4)] public float[] Features { get; set; }
    }
    public class Prediction { public float Score { get; set; } }
    // 1. Define your data structure
    public class CarPriceData
    {
        [LoadColumn(0)]
        public float Year { get; set; }

        [LoadColumn(1)]
        public float Horsepower { get; set; }

        [LoadColumn(2)]
        public float Seats { get; set; }

        // This is the label (the value you want to predict)
        [LoadColumn(3), ColumnName("Label")]
        public float Price { get; set; }
    }

    // 2. Define the structure for predictions
    public class CarPricePrediction
    {
        // ML.NET mandates the output column name 'Score' for regression predictions
        [ColumnName("Score")]
        public float PredictedPrice { get; set; }
    }
    // 1. Define the data structure
    public class HouseData
    {
        [LoadColumn(0)]
        public float Size { get; set; }

        [LoadColumn(1)]
        public float Price { get; set; } // The label (target variable)
    }

    // 2. Define the prediction structure
    public class PricePrediction
    {
        [ColumnName("Score")]
        public float PredictedPrice { get; set; }
    }

}
