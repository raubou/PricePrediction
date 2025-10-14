using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestment.Models
{
    public sealed class CycleAnalytics
    {
        public Account account = new Account();
        public List<Orders> orders { get; set; }
        public List<WatchList> watchlist { get; set; }
    }
}
