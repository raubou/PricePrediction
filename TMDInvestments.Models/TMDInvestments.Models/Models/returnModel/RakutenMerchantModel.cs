using System.Collections.Generic;
using System.Xml.Serialization;

namespace NewsAggregator.models.returnModel
{
    [XmlRoot("result")]
    public sealed class RakutenMerchantModel
    {
        [XmlElement("midlist")]
        public RakutenMerchantList midlist { get; set; }
    }
}
