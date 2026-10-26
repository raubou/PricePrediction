using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bartergora2._0.Models
{
    [Table("ProductsToSubCategories", Schema = "tmd")]
    public sealed class ProductsToSubCategories
    {
        [Key] public long? ProductToSubCategoryId { get; set; }
        public long? SubCategoryId { get; set; }

        public long? ProductId { get; set; }

        public DateTime? DateCreated { get; set; }

        public DateTime? DateUpdated { get; set; }

        public bool? Deleted { get; set; }
    }
}
