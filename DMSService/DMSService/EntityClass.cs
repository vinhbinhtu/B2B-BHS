using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DMSService
{
    public class EntityClass
    {
      
        public class fieldentity
        {
            public string fieldname { get; set; }
            public string type { get; set; }
            public string value { get; set; }
            public string entityReferenceName { get; set; }
        }
        public class EntityS
        {
            public string entity { get; set; }
            public List<fieldentity> fields { get; set; }
        }
        public class RootJson
        {
            public string typeAction { get; set; }
            public EntityS entity { get; set; }
        }
    }
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
    }
}