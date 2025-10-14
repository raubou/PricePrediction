using MongoDB.Bson;
using System;
using System.ComponentModel.DataAnnotations;

namespace TMDInvestments.Models
{
    //[Serializable]
    public sealed class RssModelItems
    {
        public string NewsId { get; set; } 
        public ObjectId _id { get; set; }
        public string _source { get; set; }
        //[Required]
        public string title { get; set; }
        //[Required]
        public string link { get; set; }
        //[Required]
        public string image { get; set; }
        public byte[]? ImageData { get; set; }
        //[Required]
        public string pubDate { get; set; }
        //[Required]
        public string price { get; set; }
        //[Required]
        public string content { get; set; }
        //[Required]
        public string source { get; set; }
        //[Required]
        public string AdvertiserId { get; set; }
        //[Required]
        public string Advertiser { get; set; }
        //[Required]
        public string Category { get; set; }
        //[Required]
        public string Banner { get; set; }

        public bool IsActiveAd { get; set; } = false;

        public bool IsInAdvertismentSpace { get; set; } = false;
        //[Required]
        public DateTime DateCreated { get; set; }
    }
}
