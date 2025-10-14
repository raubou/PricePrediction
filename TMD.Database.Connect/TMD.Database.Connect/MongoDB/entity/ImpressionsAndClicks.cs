using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMD.Database.Connect.MongoDB.entity
{
    public sealed class ImpressionsAndClicks
    {
        [Key]
        public int ImpressionsAndClicksId { get; set; }
        public string Platform { get; set; }
        public string Title { get; set; }
        public string Advertisor { get; set; }
        public string Link { get; set; }
        public int Impressions { get; set; }
        public int Clicks { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
