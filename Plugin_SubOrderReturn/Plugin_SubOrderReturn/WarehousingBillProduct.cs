using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Plugin.Service;

namespace Plugin_SubOrderReturn
{
    public class WarehousingBillProduct : IPlugin
    {
        private MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);

            if (myService.context.Depth > 1)
                return;
            myService.StartService();
            if (myService.context.MessageName == "Update")
            {
                //throw new Exception("ok");
                Entity target = myService.getTarget();
                Entity warehousingbillproduct = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                if (warehousingbillproduct.HasValue("bsd_netquantity"))
                {
                    decimal netquantity = (decimal)warehousingbillproduct["bsd_netquantity"];
                    decimal quantity = (decimal)warehousingbillproduct["bsd_quantity"];
                    decimal remainingquantity = quantity - netquantity;
                    if (remainingquantity < 0)
                        throw new Exception("net quantity is not less than quantity");
                    else
                    {
                        EntityReference warehous_ref = (EntityReference)warehousingbillproduct["bsd_warehousingbill"];
                        Entity warehousingbill = myService.service.Retrieve("bsd_warehousingbill", warehous_ref.Id, new ColumnSet(true));

                        // update status return order

                        if (warehousingbill.HasValue("bsd_returnordernew"))
                        {
                            //throw new Exception("ok");
                            Guid returnorderid = ((EntityReference)warehousingbill["bsd_returnordernew"]).Id;
                            
                            Entity updatereturnorder = new Entity("bsd_returnorder", returnorderid);
                            updatereturnorder["bsd_status"] = new OptionSetValue(861450002);        //received
                            myService.service.Update(updatereturnorder);
                        }

                        // update status warehousing bill
                        Entity updatewarehousing = new Entity("bsd_warehousingbill", warehous_ref.Id);
                        if (remainingquantity == 0)
                        {
                            updatewarehousing["bsd_status"] = new OptionSetValue(861450002);        //finish
                        }
                        else
                            updatewarehousing["bsd_status"] = new OptionSetValue(861450001);

                        myService.service.Update(updatewarehousing);

                        if (warehousingbill.HasValue("bsd_deliveryplan"))
                        {
                            EntityReference delivery_ref = (EntityReference)warehousingbill["bsd_deliveryplan"];
                            Entity delivery = myService.service.Retrieve("bsd_deliveryplan", delivery_ref.Id, new ColumnSet(true));
                            if (delivery.HasValue("bsd_suborder"))
                            {
                                EntityReference suborder_ref = (EntityReference)delivery["bsd_suborder"];
                                Entity updatesuborder = new Entity("bsd_suborder", suborder_ref.Id);
                                updatesuborder["bsd_status"] = new OptionSetValue(861450006);
                                myService.service.Update(updatesuborder);
                            }

                        }

                    }
                }
            }
        }
    }
}
