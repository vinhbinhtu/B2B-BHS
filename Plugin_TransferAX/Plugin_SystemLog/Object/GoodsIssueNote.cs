using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SystemLog.Object
{
    public class GoodsIssueNote
    {
        public static IOrganizationService service;
        public string PackingslIp { get; set; }

        public string InvoiceAccount { get; set; }

        public string InvoicingName { get; set; }

        public string Address { get; set; }

        public string Site { get; set; }

        public string Warehouse { get; set; }

        public DateTime RequestShipDate { get; set; }

        public DateTime RequestreceiptDate { get; set; }
    
        public List<GoodsIssueNoteProduct> GoodsIssueNoteProduct { get; set; }

        public string issuenoteax { get; set; }

        public string suborderid { get; set; }
        public static void Create(GoodsIssueNote objissuenote, IOrganizationService myService)
        {
            service = myService;
            var guidGoodissueNote = new Guid();
            var guidDeliveryNote = new Guid();
            int count_goodsissuenoteproduct = 0;
            int count_deliverynoteproduct = 0;
            Entity suborder = null;
            Entity deliveryplan = null;
            Entity deliverynote = null;
            Entity en_requestdelivery = null;
            var flag = false;
            string requestdeliveryid = "";
            if (objissuenote != null)
            {
                if (objissuenote.suborderid != null)
                {
                    string fetchorderid = Util.retriveLookup("bsd_suborder", "bsd_suborderax", objissuenote.suborderid, service);
                    if (fetchorderid != null)
                    {
                        #region Return Order
                        suborder = service.Retrieve("bsd_suborder", new Guid(fetchorderid), new ColumnSet(true));
                        if (suborder.Contains("bsd_type"))
                        {
                            OptionSetValue type = ((OptionSetValue)suborder["bsd_type"]);
                            if (type.Value == 861450004)
                            {
                                if (suborder.Contains("bsd_returnorder"))
                                {
                                    EntityReference returnorder = (EntityReference)suborder["bsd_returnorder"];
                                    Entity entityupdate = new Entity(returnorder.LogicalName, returnorder.Id);
                                    entityupdate["bsd_status"] = new OptionSetValue(861450002);
                                    service.Update(entityupdate);
                                    // return "succces";
                                }
                                else
                                {
                                    throw new Exception("return Order is transfer faleld b2c");
                                }

                            }
                        }
                        #endregion
                    }
                    //  return count.ToString();
                }
                // objissuenote = new GoodsIssueNote();
                flag = Util.isvalidfileld("bsd_deliverybill", "bsd_issuenoteax", objissuenote.PackingslIp,service);
                if (flag == false)
                {
                    #region Sales Order tạo Good Issues Note and Cập nhật Request Delivery and Tạo Delivery Note
                    Entity request_delivery = null;
                    #region Tạo mới Good issuse Note
                    Entity en = new Entity("bsd_deliverybill", guidGoodissueNote);
                    var fetchAccount = Util.retriveLookup("account", "accountnumber", objissuenote.InvoiceAccount,service);
                    var fetchSite = Util.retriveLookup("bsd_site", "bsd_code", objissuenote.Site,service);
                    // var fetchwarehouseentity = retriveLookup("bsd_warehouseentity", "bsd_warehouseid", objissuenote.Warehouse, org);
                    if (!string.IsNullOrEmpty(fetchAccount))
                    {
                        en["bsd_customer"] = new EntityReference("account", Guid.Parse(fetchAccount));
                    }

                    if (!string.IsNullOrEmpty(fetchSite))
                    {
                        en["bsd_site"] = new EntityReference("bsd_site", Guid.Parse(fetchSite));
                        string xmlWarehouseGood = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(fetchSite) + @"' />
                                                                      <condition attribute='bsd_warehouseid' operator='eq' value='" + objissuenote.Warehouse + @"' />
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                        var WarehouseGood = service.RetrieveMultiple(new FetchExpression(xmlWarehouseGood));
                        if (WarehouseGood.Entities.Any())
                        {

                            en["bsd_warehouse"] = new EntityReference("bsd_warehouseentity", WarehouseGood.Entities.First().Id);

                        }
                        else
                        {
                            throw new Exception("Warehouse does not exist");
                        }
                    }
                    else
                    {
                        throw new Exception("Site does not exist");
                    }

                    if (objissuenote.RequestShipDate != null)
                    {
                        en["bsd_requestedshipdate"] = objissuenote.RequestShipDate;
                    }
                    if (objissuenote.RequestreceiptDate != null)
                    {
                        en["bsd_requestedreceiptdate"] = objissuenote.RequestreceiptDate;
                    }
                    if (objissuenote.PackingslIp != null)
                    {
                        en["bsd_issuenoteax"] = objissuenote.PackingslIp;

                    }
                    if (objissuenote.issuenoteax != null)
                    {
                        #region Cập nhật lại Request Delivery
                        requestdeliveryid = Util.retrivestringvaluelookuplike("bsd_requestdelivery", "bsd_pickinglistax", objissuenote.issuenoteax.Trim(),service);
                        if (requestdeliveryid != null)
                        {
                            request_delivery = service.Retrieve("bsd_requestdelivery", Guid.Parse(requestdeliveryid), new ColumnSet(true));
                            en_requestdelivery = new Entity(request_delivery.LogicalName, request_delivery.Id);
                            en_requestdelivery["bsd_createddeliverybill"] = true;
                            en_requestdelivery["bsd_createddeliverynote"] = true;
                            en["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                            en["bsd_shiptoaddress"] = (EntityReference)request_delivery["bsd_shiptoaddress"];
                            en["bsd_deliveryplan"] = (EntityReference)request_delivery["bsd_deliveryplan"];
                        }
                        else
                        {
                            throw new Exception("Packing Slip not remember in crm");
                        }
                        #endregion
                    }
                    en["bsd_createddeliverynote"] = true;

                    #endregion
                    #region tạo delivery Note
                    EntityReference rf_deliveryplan = (EntityReference)request_delivery["bsd_deliveryplan"];
                    deliveryplan = service.Retrieve(rf_deliveryplan.LogicalName, rf_deliveryplan.Id, new ColumnSet(true));
                    deliverynote = new Entity("bsd_deliverynote", guidDeliveryNote);
                    bool request_porter = (bool)suborder["bsd_requestporter"];
                    bool request_shipping = (bool)suborder["bsd_transportation"];
                    bool shippingoption = request_delivery.HasValue("bsd_shippingoption") ? (bool)request_delivery["bsd_shippingoption"] : false;
                    Entity shipping_pricelist = null;

                    //decimal total_shipping_price = 0m;
                    if (shippingoption)
                    {
                        EntityReference shipping_pricelist_ref = (EntityReference)request_delivery["bsd_shippingpricelist"];
                        shipping_pricelist = service.Retrieve(shipping_pricelist_ref.LogicalName, shipping_pricelist_ref.Id, new ColumnSet(true));
                    }
                    deliverynote["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                    deliverynote["bsd_customer"] = new EntityReference("account", Guid.Parse(fetchAccount));

                    var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
                    fetchxmm += "<entity name='account'>";
                    fetchxmm += "<all-attributes />";
                    fetchxmm += "<filter type='and'>";
                    fetchxmm += "<condition attribute='accountnumber' operator='like' value='%" + objissuenote.InvoiceAccount + "%' />";
                    fetchxmm += "</filter>";
                    fetchxmm += "</entity>";
                    fetchxmm += "</fetch>";
                    var entityCollection = service.RetrieveMultiple(new FetchExpression(fetchxmm));
                    if (entityCollection.Entities.Count() > 0)
                    {
                        Entity account = entityCollection.Entities.First();
                        if (account.HasValue("name"))
                        {
                            deliverynote["bsd_historyreceiptcustomer"] = account["name"];
                        }

                    }

                    int request_deliverytype = ((OptionSetValue)request_delivery["bsd_type"]).Value;
                    deliverynote["bsd_packinglistax"] = objissuenote.PackingslIp;
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
                    if (objissuenote.RequestShipDate != null)
                    {
                        deliverynote["bsd_date"] = objissuenote.RequestShipDate;
                    }
                    if (request_delivery.HasValue("bsd_deliverytrucktype")) deliverynote["bsd_deliverytrucktype"] = request_delivery["bsd_deliverytrucktype"];
                    if (request_delivery.HasValue("bsd_deliverytruck")) deliverynote["bsd_deliverytruck"] = request_delivery["bsd_deliverytruck"];
                    if (request_delivery.HasValue("bsd_carrierpartner")) deliverynote["bsd_carrierpartner"] = request_delivery["bsd_carrierpartner"];
                    if (request_delivery.HasValue("bsd_historycarrierpartner")) deliverynote["bsd_historycarrierpartner"] = request_delivery["bsd_historycarrierpartner"];
                    if (request_delivery.HasValue("bsd_licenseplate")) deliverynote["bsd_licenseplate"] = request_delivery["bsd_licenseplate"];
                    if (request_delivery.HasValue("bsd_driver")) deliverynote["bsd_driver"] = request_delivery["bsd_driver"];
                    if (suborder.HasValue("bsd_carrier")) deliverynote["bsd_carrier"] = suborder["bsd_carrier"];
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
                    guidDeliveryNote = service.Create(deliverynote);
                    en["bsd_deliverynote"] = new EntityReference(deliverynote.LogicalName, guidDeliveryNote);
                    guidGoodissueNote = service.Create(en);
                    service.Update(en_requestdelivery);

                    #endregion
                    if (objissuenote.GoodsIssueNoteProduct != null)
                    {
                        #region Tạo Product Line Good issuse note and Product Line Request Delivery
                        if (objissuenote.GoodsIssueNoteProduct.Count > 0)
                        {
                            Entity chldGoodIssueNote = new Entity("bsd_deliveryproductbill");
                            Entity chldDeliveryNote = new Entity("bsd_deliverynoteproduct");
                            foreach (var objGoodsIssueNoteProduct in objissuenote.GoodsIssueNoteProduct)
                            {
                                var fetchproduct = Util.retriveLookup("product", "productnumber", objGoodsIssueNoteProduct.productnumber,service);
                                Entity retrieve = service.Retrieve("product", Guid.Parse(fetchproduct), new ColumnSet(true));
                                EntityReference uom = (EntityReference)retrieve["defaultuomid"];
                                if (!string.IsNullOrEmpty(fetchproduct))
                                {
                                    #region tạo Good issue Note Product
                                    chldGoodIssueNote["bsd_name"] = retrieve["name"];
                                    chldGoodIssueNote["bsd_deliverybill"] = new EntityReference("bsd_deliverybill", guidGoodissueNote);
                                    chldGoodIssueNote["bsd_product"] = new EntityReference("product", Guid.Parse(fetchproduct));
                                    chldGoodIssueNote["bsd_productid"] = objGoodsIssueNoteProduct.productnumber;
                                    chldGoodIssueNote["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                                    chldGoodIssueNote["bsd_uomid"] = new EntityReference(uom.LogicalName, uom.Id);
                                    chldGoodIssueNote["bsd_quantity"] = objGoodsIssueNoteProduct.Quantity;
                                    chldGoodIssueNote["bsd_netquantity"] = objGoodsIssueNoteProduct.Quantity;
                                    //  var  fetchwarehouseentityProduct = retriveLookup("bsd_warehouseentity", "bsd_warehouseid", objGoodsIssueNoteProduct.Warehouse, org);
                                    string xmlWarehouse = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(fetchSite) + @"' />
                                                                      <condition attribute='bsd_warehouseid' operator='eq' value='" + objGoodsIssueNoteProduct.Warehouse + @"' />
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                                    var Warehouse = service.RetrieveMultiple(new FetchExpression(xmlWarehouse));
                                    if (Warehouse.Entities.Any())
                                    {
                                        var WarehouseEntity = Warehouse.Entities.First();
                                        chldGoodIssueNote["bsd_warehouse"] = new EntityReference("bsd_warehouseentity", WarehouseEntity.Id);
                                        //get request product delivery 
                                        string xmlproductrequestdilivery = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                                  <entity name='bsd_requestdeliveryproduct'>
                                                                                    <attribute name='bsd_requestdeliveryproductid' />
                                                                                    <attribute name='bsd_name' />
                                                                                    <attribute name='bsd_quantity' />
                                                                                    <attribute name='bsd_netquantity' />
                                                                                    <attribute name='bsd_remainingquantity' />
                                                                                    <order attribute='bsd_name' descending='false' />
                                                                                    <filter type='and'>
                                                                                      <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                                                      <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                                                                      <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='" + WarehouseEntity.Id + @"' />
                                                                                    </filter>
                                                                                  </entity>
                                                                                </fetch>";
                                        var requestdelivery = service.RetrieveMultiple(new FetchExpression(xmlproductrequestdilivery));
                                        if (requestdelivery.Entities.Any())
                                        {
                                            var request = requestdelivery.Entities.First();
                                            var entityupdate = new Entity(request.LogicalName, request.Id);
                                            //decimal quantity_reqpro = (decimal)request["bsd_quantity"];
                                            decimal old_netquantity = (decimal)request["bsd_netquantity"];
                                            decimal old_remainingquantity = (decimal)request["bsd_remainingquantity"];
                                            entityupdate["bsd_netquantity"] = old_netquantity + objGoodsIssueNoteProduct.Quantity;
                                            entityupdate["bsd_remainingquantity"] = old_remainingquantity - objGoodsIssueNoteProduct.Quantity;
                                            service.Update(entityupdate);

                                            #region Update Delivery Schedule Truck
                                            string xml_truck = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                                                          <entity name='bsd_deliveryplantruck'>
                                                                            <attribute name='bsd_deliveryplantruckid' />
                                                                            <attribute name='bsd_quantity' />
                                                                            <attribute name='bsd_remaininggoodsissuenotequantity' />
                                                                            <attribute name='bsd_goodsissuenotequantity' />
                                                                            <filter type='and'>
                                                                              <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                                                            </filter>
                                                                            <link-entity name='bsd_requestdeliverydeliveryplantruck' from='bsd_deliveryplantruck' to='bsd_deliveryplantruckid' alias='ae'>
                                                                              <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='af'>
                                                                                <filter type='and'>
                                                                                  <condition attribute='bsd_requestdeliveryid' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                                                </filter>
                                                                              </link-entity>
                                                                            </link-entity>
                                                                          </entity>
                                                                        </fetch>";
                                            Entity ent_truck = service.RetrieveMultiple(new FetchExpression(xml_truck)).Entities.FirstOrDefault();
                                            //decimal quantity = (decimal)ent_truck["bsd_quantity"];
                                            decimal remaininggoodsissuenotequantity = (decimal)ent_truck["bsd_remaininggoodsissuenotequantity"];
                                            decimal goodsissuenotequantity = (decimal)ent_truck["bsd_goodsissuenotequantity"];
                                            decimal new_remaininggoodsissuenotequantity = remaininggoodsissuenotequantity - objGoodsIssueNoteProduct.Quantity;//0
                                            Entity update_truck = new Entity(ent_truck.LogicalName, ent_truck.Id);
                                            update_truck["bsd_goodsissuenotequantity"] = goodsissuenotequantity + objGoodsIssueNoteProduct.Quantity;
                                            update_truck["bsd_remaininggoodsissuenotequantity"] = new_remaininggoodsissuenotequantity;
                                            service.Update(update_truck);
                                            #endregion

                                            #region Update delivery Schedule Product: error
                                            //string xml_scheduleproduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                            //                                  <entity name='bsd_deliveryplanproduct'>
                                            //                                    <attribute name='bsd_deliveryplanproductid' />
                                            //                                    <attribute name='bsd_shipquantity' />
                                            //                                    <attribute name='bsd_remainingquantity' />
                                            //                                    <attribute name='bsd_remainaddtruck' />
                                            //                                    <filter type='and'>
                                            //                                      <condition attribute='bsd_deliveryplan' operator='eq' uitype='bsd_deliveryplan' value='" + rf_deliveryplan.Id + @"' />
                                            //                                      <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                            //                                    </filter>
                                            //                                  </entity>
                                            //                                </fetch>";
                                            //Entity ent_schedulepro = service.RetrieveMultiple(new FetchExpression(xml_scheduleproduct)).Entities.FirstOrDefault();
                                            //decimal schedulepro_remainqty = (decimal)ent_schedulepro["bsd_remainingquantity"];
                                            //decimal schedulepro_remainaddtruck = (decimal)ent_schedulepro["bsd_remainaddtruck"];
                                            //Entity update_schedulepro = new Entity(ent_schedulepro.LogicalName, ent_schedulepro.Id);
                                            //update_schedulepro["bsd_remainingquantity"] = schedulepro_remainqty - objGoodsIssueNoteProduct.Quantity;
                                            //update_schedulepro["bsd_remainaddtruck"] = schedulepro_remainaddtruck + new_remaininggoodsissuenotequantity;//0 + 2200
                                            //service.Update(update_schedulepro);
                                            #endregion
                                        }
                                        service.Create(chldGoodIssueNote);
                                        count_goodsissuenoteproduct++;
                                    }
                                    else
                                    {

                                        throw new Exception("Warehouse does not exist");
                                    }
                                    #endregion
                                    #region Kiểm tra đã tạo delivery note product chưa
                                    string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <all-attributes/>
                                                                <order attribute='bsd_name' descending='false' />
                                                                <filter type='and'>
                                                                  <condition attribute='statecode' operator='eq' value='0' />
                                                                  <condition attribute='bsd_deliverynote' operator='eq'  uitype='bsd_deliverynote' value='" + guidDeliveryNote + @"' />
                                                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + fetchproduct + @"' />
                                                                </filter>
                                                              </entity>
                                                            </fetch>";
                                    var lst_DeliveryNoteProduct = service.RetrieveMultiple(new FetchExpression(xml));
                                    if (lst_DeliveryNoteProduct.Entities.Any())
                                    {
                                        #region Update Delivery Note
                                        decimal total_quantity = 0m;
                                        decimal bsd_quantity = 0m;
                                        decimal standard_quantity = 1m;
                                        Entity DeliveryNoteProduct = lst_DeliveryNoteProduct.Entities.First();
                                        standard_quantity = (Decimal)DeliveryNoteProduct["bsd_standardquantity"];
                                        total_quantity = (Decimal)DeliveryNoteProduct["bsd_totalquantity"];
                                        bsd_quantity = (Decimal)DeliveryNoteProduct["bsd_quantity"];
                                        Entity DeliveryNoteProduct_Update = new Entity(DeliveryNoteProduct.LogicalName, DeliveryNoteProduct.Id);
                                        DeliveryNoteProduct_Update["bsd_totalquantity"] = total_quantity + (objGoodsIssueNoteProduct.Quantity * standard_quantity);
                                        DeliveryNoteProduct_Update["bsd_quantity"] = bsd_quantity + objGoodsIssueNoteProduct.Quantity;
                                        service.Update(DeliveryNoteProduct_Update);
                                        #endregion
                                    }
                                    else
                                    {
                                        #region Tạo Delivery Note Product
                                        decimal price_shipping_per_unit = 0m;
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
                                            decimal? factor_productunit_shippingunit = Util.GetFactor(service, retrieve.Id, uom.Id, unit_shipping.Id);

                                            if (factor_productunit_shippingunit == null)
                                            {

                                                throw new Exception("Shipping Unit Conversion has not been defined !");
                                            }
                                            if (factor_productunit_shippingunit.HasValue)
                                            {
                                                price_shipping_per_unit = price_shipping * factor_productunit_shippingunit.Value;
                                            }
                                        }
                                        #endregion
                                        chldDeliveryNote["bsd_name"] = retrieve["name"];
                                        chldDeliveryNote["bsd_product"] = new EntityReference("product", Guid.Parse(fetchproduct));
                                        chldDeliveryNote["bsd_productid"] = objGoodsIssueNoteProduct.productnumber;
                                        chldDeliveryNote["bsd_unit"] = new EntityReference(uom.LogicalName, uom.Id);

                                        // bsd_standardquantity vinhlh 24-01-2018
                                        #region 1. Tinh quantity
                                        decimal standard_quantity = 1m;
                                        decimal total_quantity = 0m;
                                        EntityReference product_unit = new EntityReference(uom.LogicalName, uom.Id);
                                        EntityReference unit_default_ref = (EntityReference)Util.GetConfigDefault(service)["bsd_unitdefault"];
                                        decimal? factor = Util.GetFactor(service, Guid.Parse(fetchproduct), product_unit.Id, unit_default_ref.Id);
                                        if (factor.HasValue)
                                        {
                                            standard_quantity = factor.Value;
                                            total_quantity = factor.Value * objGoodsIssueNoteProduct.Quantity;
                                        }
                                        else throw new Exception("Unit Convertion not created !");
                                        #endregion
                                        // end vinhlh
                                        chldDeliveryNote["bsd_standardquantity"] = standard_quantity;
                                        chldDeliveryNote["bsd_totalquantity"] = total_quantity;
                                        chldDeliveryNote["bsd_quantity"] = objGoodsIssueNoteProduct.Quantity;
                                        chldDeliveryNote["bsd_netquantity"] = 0m;
                                        chldDeliveryNote["bsd_shippingprice"] = new Money(price_shipping_per_unit);
                                        chldDeliveryNote["bsd_shippingcosts"] = new Money(price_shipping_per_unit * objGoodsIssueNoteProduct.Quantity);
                                        chldDeliveryNote["bsd_deliverynote"] = new EntityReference("bsd_deliverynote", guidDeliveryNote);
                                        service.Create(chldDeliveryNote);
                                        count_deliverynoteproduct++;
                                        #endregion
                                    }
                                    #endregion

                                }

                            }
                            #region Update delivery Schedule Product
                            EntityCollection etc_scheaddtruck = service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                                      <entity name='bsd_deliveryplantruck'>
                                                        <attribute name='bsd_deliveryplantruckid' />
                                                        <attribute name='bsd_deliveryplanproduct' />
                                                        <attribute name='bsd_goodsissuenotequantity' />
                                                        <attribute name='bsd_remaininggoodsissuenotequantity' />
                                                        <link-entity name='bsd_requestdeliverydeliveryplantruck' from='bsd_deliveryplantruck' to='bsd_deliveryplantruckid' alias='ae'>
                                                          <filter type='and'>
                                                            <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                          </filter>
                                                        </link-entity>
                                                      </entity>
                                                    </fetch>"));
                            foreach (var item_scheduletruck in etc_scheaddtruck.Entities)
                            {
                                decimal goodsissuenotequantity = (decimal)item_scheduletruck["bsd_goodsissuenotequantity"];
                                decimal remaininggoodsissuenotequantity = (decimal)item_scheduletruck["bsd_remaininggoodsissuenotequantity"];
                                EntityCollection etc_schepro = service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='bsd_deliveryplanproduct'>
                                                <attribute name='bsd_deliveryplanproductid' />
                                                <attribute name='bsd_remainingquantity' />
                                                <attribute name='bsd_remainaddtruck' />
                                                <filter type='and'>
                                                  <condition attribute='bsd_deliveryplanproductid' operator='eq' uitype='bsd_deliveryplanproduct' value='" + ((EntityReference)item_scheduletruck["bsd_deliveryplanproduct"]).Id + @"' />
                                                </filter>
                                              </entity>
                                            </fetch>"));
                                foreach (var item_scheplanpro in etc_schepro.Entities)
                                {
                                    decimal old_remainingquantity = (decimal)item_scheplanpro["bsd_remainingquantity"];
                                    decimal old_remainaddruck = (decimal)item_scheplanpro["bsd_remainaddtruck"];
                                    decimal new_remainaddtruck = old_remainaddruck + remaininggoodsissuenotequantity;
                                    decimal new_remainingquantity = old_remainingquantity - goodsissuenotequantity;
                                    Entity update_schepro = new Entity(item_scheplanpro.LogicalName, item_scheplanpro.Id);
                                    update_schepro["bsd_remainingquantity"] = new_remainingquantity;
                                    update_schepro["bsd_remainaddtruck"] = new_remainaddtruck;
                                    service.Update(update_schepro);
                                }
                            }
                            #endregion
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
                                var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_deliverynoteproduct'>
                                                    <attribute name='bsd_deliverynoteproductid' />
                                                    <attribute name='bsd_totalquantity' />
                                                    <attribute name='bsd_standardquantity' />
                                                    <attribute name='bsd_quantity' />
                                                    <filter type='and'>
                                                      <condition attribute='bsd_deliverynote' operator='eq' value='" + guidDeliveryNote + @"' />
                                                    </filter>
                                                  </entity>
                                                </fetch>";
                                EntityCollection list_product = service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
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
                                    service.Update(deliverynoteproduct);
                                }
                            }
                            #endregion
                            #region Cập nhật lại tổng giá vận chuyển
                            if (request_delivery.HasValue("bsd_shippingoption") && (bool)request_delivery["bsd_shippingoption"])
                            {
                                decimal total_shippingprice = 0m;
                                var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <attribute name='bsd_deliverynoteproductid' />
                                                                <attribute name='bsd_shippingprice' />
                                                                <attribute name='bsd_quantity' />
                                                                <filter type='and'>
                                                                  <condition attribute='bsd_deliverynote' operator='eq' value='" + guidDeliveryNote + @"' />
                                                                </filter>
                                                              </entity>
                                                            </fetch>";
                                EntityCollection list_product = service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
                                foreach (var item in list_product.Entities)
                                {
                                    decimal quantity = (decimal)item["bsd_quantity"];
                                    total_shippingprice += ((Money)item["bsd_shippingprice"]).Value * quantity;
                                }
                                Entity new_deliverynote = new Entity("bsd_deliverynote", guidDeliveryNote);
                                new_deliverynote["bsd_priceshipping"] = new Money(total_shippingprice);
                                service.Update(new_deliverynote);
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
                                service.Update(new_deliveryplan);
                                service.Update(new_suborder);

                                #region Cập nhật suborder product
                                var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_suborderproduct'>
                                                    <attribute name='bsd_suborderproductid' />
                                                    <attribute name='bsd_name' />
                                                    <attribute name='createdon' />
                                                    <order attribute='bsd_name' descending='false' />
                                                    <filter type='and'>
                                                      <condition attribute='bsd_suborder' operator='eq'  uitype='bsd_suborder' value='" + suborder.Id + @"' />
                                                    </filter>
                                                  </entity>
                                                </fetch>";
                                EntityCollection list_suborder = service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
                                // EntityCollection list_suborder = service.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);
                                foreach (var suborder_product in list_suborder.Entities)
                                {
                                    Entity n = new Entity(suborder_product.LogicalName, suborder_product.Id);
                                    n["bsd_deliverystatus"] = new OptionSetValue(861450001);
                                    service.Update(n);
                                }
                                #endregion
                            }
                            #endregion
                        }
                        #endregion
                    }
                    else
                    {
                       
                        throw new Exception(objissuenote.PackingslIp + " product does not exist");
                    }
                    #endregion
                }
                else throw new Exception(objissuenote.PackingslIp + " is existed CRM");
            }
        }
        public static GoodsIssueNote JsonParse(string jsonObject)
        {
            GoodsIssueNote obj;
            using (MemoryStream DeSerializememoryStream = new MemoryStream())
            {
                //initialize DataContractJsonSerializer object and pass Student class type to it
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(GoodsIssueNote));

                //user stream writer to write JSON string data to memory stream
                StreamWriter writer = new StreamWriter(DeSerializememoryStream);
                writer.Write(jsonObject);
                writer.Flush();
                DeSerializememoryStream.Position = 0;
                //get the Desrialized data in object of type Student
                obj = (GoodsIssueNote)serializer.ReadObject(DeSerializememoryStream);
            }
            return obj;
        }
       
    }

    public class GoodsIssueNoteProduct
    {

        public string productnumber { get; set; }

        public string Description { get; set; }

        public string Unit { get; set; }

        public decimal Quantity { get; set; }

        public string Delivered { get; set; }

        public string DeliverySchedule { get; set; }

        public string RequestDelivery { get; set; }

        public string Warehouse { get; set; }
    }
}
