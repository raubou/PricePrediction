using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.Commerce
{
    [Table("BarergoraSearch", Schema = "tmd")]
    public sealed class BarergoraSearch
    {
        public long? ProductId { get; set; }
        public long? AdvertiserId { get; set; }
        public string? AdvertiserName { get; set; } 
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public long? SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public string? SubCategoryDefinition { get; set; }
        public string Source { get; set; }
        public string ProductName { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? SalePrice { get; set; }
        //public string? KeyWords { get; set; }
        public string? linkURL { get; set; }
        public string? SKU { get; set; }
        public string? UPC { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public string? ImageUrl { get; set; }

    }
}
