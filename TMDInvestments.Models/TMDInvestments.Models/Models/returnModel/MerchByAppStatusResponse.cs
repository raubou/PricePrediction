using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace TMDInvestments.Models.Models.returnModel
{
    [XmlRoot("getMerchByAppStatusResponse", Namespace = "http://endpoint.linkservice.linkshare.com/")]
    public sealed class MerchByAppStatusResponse
    {
        public MerchByAppStatusResponse() { }
        [XmlElement("return")]
        public List<MerchantItem> Items { get; set; }



    }

    [XmlRoot("return", Namespace = "http://endpoint.linkservice.linkshare.com/")]
    public sealed class MerchantItem
    {


        public string applicationStatus { get; set; }

        public string categories { get; set; }

        public string mid { get; set; }

        public string name { get; set; }

        [XmlElement("offer")]
        public Offers Offer { get; set; }
    }

    [XmlRoot("offer", Namespace = "http://endpoint.linkservice.linkshare.com/")]
    public sealed class Offers
    {

        public string alsoName { get; set; }

        public string commissionTerms { get; set; }

        public string offerId { get; set; }

        public string offerName { get; set; }
    }
}
