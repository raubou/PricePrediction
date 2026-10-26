using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security;
using System.Threading.Tasks;


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
        //[JsonProperty("start")]
        public string Start { get; set; }

        /// <summary>
        /// Gets or sets the lowest price during the candle's time period.
        /// </summary>
        //[JsonProperty("low")]
        public string Low { get; set; }

        /// <summary>
        /// Gets or sets the highest price during the candle's time period.
        /// </summary>
        //[JsonProperty("high")]
        public string High { get; set; }

        /// <summary>
        /// Gets or sets the opening price at the start of the candle's time period.
        /// </summary>
        //[JsonProperty("open")]
        public float Open { get; set; }

        /// <summary>
        /// Gets or sets the closing price at the end of the candle's time period.
        /// </summary>
        //[JsonProperty("close")]
        public float Close { get; set; }

        /// <summary>
        /// Gets or sets the trading volume during the candle's time period.
        /// </summary>
        //[JsonProperty("volume")]
        public float Volume { get; set; }

        //[JsonProperty("opChange")]
        //public float opChange { get { return Close - Open; } set; }
        ////[JsonProperty("opPercent")]
        //public float opPercent { get { return (Close - Open) / ((Close + Open) / 2) * 100; } set; }
        public float opChange { get => Close - Open; }
        public float opPercent { get => (Close - Open) / ((Close + Open) / 2) * 100; }
        /// <summary>
        /// Gets the start date and time of the candlestick.
        /// </summary>
        //[JsonIgnore]
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
