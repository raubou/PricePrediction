using System;
using System.Collections.Generic;
using System.Text;

namespace TMDInvestment.Models
{
    public sealed class Coins
    {        
        public int Id { get; set; }
        public string Coin { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public List<HistoricData> history { get; set; }
        public Ticker ticker { get; set; }
        public List<Order> order { get; set; }
    }
}
