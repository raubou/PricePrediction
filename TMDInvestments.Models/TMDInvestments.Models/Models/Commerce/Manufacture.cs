using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models.Models.Commerce
{
    public sealed class Manufacture
    {
        [Key]
        public int? ManufacturerId { get; set; }
        public int? ManufactureKey { get; set; }
        public string? ManufacturerNamen { get; set; }
        public byte[]? Image { get; set; }
        public string? URL { get; set; }
        public string? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
        public bool? Deleted { get; set; }
    }
}
