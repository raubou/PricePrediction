using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class CryptoList
    {
        [Key]
        public long? Id { get; set; }
        public string Coin { get; set; }
        public string Name { get; set; }
        public bool ExcludeFromAutomation { get; set; }
        public bool UseRsi { get; set; }
        public bool UseSma200 { get; set; }
        public bool UseEma200 { get; set; }
        public bool UseVwap { get; set; }
        public bool UseEma9 { get; set; }
        public bool UseEma12 { get; set; }
        public bool UseEma26 { get; set; }
        public bool UseMacd { get; set; }
        public int? QuoteRetry { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int? LastUpdatedBy { get; set; }
    }
}
