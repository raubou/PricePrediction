
using System.Xml.Serialization;

namespace NewsAggregator.models.returnModel
{
    [XmlRoot("merchant")]
    public sealed class RakutenMerchantItems
    {
        [XmlElement("mid")]
        public string mid { get; set; }
        [XmlElement("merchantname")]
        public string merchantName { get; set; }
    }
}
