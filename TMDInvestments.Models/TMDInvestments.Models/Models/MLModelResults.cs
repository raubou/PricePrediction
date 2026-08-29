using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class MLModelResults
    {
        public float[] ForecastedPriceDifference { get; init; }
        public float[] LowerBoundPriceDifference { get; init; }
        public float[] UpperBoundPriceDifference { get; init; }
    }
    //// Define the data classes
    //public sealed class SentimentData
    //{
    //    public string Text { get; set; }
    //    public string Sentiment { get; set; }
    //}

    //public sealed class SentimentPrediction
    //{
    //    // ML.NET requires a property with this specific name for the prediction output
    //    public string PredictedLabel { get; set; }
    //    public float[] Score { get; set; } // Confidence scores for each class
    //}

    public class ModelInput
    {

        //[LoadColumn(0)]
        public int SubCategoryId { get; set; }

        //[LoadColumn(1)]
        //public string Title { get; set; }
        //[LoadColumn(2)] // Column index in your data file
        public string SubCategoryName { get; set; }
        public string KeyWords { get; set; }

    }

    public class ModelOutput //: ModelInput
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; }
        public float[] Score { get; set; }
    }
}
