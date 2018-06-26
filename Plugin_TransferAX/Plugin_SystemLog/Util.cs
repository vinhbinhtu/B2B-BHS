using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SystemLog
{
    public static class Util
    {
        public static Entity GetConfigDefault(IOrganizationService service)
        {
            return service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_configdefault'><all-attributes /></entity></fetch>")).Entities.First();
        }
        public static bool isvalidfileld(string LogicalName, string fieldName, string Value, IOrganizationService service)
        {
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            if (LogicalName != "uom")
            {
                fetchxmm += "<condition attribute='statecode' operator='eq' value='0' />";
            }
            fetchxmm += "<condition attribute='" + fieldName + "' operator='eq' value='" + Value + "' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string retrivestringvaluelookuplike(string LogicalName, string fieldName, string Value, IOrganizationService service)
        {
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            if (LogicalName != "uom")
            {
                fetchxmm += "<condition attribute='statecode' operator='eq' value='0' />";
            }
            fetchxmm += "<condition attribute='" + fieldName + "' operator='like' value='%" + Value + "%' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }

        public static string retriveLookup(string LogicalName, string fieldName, string Value, IOrganizationService service)
        {
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            //if (LogicalName != "uom")
            //{
            //    fetchxmm += "<condition attribute='statecode' operator='eq' value='0' />";
            //}
            fetchxmm += "<condition attribute='" + fieldName + "' operator='eq' value='" + Value + "' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }
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
        public static void Throw(this object obj)
        {
            throw new Exception(obj.ToString());
        }
    }
    
}
