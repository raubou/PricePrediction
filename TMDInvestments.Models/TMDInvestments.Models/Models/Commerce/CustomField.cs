using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bartergora2._0.Models
{
    [Table("CustomFields", Schema = "tmd")]
    public sealed class CustomField
    {
        [Key]
        public int CustomFieldId { get; set; }
        public string CustomFieldName { get; set; }
        public string CustomFieldValue { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public bool Deleted { get; set; }
    }
}
