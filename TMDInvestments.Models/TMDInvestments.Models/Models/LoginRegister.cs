using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class LoginRegister
    {
        public Login login { get; set; }
        public Register register { get; set; }
        public Recover recover { get; set; }
     }
}
