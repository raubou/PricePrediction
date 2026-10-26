using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bartergora2._0.Models
{
    [Table("SubCategories3", Schema = "tmd")]
    public sealed class SubCategory
    {
        [Key]
        public long? SubCategoryId { get; set; }
        public long? CategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public string? SubCategoryDefinition { get; set; }
        public string? KeyWords { get; set; } = string.Empty;
        public byte[]? Image { get; set; }
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool? Deleted { get; set; }
    }
}
