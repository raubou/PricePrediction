using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMD.Database.Connect.MongoDB.entity
{
    public sealed class ClientInfo
    {
        [Key]
        public int ClientInfoId { get; set; }
        public string Page { get; set; }
        public int Timezone { get; set; }
        public string System { get; set; }
        public string Browser { get; set; }
        public string Language { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string PostaCode { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string IP { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
