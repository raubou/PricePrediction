using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMDInvestments.Models;

namespace NewsAggregator.models.returnModel
{
    [Serializable]
    public sealed class StackcommerceModel
    {
        [Required]
        public int source { get; set; }
        [Required]
        public string title { get; set; }
        [Required]
        public IList<RssModelItems> items { get; set; }
    }
}
