using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugin.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Plugin
{
    public class Util
    {
        public static decimal? GetFactor(IOrganizationService _service, Guid productid, Guid fromunitid, Guid tounitid)
        {
            if (fromunitid.Equals(tounitid))
            {
                return 1;
            }
            else
            {
                string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_unitconversions'>
                    <attribute name='bsd_unitconversionsid' />
                    <attribute name='bsd_tounit' />
                    <attribute name='bsd_fromunit' />
                    <attribute name='bsd_factor' />
                    <filter type='and'>
                      <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                      <condition attribute='bsd_fromunit' operator='eq' uitype='uom' value='" + fromunitid + @"' />
                      <condition attribute='bsd_tounit' operator='eq' uitype='uom' value='" + tounitid + @"' />
                    </filter>
                  </entity>
                </fetch>";
                EntityCollection list_unitconversion = _service.RetrieveMultiple(new FetchExpression(xml));
                if (list_unitconversion.Entities.Any())
                {
                    return (decimal)list_unitconversion.Entities.First()["bsd_factor"];
                }
                else
                {
                    return null;
                }
            }
        }
        public static void Test(Entity e, string[] arr, bool diaglog = false)
        {
            string s = string.Empty;
            if (arr.Count() > 0)
            {
                foreach (string item in arr)
                {
                    if (!e.HasValue(item))
                    {
                        s += ", " + item;
                    }
                }
                if (!string.IsNullOrWhiteSpace(s))
                    throw new Exception("No Hasvalue Attributes : " + s);
                else if (diaglog)
                    throw new Exception("Has Value");
            }
        }
        public static Entity GetConfigDefault(IOrganizationService service)
        {
            return service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_configdefault'><all-attributes /></entity></fetch>")).Entities.First();
        }

        public static string JSONSerialize<T>(T obj)
        {
            string retVal = String.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                serializer.WriteObject(ms, obj);
                var byteArray = ms.ToArray();
                retVal = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
            }
            return retVal;
        }
    }
}
