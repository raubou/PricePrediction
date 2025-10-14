using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class Banner
    {
        public long? Id { get; set; }
        public int? BannerId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string BackGroundColor { get; set; }
        public bool Disabled { get; set; }
        public DateTime? DateUpdated { get; set; }
        public RssModel rssModel { get; set; }
    }
}
