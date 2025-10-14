//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class WatchList
    {
        public string name { get; set; }
        public string watchlistId { get; set; }
        public string accountId { get; set; }
        //[JsonProperty("watchlistItems")]
        public List<WatchListItems> watchListItems { get; set; } = new List<WatchListItems>();
    }

}
