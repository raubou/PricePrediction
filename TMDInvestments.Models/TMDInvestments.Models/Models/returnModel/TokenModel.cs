//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NewsAggregator.models.returnModel
{
    [Serializable]
    public sealed class TokenModel
    {
        
        public string token_type { get; set; }
    
        public int expires_in { get; set; }
     
        public string refresh_token { get; set; }
     
        public string access_token { get; set; }
    }
}
