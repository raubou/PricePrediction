using MongoDB.Bson;
using System;
using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;

namespace TMDInvestments.Models
{
    //[Serializable]
    public class RssModel
    {
        //public string _id {get; set;}
        public ObjectId _id { get; set; }
        //[Required]
        public int source { get; set; }
        //[Required]
        public string title { get; set; }
        //[Required]
        public int totalFound { get; set; }
        //[Required]
        public List<RssModelItems> items { get; set; }// = new List<RssModelItems>();
    }
}
