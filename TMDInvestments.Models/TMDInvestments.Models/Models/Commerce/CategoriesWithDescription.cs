using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.Commerce
{
    public sealed class CategoriesWithDescription
    {
        //[Key] // Column index in your data file
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDescription { get; set; }
        public string SubCategoryName { get; set; }
        public string SubCategoryDefinition { get; set; }
        public string KeyWords { get; set; }
    }
}
