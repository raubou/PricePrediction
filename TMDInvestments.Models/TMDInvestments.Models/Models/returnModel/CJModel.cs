using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NewsAggregator.models.returnModel
{
    //[XmlRoot("links")]
    public sealed class CJModel
    {
        //[XmlElement("link")]
        //List<LinkCode> items = new List<LinkCode>();
        public int count { get; set; }
        public int limit { get; set; }
        public int totalCount { get; set; }
        public int productCount { get; set; }
        public string lastUpdated { get; set; }
        public string nextPage { get; set; }
        public Products products { get; set; }
    }

    public sealed class Products 
    {
        public int count { get; set; }
        public int limit { get; set; }
        public int totalCount { get; set; }
        public int productCount { get; set; }
        public string lastUpdated { get; set; }
        public string nextPage { get; set; }
        public IEnumerable<ResultList> ResultList { get; set; } = new List<ResultList>();
    }
    public sealed class ResultList
    {
        public long SubCategoryId { get; set; }
        public string SubCategoryName { get; set; }
        public string sourceFeedType { get; set; }
        public string description { get; set; }
        public Price price { get; set; }
        public string advertiserName { get; set; }
        public string brand { get; set; }
        public string id { get; set; }
        public string catalogId { get; set; }
        public string title { get; set; }
        public Price salePrice { get; set; }
        public LinkCode linkCode { get; set; }
        public string itemListName { get; set; }
        public string advertiserId { get; set; }
        public string feedName { get; set; }
        public string advertiserCountry { get; set; }
        public string customLabel0 { get; set; }
        public string customLabel1 { get; set; }
        public string customLabel2 { get; set; }
        public string customLabel3 { get; set; }
        public string customLabel4 { get; set; }


    }
    public sealed class Price
    {
        public string amount { get; set; }
        public string currency { get; set; }
    }

    //[Serializable, XmlRoot("link")]
    public sealed class LinkCode
    {
        public string clickUrl { get; set; }
        public string imageUrl { get; set; }
    }


}
