using System;
using System.Collections.Generic;
using System.Text;

namespace Coinbase.AdvancedTrade.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class Portfolio
    {
        public string name { get; set; }
        public string uuid { get; set; }
        public string type { get; set; }
        public bool deleted { get; set; }
    }
}

