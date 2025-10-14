using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class Login
    {
        public long UserId { get; set; }
        [Required]
        //[EmailAddress(ErrorMessage = "Please enter a valid email address!")]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
