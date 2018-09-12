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
    }
}
