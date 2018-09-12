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
    public class TransferOrder
    {
        public static IOrganizationService service;
        public string bsd_name { get; set; }

        public DateTime bsd_shipdate { get; set; }

        public DateTime bsd_receiptdate { get; set; }

        public string bsd_tosite { get; set; }

        public string bsd_towarehouse { get; set; }

        public string bsd_fromsite { get; set; }

        public string bsd_description { get; set; }
        public string bsd_vendorid { get; set; }
        public string bsd_vendorname { get; set; }
        public string bsd_vendoraddress { get; set; }

        public string bsd_fromwarehouse { get; set; }


        public List<TransferOrderProduct> TransferOrderProduct { get; set; }

        public string RecId { get; set; }
        public string bsd_fromaddressid { get; set; }
        public string bsd_toaddressid { get; set; }
        public static void Create(TransferOrder obj, IOrganizationService myService)
        {
            string trace = "start";
            try
            {
                decimal priceofshipping = 0m;
                string entityName = "bsd_transferorder";
                service = myService;
                if (obj.bsd_name == null) throw new Exception("Purchase Order Id is not null");
                if (obj.bsd_vendorid == null) throw new Exception("Vendor Id is not null");
                if (obj.bsd_tosite == null) throw new Exception("To site Id is not null");
                string guid = Util.retriveLookup(entityName, "bsd_codeax", obj.RecId.Trim(), service);
                if (guid != null) throw new Exception(obj.RecId.Trim() + " Product Receipt existed in CRM");
                Entity transferOrder = new Entity(entityName);
                transferOrder["bsd_type"] = new OptionSetValue(861450001);//Purchase Order Type
                transferOrder["bsd_name"] = obj.bsd_name.Trim();
                transferOrder["bsd_codeax"] = obj.RecId.Trim();
                transferOrder["bsd_totalpriceporter"] = new Money(0);
                if (obj.bsd_receiptdate != null)
                    transferOrder["bsd_receiptdate"] = obj.bsd_receiptdate;
                //if (obj.bsd_shipdate != null)
                //    transferOrder["bsd_shipdate"] = obj.bsd_shipdate;
                if (obj.bsd_vendorid != null)
                    transferOrder["bsd_vendorid"] = obj.bsd_vendorid;
                if (obj.bsd_vendorname != null)
                    transferOrder["bsd_vendorname"] = obj.bsd_vendorname;
                if (obj.bsd_vendoraddress != null)
                    transferOrder["bsd_vendoraddress"] = obj.bsd_vendoraddress;
                string siteid = Util.retriveLookup("bsd_site", "bsd_code", obj.bsd_tosite, service);
                if (siteid == null) throw new Exception(obj.bsd_tosite + " Site not found in CRM");
                transferOrder["bsd_tosite"] = new EntityReference("bsd_site", Guid.Parse(siteid));
                string warehouseId = Util.retriveLookup("bsd_warehouseentity", "bsd_warehouseid", obj.bsd_towarehouse, service);
                if (warehouseId != null) transferOrder["bsd_towarehouse"] = new EntityReference("bsd_warehouseentity", Guid.Parse(warehouseId));
                Guid transferOrderId = service.Create(transferOrder);
                foreach (var item in obj.TransferOrderProduct)
                {
                    if (item != null)
                    {
                        Entity bsd_carrierpartner = null; Entity product = null;
                        Entity transferOrderProduct = new Entity("bsd_transferorderproduct");
                        transferOrderProduct["bsd_transferorder"] = new EntityReference(entityName, transferOrderId);
                        if (item.productnumber != null)
                            product = Util.getProduct(item.productnumber.Trim(), service);
                        else throw new Exception("Product number is not null");
                        transferOrderProduct["bsd_name"] = item.productname;
                        if (product != null)
                        {
                            transferOrderProduct["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                            // transferOrderProduct["bsd_unit"] = (EntityReference)product["defaultuomid"];
                            transferOrderProduct["bsd_name"] = product["name"];

                        }
                        transferOrderProduct["bsd_codeax"] = obj.RecId;
                        transferOrderProduct["bsd_recid"] = item.RecId;
                        transferOrderProduct["bsd_unit"] = Util.getUnitDefault_Configdefault(service);
                        transferOrderProduct["bsd_productid"] = item.productnumber.Trim().ToUpper();
                        transferOrderProduct["bsd_quantity"] = item.bsd_quantity;
                        transferOrderProduct["bsd_deliveryfee"] = true;//item.bsd_deliveryfee;
                        transferOrderProduct["bsd_porter"] = item.bsd_porter;
                        transferOrderProduct["bsd_driver"] = item.bsd_driver;
                        if (item.bsd_carrierpartner != null)
                        {
                            bsd_carrierpartner = Util.getAccount(item.bsd_carrierpartner, service);
                            transferOrderProduct["bsd_carrierpartner"] = new EntityReference(bsd_carrierpartner.LogicalName, bsd_carrierpartner.Id);
                        }
                        else throw new Exception("Carrier partner is not null");
                        EntityReference ref_carrierpartner = (EntityReference)transferOrderProduct["bsd_carrierpartner"];
                        transferOrderProduct["bsd_totalpriceporter"] = new Money(0);
                        decimal bsd_truckload = 0m;
                        if (item.bsd_truckload.ToString() != null)
                        {
                            bsd_truckload = item.bsd_truckload;
                        }
                        transferOrderProduct["bsd_truckload"] = bsd_truckload;
                        if (item.bsd_licenseplate != null)
                            transferOrderProduct["bsd_licenseplate"] = item.bsd_licenseplate;
                        int bsd_deliverymethod = 861450000;//Ton
                        if (item.bsd_deliverymethod != null)
                            if (item.bsd_deliverymethod.ToLower() == "trip" || item.bsd_deliverymethod.ToLower() == "861450001") bsd_deliverymethod = 861450001;
                        transferOrderProduct["bsd_deliverymethod"] = new OptionSetValue(bsd_deliverymethod);
                        DateTime date = Util.RetrieveLocalTimeFromUTCTime(DateTime.Now, service);
                        #region Kiểm tra cung đường
                        int deliverymethod = ((OptionSetValue)transferOrderProduct["bsd_deliverymethod"]).Value;
                        // EntityReference ref_unitshipping = getUnitShipping_Configdefault();
                        bool port = transferOrderProduct.HasValue("bsd_porter") ? (bool)transferOrderProduct["bsd_porter"] : false;
                        EntityCollection etc_route = Util.getRouteTypePurChaseOrder(service, Guid.Parse(siteid), obj.bsd_vendorid);
                        if (etc_route.Entities.Any())
                        {
                            foreach (var item_route in etc_route.Entities)
                            {
                                string condition_order = "";
                                string condition_main = "";
                                #region Ton

                                if (deliverymethod == 861450000)
                                {
                                    if (port)
                                    {
                                        condition_order = @"<order attribute = 'bsd_priceunitporter' descending = 'false' />
                                                    <order attribute='bsd_priceofton' descending='false' />";
                                    }
                                    else
                                    {
                                        condition_order = @"<order attribute='bsd_priceofton' descending='false' />";
                                    }
                                    condition_main = @"<condition attribute='bsd_deliverymethod' operator='eq' value='861450000' />";
                                    //   <condition attribute = 'bsd_unit' operator= 'eq' uitype = 'uom' value = '" + ref_unitshipping.Id + @"' /> ";
                                }
                                #endregion

                                #region Trip
                                else if (deliverymethod == 861450001)
                                {
                                    if (port)
                                    {
                                        condition_order = @"<order attribute='bsd_pricetripporter' descending='false' />
                                                    <order attribute='bsd_priceoftrip' descending='false' />";
                                    }
                                    else
                                    {
                                        condition_order = @"<order attribute='bsd_priceoftrip' descending='false' />";
                                    }
                                    condition_main = "<condition attribute='bsd_deliverymethod' operator='eq' value='861450001' />";

                                }
                                #endregion
                                trace = "1";
                                EntityCollection etc_shippingpricelist = Util.getShippingPriceList(date, condition_order, condition_main, item_route.Id, ref_carrierpartner.Id, service);
                                trace = "2";
                                if (etc_shippingpricelist.Entities.Any())
                                {
                                    Entity ent_ShiPriLst = etc_shippingpricelist.Entities.FirstOrDefault();

                                    if (deliverymethod == 861450001) // Trip
                                    {
                                        bool trip_truck = false;
                                        foreach (var shipping in etc_shippingpricelist.Entities)
                                        {
                                            if (shipping.HasValue("bsd_truckload"))
                                            {
                                                EntityReference Rf_truck_Load = (EntityReference)shipping["bsd_truckload"];
                                                Entity Entity_truckload = service.Retrieve(Rf_truck_Load.LogicalName, Rf_truck_Load.Id, new ColumnSet(true));
                                                if (Entity_truckload.HasValue("bsd_weight"))
                                                {
                                                    if ((Decimal)Entity_truckload["bsd_weight"] == item.bsd_truckload)
                                                    {
                                                        trip_truck = true;
                                                        ent_ShiPriLst = shipping;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if (trip_truck == false) throw new Exception("Truck load not found in Shipping price list in CRM");
                                    }

                                    EntityReference ref_ShiPriLst = new EntityReference(ent_ShiPriLst.LogicalName, ent_ShiPriLst.Id);
                                    if (port)
                                    {
                                        if (ent_ShiPriLst.HasValue("bsd_priceunitporter"))
                                        {
                                            transferOrderProduct["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceunitporter"];
                                        }
                                        else if (ent_ShiPriLst.HasValue("bsd_pricetripporter"))
                                        {
                                            transferOrderProduct["bsd_priceofshipping"] = ent_ShiPriLst["bsd_pricetripporter"];
                                        }
                                    }
                                    else
                                    {
                                        if (ent_ShiPriLst.HasValue("bsd_priceofton"))
                                        {
                                            transferOrderProduct["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceofton"];
                                        }
                                        else if (ent_ShiPriLst.HasValue("bsd_priceoftrip"))
                                        {
                                            transferOrderProduct["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceoftrip"];
                                        }
                                    }

                                    if (transferOrderProduct.HasValue("bsd_priceofshipping"))
                                    {
                                        transferOrderProduct["bsd_shippingpricelist"] = ref_ShiPriLst;
                                        break;
                                    }
                                }
                            }
                            if (!transferOrderProduct.HasValue("bsd_priceofshipping"))
                            {
                                throw new Exception("Shipping price list is not defined");
                            }
                            else
                            {
                                if (deliverymethod == 861450001)
                                {
                                    transferOrderProduct["bsd_totalpriceshipping"] = transferOrderProduct["bsd_priceofshipping"];
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Route is not defined!");
                        }
                        EntityReference ref_shippingpricelist = (EntityReference)transferOrderProduct["bsd_shippingpricelist"];
                        Entity shippingpricelist = service.Retrieve(ref_shippingpricelist.LogicalName, ref_shippingpricelist.Id, new ColumnSet(true));
                        decimal priceshipping_pro = 0m;
                        decimal qty_transferorderproduct = item.bsd_quantity;
                        decimal factor = 0.001m;
                        #region Ton, tính tổng giá vận chuyển của product
                        if (deliverymethod == 861450000)//Ton
                        {
                            priceofshipping = ((Money)transferOrderProduct["bsd_priceofshipping"]).Value;
                            // factor = getFactor_UnitConversion(product, unit, ref_unitconfig);
                            priceshipping_pro = priceofshipping * factor * qty_transferorderproduct;
                            transferOrderProduct["bsd_totalpriceshipping"] = new Money(priceshipping_pro);

                        }
                        #endregion

                        #region tính total weight
                        transferOrderProduct["bsd_standardquantity"] = factor;
                        factor = 1m;
                        transferOrderProduct["bsd_totalweight"] = factor * qty_transferorderproduct;
                        
                        #endregion
                        #endregion'
                        trace = "3";
                        service.Create(transferOrderProduct);
                        trace = "4";
                    }

                }
                trace = "5";
                UpdateTransferOder(transferOrderId, entityName);
                trace = "6";

            }
            catch (Exception ex)
            {
                throw new Exception("CRM Plugin:" + ex.Message);
            }

        }
        private static void UpdateTransferOder(Guid transferOrderId, string entityName)
        {

            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_transferorderproduct'>
                        <attribute name='bsd_transferorderproductid' />
                        <attribute name='bsd_name' />
                        <attribute name='createdon' />
                        <attribute name='bsd_totalweight' />
                        <attribute name='bsd_totalpriceshipping' />
                        <order attribute='bsd_name' descending='false' />
                        <filter type='and'>
                          <condition attribute='bsd_transferorder' operator='eq'  uitype='bsd_transferorder' value='" + transferOrderId + @"' />
                          <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                      </entity>
                    </fetch>";
            EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
           //throw new Exception("list.Entities.Any():" + list.Entities.Any());
            if (list.Entities.Any())
            {
                decimal bsd_totalweight = 0m; decimal bsd_totalpriceshipping = 0m;
                foreach (var item in list.Entities)
                {
                    if (item.HasValue("bsd_totalweight"))
                        bsd_totalweight += (decimal)item["bsd_totalweight"];
                    if (item.HasValue("bsd_totalpriceshipping"))
                        bsd_totalpriceshipping += ((Money)item["bsd_totalpriceshipping"]).Value;
                }
                //throw new Exception("bsd_totalpriceshipping:" + bsd_totalpriceshipping + " bsd_totalweight:" + bsd_totalweight);
                Entity transfeOrder_Update = new Entity(entityName, transferOrderId);
                transfeOrder_Update["bsd_totalweight"] = bsd_totalweight;
                transfeOrder_Update["bsd_totalpriceshipping"] = new Money(bsd_totalpriceshipping);
                service.Update(transfeOrder_Update);
                //throw new Exception("transferOrderId:" + transferOrderId);
            }

        }
        public static void Update(TransferOrder obj, IOrganizationService myService)
        {
            try
            {
                string entityName = "bsd_transferorder";
                service = myService;
                if (string.IsNullOrEmpty(obj.RecId.Trim())) throw new Exception("Product Receipt is not null");
                string transferOrderid = Util.retrivestringvaluelookup(entityName, "bsd_codeax", obj.RecId.Trim(), service);

                if (transferOrderid != null)
                {
                    foreach (var item in obj.TransferOrderProduct)
                    {
                        if (item != null)
                        {
                            string transferOrderProductid = Util.retriveLookup("bsd_transferorderproduct", "bsd_recid", item.RecId.Trim(), service);
                            if (transferOrderProductid != null)
                            {
                                decimal bsd_standardquantity = 0.001m;
                                decimal bsd_priceofshipping = 0m;
                                Entity transferOrderProduct = service.Retrieve("bsd_transferorderproduct", Guid.Parse(transferOrderProductid), new ColumnSet(true));
                                Entity TransferOrderProduct_Update = new Entity("bsd_transferorderproduct", Guid.Parse(transferOrderProductid));
                                TransferOrderProduct_Update["bsd_quantity"] = item.bsd_quantity;
                                if (transferOrderProduct.HasValue("bsd_standardquantity")) bsd_standardquantity = (decimal)transferOrderProduct["bsd_standardquantity"];
                                if (transferOrderProduct.HasValue("bsd_priceofshipping")) bsd_priceofshipping = ((Money)transferOrderProduct["bsd_priceofshipping"]).Value;
                                TransferOrderProduct_Update["bsd_totalweight"] = (decimal)(item.bsd_quantity);
                                TransferOrderProduct_Update["bsd_totalpriceshipping"] = new Money(bsd_priceofshipping * bsd_standardquantity * item.bsd_quantity);
                                service.Update(TransferOrderProduct_Update);
                            }
                        }
                    }
                    UpdateTransferOder(Guid.Parse(transferOrderid), entityName);

                }
                else throw new Exception(obj.bsd_name.Trim() + " Purchase Order Id not found in CRM");
            }
            catch (Exception ex)
            {
                throw new Exception("CRM " + ex.Message);
            }
        }
        public static TransferOrder JsonParse(string jsonObject)

        {
            /// throw new Exception(jsonObject);
            TransferOrder obj;
            using (MemoryStream DeSerializememoryStream = new MemoryStream())
            {
                //initialize DataContractJsonSerializer object and pass Student class type to it
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TransferOrder));

                //user stream writer to write JSON string data to memory stream
                StreamWriter writer = new StreamWriter(DeSerializememoryStream);
                writer.Write(jsonObject);
                writer.Flush();
                DeSerializememoryStream.Position = 0;
                //get the Desrialized data in object of type Student
                obj = (TransferOrder)serializer.ReadObject(DeSerializememoryStream);
            }
            return obj;
        }
    }

    public class TransferOrderProduct
    {

        public string productnumber { get; set; }
        public string productname { get; set; }
        public string bsd_unit { get; set; }

        public decimal bsd_quantity { get; set; }

        public string RecId { get; set; }

        public bool bsd_deliveryfee { get; set; }

        public string bsd_deliverymethod { get; set; }

        public string bsd_carrierpartner { get; set; }

        public string bsd_licenseplate { get; set; }

        public string bsd_driver { get; set; }
        public decimal bsd_truckload { get; set; }
        public bool bsd_porter { get; set; }
        public decimal bsd_totalpriceporter { get; set; }
        public decimal bsd_totalpriceshipping { get; set; }

    }
}
