using Microsoft.Xrm.Sdk;
using Plugin.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;

namespace TransferOrderPlugin
{
    public class TransferOrderProduct : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1)
                return;
            try
            {
                if (myService.context.MessageName == "Create")
                {
                    //throw new Exception("Create");
                    #region
                    myService.StartService();
                    decimal priceofshipping = 0m; int bsd_type = 861450000;
                    Entity target = myService.getTarget();
                    EntityReference ref_transferorder = (EntityReference)target["bsd_transferorder"];
                    EntityReference ref_product = (EntityReference)target["bsd_product"];
                    EntityReference ref_unit = (EntityReference)target["bsd_unit"];
                    decimal qty_transferorderproduct = Convert.ToDecimal((int)target["bsd_quantity"]);
                    Entity transferorder = myService.service.Retrieve(ref_transferorder.LogicalName, ref_transferorder.Id, new ColumnSet(true));
                    // EntityReference ref_shippingpricelist = (EntityReference)target["bsd_shippingpricelist"];
                    if (transferorder.HasValue("bsd_type"))
                    {
                        bsd_type = ((OptionSetValue)transferorder["bsd_type"]).Value;
                    }
                    if (bsd_type == 861450000)
                    {
                        #region Type Transfer Order
                        EntityReference ref_carrierpartner = (EntityReference)target["bsd_carrierpartner"];
                        int deliverymethod = ((OptionSetValue)target["bsd_deliverymethod"]).Value;
                        EntityReference ref_fromsite = (EntityReference)transferorder["bsd_fromsite"];
                        EntityReference ref_tosite = (EntityReference)transferorder["bsd_tosite"];
                        EntityReference ref_fromwarehouse = (EntityReference)transferorder["bsd_fromwarehouse"];
                        EntityReference ref_towarehouse = (EntityReference)transferorder["bsd_towarehouse"];
                        EntityReference ref_unitshipping = getUnitShipping_Configdefault();
                        bool port = target.HasValue("bsd_porter") ? (bool)target["bsd_porter"] : false;
                        // Entity shippingpricelist = myService.service.Retrieve(ref_shippingpricelist.LogicalName, ref_shippingpricelist.Id, new ColumnSet(true));
                        DateTime date = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                        #region Kiểm tra cung đường
                        EntityCollection etc_route = getRoute(ref_fromsite.Id, ref_fromwarehouse.Id, ref_tosite.Id, ref_towarehouse.Id);
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
                                    condition_main = @"<condition attribute='bsd_deliverymethod' operator='eq' value='861450000' />
                                               <condition attribute = 'bsd_unit' operator= 'eq' uitype = 'uom' value = '" + ref_unitshipping.Id + @"' /> ";
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

                                EntityCollection etc_shippingpricelist = getShippingPriceList(date, condition_order, condition_main, item_route.Id, ref_carrierpartner.Id);
                                if (etc_shippingpricelist.Entities.Any())
                                {
                                    Entity ent_ShiPriLst = etc_shippingpricelist.Entities.FirstOrDefault();
                                    EntityReference ref_ShiPriLst = new EntityReference(ent_ShiPriLst.LogicalName, ent_ShiPriLst.Id);
                                    if (port)
                                    {
                                        if (ent_ShiPriLst.HasValue("bsd_priceunitporter"))
                                        {
                                            target["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceunitporter"];
                                        }
                                        else if (ent_ShiPriLst.HasValue("bsd_pricetripporter"))
                                        {
                                            target["bsd_priceofshipping"] = ent_ShiPriLst["bsd_pricetripporter"];
                                        }
                                    }
                                    else
                                    {
                                        if (ent_ShiPriLst.HasValue("bsd_priceofton"))
                                        {
                                            target["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceofton"];
                                        }
                                        else if (ent_ShiPriLst.HasValue("bsd_priceoftrip"))
                                        {
                                            target["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceoftrip"];
                                        }
                                    }

                                    if (target.HasValue("bsd_priceofshipping"))
                                    {
                                        target["bsd_shippingpricelist"] = ref_ShiPriLst;
                                        break;
                                    }
                                }
                            }

                            if (!target.HasValue("bsd_priceofshipping"))
                            {
                                throw new Exception("Shipping price list is not defined");
                            }
                            else
                            {
                                if (deliverymethod == 861450001)
                                {
                                    target["bsd_totalpriceshipping"] = target["bsd_priceofshipping"];
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Route is not defined!");
                        }
                        #endregion


                        EntityReference ref_unitconfig = null;
                        EntityReference ref_shippingpricelist = (EntityReference)target["bsd_shippingpricelist"];
                        Entity shippingpricelist = myService.service.Retrieve(ref_shippingpricelist.LogicalName, ref_shippingpricelist.Id, new ColumnSet(true));
                        decimal totalweight = transferorder.HasValue("bsd_totalweight") ? (decimal)transferorder["bsd_totalweight"] : 0m;
                        decimal totalpriceshipping = transferorder.HasValue("bsd_totalpriceshipping") ? ((Money)transferorder["bsd_totalpriceshipping"]).Value : 0m;
                        decimal priceshipping_pro = 0m;
                        decimal factor = 1m;
                        #region Ton, tính tổng giá vận chuyển của product
                        if (deliverymethod == 861450000)//Ton
                        {
                            ref_unitconfig = (EntityReference)shippingpricelist["bsd_unit"];// unit shipping
                            priceofshipping = ((Money)target["bsd_priceofshipping"]).Value;
                            factor = getFactor_UnitConversion(ref_product, ref_unit, ref_unitconfig);
                            priceshipping_pro = priceofshipping * factor * qty_transferorderproduct;
                            target["bsd_totalpriceshipping"] = new Money(priceshipping_pro);
                            totalpriceshipping += priceshipping_pro;

                        }
                        else totalpriceshipping += ((Money)target["bsd_totalpriceshipping"]).Value;
                        #endregion

                        #region tính total weight
                        ref_unitconfig = getUnitDefault_Configdefault();//unit default
                        factor = getFactor_UnitConversion(ref_product, ref_unit, ref_unitconfig, false);
                        target["bsd_totalweight"] = factor * qty_transferorderproduct;
                        totalweight += factor * qty_transferorderproduct;
                        #endregion

                        target["bsd_standardquantity"] = factor;
                        // Entity ent_transferorderproduct = new Entity(target.LogicalName, target.Id);
                        //// throw new Exception("bsd_standardquantity" + factor);
                        // ent_transferorderproduct["bsd_standardquantity"] = factor;
                        //  myService.service.Update(ent_transferorderproduct);
                        Entity new_transferorder = new Entity(transferorder.LogicalName, transferorder.Id);
                        new_transferorder["bsd_totalpriceshipping"] = new Money(totalpriceshipping);
                        new_transferorder["bsd_totalweight"] = totalweight;
                        myService.service.Update(new_transferorder);
                        #endregion
                    }
                    else
                    {
                        #region Type Purchase Order
                        EntityReference ref_carrierpartner = (EntityReference)target["bsd_carrierpartner"];
                        int deliverymethod = ((OptionSetValue)target["bsd_deliverymethod"]).Value;
                        EntityReference ref_unitshipping = getUnitShipping_Configdefault();
                        bool port = target.HasValue("bsd_porter") ? (bool)target["bsd_porter"] : false;
                        // Entity shippingpricelist = myService.service.Retrieve(ref_shippingpricelist.LogicalName, ref_shippingpricelist.Id, new ColumnSet(true));
                        DateTime date = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                        #region Kiểm tra cung đường
                        EntityCollection etc_route = getRouteTypePurChaseOrder();
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
                                    condition_main = @"<condition attribute='bsd_deliverymethod' operator='eq' value='861450000' />
                                               <condition attribute = 'bsd_unit' operator= 'eq' uitype = 'uom' value = '" + ref_unitshipping.Id + @"' /> ";
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

                                EntityCollection etc_shippingpricelist = getShippingPriceList(date, condition_order, condition_main, item_route.Id, ref_carrierpartner.Id);
                                if (etc_shippingpricelist.Entities.Any())
                                {
                                    Entity ent_ShiPriLst = etc_shippingpricelist.Entities.FirstOrDefault();
                                    EntityReference ref_ShiPriLst = new EntityReference(ent_ShiPriLst.LogicalName, ent_ShiPriLst.Id);
                                    if (port)
                                    {
                                        if (ent_ShiPriLst.HasValue("bsd_priceunitporter"))
                                        {
                                            target["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceunitporter"];
                                        }
                                        else if (ent_ShiPriLst.HasValue("bsd_pricetripporter"))
                                        {
                                            target["bsd_priceofshipping"] = ent_ShiPriLst["bsd_pricetripporter"];
                                        }
                                    }
                                    else
                                    {
                                        if (ent_ShiPriLst.HasValue("bsd_priceofton"))
                                        {
                                            target["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceofton"];
                                        }
                                        else if (ent_ShiPriLst.HasValue("bsd_priceoftrip"))
                                        {
                                            target["bsd_priceofshipping"] = ent_ShiPriLst["bsd_priceoftrip"];
                                        }
                                    }

                                    if (target.HasValue("bsd_priceofshipping"))
                                    {
                                        target["bsd_shippingpricelist"] = ref_ShiPriLst;
                                        break;
                                    }
                                }
                            }

                            if (!target.HasValue("bsd_priceofshipping"))
                            {
                                throw new Exception("Shipping price list is not defined");
                            }
                            else
                            {
                                if (deliverymethod == 861450001)
                                {
                                    target["bsd_totalpriceshipping"] = target["bsd_priceofshipping"];
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Route is not defined!");
                        }
                        #endregion


                        EntityReference ref_unitconfig = null;
                        EntityReference ref_shippingpricelist = (EntityReference)target["bsd_shippingpricelist"];
                        Entity shippingpricelist = myService.service.Retrieve(ref_shippingpricelist.LogicalName, ref_shippingpricelist.Id, new ColumnSet(true));
                        decimal totalweight = transferorder.HasValue("bsd_totalweight") ? (decimal)transferorder["bsd_totalweight"] : 0m;
                        decimal totalpriceshipping = transferorder.HasValue("bsd_totalpriceshipping") ? ((Money)transferorder["bsd_totalpriceshipping"]).Value : 0m;
                        decimal priceshipping_pro = 0m;
                        decimal factor = 1m;
                        #region Ton, tính tổng giá vận chuyển của product
                        if (deliverymethod == 861450000)//Ton
                        {
                            ref_unitconfig = (EntityReference)shippingpricelist["bsd_unit"];// unit shipping
                            priceofshipping = ((Money)target["bsd_priceofshipping"]).Value;
                            factor = getFactor_UnitConversion(ref_product, ref_unit, ref_unitconfig);
                            priceshipping_pro = priceofshipping * factor * qty_transferorderproduct;
                            target["bsd_totalpriceshipping"] = new Money(priceshipping_pro);
                            totalpriceshipping += priceshipping_pro;

                        }
                        else totalpriceshipping += ((Money)target["bsd_totalpriceshipping"]).Value;
                            #endregion

                            #region tính total weight
                        ref_unitconfig = getUnitDefault_Configdefault();//unit default
                        factor = getFactor_UnitConversion(ref_product, ref_unit, ref_unitconfig, false);
                        target["bsd_totalweight"] = factor * qty_transferorderproduct;
                        totalweight += factor * qty_transferorderproduct;
                        #endregion

                        target["bsd_standardquantity"] = factor;
                        Entity new_transferorder = new Entity(transferorder.LogicalName, transferorder.Id);
                        new_transferorder["bsd_totalpriceshipping"] = new Money(totalpriceshipping);
                        new_transferorder["bsd_totalweight"] = totalweight;
                        myService.service.Update(new_transferorder);
                        #endregion
                    }
                    #endregion

                }
                else if (myService.context.MessageName == "Update")
                {
                    myService.StartService();
                    decimal priceofshipping = 0m;
                    Entity target = myService.getTarget();
                    if (target.HasValue("bsd_quantity"))
                    {
                        #region lấy thông tin transfer Order
                        decimal bsd_totalpriceshipping_old = 0m;
                        decimal bsd_totalweight_old = 0m;
                        decimal qty_transferorderproduct = Convert.ToDecimal((int)target["bsd_quantity"]);
                        Entity transferOrderProduct = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        #endregion

                        #region
                        EntityReference ref_unitconfig = null;
                        EntityReference ref_product = (EntityReference)transferOrderProduct["bsd_product"];
                        EntityReference ref_unit = (EntityReference)transferOrderProduct["bsd_unit"];
                        EntityReference ref_transferorder = (EntityReference)transferOrderProduct["bsd_transferorder"];
                        Entity transferorder = myService.service.Retrieve(ref_transferorder.LogicalName, ref_transferorder.Id, new ColumnSet(true));
                        EntityReference ref_shippingpricelist = (EntityReference)transferOrderProduct["bsd_shippingpricelist"];
                        int deliverymethod = ((OptionSetValue)transferOrderProduct["bsd_deliverymethod"]).Value;
                        Entity shippingpricelist = myService.service.Retrieve(ref_shippingpricelist.LogicalName, ref_shippingpricelist.Id, new ColumnSet(true));
                        decimal totalweight = transferorder.HasValue("bsd_totalweight") ? (decimal)transferorder["bsd_totalweight"] : 0m;
                        decimal totalpriceshipping = transferorder.HasValue("bsd_totalpriceshipping") ? ((Money)transferorder["bsd_totalpriceshipping"]).Value : 0m;
                        decimal priceshipping_pro = 0m;
                        decimal factor = 1m;

                        #region Ton, tính tổng giá vận chuyển của product
                        Entity transferOrderProduct_Update = new Entity(transferOrderProduct.LogicalName, transferOrderProduct.Id);
                        if (deliverymethod == 861450000)//Ton
                        {
                            ref_unitconfig = (EntityReference)shippingpricelist["bsd_unit"];// unit shipping
                            priceofshipping = ((Money)transferOrderProduct["bsd_priceofshipping"]).Value;
                            bsd_totalpriceshipping_old = ((Money)transferOrderProduct["bsd_totalpriceshipping"]).Value;
                            factor = getFactor_UnitConversion(ref_product, ref_unit, ref_unitconfig);
                            priceshipping_pro = priceofshipping * factor * qty_transferorderproduct;
                            transferOrderProduct_Update["bsd_totalpriceshipping"] = new Money(priceshipping_pro);
                            totalpriceshipping = totalpriceshipping + priceshipping_pro - bsd_totalpriceshipping_old;


                        }
                        #endregion

                        #region tính total weight
                        ref_unitconfig = getUnitDefault_Configdefault();//unit default
                        factor = getFactor_UnitConversion(ref_product, ref_unit, ref_unitconfig, false);
                        bsd_totalweight_old = (decimal)transferOrderProduct["bsd_totalweight"];
                       //khoir dau dc roi
                        transferOrderProduct_Update["bsd_totalweight"] = factor * qty_transferorderproduct;
                       // throw new Exception("totalweight:" + totalweight + "(factor * qty_transferorderproduct) : " + (factor * qty_transferorderproduct) + " bsd_totalweight_old:" + bsd_totalweight_old);
                        totalweight = totalweight + (factor * qty_transferorderproduct) - bsd_totalweight_old;
                       
                        myService.service.Update(transferOrderProduct_Update);
                        #endregion
                        Entity new_transferorder = new Entity(transferorder.LogicalName, transferorder.Id);
                        myService.StartService();
                       
                        new_transferorder["bsd_totalpriceshipping"] = new Money(totalpriceshipping);
                        new_transferorder["bsd_totalweight"] = totalweight;
                        myService.service.Update(new_transferorder);
                        #endregion

                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public decimal getFactor_UnitConversion(EntityReference product, EntityReference fromunit, EntityReference tounit, bool unitshipping = true)
        {
            decimal factor = 1;
            if (!fromunit.Equals(tounit))
            {
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
                    if (unitshipping)
                    {
                        throw new Exception("Shipping Unit Conversion has not been defined !");
                    }
                    else
                    {
                        throw new Exception("Unit Conversion has not been defined !");
                    }
                }
            }
            return factor;
        }

        public EntityReference getUnitDefault_Configdefault()
        {
            Entity configdefault = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                      <entity name='bsd_configdefault'>
                                        <attribute name='bsd_configdefaultid' />
                                        <attribute name='createdon' />
                                        <attribute name='bsd_unitdefault' />
                                        <order attribute='createdon' descending='true' />
                                      </entity>
                                    </fetch>").Entities.FirstOrDefault();
            EntityReference ref_unitdefault = (EntityReference)configdefault["bsd_unitdefault"];
            return ref_unitdefault;
        }
        public EntityCollection getRoute(Guid fromSite, Guid fromWareHouse, Guid toSite, Guid toWarehouse)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_distance'>
                    <attribute name='bsd_distanceid' />
                    <filter type='and'>
                      <condition attribute='bsd_type' operator='eq' value='861450002' />
                      <condition attribute='statecode' operator='eq' value='0' />
                      <filter type='or'>
                          <filter type='and'>
                              <condition attribute='bsd_fromsite' operator='eq' uitype='bsd_site' value='" + fromSite + @"' />
                              <condition attribute='bsd_fromwarehouse' operator='eq' uitype='bsd_warehouseentity' value='" + fromWareHouse + @"' />
                              <condition attribute='bsd_tosite' operator='eq' uitype='bsd_site' value='" + toSite + @"' />
                              <condition attribute='bsd_towarehouse' operator='eq' uitype='bsd_warehouseentity' value='" + toWarehouse + @"' />
                          </filter>
                          <filter type='and'>
                              <condition attribute='bsd_fromsite' operator='eq' uitype='bsd_site' value='" + toSite + @"' />
                              <condition attribute='bsd_fromwarehouse' operator='eq' uitype='bsd_warehouseentity' value='" + toWarehouse + @"' />
                              <condition attribute='bsd_tosite' operator='eq' uitype='bsd_site' value='" + fromSite + @"' />
                              <condition attribute='bsd_towarehouse' operator='eq' uitype='bsd_warehouseentity' value='" + fromWareHouse + @"' />
                          </filter>
                      </filter>
                    </filter>
                  </entity>
                </fetch>";
            EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(xml));
            return list;
            
        }
        public EntityCollection getRouteTypePurChaseOrder()
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_distance'>
                    <attribute name='bsd_distanceid' />
                    <filter type='and'>
                      <condition attribute='bsd_type' operator='eq' value='100000000' />
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                  </entity>
                </fetch>";
            EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(xml));
            return list;

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
        public EntityCollection getShippingPriceList(DateTime date, string conditionOrder, string conditionMain, Guid itemRoute, Guid carrierPartner)
        {
            string xml = "";
            try
            {
                xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_shippingpricelist'>
                                <attribute name='bsd_shippingpricelistid' />
                                <attribute name='bsd_name' />
                                <attribute name='bsd_priceunitporter' />
                                <attribute name='bsd_priceofton' />
                                <attribute name='bsd_pricetripporter' />
                                <attribute name='bsd_priceoftrip' />
                                " + conditionOrder + @"
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <filter type='or'>
                                    <filter type='and'>
                                      <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.Date.ToString("yyyy-MM-dd") + @"' />
                                      <condition attribute='bsd_effectiveto' operator='on-or-after' value='" + date.Date.ToString("yyyy-MM-dd") + @"' />
                                      <condition attribute='bsd_effectiveto' operator='not-null' />
                                    </filter>
                                    <filter type='and'>
                                      <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.Date.ToString("yyyy-MM-dd") + @"' />
                                      <condition attribute='bsd_effectiveto' operator='null' />
                                    </filter>
                                  </filter>
                                  <condition attribute='bsd_route' operator='eq' uiname='Route sTransfer Order' uitype='bsd_distance' value='" + itemRoute + @"' />
                                  <condition attribute='bsd_carrierpartners' operator='eq' uitype='account' value='" + carrierPartner + @"' />
                                  " + conditionMain + @"
                                </filter>
                              </entity>
                            </fetch>";
                EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(xml));
                return list;
                
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "xml: " + xml);
            }
        }
    }
}
