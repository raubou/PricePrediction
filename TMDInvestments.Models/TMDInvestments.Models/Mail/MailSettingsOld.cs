using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class MailSettingsOld
    {
        public string Host { get; set; }
        public string To { get; set; }
        public string Password { get; set; }
        public string Alias { get; set; }
        public int Port { get; set; }
    }
}
