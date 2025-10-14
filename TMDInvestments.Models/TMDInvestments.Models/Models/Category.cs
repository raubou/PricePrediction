using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class Category
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public int? SortOrder { get; set; }
        public bool Disabled { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
