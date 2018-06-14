using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugin;
using Microsoft.Crm.Sdk.Messages;
using Plugin.Service;
namespace DeliveryPlugin.Service
{
    public class SuborderService
    {
        public MyService myService;
        public SuborderService(MyService myService)
        {
            this.myService = myService;
        }
        public void Create_Update_Suborder_Product(Entity suborder_product, int formtype, bool update_suborder = true, decimal pre_quantity = 0)
        {
            Entity quote = null;
            Entity quoteproduct = null;

            Entity order = null;
            Entity salesorderdetail = null;

            bool have_quantity = true;

            Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)suborder_product["bsd_suborder"]).Id, new ColumnSet(true));
            Entity product = myService.service.Retrieve("product", ((EntityReference)suborder_product["bsd_product"]).Id, new ColumnSet(true));

            bool multipleaddress = (bool)suborder["bsd_multipleaddress"];
            bool priceinclude_shippingporter = (bool)suborder["bsd_priceincludeshippingporter"];
            decimal type = ((OptionSetValue)suborder["bsd_type"]).Value;

            #region lấy quoteproduct or salesorderdetail
            if (type == 861450001)
            {
                quote = myService.service.Retrieve("quote", ((EntityReference)suborder["bsd_quote"]).Id, new ColumnSet(true));
                quoteproduct = this.getQuoteDetailFromSuborderProduct(suborder_product, formtype);
                have_quantity = (bool)quote["bsd_havequantity"];
            }
            else if (type == 861450002)
            {
                order = myService.service.Retrieve("salesorder", ((EntityReference)suborder["bsd_order"]).Id, new ColumnSet(true));
                salesorderdetail = this.getSalesorderDetailFromSuborderProduct(suborder_product, formtype);
                have_quantity = (bool)order["bsd_havequantity"];
            }
            #endregion

            decimal price_per_unit = ((Money)suborder_product["bsd_priceperunit"]).Value;
            decimal product_quantity = (decimal)suborder_product["bsd_shipquantity"];
            decimal price_shipping_per_unit = 0m;
            decimal porter_price = 0;
            decimal standard_quantity = 1m;
            decimal total_quantity = product_quantity;
            decimal? item_sales_tax = null;
            decimal vat_percentageamount = 0m;
            bool check_using_tax = false;

            Entity newTarget = new Entity(suborder_product.LogicalName, suborder_product.Id);

            #region 1. Tinh quantity
            EntityReference unit = (EntityReference)suborder_product["bsd_unit"];

            Entity bsd_unitdefault = myService.service.Retrieve(((EntityReference)suborder["bsd_unitdefault"]).LogicalName, ((EntityReference)suborder["bsd_unitdefault"]).Id, new ColumnSet(true)); // unit default

            if (!unit.Id.Equals(bsd_unitdefault.Id))
            {
                EntityCollection list_unitconversion = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_unitconversions'>
                    <attribute name='bsd_unitconversionsid' />
                    <attribute name='bsd_factor' />
                    <filter type='and'>
                      <condition attribute='bsd_product' operator='eq' uitype='product' value='{0}' />
                      <condition attribute='bsd_fromunit' operator='eq' uitype='uom' value='{1}' />
                      <condition attribute='bsd_tounit' operator='eq' uitype='uom' value='{2}' />
                    </filter>
                  </entity>
                </fetch>", product.Id, unit.Id, bsd_unitdefault.Id)));

                if (list_unitconversion.Entities.Any())
                {
                    var unit_conversion = list_unitconversion.Entities.FirstOrDefault();
                    decimal factor = (decimal)unit_conversion["bsd_factor"];
                    standard_quantity = factor;
                    total_quantity = factor * product_quantity;
                }
                else
                {
                    throw new Exception("Unit Convertion not created !");
                }
            }
            #endregion

            #region 2. Lấy thuế
            if (suborder.HasValue("bsd_saletaxgroup"))
            {
                Entity sale_tax_group = myService.service.Retrieve("bsd_saletaxgroup", ((EntityReference)suborder["bsd_saletaxgroup"]).Id, new ColumnSet("bsd_type"));
                if (sale_tax_group.HasValue("bsd_type") && ((OptionSetValue)sale_tax_group["bsd_type"]).Value == 861450001)
                {
                    if (suborder_product.HasValue("bsd_itemsalestaxgroup"))
                    {
                        EntityReference ref_itemsalestaxgroup = (EntityReference)suborder_product["bsd_itemsalestaxgroup"];
                        Entity ent_itemsalestaxgroup = myService.service.Retrieve(ref_itemsalestaxgroup.LogicalName, ref_itemsalestaxgroup.Id, new ColumnSet("bsd_percentageamount"));
                        item_sales_tax = (decimal)ent_itemsalestaxgroup["bsd_percentageamount"];
                        vat_percentageamount = (decimal)ent_itemsalestaxgroup["bsd_percentageamount"];
                        check_using_tax = true;
                    }else
                    {
                        #region Lấy sales Tax Group
                        if (product.HasValue("bsd_itemsalestaxgroup"))
                        {
                            Entity bsd_itemsalestaxgroup = myService.service.Retrieve("bsd_itemsalestaxgroup", ((EntityReference)product["bsd_itemsalestaxgroup"]).Id, new ColumnSet("bsd_percentageamount"));
                            if (bsd_itemsalestaxgroup.HasValue("bsd_percentageamount"))
                            {
                                item_sales_tax = (decimal)bsd_itemsalestaxgroup["bsd_percentageamount"];
                            }
                            newTarget["bsd_itemsalestaxgroup"] = (EntityReference)product["bsd_itemsalestaxgroup"];
                        }
                        #endregion

                        if (item_sales_tax.HasValue)
                        {
                            EntityCollection list_saletaxgroup = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                          <entity name='bsd_saletaxgroup'>
                            <attribute name='bsd_saletaxgroupid' />
                            <attribute name='bsd_name' />
                            <link-entity name='bsd_saletaxcodesaletaxgroup' from='bsd_saletaxgroup' to='bsd_saletaxgroupid' alias='ab'>
                              <filter type='and'>
                                <condition attribute='bsd_percentageamount' operator='eq' value='{0}' />
                              </filter>
                            </link-entity>
                            <filter type='and'>
                                 <condition attribute='bsd_saletaxgroupid' operator='eq' uitype='bsd_saletaxgroup' value='{1}' />
                             </filter>
                          </entity>
                        </fetch>", (decimal)item_sales_tax, sale_tax_group.Id)));
                            if (list_saletaxgroup.Entities.Any())
                            {
                                vat_percentageamount = (decimal)item_sales_tax;
                                check_using_tax = true;
                            }
                        }
                    }
                }
                else
                {
                    newTarget["bsd_itemsalestaxgroup"] = null;
                }
            }
            
            //if (suborder.HasValue("bsd_saletaxgroup"))
            //{
            //    Entity sale_tax_group = myService.service.Retrieve("bsd_saletaxgroup", ((EntityReference)suborder["bsd_saletaxgroup"]).Id, new ColumnSet("bsd_type"));

            //    if (sale_tax_group.HasValue("bsd_type") && ((OptionSetValue)sale_tax_group["bsd_type"]).Value == 861450001)
            //    {
            //        // VAT
            //        #region Lấy sales Tax Group
            //        if (product.HasValue("bsd_itemsalestaxgroup"))
            //        {
            //            Entity bsd_itemsalestaxgroup = myService.service.Retrieve("bsd_itemsalestaxgroup", ((EntityReference)product["bsd_itemsalestaxgroup"]).Id, new ColumnSet("bsd_percentageamount"));
            //            if (bsd_itemsalestaxgroup.HasValue("bsd_percentageamount"))
            //            {
            //                item_sales_tax = (decimal)bsd_itemsalestaxgroup["bsd_percentageamount"];
            //            }
            //        }
            //        #endregion
            //        if (item_sales_tax.HasValue)
            //        {
            //            EntityCollection list_saletaxgroup = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
            //              <entity name='bsd_saletaxgroup'>
            //                <attribute name='bsd_saletaxgroupid' />
            //                <attribute name='bsd_name' />
            //                <link-entity name='bsd_saletaxcodesaletaxgroup' from='bsd_saletaxgroup' to='bsd_saletaxgroupid' alias='ab'>
            //                  <filter type='and'>
            //                    <condition attribute='bsd_percentageamount' operator='eq' value='{0}' />
            //                  </filter>
            //                </link-entity>
            //                <filter type='and'>
            //                     <condition attribute='bsd_saletaxgroupid' operator='eq' uitype='bsd_saletaxgroup' value='{1}' />
            //                 </filter>
            //              </entity>
            //            </fetch>", (decimal)item_sales_tax, sale_tax_group.Id)));
            //            if (list_saletaxgroup.Entities.Any())
            //            {
            //                vat_percentageamount = (decimal)item_sales_tax;
            //                check_using_tax = true;
            //            }
            //            else
            //            {
            //                vat_percentageamount = 0m;
            //                check_using_tax = false;
            //            }
            //        }
            //        // NON VAT
            //        else
            //        {
            //            vat_percentageamount = 0m;
            //            check_using_tax = false;
            //        }
            //    }
            //}

            #endregion

            #region 4. Giá vận chuyển
            if (priceinclude_shippingporter)
            {
                #region Nếu Suborder product được tạo mới, từ suborder được tạo từ quote.
                if (type == 861450001)
                {
                    #region Nhiều địa chỉ.
                    if (multipleaddress && (bool)quoteproduct["bsd_shippingoption"])
                    {
                        var method = ((OptionSetValue)suborder["bsd_shippingdeliverymethod"]).Value;
                        decimal price_shipping = ((Money)suborder["bsd_priceoftransportationn"]).Value;
                        // Ton
                        if (method == 861450000)
                        {
                            if (quoteproduct.HasValue("bsd_shippingdeliverymethod") && ((OptionSetValue)quoteproduct["bsd_shippingdeliverymethod"]).Value == 861450000)
                            {

                                EntityReference product_unit = (EntityReference)suborder_product["bsd_unit"];
                                EntityReference shipping_unit = (EntityReference)suborder["bsd_unitshipping"];
                                decimal? factor_productunit_shippingunit = Util.GetFactor(myService.service, product.Id, product_unit.Id, shipping_unit.Id);
                                if (factor_productunit_shippingunit == null) throw new Exception("Shipping Unit Conversion has not been defined !");
                                if (factor_productunit_shippingunit.HasValue)
                                {
                                    price_shipping_per_unit = price_shipping * factor_productunit_shippingunit.Value;
                                }
                            }
                        }
                        // Trip
                        else
                        {
                            price_shipping_per_unit = price_shipping;
                        }
                    }
                    #endregion

                    #region 1 Địa chỉ
                    if (!multipleaddress && quote.HasValue("bsd_transportation") && (bool)quote["bsd_transportation"])
                    {
                        var method = ((OptionSetValue)quote["bsd_shippingdeliverymethod"]).Value;

                        #region Ton.
                        if (method == 861450000)
                        {
                            price_shipping_per_unit = ((decimal)quoteproduct["bsd_giashipsauthue_full"]);
                        }
                        #endregion

                        #region Trip
                        else
                        {
                            decimal price_shipping = ((Money)quote["bsd_priceoftransportationn"]).Value;
                            EntityCollection list_subproduct = myService.service.RetrieveMultiple(new FetchExpression(
                               @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_suborderproduct'>
                                    <attribute name='bsd_totalquantity' />
                                    <attribute name='bsd_standardquantity' />
                                    <filter type='and'>
                                      <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='" + suborder.Id + @"' />
                                      <condition attribute='bsd_suborderproductid' operator='ne' uitype='bsd_suborderproduct' value='" + suborder_product.Id + @"' />  
                                    </filter>
                                    <link-entity name='product' from='productid' to='bsd_product' alias='ad'>    
                                        <filter type='and'>
                                             <condition attribute='bsd_shippingprice' operator='eq' value='1' />
                                        </filter>
                                     </link-entity>
                                  </entity>
                                </fetch>"));

                            decimal total_quotedetail_quantity = total_quantity;
                            foreach (var item in list_subproduct.Entities)
                            {
                                if (item.HasValue("bsd_totalquantity"))
                                {
                                    total_quotedetail_quantity += (decimal)item["bsd_totalquantity"];
                                }
                            }

                            price_shipping_per_unit = (price_shipping / total_quotedetail_quantity) * standard_quantity;

                            foreach (var item in list_subproduct.Entities)
                            {
                                decimal item_standardquantity = (decimal)item["bsd_standardquantity"];
                                Entity new_quotedetail = new Entity(item.LogicalName, item.Id);
                                new_quotedetail["bsd_shippingprice"] = new Money(price_shipping / total_quotedetail_quantity * item_standardquantity);
                                myService.service.Update(new_quotedetail);
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
                #endregion

                #region Nếu Suborder product được tạo mới, từ suborder được tạo từ Order.
                if (type == 861450002)
                {
                    #region Nhiều địa chỉ.

                    if (multipleaddress && (bool)suborder["bsd_transportation"])
                    {

                        var method = ((OptionSetValue)suborder["bsd_shippingdeliverymethod"]).Value;
                        decimal price_shipping = ((Money)suborder["bsd_priceoftransportationn"]).Value;
                        // Ton
                        if (method == 861450000)
                        {
                            if (salesorderdetail.HasValue("bsd_shippingdeliverymethod") && ((OptionSetValue)salesorderdetail["bsd_shippingdeliverymethod"]).Value == 861450000)
                            {

                                EntityReference product_unit = (EntityReference)suborder_product["bsd_unit"];
                                EntityReference shipping_unit = (EntityReference)suborder["bsd_unitshipping"];
                                decimal? factor_productunit_shippingunit = Util.GetFactor(myService.service, product.Id, product_unit.Id, shipping_unit.Id);
                                if (factor_productunit_shippingunit == null) throw new Exception("Shipping Unit Conversion has not been defined !");
                                if (factor_productunit_shippingunit.HasValue)
                                {
                                    price_shipping_per_unit = price_shipping * factor_productunit_shippingunit.Value;
                                }
                            }
                        }
                        // Trip
                        else
                        {
                            price_shipping_per_unit = price_shipping;
                        }
                    }
                    #endregion

                    #region 1 Địa chỉ
                    if (!multipleaddress && order.HasValue("bsd_transportation") && (bool)order["bsd_transportation"])
                    {
                        var method = ((OptionSetValue)order["bsd_shippingdeliverymethod"]).Value;

                        #region Ton.
                        if (method == 861450000)
                        {
                            price_shipping_per_unit = ((decimal)salesorderdetail["bsd_giashipsauthue_full"]);
                        }
                        #endregion

                        #region Trip
                        else
                        {
                            decimal price_shipping = ((Money)order["bsd_priceoftransportationn"]).Value;
                            EntityCollection list_subproduct = myService.service.RetrieveMultiple(new FetchExpression(
                               @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_suborderproduct'>
                                    <attribute name='bsd_totalquantity' />
                                    <attribute name='bsd_standardquantity' />
                                    <filter type='and'>
                                      <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='" + suborder.Id + @"' />
                                      <condition attribute='bsd_suborderproductid' operator='ne' uitype='bsd_suborderproduct' value='" + suborder_product.Id + @"' />  
                                    </filter>
                                    <link-entity name='product' from='productid' to='bsd_product' alias='ad'>    
                                        <filter type='and'>
                                             <condition attribute='bsd_shippingprice' operator='eq' value='1' />
                                        </filter>
                                     </link-entity>
                                  </entity>
                                </fetch>"));

                            decimal total_quotedetail_quantity = total_quantity;
                            foreach (var item in list_subproduct.Entities)
                            {
                                if (item.HasValue("bsd_totalquantity"))
                                {
                                    total_quotedetail_quantity += (decimal)item["bsd_totalquantity"];
                                }
                            }

                            price_shipping_per_unit = (price_shipping / total_quotedetail_quantity) * standard_quantity;

                            foreach (var item in list_subproduct.Entities)
                            {
                                decimal item_standardquantity = (decimal)item["bsd_standardquantity"];
                                Entity new_quotedetail = new Entity(item.LogicalName, item.Id);
                                new_quotedetail["bsd_shippingprice"] = new Money(price_shipping / total_quotedetail_quantity * item_standardquantity);
                                myService.service.Update(new_quotedetail);
                            }
                        }
                        #endregion
                    }
                    #endregion

                }
                #endregion

                #region Tạo mới từ suborder trực tiếp
                if (type == 861450000)
                {

                    if (suborder.HasValue("bsd_transportation") && (bool)suborder["bsd_transportation"] && suborder.HasValue("bsd_priceoftransportationn"))
                    {
                        decimal price_shipping = ((Money)suborder["bsd_priceoftransportationn"]).Value;

                        #region Ton
                        if (suborder.HasValue("bsd_shippingdeliverymethod") && ((OptionSetValue)suborder["bsd_shippingdeliverymethod"]).Value == 861450000)
                        {
                            price_shipping_per_unit = price_shipping / 1000 * standard_quantity;

                            EntityReference product_unit = (EntityReference)suborder_product["bsd_unit"];
                            EntityReference shipping_unit = (EntityReference)suborder["bsd_unitshipping"];
                            decimal? factor_productunit_shippingunit = Util.GetFactor(myService.service, product.Id, product_unit.Id, shipping_unit.Id);

                            if (factor_productunit_shippingunit == null)
                            {
                                throw new Exception("Shipping Unit Convertion not created !");
                            }

                            if (factor_productunit_shippingunit.HasValue)
                            {
                                price_shipping_per_unit = price_shipping * factor_productunit_shippingunit.Value;
                            }
                        }
                        else
                        #endregion

                        #region Trip
                        {
                            EntityCollection list_subproduct = myService.service.RetrieveMultiple(new FetchExpression(
                                  @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_suborderproduct'>
                                    <attribute name='bsd_totalquantity' />
                                    <attribute name='bsd_standardquantity' />
                                    <filter type='and'>
                                      <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='" + suborder.Id + @"' />
                                      <condition attribute='bsd_suborderproductid' operator='ne' uitype='bsd_suborderproduct' value='" + suborder_product.Id + @"' />  
                                    </filter>
                                    <link-entity name='product' from='productid' to='bsd_product' alias='ad'>    
                                        <filter type='and'>
                                             <condition attribute='bsd_shippingprice' operator='eq' value='1' />
                                        </filter>
                                     </link-entity>
                                  </entity>
                                </fetch>"));

                            decimal total_quotedetail_quantity = total_quantity;
                            foreach (var item in list_subproduct.Entities)
                            {
                                if (item.HasValue("bsd_totalquantity"))
                                {
                                    total_quotedetail_quantity += (decimal)item["bsd_totalquantity"];
                                }
                            }

                            price_shipping_per_unit = (price_shipping / total_quotedetail_quantity) * standard_quantity;

                            foreach (var item in list_subproduct.Entities)
                            {
                                decimal item_standardquantity = (decimal)item["bsd_standardquantity"];
                                Entity new_quotedetail = new Entity(item.LogicalName, item.Id);
                                new_quotedetail["bsd_shippingprice"] = new Money(price_shipping / total_quotedetail_quantity * item_standardquantity);
                                myService.service.Update(new_quotedetail);
                            }
                        }
                        #endregion
                    }
                }
                #endregion
            }
            #endregion

            #region 5. Tính bốc xếp

            if (priceinclude_shippingporter)
            {
                #region Suborder từ Quote
                if (type == 861450001 && quoteproduct != null)
                {
                    if ((bool)suborder["bsd_porteroption"] && suborder.HasValue("bsd_priceofporter") && suborder.HasValue("bsd_pricepotter"))
                    {
                        EntityReference porter_ref = (EntityReference)suborder["bsd_priceofporter"];
                        Entity porter = myService.service.Retrieve(porter_ref.LogicalName, porter_ref.Id, new ColumnSet("bsd_pricee", "bsd_unit"));

                        EntityReference porter_unit = (EntityReference)porter["bsd_unit"];

                        EntityReference product_unit = (EntityReference)suborder_product["bsd_unit"];

                        decimal? factor_productunit_porterunit = Util.GetFactor(myService.service, product.Id, product_unit.Id, porter_unit.Id);

                        if (factor_productunit_porterunit.HasValue)
                        {
                            decimal price = ((Money)suborder["bsd_pricepotter"]).Value;
                            porter_price = price * factor_productunit_porterunit.Value;
                        }
                        else
                        {
                            throw new Exception("Porter Unit Conversion has not been defined !");
                        }
                    };
                }
                #endregion

                #region Suborder từ Order
                if (type == 861450002 && salesorderdetail != null)
                {

                    if ((bool)suborder["bsd_porteroption"] && suborder.HasValue("bsd_priceofporter") && suborder.HasValue("bsd_pricepotter")) // yes/no / lookup / price
                    {
                        EntityReference porter_ref = (EntityReference)suborder["bsd_priceofporter"];
                        Entity porter = myService.service.Retrieve(porter_ref.LogicalName, porter_ref.Id, new ColumnSet("bsd_pricee", "bsd_unit"));

                        EntityReference porter_unit = (EntityReference)porter["bsd_unit"];

                        EntityReference product_unit = (EntityReference)suborder_product["bsd_unit"];

                        decimal? factor_productunit_porterunit = Util.GetFactor(myService.service, product.Id, product_unit.Id, porter_unit.Id);

                        if (factor_productunit_porterunit.HasValue)
                        {
                            decimal price = ((Money)suborder["bsd_pricepotter"]).Value;
                            porter_price = price * factor_productunit_porterunit.Value;
                        }
                        else
                        {
                            throw new Exception("Porter Unit Conversion has not been defined !");
                        }
                    }
                }
                #endregion

                #region Suborder tạo trực tiếp
                if (type == 861450000)
                {
                    if ((bool)suborder["bsd_porteroption"] && suborder.HasValue("bsd_priceofporter") && suborder.HasValue("bsd_pricepotter"))
                    {
                        EntityReference porter_ref = (EntityReference)suborder["bsd_priceofporter"];
                        Entity porter = myService.service.Retrieve(porter_ref.LogicalName, porter_ref.Id, new ColumnSet("bsd_pricee", "bsd_unit"));

                        EntityReference porter_unit = (EntityReference)porter["bsd_unit"];

                        EntityReference product_unit = (EntityReference)suborder_product["bsd_unit"];

                        decimal? factor_productunit_porterunit = Util.GetFactor(myService.service, product.Id, product_unit.Id, porter_unit.Id);

                        if (factor_productunit_porterunit.HasValue)
                        {
                            decimal price = ((Money)suborder["bsd_pricepotter"]).Value;
                            porter_price = price * factor_productunit_porterunit.Value;
                        }
                        else
                        {
                            throw new Exception("Porter Unit Convertion not created !");
                        }
                    }
                }
                #endregion
            }

            #endregion

            decimal giatruocthue = price_per_unit + price_shipping_per_unit + porter_price;
            decimal vat = (giatruocthue / 100) * vat_percentageamount;
            decimal tax = vat * product_quantity;
            decimal giasauthue = giatruocthue + vat;
            decimal amount = giatruocthue * product_quantity;
            decimal extendedamount = giasauthue * product_quantity;
            decimal bsd_exchangeratevalue = (decimal)suborder["bsd_exchangeratevalue"];
            decimal currency_exchange = bsd_exchangeratevalue * extendedamount;

            if (suborder.HasValue("bsd_warehousefrom")) newTarget["bsd_warehouse"] = suborder["bsd_warehousefrom"];
            newTarget["bsd_shippingprice"] = new Money(price_shipping_per_unit);
            newTarget["bsd_shipquantity"] = product_quantity;
            newTarget["bsd_standardquantity"] = standard_quantity;
            newTarget["bsd_totalquantity"] = total_quantity;
            newTarget["bsd_shippedquantity"] = 0m;
            newTarget["bsd_residualquantity"] = product_quantity;
            newTarget["bsd_porterprice"] = new Money(porter_price);
            newTarget["bsd_giatruocthue"] = new Money(giatruocthue);
            newTarget["bsd_giasauthue"] = new Money(giasauthue);
            newTarget["bsd_amount"] = new Money(amount);
            newTarget["bsd_extendedamount"] = new Money(extendedamount);
            newTarget["bsd_tax"] = new Money(tax);
            newTarget["bsd_itemsalestax"] = item_sales_tax;
            newTarget["bsd_usingtax"] = check_using_tax;
            newTarget["bsd_currencyexchangecurrency"] = new Money(currency_exchange);
            newTarget["bsd_currencyexchangetext"] = currency_exchange.DecimalToStringHideSymbol();
            if (check_using_tax)
            {
                newTarget["bsd_vatprice"] = new Money(vat);
                newTarget["bsd_tax"] = new Money(tax);
            }
            else
            {
                newTarget["bsd_vatprice"] = null;
                newTarget["bsd_tax"] = null;
            }

            myService.service.Update(newTarget);

            #region Cập nhật subgrid product sau khi tạo

            #region Quote
            if (type == 861450001) //Quote
            {
                myService.SetState(quote.Id, quote.LogicalName, 0, 1);
                decimal shipquantity = (decimal)suborder_product["bsd_shipquantity"];
                if (formtype == 1)
                {
                    decimal old_suborder_quantity = (decimal)quoteproduct["bsd_suborderquantity"];
                    decimal order_quantity = (decimal)quoteproduct["quantity"];
                    decimal suborder_quantity = old_suborder_quantity + shipquantity;

                    #region Update order_quantity trên suborderproduct
                    Entity new_suborderproduct = new Entity(suborder_product.LogicalName, suborder_product.Id);
                    new_suborderproduct["bsd_orderquantity"] = order_quantity;
                    new_suborderproduct["bsd_shippedquantity"] = 0m;
                    new_suborderproduct["bsd_residualquantity"] = shipquantity;
                    myService.service.Update(new_suborderproduct);
                    #endregion

                    #region update suborder_quantity tren orderproduct
                    Entity new_quoteproduct = new Entity(quoteproduct.LogicalName, quoteproduct.Id);
                    new_quoteproduct["bsd_suborderquantity"] = suborder_quantity;
                    new_quoteproduct["bsd_remainingquantity"] = order_quantity - suborder_quantity;
                    myService.service.Update(new_quoteproduct);
                    #endregion
                }
                else
                {
                    myService.SetState(quote.Id, quote.LogicalName, 0, 1);

                    decimal change_quantity = shipquantity - pre_quantity;
                    if (multipleaddress && have_quantity == false)
                    {
                        Entity quotedetail_totalline = Get_LineTotal_QuoteProduct_Quantity(product.Id, quote.Id);
                        if (quotedetail_totalline == null) throw new Exception("khong tim thay salesorder ");

                        #region cập nhật line tổng
                        decimal totalline_quantity = (decimal)quotedetail_totalline["bsd_quantity"];
                        decimal totalline_quantity_old_suborder_quantity = (decimal)quotedetail_totalline["bsd_suborderquantity"];
                        decimal new_quoteproduct_new_suborder_quantity = totalline_quantity_old_suborder_quantity + change_quantity;

                        Entity new_quotedetail_totalline = new Entity(quotedetail_totalline.LogicalName, quotedetail_totalline.Id);

                        new_quotedetail_totalline["bsd_suborderquantity"] = new_quoteproduct_new_suborder_quantity;
                        //new_quotedetail_totalline["bsd_residualquantity"] = new_quoteproduct_new_suborder_quantity;
                        new_quotedetail_totalline["bsd_remainingquantity"] = totalline_quantity - new_quoteproduct_new_suborder_quantity;
                        myService.service.Update(new_quotedetail_totalline);
                        #endregion

                        #region Cập nhật của line số lượng bằn 0, chỉ cập nhật subquantity

                        decimal quotedetail_old_suborder_quantity = (decimal)quoteproduct["bsd_suborderquantity"];
                        decimal quotedetail_new_suborder_quantity = quotedetail_old_suborder_quantity + change_quantity;
                        decimal quotedetail_standardquantity = (decimal)quoteproduct["bsd_standardquantity"];

                        Entity new_orderproduct = new Entity(quoteproduct.LogicalName, quoteproduct.Id);
                        new_orderproduct["bsd_suborderquantity"] = quotedetail_new_suborder_quantity;
                        new_orderproduct["bsd_residualquantity"] = quotedetail_new_suborder_quantity;
                        new_orderproduct["bsd_totalquantity"] = quotedetail_new_suborder_quantity * quotedetail_standardquantity;
                        myService.service.Update(new_orderproduct);
                        #endregion
                    }
                    else
                    {
                        decimal old_suborder_quantity = (decimal)quoteproduct["bsd_suborderquantity"];
                        decimal order_quantity = (decimal)quoteproduct["quantity"];

                        #region cập nhật quote_product
                        Entity new_quoteproduct = new Entity(quoteproduct.LogicalName, quoteproduct.Id);
                        decimal new_suborder_quantity = old_suborder_quantity + change_quantity;
                        new_quoteproduct["bsd_suborderquantity"] = new_suborder_quantity;
                        new_quoteproduct["bsd_remainingquantity"] = order_quantity - new_suborder_quantity;
                        myService.service.Update(new_quoteproduct);
                        #endregion
                    }
                }

                #region Won Quote
                myService.SetState(quote.Id, quote.LogicalName, 1, 2);
                WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
                Entity quoteClose = new Entity("quoteclose");
                quoteClose.Attributes["quoteid"] = new EntityReference("quote", quote.Id);
                quoteClose.Attributes["subject"] = "Quote Close" + DateTime.Now.ToString();
                winQuoteRequest.QuoteClose = quoteClose;
                winQuoteRequest.Status = new OptionSetValue(-1);
                myService.service.Execute(winQuoteRequest);
                #endregion
            }
            #endregion

            #region Order
            else if (type == 861450002) // Order
            {
                decimal shipquantity = (decimal)suborder_product["bsd_shipquantity"];
                if (formtype == 1)
                {
                    decimal order_quantity = (decimal)salesorderdetail["quantity"];
                    decimal old_suborder_quantity = (decimal)salesorderdetail["bsd_suborderquantity"];
                    decimal suborder_quantity = old_suborder_quantity + shipquantity;

                    #region Update order_quantity trene suborderproduct
                    Entity new_suborderproduct = new Entity(suborder_product.LogicalName, suborder_product.Id);
                    new_suborderproduct["bsd_orderquantity"] = order_quantity;
                    myService.service.Update(new_suborderproduct);
                    #endregion

                    #region update suborder_quantity tren orderproduct
                    Entity new_salesorderdetail = new Entity(salesorderdetail.LogicalName, salesorderdetail.Id);
                    new_salesorderdetail["bsd_suborderquantity"] = suborder_quantity;
                    new_salesorderdetail["bsd_remainingquantity"] = order_quantity - suborder_quantity;
                    myService.service.Update(new_salesorderdetail);
                    #endregion
                }
                else
                {
                    decimal change_quantity = shipquantity - pre_quantity;
                    if (multipleaddress && have_quantity == false)
                    {
                        Entity orderproduct = Get_LineTotal_OrderProduct_Quantity(product.Id, order.Id);
                        if (orderproduct == null) throw new Exception("khong tim thay salesorder ");

                        #region cập nhật line tổng
                        decimal totalline_quantity = (decimal)orderproduct["bsd_quantity"];
                        decimal totalline_quantity_old_suborder_quantity = (decimal)orderproduct["bsd_suborderquantity"];
                        decimal new_orderproduct_new_suborder_quantity = totalline_quantity_old_suborder_quantity + change_quantity;

                        Entity new_orderproduct_totalline = new Entity(orderproduct.LogicalName, orderproduct.Id);

                        new_orderproduct_totalline["bsd_suborderquantity"] = new_orderproduct_new_suborder_quantity;
                        //new_orderproduct_totalline["bsd_residualquantity"] = new_orderproduct_new_suborder_quantity;
                        new_orderproduct_totalline["bsd_remainingquantity"] = totalline_quantity - new_orderproduct_new_suborder_quantity;
                        myService.service.Update(new_orderproduct_totalline);
                        #endregion

                        #region Cập nhật của line số lượng bằn 0, chỉ cập nhật subquantity

                        decimal salesorderdetail_old_suborder_quantity = (decimal)salesorderdetail["bsd_suborderquantity"];
                        decimal salesorderdetail_new_suborder_quantity = salesorderdetail_old_suborder_quantity + change_quantity;
                        decimal salesorderdetail_standardquantity = (decimal)orderproduct["bsd_standardquantity"];

                        Entity new_orderproduct = new Entity(salesorderdetail.LogicalName, salesorderdetail.Id);
                        new_orderproduct["bsd_suborderquantity"] = salesorderdetail_new_suborder_quantity;
                        new_orderproduct["bsd_residualquantity"] = salesorderdetail_new_suborder_quantity;
                        new_orderproduct["bsd_totalquantity"] = salesorderdetail_new_suborder_quantity * salesorderdetail_standardquantity;
                        myService.service.Update(new_orderproduct);
                        #endregion
                    }
                    else
                    {
                        decimal order_quantity = (decimal)salesorderdetail["quantity"];
                        decimal old_suborder_quantity = (decimal)salesorderdetail["bsd_suborderquantity"];

                        #region cập nhật salesorderdetail
                        Entity new_orderproduct = new Entity(salesorderdetail.LogicalName, salesorderdetail.Id);
                        decimal new_suborder_quantity = old_suborder_quantity + change_quantity;
                        new_orderproduct["bsd_suborderquantity"] = new_suborder_quantity;
                        new_orderproduct["bsd_remainingquantity"] = order_quantity - new_suborder_quantity;
                        myService.service.Update(new_orderproduct);
                        #endregion
                    }
                }
            }
            #endregion

            #endregion

            if (update_suborder) UpdateSubOrder(suborder);

        }
        public void DeleteSuborderProduct(Entity pre_suborder_product)
        {
            Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)pre_suborder_product["bsd_suborder"]).Id, new ColumnSet(true));
            bool multipleaddress = (bool)suborder["bsd_multipleaddress"];
            decimal type = ((OptionSetValue)suborder["bsd_type"]).Value;

            #region quote
            if (type == 861450001)
            {
                EntityReference quote_ref = (EntityReference)suborder["bsd_quote"];
                Entity quote = myService.service.Retrieve(quote_ref.LogicalName, quote_ref.Id, new ColumnSet(true));
                Entity quoteproduct = this.getQuoteDetailFromSuborderProduct(pre_suborder_product, 2);
                bool have_quantity = (bool)quote["bsd_havequantity"];
                myService.SetState(quote.Id, quote.LogicalName, 0, 1);
                if (multipleaddress && have_quantity == false)
                {
                    EntityReference product = (EntityReference)quoteproduct["productid"];

                    decimal shipquantity = (decimal)pre_suborder_product["bsd_shipquantity"];
                    decimal quantity = (decimal)quoteproduct["quantity"];
                    decimal old_suborderquantity = (decimal)quoteproduct["bsd_suborderquantity"];
                    decimal new_suborderquantity = old_suborderquantity - shipquantity;
                    decimal remaining_quantity = quantity - new_suborderquantity;


                    Entity new_quoteproduct = new Entity(quoteproduct.LogicalName, quoteproduct.Id);
                    new_quoteproduct["bsd_suborderquantity"] = new_suborderquantity;
                    new_quoteproduct["bsd_remainingquantity"] = remaining_quantity;
                    myService.service.Update(new_quoteproduct);

                    #region cập nhật line tổng
                    Entity orderproduct_totalline = Get_LineTotal_QuoteProduct_Quantity(product.Id, quote.Id);
                    decimal orderproduct_totalline_quantity = (decimal)orderproduct_totalline["bsd_quantity"];
                    decimal orderproduct_totalline_old_suborderquantity = (decimal)orderproduct_totalline["bsd_suborderquantity"];
                    decimal orderproduct_totalline_new_suborderquantity = orderproduct_totalline_old_suborderquantity - shipquantity;
                    decimal orderproduct_totalline_remainingquantity = orderproduct_totalline_quantity - orderproduct_totalline_new_suborderquantity;
                    Entity new_orderproduct_totalline = new Entity(orderproduct_totalline.LogicalName, orderproduct_totalline.Id);
                    new_orderproduct_totalline["bsd_suborderquantity"] = orderproduct_totalline_new_suborderquantity;
                    //new_orderproduct_totalline["bsd_residualquantity"] = orderproduct_totalline_new_suborderquantity;
                    new_orderproduct_totalline["bsd_remainingquantity"] = orderproduct_totalline_remainingquantity;
                    myService.Update(new_orderproduct_totalline);
                    #endregion

                    #region cập nhật line số lượng 0
                    decimal quotedetail_old_suborder_quantity = (decimal)quoteproduct["bsd_suborderquantity"];
                    decimal quotedetail_new_suborder_quantity = quotedetail_old_suborder_quantity - shipquantity;
                    decimal quotedetail_standardquantity = (decimal)quoteproduct["bsd_standardquantity"];
                    Entity new_orderproduct = new Entity(quoteproduct.LogicalName, quoteproduct.Id);
                    new_orderproduct["bsd_suborderquantity"] = quotedetail_new_suborder_quantity;
                    new_orderproduct["bsd_residualquantity"] = quotedetail_new_suborder_quantity;
                    new_orderproduct["bsd_totalquantity"] = quotedetail_new_suborder_quantity * quotedetail_standardquantity;
                    myService.service.Update(new_orderproduct);
                    #endregion
                }
                else
                {
                    decimal shipquantity = (decimal)pre_suborder_product["bsd_shipquantity"];
                    decimal quantity = (decimal)quoteproduct["quantity"];
                    decimal old_suborderquantity = (decimal)quoteproduct["bsd_suborderquantity"];
                    decimal new_suborderquantity = old_suborderquantity - shipquantity;
                    decimal remaining_quantity = quantity - new_suborderquantity;
                    Entity new_quoteproduct = new Entity(quoteproduct.LogicalName, quoteproduct.Id);
                    new_quoteproduct["bsd_suborderquantity"] = new_suborderquantity;
                    new_quoteproduct["bsd_remainingquantity"] = remaining_quantity;
                    myService.service.Update(new_quoteproduct);
                }

                #region Won Quote
                myService.SetState(quote.Id, quote.LogicalName, 1, 2);
                WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
                Entity quoteClose = new Entity("quoteclose");
                quoteClose.Attributes["quoteid"] = new EntityReference("quote", new Guid(quote.Id.ToString()));
                quoteClose.Attributes["subject"] = "Quote Close" + DateTime.Now.ToString();
                winQuoteRequest.QuoteClose = quoteClose;
                winQuoteRequest.Status = new OptionSetValue(-1);
                myService.service.Execute(winQuoteRequest);
                #endregion
            }
            #endregion

            #region order
            else if (type == 861450002)
            {
                EntityReference order_ref = (EntityReference)suborder["bsd_order"];
                Entity order = myService.service.Retrieve(order_ref.LogicalName, order_ref.Id, new ColumnSet(true));
                Entity salesorderdetail = this.getSalesorderDetailFromSuborderProduct(pre_suborder_product, 2);
                bool have_quantity = (bool)order["bsd_havequantity"];
                if (multipleaddress && have_quantity == false)
                {
                    EntityReference product = (EntityReference)salesorderdetail["productid"];
                    decimal shipquantity = (decimal)pre_suborder_product["bsd_shipquantity"];

                    #region cập nhật line tổng
                    Entity orderproduct_totalline = Get_LineTotal_OrderProduct_Quantity(product.Id, order.Id);
                    decimal orderproduct_totalline_quantity = (decimal)orderproduct_totalline["bsd_quantity"];
                    decimal orderproduct_totalline_old_suborderquantity = (decimal)orderproduct_totalline["bsd_suborderquantity"];
                    decimal orderproduct_totalline_new_suborderquantity = orderproduct_totalline_old_suborderquantity - shipquantity;
                    decimal orderproduct_totalline_remainingquantity = orderproduct_totalline_quantity - orderproduct_totalline_new_suborderquantity;
                    Entity new_orderproduct_totalline = new Entity(orderproduct_totalline.LogicalName, orderproduct_totalline.Id);
                    new_orderproduct_totalline["bsd_suborderquantity"] = orderproduct_totalline_new_suborderquantity;
                    //new_orderproduct_totalline["bsd_residualquantity"] = orderproduct_totalline_new_suborderquantity;
                    new_orderproduct_totalline["bsd_remainingquantity"] = orderproduct_totalline_remainingquantity;
                    myService.Update(new_orderproduct_totalline);
                    #endregion

                    #region cập nhật line số lượng 0
                    decimal salesorderdetail_old_suborder_quantity = (decimal)salesorderdetail["bsd_suborderquantity"];
                    decimal salesorderdetail_new_suborder_quantity = salesorderdetail_old_suborder_quantity - shipquantity;
                    decimal salesorderdetail_standardquantity = (decimal)salesorderdetail["bsd_standardquantity"];
                    Entity new_orderproduct = new Entity(salesorderdetail.LogicalName, salesorderdetail.Id);
                    new_orderproduct["bsd_suborderquantity"] = salesorderdetail_new_suborder_quantity;
                    new_orderproduct["bsd_residualquantity"] = salesorderdetail_new_suborder_quantity;
                    new_orderproduct["bsd_totalquantity"] = salesorderdetail_new_suborder_quantity * salesorderdetail_standardquantity;
                    myService.service.Update(new_orderproduct);
                    #endregion
                }
                else
                {
                    decimal shipquantity = (decimal)pre_suborder_product["bsd_shipquantity"];
                    decimal quantity = (decimal)salesorderdetail["quantity"];
                    decimal old_suborderquantity = (decimal)salesorderdetail["bsd_suborderquantity"];
                    decimal new_suborderquantity = old_suborderquantity - shipquantity;
                    decimal remaining_quantity = quantity - new_suborderquantity;

                    Entity new_orderproduct = new Entity(salesorderdetail.LogicalName, salesorderdetail.Id);
                    new_orderproduct["bsd_suborderquantity"] = new_suborderquantity;
                    new_orderproduct["bsd_remainingquantity"] = remaining_quantity;

                    myService.service.Update(new_orderproduct);
                }
            }
            #endregion

            UpdateSubOrder(suborder);
        }
        public void UpdateSubOrder(Entity suborder)
        {
            bool multiple_address = (bool)suborder["bsd_multipleaddress"];
            decimal total_tax = 0m;
            decimal detail_amount = 0m;
            decimal total_amount = 0m;
            decimal total_currency_exchange = 0;

            EntityCollection list_suborderproduct = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);

            bool flag_check_using_shipping = false;
            bool flat_check_using_porter = false;
            if (list_suborderproduct.Entities.Any())
            {
                foreach (var suborder_product in list_suborderproduct.Entities)
                {
                    if (suborder_product.HasValue("bsd_tax")) total_tax += ((Money)suborder_product["bsd_tax"]).Value;
                    detail_amount += ((Money)suborder_product["bsd_amount"]).Value;
                    total_amount += ((Money)suborder_product["bsd_extendedamount"]).Value;
                    total_currency_exchange += ((Money)suborder_product["bsd_currencyexchangecurrency"]).Value;

                    #region kiểm tra có sử dụng porter và shipping không 
                    if (multiple_address)
                    {
                        if (((Money)suborder_product["bsd_shippingprice"]).Value > 0)
                        {
                            flag_check_using_shipping = true;
                        }

                        if (((Money)suborder_product["bsd_porterprice"]).Value > 0)
                        {
                            flat_check_using_porter = true;
                        }
                        else
                        {
                            // giá bốc xếp phải móc thằng quote hoặc order lên để kiểm tra vì có trường hợp shipping + porter. thì co scái giá porter vẫn là 0, nên ko dùng kiểm tra đươc.
                            var type = ((OptionSetValue)suborder["bsd_type"]).Value;
                            if (type == 861450001) // quote
                            {
                                Entity quoteproduct = this.getQuoteDetailFromSuborderProduct(suborder_product, 2);
                                // nếu giá shipping bao gồm porter hoặc porter là yes.
                                if ((bool)quoteproduct["bsd_shippingporter"] || (bool)quoteproduct["bsd_porteroption"])
                                {
                                    flat_check_using_porter = true;
                                }
                            }
                            else if (type == 861450002) // order
                            {
                                Entity salesorderdetail = this.getSalesorderDetailFromSuborderProduct(suborder_product, 2);
                                // nếu giá shipping bao gồm porter hoặc porter là yes.
                                if ((bool)salesorderdetail["bsd_shippingporter"] || (bool)salesorderdetail["bsd_porteroption"])
                                {
                                    flat_check_using_porter = true;
                                }
                            }
                        }

                    }
                    #endregion

                }
            }
            Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
            new_suborder["bsd_detailamount"] = new Money(detail_amount);
            new_suborder["bsd_totaltax"] = new Money(total_tax);
            new_suborder["bsd_totalamount"] = new Money(total_amount);
            new_suborder["bsd_totalcurrencyexchange"] = new Money(total_currency_exchange);
            new_suborder["bsd_totalcurrencyexchangetext"] = total_currency_exchange.DecimalToStringHideSymbol();
            decimal new_debt = (suborder.HasValue("bsd_olddebt") ? ((Money)suborder["bsd_olddebt"]).Value : 0) + total_currency_exchange;
            new_suborder["bsd_newdebt"] = new Money(new_debt);
            #region cập nhật nếu là multiple address -> kiểm tra có sử dụng vận chuyện và porter
            if (multiple_address)
            {
                new_suborder["bsd_transportation"] = flag_check_using_shipping;
                new_suborder["bsd_porteroption"] = flat_check_using_porter;
            }
            #endregion
            myService.Update(new_suborder);
        }
        public Entity getQuoteDetailFromSuborderProduct(Entity suborder_product, int FormType)
        {
            Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)suborder_product["bsd_suborder"]).Id, new ColumnSet(true));
            bool multiple_address = (bool)suborder["bsd_multipleaddress"];
            EntityReference product = (EntityReference)suborder_product["bsd_product"];
            EntityReference quote = (EntityReference)suborder["bsd_quote"];

            if (multiple_address)
            {
                int deliveryfrom = ((OptionSetValue)suborder["bsd_deliveryfrom"]).Value;
                EntityReference fromaddress = (EntityReference)suborder["bsd_shippingfromaddress"];
                EntityReference toaddress = (EntityReference)suborder["bsd_shippingaddress"];
                EntityReference site_ref = (EntityReference)suborder["bsd_site"];
                EntityReference receipt_customer = (EntityReference)suborder["bsd_shiptoaccount"];
                EntityReference customer_address = (EntityReference)suborder["bsd_shiptoaddress"];
                bool shipping = false;
                if (FormType == 1)
                {
                    shipping = (bool)suborder_product["bsd_subordershipping"];
                }
                else
                {
                    shipping = ((Money)suborder_product["bsd_shippingprice"]).Value > 0 ? true : false;
                }
                return myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='quotedetail'>
                                <attribute name='bsd_shippingoption' />
                                <attribute name='quotedetailid' />
                                <attribute name='bsd_suborderquantity' />
                                <attribute name='bsd_standardquantity' />
                                <attribute name='bsd_shippingpricelistvalue' />
                                <attribute name='bsd_shippingdeliverymethod' />
                                <attribute name='bsd_remainingquantity' />
                                <attribute name='uomid' />
                                <attribute name='bsd_unitshipping' />
                                <attribute name='bsd_giashipsauthue' />
                                <attribute name='bsd_porterprice' />
                                <attribute name='quantity' />
                                <attribute name='bsd_quantity' />
                                <attribute name='bsd_shippingporter' />
                                <attribute name='bsd_porteroption' />
                                <attribute name='bsd_shippedquantity' />
                                <attribute name='quoteid' />
                                <attribute name='productid' />
                                <filter type='and'>
                                      <condition attribute='quoteid' operator='eq' uitype='quote' value='" + quote.Id + @"' />
                                      <condition attribute='productid' operator='eq' uitype='product' value='" + product.Id + @"' />
                                      <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + site_ref.Id + @"' />                              
                                      <condition attribute='bsd_deliveryfrom' operator='eq' value='" + deliveryfrom + @"' />                         
                                      <condition attribute='bsd_shippingfromaddress' operator='eq' uitype='bsd_address' value='" + fromaddress.Id + @"' />
                                      <condition attribute='bsd_shippingaddress' operator='eq' uitype='bsd_address' value='" + toaddress.Id + @"' />
                                      <condition attribute='bsd_partnerscompany' operator='eq' uitype='account' value='" + receipt_customer.Id + @"' />
                                      <condition attribute='bsd_shiptoaddress' operator='eq' uitype='bsd_address' value='" + customer_address.Id + @"' />
                                      <condition attribute='bsd_usingshipping' operator='eq' value='" + shipping + @"' />
                                </filter>
                              </entity>
                            </fetch>").Entities.FirstOrDefault();
            }
            else
            {
                return myService.FetchXml(
                        @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='quotedetail'>
                                <attribute name='bsd_shippingoption' />
                                <attribute name='quotedetailid' />
                                <attribute name='bsd_suborderquantity' />
                                <attribute name='bsd_standardquantity' />
                                <attribute name='bsd_remainingquantity' />
                                <attribute name='bsd_shippingpricelistvalue' />
                                <attribute name='bsd_shippingdeliverymethod' />
                                <attribute name='uomid' />
                                <attribute name='bsd_unitshipping' />
                                <attribute name='bsd_giashipsauthue' />
                                <attribute name='bsd_porterprice' />
                                <attribute name='quantity' />
                                <attribute name='bsd_shippingporter' />
                                <attribute name='bsd_porteroption' />
                                <attribute name='bsd_shippedquantity' />
                                <attribute name='quoteid' />
                                <attribute name='productid' />
                                <filter type='and'>
                                      <condition attribute='quoteid' operator='eq' uitype='quote' value='" + quote.Id + @"' />
                                      <condition attribute='productid' operator='eq' uitype='product' value='" + product.Id + @"' />
                                </filter>
                              </entity>
                            </fetch>").Entities.FirstOrDefault();
            }

        }
        public Entity getSalesorderDetailFromSuborderProduct(Entity suborder_product, int FormType)
        {
            Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)suborder_product["bsd_suborder"]).Id, new ColumnSet(true));
            bool multiple_address = (bool)suborder["bsd_multipleaddress"];
            EntityReference product = (EntityReference)suborder_product["bsd_product"];
            EntityReference order = (EntityReference)suborder["bsd_order"];
            if (multiple_address)
            {
                int deliveryfrom = ((OptionSetValue)suborder["bsd_deliveryfrom"]).Value;
                EntityReference fromaddress = (EntityReference)suborder["bsd_shippingfromaddress"];
                EntityReference toaddress = (EntityReference)suborder["bsd_shippingaddress"];
                EntityReference site_ref = (EntityReference)suborder["bsd_site"];
                EntityReference receipt_customer = (EntityReference)suborder["bsd_shiptoaccount"];
                EntityReference customer_address = (EntityReference)suborder["bsd_shiptoaddress"];
                bool shipping = false;

                if (FormType == 1)
                {
                    shipping = (bool)suborder_product["bsd_subordershipping"];
                }
                else
                {
                    shipping = ((Money)suborder_product["bsd_shippingprice"]).Value > 0 ? true : false;
                }

                return myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='salesorderdetail'>
                                <attribute name='bsd_shippingoption' />
                                <attribute name='salesorderdetailid' />
                                <attribute name='bsd_suborderquantity' />
                                <attribute name='bsd_standardquantity' />
                                <attribute name='bsd_remainingquantity' />
                                <attribute name='bsd_shippingpricelistvalue' />
                                <attribute name='bsd_shippingdeliverymethod' />
                                <attribute name='uomid' />
                                <attribute name='bsd_unitshipping' />
                                <attribute name='bsd_giashipsauthue' />
                                <attribute name='bsd_giashipsauthue_full' />
                                <attribute name='bsd_porterprice' />
                                <attribute name='bsd_porterprice_full' />
                                <attribute name='quantity' />
                                <attribute name='bsd_quantity' />
                                <attribute name='bsd_shippingporter' />
                                <attribute name='bsd_porteroption' />
                                <attribute name='bsd_shippedquantity' />
                                <attribute name='salesorderid' />
                                <attribute name='productid' />
                                <filter type='and'>
                                      <condition attribute='salesorderid' operator='eq' uitype='salesorder' value='" + order.Id + @"' />
                                      <condition attribute='productid' operator='eq' uitype='product' value='" + product.Id + @"' />
                                      <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + site_ref.Id + @"' />                              
                                      <condition attribute='bsd_deliveryfrom' operator='eq' value='" + deliveryfrom + @"' />                         
                                      <condition attribute='bsd_shippingfromaddress' operator='eq' uitype='bsd_address' value='" + fromaddress.Id + @"' />
                                      <condition attribute='bsd_shippingaddress' operator='eq' uitype='bsd_address' value='" + toaddress.Id + @"' />
                                      <condition attribute='bsd_shiptoaccount' operator='eq' uitype='account' value='" + receipt_customer.Id + @"' />
                                      <condition attribute='bsd_shiptoaddress' operator='eq' uitype='bsd_address' value='" + customer_address.Id + @"' />
                                      <condition attribute='bsd_usingshipping' operator='eq' value='" + shipping + @"' />
                                </filter>
                              </entity>
                            </fetch>").Entities.FirstOrDefault();
            }
            else
            {
                return myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='salesorderdetail'>
                                <attribute name='bsd_shippingoption' />
                                <attribute name='salesorderdetailid' />
                                <attribute name='bsd_suborderquantity' />
                                <attribute name='bsd_standardquantity' />
                                <attribute name='bsd_remainingquantity' />
                                <attribute name='bsd_shippingpricelistvalue' />
                                <attribute name='bsd_shippingdeliverymethod' />
                                <attribute name='uomid' />
                                <attribute name='bsd_unitshipping' />
                                <attribute name='bsd_giashipsauthue' />
                                <attribute name='bsd_giashipsauthue_full' />
                                <attribute name='bsd_porterprice' />
                                <attribute name='bsd_porterprice_full' />
                                <attribute name='quantity' />
                                <attribute name='bsd_shippingporter' />
                                <attribute name='bsd_porteroption' />
                                <attribute name='bsd_shippedquantity' />
                                <attribute name='salesorderid' />
                                <attribute name='productid' />
                                <filter type='and'>
                                      <condition attribute='salesorderid' operator='eq' uitype='salesorder' value='" + order.Id + @"' />
                                      <condition attribute='productid' operator='eq' uitype='product' value='" + product.Id + @"' />
                                </filter>
                              </entity>
                            </fetch>").Entities.FirstOrDefault();
            }

        }

        //huy
        public Entity getAppendixContractdetailFromSuborderProduct(Entity suborder_product, int FormType)
        {
            Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)suborder_product["bsd_suborder"]).Id, new ColumnSet(true));
            bool multiple_address = (bool)suborder["bsd_multipleaddress"];
            int deliveryfrom = ((OptionSetValue)suborder["bsd_deliveryfrom"]).Value;
            EntityReference product = (EntityReference)suborder_product["bsd_product"];
            EntityReference appendixcontract = (EntityReference)suborder["bsd_appendixcontract"];
            if (multiple_address)
            {
                EntityReference site_ref = (EntityReference)suborder["bsd_site"];
                EntityReference siteaddress_ref = (EntityReference)suborder["bsd_siteaddress"];
                EntityReference fromaddress = (EntityReference)suborder["bsd_shippingfromaddress"];
                EntityReference toaddress = (EntityReference)suborder["bsd_shippingaddress"];
                EntityReference receipt_customer = (EntityReference)suborder["bsd_shiptoaccount"];
                EntityReference customer_address = (EntityReference)suborder["bsd_shiptoaddress"];
                bool shipping = false;
                if (FormType == 1)
                {
                    shipping = (bool)suborder_product["bsd_subordershipping"];
                }
                else
                {
                    shipping = ((Money)suborder_product["bsd_shippingprice"]).Value > 0 ? true : false;
                }
                return myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_appendixcontractdetail'>
                                <all-attributes />
                                <filter type='and'>
                                  <condition attribute='bsd_appendixcontract' operator='eq' uitype='bsd_appendixcontract' value='" + appendixcontract.Id + @"' />
                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product.Id + @"' />
                                  <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + site_ref.Id + @"' />                              
                                  <condition attribute='bsd_deliveryfrom' operator='eq' value='" + deliveryfrom + @"' />                         
                                  <condition attribute='bsd_shippingfromaddress' operator='eq' uitype='bsd_address' value='" + fromaddress.Id + @"' />
                                  <condition attribute='bsd_shippingaddress' operator='eq' uitype='bsd_address' value='" + toaddress.Id + @"' />
                                  <condition attribute='bsd_shiptoaccount' operator='eq' uitype='account' value='" + receipt_customer.Id + @"' />
                                  <condition attribute='bsd_shiptoaddress' operator='eq' uitype='bsd_address' value='" + customer_address.Id + @"' />
                                  <condition attribute='bsd_usingshipping' operator='eq' value='" + shipping + @"' />                                             
                                </filter>
                              </entity>
                            </fetch>").Entities.FirstOrDefault();
            }
            else
            {
                return myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_appendixcontractdetail'>
                                <all-attributes />
                                <filter type='and'>
                                      <condition attribute='bsd_appendixcontract' operator='eq' uitype='bsd_appendixcontract' value='" + appendixcontract.Id + @"' />
                                      <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product.Id + @"' />
                                </filter>
                              </entity>
                            </fetch>").Entities.FirstOrDefault();
            }
        }
        public string GetRoleName(Guid userId, IOrganizationService service)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                  <entity name='systemuser'>
                                    <attribute name='systemuserid' />
                                    <order attribute='fullname' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='isdisabled' operator='eq' value='0' />
                                       <condition attribute='systemuserid' operator='eq' uitype='systemuser' value='" + userId + @"' />
                                    </filter>
                                    <link-entity name='systemuserroles' from='systemuserid' to='systemuserid' visible='false' intersect='true'>
                                      <link-entity name='role' from='roleid' to='roleid'>
                                         <attribute name='name' alias='role' />
                                      </link-entity>
                                    </link-entity>
                                  </entity>
                                </fetch>";
            EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(xml));
            if (list.Entities.Any())
            {
                AliasedValue role_aliasedvalue = (AliasedValue)list.Entities.First()["role"];
                return role_aliasedvalue.Value.ToString();
            }
            else return null;
        }
        public Entity GetCustomerDebtByTimeAndAccount(Guid accountid, DateTime date)
        {
            date = date.AddMinutes(1);
            string date_str = date.Year + "-" + (date.Month < 10 ? "0" + date.Month : date.Month.ToString()) + "-" + (date.Day < 10 ? "0" + date.Day : date.Day.ToString()) + " " + date.Hour + ":" + date.Minute + ":" + date.Second;
            //throw new Exception(date.ToString("yyyy-MM-dd HH:mm:ss"));
            string xml = @"<fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_customerdebt'>
                                        <attribute name='bsd_customerdebtid' />
                                        <attribute name='bsd_credithold' />
                                        <attribute name='bsd_amount' />
                                        <attribute name='bsd_newdebt' />
                                        <order attribute='modifiedon' descending='true' />
                                        <filter type='and'>',
                                          <condition attribute='bsd_account' operator='eq' uitype='account' value='" + accountid + @"' />
                                          <condition attribute='modifiedon' operator='le' value='" + date_str + @"' />
                                        </filter>',
                                  </entity>',
                            </fetch>";
            // lessthan or equal
            EntityCollection list_customerdebt = myService.service.RetrieveMultiple(new FetchExpression(xml));
            if (list_customerdebt.Entities.Any())
                return list_customerdebt.Entities.First();
            return null;
        }
        public Entity Get_LineTotal_OrderProduct_Quantity(Guid productid, Guid orderid)
        {
            // lấy line tổng. !
            EntityCollection list_salesorderdetail = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='salesorderdetail'>
                            <attribute name='productid' />
                            <attribute name='salesorderid' />
                            <attribute name='bsd_suborderquantity' />
                            <attribute name='bsd_remainingquantity' />
                            <attribute name='bsd_residualquantity' />
                            <attribute name='bsd_shippedquantity' />
                            <attribute name='bsd_standardquantity' />
                            <attribute name='bsd_totalquantity' />
                            <attribute name='priceperunit' />
                            <attribute name='bsd_quantity' />
                            <attribute name='extendedamount' />
                            <attribute name='salesorderdetailid' />
                            <filter type='and'>
                              <condition attribute='productid' operator='eq' uitype='product' value='" + productid + @"' />
                              <condition attribute='salesorderid' operator='eq' uitype='salesorder' value='" + orderid + @"' />
                              <condition attribute='bsd_quantity' operator='gt' value='0' />
                            </filter>
                          </entity>
                        </fetch>");
            if (list_salesorderdetail.Entities.Any())
            {
                return list_salesorderdetail.Entities.First();
            }
            else return null;
        }
        public Entity Get_LineTotal_QuoteProduct_Quantity(Guid productid, Guid quoteid)
        {
            // lấy line tổng. !
            EntityCollection list_quotedetail = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='quotedetail'>
                            <attribute name='productid' />
                            <attribute name='quoteid' />
                            <attribute name='bsd_suborderquantity' />
                            <attribute name='bsd_remainingquantity' />
                            <attribute name='bsd_residualquantity' />
                            <attribute name='bsd_shippedquantity' />
                            <attribute name='bsd_standardquantity' />
                            <attribute name='bsd_totalquantity' />
                            <attribute name='priceperunit' />
                            <attribute name='bsd_quantity' />
                            <attribute name='extendedamount' />
                            <attribute name='quotedetailid' />
                            <filter type='and'>
                              <condition attribute='productid' operator='eq' uitype='product' value='" + productid + @"' />
                              <condition attribute='quoteid' operator='eq' uitype='quoteid' value='" + quoteid + @"' />
                              <condition attribute='bsd_quantity' operator='gt' value='0' />
                            </filter>
                          </entity>
                        </fetch>");
            if (list_quotedetail.Entities.Any())
            {
                return list_quotedetail.Entities.First();
            }
            else return null;
        }
        //Phong 2-2-2018 apprve
        public static void Stage_Aprrove(IOrganizationService service, string entityName, Guid RecordID, Guid User, Guid StageId, string traversedpath, string attributeName, int attributeValue
                                        , string approveperson, string Approvedate, Entity updatedStage)
        {
            Entity suborder = service.Retrieve(entityName, RecordID, new ColumnSet(true));
            string fetchketoan = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                              <entity name='systemuser'>
                                <attribute name='fullname' />
                                <attribute name='businessunitid' />
                                <attribute name='title' />
                                <attribute name='address1_telephone1' />
                                <attribute name='positionid' />
                                <attribute name='systemuserid' />
                                <order attribute='fullname' descending='false' />
                                <filter type='and'>
                                  <condition attribute='isdisabled' operator='eq' value='0' />
                                </filter>
                                <link-entity name='systemuserroles' from='systemuserid' to='systemuserid' visible='false' intersect='true'>
                                  <link-entity name='role' from='roleid' to='roleid' alias='ah'>
                                    <filter type='and'>
                                      <condition attribute='name' operator='like' value='%Phòng Kế Toán%' />
                                    </filter>
                                  </link-entity>
                                </link-entity>
                              </entity>
                            </fetch>";
            EntityCollection etc_ketoan = service.RetrieveMultiple(new FetchExpression(fetchketoan));
            if (etc_ketoan.Entities.Any())
            {
                foreach (var itemsketoan in etc_ketoan.Entities)
                {
                    if (itemsketoan["systemuserid"].Equals(User))
                    {

                        updatedStage.Id = RecordID;
                        updatedStage["stageid"] = StageId;
                        updatedStage["traversedpath"] = traversedpath;// context.InputParameters["StageId"].ToString();
                        updatedStage["bsd_stageid"] = StageId.ToString();
                        updatedStage[attributeName] = new OptionSetValue(attributeValue);
                        updatedStage["bsd_rejectnumber"] = "Approve";
                        updatedStage["bsd_duyet"] = true;
                        updatedStage["bsd_congno"] = true;
                        updatedStage["" + approveperson + ""] = new EntityReference("systemuser", User);
                        updatedStage["" + Approvedate + ""] = RetrieveLocalTimeFromUTCTimeStatic(DateTime.Now, service);
                        //string fetchsavemultihistory = @"
                        //               <fetch mapping='logical'>
                        //                 <entity name='bsd_savemultihistory'>       
                        //                   <all-attributes />     
                        //                   <filter type='and'>                              
                        //                          <condition attribute='bsd_id' operator='eq' value='" + "{" + RecordID.ToString().ToUpper() + "}" + @"' />
                        //                           <condition attribute='bsd_type' operator='eq' value='861450000' />
                        //                       </filter>     
                        //                 </entity> 
                        //               </fetch> ";


                        //EntityCollection listsavemultihistory = service.RetrieveMultiple(new FetchExpression(fetchsavemultihistory));

                        //if (listsavemultihistory.Entities.Any())
                        //{

                        //    Entity multisavehistory = new Entity("bsd_savemultihistory", listsavemultihistory.Entities.First().Id);
                        //    multisavehistory["bsd_type"] = new OptionSetValue(861450001);
                        //    service.Update(multisavehistory);

                        //}
                        service.Update(updatedStage);
                    }
                    else if (!itemsketoan["systemuserid"].Equals(User))
                    {

                        updatedStage.Id = RecordID;
                        updatedStage["stageid"] = StageId;
                        updatedStage["traversedpath"] = traversedpath;// context.InputParameters["StageId"].ToString();
                        updatedStage["bsd_stageid"] = StageId.ToString();
                        updatedStage[attributeName] = new OptionSetValue(attributeValue);
                        updatedStage["bsd_rejectnumber"] = "Approve";
                        updatedStage["" + approveperson + ""] = new EntityReference("systemuser", User);
                        updatedStage["" + Approvedate + ""] = RetrieveLocalTimeFromUTCTimeStatic(DateTime.Now, service);
                        //string fetchsavemultihistory = @"
                        //               <fetch mapping='logical'>
                        //                 <entity name='bsd_savemultihistory'>       
                        //                   <all-attributes />     
                        //                   <filter type='and'>                              
                        //                          <condition attribute='bsd_id' operator='eq' value='" + "{" + RecordID.ToString().ToUpper() + "}" + @"' />
                        //                           <condition attribute='bsd_type' operator='eq' value='861450000' />
                        //                       </filter>     
                        //                 </entity> 
                        //               </fetch> ";


                        //EntityCollection listsavemultihistory = service.RetrieveMultiple(new FetchExpression(fetchsavemultihistory));

                        //if (listsavemultihistory.Entities.Any())
                        //{

                        //    Entity multisavehistory = new Entity("bsd_savemultihistory", listsavemultihistory.Entities.First().Id);
                        //    multisavehistory["bsd_type"] = new OptionSetValue(861450001);
                        //    service.Update(multisavehistory);

                        //}
                        service.Update(updatedStage);
                    }
                }
            }
        }

        public DateTime RetrieveLocalTimeFromUTCTime(DateTime utcTime, IOrganizationService service)
        {
            int? timeZoneCode = RetrieveCurrentUsersSettings(service);
            if (!timeZoneCode.HasValue)
                throw new Exception("Can't find time zone code");
            var request = new LocalTimeFromUtcTimeRequest
            {
                TimeZoneCode = timeZoneCode.Value,
                UtcTime = utcTime.ToUniversalTime()
            };
            var response = (LocalTimeFromUtcTimeResponse)service.Execute(request);
            return response.LocalTime;
            //var utcTime = utcTime.ToString("MM/dd/yyyy HH:mm:ss");
            //var localDateOnly = response.LocalTime.ToString("dd-MM-yyyy");
        }
        public static DateTime RetrieveLocalTimeFromUTCTimeStatic(DateTime utcTime, IOrganizationService service)
        {
            int? timeZoneCode = RetrieveCurrentUsersSettingsStatic(service);
            if (!timeZoneCode.HasValue)
                throw new Exception("Can't find time zone code");
            var request = new LocalTimeFromUtcTimeRequest
            {
                TimeZoneCode = timeZoneCode.Value,
                UtcTime = utcTime.ToUniversalTime()
            };
            var response = (LocalTimeFromUtcTimeResponse)service.Execute(request);
            return response.LocalTime;
            //var utcTime = utcTime.ToString("MM/dd/yyyy HH:mm:ss");
            //var localDateOnly = response.LocalTime.ToString("dd-MM-yyyy");
        }
        public int? RetrieveCurrentUsersSettings(IOrganizationService service)
        {

            var currentUserSettings = service.RetrieveMultiple(
            new QueryExpression("usersettings")
            {
                ColumnSet = new ColumnSet("localeid", "timezonecode"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
            new ConditionExpression("systemuserid", ConditionOperator.EqualUserId)
            }
                }
            }).Entities[0].ToEntity<Entity>();

            return (int?)currentUserSettings.Attributes["timezonecode"];
        }
        public static int? RetrieveCurrentUsersSettingsStatic(IOrganizationService service)
        {

            var currentUserSettings = service.RetrieveMultiple(
            new QueryExpression("usersettings")
            {
                ColumnSet = new ColumnSet("localeid", "timezonecode"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
            new ConditionExpression("systemuserid", ConditionOperator.EqualUserId)
            }
                }
            }).Entities[0].ToEntity<Entity>();

            return (int?)currentUserSettings.Attributes["timezonecode"];
        }

    }
}
