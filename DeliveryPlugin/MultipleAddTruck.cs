using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk.Query;
using DeliveryPlugin.Service;

namespace DeliveryPlugin
{
    public class MultipleAddTruck : IPlugin
    {
        private MyService myService;

        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                myService = new MyService(serviceProvider);

                if (myService.context.Depth > 1) return;
                myService.StartService();

                if (myService.context.MessageName == "bsd_Action_Create_MultipleAddtruck")
                {
                    #region vinhlh 10/05/2017
                    EntityReference target = myService.getTargetEntityReference();

                    Entity MultiTruck = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    

                    if (MultiTruck.HasValue("bsd_ids"))
                    {
                        #region Add Truck Product
                        string[] lstDeliveryProduct = MultiTruck["bsd_ids"].ToString().Split(';');
                        string licenseplate = MultiTruck["bsd_licenseplate"].ToString();
                        int trucktype = ((OptionSetValue)MultiTruck["bsd_deliverytrucktype"]).Value;
                        foreach (var item in lstDeliveryProduct)
                        {
                            EntityReference ref_deliveryschedule = (EntityReference)MultiTruck["bsd_deliveryschedule"];
                            Entity deliveryschedule = myService.service.Retrieve(ref_deliveryschedule.LogicalName, ref_deliveryschedule.Id, new ColumnSet("bsd_type", "bsd_totalshippingprice"));
                            int type = ((OptionSetValue)deliveryschedule["bsd_type"]).Value;
                            Guid DeliveryProductId = Guid.Parse(item.Replace("{", "").Replace("}", ""));
                            Entity DeliveryProduct = myService.service.Retrieve("bsd_deliveryplanproduct", DeliveryProductId, new ColumnSet(true));

                            EntityReference ref_unit = (EntityReference)DeliveryProduct["bsd_unit"];
                            EntityReference product = (EntityReference)DeliveryProduct["bsd_product"];
                            #region
                            if (MultiTruck.HasValue("bsd_deliveryschedule"))
                            {
                                EntityReference ref_deliveryPlan = (EntityReference)MultiTruck["bsd_deliveryschedule"];
                                string condition = "";
                                if (MultiTruck.HasValue("bsd_carrierpartner"))
                                {
                                    condition += "<condition attribute='bsd_carrierpartner' operator='eq' uitype='account' value='" + ((EntityReference)MultiTruck["bsd_carrierpartner"]).Id + "' />";
                                }
                                if (MultiTruck.HasValue("bsd_shippingdeliverymethod"))
                                {
                                    condition += "<condition attribute='bsd_shippingdeliverymethod' operator='eq' value='" + ((OptionSetValue)MultiTruck["bsd_shippingdeliverymethod"]).Value + "' />";
                                }
                                if (MultiTruck.HasValue("bsd_shippingoption"))
                                {
                                    condition += "<condition attribute='bsd_shippingoption' operator='eq' value='" + ((bool)MultiTruck["bsd_shippingoption"]) + "' />";
                                }
                                if (MultiTruck.HasValue("bsd_truckload"))
                                {
                                    condition += "<condition attribute='bsd_truckload' uitype='bsd_truckload' operator='eq' value='" + ((EntityReference)MultiTruck["bsd_truckload"]).Id + "' />";
                                }
                                EntityCollection lstDeliveryTruck = this.getDeliveryTruck(ref_deliveryPlan.Id, DeliveryProductId,licenseplate,trucktype,condition);
                                #region get total quantity delivery Schedule Product
                                decimal totalQuantity = (decimal)DeliveryProduct["bsd_shipquantity"];
                                #endregion
                                decimal remainaddtruck = (decimal)DeliveryProduct["bsd_remainaddtruck"];
                                decimal totalQuantityInsert = 0m;
                               
                                if (lstDeliveryTruck.Entities.Any())
                                {
                                    bool flat = true;
                                    foreach (var deliveryTruck in lstDeliveryTruck.Entities)
                                    {
                                        totalQuantityInsert += (decimal)deliveryTruck["bsd_quantity"];
                                        
                                        int method = 0;
                                        decimal factor = 1m;
                                        decimal shippingprice = 0m;
                                        decimal shippingcost_old = 0m;
                                        decimal shippingcost_new = 0m;
                                        EntityReference ref_ShipPriLst = null;
                                        Entity ent_ShipPriLst = null;
                                        EntityReference ref_unitshipping = null;
                                        if (type == 861450004 && trucktype == 861450002)
                                        {
                                            ref_ShipPriLst = (EntityReference)MultiTruck["bsd_shippingpricelist"];
                                            ent_ShipPriLst = myService.service.Retrieve(ref_ShipPriLst.LogicalName, ref_ShipPriLst.Id, new ColumnSet(true));
                                            method = ((OptionSetValue)MultiTruck["bsd_shippingdeliverymethod"]).Value;
                                            shippingprice = MultiTruck.HasValue("bsd_shippingprice") ? ((Money)MultiTruck["bsd_shippingprice"]).Value : 0m;
                                            if (method == 861450000)//ton
                                            {
                                                ref_unitshipping = (EntityReference)ent_ShipPriLst["bsd_unit"];
                                                factor = getFactor_UnitConversion(product, ref_unit, ref_unitshipping);
                                                shippingcost_old = deliveryTruck.HasValue("bsd_shippingcosts") ? ((Money)deliveryTruck["bsd_shippingcosts"]).Value : 0m;
                                            }
                                        }
                                        
                                        if (((OptionSetValue)MultiTruck["bsd_deliverytrucktype"]).Value == ((OptionSetValue)deliveryTruck["bsd_deliverytrucktype"]).Value && MultiTruck["bsd_licenseplate"].ToString().Trim() == deliveryTruck["bsd_licenseplate"].ToString().Trim())
                                        {
                                            #region cập nhật lại deliveryTruck
                                            Entity entitydeliveryTruck = new Entity(deliveryTruck.LogicalName, deliveryTruck.Id);
                                            decimal bsd_remainingquantity = totalQuantity;
                                            foreach (var deliveryTruckEntityQuantity in lstDeliveryTruck.Entities)
                                            {
                                                if (deliveryTruck.Id != deliveryTruckEntityQuantity.Id)
                                                {
                                                    bsd_remainingquantity -= (decimal)deliveryTruckEntityQuantity["bsd_quantity"];
                                                }
                                            }
                                            entitydeliveryTruck["bsd_quantity"] = bsd_remainingquantity;
                                            entitydeliveryTruck["bsd_remainingquantity"] = bsd_remainingquantity;
                                            entitydeliveryTruck["bsd_remaininggoodsissuenotequantity"] = bsd_remainingquantity;
                                            if (type == 861450004 && trucktype == 861450002)
                                            {
                                                if (method == 861450000)//ton
                                                {
                                                    shippingcost_new = shippingprice * factor * bsd_remainingquantity;
                                                }
                                            }

                                            entitydeliveryTruck["bsd_shippingcosts"] = new Money(shippingcost_new);
                                            myService.Update(entitydeliveryTruck);
                                            Entity new_deliveryschedule = new Entity(ref_deliveryPlan.LogicalName, ref_deliveryPlan.Id);
                                            decimal totalshippingprice = deliveryschedule.HasValue("bsd_totalshippingprice") ? ((Money)deliveryschedule["bsd_totalshippingprice"]).Value : 0m;
                                            decimal new_totalshippingprice = totalshippingprice - shippingcost_old + shippingcost_new;
                                            new_deliveryschedule["bsd_totalshippingprice"] = new Money(new_totalshippingprice);
                                            myService.service.Update(new_deliveryschedule);
                                            flat = false;
                                            #endregion
                                            
                                            Entity new_scheduleproduct = new Entity(DeliveryProduct.LogicalName, DeliveryProduct.Id);
                                            new_scheduleproduct["bsd_remainaddtruck"] = totalQuantity - bsd_remainingquantity;
                                            myService.service.Update(new_scheduleproduct);
                                        }

                                    }

                                    if (flat == true)
                                    {
                                        // Insert Truck
                                        Entity Product = myService.service.Retrieve("product", product.Id, new ColumnSet("productnumber"));
                                        insertTruck(MultiTruck, product, Product["productnumber"].ToString(), totalQuantity, totalQuantity - totalQuantityInsert, ref_unit, DeliveryProduct.Id);
                                    }
                                }
                                else
                                {
                                    // Insert Truck
                                    Entity Product = myService.service.Retrieve("product", product.Id, new ColumnSet("productnumber"));
                                    insertTruck(MultiTruck, product, Product["productnumber"].ToString(), totalQuantity, remainaddtruck, ref_unit, DeliveryProduct.Id);
                                }
                            }
                            #endregion
                        }
                        #endregion
                        myService.service.Delete(target.LogicalName, target.Id);
                    }
                    else
                    {
                        throw new Exception("Non check product");
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public EntityCollection getDeliveryTruck(Guid deliveryPlanId,Guid deliveryplanproductId, string licenseplate,int deliverytruck_type, string condition)
        {
            //<condition attribute='bsd_product' operator='eq' uiname='1' uitype='product' value='" + productId + @"' />
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_deliveryplantruck'>
                               <all-attributes />
                                <order attribute='bsd_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='statuscode' operator='eq' value='1' />
                                  <condition attribute='bsd_status' operator='eq' value='861450001' />
                                  <condition attribute='bsd_deliveryplan' operator='eq'  uitype='bsd_deliveryplan' value='" + deliveryPlanId + @"' />
                                  
                                  <condition attribute='bsd_deliveryplanproduct' operator='eq' uitype='bsd_deliveryplanproduct' value='" + deliveryplanproductId + @"' />
                                  <condition attribute='bsd_licenseplate' operator='eq' value='" + licenseplate + @"' />
                                  <condition attribute='bsd_deliverytrucktype' operator='eq' value='" + deliverytruck_type + @"' />
                                  "+condition+@"
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection list = myService.RetrieveMultiple(xml);
            return list;
        }
        public decimal getDeliveryScheduleProduct(Guid deliveryPlanId, Guid productId)
        {
            decimal bsd_shipquantity = 0m;
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_deliveryplanproduct'>
                           <all-attributes />
                            <order attribute='bsd_name' descending='false' />
                            <filter type='and'>
                              <condition attribute='statuscode' operator='eq' value='1' />
                              <condition attribute='bsd_deliveryplan' operator='eq' uitype='bsd_deliveryplan' value='" + deliveryPlanId + @"' />
                              <condition attribute='bsd_product' operator='eq' uiname='1' uitype='product' value='" + productId + @"' />
                            </filter>
                          </entity>
                        </fetch>";
            EntityCollection list = myService.RetrieveMultiple(xml);
            bsd_shipquantity = (decimal)list.Entities.First()["bsd_shipquantity"];
            return bsd_shipquantity;
        }
        public void insertTruck(Entity multiTruck, EntityReference productId, string productnumber, decimal totalQuantity, decimal quantity, EntityReference unitProduct, Guid deliveryplanproductId)
        { 
            Entity truck = new Entity("bsd_deliveryplantruck", Guid.NewGuid());
            if (quantity != 0)
            {
                truck["bsd_name"] = productnumber;
                truck["bsd_product"] = productId;
                truck["bsd_productid"] = productnumber;
                truck["bsd_deliveryplan"] = (EntityReference)multiTruck["bsd_deliveryschedule"];
                truck["bsd_totalquantity"] = totalQuantity;
                truck["bsd_remainingquantity"] = quantity;
                truck["bsd_quantity"] = quantity;
                truck["bsd_remaininggoodsissuenotequantity"] = quantity;
                truck["bsd_goodsissuenotequantity"] = 0m;
                truck["bsd_unit"] = unitProduct;
                truck["bsd_deliveryplanproduct"] = new EntityReference("bsd_deliveryplanproduct", deliveryplanproductId);
                truck["bsd_deliverytrucktype"] = (OptionSetValue)multiTruck["bsd_deliverytrucktype"];
                if (multiTruck.HasValue("bsd_carrierpartner"))
                    truck["bsd_carrierpartner"] = multiTruck["bsd_carrierpartner"];
                if (multiTruck.HasValue("bsd_deliverytruck"))
                    truck["bsd_deliverytruck"] = multiTruck["bsd_deliverytruck"];
                if (multiTruck.HasValue("bsd_licenseplate"))
                    truck["bsd_licenseplate"] = multiTruck["bsd_licenseplate"];
                if (multiTruck.HasValue("bsd_driver"))
                    truck["bsd_driver"] = multiTruck["bsd_driver"];
                if (multiTruck.HasValue("bsd_shippingoption"))
                    truck["bsd_shippingoption"] = multiTruck["bsd_shippingoption"];
                if (multiTruck.HasValue("bsd_description"))
                    truck["bsd_description"] = multiTruck["bsd_description"];
                if (multiTruck.HasValue("bsd_shippingdeliverymethod"))
                    truck["bsd_shippingdeliverymethod"] = (OptionSetValue)multiTruck["bsd_shippingdeliverymethod"];
                if(multiTruck.HasValue("bsd_truckload"))//return order 2
                    truck["bsd_truckload"] = multiTruck["bsd_truckload"];
                Entity ent_delschepro = myService.service.Retrieve("bsd_deliveryplanproduct", deliveryplanproductId, new ColumnSet("bsd_remainaddtruck", "bsd_standardquantity"));
                if (ent_delschepro.HasValue("bsd_standardquantity")) truck["bsd_standardquantity"] = ent_delschepro["bsd_standardquantity"];
                ent_delschepro["bsd_remainaddtruck"] = (decimal)ent_delschepro["bsd_remainaddtruck"] - quantity;
                myService.service.Update(ent_delschepro);
                #region Huy 3/11/2017: tính giá vận chuyển cho từng line
                EntityReference ref_deliveryschedule = (EntityReference)multiTruck["bsd_deliveryschedule"];
                Entity deliveryschedule = myService.service.Retrieve(ref_deliveryschedule.LogicalName, ref_deliveryschedule.Id, new ColumnSet("bsd_type", "bsd_totalshippingprice"));
                int type = ((OptionSetValue)deliveryschedule["bsd_type"]).Value;
                int trucktype = ((OptionSetValue)multiTruck["bsd_deliverytrucktype"]).Value;

                if (type == 861450004 && trucktype == 861450002)//áp dụng cho return order + shipper
                {
                    decimal total_ShipPri = deliveryschedule.HasValue("bsd_totalshippingprice") ? ((Money)deliveryschedule["bsd_totalshippingprice"]).Value : 0m;
                    EntityReference ref_ShipPriLst = (EntityReference)multiTruck["bsd_shippingpricelist"];
                    Entity ent_ShipPriLst = myService.service.Retrieve(ref_ShipPriLst.LogicalName, ref_ShipPriLst.Id, new ColumnSet(true));

                    int method = ((OptionSetValue)multiTruck["bsd_shippingdeliverymethod"]).Value;
                    decimal shippingprice = multiTruck.HasValue("bsd_shippingprice") ? ((Money)multiTruck["bsd_shippingprice"]).Value : 0m;
                    decimal shippingcost = shippingprice;
                    if (method == 861450000)//ton
                    {
                        EntityReference ref_unitshipping = (EntityReference)ent_ShipPriLst["bsd_unit"];
                        decimal factor = getFactor_UnitConversion(productId, unitProduct, ref_unitshipping);
                        shippingcost = shippingprice * factor * totalQuantity;
                    }
                    total_ShipPri += shippingcost;
                    truck["bsd_shippingpricelist"] = ref_ShipPriLst;
                    truck["bsd_shippingprice"] = new Money(shippingprice);
                    truck["bsd_shippingcosts"] = new Money(shippingcost);
                    #region cập nhật total shipping price ở delivery schedule
                    Entity new_deliveryschedule = new Entity(deliveryschedule.LogicalName, deliveryschedule.Id);
                    new_deliveryschedule["bsd_totalshippingprice"] = new Money(total_ShipPri);
                    myService.service.Update(new_deliveryschedule);
                    #endregion
                }
                #endregion

                myService.service.Create(truck);
            }
        }

        public decimal getFactor_UnitConversion(EntityReference product, EntityReference fromunit, EntityReference tounit)
        {
            decimal factor = 1;
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_unitconversions'>
                                <attribute name='bsd_factor' />
                                <filter type='and'>
                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product.Id + @"' />
                                  <condition attribute='bsd_fromunit' operator='eq' uitype='uom' value='" + fromunit.Id + @"' />
                                  <condition attribute='bsd_tounit' operator='eq' uitype='uom' value='" + tounit.Id + @"' />
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection unitconversion = myService.FetchXml(xml);
            if (unitconversion.Entities.Any())
            {
                factor = (decimal)unitconversion.Entities.FirstOrDefault()["bsd_factor"];
            }
            else
            {
                throw new Exception("Shipping Unit Conversion has not been defined !");
            }
            return factor;
        }
    }
}
