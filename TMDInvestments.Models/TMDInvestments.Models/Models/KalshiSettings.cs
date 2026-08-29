using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class KalshiSettings
    {
        public KalshiSettings() { }
        public string? BaseUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? PrivateKey { get; set; }
    }
}
