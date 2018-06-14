using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Crm.Sdk.Messages;
using DeliveryPlugin.Service;

namespace DeliveryPlugin
{
    public class SubOrderCancel : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);

            #region Update
            if (myService.context.MessageName == "Update")
            {
                myService.StartService();
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_skipplugin") && (bool)target["bsd_skipplugin"]) return;
                Entity suborder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                if (target.HasValue("statecode"))
                {
                    int statecode = ((OptionSetValue)target["statecode"]).Value;
                    if (statecode == 1)
                    {
                        #region Nếu có Status và nó là từ quote hoặc order
                        if (suborder.HasValue("bsd_quote") || suborder.HasValue("bsd_order"))
                        {
                            Service.SuborderService subService = new Service.SuborderService(myService);
                            EntityCollection list_suborderproduct = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", target.Id);
                            foreach (var suborder_product in list_suborderproduct.Entities)
                            {
                                decimal ship_quantity = (decimal)suborder_product["bsd_shipquantity"];
                                if (suborder.HasValue("bsd_quote"))
                                {
                                    Entity quotedetail = subService.getQuoteDetailFromSuborderProduct(suborder_product, 2);
                                    Entity quote = myService.service.Retrieve("quote", ((EntityReference)quotedetail["quoteid"]).Id, new ColumnSet(true));

                                    bool multiple_address = (bool)quote["bsd_multipleaddress"];
                                    bool have_quantity = (bool)quote["bsd_havequantity"];

                                    if (multiple_address == false || (multiple_address == true && have_quantity == true))
                                    {
                                        decimal quantity = (decimal)quotedetail["quantity"];
                                        decimal old_suborder_quantity = (decimal)quotedetail["bsd_suborderquantity"];
                                        decimal remaining_quantity = (decimal)quotedetail["bsd_remainingquantity"];


                                        decimal new_suborder_quantity = old_suborder_quantity - ship_quantity;
                                        decimal new_remaining_quantity = quantity - new_suborder_quantity;

                                        #region Cập nhật lại quantity

                                        EntityReference quote_ref = (EntityReference)suborder["bsd_quote"];
                                        myService.SetState(quote_ref.Id, quote_ref.LogicalName, 0, 1);

                                        Entity new_quotedetail = new Entity(quotedetail.LogicalName, quotedetail.Id);
                                        new_quotedetail["bsd_suborderquantity"] = new_suborder_quantity;
                                        new_quotedetail["bsd_remainingquantity"] = new_remaining_quantity;
                                        myService.Update(new_quotedetail);

                                        #region won quote
                                        myService.SetState(quote_ref.Id, quote_ref.LogicalName, 1, 2);
                                        WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
                                        Entity quoteClose = new Entity("quoteclose");
                                        quoteClose.Attributes["quoteid"] = new EntityReference("quote", new Guid(quote_ref.Id.ToString()));
                                        quoteClose.Attributes["subject"] = "Quote Close" + DateTime.Now.ToString();
                                        winQuoteRequest.QuoteClose = quoteClose;
                                        winQuoteRequest.Status = new OptionSetValue(-1);
                                        myService.service.Execute(winQuoteRequest);
                                        #endregion
                                        #endregion
                                    }
                                    else
                                    {
                                        // cancel thì trừ đi cũng giống như xóa
                                        subService.DeleteSuborderProduct(suborder_product);
                                    }
                                }
                                else if (suborder.HasValue("bsd_order") && !suborder.HasValue("bsd_appendixcontract"))//không có phụ lục, nếu có thì chạy qua code Huy
                                {
                                    Entity salesorderdetail = subService.getSalesorderDetailFromSuborderProduct(suborder_product, 2);
                                    Entity order = myService.service.Retrieve("salesorder", ((EntityReference)salesorderdetail["salesorderid"]).Id, new ColumnSet(true));

                                    bool multiple_address = (bool)order["bsd_multipleaddress"];
                                    bool have_quantity = (bool)order["bsd_havequantity"];

                                    if (multiple_address == false || (multiple_address == true && have_quantity == true))
                                    {
                                        decimal quantity = (decimal)salesorderdetail["quantity"];
                                        decimal old_suborder_quantity = (decimal)salesorderdetail["bsd_suborderquantity"];
                                        decimal remaining_quantity = (decimal)salesorderdetail["bsd_remainingquantity"];

                                        decimal new_suborder_quantity = old_suborder_quantity - ship_quantity;
                                        decimal new_remaining_quantity = quantity - new_suborder_quantity;

                                        #region Cập nhật lại quantity
                                        Entity new_salesorderdetail = new Entity(salesorderdetail.LogicalName, salesorderdetail.Id);
                                        new_salesorderdetail["bsd_suborderquantity"] = new_suborder_quantity;
                                        new_salesorderdetail["bsd_remainingquantity"] = new_remaining_quantity;
                                        myService.Update(new_salesorderdetail);
                                        #endregion
                                    }
                                    else
                                    {
                                        subService.DeleteSuborderProduct(suborder_product);
                                    }
                                }
                            }
                        }
                        #endregion
                        TruCongNoFromSub(suborder);
                    }
                }
            }
            #endregion
        }
        public void TruCongNoFromSub(Entity suborder)
        {
            if (suborder.HasValue("bsd_potentialcustomer") && suborder.HasValue("bsd_submittedcustomerdebt"))
            {
                SuborderService subService = new SuborderService(myService);
                Entity configdefault = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_configdefault'>
                    <attribute name='bsd_configdefaultid' />
                    <attribute name='createdon' />
                    <attribute name='bsd_checkcustomerbalance' />
                    <order attribute='createdon' descending='true' />
                  </entity>
                </fetch>").Entities.FirstOrDefault();
                bool check_customerbalance = configdefault.HasValue("bsd_checkcustomerbalance") ? (bool)configdefault["bsd_checkcustomerbalance"] : true;
                if (check_customerbalance)
                {
                    EntityReference account_ref = (EntityReference)suborder["bsd_potentialcustomer"];
                    Entity submitted_customerdebt = myService.service.Retrieve("bsd_customerdebt", ((EntityReference)suborder["bsd_submittedcustomerdebt"]).Id, new ColumnSet(true));
                    Entity current_customerdebt = subService.GetCustomerDebtByTimeAndAccount(account_ref.Id, DateTime.Now);
                    if (submitted_customerdebt.Id.Equals(current_customerdebt.Id))
                    {
                        decimal current_debt = submitted_customerdebt.HasValue("bsd_newdebt") ? ((Money)submitted_customerdebt["bsd_newdebt"]).Value : 0;
                        decimal submitted_grandtotal = ((Money)suborder["bsd_submittedgrandtotal"]).Value;
                        Entity new_customerdebt = new Entity(current_customerdebt.LogicalName, current_customerdebt.Id);
                        new_customerdebt["bsd_newdebt"] = new Money(current_debt - submitted_grandtotal); // Hiện tại trừ đi Grand Total đã submitted.
                        myService.Update(new_customerdebt);
                    }
                }
            }
        }
    }
}
