using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class Exceptions
    {
        [Key]
        public long Id { get; set; }

        public string Source { get; set; }
        public string StackTrace { get; set; }
        public string InnerException { get; set; }
        public string Message { get; set; }
        public DateTime DateCreated { get; set; }

        public int LastUpdateBy { get; set; }
    }
}
