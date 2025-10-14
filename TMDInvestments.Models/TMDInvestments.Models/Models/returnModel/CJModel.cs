using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NewsAggregator.models.returnModel
{
    [XmlRoot("links")]
    public sealed class CJModel
    {
        [XmlElement("link")]
        List<link> items = new List<link>();
    }

    [Serializable, XmlRoot("link")]
    public sealed class link
    {
        [XmlElement("advertiser-id")]
        public int adId { get; set; }
        [XmlElement("advertiser-name")]
        public int adName { get; set; }
        [XmlElement("category")]
        public int adCategory { get; set; }
        //< click-commission>
        //<creative-height>
        //<creative-width>
        //<language>
        //<lead-commission>
        //<link-code-html>
    }
}
