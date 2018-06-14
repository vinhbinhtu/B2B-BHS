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
    public class RequestDeliveryDeliveryPlanTruck : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1)
                return;
            if (myService.context.MessageName == "Delete")
            {
                EntityReference target = myService.getTargetEntityReference();
                myService.StartService();
                Entity requestdeliverydeliveryplantruck = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_deliveryplantruck"));
                Entity deliveryplantruck = new Entity("bsd_deliveryplantruck", ((EntityReference)requestdeliverydeliveryplantruck["bsd_deliveryplantruck"]).Id);
                deliveryplantruck["bsd_status"] = new OptionSetValue(861450001);
                myService.service.Update(deliveryplantruck);
            }
        }

    }
}
