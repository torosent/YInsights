using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YInsights.Web.Extentions
{
    public static class ConvertExtentions
    {
        public static DateTime UnixTimeStampToDateTime( double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source != null && toCheck != null && source.IndexOf(toCheck, comp) >= 0;
        }
        public static bool Contains(this IEnumerable<string> source, string toCheck, StringComparison comp)
        {
            if (source == null || toCheck == null)
                return false;
            foreach (var item in source)
            {
                if (item.IndexOf(toCheck, comp) >= 0)
                {
                    return true;

                }

            }

            return false;
        }
    }
}
