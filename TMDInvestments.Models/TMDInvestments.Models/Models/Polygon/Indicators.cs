using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.Polygon
{
    public sealed class Indicators
    {
        public string status { get; set; } = string.Empty;
        [JsonPropertyName("results")]
        public Results results { get; set; } = new Results();
        public string error { get; set; } = string.Empty;
    }
    public sealed class  Results
    {
        [JsonPropertyName("values")]
        public List<Value> values { get; set; } = new List<Value>();
    }

    public sealed class Value
    {
        public long timestamp { get { return new DateTimeOffset(date).ToUnixTimeMilliseconds(); } set { date = DateTimeOffset.FromUnixTimeMilliseconds(value).DateTime; } }
        public decimal? histogram { get; set; }
        public decimal? signal { get; set; }
        public decimal? value { get; set; }
        public DateTime date;
  
    }
}
