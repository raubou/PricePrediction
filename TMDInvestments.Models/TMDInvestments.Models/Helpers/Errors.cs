//using Microsoft.AspNetCore.Http;
//using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TMDInvestments.PricePrediction.Helpers
{
    public sealed class Errors
    {
        public static bool HasErrors(dynamic results)
        {
            bool hasErrors = false;
            //try
            //{
                //if(((JsonElement)results).GetType() != null)//results.error != null)
                if (results is JsonElement)
                {
                    hasErrors = true;
                }
                //else
                //{
                //    hasErrors = false;
                //}
            //}
            //catch (Exception ex)
            //{
            //    try
            //    {
            //        if (!string.IsNullOrEmpty(((JsonElement)results).GetProperty("error").GetString()))
            //        {
            //            hasErrors = true;
            //        }
            //        else
            //        {
            //            hasErrors = false;
            //        }
            //    }
            //    catch (Exception ex2)
            //    {
            //        hasErrors = false;
            //    }
            //}
            return hasErrors;
        }

        public static bool NotFound(dynamic results)
        {
            bool hasErrors;
            try
            {
                if (results.message != null && results.message == "NotFound" && results.message.ToLower().Contains("error"))
                {
                    hasErrors = true;
                }
                else
                {
                    hasErrors = false;
                }
            }
            catch (Exception ex)
            {
                //no errors
                hasErrors = false;
            }
            return hasErrors;
        }

    }
}
