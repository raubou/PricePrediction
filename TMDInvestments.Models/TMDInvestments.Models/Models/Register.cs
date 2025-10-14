using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class Register
    {
        public long UserId { get; set; }
        [Required]
        //[EmailAddress(ErrorMessage = "Please enter a valid email address!")]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        [Required, Compare(nameof(Password))]
        public string Password2 { get; set; }
        [Required]
        public bool AgreetoTerms { get; set; }
    }
}
