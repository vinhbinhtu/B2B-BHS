using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

namespace Plugin_SubOrderReturn
{
    public class DeliveryNote_SaleReplace : IPlugin
    {
        private MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider,true);

            if (myService.context.Depth > 1)
                return;
            
            if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();

                //throw new Exception("ok");

                Entity deliverynote = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                //throw new Exception("oka");
                int type = ((OptionSetValue)deliverynote["bsd_type"]).Value;
                
                if (type == 861450005)
                {
                    
                    if (target.Contains("bsd_confirmedreceiptdate"))
                    {
                        //throw new Exception("1");
                        UpdateReturnOrderStatus(deliverynote.Id);
                    }
                }

            }
        }
        public void UpdateReturnOrderStatus(Guid deliverynoteid)
        {
            Entity suborder = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
              <entity name='bsd_suborder'>
                <attribute name='bsd_suborderid' />
                <attribute name='bsd_name' />
                <attribute name='bsd_status' />
                <attribute name='bsd_returnorder' />
                <attribute name='createdon' />
                <order attribute='bsd_name' descending='false' />
                <link-entity name='bsd_deliveryplan' from='bsd_suborder' to='bsd_suborderid' alias='be'>
                  <link-entity name='bsd_requestdelivery' from='bsd_deliveryplan' to='bsd_deliveryplanid' alias='bf'>
                    <link-entity name='bsd_deliverynote' from='bsd_requestdelivery' to='bsd_requestdeliveryid' alias='bg'>
                      <filter type='and'>
                          <condition attribute='bsd_deliverynoteid' operator='eq' uitype='bsd_deliverynote' value='{0}' />
                      </filter>
                    </link-entity>
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>", deliverynoteid)))?.Entities?.First();
            decimal status_suborder = ((OptionSetValue)suborder["bsd_status"]).Value;
            if (status_suborder == 861450003)
            {
                if (suborder.HasValue("bsd_returnorder"))
                {
                    //throw new Exception("2");
                    EntityReference returnorder_ref = (EntityReference)suborder["bsd_returnorder"];
                    Entity updatereturnorder = new Entity("bsd_returnorder", returnorder_ref.Id);
                    updatereturnorder["bsd_statussalereplace"] = new OptionSetValue(861450002);
                    myService.service.Update(updatereturnorder);
                }
            }
        }
    }
}
