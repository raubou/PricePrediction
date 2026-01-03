using Microsoft.ML;
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
                windowSize: (inputs.Count <= 14) ? (int)Math.Round((decimal)inputs.Count /2, 0) : 7,
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
        public void Calculate(List<Coinbase.AdvancedTrade.Models.Candle> inputs, ref JsonObject results)
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
    }
}
