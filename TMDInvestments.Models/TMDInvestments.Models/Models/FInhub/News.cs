using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.FInhub
{
    public sealed class News
    {
     public string category { get; set; }
        public int datetime { get; set; }
        public string headline { get; set; }
        public int id { get; set; }
        public string image { get; set; }
        public string related { get; set; }
        public string source { get; set; }
        public string summary { get; set; }
    }
}
