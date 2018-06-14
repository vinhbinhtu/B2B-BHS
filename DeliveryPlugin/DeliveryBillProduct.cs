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
    public class DeliveryBillProduct : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {

            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 2) return;

            if (myService.context.MessageName == "Update")
            {
                myService.StartService();
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_netquantity"))
                {
                    Entity preimage = myService.context.PreEntityImages["PreImage"];
                    GoodsIssueNote_Manager goodsIssueNote_Manager = new GoodsIssueNote_Manager(myService.service);

                    #region Lấy delivery bill dettail (target) + lấy warehouse ở delivery bill.
                    Entity deliverybilldetail = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_deliveryproductbill'>
                        <attribute name='bsd_deliveryproductbillid' />
                        <attribute name='bsd_requestdelivery' />
                        <attribute name='bsd_product' />
                        <filter type='and'>
                          <condition attribute='bsd_deliveryproductbillid' operator='eq' uitype='bsd_deliveryproductbill' value='{0}' />
                        </filter>
                        <link-entity name='bsd_deliverybill' from='bsd_deliverybillid' to='bsd_deliverybill' alias='ad'>
                          <attribute name='bsd_warehouse' />
                          <attribute name='bsd_deliveryplan' />
                        </link-entity>
                      </entity>
                    </fetch>", target.Id)))?.Entities?.First();

                    EntityReference warehouse_ref = (EntityReference)((AliasedValue)deliverybilldetail["ad.bsd_warehouse"]).Value;
                    EntityReference requestdelivery_ref = (EntityReference)deliverybilldetail["bsd_requestdelivery"];
                    EntityReference product_ref = (EntityReference)deliverybilldetail["bsd_product"];
                    #endregion

                    #region Lấy request delivery product để cập nhật.
                    Entity requestdeliveryproduct = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_requestdeliveryproduct'>
                        <attribute name='bsd_requestdeliveryproductid' />
                        <attribute name='bsd_netquantity' />
                        <attribute name='bsd_quantity' />
                        <attribute name='bsd_requestdelivery' />
                        <filter type='and'>
                          <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='{0}' />
                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                          <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='{2}' />
                        </filter>
                      </entity>
                    </fetch>", warehouse_ref.Id, product_ref.Id, requestdelivery_ref.Id)))?.Entities?.First();
                    if (requestdeliveryproduct == null) throw new Exception("requestdeliveryproduct null");

                    EntityReference request_ref = (EntityReference)requestdeliveryproduct["bsd_requestdelivery"];

                    decimal requestdeliveryproduct_netquantity = (decimal)requestdeliveryproduct["bsd_netquantity"];
                    decimal request_quantity = (decimal)requestdeliveryproduct["bsd_quantity"];
                    decimal old_netquantity = (decimal)preimage["bsd_netquantity"];
                    decimal new_quantity = (decimal)target["bsd_netquantity"];
                    Entity new_requestdeliveryproduct = new Entity(requestdeliveryproduct.LogicalName, requestdeliveryproduct.Id);
                    new_requestdeliveryproduct["bsd_netquantity"] = requestdeliveryproduct_netquantity - old_netquantity + new_quantity;
                    new_requestdeliveryproduct["bsd_remainingquantity"] = request_quantity - (requestdeliveryproduct_netquantity - old_netquantity + new_quantity);
                    myService.service.Update(new_requestdeliveryproduct);
                    #endregion

                    #region UpdateDeliveryScheduleProduct
                    EntityReference deliverySchedule_ref = (EntityReference)((AliasedValue)deliverybilldetail["ad.bsd_deliveryplan"]).Value;
                    Entity request = myService.service.Retrieve(request_ref.LogicalName, request_ref.Id, new ColumnSet(true));
                    goodsIssueNote_Manager.UpdateDeliveryScheduleProduct(request, deliverySchedule_ref.Id, product_ref.Id, old_netquantity, new_quantity);
                    #endregion
                }
            }
            else if (myService.context.MessageName == "Delete")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
               
                GoodsIssueNote_Manager goodsIssueNote_Manager = new GoodsIssueNote_Manager(myService.service);

                #region Lấy delivery bill dettail (target) + lấy warehouse ở delivery bill.
                Entity deliverybilldetail = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_deliveryproductbill'>
                        <attribute name='bsd_deliveryproductbillid' />
                        <attribute name='bsd_requestdelivery' />
                        <attribute name='bsd_product' />
                        <attribute name='bsd_netquantity' />
                        <filter type='and'>
                          <condition attribute='bsd_deliveryproductbillid' operator='eq' uitype='bsd_deliveryproductbill' value='{0}' />
                        </filter>
                        <link-entity name='bsd_deliverybill' from='bsd_deliverybillid' to='bsd_deliverybill' alias='ad'>
                          <attribute name='bsd_warehouse' />
                          <attribute name='bsd_deliveryplan' />
                        </link-entity>
                      </entity>
                    </fetch>", target.Id)))?.Entities?.First();
                EntityReference warehouse_ref = (EntityReference)((AliasedValue)deliverybilldetail["ad.bsd_warehouse"]).Value;
                EntityReference requestdelivery_ref = (EntityReference)deliverybilldetail["bsd_requestdelivery"];
                EntityReference product_ref = (EntityReference)deliverybilldetail["bsd_product"];

                #endregion
              
                #region Lấy request delivery product để cập nhật.
                Entity requestdeliveryproduct = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_requestdeliveryproduct'>
                        <attribute name='bsd_requestdeliveryproductid' />
                        <attribute name='bsd_netquantity' />
                        <attribute name='bsd_quantity' />
                        <attribute name='bsd_requestdelivery' />
                        <filter type='and'>
                          <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='{0}' />
                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                          <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='{2}' />
                        </filter>
                      </entity>
                    </fetch>", warehouse_ref.Id, product_ref.Id, requestdelivery_ref.Id)))?.Entities?.First();

                EntityReference request_ref = (EntityReference)requestdeliveryproduct["bsd_requestdelivery"];

                decimal requestdeliveryproduct_netquantity = (decimal)requestdeliveryproduct["bsd_netquantity"];
                decimal delete_quantity = (decimal)deliverybilldetail["bsd_netquantity"];
                Entity new_requestdeliveryproduct = new Entity(requestdeliveryproduct.LogicalName, requestdeliveryproduct.Id);
                decimal request_quantity = (decimal)requestdeliveryproduct["bsd_quantity"];
                decimal net_quantity = requestdeliveryproduct_netquantity - delete_quantity;
                new_requestdeliveryproduct["bsd_netquantity"] = net_quantity;
                new_requestdeliveryproduct["bsd_remainingquantity"] = request_quantity - net_quantity;
                myService.service.Update(new_requestdeliveryproduct);
                #endregion

                #region UpdateDeliveryScheduleProduct
                EntityReference deliverySchedule_ref = (EntityReference)((AliasedValue)deliverybilldetail["ad.bsd_deliveryplan"]).Value;
                Entity request = myService.service.Retrieve(request_ref.LogicalName, request_ref.Id, new ColumnSet(true));
                goodsIssueNote_Manager.UpdateDeliveryScheduleProduct(request, deliverySchedule_ref.Id, product_ref.Id, delete_quantity, 0);
                #endregion
            }
        }
    }
}
