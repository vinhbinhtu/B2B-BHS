using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DeliveryPlugin.Service;

namespace DeliveryPlugin
{
    public class SplitRequestDeliveryProduct : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1)
                return;
            if (myService.context.MessageName == "Create" || myService.context.MessageName == "Update") throw new Exception("ERROR");


            if (myService.context.MessageName == "bsd_Action_SplitRequestDeliveryProduct")
            {
                EntityReference target = myService.getTargetEntityReference();
                myService.StartService();
                RequestDeliveryService requestDeliveryService = new RequestDeliveryService();
                Entity requestdeliveryproduct = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                string WarehouseConsignment = myService.context.InputParameters["WarehouseConsignment"].ToString();
                // throw new Exception("WarehouseConsignment:" +Guid.Parse(WarehouseConsignment));
                Guid warehouseid = Guid.Parse(myService.context.InputParameters["Warehouse"].ToString());
                //Guid warehouse_old_id = requestdeliveryproduct.HasValue("bsd_warehouse") ? ((EntityReference)requestdeliveryproduct["bsd_warehouse"]).Id : Guid.Empty;
                decimal split_quantity = (decimal)myService.context.InputParameters["SplitQuantity"];

                decimal old_quantity = 0m;
                decimal change_quantity = 0m;
                bool increase = true;

                var existing = Get_RequestDeliveryProduct_WithWarehouese(requestdeliveryproduct, warehouseid);
                if (existing != null)
                {
                    old_quantity = (decimal)existing["bsd_quantity"];
                    change_quantity = (split_quantity - old_quantity);
                    increase = (split_quantity - old_quantity) > 0;
                }
                else
                {
                    change_quantity = split_quantity;
                    increase = true;
                }

                change_quantity = increase ? change_quantity : (change_quantity * -1);
                if (existing != null)
                {
                    if (split_quantity > 0)
                    {
                        Entity new_existing = new Entity(existing.LogicalName, existing.Id);
                        new_existing["bsd_quantity"] = split_quantity;
                        new_existing["bsd_remainingquantity"] = split_quantity;
                       
                        if (!string.IsNullOrEmpty(WarehouseConsignment))
                        {
                            new_existing["bsd_warehouseconsignment"] = new EntityReference("bsd_warehouseentity", Guid.Parse(WarehouseConsignment));
                        }
                        new_existing["bsd_warehousestatus"] = true;
                        myService.Update(new_existing);
                    }
                    else
                    {
                        myService.service.Delete(existing.LogicalName, existing.Id);
                    }
                }
                else
                {
                    if (split_quantity > 0)
                    {
                        Entity new_requestdeliveryproduct = new Entity(target.LogicalName);
                        foreach (var attr in requestdeliveryproduct.Attributes)
                        {
                            new_requestdeliveryproduct[attr.Key] = requestdeliveryproduct[attr.Key];
                        }
                        new_requestdeliveryproduct.Attributes.Remove("createon");
                        new_requestdeliveryproduct.Attributes.Remove("bsd_requestdeliveryproductid");
                        new_requestdeliveryproduct["bsd_quantity"] = split_quantity;
                        new_requestdeliveryproduct["bsd_remainingquantity"] = split_quantity;
                        new_requestdeliveryproduct["bsd_warehouse"] = new EntityReference("bsd_warehouseentity", warehouseid);
                        if (!string.IsNullOrEmpty(WarehouseConsignment))
                            new_requestdeliveryproduct["bsd_warehouseconsignment"] = new EntityReference("bsd_warehouseentity", Guid.Parse(WarehouseConsignment));
                        new_requestdeliveryproduct["bsd_warehousestatus"] = true;
                        myService.service.Create(new_requestdeliveryproduct);
                    }
                }

                #region Tạo và cập nhật kho rỗng
                Entity requestdeliveryproduct_emptywarehouse = Get_RequestDeliveryProduct_WithWarehouese(requestdeliveryproduct, null); // kho rỗng
                if (requestdeliveryproduct_emptywarehouse != null) // có kho trống, thì update.
                {
                    Entity new_requestdeliveryproduct_emptywarehouse = new Entity("bsd_requestdeliveryproduct", requestdeliveryproduct_emptywarehouse.Id);
                    decimal emptywarehouse_quantity = (decimal)requestdeliveryproduct_emptywarehouse["bsd_quantity"];
                    decimal new_quantity = 0m;

                    if (increase == true) // tăng trong request mới, giảm trong kho.
                    {
                        new_quantity = emptywarehouse_quantity - change_quantity;
                    }
                    else
                    {
                        new_quantity = emptywarehouse_quantity + change_quantity;
                    }

                    if (new_quantity > 0)
                    {
                        new_requestdeliveryproduct_emptywarehouse["bsd_quantity"] = new_quantity;
                        new_requestdeliveryproduct_emptywarehouse["bsd_remainingquantity"] = new_quantity;
                        myService.service.Update(new_requestdeliveryproduct_emptywarehouse);
                    }
                    else
                    {
                        myService.service.Delete(new_requestdeliveryproduct_emptywarehouse.LogicalName, new_requestdeliveryproduct_emptywarehouse.Id);
                    }
                }
                else
                {
                    if (change_quantity > 0)
                        Create_New_EmptyWarehouse_For_RequestDelivery(requestdeliveryproduct, change_quantity);
                }
                #endregion

                #region cập nhật lại tình trạng của tổng request
                requestDeliveryService.UpdateWarehouseStatusRequest(((EntityReference)requestdeliveryproduct["bsd_requestdelivery"]).Id, myService.service);
                #endregion

                #region Kiểm tra xem số lượng đã split+jion có vượt quá số lượng trong quantity detail hay không !
                EntityReference product_ref = (EntityReference)requestdeliveryproduct["bsd_product"];
                decimal splited_quantity = 0m;
                decimal quantitydetail = 0m;
                bool freeitem = requestdeliveryproduct.HasValue("bsd_freeitem") ? (bool)requestdeliveryproduct["bsd_freeitem"] : false;

                // sum lại danh sách product đã tách và gộp
                EntityCollection list_splited = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_requestdeliveryproduct'>
                    <attribute name='bsd_requestdeliveryproductid'/>
                    <attribute name='bsd_quantity'/>
                    <filter type='and'>
                      <condition attribute='bsd_requestdelivery' operator='eq' uiname='' uitype='bsd_requestdelivery' value='" + ((EntityReference)requestdeliveryproduct["bsd_requestdelivery"]).Id + @"' />
                      <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product_ref.Id + @"' />
                      <condition attribute='bsd_freeitem' operator='eq' value='" + freeitem + @"' />
                    </filter>
                  </entity>
                </fetch>");
                //throw new Exception(list_splited.Entities.Count.ToString());
                // list gốc, lúc chưa gộp chưa tách.
                EntityCollection list_quantitydetail = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_requestdeliveryquantitydetail'>
                    <attribute name='bsd_requestdeliveryquantitydetailid'/>
                    <attribute name='bsd_quantity'/>
                    <filter type='and'>
                      <condition attribute='bsd_requestdelivery' operator='eq' uiname='' uitype='bsd_requestdelivery' value='" + ((EntityReference)requestdeliveryproduct["bsd_requestdelivery"]).Id + @"' />
                      <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product_ref.Id + @"' />
                      <condition attribute='bsd_freeitem' operator='eq' value='" + freeitem + @"' />
                    </filter>
                  </entity>
                </fetch>");

                if (list_splited.Entities.Any())
                {
                    foreach (var item in list_splited.Entities)
                    {
                        splited_quantity += (decimal)item["bsd_quantity"];
                    }
                }
                if (list_quantitydetail.Entities.Any())
                {
                    foreach (var item in list_quantitydetail.Entities)
                    {
                        quantitydetail += (decimal)item["bsd_quantity"];
                    }
                }

                if (splited_quantity > quantitydetail)
                {
                    throw new Exception("Vượt quá số lượng !");
                }

                #endregion

            }

        }
        public void Create_New_EmptyWarehouse_For_RequestDelivery(Entity requestdeliveryproduct, decimal quantity)
        {
            Entity new_requestdeliveryproduct = new Entity(requestdeliveryproduct.LogicalName);
            foreach (var attr in requestdeliveryproduct.Attributes)
            {
                new_requestdeliveryproduct[attr.Key] = requestdeliveryproduct[attr.Key];
            }
            new_requestdeliveryproduct.Attributes.Remove("createon");
            new_requestdeliveryproduct.Attributes.Remove("bsd_requestdeliveryproductid");
            new_requestdeliveryproduct["bsd_quantity"] = quantity;
            new_requestdeliveryproduct["bsd_remainingquantity"] = quantity;
            new_requestdeliveryproduct["bsd_warehousestatus"] = false;
            new_requestdeliveryproduct.Attributes.Remove("bsd_warehouse");
            myService.service.Create(new_requestdeliveryproduct);
        }
        public Entity Get_RequestDeliveryProduct_WithWarehouese(Entity requestdeliveryproduct, Guid? warehouseid)
        {
            EntityCollection list_existing = null;
            EntityReference product_ref = (EntityReference)requestdeliveryproduct["bsd_product"];
            EntityReference requestdelivery_ref = (EntityReference)requestdeliveryproduct["bsd_requestdelivery"];
            bool freeitem = (bool)requestdeliveryproduct["bsd_freeitem"];
            if (warehouseid.HasValue)
            {
                list_existing = myService.FetchXml(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_requestdeliveryproduct'>
                            <attribute name='bsd_requestdeliveryproductid' />
                            <attribute name='bsd_warehouse' />
                            <attribute name='bsd_product' />
                            <attribute name='bsd_quantity' />
                            <filter type='and'>
                              <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + requestdelivery_ref.Id + @"' />
                              <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='" + warehouseid + @"' />
                              <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product_ref.Id + @"' />
                              <condition attribute='bsd_freeitem' operator='eq' value='" + freeitem + @"' />
                            </filter>
                          </entity>
                        </fetch>"));
                if (list_existing.Entities.Any())
                    return list_existing.Entities.First();
                return null;
            }
            else
            {
                list_existing = myService.FetchXml(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_requestdeliveryproduct'>
                            <attribute name='bsd_requestdeliveryproductid' />
                            <attribute name='bsd_warehouse' />
                            <attribute name='bsd_product' />
                            <attribute name='bsd_quantity' />
                            <filter type='and'>
                              <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + requestdelivery_ref.Id + @"' />
                              <condition attribute='bsd_warehouse' operator='null' />
                              <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product_ref.Id + @"' />
                              <condition attribute='bsd_freeitem' operator='eq' value='" + freeitem + @"' />
                            </filter>
                          </entity>
                        </fetch>"));
            }
            if (list_existing.Entities.Any())
                return list_existing.Entities.First();
            return null;
        }
    }
}
