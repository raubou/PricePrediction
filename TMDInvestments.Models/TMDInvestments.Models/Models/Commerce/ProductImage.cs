using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bartergora2._0.Models
{
    [Table("ProductImages", Schema = "tmd")]
    public sealed class ProductImage
    {
        [Key]
        public long? ProductImageId { get; set; }
        public long? ProductId { get; set; }
        public byte[]? Image { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool? Deleted { get; set; }
    }
}
