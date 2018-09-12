using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;
namespace Plugin_CloseSubOrder
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
        public static string _userName = "";
        public static string _passWord = "";
        public static string _company = "";
        public static string _port = "";
        public static string _domain = "";
        //public static string _userName = "s.ttctech";
        //public static string _passWord = "AX@tct2017";
        //public static string _company = "102";
        //public static string _port = "10.33.21.1:8201";
        //public static string _domain = "SUG.TTCG.LAN";
    }
}
