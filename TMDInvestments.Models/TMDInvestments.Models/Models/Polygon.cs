using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class Polygon
    {
        public string Url { get; set; }
        public string PolygonToken { get; set; }
        public string GroupDaily { get; set; }
        public string GainersLosers { get; set; }
        public string History { get; set; }        
        public string Search { get; set; }
        public string Profile { get; set; }
        public string Quote { get; set; }

        public string Indicators { get; set; }
    }
}
