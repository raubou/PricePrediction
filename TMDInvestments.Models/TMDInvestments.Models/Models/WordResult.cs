using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class WordResult
    {  
        public string word { get; set; }
        public int score { get; set; }
        public List<string> tags { get; set; }

    }
}
