using System;
using System.Collections.Generic;
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
}
