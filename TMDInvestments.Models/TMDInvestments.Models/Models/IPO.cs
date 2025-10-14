using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class IPO
    {
        [Key]
        public long? Id { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Exchange { get; set; }
        public string Price { get; set; }
        public string Status { get; set; }
        public long? NumberofShares { get; set; }
        public long? TotalSharesValue { get; set; }
        public DateTime? DateExpected { get; set; }
        public DateTime? DateCreated { get; set; }
    }
}
