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
        public string Description { get; set; }

    }

    public class ModelOutput //: ModelInput
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; }
        public float[] Score { get; set; }
    }

    public class ProductInput
    {
        [LoadColumn(0)]
        public long SubCategoryId { get; set; }
        [LoadColumn(1)]
        public string SubCategoryName { get; set; }
        [LoadColumn(2)]
        public string SubCategoryDefinition { get; set; }
        [LoadColumn(3)]
        public string SubCategoryOther { get; set; }
        [LoadColumn(4)]
        public long CategoryId { get; set; }
        [LoadColumn(5)]
        //public long SubCategoryId { get; set; }
        public string CategoryName { get; set; }
        [LoadColumn(6)]
        public string CategoryDescription { get; set; }
        //public string SubCategoryName { get; set; }
        //public string SubCategoryDefinition { get; set; }
        //public string SubCategoryOther { get; set; }

    }

    public class ProductPrediction
    {
        // ML.NET defaults the output classification text to "PredictedLabel"
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; }

        // Floating-point probabilities corresponding to all categories
        public float[] Score { get; set; }
    }
}
