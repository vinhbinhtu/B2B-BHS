using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugin.Service;

namespace Plugin_ReturnOrder
{
    public class ReturnOrderProduct : IPlugin
    {
        private MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider, true);
            if (myService.context.Depth > 1) return;

            if (myService.context.MessageName == "Create")
            {
                try
                {
                    Entity target = myService.getTarget();
                    if (target.HasValue("bsd_product"))
                    {
                            if (CheckReturnType(target) == 861450002)
                            {
                                CreateReturnOrderProductForReturnNoResource(target);
                            }
                            else
                                CreateReturnOrderProduct(target);
                    }
                    else
                    {
                        throw new Exception("You have must input product!");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }

            #region Update
            else if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();
                Entity returnorder_product = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                EntityReference returnorder_ref = (EntityReference)returnorder_product["bsd_returnorderid"];
                Entity returnorder = myService.service.Retrieve(returnorder_ref.LogicalName, returnorder_ref.Id, new ColumnSet(true));
                if (CheckReturnType(returnorder_product) != 861450002)
                {
                    if (returnorder_product.HasValue("bsd_orderquantity"))
                    {
                        UpdateReturnOrderProduct(returnorder, returnorder_product);
                    }
                }
                else
                {
                    if(target.HasValue("bsd_orderquantity") || target.HasValue("bsd_giatruocthue"))
                    {
                        CreateReturnOrderProductForReturnNoResource(returnorder_product);
                    }
                }

            }
            #endregion

            #region Delete
            else if (myService.context.MessageName == "Delete")
            {
                Entity ReturnOrderProduct = myService.context.PreEntityImages["PreImage"];
                decimal total_amount = 0m;
                decimal detail_amount = 0m;
                decimal total_tax = 0m;

                EntityReference returnorder_ref = (EntityReference)ReturnOrderProduct["bsd_returnorderid"];
                Entity returnorder = myService.service.Retrieve(returnorder_ref.LogicalName, returnorder_ref.Id, new ColumnSet("bsd_status"));
                // throw new Exception(((OptionSetValue)returnorder["bsd_status"]).Value + "ooo" + ReturnOrderProduct["bsd_orderquantity"]);
                if (((OptionSetValue)returnorder["bsd_status"]).Value != 861450000)
                {
                    throw new Exception("Order created Suborder. Can not delete.");
                }
                EntityCollection list_returnorderproduct = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_returnorderproduct'>
                            <attribute name='bsd_returnorderproductid' />
                            <attribute name='bsd_tax' />
                            <attribute name='bsd_amount' />
                            <attribute name='bsd_extendedamount' />
                            <filter type='and'>
                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                            </filter>
                          </entity>
                        </fetch>", returnorder_ref.Id)));

                foreach (var item in list_returnorderproduct.Entities)
                {
                    total_amount += ((Money)item["bsd_extendedamount"]).Value;
                    detail_amount += ((Money)item["bsd_amount"]).Value;
                    if (item.HasValue("bsd_tax"))
                    {
                        total_tax += ((Money)item["bsd_tax"]).Value;
                    }
                }
                //throw new Exception("ok");
                Entity newreturnorder = new Entity(returnorder_ref.LogicalName, returnorder_ref.Id);
                newreturnorder["bsd_detailamount"] = new Money(detail_amount);
                newreturnorder["bsd_totaltax"] = new Money(total_tax);
                newreturnorder["bsd_totalamount"] = new Money(total_amount);
                myService.service.Update(newreturnorder);

            }
            #endregion
        }
        //vinhlh 06/08/2018 Create Return Order Product For Return No Resource
        public int CheckReturnType(Entity target)
        {
            int result = 861450002;
            EntityReference returnorder_ref = (EntityReference)target["bsd_returnorderid"];
            Entity returnorder = myService.service.Retrieve(returnorder_ref.LogicalName, returnorder_ref.Id, new ColumnSet("bsd_type"));
            if (returnorder.HasValue("bsd_type")) result = ((OptionSetValue)returnorder["bsd_type"]).Value;
            return result;
        }
        public void CreateReturnOrderProductForReturnNoResource(Entity ReturnOrderProduct)
        {
            try
            {
         
                EntityReference returnorder_ref = (EntityReference)ReturnOrderProduct["bsd_returnorderid"];
                Entity returnorder = myService.service.Retrieve(returnorder_ref.LogicalName, returnorder_ref.Id, new ColumnSet(true));
                EntityReference product_ref = (EntityReference)ReturnOrderProduct["bsd_product"];
                Entity product = myService.service.Retrieve(product_ref.LogicalName, product_ref.Id, new ColumnSet(true));
                decimal quantity = (decimal)ReturnOrderProduct["bsd_orderquantity"];
                decimal giasauthue = 0m;
                decimal standard_quantity = 1m;
                decimal total_quantity = 0m;
                decimal vat = 0m;
                bool existUnitConversion = false;

                if (!existUnitConversion) standard_quantity = 1;// (decimal)suborderproduct["bsd_standardquantity"];


                Entity Update_returnorderproduct = new Entity(ReturnOrderProduct.LogicalName, ReturnOrderProduct.Id);
                total_quantity = standard_quantity * quantity;
                Update_returnorderproduct["bsd_orderquantity"] = quantity;
                Update_returnorderproduct["bsd_remainningquantity"] = quantity;
                Update_returnorderproduct["bsd_shipquantity"] = (-1) * quantity;//* shippedquantity;
                Update_returnorderproduct["bsd_standardquantity"] = standard_quantity;
                Update_returnorderproduct["bsd_totalquantity"] = total_quantity;
                Update_returnorderproduct["bsd_name"] = product_ref.Name;
                Update_returnorderproduct["bsd_unit"] = (EntityReference)product["defaultuomid"];
                Update_returnorderproduct["transactioncurrencyid"] = (EntityReference)returnorder["transactioncurrencyid"];
                Update_returnorderproduct["exchangerate"] = (decimal)returnorder["bsd_exchangerate"];
                decimal bsd_itemsalestax = (decimal)ReturnOrderProduct["bsd_itemsalestax"];
                Update_returnorderproduct["bsd_usingtax"] = true;//suborderproduct["bsd_usingtax"];


                
                decimal giatruocthue = ((Money)ReturnOrderProduct["bsd_giatruocthue"]).Value;//((Money)suborderproduct["bsd_giatruocthue"]).Value;                                                                     
                vat = giatruocthue * (bsd_itemsalestax / 100);
                Update_returnorderproduct["bsd_vatprice"] =new Money(vat);//suborderproduct["bsd_vatprice"];
                decimal tax = giatruocthue * (bsd_itemsalestax / 100) * quantity;
                Update_returnorderproduct["bsd_tax"] = new Money(tax);
                decimal amount = giatruocthue * quantity;
                Update_returnorderproduct["bsd_amount"] = new Money(amount);
                giasauthue = giatruocthue + vat;
                Update_returnorderproduct["bsd_giasauthue"] = new Money(giasauthue);
                decimal extendedamount = (giatruocthue + vat) * quantity;
                Update_returnorderproduct["bsd_extendedamount"] = new Money(extendedamount);
                Update_returnorderproduct["bsd_priceperunit"] = new Money(giatruocthue + vat);//suborderproduct["bsd_priceperunit"];      // gán priceperunit vào để đẩy qua khi tạo suborder TH bán
                decimal currencyexchange = (decimal)returnorder["bsd_exchangerate"] * extendedamount;//* ((Money)suborderproduct["bsd_currencyexchangecurrency"]).Value;
                Update_returnorderproduct["bsd_currencyexchange"] = new Money(currencyexchange);
                myService.service.Update(Update_returnorderproduct);
                //  myService.service.Update(Update_returnorderproduct);
               
                #region Cap nhat Return Order

                decimal total_amount = 0m;
                decimal detail_amount = 0m;
                decimal total_tax = 0m;
              
                EntityCollection list_returnorderproduct = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_returnorderproduct'>
                            <attribute name='bsd_returnorderproductid' />
                            <attribute name='bsd_tax' />
                            <attribute name='bsd_amount' />
                            <attribute name='bsd_extendedamount' />
                            <filter type='and'>
                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                            </filter>
                          </entity>
                        </fetch>", returnorder.Id)));
                foreach (var item in list_returnorderproduct.Entities)
                {
                    if (item.HasValue("bsd_extendedamount")) total_amount += ((Money)item["bsd_extendedamount"]).Value;
                    if (item.HasValue("bsd_amount")) detail_amount += ((Money)item["bsd_amount"]).Value;
                    if (item.HasValue("bsd_tax"))
                    {
                        total_tax += ((Money)item["bsd_tax"]).Value;
                    }
                }
             
                Entity newreturnorder = new Entity(returnorder.LogicalName, returnorder.Id);
                newreturnorder["bsd_detailamount"] = new Money(detail_amount);
                newreturnorder["bsd_totaltax"] = new Money(total_tax);
                newreturnorder["bsd_totalamount"] = new Money(total_amount);
                myService.service.Update(newreturnorder);

                #endregion
                
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        //end vinhlh
        public void UpdateReturnOrderProduct(Entity ReturnOrder, Entity ReturnOrderProduct)
        {
            try
            {
                if (ReturnOrderProduct.HasValue("bsd_orderquantity"))
                {
                    decimal standard_quantity = 1m;
                    decimal total_quantity = 0m;
                    decimal quantity = (decimal)ReturnOrderProduct["bsd_orderquantity"];
                    decimal quantity_recevied = 0m;
                    decimal quantity_suborder = 0m;
                    decimal bsd_exchangeratevalue = 1m;
                    EntityReference unit = (EntityReference)ReturnOrderProduct["bsd_unit"];
                    EntityReference product = (EntityReference)ReturnOrderProduct["bsd_product"];
                    EntityReference unitdefault = (EntityReference)ReturnOrder["bsd_unitdefault"];
                    //lấy số lượng đã trả
                    EntityCollection entity_recevied = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_returnorderproduct'>
                                                        <attribute name='bsd_returnorderproductid' />
                                                        <attribute name='bsd_orderquantity' />
                                                        <order attribute='bsd_orderquantity' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_findsuborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                                                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                                                            <condition attribute='bsd_deliverynote' operator='eq'  uitype='bsd_deliverynote' value='" + ((EntityReference)ReturnOrderProduct["bsd_deliverynote"]).Id + @"' />
                                                          <condition attribute='bsd_returnorderproductid' operator='ne' uitype='bsd_returnorderproduct' value='{2}' />                                                    
                                                        </filter>
                                                         <link-entity name='bsd_returnorder' from='bsd_returnorderid' to='bsd_returnorderid' alias='ad'>
                                                              <filter type='and'>
                                                                <condition attribute='statecode' operator='eq' value='0' />
                                                              </filter>
                                                          </link-entity>
                                                      </entity>
                                                    </fetch>", ((EntityReference)ReturnOrder["bsd_findsuborder"]).Id, product.Id, ReturnOrderProduct.Id)));
                    if (entity_recevied.Entities.Count > 0)
                    {
                        //throw new Exception("co1");
                        foreach (var cus in entity_recevied.Entities)
                        {
                            quantity_recevied += (decimal)cus["bsd_orderquantity"];
                        }
                    }
                    //lấy số lượng xuất kho (delivery note product)
                    //EntityCollection entity_suborder = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    //                          <entity name='bsd_suborderproduct'>
                    //                            <attribute name='bsd_suborderproductid' />
                    //                            <attribute name='bsd_shipquantity' />
                    //                            <filter type='and'>
                    //                              <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                    //                              <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                    //                            </filter>
                    //                          </entity>
                    //                        </fetch>", ((EntityReference)ReturnOrder["bsd_findsuborder"]).Id, product.Id)));
                    //if (entity_suborder.Entities.Count>0)
                    //{
                    //    //throw new Exception("co2");
                    //    foreach(var cus in entity_suborder.Entities)
                    //    {
                    //        quantity_suborder += (decimal)cus["bsd_shipquantity"];
                    //    }
                    //}
                    EntityCollection list_deliverynote = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' aggregate='true'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <attribute name='bsd_quantity' aggregate='sum' alias='bsd_quantityship' />
                                                                <attribute name='bsd_product' groupby='true' alias='bsd_product' />
                                                                <filter type='and'>
                                                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='{0}' />
                                                                    <condition attribute='bsd_deliverynote' operator='eq'  uitype='bsd_deliverynote' value='" + ((EntityReference)ReturnOrderProduct["bsd_deliverynote"]).Id + @"' />
                                                                </filter>
                                                                <link-entity name='bsd_deliverynote' from='bsd_deliverynoteid' to='bsd_deliverynote' alias='bi'>
                                                                  <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='bj'>
                                                                    <link-entity name='bsd_deliveryplan' from='bsd_deliveryplanid' to='bsd_deliveryplan' alias='bk'>
                                                                      <filter type='and'>
                                                                        <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='{1}' />
                                                                      </filter>
                                                                    </link-entity>
                                                                  </link-entity>
                                                                </link-entity>
                                                              </entity>
                                                            </fetch>", product.Id, ((EntityReference)ReturnOrder["bsd_findsuborder"]).Id)));
                    if (list_deliverynote.Entities.Count > 0)
                    {
                        quantity_suborder = (decimal)((AliasedValue)list_deliverynote[0]["bsd_quantityship"]).Value;
                    }
                    if (Math.Abs(quantity + quantity_recevied) > quantity_suborder)
                    {
                        throw new Exception("Số lượng trả về không được vượt quá số lượng trong đơn hàng");

                    }
                    decimal price_per_unit = ReturnOrderProduct.HasValue("bsd_priceperunit") ? ((Money)ReturnOrderProduct["bsd_priceperunit"]).Value : 0m;

                    decimal? item_sales_tax = ReturnOrderProduct.HasValue("bsd_itemsalestax") ? (decimal?)ReturnOrderProduct["bsd_itemsalestax"] : 0m;
                    bool check_using_tax = ReturnOrderProduct.HasValue("bsd_usingtax") ? (bool)ReturnOrderProduct["bsd_usingtax"] : false;

                    bool existUnitConversion = false;
                    standard_quantity = getFactorUnitDefault(unit.Id, unitdefault.Id, product.Id, ref existUnitConversion);
                    total_quantity = standard_quantity * quantity;
                    #region Tinh Tien
                    if (ReturnOrder.HasValue("exchangerate")) bsd_exchangeratevalue = (decimal)ReturnOrder["exchangerate"];
                    decimal giatruocthue = ReturnOrderProduct.HasValue("bsd_giatruocthue") ? ((Money)ReturnOrderProduct["bsd_giatruocthue"]).Value : 0m;
                    //decimal vat = ReturnOrderProduct.HasValue("bsd_vatprice") ? ((Money)ReturnOrderProduct["bsd_vatprice"]).Value : 0m;
                    decimal itemVAT = ReturnOrderProduct.HasValue("bsd_itemsalestax") ? (decimal)ReturnOrderProduct["bsd_itemsalestax"] : 0m;
                    decimal vat = giatruocthue * itemVAT / 100; // new
                    decimal tax = vat * quantity;
                    //decimal giasauthue = ReturnOrderProduct.HasValue("bsd_giasauthue") ? ((Money)ReturnOrderProduct["bsd_giasauthue"]).Value : 0m;
                    decimal giasauthue = giatruocthue + vat; // new
                    decimal amount = giatruocthue * quantity;
                    decimal extendedamount = giasauthue * quantity;

                    #endregion

                    #region Cap Nhap Return Product

                    Entity newTarget = new Entity(ReturnOrderProduct.LogicalName, ReturnOrderProduct.Id);
                    newTarget["bsd_standardquantity"] = standard_quantity;//
                    newTarget["bsd_totalquantity"] = total_quantity;//
                    newTarget["bsd_remainningquantity"] = quantity;
                    //newTarget["bsd_porterprice"] = new Money(porter_price);//
                    //newTarget["bsd_giatruocthue"] = new Money(giatruocthue);//
                    newTarget["bsd_giasauthue"] = new Money(giasauthue);// mở lại
                    newTarget["bsd_amount"] = new Money(amount);//
                    newTarget["bsd_extendedamount"] = new Money(extendedamount);//
                    newTarget["bsd_currencyexchange"] = new Money(extendedamount * bsd_exchangeratevalue);
                    newTarget["bsd_tax"] = new Money(tax); //
                    newTarget["bsd_vatprice"] = new Money(vat); // new: update bsd_giatruocthue -> update bsd_vatprice
                    myService.service.Update(newTarget);

                    #endregion

                    #region Cap nhat Return Order

                    decimal total_amount = 0m;
                    decimal detail_amount = 0m;
                    decimal total_tax = 0m;
                    EntityReference parent = (EntityReference)ReturnOrderProduct["bsd_returnorderid"];

                    EntityCollection list_returnorderproduct = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_returnorderproduct'>
                            <attribute name='bsd_returnorderproductid' />
                            <attribute name='bsd_tax' />
                            <attribute name='bsd_amount' />
                            <attribute name='bsd_extendedamount' />
                            <filter type='and'>
                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                            </filter>
                          </entity>
                        </fetch>", parent.Id)));

                    foreach (var item in list_returnorderproduct.Entities)
                    {
                        total_amount += ((Money)item["bsd_extendedamount"]).Value;
                        detail_amount += ((Money)item["bsd_amount"]).Value;
                        if (item.HasValue("bsd_tax"))
                        {
                            total_tax += ((Money)item["bsd_tax"]).Value;
                        }
                    }
                    Entity newreturnorder = new Entity(parent.LogicalName, parent.Id);
                    newreturnorder["bsd_detailamount"] = new Money(detail_amount);
                    newreturnorder["bsd_totaltax"] = new Money(total_tax);
                    newreturnorder["bsd_totalamount"] = new Money(total_amount);
                    myService.service.Update(newreturnorder);

                    #endregion
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        public decimal getFactorUnitDefault(Guid UnitId, Guid UnitdefaultId, Guid ProductId, ref bool ExistUnitConversion)
        {
            decimal factor = 1;
            if (UnitId.Equals(UnitdefaultId))
            {
                factor = 1;
                ExistUnitConversion = true;
            }
            else
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
                                    </fetch>", ProductId, UnitId, UnitdefaultId)));

                if (list_unitconversion.Entities.Any())
                {
                    var unit_conversion = list_unitconversion.Entities.First();
                    factor = (decimal)unit_conversion["bsd_factor"];
                    ExistUnitConversion = true;
                }
                else
                {
                    factor = 1;
                    ExistUnitConversion = false;
                }
            }
            return factor;
        }

        public void CreateReturnOrderProduct(Entity ReturnOrderProduct)
        {

            try
            {
                EntityReference returnorder_ref = (EntityReference)ReturnOrderProduct["bsd_returnorderid"];
                Entity returnorder = myService.service.Retrieve(returnorder_ref.LogicalName, returnorder_ref.Id, new ColumnSet(true));
                EntityReference suborder_ref = (EntityReference)returnorder["bsd_findsuborder"];
                EntityReference deliverynote_ref = (EntityReference)returnorder["bsd_deliverynote"];
                EntityReference product_ref = (EntityReference)ReturnOrderProduct["bsd_product"];
                //throw new Exception(product_ref.Id + "  " );

                decimal order_quantity = (decimal)ReturnOrderProduct["bsd_orderquantity"];
                string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_suborderproduct'>
                                            <all-attributes />
                                            <filter type='and'>
                                              <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                                              <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                                              <condition attribute='bsd_shippedquantity' operator='gt' value='0' />
                                              <condition attribute='bsd_deliverystatus' operator='in'>
                                                <value>861450003</value>
                                                <value>861450002</value>
                                              </condition>
                                            </filter>
                                          </entity>
                                        </fetch>", suborder_ref.Id, product_ref.Id);
                EntityCollection subproduct_list = myService.service.RetrieveMultiple(new FetchExpression(xml));
                if (subproduct_list.Entities.Any())
                {
                    Entity suborderproduct = subproduct_list.Entities.First();
                    decimal giasauthue = 0m;
                    decimal requantity = 0m;
                    decimal quantity = 0m;
                    decimal standard_quantity = 1m;
                    decimal total_quantity = 0m;
                    decimal vat = 0m;
                    decimal shippedquantity = suborderproduct.HasValue("bsd_shippedquantity") ? (decimal)suborderproduct["bsd_shippedquantity"] : 0m;
                    //throw new Exception("1");
                    bool existUnitConversion = false;

                    if (!existUnitConversion) standard_quantity = (decimal)suborderproduct["bsd_standardquantity"];

                    xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_returnorderproduct'>
                                                        <attribute name='bsd_returnorderproductid' />
                                                        <attribute name='bsd_orderquantity' />
                                                        <order attribute='bsd_orderquantity' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_findsuborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                                                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />                                
                                                        </filter>
                                                         <link-entity name='bsd_returnorder' from='bsd_returnorderid' to='bsd_returnorderid' alias='ad'>
                                                              <filter type='and'>
                                                                <condition attribute='statecode' operator='eq' value='0' />
                                                              </filter>
                                                          </link-entity>
                                                      </entity>
                                                    </fetch>", suborder_ref.Id, product_ref.Id);
                    EntityCollection entity_recevied = myService.service.RetrieveMultiple(new FetchExpression(xml));

                    if (entity_recevied.Entities.Count > 0)
                    {
                        decimal quantity_recevied = 0m;
                        foreach (var cus in entity_recevied.Entities)
                        {
                            quantity_recevied += (decimal)cus["bsd_orderquantity"];
                        }
                        requantity = (shippedquantity - Math.Abs(quantity_recevied));
                        throw new Exception(quantity_recevied.ToString());
                    }
                    else
                    {
                        requantity = shippedquantity;

                    }

                    if (requantity <= 0)
                    {
                        throw new Exception("The suborder has out of quantity to create return order!");
                    }

                    Entity Update_returnorderproduct = new Entity(ReturnOrderProduct.LogicalName, ReturnOrderProduct.Id);
                    if (requantity > Math.Abs(order_quantity) && order_quantity != 0)
                    {
                        quantity = order_quantity;
                    }
                    else
                    {
                        quantity = (-1) * requantity;
                    }
                    total_quantity = standard_quantity * quantity;
                    if (Math.Abs(order_quantity) > quantity)
                    {
                        Update_returnorderproduct["bsd_orderquantity"] = quantity;
                    }
                    Update_returnorderproduct["bsd_remainningquantity"] = quantity;
                    Update_returnorderproduct["bsd_shipquantity"] = (-1) * shippedquantity;
                    Update_returnorderproduct["bsd_standardquantity"] = standard_quantity;
                    Update_returnorderproduct["bsd_totalquantity"] = total_quantity;
                    Update_returnorderproduct["bsd_name"] = product_ref.Name;
                    if (suborderproduct.HasValue("bsd_unit")) Update_returnorderproduct["bsd_unit"] = suborderproduct["bsd_unit"];
                    if (suborderproduct.HasValue("transactioncurrencyid")) Update_returnorderproduct["transactioncurrencyid"] = suborderproduct["transactioncurrencyid"];
                    if (suborderproduct.HasValue("exchangerate")) Update_returnorderproduct["exchangerate"] = suborderproduct["exchangerate"];
                    if (suborderproduct.HasValue("bsd_itemsalestax")) Update_returnorderproduct["bsd_itemsalestax"] = suborderproduct["bsd_itemsalestax"];
                    if (suborderproduct.HasValue("bsd_usingtax")) Update_returnorderproduct["bsd_usingtax"] = suborderproduct["bsd_usingtax"];
                    if (suborderproduct.HasValue("bsd_currencyexchangecurrency"))
                    {
                        decimal currencyexchange = (-1) * ((Money)suborderproduct["bsd_currencyexchangecurrency"]).Value;
                        Update_returnorderproduct["bsd_currencyexchange"] = new Money(currencyexchange);
                    }
                    if (suborderproduct.HasValue("bsd_vatprice"))
                    {
                        Update_returnorderproduct["bsd_vatprice"] = suborderproduct["bsd_vatprice"];
                        vat = ((Money)suborderproduct["bsd_vatprice"]).Value;
                        decimal tax = vat * quantity;
                        Update_returnorderproduct["bsd_tax"] = new Money(tax);
                    }
                    if (suborderproduct.HasValue("bsd_giatruocthue"))
                    {
                        decimal giatruocthue = ((Money)suborderproduct["bsd_giatruocthue"]).Value;
                        Update_returnorderproduct["bsd_giatruocthue"] = suborderproduct["bsd_giatruocthue"];
                        decimal amount = giatruocthue * quantity;
                        Update_returnorderproduct["bsd_amount"] = new Money(amount);

                        giasauthue = ((Money)suborderproduct["bsd_giatruocthue"]).Value + vat;
                        Update_returnorderproduct["bsd_giasauthue"] = new Money(giasauthue);
                        decimal extendedamount = giasauthue * quantity;
                        Update_returnorderproduct["bsd_extendedamount"] = new Money(extendedamount);
                    }
                    if (suborderproduct.HasValue("bsd_priceperunit")) Update_returnorderproduct["bsd_priceperunit"] = suborderproduct["bsd_priceperunit"];      // gán priceperunit vào để đẩy qua khi tạo suborder TH bán
                    if (suborderproduct.HasValue("bsd_shippingprice")) Update_returnorderproduct["bsd_shippingprice"] = suborderproduct["bsd_shippingprice"];
                    if (suborderproduct.HasValue("bsd_porterprice")) Update_returnorderproduct["bsd_porterprice"] = suborderproduct["bsd_porterprice"];
                    Update_returnorderproduct["bsd_findsuborder"] = new EntityReference("bsd_suborder", suborder_ref.Id);
                    Update_returnorderproduct["bsd_deliverynote"] = new EntityReference(deliverynote_ref.LogicalName, deliverynote_ref.Id);

                    myService.service.Update(Update_returnorderproduct);

                    #region Cap nhat Return Order

                    decimal total_amount = 0m;
                    decimal detail_amount = 0m;
                    decimal total_tax = 0m;
                    EntityReference parent = (EntityReference)ReturnOrderProduct["bsd_returnorderid"];

                    EntityCollection list_returnorderproduct = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_returnorderproduct'>
                            <attribute name='bsd_returnorderproductid' />
                            <attribute name='bsd_tax' />
                            <attribute name='bsd_amount' />
                            <attribute name='bsd_extendedamount' />
                            <filter type='and'>
                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                            </filter>
                          </entity>
                        </fetch>", parent.Id)));

                    foreach (var item in list_returnorderproduct.Entities)
                    {
                        total_amount += ((Money)item["bsd_extendedamount"]).Value;
                        detail_amount += ((Money)item["bsd_amount"]).Value;
                        if (item.HasValue("bsd_tax"))
                        {
                            total_tax += ((Money)item["bsd_tax"]).Value;
                        }
                    }
                    Entity newreturnorder = new Entity(parent.LogicalName, parent.Id);
                    newreturnorder["bsd_detailamount"] = new Money(detail_amount);
                    newreturnorder["bsd_totaltax"] = new Money(total_tax);
                    newreturnorder["bsd_totalamount"] = new Money(total_amount);
                    myService.service.Update(newreturnorder);

                    #endregion
                }
                else
                {
                    throw new Exception("The suborder has out of quantity to create return order!");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}

