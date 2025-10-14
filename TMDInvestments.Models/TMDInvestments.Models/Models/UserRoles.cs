using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TMDInvestments.Models
{
    public sealed class UserRoles
    {
        [Key]
        public long Id { get; set; }
        public string Name { get; set; }
        public bool? Disabled { get; set; }
        [DefaultValue("true")]
        public DateTime? DateEntered { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
