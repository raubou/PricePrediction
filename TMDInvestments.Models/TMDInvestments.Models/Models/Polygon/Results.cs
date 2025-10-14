using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.Polygon
{
    public sealed class Results<T>
    {
        public Results() {
            results = new List<T>();
        }
        public List<T> results { get; set; }
    }
}
