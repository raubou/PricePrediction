using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartergora2._0.Models
{
    [Table("SubCategoriesToCategories", Schema = "tmd")]
    public sealed class SubCategoriesToCategories
    {
        [Key] public int? SubCategoryToCategoryId { get; set; }
        public int? CategoryId { get; set; }

        public int? SubCategoryId { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateUpdated { get; set; }
        public bool? Deleted { get; set; }
    }
}
