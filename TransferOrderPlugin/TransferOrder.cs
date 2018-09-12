using Microsoft.Xrm.Sdk;
using Plugin.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransferOrderPlugin
{
    public class TransferOrder : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1)
                return;

            if (myService.context.MessageName == "Create")
            {
                myService.StartService();
                Entity target = myService.getTarget();
                // PO / 861450001
                //TO / 861450000
                int bsd_type = 861450000;
                if (target.HasValue("bsd_type"))
                {
                    bsd_type =((OptionSetValue)target["bsd_type"]).Value;
                }
                if (bsd_type == 861450000)
                {
                    EntityReference ref_fromsite = (EntityReference)target["bsd_fromsite"];
                    EntityReference ref_tosite = (EntityReference)target["bsd_tosite"];
                    EntityReference ref_fromwarehouse = (EntityReference)target["bsd_fromwarehouse"];
                    EntityReference ref_towarehouse = (EntityReference)target["bsd_towarehouse"];
                    EntityReference ref_carrierpartner = (EntityReference)target["bsd_carrierpartner"];
                    EntityReference ref_unitshipping = getUnitShipping_Configdefault();
                    int deliverymethod = ((OptionSetValue)target["bsd_deliverymethod"]).Value;
                    bool port = target.HasValue("bsd_porter") ? (bool)target["bsd_porter"] : false;
                    DateTime date = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                }

              

            }
        }

        public EntityReference getUnitShipping_Configdefault()
        {
            Entity configdefault = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                      <entity name='bsd_configdefault'>
                                        <attribute name='bsd_configdefaultid' />
                                        <attribute name='createdon' />
                                        <attribute name='bsd_unitshippingdefault' />
                                        <order attribute='createdon' descending='true' />
                                      </entity>
                                    </fetch>").Entities.FirstOrDefault();
            EntityReference ref_unitshipping = (EntityReference)configdefault["bsd_unitshippingdefault"];
            return ref_unitshipping;
        }

    }
}
