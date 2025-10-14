using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TMD.Database.Connect.MongoDB.entity
{    
    public sealed class Subscriber
    {
        //System.ComponentModel.DataAnnotations.Schema.
        //[ForeignKey("CustomerId")]
        [Key]
        public int SubscriberId { get; set; }
        public string email { get; set; }
        public int Timezone { get; set; }
        public string System { get; set; }
        public string Browser { get; set; }
        public string Language { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string IP { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
