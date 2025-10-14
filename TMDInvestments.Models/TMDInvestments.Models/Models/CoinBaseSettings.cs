using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class CoinBaseSettings
    {
        public string loginUrl { get; set; }
        public string Url {  get; set; } 
        public string Name { get; set; }
        public string PrivateKey { get; set; }
    }
}
