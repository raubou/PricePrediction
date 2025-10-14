using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace TMDInvestments.Models
{
    public static class Extensions

    {
        public static DataTable CreateDataTableItem(this List<RssModelItems> ItemList)
        {
            DataTable dt = new DataTable();
            try
            {
                dt.TableName = "RssModelItems";
                foreach (PropertyInfo property in typeof(ListItem).GetProperties())
                {
                    dt.Columns.Add(new DataColumn() { ColumnName = property.Name, DataType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType, AllowDBNull = true });
                }

                foreach (var item in ItemList)
                {
                    DataRow newRow = dt.NewRow();
                    newRow["Id"] = Guid.NewGuid();
                    foreach (PropertyInfo property in typeof(RssModelItems).GetProperties())
                    {
                        if(property.Name != "NewsId" && property.Name != "_id" && property.Name != "_source" && property.Name != "AdvertiserId" && property.Name != "IsActiveAd" && property.Name != "IsInAdvertismentSpace")
                        {
                            newRow[property.Name] = item.GetType().GetProperty(property.Name)?.GetValue(item, null) ?? DBNull.Value;
                        }
                    }
                    dt.Rows.Add(newRow);
                }
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public class ListItem
        {
            public Guid Id { get; set; }
            public string Source { get; set; }
            public string Title { get; set; }
            public string Link { get; set; }
            public decimal Price { get; set; }
            public string Image { get; set; }
            public byte[] ImageData { get; set; }
            public DateTime PubDate { get; set; }
            public string Content { get; set; }
            public string Advertiser { get; set; }
            public string Category { get; set; }
            public string Banner { get; set; }
            public DateTime DateCreated { get; set; }

        }
    }
}
