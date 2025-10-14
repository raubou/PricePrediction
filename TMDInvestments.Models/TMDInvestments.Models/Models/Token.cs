using System;
using System.ComponentModel.DataAnnotations;

namespace TMDInvestments.Models
{
    public sealed class Token
    {
        public Token()
        {

        }
        [Key]
        public long? Id { get; set; }
        public TokenType TokenType { get; set; }
        public byte[]? Access_Token { get; set; }
        public byte[]? Refresh_Token { get; set; }
        public DateTime? DateExpires { get; set; }
        public DateTime? DateEntered { get; set; }
        public DateTime? DateUpdated { get; set; }
        public int? AuditId { get; set; }
    }
}
