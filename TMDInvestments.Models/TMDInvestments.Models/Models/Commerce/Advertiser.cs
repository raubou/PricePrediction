using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bartergora2._0.Models
{
    [Table("Advertisers", Schema = "tmd")]
    public sealed class Advertiser
    {
        [Key]
        public int? AdvertiserId { get; set; }
        public int? AdvertiserKey { get; set; }
        public string AdvertiserName { get; set; }
        public byte[]? Image { get; set; }
        public string? URL { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool? Deleted { get; set; }

        public bool? IsActive { get; set; }
    }
}
