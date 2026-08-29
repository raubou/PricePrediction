using NewsAggregator.models.returnModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;



namespace TMDInvestments.Models
{
    [XmlRoot("item")]
    public sealed class RakutenModelItems
    {
        [XmlElement("mid")]
        public string mid { get; set; }
        [XmlElement("merchantname")]
        public string merchantname { get; set; }
        [XmlElement("linkid")]
        public string linkid { get; set; }
        [XmlElement("createdon")]
        public string createdon { get; set; }
        [XmlElement("sku")]
        public string sku { get; set; }
        [XmlElement("productname")]
        public string productname { get; set; }
        [XmlElement("category")]
        public List<RakutenModelCategory> categories = new List<RakutenModelCategory>();
        [XmlElement("price")]
        public string price { get; set; }
        [XmlElement("saleprice")]
        public string saleprice { get; set; }
        [XmlElement("upccode")]
        public string upccode { get; set; }
        [XmlElement("description")]
        public List<RakutenModelDescription> description = new List<RakutenModelDescription>();
        [XmlElement("keywords")]
        public string keywords { get; set; }
        [XmlElement("linkurl")]
        public string linkurl { get; set; }
        [XmlElement("imageurl")]
        public string imageurl { get; set; }

        public int? SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
    }
}
