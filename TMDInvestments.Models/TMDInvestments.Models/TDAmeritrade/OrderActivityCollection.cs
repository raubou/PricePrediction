using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public sealed class OrderActivityCollection
    {
        public string activityType { get; set; }
        public long activityId { get; set; }
        public string executionType { get; set; }
        public float quantity { get; set; }
        public float orderRemainingQuantity { get; set; }
        public List<ExecutionLegs> executionLegs { get; set; }
    }
}
