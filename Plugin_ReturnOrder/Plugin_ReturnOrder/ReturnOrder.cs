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
    public class ReturnOrder : IPlugin
    {
        private MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider, true);
            // IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            #region Create

            if (myService.context.MessageName == "Create")
            {
              
                Entity target = myService.getTarget();
                Entity returnorder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                string test = 0.ToString();
                try
                {
                    if (target.Contains("bsd_findsuborder") && target["bsd_findsuborder"] != null)
                    {
                        EntityReference findsuborder_ref = (EntityReference)target["bsd_findsuborder"];
                        test = 1.ToString();
                        CreateReturnOrderProduct(findsuborder_ref.Id, returnorder.Id, returnorder, target);
                        test = 2.ToString();
                        UpdateTotalAmountProduct(target);
                        test = 3.ToString();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message + test);
                }
            }

            #endregion

            #region Update

            else if (myService.context.MessageName == "Update")
            {
                // Entity PreImage = (Entity)context.PreEntityImages["PreImage"];
                Entity target = myService.getTarget();
                Entity returnorder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                if (target.Contains("bsd_findsuborder"))
                {
                    //throw new Exception("okie1");
                    #region Co Suborder
                    if (target["bsd_findsuborder"] != null)
                    {
                        //throw new Exception("co");
                        string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_returnorderproduct'>
                            <attribute name='bsd_returnorderproductid' />
                            <attribute name='bsd_name' />
                            <order attribute='bsd_name' descending='false' />
                            <filter type='and'>
                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                            </filter>
                          </entity>
                        </fetch>", returnorder.Id, ((EntityReference)target["bsd_findsuborder"]).Id);

                        EntityCollection list_returnorder = myService.service.RetrieveMultiple(new FetchExpression(xml));
                        if (list_returnorder.Entities.Count > 0)
                        {
                            foreach (var returnproduct in list_returnorder.Entities)
                            {
                                myService.service.Delete("bsd_returnorderproduct", returnproduct.Id);
                            }
                        }
                        EntityReference findsuborder_ref = (EntityReference)target["bsd_findsuborder"];
                        if (target.HasValue("bsd_deliverynote"))
                        {
                            EntityReference deliveryNote = (EntityReference)target["bsd_deliverynote"];
                            UpdateReturnOrderProduct(findsuborder_ref.Id, returnorder.Id, returnorder, target, true, deliveryNote.Id);
                        }
                        else
                        {
                            UpdateReturnOrderProduct(findsuborder_ref.Id, returnorder.Id, returnorder, target, false, Guid.Empty);
                        }
                        UpdateTotalAmountProduct(target);
                    }
                    #endregion

                    #region Khong co Suborder

                    else
                    {
                        //throw new Exception("ko");
                        string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_returnorderproduct'>
                            <attribute name='bsd_returnorderproductid' />
                            <attribute name='bsd_name' />
                            <order attribute='bsd_name' descending='false' />
                            <filter type='and'>
                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                            </filter>
                          </entity>
                        </fetch>", returnorder.Id);

                        EntityCollection list_returnorder = myService.service.RetrieveMultiple(new FetchExpression(xml));
                        if (list_returnorder.Entities.Count > 0)
                        {
                            foreach (var returnproduct in list_returnorder.Entities)
                            {
                                myService.service.Delete("bsd_returnorderproduct", returnproduct.Id);
                            }
                        }
                        UpdateTotalAmountProduct(target);
                    }
                    #endregion

                }
                else
                {
                    //throw new Exception((target["bsd_deliverynote"] != null).ToString());

                    if (target.Contains("bsd_deliverynote"))
                    {
                        #region
                        string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_returnorderproduct'>
                            <attribute name='bsd_returnorderproductid' />
                            <attribute name='bsd_name' />
                            <order attribute='bsd_name' descending='false' />
                            <filter type='and'>
                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                            </filter>
                          </entity>
                        </fetch>", returnorder.Id, ((EntityReference)returnorder["bsd_findsuborder"]).Id);

                        EntityCollection list_returnorder = myService.service.RetrieveMultiple(new FetchExpression(xml));
                        if (list_returnorder.Entities.Count > 0)
                        {
                            foreach (var returnproduct in list_returnorder.Entities)
                            {
                                myService.service.Delete("bsd_returnorderproduct", returnproduct.Id);
                            }
                        }
                        EntityReference findsuborder_ref = (EntityReference)returnorder["bsd_findsuborder"];
                        if (target["bsd_deliverynote"] != null)
                        {
                            EntityReference deliveryNote = (EntityReference)target["bsd_deliverynote"];
                            UpdateReturnOrderProduct(findsuborder_ref.Id, returnorder.Id, returnorder, target, true, deliveryNote.Id);
                        }
                        else UpdateReturnOrderProduct(findsuborder_ref.Id, returnorder.Id, returnorder, target, false, Guid.Empty);
                        UpdateTotalAmountProduct(target);
                        #endregion
                    }


                }

            }
            #endregion

        }

        public void CreateReturnOrderProduct(Guid suborderid, Guid returnorderid, Entity returnorder, Entity target)
        {
            #region
            string test = 0.ToString();
            int i_flat = 0;
            int i_flat_am = 0;
            int i_lstproduct = 0;
            try
            {
               
                EntityCollection list_suborderproduct;
                list_suborderproduct = getSubOrderProduct(suborderid);
                if (list_suborderproduct.Entities.Any())
                {
                    i_lstproduct = list_suborderproduct.Entities.Count;
                    foreach (var suborderproduct in list_suborderproduct.Entities)
                    {
                        #region
                        decimal giasauthue = 0m;
                        decimal requantity = 0m;
                        decimal quantity = 0m;
                        decimal standard_quantity = 1m;
                        decimal total_quantity = 0m;
                        decimal vat = 0m;
                        decimal vat2 = 0m;
                        //decimal shippedquantity = suborderproduct.HasValue("bsd_shippedquantity") ? (decimal)suborderproduct["bsd_shippedquantity"] : 0m;
                        decimal shippedquantity = 0m;/// Nudo 7-11-17: lấy số lượng dựa vào delivery note
                        EntityReference unit = (EntityReference)suborderproduct["bsd_unit"];
                        EntityReference product = (EntityReference)suborderproduct["bsd_product"];
                        EntityReference unitdefault = (EntityReference)returnorder["bsd_unitdefault"];
                        bool existUnitConversion = false;
                        standard_quantity = getFactorUnitDefault(unit.Id, unitdefault.Id, product.Id, ref existUnitConversion);

                        if (!existUnitConversion) standard_quantity = (decimal)suborderproduct["bsd_standardquantity"];
                        EntityCollection list_deliverynote;
                        EntityReference deliveryNote = (EntityReference)target["bsd_deliverynote"];
                        list_deliverynote = getDeliveryNote(suborderid, product.Id, deliveryNote.Id);
                       
                        #region lst Delivery Note
                        if (list_deliverynote.Entities.Count > 0)
                        {
                            Entity NoteProduct = list_deliverynote.Entities.First();
                            // Entity NoteProduct_SubOrder = list_deliverynote_suborder.Entities.First();
                            //shippedquantity_suborder = NoteProduct_SubOrder.HasValue("bsd_quantity") ? (decimal)((AliasedValue)NoteProduct_SubOrder["bsd_quantity"]).Value : 0m;
                            shippedquantity = NoteProduct.HasValue("bsd_quantity") ? (decimal)((AliasedValue)NoteProduct["bsd_quantity"]).Value : 0m;
                            EntityCollection entity_recevied = getReturnOrderDeliveryNote(suborderid, product.Id, deliveryNote.Id);
                           // throw new Exception("oke "+ entity_recevied.Entities.Count);
                            if (entity_recevied.Entities.Count > 0)
                            {
                                decimal quantity_recevied = 0m;
                                foreach (var cus in entity_recevied.Entities)
                                {
                                    quantity_recevied += (decimal)cus["bsd_orderquantity"];
                                }

                                requantity = (shippedquantity - Math.Abs(quantity_recevied));
                            }
                            else
                            {
                                requantity = shippedquantity;
                            }
                            if (requantity == 0)
                            {
                                i_flat++;
                            }
                            #endregion
                        }
                       
                        if (requantity > 0)
                        {
                            #region
                            quantity = (-1) * requantity;
                            Entity new_returnorderproduct = new Entity("bsd_returnorderproduct");

                            total_quantity = standard_quantity * quantity;
                            new_returnorderproduct["bsd_orderquantity"] = quantity;
                            new_returnorderproduct["bsd_remainningquantity"] = quantity;
                            new_returnorderproduct["bsd_shipquantity"] = (-1) * shippedquantity;       //số lượng đã giao trên suborder
                            new_returnorderproduct["bsd_standardquantity"] = standard_quantity;
                            new_returnorderproduct["bsd_totalquantity"] = total_quantity;
                            new_returnorderproduct["bsd_returnorderid"] = new EntityReference("bsd_returnorder", returnorderid);
                            if (suborderproduct.HasValue("bsd_productid"))
                            {
                                new_returnorderproduct["bsd_name"] = suborderproduct["bsd_productid"];
                                new_returnorderproduct["bsd_productid"] = suborderproduct["bsd_productid"];
                            }
                            else if (suborderproduct.HasValue("bsd_product")) new_returnorderproduct["bsd_product"] = suborderproduct["bsd_product"];
                          
                            if (suborderproduct.HasValue("bsd_unit")) new_returnorderproduct["bsd_unit"] = suborderproduct["bsd_unit"];
                            if (suborderproduct.HasValue("bsd_product")) new_returnorderproduct["bsd_product"] = suborderproduct["bsd_product"];
                            if (suborderproduct.HasValue("transactioncurrencyid")) new_returnorderproduct["transactioncurrencyid"] = suborderproduct["transactioncurrencyid"];
                            if (suborderproduct.HasValue("exchangerate")) new_returnorderproduct["exchangerate"] = suborderproduct["exchangerate"];
                            if (suborderproduct.HasValue("bsd_itemsalestax")) new_returnorderproduct["bsd_itemsalestax"] = suborderproduct["bsd_itemsalestax"];
                            if (suborderproduct.HasValue("bsd_usingtax")) new_returnorderproduct["bsd_usingtax"] = suborderproduct["bsd_usingtax"];
                           
                            if (suborderproduct.HasValue("bsd_currencyexchange"))
                            {
                                decimal currencyexchange = (-1) * (decimal)suborderproduct["bsd_currencyexchange"];
                                new_returnorderproduct["bsd_currencyexchange"] = new Money(currencyexchange);
                            }
                          
                            //throw new Exception("4");
                            if (suborderproduct.HasValue("bsd_vatprice"))
                            {
                                new_returnorderproduct["bsd_vatprice"] = suborderproduct["bsd_vatprice"];
                                vat = ((Money)suborderproduct["bsd_vatprice"]).Value;
                                vat2 = (((Money)suborderproduct["bsd_giatruocthue"]).Value / 100) * (decimal)suborderproduct["bsd_itemsalestax"];
                                decimal tax = vat2 * quantity;
                                new_returnorderproduct["bsd_tax"] = new Money(tax);
                            }
                           
                            if (suborderproduct.HasValue("bsd_giatruocthue"))
                            {
                                decimal giatruocthue = ((Money)suborderproduct["bsd_giatruocthue"]).Value;
                                new_returnorderproduct["bsd_giatruocthue"] = suborderproduct["bsd_giatruocthue"];
                                decimal amount = giatruocthue * quantity;
                                new_returnorderproduct["bsd_amount"] = new Money(amount);
                                if (!suborderproduct.HasValue("bsd_itemsalestax")) throw new Exception(suborderproduct["bsd_productid"]+" Item Sales Tax is null value");
                                vat2 = (((Money)suborderproduct["bsd_giatruocthue"]).Value / 100) * (decimal)suborderproduct["bsd_itemsalestax"];
                                giasauthue = ((Money)suborderproduct["bsd_giatruocthue"]).Value + vat2;
                                new_returnorderproduct["bsd_giasauthue"] = new Money(giasauthue);
                                decimal extendedamount = giasauthue * quantity;
                                new_returnorderproduct["bsd_extendedamount"] = new Money(extendedamount);
                            }

                            if (suborderproduct.HasValue("bsd_itemsalestaxgroup")) new_returnorderproduct["bsd_itemsalestaxgroup"] = suborderproduct["bsd_itemsalestaxgroup"];
                            if (suborderproduct.HasValue("bsd_priceperunit")) new_returnorderproduct["bsd_priceperunit"] = suborderproduct["bsd_priceperunit"];      // gán priceperunit vào để đẩy qua khi tạo suborder TH bán
                            if (suborderproduct.HasValue("bsd_shippingprice")) new_returnorderproduct["bsd_shippingprice"] = suborderproduct["bsd_shippingprice"];
                            if (suborderproduct.HasValue("bsd_porterprice")) new_returnorderproduct["bsd_porterprice"] = suborderproduct["bsd_porterprice"];
                            new_returnorderproduct["bsd_findsuborder"] = new EntityReference("bsd_suborder", suborderid);
                            if (target.HasValue("bsd_deliverynote"))
                            {
                                new_returnorderproduct["bsd_deliverynote"] = new EntityReference("bsd_deliverynote", deliveryNote.Id);
                            }
                            myService.service.Create(new_returnorderproduct);
                            // throw new Exception("okie1");
                            #endregion
                        }
                        else i_flat_am++;
                  
                        #endregion
                    }
                    if (i_lstproduct == i_flat || i_lstproduct == i_flat_am)
                    {
                        throw new Exception("The order has out of quantity to create return order!");
                    }
                }
                else throw new Exception("Suborder does'nt exist list product or shipped quantity less than 0");

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            #endregion
        }
        public void UpdateReturnOrderProduct(Guid suborderid, Guid returnorderid, Entity returnorder, Entity target, bool b_deliveryNote, Guid delievryNoteId)
        {
            #region
            string test = 0.ToString();
            int i_flat = 0;
            int i_flat_am = 0;
            int i_lstproduct = 0;
            try
            {
                EntityCollection list_suborderproduct;
                list_suborderproduct = getSubOrderProduct(suborderid);

                if (list_suborderproduct.Entities.Count > 0)
                {
                    i_lstproduct = list_suborderproduct.Entities.Count;
                    foreach (var suborderproduct in list_suborderproduct.Entities)
                    {
                        #region
                        decimal giasauthue = 0m;
                        decimal requantity = 0m;
                        decimal quantity = 0m;
                        decimal standard_quantity = 1m;
                        decimal total_quantity = 0m;
                        decimal vat = 0m;
                        test = 5.ToString();

                        //decimal shippedquantity = suborderproduct.HasValue("bsd_shippedquantity") ? (decimal)suborderproduct["bsd_shippedquantity"] : 0m;
                        decimal shippedquantity = 0m;/// Nudo 7-11-17: lấy số lượng dựa vào delivery note
                        EntityReference unit = (EntityReference)suborderproduct["bsd_unit"];
                        EntityReference product = (EntityReference)suborderproduct["bsd_product"];
                        EntityReference unitdefault = (EntityReference)returnorder["bsd_unitdefault"];
                        bool existUnitConversion = false;

                        standard_quantity = getFactorUnitDefault(unit.Id, unitdefault.Id, product.Id, ref existUnitConversion);

                        if (!existUnitConversion) standard_quantity = (decimal)suborderproduct["bsd_standardquantity"];
                        EntityCollection list_deliverynote;
                        // EntityCollection list_deliverynote_suborder = getDeliveryNote(suborderid, product.Id);
                        if (b_deliveryNote == false)
                        {
                            // throw new Exception("1");
                            list_deliverynote = getDeliveryNote(suborderid, product.Id);
                        }
                        else
                        {
                            // throw new Exception("2");
                            list_deliverynote = getDeliveryNote(suborderid, product.Id, delievryNoteId);
                        }
                        #region lst Delivery Note

                        if (list_deliverynote.Entities.Count > 0)
                        {
                            Entity NoteProduct = list_deliverynote.Entities.First();
                            //Entity NoteProduct_SubOrder = list_deliverynote_suborder.Entities.First();
                            //  shippedquantity_suborder = NoteProduct_SubOrder.HasValue("bsd_quantity") ? (decimal)((AliasedValue)NoteProduct_SubOrder["bsd_quantity"]).Value : 0m;
                            shippedquantity = NoteProduct.HasValue("bsd_quantity") ? (decimal)((AliasedValue)NoteProduct["bsd_quantity"]).Value : 0m;
                            EntityCollection entity_recevied = getReturnOrderDeliveryNote(suborderid, product.Id, delievryNoteId);
                            if (entity_recevied.Entities.Count > 0)
                            {
                                decimal quantity_recevied = 0m;
                                foreach (var cus in entity_recevied.Entities)
                                {
                                    if (cus.HasValue("bsd_orderquantity"))
                                        quantity_recevied += (decimal)cus["bsd_orderquantity"];
                                }
                                requantity = (shippedquantity - Math.Abs(quantity_recevied));

                            }
                            else
                            {
                                requantity = shippedquantity;
                            }
                            if (requantity == 0)
                            {
                                // throw new Exception("The order has out of quantity to create return order!");
                                i_flat++;
                            }
                        }


                        #endregion

                        if (requantity > 0)
                        {
                            #region
                            quantity = (-1) * requantity;
                            Entity new_returnorderproduct = new Entity("bsd_returnorderproduct");
                            test = "1";
                            total_quantity = standard_quantity * quantity;
                            new_returnorderproduct["bsd_orderquantity"] = quantity;
                            new_returnorderproduct["bsd_remainningquantity"] = quantity;
                            new_returnorderproduct["bsd_shipquantity"] = (-1) * shippedquantity;       //số lượng đã giao trên suborder
                            new_returnorderproduct["bsd_standardquantity"] = standard_quantity;
                            new_returnorderproduct["bsd_totalquantity"] = total_quantity;
                            new_returnorderproduct["bsd_returnorderid"] = new EntityReference("bsd_returnorder", returnorderid);
                            if (suborderproduct.HasValue("bsd_productid"))
                            {
                                new_returnorderproduct["bsd_name"] = suborderproduct["bsd_productid"];
                                new_returnorderproduct["bsd_productid"] = suborderproduct["bsd_productid"];
                            }

                            if (suborderproduct.HasValue("bsd_product")) new_returnorderproduct["bsd_product"] = suborderproduct["bsd_product"];
                            test = "2";
                            if (suborderproduct.HasValue("bsd_unit")) new_returnorderproduct["bsd_unit"] = suborderproduct["bsd_unit"];
                            if (suborderproduct.HasValue("bsd_product")) new_returnorderproduct["bsd_product"] = suborderproduct["bsd_product"];
                            if (suborderproduct.HasValue("transactioncurrencyid")) new_returnorderproduct["transactioncurrencyid"] = suborderproduct["transactioncurrencyid"];
                            if (suborderproduct.HasValue("exchangerate")) new_returnorderproduct["exchangerate"] = suborderproduct["exchangerate"];
                            if (suborderproduct.HasValue("bsd_itemsalestax")) new_returnorderproduct["bsd_itemsalestax"] = suborderproduct["bsd_itemsalestax"];
                            if (suborderproduct.HasValue("bsd_usingtax")) new_returnorderproduct["bsd_usingtax"] = suborderproduct["bsd_usingtax"];
                            if (suborderproduct.HasValue("bsd_currencyexchange"))
                            {
                                decimal currencyexchange = (-1) * (decimal)suborderproduct["bsd_currencyexchange"];
                                new_returnorderproduct["bsd_currencyexchange"] = new Money(currencyexchange);
                            }
                            //throw new Exception("4");
                            test = "3";
                            if (suborderproduct.HasValue("bsd_vatprice"))
                            {
                                new_returnorderproduct["bsd_vatprice"] = suborderproduct["bsd_vatprice"];
                                vat = ((Money)suborderproduct["bsd_vatprice"]).Value;
                                decimal tax = vat * quantity;
                                new_returnorderproduct["bsd_tax"] = new Money(tax);
                            }
                            if (suborderproduct.HasValue("bsd_giatruocthue"))
                            {
                                decimal giatruocthue = ((Money)suborderproduct["bsd_giatruocthue"]).Value;
                                new_returnorderproduct["bsd_giatruocthue"] = suborderproduct["bsd_giatruocthue"];
                                decimal amount = giatruocthue * quantity;
                                new_returnorderproduct["bsd_amount"] = new Money(amount);

                                giasauthue = ((Money)suborderproduct["bsd_giatruocthue"]).Value + vat;
                                new_returnorderproduct["bsd_giasauthue"] = new Money(giasauthue);
                                decimal extendedamount = giasauthue * quantity;
                                new_returnorderproduct["bsd_extendedamount"] = new Money(extendedamount);
                            }
                            test = "4";
                            if (suborderproduct.HasValue("bsd_priceperunit")) new_returnorderproduct["bsd_priceperunit"] = suborderproduct["bsd_priceperunit"];      // gán priceperunit vào để đẩy qua khi tạo suborder TH bán
                            if (suborderproduct.HasValue("bsd_shippingprice")) new_returnorderproduct["bsd_shippingprice"] = suborderproduct["bsd_shippingprice"];
                            if (suborderproduct.HasValue("bsd_porterprice")) new_returnorderproduct["bsd_porterprice"] = suborderproduct["bsd_porterprice"];
                            new_returnorderproduct["bsd_findsuborder"] = new EntityReference("bsd_suborder", suborderid);
                            if (b_deliveryNote)
                            {
                                new_returnorderproduct["bsd_deliverynote"] = new EntityReference("bsd_deliverynote", delievryNoteId);
                            }
                            myService.service.Create(new_returnorderproduct);
                            test = "5";
                            // throw new Exception("okie1");
                            #endregion
                        }
                        else i_flat_am++;


                        #endregion
                    }
                    if (i_lstproduct == i_flat || i_lstproduct == i_flat_am)
                    {
                        throw new Exception("The order has out of quantity to create return order!");
                    }
                }
                else throw new Exception("Suborder does'nt exist list product or shipped quantity less than 0");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            #endregion
        }

        public void UpdateTotalAmountProduct(Entity returnorder)
        {
           
            try
            {
                string trace = "";
                decimal total_amount = 0m;
                decimal detail_amount = 0m;
                decimal total_tax = 0m;

                Entity newreturnorder = new Entity(returnorder.LogicalName, returnorder.Id);

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

                trace = "1";
                foreach (var item in list_returnorderproduct.Entities)
                {
                    total_amount += ((Money)item["bsd_extendedamount"]).Value;
                    detail_amount += ((Money)item["bsd_amount"]).Value;
                    if (item.HasValue("bsd_tax"))
                    {
                        total_tax += ((Money)item["bsd_tax"]).Value;
                    }
                }
                trace = "2";
                if (returnorder.HasValue("bsd_findsuborder"))
                {
                    EntityReference findsuborder_ref = (EntityReference)returnorder["bsd_findsuborder"];
                    Entity suborder = myService.service.Retrieve(findsuborder_ref.LogicalName, findsuborder_ref.Id, new ColumnSet(true));
                    if (suborder.HasValue("transactioncurrencyid"))
                    {
                        newreturnorder["transactioncurrencyid"] = suborder["transactioncurrencyid"];
                        newreturnorder["bsd_exchangeratevalue"] = suborder["bsd_exchangeratevalue"];
                    }
                    else
                        newreturnorder["transactioncurrencyid"] = null;
                    if (suborder.HasValue("bsd_pricelist"))
                        newreturnorder["bsd_pricelist"] = suborder["bsd_pricelist"];
                    else
                        newreturnorder["bsd_pricelist"] = null;
                }
                trace = "3";
                newreturnorder["bsd_detailamount"] = new Money(detail_amount);
                newreturnorder["bsd_totaltax"] = new Money(total_tax);
                newreturnorder["bsd_totalamount"] = new Money(total_amount);
                trace = "4";
                myService.service.Update(newreturnorder);
                trace = "5";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public decimal getFactorUnitDefault(Guid UnitId, Guid UnitdefaultId, Guid ProductId, ref bool ExistUnitConversion)
        {
            //throw new Exception(ProductId.ToString());
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
        public EntityCollection getDeliveryNote(Guid subOrder, Guid product)
        {
            string xmlnote = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' aggregate='true'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <attribute name='bsd_quantity' aggregate='sum' alias='bsd_quantity' />
                                                                <attribute name='bsd_product' groupby='true' alias='bsd_product' />
                                                                <filter type='and'>
                                                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='{0}' />
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
                                                            </fetch>", product, subOrder);
            // throw new Exception("xml: "+xmlnote);
            return myService.service.RetrieveMultiple(new FetchExpression(xmlnote));
        }
        public EntityCollection getDeliveryNote(Guid subOrder, Guid product, Guid deliveryNote)
        {
            string xmlnote = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' aggregate='true'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <attribute name='bsd_quantity' aggregate='sum' alias='bsd_quantity' />
                                                                <attribute name='bsd_product' groupby='true' alias='bsd_product' />
                                                                <filter type='and'>
                                                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='{0}' />
                                                                    <condition attribute='bsd_deliverynote' operator='eq'  uitype='bsd_deliverynote' value='" + deliveryNote + @"' /> 
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
                                                            </fetch>", product, subOrder);
            // throw new Exception("xml: "+xmlnote);
            return myService.service.RetrieveMultiple(new FetchExpression(xmlnote));
        }
        public EntityCollection getDeliveryNoteProduct(Guid deliveryNote)
        {
            string xmlnote = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_deliverynoteproduct'>
                                <all-attributes />
                                <order attribute='bsd_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='bsd_deliverynote' operator='eq'  uitype='bsd_deliverynote' value='{0}' />
                                </filter>
                              </entity>
                            </fetch>", deliveryNote);
            // throw new Exception("xml: "+xmlnote);
            return myService.service.RetrieveMultiple(new FetchExpression(xmlnote));
        }
        public decimal getQuantityReturnOrderDeliveryNote(Guid suborderid, Guid productId, Guid deliveryNoteId)
        {
            decimal result = 0m;
            #region
            string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_returnorderproduct'>
                                                        <attribute name='bsd_returnorderproductid' />
                                                        <attribute name='bsd_orderquantity' />
                                                        <order attribute='bsd_orderquantity' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_findsuborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                                                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                                                           <condition attribute='bsd_deliverynote' operator='eq' uitype='bsd_deliverynote' value='{2}' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>", suborderid, productId, deliveryNoteId);
            EntityCollection entity_recevied = myService.service.RetrieveMultiple(new FetchExpression(xml));
            if (entity_recevied.Entities.Count > 0)
            {
                decimal quantity_recevied = 0m;
                foreach (var cus in entity_recevied.Entities)
                {
                    quantity_recevied += (decimal)cus["bsd_orderquantity"];
                }
                result = quantity_recevied;
            }
            #endregion
            return result;
        }
        public EntityCollection getReturnOrderDeliveryNote(Guid suborderid, Guid productId, Guid deliveryNoteId)
        {

            string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_returnorderproduct'>
                                                        <attribute name='bsd_returnorderproductid' />
                                                        <attribute name='bsd_orderquantity' />
                                                        <order attribute='bsd_orderquantity' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_findsuborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                                                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                                                           <condition attribute='bsd_deliverynote' operator='eq' uitype='bsd_deliverynote' value='{2}' />
                                                        </filter>
                                                         <link-entity name='bsd_returnorder' from='bsd_returnorderid' to='bsd_returnorderid' alias='ad'>
                                                              <filter type='and'>
                                                                <condition attribute='statecode' operator='eq' value='0' />
                                                              </filter>
                                                            </link-entity>
                                                      </entity>
                                                    </fetch>", suborderid, productId, deliveryNoteId);
            return myService.service.RetrieveMultiple(new FetchExpression(xml));


        }
        public decimal getQuantityReturnOrderSuborder(Guid suborderid, Guid productId)
        {
            decimal result = 0m;
            #region
            string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_returnorderproduct'>
                                                        <attribute name='bsd_returnorderproductid' />
                                                        <attribute name='bsd_orderquantity' />
                                                        <order attribute='bsd_orderquantity' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_findsuborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                                                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>", suborderid, productId);
            EntityCollection entity_recevied = myService.service.RetrieveMultiple(new FetchExpression(xml));
            if (entity_recevied.Entities.Count > 0)
            {
                decimal quantity_recevied = 0m;
                foreach (var cus in entity_recevied.Entities)
                {
                    quantity_recevied += (decimal)cus["bsd_orderquantity"];
                }
                result = quantity_recevied;
            }
            #endregion
            return result;
        }
        public EntityCollection getReturnOrderSuborder(Guid suborderid, Guid productId)
        {
            #region
            string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_returnorderproduct'>
                                                        <attribute name='bsd_returnorderproductid' />
                                                        <attribute name='bsd_orderquantity' />
                                                        <order attribute='bsd_orderquantity' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_findsuborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                                                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>", suborderid, productId);
            return myService.service.RetrieveMultiple(new FetchExpression(xml));
            #endregion
        }
        public EntityCollection getSubOrderProduct(Guid subOrder)
        {
            string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_suborderproduct'>
                        <all-attributes />
                        <filter type='and'>
                          <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                          <condition attribute='bsd_deliverystatus' operator='in'>
                            <value>861450003</value>
                            <value>861450004</value>
                            <value>861450002</value>
                          </condition>
                        </filter>
                      </entity>
                    </fetch>", subOrder);
            // throw new Exception("xml: "+xmlnote);                          //<condition attribute='bsd_shippedquantity' operator='gt' value='0' />
            return myService.service.RetrieveMultiple(new FetchExpression(xml));
        }
    }
}
