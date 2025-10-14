using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NewsAggregator.models.returnModel
{
    [XmlRoot("description")]
    public sealed class RakutenModelDescription
    {
        [XmlElement("short")]
        public string _short { get; set; }
        [XmlElement("long")]
        public string secondary { get; set; }
    }
}
