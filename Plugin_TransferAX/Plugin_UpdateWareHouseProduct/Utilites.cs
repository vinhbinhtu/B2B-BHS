using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;
namespace Plugin_UpdateWareHouseProduct
{
    public static class Utilites
    {
        public static bool HasValue(this Entity en, string attr)
        {
            return en.Contains(attr) && en[attr] != null;
        }
        public static EntityCollection RetrieveOneCondition(string localname, string attribute, object value, IOrganizationService service)
        {
            QueryExpression q = new QueryExpression(localname);
            q.ColumnSet = new ColumnSet(true);
            FilterExpression filter = new FilterExpression();
            filter.AddCondition(new ConditionExpression(attribute, ConditionOperator.Equal, value));
            if (localname != "productpricelevel") filter.AddCondition(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            q.Criteria = filter;
            return service.RetrieveMultiple(q);
        }
    }
}
