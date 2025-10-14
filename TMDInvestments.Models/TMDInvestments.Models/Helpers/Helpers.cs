using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TMDInvestment.Models;

namespace TMDInvestment.Models.Helpers
{
    public static class Reflection<T>
    {
        public static dynamic GetPropertyValue(string property)
        {
            var prop = property.GetType().GetProperty(property);
            //Console.ForegroundColor = ConsoleColor.DarkYellow;
            //log.LogInformation("Name: " + prop.Name + "Value: " + prop.GetValue(property, null));
            var val = prop.GetValue(property, null);
            return val;
        }

        public static void GetAllProperties(T items)
        {
            foreach(var item in items.GetType().GetProperties())
            {
                try
                {
                    //log.LogInformation(item.Name + ": " + item.GetValue(items));
                }
                catch (Exception ex)
                {

                }                
            }
            //log.LogInformation("");
        }
        public static void GetAllPropertiesArray(List<T> itemsList)
        {
            foreach (var items in itemsList)
            {
                foreach (var item in items.GetType().GetProperties())
                {
                    //log.LogInformation(item.Name + ": " + item.GetValue(items));
                }
            }
            //log.LogInformation("");
        }

    }
}
