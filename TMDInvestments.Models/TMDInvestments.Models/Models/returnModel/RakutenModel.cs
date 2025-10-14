using System.Collections.Generic;
using System.Xml.Serialization;
using TMDInvestments.Models;

namespace NewsAggregator.models.returnModel
{
    [XmlRoot("result")]
    public sealed class RakutenModel
    {
        [XmlElement("TotalMatches")]
        public string TotalMatches { get; set; }
        [XmlElement("TotalPages")]
        public string TotalPages { get; set; }
        [XmlElement("PageNumber")]
        public string PageNumber { get; set; }
        [XmlElement("item")]
        public List<RakutenModelItems> item = new List<RakutenModelItems>();
    }
}
