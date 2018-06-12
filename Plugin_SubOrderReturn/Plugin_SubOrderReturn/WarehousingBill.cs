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
    public class WarehousingBill : IPlugin
    {
        private MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);

            if (myService.context.Depth > 1)
                return;
            
            if (myService.context.MessageName== "bsd_Action_CreateWarehousingBill")
            {
                myService.StartService();
                //throw new Exception("ok");

                EntityReference target = myService.getTargetEntityReference();
                Entity deliveryplan = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

                Entity warehousingbill = new Entity("bsd_warehousingbill");

                EntityReference subref = (EntityReference)deliveryplan["bsd_suborder"];
                Entity suborder = myService.service.Retrieve(subref.LogicalName, subref.Id, new ColumnSet(true));

                string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_deliveryplanproduct'>
                                            <all-attributes />
                                            <filter type='and'>
                                              <condition attribute='bsd_deliveryplan' operator='eq' uitype='bsd_deliveryplan' value='{0}' />
                                            </filter>
                                          </entity>
                                        </fetch>", target.Id);
                EntityCollection list_orderproduct = myService.service.RetrieveMultiple(new FetchExpression(xml));

                #region Tao warehousingbill 
                myService.SetState(target.Id, target.LogicalName, 0, 1);
                //throw new Exception("1");
                warehousingbill["bsd_date"] = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                warehousingbill["bsd_deliveryplan"] = new EntityReference(deliveryplan.LogicalName, deliveryplan.Id);
                warehousingbill["bsd_customer"] = deliveryplan["bsd_potentialcustomer"];
                if (deliveryplan.HasValue("bsd_addresscustomeraccount")) warehousingbill["bsd_customeraddress"] = deliveryplan["bsd_addresscustomeraccount"];
                //throw new Exception("2");
                if (suborder.HasValue("bsd_warehouseaddress")) warehousingbill["bsd_warehouseaddress"] = suborder["bsd_warehouseaddress"];        ////
                if (suborder.HasValue("bsd_returnorder")) {
                    //throw new Exception("ok");
                    warehousingbill["bsd_returnordernew"] = suborder["bsd_returnorder"];
                }
                if (deliveryplan.HasValue("bsd_warehousefrom"))
                {
                    warehousingbill["bsd_warehouse"] = deliveryplan["bsd_warehousefrom"];
                    Entity warehouse = myService.service.Retrieve("bsd_warehouseentity", ((EntityReference)deliveryplan["bsd_warehousefrom"]).Id, new ColumnSet(true));
                    warehousingbill["bsd_site"] = warehouse["bsd_site"];
                }
                Guid warehousing_id = myService.service.Create(warehousingbill);

                #endregion

                #region "Tao Suborder product"
                foreach (var orderproduct in list_orderproduct.Entities)
                {
                    Entity product = myService.service.Retrieve("product", ((EntityReference)orderproduct["bsd_product"]).Id, new ColumnSet(true));
                    //throw new Exception("6");

                    #region tao sub product
                    Entity warehousing_product = new Entity("bsd_warehousingbillproduct");
                    warehousing_product["bsd_name"] = product["name"];

                    warehousing_product["bsd_warehousingbill"] = new EntityReference("bsd_warehousingbill", warehousing_id);
                    warehousing_product["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                    warehousing_product["bsd_quantity"] = (-1) * (decimal)orderproduct["bsd_shipquantity"];
                    warehousing_product["bsd_remainingquantity"] = (-1) * (decimal)orderproduct["bsd_shipquantity"];
                    warehousing_product["bsd_netquantity"] = 0m;
                    warehousing_product["bsd_unit"] = orderproduct["bsd_unit"];

                    myService.service.Create(warehousing_product);
                    #endregion

                }
                #endregion

                myService.context.OutputParameters["ReturnId"] = warehousing_id.ToString();

            }
        }
    }
}
