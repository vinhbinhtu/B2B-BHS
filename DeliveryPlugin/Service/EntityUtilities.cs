using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
namespace Plugin.Service
{
    public static class EntityUtilities
    {
        public static bool ContainAndHasValue(this Entity entity, string attribute)
        {
            return entity.Contains(attribute) && entity[attribute] != null;
        }
        public static bool HasValue(this Entity entity, string attribute)
        {
            return entity.Contains(attribute) && entity[attribute] != null;
        }
        public static List<string> GettAttrs(this Entity entity)
        {
            List<string> list = new List<string>();
            entity.Attributes.ToList().ForEach(x => list.Add(x.Key));
            return list;
        }

        public static bool? GetAttrBool(this Entity entity, string attr)
        {
            return (bool?)entity[attr];
        }

        public static void Throw(this object o)
        {
            throw new Exception(o.ToString());
        }
        public static string ToCrmFormat(this DateTime date)
        {
            return date.Year + "-" + (date.Month < 10 ? "0" + date.Month : date.Month.ToString()) + "-" + (date.Day < 10 ? "0" + date.Day : date.Day.ToString()) + " " + date.Hour + ":" + date.Minute + ":" + date.Second;
        }
        public static string DecimalToStringHideSymbol(this decimal dec)
        {
            string res = String.Format("{0:0,0.00đ}", dec);
            res = res.Replace(',', '*');
            res = res.Replace('.', ',');
            res = res.Replace('*', '.');
            return res;
        }
    }
}
