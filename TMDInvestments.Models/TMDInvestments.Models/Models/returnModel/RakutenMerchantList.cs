using System.Collections.Generic;
using System.Xml.Serialization;

namespace NewsAggregator.models.returnModel
{
    [XmlRoot("midlist")]
    public sealed class RakutenMerchantList
    {
        [XmlElement("merchant")]
        public List<RakutenMerchantItems> merchant = new List<RakutenMerchantItems>();
    }
}
