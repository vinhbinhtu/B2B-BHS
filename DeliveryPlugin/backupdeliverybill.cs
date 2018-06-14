//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Plugin.Service;
//using Microsoft.Xrm.Sdk;
//using Microsoft.Xrm.Sdk.Query;
//namespace DeliveryPlugin
//{
//    public class DeliveryBill2 : IPlugin
//    {
//        MyService myService;
//        public void Execute(IServiceProvider serviceProvider)
//        {
//            myService = new MyService(serviceProvider);

//            #region bsd_Action_CreateDeliveryBill_1Request
//            if (myService.context.MessageName == "bsd_Action_CreateDeliveryBill_1Request")
//            {
//                if (myService.context.Depth > 1) return;
//                myService.StartService();
//                GoodsIssueNote_Manager goodsIssueNote_Manager = new GoodsIssueNote_Manager(myService.service);
//                EntityReference target = myService.getTargetEntityReference();

//                Entity request = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
//                EntityReference DeliverySchedule_Ref = (EntityReference)request["bsd_deliveryplan"];
//                if ((bool)request["bsd_warehousestatus"] == false) "No quantity to create Goods Issue Note !".Throw();

//                // Danh sách request product còn dư số lượng.
//                EntityCollection list_requestdeliveryproduct = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
//                  <entity name='bsd_requestdeliveryproduct'>
//                    <attribute name='bsd_requestdeliveryproductid' />
//                    <attribute name='bsd_warehousestatus' />
//                    <attribute name='bsd_warehousequantity' />
//                    <attribute name='bsd_warehouse' />
//                    <attribute name='bsd_uomid' />
//                    <attribute name='bsd_totalquantity' />
//                    <attribute name='bsd_requestedquantity' />
//                    <attribute name='bsd_requestdelivery' />
//                    <attribute name='bsd_remainingquantity' />
//                    <attribute name='overriddencreatedon' />
//                    <attribute name='bsd_quantity' />
//                    <attribute name='bsd_product' />
//                    <attribute name='bsd_netquantity' />
//                    <attribute name='bsd_name' />
//                    <attribute name='bsd_productid' />
//                    <attribute name='bsd_freeitem' />
//                    <attribute name='bsd_descriptionproduct' />
//                    <filter type='and'>
//                      <condition attribute='bsd_requestdelivery' operator='eq' value='" + request.Id + @"' />
//                      <condition attribute='bsd_remainingquantity' operator='gt' value='0' />
//                    </filter>
//                  </entity>
//                </fetch>");

//                if (list_requestdeliveryproduct.Entities.Any())
//                {
//                    // Distinct theo warehouse, là 1 danh sách request product có warehouse không trùng nhau.
//                    var list_distinct = list_requestdeliveryproduct.Entities.GroupBy(i => ((EntityReference)i["bsd_warehouse"]).Id, (key, group) => group.First()).ToList();
//                    // lọc qua từng thằng này để tìm những cái request product có cùng sản phẩm và warehouse thì tạo 1 phiếu xuất kho
//                    foreach (var distinct_item in list_distinct)
//                    {
//                        // danh sach request product. nhung line nao co warehouse o tren.
//                        EntityCollection list_requestdeliveryproduct_sub = new EntityCollection();
//                        foreach (var requestdeliveryproduct in list_requestdeliveryproduct.Entities)
//                            if (((EntityReference)distinct_item["bsd_warehouse"]).Id.Equals(((EntityReference)requestdeliveryproduct["bsd_warehouse"]).Id))
//                                list_requestdeliveryproduct_sub.Entities.Add(requestdeliveryproduct);


//                        Entity deliverybill = new Entity("bsd_deliverybill");
//                        EntityReference deliveryplan_ref = (EntityReference)request["bsd_deliveryplan"];
//                        Entity deliveryplan = myService.service.Retrieve(deliveryplan_ref.LogicalName, deliveryplan_ref.Id, new ColumnSet(true));
//                        deliverybill["bsd_requestdelivery"] = new EntityReference(request.LogicalName, request.Id);
//                        deliverybill["bsd_deliveryplan"] = request["bsd_deliveryplan"];
//                        if (request.HasValue("bsd_order"))
//                        {
//                            deliverybill["bsd_order"] = request["bsd_order"];
//                        }
//                        else if (request.HasValue("bsd_quote"))
//                        {
//                            deliverybill["bsd_quote"] = request["bsd_quote"];
//                        }
//                        deliverybill["bsd_customer"] = request["bsd_account"];
//                        deliverybill["bsd_shiptoaddress"] = request["bsd_shiptoaddress"];
//                        deliverybill["bsd_requestedshipdate"] = deliveryplan["bsd_requestedshipdate"];
//                        deliverybill["bsd_requestedreceiptdate"] = deliveryplan["bsd_requestedreceiptdate"];
//                        deliverybill["bsd_warehouse"] = distinct_item["bsd_warehouse"];
//                        deliverybill["bsd_site"] = request["bsd_site"];

//                        Guid? deliverybill_id = null;
//                        foreach (var item in list_requestdeliveryproduct_sub.Entities)
//                        {
//                            EntityReference product_ref = (EntityReference)item["bsd_product"];
//                            EntityReference warehouse_ref = (EntityReference)distinct_item["bsd_warehouse"];

//                            decimal request_quantity = (decimal)item["bsd_quantity"];
//                            decimal request_netquantity = (decimal)item["bsd_netquantity"];

//                            // số lượng còn lại chưa xuất của Request Delivery
//                            decimal bill_quantity = request_quantity - request_netquantity;

//                            // Số lượng còn lại của sản phẩm này chưa xuất kho trên Delivery Schedule.
//                            decimal DeliveryScheduleProduct_Remaining_Quantity = goodsIssueNote_Manager.GetRemainingQuantitybyProduct(product_ref.Id, DeliverySchedule_Ref.Id);

//                            // Kiểm tra yêu cầu giao hàng này có product bill này kho này sản phẩm này chưa. có rồi thì cộng dồn chưa có thì tạo mới. !
//                            EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
//                                  <entity name='bsd_deliveryproductbill'>
//                                    <attribute name='bsd_deliveryproductbillid' />
//                                    <attribute name='bsd_netquantity' />
//                                    <filter type='and'>
//                                      <condition attribute='bsd_product' operator='eq' uitype='product' value='{0}' />
//                                      <condition attribute='bsd_requestdelivery' operator='eq' value='{1}' />
//                                    </filter>
//                                    <link-entity name='bsd_deliverybill' from='bsd_deliverybillid' to='bsd_deliverybill' alias='ab'>
//                                      <filter type='and'>
//                                        <condition attribute='bsd_createddeliverynote' operator='eq' value='0' />
//                                        <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='{2}' />
//                                      </filter>
//                                    </link-entity>
//                                  </entity>
//                                </fetch>", product_ref.Id, request.Id, warehouse_ref.Id)));

//                            if (list.Entities.Any())
//                            {
//                                Entity productbill = list.Entities.First();
//                                decimal old_netquantity = (decimal)productbill["bsd_netquantity"];
//                                decimal net_quantity = old_netquantity + bill_quantity;
//                                Entity new_productbill = new Entity(productbill.LogicalName, productbill.Id);
//                                new_productbill["bsd_netquantity"] = GetRemainingQuantityToCreateDeliveryBill(DeliveryScheduleProduct_Remaining_Quantity, net_quantity);
//                                myService.service.Update(new_productbill);
//                            }
//                            else
//                            {
//                                if (deliverybill_id == null) deliverybill_id = myService.service.Create(deliverybill);
//                                Entity bsd_deliveryproductbill = new Entity("bsd_deliveryproductbill");
//                                bsd_deliveryproductbill["bsd_deliverybill"] = new EntityReference("bsd_deliverybill", (Guid)deliverybill_id);
//                                bsd_deliveryproductbill["bsd_requestdelivery"] = new EntityReference("bsd_requestdelivery", request.Id);
//                                bsd_deliveryproductbill["bsd_name"] = item["bsd_name"];
//                                bsd_deliveryproductbill["bsd_product"] = product_ref;
//                                if (item.HasValue("bsd_productid")) bsd_deliveryproductbill["bsd_productid"] = item["bsd_productid"];
//                                if (item.HasValue("bsd_descriptionproduct")) bsd_deliveryproductbill["bsd_descriptionproduct"] = item["bsd_descriptionproduct"];
//                                if (item.HasValue("bsd_freeitem")) bsd_deliveryproductbill["bsd_freeitem"] = (bool)item["bsd_freeitem"];
//                                bsd_deliveryproductbill["bsd_uomid"] = item["bsd_uomid"];
//                                bill_quantity = GetRemainingQuantityToCreateDeliveryBill(DeliveryScheduleProduct_Remaining_Quantity, bill_quantity);
//                                bsd_deliveryproductbill["bsd_quantity"] = bill_quantity;
//                                bsd_deliveryproductbill["bsd_netquantity"] = bill_quantity;
//                                myService.service.Create(bsd_deliveryproductbill);

//                                #region UpdateDeliveryScheduleProduct
//                                goodsIssueNote_Manager.UpdateDeliveryScheduleProduct(request, deliveryplan.Id, product_ref.Id, 0, bill_quantity);
//                                #endregion
//                            }

//                            #region cập nhật số lượng đã xuất trên request delivery product.
//                            UpdateRequestDeliveryProduct_Quantity(item.LogicalName, item.Id);
//                            #endregion
//                        }

//                        #region update lại request là đã tạo rồi
//                        Entity new_request = new Entity(request.LogicalName, request.Id);
//                        new_request["bsd_createddeliverybill"] = true;
//                        myService.Update(new_request);
//                        #endregion
//                    }
//                }
//                else
//                {
//                    "Product quantities on Delivery Request are no longer enough to create Goods Issue Note.".Throw();
//                }
//            }
//            #endregion

//            #region Update
//            else if (myService.context.MessageName == "Update")
//            {
//                Entity target = myService.getTarget();
//                if (target.Contains("bsd_deliverynote") && target["bsd_deliverynote"] == null)
//                {
//                    target["bsd_createddeliverynote"] = false;
//                }
//            }
//            #endregion

//            #region bsd_Action_CreateDeliveryBill
//            else if (myService.context.MessageName == "bsd_Action_CreateDeliveryBill")
//            {
//                if (myService.context.Depth > 1)
//                    return;
//                EntityReference target = myService.getTargetEntityReference();
//                myService.StartService();

//                EntityCollection list_requestdelivery = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
//                  <entity name='bsd_requestdelivery'>
//                    <all-attributes />
//                    <filter type='and'>
//                      <condition attribute='bsd_deliveryplan' operator='eq' uitype='bsd_deliveryplan' value='{0}' />
//                      <condition attribute='bsd_createddeliverybill' operator='eq' value='0' />
//                      <condition attribute='bsd_warehousestatus' operator='eq' value='1' />
//                    </filter>
//                  </entity>
//                </fetch>", target.Id)));

//                foreach (var request in list_requestdelivery.Entities)
//                {
//                    //EntityCollection list_requestdeliveryproduct = myService.RetrieveOneCondition("bsd_requestdeliveryproduct", "bsd_requestdelivery", request.Id);
//                    //var list_distinct = list_requestdeliveryproduct.Entities.GroupBy(i => ((EntityReference)i["bsd_warehouse"]).Id, (key, group) => group.First()).ToList();
//                    //foreach (var distinct_item in list_distinct)
//                    //{
//                    //    Entity deliverybill = new Entity("bsd_deliverybill");
//                    //    Entity deliveryplan = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
//                    //    deliverybill["bsd_name"] = "Phiếu xuất kho - " + request["bsd_name"];
//                    //    deliverybill["bsd_requestdelivery"] = new EntityReference(request.LogicalName, request.Id);
//                    //    deliverybill["bsd_deliveryplan"] = request["bsd_deliveryplan"];
//                    //    deliverybill["bsd_order"] = request["bsd_order"];
//                    //    deliverybill["bsd_customer"] = request["bsd_account"];
//                    //    deliverybill["bsd_shiptoaddress"] = request["bsd_shiptoaddress"];
//                    //    deliverybill["bsd_requestedshipdate"] = deliveryplan["bsd_requestedshipdate"];
//                    //    deliverybill["bsd_requestedreceiptdate"] = deliveryplan["bsd_requestedreceiptdate"];
//                    //    deliverybill["bsd_warehouse"] = distinct_item["bsd_warehouse"];
//                    //    Guid deliverybill_id = myService.service.Create(deliverybill);

//                    //    EntityCollection list_requestdeliveryproduct_sub = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
//                    //      <entity name='bsd_requestdeliveryproduct'>
//                    //        <attribute name='bsd_requestdeliveryproductid' />
//                    //        <attribute name='bsd_name' />
//                    //        <attribute name='createdon' />
//                    //        <attribute name='bsd_uomid' />
//                    //        <attribute name='bsd_quantity' />
//                    //        <attribute name='bsd_product' />
//                    //        <order attribute='bsd_name' descending='false' />
//                    //        <filter type='and'>
//                    //          <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='{0}' />
//                    //          <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='{1}' />
//                    //        </filter>
//                    //      </entity>
//                    //    </fetch>", request.Id, ((EntityReference)distinct_item["bsd_warehouse"]).Id)));
//                    //    foreach (var item in list_requestdeliveryproduct_sub.Entities)
//                    //    {
//                    //        Entity bsd_deliveryproductbill = new Entity("bsd_deliveryproductbill");
//                    //        bsd_deliveryproductbill["bsd_deliverybill"] = new EntityReference("bsd_deliverybill", deliverybill_id);
//                    //        bsd_deliveryproductbill["bsd_requestdelivery"] = new EntityReference("bsd_requestdelivery", request.Id);
//                    //        bsd_deliveryproductbill["bsd_name"] = item["bsd_name"];
//                    //        bsd_deliveryproductbill["bsd_product"] = item["bsd_product"];
//                    //        bsd_deliveryproductbill["bsd_uomid"] = item["bsd_uomid"];
//                    //        bsd_deliveryproductbill["bsd_quantity"] = item["bsd_quantity"];
//                    //        bsd_deliveryproductbill["bsd_netquantity"] = item["bsd_quantity"];
//                    //        myService.service.Create(bsd_deliveryproductbill);
//                    //    }

//                    //}
//                    //#region update lại request là đã tạo rồi
//                    //Entity new_request = new Entity(request.LogicalName, request.Id);
//                    //new_request["bsd_createddeliverybill"] = true;
//                    //myService.Update(new_request);
//                    //#endregion
//                    EntityCollection list_requestdeliveryproduct = myService.RetrieveOneCondition("bsd_requestdeliveryproduct", "bsd_requestdelivery", request.Id);
//                    // distinct theo warehouse
//                    var list_distinct = list_requestdeliveryproduct.Entities.GroupBy(i => ((EntityReference)i["bsd_warehouse"]).Id, (key, group) => group.First()).ToList();
//                    foreach (var distinct_item in list_distinct)
//                    {
//                        // danh sach request product. nhung line nao co warehouse o tren.
//                        EntityCollection list_requestdeliveryproduct_sub = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
//                          <entity name='bsd_requestdeliveryproduct'>
//                            <attribute name='bsd_requestdeliveryproductid' />
//                            <attribute name='bsd_name' />
//                            <attribute name='bsd_uomid' />
//                            <attribute name='bsd_quantity' />
//                            <attribute name='bsd_netquantity' />
//                            <attribute name='bsd_product' />
//                            <filter type='and'>
//                              <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='{0}' />
//                              <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='{1}' />
//                            </filter>
//                          </entity>
//                        </fetch>", request.Id, ((EntityReference)distinct_item["bsd_warehouse"]).Id)));
//                        bool flag_check_requestdelivery_quantity = false;    // kiem tra so luong tao phieu xuat kho con khong.
//                        foreach (var item in list_requestdeliveryproduct_sub.Entities)
//                        {
//                            decimal request_quantity = (decimal)item["bsd_quantity"];
//                            decimal request_netquantity = (decimal)item["bsd_netquantity"];
//                            decimal bill_quantity = request_quantity - request_netquantity;
//                            if (bill_quantity > 0)
//                            {
//                                flag_check_requestdelivery_quantity = true;
//                            }
//                        }

//                        if (flag_check_requestdelivery_quantity == true) // du so luong moi lam tiep.
//                        {
//                            Entity deliverybill = new Entity("bsd_deliverybill");

//                            EntityReference deliveryplan_ref = (EntityReference)request["bsd_deliveryplan"];
//                            Entity deliveryplan = myService.service.Retrieve(deliveryplan_ref.LogicalName, deliveryplan_ref.Id, new ColumnSet(true));
//                            deliverybill["bsd_name"] = "Phiếu xuất kho - " + request["bsd_name"];
//                            deliverybill["bsd_requestdelivery"] = new EntityReference(request.LogicalName, request.Id);
//                            deliverybill["bsd_deliveryplan"] = request["bsd_deliveryplan"];
//                            deliverybill["bsd_order"] = request["bsd_order"];
//                            deliverybill["bsd_customer"] = request["bsd_account"];
//                            deliverybill["bsd_shiptoaddress"] = request["bsd_shiptoaddress"];
//                            deliverybill["bsd_requestedshipdate"] = deliveryplan["bsd_requestedshipdate"];
//                            deliverybill["bsd_requestedreceiptdate"] = deliveryplan["bsd_requestedreceiptdate"];
//                            deliverybill["bsd_warehouse"] = distinct_item["bsd_warehouse"];
//                            Guid deliverybill_id = myService.service.Create(deliverybill);

//                            foreach (var item in list_requestdeliveryproduct_sub.Entities)
//                            {
//                                EntityReference product_ref = (EntityReference)item["bsd_product"];
//                                EntityReference warehouse_ref = (EntityReference)distinct_item["bsd_warehouse"];

//                                decimal request_quantity = (decimal)item["bsd_quantity"];
//                                decimal request_netquantity = (decimal)item["bsd_netquantity"];
//                                decimal bill_quantity = request_quantity - request_netquantity;
//                                if (bill_quantity > 0) // kiểm tra 
//                                {
//                                    // Kiểm tra yêu cầu giao hàng này có product bill này kho này sản phẩm này chưa. có rồi thì cộng dồn chưa có thì tạo mới. !
//                                    EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
//                                  <entity name='bsd_deliveryproductbill'>
//                                    <attribute name='bsd_deliveryproductbillid' />
//                                    <attribute name='bsd_netquantity' />
//                                    <filter type='and'>
//                                      <condition attribute='bsd_product' operator='eq' uitype='product' value='{0}' />
//                                      <condition attribute='bsd_requestdelivery' operator='eq' value='{1}' />
//                                    </filter>
//                                    <link-entity name='bsd_deliverybill' from='bsd_deliverybillid' to='bsd_deliverybill' alias='ab'>
//                                      <filter type='and'>
//                                        <condition attribute='bsd_createddeliverynote' operator='eq' value='0' />
//                                        <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='{2}' />
//                                      </filter>
//                                    </link-entity>
//                                  </entity>
//                                </fetch>", product_ref.Id, request.Id, warehouse_ref.Id)));
//                                    if (list.Entities.Any())
//                                    {
//                                        Entity productbill = list.Entities.First();
//                                        decimal old_netquantity = (decimal)productbill["bsd_netquantity"];
//                                        Entity new_productbill = new Entity(productbill.LogicalName, productbill.Id);
//                                        new_productbill["bsd_netquantity"] = old_netquantity + bill_quantity;
//                                        myService.service.Update(new_productbill);

//                                    }
//                                    else
//                                    {
//                                        Entity bsd_deliveryproductbill = new Entity("bsd_deliveryproductbill");
//                                        bsd_deliveryproductbill["bsd_deliverybill"] = new EntityReference("bsd_deliverybill", deliverybill_id);
//                                        bsd_deliveryproductbill["bsd_requestdelivery"] = new EntityReference("bsd_requestdelivery", request.Id);
//                                        bsd_deliveryproductbill["bsd_name"] = item["bsd_name"];
//                                        bsd_deliveryproductbill["bsd_product"] = product_ref;
//                                        bsd_deliveryproductbill["bsd_uomid"] = item["bsd_uomid"];
//                                        bsd_deliveryproductbill["bsd_quantity"] = bill_quantity;
//                                        bsd_deliveryproductbill["bsd_netquantity"] = bill_quantity;
//                                        myService.service.Create(bsd_deliveryproductbill);
//                                    }


//                                    // kiểm tra cái phiếu xuất kho này có sản phẩm không, không có thì xóa,. !
//                                    EntityCollection list_productbill = myService.service.RetrieveMultiple(
//                                        new FetchExpression(
//                                     string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' aggregate='true'>
//                                  <entity name='bsd_deliveryproductbill' >
//                                    <attribute name='bsd_deliveryproductbillid' alias='count_line' aggregate='count' />
//                                    <filter type='and'>
//                                      <condition attribute='bsd_deliverybill' operator='eq' uitype='bsd_deliverybill' value='{0}' />
//                                    </filter>
//                                  </entity>
//                                </fetch>", deliverybill_id)));
//                                    if (list_productbill.Entities.Any())
//                                    {
//                                        var c = list_productbill.Entities.First();
//                                        Int32 count = (Int32)((AliasedValue)c["count_line"]).Value;
//                                        if (count > 0)
//                                        {
//                                            #region cập nhật số lượng đã xuất trên request delivery product.
//                                            Entity new_requestdeliveryproduct = new Entity(item.LogicalName, item.Id);
//                                            new_requestdeliveryproduct["bsd_netquantity"] = request_quantity;
//                                            myService.service.Update(new_requestdeliveryproduct);
//                                            #endregion
//                                        }
//                                        else
//                                        {
//                                            myService.service.Delete("bsd_deliverybill", deliverybill_id);
//                                        }
//                                    }
//                                }
//                            }
//                            #region update lại request là đã tạo rồi
//                            Entity new_request = new Entity(request.LogicalName, request.Id);
//                            new_request["bsd_createddeliverybill"] = true;
//                            myService.Update(new_request);
//                            #endregion
//                        }
//                    }
//                }
//            }
//            #endregion

//            #region bsd_Action_DeleteDeliveryBill
//            else if (myService.context.MessageName == "bsd_Action_DeleteDeliveryBill")
//            {
//                EntityReference target = myService.getTargetEntityReference();
//                myService.StartService();
//                EntityCollection list_deliverybill = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
//                  <entity name='bsd_deliverybill'>
//                    <attribute name='bsd_deliverybillid' />
//                    <attribute name='bsd_name' />
//                    <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='ac'>
//                      <filter type='and'>
//                        <condition attribute='bsd_requestdeliveryid' operator='eq' uitype='bsd_requestdelivery' value='{0}' />
//                        <condition attribute='bsd_createddeliverynote' operator='eq' value='0' />
//                        <condition attribute='bsd_createddeliverybill' operator='eq' value='1' />
//                      </filter>
//                    </link-entity>
//                  </entity>
//                </fetch>", target.Id)));
//                // Lấy danh phiếu xuất kho từ yêu cầu giao hàng (đẫ xuất kho, chưa tạo phiếu giao hàng).
//                if (list_deliverybill.Entities.Any())
//                {
//                    foreach (var item in list_deliverybill.Entities)
//                    {
//                        myService.service.Delete(item.LogicalName, item.Id);
//                    }
//                    Entity new_request = new Entity(target.LogicalName, target.Id);
//                    new_request["bsd_createddeliverybill"] = false;
//                    myService.Update(new_request);
//                }
//                else
//                {
//                    // ngược lại mới kiểm tra xem bị gì. !
//                    Entity request = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_createddeliverybill", "bsd_createddeliverynote"));
//                    if ((bool)request["bsd_createddeliverybill"] == false)
//                    {
//                        throw new Exception("Chưa tạo phiếu xuất kho ");
//                    }
//                    else if ((bool)request["bsd_createddeliverynote"] == true)
//                    {
//                        throw new Exception("Đã tạo phiếu giao hàng");
//                    }
//                }
//            }
//            #endregion

//            #region Delete
//            else if (myService.context.MessageName == "Delete")
//            {
//                EntityReference target = myService.getTargetEntityReference();
//                myService.StartService();
//                Entity deliverybill = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_createddeliverynote", "bsd_requestdelivery"));
//                if (deliverybill.HasValue("bsd_createddeliverynote") && (bool)deliverybill["bsd_createddeliverynote"] == true)
//                {
//                    throw new Exception("Delivery Note has been created. Cannot delete Goods Issue Note. !");
//                }

//                #region Kiểm tra nếu xóa hết bill thì cập nhật là chưa tạo
//                EntityReference requestdelivery_ref = (EntityReference)deliverybill["bsd_requestdelivery"];
//                EntityCollection list_deliverybill = myService.RetrieveOneCondition("bsd_deliverybill", "bsd_requestdelivery", requestdelivery_ref.Id);
//                if (list_deliverybill.Entities.Count == 1)
//                {
//                    Entity new_requestdelivery = new Entity(requestdelivery_ref.LogicalName, requestdelivery_ref.Id);
//                    new_requestdelivery["bsd_createddeliverybill"] = false;
//                    myService.service.Update(new_requestdelivery);
//                }
//                #endregion
//            }
//            #endregion
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="DeliveryScheduleProduct_RemainingQuantity"></param>
//        /// <param name="RequestDeliveryRemainingQuantity"></param>
//        /// <returns>
//        /// Trả về số lượng để tạo Phiếu xuất kho, nếu số lượng còn dư( chưa tạo phiếu xuất kho trên Request Product) lớn hơn số lượng chưa tạo trên tổng Delivery Schedule thì lấy số lượng còn lại của DeliverySchedule
//        /// </returns>
//        public decimal GetRemainingQuantityToCreateDeliveryBill(decimal DeliveryScheduleProduct_RemainingQuantity, decimal RequestDeliveryRemainingQuantity)
//        {
//            decimal Result = 0m;
//            if (RequestDeliveryRemainingQuantity > DeliveryScheduleProduct_RemainingQuantity)
//            {
//                Result = DeliveryScheduleProduct_RemainingQuantity;
//            }
//            else
//            {
//                Result = RequestDeliveryRemainingQuantity;
//            }
//            if (Result == 0) "No quantity to create Goods Issue Note !".Throw();
//            return Result;
//        }

//        /// <summary>
//        /// Update laij RemainngQuantity khi taoj phieu xuat kho
//        /// </summary>
//        /// <param name="LogicalName"></param>
//        /// <param name="RequestDeliveryProductId"></param>
//        public void UpdateRequestDeliveryProduct_Quantity(string LogicalName, Guid RequestDeliveryProductId)
//        {

//            Entity RequestDeliveryProduct = myService.Retrieve(LogicalName, RequestDeliveryProductId);
//            decimal RequestQuantity = (decimal)RequestDeliveryProduct["bsd_quantity"];
//            decimal GoodsIssueNoteQuantity = (decimal)RequestDeliveryProduct["bsd_netquantity"];
//            decimal RemainingQuantity = RequestQuantity - GoodsIssueNoteQuantity;
//            Entity new_requestdeliveryproduct = new Entity(LogicalName, RequestDeliveryProductId);
//            new_requestdeliveryproduct["bsd_remainingquantity"] = RemainingQuantity;
//            myService.service.Update(new_requestdeliveryproduct);
//        }
//    }

//}
