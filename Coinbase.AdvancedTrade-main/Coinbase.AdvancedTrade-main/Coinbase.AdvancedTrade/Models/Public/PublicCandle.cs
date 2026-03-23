using Newtonsoft.Json;
using System;

namespace Coinbase.AdvancedTrade.Models.Public
{
    /// <summary>
    /// Represents a single candle data point in a trading chart.
    /// </summary>
    public class PublicCandle
    {
        /// <summary>
        /// Gets or sets the start time of the candle in UNIX time.
        /// </summary>
        [JsonProperty("start")]
        public string Start { get; set; }

        /// <summary>
        /// Gets or sets the lowest price during the candle's time period.
        /// </summary>
        [JsonProperty("low")]
        public string Low { get; set; }

        /// <summary>
        /// Gets or sets the highest price during the candle's time period.
        /// </summary>
        [JsonProperty("high")]
        public string High { get; set; }

        /// <summary>
        /// Gets or sets the opening price at the start of the candle's time period.
        /// </summary>
        [JsonProperty("open")]
        public string Open { get; set; }

        /// <summary>
        /// Gets or sets the closing price at the end of the candle's time period.
        /// </summary>
        [JsonProperty("close")]
        public float Close { get; set; }

        /// <summary>
        /// Gets or sets the trading volume during the candle's time period.
        /// </summary>
        [JsonProperty("volume")]
        public string Volume { get; set; }

        /// <summary>
        /// Gets the start date and time of the candlestick.
        /// </summary>
        [JsonIgnore]
        public DateTime StartDate => !string.IsNullOrEmpty(Start) ? UnixTimeStampToDateTime(Start) : DateTime.MinValue;

        /// <summary>
        /// Converts a UNIX timestamp string to its corresponding DateTime value.
        /// </summary>
        /// <param name="unixTimeStamp">The UNIX timestamp string.</param>
        /// <returns>The converted DateTime value, or DateTime.MinValue if the conversion fails.</returns>
        private static DateTime UnixTimeStampToDateTime(string unixTimeStamp)
        {
            if (long.TryParse(unixTimeStamp, out long parsedUnixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(parsedUnixTime).UtcDateTime;
            }
            return DateTime.MinValue;
        }
    }
}
