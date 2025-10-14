using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class Users
    {
        [Key]
        public long UserId { get; set; }
        public long RoleId { get; set; }
        [EmailAddress]
        public string UserName { get; set; }
        [DataType(DataType.Password)]
        public byte[]? Password { get; set; }
        public bool? Disabled { get; set; }
        [DefaultValue("true")]
        public DateTime? DateEntered { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
