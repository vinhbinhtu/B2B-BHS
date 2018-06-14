using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DeliveryPlugin
{
    public class ExChangeRate : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1)
                return;

            if (myService.context.MessageName == "Create")
            {
                Entity target = myService.getTarget();
                myService.StartService();
                DateTime date = myService.RetrieveLocalTimeFromUTCTime((DateTime)target["bsd_date"], myService.service);
                EntityCollection list_exchangerate = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"
                    <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_exchangerate'>
                        <attribute name='bsd_exchangerateid' />
                        <attribute name='bsd_name' />
                        <filter type='and'>
                          <condition attribute='bsd_currencyfrom' operator='eq' uitype='transactioncurrency' value='{0}' />
                          <condition attribute='bsd_currencyto' operator='eq' uitype='transactioncurrency' value='{1}' />
                          <condition attribute='bsd_bankaccount' operator='eq' uitype='bsd_bankgroup' value='{2}' />
                          <condition attribute='bsd_date' operator='on' value='{3}' />
                        </filter>
                      </entity>
                    </fetch>", ((EntityReference)target["bsd_currencyfrom"]).Id, ((EntityReference)target["bsd_currencyto"]).Id, ((EntityReference)target["bsd_bankaccount"]).Id, date)));
                if (list_exchangerate.Entities.Count > 0)
                {
                    throw new Exception("Đã tồn tại!");
                }

            }
            else if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();
                myService.StartService();

                if (target.Contains("bsd_date") || target.Contains("bsd_currencyfrom") || target.Contains("bsd_currencyto") || target.Contains("bsd_bankaccount"))
                {

                    Entity exchangerate = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    DateTime? date = null;
                    EntityReference bsd_currencyfrom = null;
                    EntityReference bsd_currencyto = null;
                    EntityReference bsd_bankaccount = null;

                    if (!target.Contains("bsd_date"))
                    {
                        date = myService.RetrieveLocalTimeFromUTCTime((DateTime)exchangerate["bsd_date"], myService.service);
                    }
                    else
                    {
                        date = myService.RetrieveLocalTimeFromUTCTime((DateTime)target["bsd_date"], myService.service);
                    }

                    if (!target.Contains("bsd_currencyfrom"))
                    {
                        bsd_currencyfrom = (EntityReference)exchangerate["bsd_currencyfrom"];
                    }
                    else
                    {
                        bsd_currencyfrom = (EntityReference)target["bsd_currencyfrom"];
                    }

                    if (!target.Contains("bsd_currencyto"))
                    {
                        bsd_currencyto = (EntityReference)exchangerate["bsd_currencyto"];
                    }
                    else
                    {
                        bsd_currencyto = (EntityReference)target["bsd_currencyto"];
                    }

                    if (!target.Contains("bsd_bankaccount"))
                    {
                        bsd_bankaccount = (EntityReference)exchangerate["bsd_bankaccount"];
                    }
                    else
                    {
                        bsd_bankaccount = (EntityReference)target["bsd_bankaccount"];
                    }
                    EntityCollection list_exchangerate = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"
                        <fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_exchangerate'>
                            <attribute name='bsd_exchangerateid' />
                            <attribute name='bsd_name' />
                            <filter type='and'>
                              <condition attribute='bsd_currencyfrom' operator='eq' uitype='transactioncurrency' value='{0}' />
                              <condition attribute='bsd_currencyto' operator='eq' uitype='transactioncurrency' value='{1}' />
                              <condition attribute='bsd_bankaccount' operator='eq' uitype='bsd_bankgroup' value='{2}' />
                              <condition attribute='bsd_date' operator='on' value='{3}' />
                            </filter>
                          </entity>
                        </fetch>", bsd_currencyfrom.Id, bsd_currencyto.Id, bsd_bankaccount.Id, date)));

                    if (list_exchangerate.Entities.Count > 0)
                    {
                        throw new Exception("Đã tồn tại!");
                    }

                }
            }
            else if (myService.context.MessageName == "Delete")
            {
            }




        }
    }
}
