using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class MLModelResults
    {
        public float[] ForecastedPriceDiffrence { get; init; }
        public float[] LowerBoundPriceDiffrence { get; init; }
        public float[] UpperBoundPriceDiffrence { get; init; }
    }
}
