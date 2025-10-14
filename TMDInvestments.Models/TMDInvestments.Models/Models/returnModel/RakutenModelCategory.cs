using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NewsAggregator.models.returnModel
{
    [XmlRoot("category")]
    public sealed class RakutenModelCategory
    {
        [XmlElement("primary")]
        public string primary { get; set; }
        [XmlElement("secondary")]
        public string secondary { get; set; }
    }
}
