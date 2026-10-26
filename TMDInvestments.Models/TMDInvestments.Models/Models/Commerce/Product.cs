using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bartergora2._0.Models
{
    [Table("Products", Schema = "tmd")]
    public sealed class Product
    {
        [Key]
        public long? ProductId { get; set; }       
        public long? AdvertiserId { get; set; }
        public string Source { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? SalePrice { get; set; }
        public string? KeyWords { get; set; }
        public string? linkURL { get; set; }
        public string? SKU { get; set; }
        public string? UPC { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool? Deleted { get; set; }
    }
}
