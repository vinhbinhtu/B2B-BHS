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
    public class DeliveryNote : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            MyService myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1) return;

            #region bsd_Action_Create_DeliveryNote
            if (myService.context.MessageName == "bsd_Action_Create_DeliveryNote")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
                Entity request_delivery = myService.Retrieve(target.LogicalName, target.Id);
                int request_deliverytype = ((OptionSetValue)request_delivery["bsd_type"]).Value;

                #region Kiểm tra xem còn phiếu xuất kho nào chưa giao không.
                EntityCollection list_phieuxuatkho = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_deliverybill'>
                        <attribute name='bsd_deliverybillid' />
                        <filter type='and'>
                          <condition attribute='bsd_createddeliverynote' operator='eq' value='0' />
                          <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='{0}' />
                        </filter>
                      </entity>
                    </fetch>", request_delivery.Id)));
                #endregion

                if (list_phieuxuatkho.Entities.Any())
                {
                    Entity deliverynote = new Entity("bsd_deliverynote");
                    Entity deliveryplan = myService.Retrieve2("bsd_deliveryplan", request_delivery["bsd_deliveryplan"]);
                    Entity suborder = myService.Retrieve2("bsd_suborder", deliveryplan["bsd_suborder"]);
                    bool request_porter = (bool)suborder["bsd_requestporter"];
                    bool request_shipping = (bool)suborder["bsd_transportation"];
                    bool shippingoption = request_delivery.HasValue("bsd_shippingoption") ? (bool)request_delivery["bsd_shippingoption"] : false;
                    Entity shipping_pricelist = null;

                    decimal total_shipping_price = 0m;

                    if (shippingoption)
                    {
                        EntityReference shipping_pricelist_ref = (EntityReference)request_delivery["bsd_shippingpricelist"];
                        shipping_pricelist = myService.service.Retrieve(shipping_pricelist_ref.LogicalName, shipping_pricelist_ref.Id, new ColumnSet(true));
                    }
                    deliverynote["bsd_requestdelivery"] = target;
                    deliverynote["bsd_customer"] = request_delivery["bsd_account"];
                    if (request_deliverytype == 861450001)
                    {
                        deliverynote["bsd_quote"] = request_delivery["bsd_quote"];
                    }
                    else if (request_deliverytype == 861450002)
                    {
                        deliverynote["bsd_order"] = request_delivery["bsd_order"];
                    }
                    deliverynote["bsd_type"] = new OptionSetValue(request_deliverytype);
                    deliverynote["bsd_date"] = request_delivery["bsd_date"];
                    if (request_delivery.HasValue("bsd_deliverytrucktype")) deliverynote["bsd_deliverytrucktype"] = request_delivery["bsd_deliverytrucktype"];
                    if (request_delivery.HasValue("bsd_deliverytruck")) deliverynote["bsd_deliverytruck"] = request_delivery["bsd_deliverytruck"];
                    if (request_delivery.HasValue("bsd_carrierpartner")) deliverynote["bsd_carrierpartner"] = request_delivery["bsd_carrierpartner"];
                    if (request_delivery.HasValue("bsd_licenseplate")) deliverynote["bsd_licenseplate"] = request_delivery["bsd_licenseplate"];
                    if (request_delivery.HasValue("bsd_driver")) deliverynote["bsd_driver"] = request_delivery["bsd_driver"];
                    if (suborder.HasValue("bsd_carrier")) deliverynote["bsd_carrier"] = suborder["bsd_carrier"];
                    //bsd_carrier
                    deliverynote["bsd_requestedshipdate"] = deliveryplan["bsd_requestedshipdate"];
                    deliverynote["bsd_requestedreceiptdate"] = deliveryplan["bsd_requestedreceiptdate"];
                    deliverynote["bsd_shiptoaddress"] = request_delivery["bsd_shiptoaddress"];
                    deliverynote["bsd_istaketrip"] = request_delivery["bsd_istaketrip"];
                    deliverynote["bsd_site"] = request_delivery["bsd_site"];
                    deliverynote["bsd_siteaddress"] = request_delivery["bsd_siteaddress"];
                    if (request_delivery.HasValue("bsd_shippingpricelist"))
                    {
                        deliverynote["bsd_shippingpricelist"] = request_delivery["bsd_shippingpricelist"];
                    }
                    if (request_delivery.HasValue("bsd_shippingoption"))
                    {
                        deliverynote["bsd_shippingoption"] = request_delivery["bsd_shippingoption"];
                    }
                    Guid deliverynote_id = myService.service.Create(deliverynote);

                    myService.context.OutputParameters["ReturnId"] = deliverynote_id.ToString();
                    // lấy product của phiếu xuất kho., bị trùng sản phẩm này phải gom lại. !
                    EntityCollection list_deliveryproductbilll = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_deliveryproductbill'>
                            <attribute name='bsd_deliveryproductbillid' />
                            <attribute name='bsd_name' />
                            <attribute name='bsd_uomid' />
                            <attribute name='bsd_requestdelivery' />
                            <attribute name='bsd_quantity' />
                            <attribute name='bsd_product' />
                            <attribute name='bsd_netquantity' />
                            <attribute name='bsd_deliverybill' />
                            <attribute name='bsd_productid' />
                            <attribute name='bsd_descriptionproduct' />
                            <attribute name='bsd_freeitem' />
                            <order attribute='bsd_name' descending='false' />
                            <filter type='and'>
                              <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='{0}' />
                            </filter>
                            <link-entity name='bsd_deliverybill' from='bsd_deliverybillid' to='bsd_deliverybill' alias='ab'>
                              <filter type='and'>
                                <condition attribute='bsd_createddeliverynote' operator='eq' value='0' />
                              </filter>
                            </link-entity>
                          </entity>
                        </fetch>", target.Id)));
                    var list_distinct = list_deliveryproductbilll.Entities.GroupBy(i => ((EntityReference)i["bsd_product"]).Id, (key, group) => group.First()).ToList();
                    foreach (var item_distinct in list_distinct)
                    {
                        decimal net_quantity = 0;

                        #region foreach qua list đầy đủ lấy thằng product = product này thì sẽ dồn số lượng bỏ vào 1 thằng duy nhất là distinct.
                        foreach (var item in list_deliveryproductbilll.Entities)
                        {
                            if (((EntityReference)item_distinct["bsd_product"]).Id.Equals(((EntityReference)item["bsd_product"]).Id))
                            {
                                net_quantity += (decimal)item["bsd_netquantity"];
                            }
                        }
                        #endregion

                        decimal standard_quantity = 1m;
                        decimal total_quantity = net_quantity;
                        decimal price_shipping_per_unit = 0m;
                        EntityReference product_ref = (EntityReference)item_distinct["bsd_product"];

                        Guid deliverynote_product_id = Guid.NewGuid();

                        #region 1. Tinh quantity
                        EntityReference product_unit = (EntityReference)item_distinct["bsd_uomid"];
                        EntityReference unit_default_ref = (EntityReference)Plugin.Util.GetConfigDefault(myService.service)["bsd_unitdefault"];
                        decimal? factor = Plugin.Util.GetFactor(myService.service, product_ref.Id, product_unit.Id, unit_default_ref.Id);
                        if (factor.HasValue)
                        {
                            standard_quantity = factor.Value;
                            total_quantity = factor.Value * net_quantity;
                        }
                        else throw new Exception("Unit Convertion not created !");
                        #endregion

                        #region 2. Tính vận chuyển ton
                        if (shippingoption && request_shipping && ((OptionSetValue)shipping_pricelist["bsd_deliverymethod"]).Value == 861450000)
                        {
                            decimal price_shipping = 0m;
                            // có bốc xếp
                            if (request_porter && shipping_pricelist.HasValue("bsd_priceunitporter"))
                            {
                                price_shipping = ((Money)shipping_pricelist["bsd_priceunitporter"]).Value; // Giá đã gồm bốc xếp
                            }
                            else
                            {
                                if (shipping_pricelist.HasValue("bsd_priceofton"))
                                {
                                    price_shipping = ((Money)shipping_pricelist["bsd_priceofton"]).Value; // Giá không bốc xếp
                                }
                                else
                                {
                                    "You must provide a value for Price Unit (Shipping Price List)".Throw();
                                }
                            }

                            EntityReference unit_shipping = (EntityReference)shipping_pricelist["bsd_unit"];
                            decimal? factor_productunit_shippingunit = Plugin.Util.GetFactor(myService.service, product_ref.Id, product_unit.Id, unit_shipping.Id);

                            if (factor_productunit_shippingunit == null) throw new Exception("Shipping Unit Conversion has not been defined !");
                            if (factor_productunit_shippingunit.HasValue)
                            {
                                price_shipping_per_unit = price_shipping * factor_productunit_shippingunit.Value;
                            }
                        }
                        #endregion

                        Entity deliverynote_product = new Entity("bsd_deliverynoteproduct");
                        deliverynote_product.Id = deliverynote_product_id;
                        deliverynote_product["bsd_product"] = product_ref;
                        deliverynote_product["bsd_name"] = item_distinct["bsd_name"];
                        deliverynote_product["bsd_unit"] = product_unit;
                        deliverynote_product["bsd_quantity"] = net_quantity;
                        deliverynote_product["bsd_standardquantity"] = standard_quantity;
                        deliverynote_product["bsd_totalquantity"] = total_quantity;
                        deliverynote_product["bsd_netquantity"] = 0m;
                        deliverynote_product["bsd_shippingprice"] = new Money(price_shipping_per_unit);
                        deliverynote_product["bsd_shippingcosts"] = new Money(price_shipping_per_unit * net_quantity);
                        deliverynote_product["bsd_deliverynote"] = new EntityReference("bsd_deliverynote", deliverynote_id);

                        if (item_distinct.HasValue("bsd_productid")) deliverynote_product["bsd_productid"] = item_distinct["bsd_productid"];
                        if (item_distinct.HasValue("bsd_descriptionproduct")) deliverynote_product["bsd_descriptionproduct"] = item_distinct["bsd_descriptionproduct"];
                        if (item_distinct.HasValue("bsd_freeitem")) deliverynote_product["bsd_freeitem"] = item_distinct["bsd_freeitem"];
                        myService.service.Create(deliverynote_product);
                    }
                    #region Tính giá vận chuyển là Trip.
                    if (shippingoption && request_shipping && ((OptionSetValue)shipping_pricelist["bsd_deliverymethod"]).Value == 861450001)
                    {
                        decimal price_shipping = 0m;
                        // nếu porter Yes
                        if (request_porter && shipping_pricelist.HasValue("bsd_pricetripporter"))
                        {
                            price_shipping = ((Money)shipping_pricelist["bsd_pricetripporter"]).Value; // Giá đã gồm bốc xếp
                        }
                        else
                        {
                            if (shipping_pricelist.HasValue("bsd_priceoftrip"))
                            {
                                price_shipping = ((Money)shipping_pricelist["bsd_priceoftrip"]).Value; // Giá không bốc xếp
                            }
                            else
                            {
                                "You must provide a value for Price Trip (Shipping Price List)".Throw();
                            }
                        }

                        EntityCollection list_product = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_deliverynoteproduct'>
                                    <attribute name='bsd_deliverynoteproductid' />
                                    <attribute name='bsd_totalquantity' />
                                    <attribute name='bsd_standardquantity' />
                                    <attribute name='bsd_quantity' />
                                    <filter type='and'>
                                      <condition attribute='bsd_deliverynote' operator='eq' value='" + deliverynote_id + @"' />
                                    </filter>
                                  </entity>
                                </fetch>");

                        decimal total_quotedetail_quantity = 0m;
                        list_product.Entities.ToList().ForEach(x => total_quotedetail_quantity += (decimal)x["bsd_totalquantity"]);
                        foreach (var item in list_product.Entities)
                        {
                            decimal item_standardquantity = (decimal)item["bsd_standardquantity"];
                            Entity deliverynoteproduct = new Entity(item.LogicalName, item.Id);
                            decimal shippingprice = price_shipping / total_quotedetail_quantity * item_standardquantity;
                            decimal quantity = (decimal)item["bsd_quantity"];
                            deliverynoteproduct["bsd_shippingprice"] = new Money(shippingprice);
                            deliverynoteproduct["bsd_shippingcosts"] = new Money(shippingprice * quantity);
                            myService.service.Update(deliverynoteproduct);
                        }
                    }
                    #endregion

                    #region Cập nhật lại tổng giá vận chuyển
                    if (request_delivery.HasValue("bsd_shippingoption") && (bool)request_delivery["bsd_shippingoption"])
                    {
                        decimal total_shippingprice = 0m;
                        EntityCollection list_product = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_deliverynoteproduct'>
                                    <attribute name='bsd_deliverynoteproductid' />
                                    <attribute name='bsd_shippingprice' />
                                    <attribute name='bsd_quantity' />
                                    <filter type='and'>
                                      <condition attribute='bsd_deliverynote' operator='eq' value='" + deliverynote_id + @"' />
                                    </filter>
                                  </entity>
                                </fetch>");
                        foreach (var item in list_product.Entities)
                        {
                            decimal quantity = (decimal)item["bsd_quantity"];
                            total_shippingprice += ((Money)item["bsd_shippingprice"]).Value * quantity;
                        }
                        Entity new_deliverynote = new Entity("bsd_deliverynote", deliverynote_id);
                        new_deliverynote["bsd_priceshipping"] = new Money(total_shippingprice);
                        myService.Update(new_deliverynote);
                    }
                    #endregion

                    #region Cập nhật lại filed Delivery Note của Delivery Bill
                    foreach (var pxk in list_phieuxuatkho.Entities)
                    {
                        Entity new_pxk = new Entity(pxk.LogicalName, pxk.Id);
                        new_pxk["bsd_deliverynote"] = new EntityReference("bsd_deliverynote", deliverynote_id);
                        myService.Update(new_pxk);
                    }
                    #endregion

                    #region cập nhật lại trạng thái của Request delivery đã tạo phiếu giao hàng rồi. 
                    Entity new_request = new Entity(request_delivery.LogicalName, request_delivery.Id);
                    new_request["bsd_createddeliverynote"] = true;
                    myService.service.Update(new_request);
                    #endregion

                    #region cập nhật lại tráng thái của Delivery Bill đã tạo phiếu giao hàng rồi luôn.
                    EntityCollection list_deliverybill = myService.RetrieveOneCondition("bsd_deliverybill", "bsd_requestdelivery", request_delivery.Id);
                    foreach (var deliverybill in list_deliverybill.Entities)
                    {
                        Entity new_deliverybill = new Entity(deliverybill.LogicalName, deliverybill.Id);
                        new_deliverybill["bsd_createddeliverynote"] = true;
                        myService.Update(new_deliverybill);
                    }
                    #endregion

                    #region Cập nhật lại suborder + delivery plan : tình trạng : Đang giao
                    var deliveryplan_status = ((OptionSetValue)deliveryplan["bsd_status"]).Value;
                    var suborder_status = ((OptionSetValue)deliveryplan["bsd_status"]).Value;

                    if (deliveryplan_status == 861450000 && suborder_status == 861450000)
                    {
                        Entity new_deliveryplan = new Entity(deliveryplan.LogicalName, deliveryplan.Id);
                        Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
                        new_suborder["bsd_status"] = new OptionSetValue(861450001);
                        new_deliveryplan["bsd_status"] = new OptionSetValue(861450001);
                        myService.Update(new_deliveryplan);
                        myService.Update(new_suborder);

                        #region Cập nhật suborder product
                        EntityCollection list_suborder = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);
                        foreach (var suborder_product in list_suborder.Entities)
                        {
                            Entity n = new Entity(suborder_product.LogicalName, suborder_product.Id);
                            n["bsd_deliverystatus"] = new OptionSetValue(861450001);
                            myService.service.Update(n);
                        }
                        #endregion
                    }
                    #endregion
                }
                else
                {
                    throw new Exception("Please create Goods Issue Note before creating a Delivery Note");
                }
            }
            #endregion

            #region Update
            else if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_confirmedreceiptdate"))
                {
                    myService.StartService();
                    DeliveryNoteService deliverynoteService = new DeliveryNoteService(myService);
                    deliverynoteService.Update_StatusDeliveryNote(target.Id);
                    Entity PreImage = myService.context.PreEntityImages["PreImage"];
                    int status = ((OptionSetValue)PreImage["bsd_status"]).Value;
                    
                    if (status == 861450000) // chuyển từ đang giao
                    {
                        Entity deliverynote = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_updatequantity"));
                        bool updated = (bool)deliverynote["bsd_updatequantity"];
                        if (updated == false)
                        {
                            EntityCollection list_deliverynoteproduct = myService.RetrieveOneCondition("bsd_deliverynoteproduct", "bsd_deliverynote", target.Id);
                            foreach (var deliverynoteproduct in list_deliverynoteproduct.Entities)
                            {
                                deliverynoteService.Update_SuborderProductStatus(deliverynoteproduct, 0, 0, 0);
                            }

                            // Chuyển trạng thái thành Update.
                            Entity new_deliverynote = new Entity(deliverynote.LogicalName, deliverynote.Id);
                            new_deliverynote["bsd_updatequantity"] = true;
                            myService.Update(new_deliverynote);
                        }
                    }
                }
            }
            #endregion

            #region Delete
            else if (myService.context.MessageName == "Delete")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
                Entity deliverynote = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_requestdelivery", "bsd_status"));
                int status = ((OptionSetValue)deliverynote["bsd_status"]).Value;
                if (status != 861450000)
                {
                    throw new Exception("Cannot delete this Delivery Note");
                }
                else
                {
                    Entity request_delivery = myService.service.Retrieve("bsd_requestdelivery", ((EntityReference)deliverynote["bsd_requestdelivery"]).Id, new ColumnSet("bsd_deliveryplan"));

                    #region Câp nhật lịa Request  Delivery là chưa tạo phiếu giao hàng!
                    EntityCollection list_deliverynote = myService.RetrieveOneCondition("bsd_deliverynote", "bsd_requestdelivery", request_delivery.Id);
                    if (list_deliverynote.Entities.Count == 1)
                    {
                        Entity new_request = new Entity("bsd_requestdelivery", (((EntityReference)deliverynote["bsd_requestdelivery"]).Id));
                        new_request["bsd_createddeliverynote"] = false;
                        myService.Update(new_request);
                    }
                    #endregion


                    Entity delivery_plan = myService.service.Retrieve("bsd_deliveryplan", ((EntityReference)request_delivery["bsd_deliveryplan"]).Id, new ColumnSet("bsd_suborder"));

                    #region kiểm tra có phiếu giao hàng nào của suborder này không (khác cái đang xóa), nếu cái này là cái cuối thì mới cập nhật là đang giao. vì 1 delivery plan có nhiều phiếu giao hàng.
                    EntityCollection list_deliverynote_deliveryplan = myService.FetchXml(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_deliverynote'>
                        <attribute name='bsd_deliverynoteid' />
                        <attribute name='bsd_name' />
                        <attribute name='createdon' />
                        <order attribute='bsd_name' descending='false' />
                        <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='ae'>
                          <link-entity name='bsd_deliveryplan' from='bsd_deliveryplanid' to='bsd_deliveryplan' alias='af'>
                            <filter type='and'>
                              <condition attribute='bsd_deliveryplanid' operator='eq' uitype='bsd_deliveryplan' value='{0}' />
                            </filter>
                          </link-entity>
                        </link-entity>
                        <filter type='and'>
                             <condition attribute='bsd_deliverynoteid' operator='ne' uitype='bsd_deliverynote' value='{1}' />
                         </filter>
                      </entity>
                    </fetch>", delivery_plan.Id, deliverynote.Id));


                    if (!list_deliverynote_deliveryplan.Entities.Any())
                    {
                        // NẾU KHHÔNG CÓ, TỨC LÀ THĂNG FNÀY LÀ THẰNG DUY NHẤT. THÌ CẬP NHẬT LẠI LÀ ĐANG GIAO TRÊN SUBORDER + DELIVERY PLAN

                        #region Cập nhật kế hoạch giao hàng !

                        Entity new_deliveryplan = new Entity(delivery_plan.LogicalName, delivery_plan.Id);
                        new_deliveryplan["bsd_status"] = new OptionSetValue(861450000);
                        myService.Update(new_deliveryplan);
                        #endregion

                        #region Cập nhật suborder và product
                        Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)delivery_plan["bsd_suborder"]).Id, new ColumnSet(true));
                        Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
                        new_suborder["bsd_status"] = new OptionSetValue(861450000);
                        myService.Update(new_suborder);

                        EntityCollection list_suborder = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);
                        foreach (var suborder_product in list_suborder.Entities)
                        {
                            Entity new_suborderproduct = new Entity(suborder_product.LogicalName, suborder_product.Id);
                            new_suborderproduct["bsd_deliverystatus"] = new OptionSetValue(861450000);
                            myService.Update(new_suborderproduct);
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            #endregion
        }
    }
}
