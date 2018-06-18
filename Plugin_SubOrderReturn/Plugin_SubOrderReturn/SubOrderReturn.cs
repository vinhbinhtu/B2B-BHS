using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using System.Globalization;

namespace Plugin_SubOrderReturn
{
    public class SubOrderReturn : IPlugin
    {
        private MyService myService;
        void IPlugin.Execute(IServiceProvider serviceProvider)
        {

            myService = new MyService(serviceProvider);

            if (myService.context.Depth > 1)
                return;

            #region Create Suborder type Return

            if (myService.context.MessageName == "bsd_Action_CreateSubOrderReturn")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();

                //throw new Exception("1");
                // returnorder
                Entity returnorder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

                Entity SubOrder = new Entity("bsd_suborder");

                string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_returnorderproduct'>
                                            <all-attributes />
                                            <filter type='and'>
                                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                                            </filter>
                                          </entity>
                                        </fetch>", target.Id);
                EntityCollection list_orderproduct = myService.service.RetrieveMultiple(new FetchExpression(xml));
                if (!list_orderproduct.Entities.Any()) throw new Exception("Please choose product!");
                #region Tao Suborder 
                myService.SetState(target.Id, target.LogicalName, 0, 1);

                SubOrder["bsd_type"] = new OptionSetValue(861450004);
                SubOrder["bsd_deliveryfrom"] = new OptionSetValue(861450000);
                SubOrder["bsd_date"] = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                SubOrder["bsd_returnorder"] = new EntityReference(returnorder.LogicalName, returnorder.Id);


                SubOrder["bsd_potentialcustomer"] = returnorder["bsd_potentialcustomer"];

                Entity account = myService.service.Retrieve("account", ((EntityReference)returnorder["bsd_potentialcustomer"]).Id, new ColumnSet(true));
                if (account.HasValue("bsd_timeship")) SubOrder["bsd_timeship"] = account["bsd_timeship"];

                if (returnorder.HasValue("transactioncurrencyid")) SubOrder["transactioncurrencyid"] = returnorder["transactioncurrencyid"];
                if (returnorder.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addresscontractoffer"] = returnorder["bsd_addressinvoiceaccount"];
                if (returnorder.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addresscustomeraccount"] = returnorder["bsd_addressinvoiceaccount"];
                if (returnorder.HasValue("bsd_telephone")) SubOrder["bsd_telephone"] = returnorder["bsd_telephone"];
                if (returnorder.HasValue("bsd_taxregistration")) SubOrder["bsd_taxregistration"] = returnorder["bsd_taxregistration"];
                if (returnorder.HasValue("bsd_contact")) SubOrder["bsd_contact"] = returnorder["bsd_contact"];
                if (returnorder.HasValue("bsd_customercode")) SubOrder["bsd_customercode"] = returnorder["bsd_customercode"];
                if (returnorder.HasValue("bsd_pricelist")) SubOrder["bsd_pricelist"] = returnorder["bsd_pricelist"];
                if (returnorder.HasValue("bsd_exchangerate")) SubOrder["bsd_exchangeratevalue"] = returnorder["bsd_exchangerate"];
                //throw new Exception(returnorder["bsd_exchangerate"].ToString());

                if (returnorder.HasValue("bsd_shiptoaccount"))
                {
                    SubOrder["bsd_shiptoaccount"] = returnorder["bsd_shiptoaccount"];
                }
                else
                {
                    SubOrder["bsd_shiptoaccount"] = returnorder["bsd_potentialcustomer"];
                }
                if (returnorder.HasValue("bsd_contactshiptoaccount")) SubOrder["bsd_contactshiptoaccount"] = returnorder["bsd_contactshiptoaccount"];
                if (returnorder.HasValue("bsd_shiptoaddress")) SubOrder["bsd_shiptoaddress"] = returnorder["bsd_shiptoaddress"];
                if (returnorder.HasValue("bsd_date"))
                {
                    SubOrder["bsd_todate"] = returnorder["bsd_date"];
                    SubOrder["bsd_fromdate"] = returnorder["bsd_date"];
                    SubOrder["bsd_requestedshipdate"] = returnorder["bsd_date"];
                    SubOrder["bsd_requestedreceiptdate"] = returnorder["bsd_date"];
                }
                //if (returnorder.HasValue("bsd_fromdate")) SubOrder["bsd_fromdate"] = returnorder["bsd_fromdate"];                       /////  
                //if (returnorder.HasValue("bsd_todate")) SubOrder["bsd_todate"] = returnorder["bsd_todate"];                             /////
                //if (returnorder.HasValue("bsd_requestedshipdate")) SubOrder["bsd_requestedshipdate"] = returnorder["bsd_requestedshipdate"];                       /////  
                //if (returnorder.HasValue("bsd_requestedreceiptdate")) SubOrder["bsd_requestedreceiptdate"] = returnorder["bsd_requestedreceiptdate"];                             /////

                //  throw new Exception("2");

                if (returnorder.HasValue("bsd_invoiceaccount")) SubOrder["bsd_invoiceaccount"] = returnorder["bsd_invoiceaccount"];
                if (returnorder.HasValue("bsd_customer")) SubOrder["bsd_invoicenameaccount"] = returnorder["bsd_customer"];
                if (returnorder.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addressinvoiceaccount"] = returnorder["bsd_addressinvoiceaccount"];
                if (returnorder.HasValue("bsd_contactinvoiceaccount")) SubOrder["bsd_contactinvoiceaccount"] = returnorder["bsd_contactinvoiceaccount"];        ////

                if (returnorder.HasValue("bsd_unitdefault")) SubOrder["bsd_unitdefault"] = returnorder["bsd_unitdefault"];
                SubOrder["bsd_priceincludeshippingporter"] = false;
                // throw new Exception("3");

                #region Lấy Exchange Rate 
                /*
                decimal bsd_exchangeratevalue = 1m;

                Guid account_currency = ((EntityReference)returnorder["transactioncurrencyid"]).Id;

                Entity config_default = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_configdefault'>
                        <attribute name='bsd_configdefaultid' />
                        <attribute name='bsd_unitdefault' />
                        <attribute name='bsd_currencydefault' />
                        <attribute name='bsd_bankdefault' />
                      </entity>
                    </fetch>"))).Entities.First();

                Entity bsd_currencydefault = myService.service.Retrieve(((EntityReference)config_default["bsd_currencydefault"]).LogicalName, ((EntityReference)config_default["bsd_currencydefault"]).Id, new ColumnSet(true));
                Entity bsd_unitdefault = myService.service.Retrieve(((EntityReference)config_default["bsd_unitdefault"]).LogicalName, ((EntityReference)config_default["bsd_unitdefault"]).Id, new ColumnSet(true));
                Entity bsd_bankdefault = myService.service.Retrieve(((EntityReference)config_default["bsd_bankdefault"]).LogicalName, ((EntityReference)config_default["bsd_bankdefault"]).Id, new ColumnSet(true));
                SubOrder["bsd_currencydefault"] = config_default["bsd_currencydefault"];
                if (!account_currency.Equals(bsd_unitdefault.Id)) // nếu không bằng với unit default.
                {
                    EntityCollection list_exchangerate = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_exchangerate'>
                                <attribute name='bsd_exchangerateid' />
                                <attribute name='bsd_exchangerate' />
                                <order attribute='bsd_date' descending='true' />
                                    <order attribute='createdon' descending='true' />
                                <filter type='and'>
                                  <condition attribute='bsd_currencyfrom' operator='eq' uitype='transactioncurrency' value='{0}' />
                                  <condition attribute='bsd_currencyto' operator='eq' uitype='transactioncurrency' value='{1}' />
                                  <condition attribute='bsd_bankaccount' operator='eq' uitype='bsd_bankgroup' value='{2}' />
                                  <condition attribute='bsd_date' operator='on-or-before' value='{3}' />
                                </filter>
                              </entity>
                            </fetch>", account_currency
                        , bsd_currencydefault.Id
                        , bsd_bankdefault.Id,
                    myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service).Date.ToString("yyyy/MM/dd"))));

                    if (list_exchangerate.Entities.Any())
                    {
                        Entity ex_changerate = list_exchangerate.Entities.First();
                        SubOrder["bsd_exchangerate"] = new EntityReference(ex_changerate.LogicalName, ex_changerate.Id);
                        SubOrder["bsd_bank"] = new EntityReference(bsd_bankdefault.LogicalName, bsd_bankdefault.Id);
                        bsd_exchangeratevalue = (decimal)ex_changerate["bsd_exchangerate"];
                    }
                }
                else
                {
                    bsd_exchangeratevalue = 1m;
                }
                SubOrder["bsd_exchangeratevalue"] = bsd_exchangeratevalue;
                */
                #endregion
                //decimal bsd_exchangeratevalue = 1m;
                //if (returnorder.HasValue("bsd_exchangerate")) bsd_exchangeratevalue = (decimal)returnorder["bsd_exchangerate"];
                //SubOrder["bsd_exchangeratevalue"] = bsd_exchangeratevalue;
                // throw new Exception("4");
                #region Chuyển shiping porter tax

                // warehousse

                if (returnorder.HasValue("bsd_warehouse")) SubOrder["bsd_warehouse"] = returnorder["bsd_warehouse"];
                //if (returnorder.HasValue("bsd_warehouseaddress")) SubOrder["bsd_warehouseaddress"] = returnorder["bsd_warehouseaddress"];
                if (returnorder.HasValue("bsd_site")) SubOrder["bsd_site"] = returnorder["bsd_site"];
                if (returnorder.HasValue("bsd_siteaddress")) SubOrder["bsd_siteaddress"] = returnorder["bsd_siteaddress"];
                if (returnorder.HasValue("bsd_saletaxgroup")) SubOrder["bsd_saletaxgroup"] = returnorder["bsd_saletaxgroup"];
                if (returnorder.HasValue("bsd_requestporter")) SubOrder["bsd_requestporter"] = returnorder["bsd_requestporter"];
                if (returnorder.HasValue("bsd_transportation")) SubOrder["bsd_transportation"] = returnorder["bsd_transportation"];
                if (returnorder.HasValue("bsd_shippingdeliverymethod")) SubOrder["bsd_shippingdeliverymethod"] = returnorder["bsd_shippingdeliverymethod"];
                if (returnorder.HasValue("bsd_truckload")) SubOrder["bsd_truckload"] = returnorder["bsd_truckload"];
                if (returnorder.HasValue("bsd_unitshipping")) SubOrder["bsd_unitshipping"] = returnorder["bsd_unitshipping"];
                if (returnorder.HasValue("bsd_shippingpricelistname")) SubOrder["bsd_shippingpricelistname"] = returnorder["bsd_shippingpricelistname"];
                if (returnorder.HasValue("bsd_priceoftransportationn")) SubOrder["bsd_priceoftransportationn"] = returnorder["bsd_priceoftransportationn"];
                if (returnorder.HasValue("bsd_shippingporter")) SubOrder["bsd_shippingporter"] = returnorder["bsd_shippingporter"];
                if (returnorder.HasValue("bsd_porteroption")) SubOrder["bsd_porteroption"] = returnorder["bsd_porteroption"];
                if (returnorder.HasValue("bsd_priceofporter")) SubOrder["bsd_priceofporter"] = returnorder["bsd_priceofporter"];
                if (returnorder.HasValue("bsd_pricepotter")) SubOrder["bsd_pricepotter"] = returnorder["bsd_pricepotter"];
                if (returnorder.HasValue("bsd_porter")) SubOrder["bsd_porter"] = returnorder["bsd_porter"];
                // ((Money)returnorder["bsd_totalamount"]).Value.ToString("N", new CultureInfo("is-IS"));
                #endregion

                // throw new Exception("5");
                Entity suborderold;
                if (returnorder.HasValue("bsd_findsuborder"))
                {
                    suborderold = myService.service.Retrieve("bsd_suborder", ((EntityReference)returnorder["bsd_findsuborder"]).Id, new ColumnSet(true));

                    if (suborderold.HasValue("bsd_paymentterm"))
                    {
                        SubOrder["bsd_paymentterm"] = suborderold["bsd_paymentterm"];
                        Entity bsd_paymentterm = myService.service.Retrieve(((EntityReference)suborderold["bsd_paymentterm"]).LogicalName, ((EntityReference)suborderold["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                        SubOrder["bsd_datept"] = new decimal((int)bsd_paymentterm["bsd_date"]);
                    }
                    if (suborderold.HasValue("bsd_paymentmethod")) SubOrder["bsd_paymentmethod"] = suborderold["bsd_paymentmethod"];
                    if (suborderold.HasValue("bsd_duedate")) SubOrder["bsd_duedate"] = suborderold["bsd_duedate"];
                }
                else // Return Order với type Return not resource
                {
                    if (account.HasValue("bsd_paymentterm"))
                    {
                        SubOrder["bsd_paymentterm"] = account["bsd_paymentterm"];
                        Entity bsd_paymentterm = myService.service.Retrieve(((EntityReference)account["bsd_paymentterm"]).LogicalName, ((EntityReference)account["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                        SubOrder["bsd_datept"] = new decimal((int)bsd_paymentterm["bsd_date"]);
                        SubOrder["bsd_duedate"] = DateTime.Now.AddDays((int)bsd_paymentterm["bsd_date"]);
                    }
                    if (account.HasValue("bsd_paymentmethod")) SubOrder["bsd_paymentmethod"] = account["bsd_paymentmethod"];
                    #region lấy bảng giá tạo suborder gần nhất
                    EntityCollection list = PriceList(((EntityReference)returnorder["bsd_potentialcustomer"]).Id);
                    if (!list.Entities.Any()) throw new Exception("not found Price List.");
                    SubOrder["bsd_pricelist"] = (EntityReference)list.Entities.First()["bsd_pricelist"];
                    #endregion
                }
                Guid suborder_id = myService.service.Create(SubOrder);

                //  throw new Exception("ok order");
                #endregion

                #region "Tao Suborder product"

                foreach (var orderproduct in list_orderproduct.Entities)
                {
                    Entity product = myService.service.Retrieve("product", ((EntityReference)orderproduct["bsd_product"]).Id, new ColumnSet(true));
                    //throw new Exception("6");

                    #region tao sub product
                    Entity sub_product = new Entity("bsd_suborderproduct");
                    sub_product["bsd_name"] = product["name"];
                    sub_product["bsd_returnorder"] = target;                    /////
                                                                                // throw new Exception("7");
                    if (returnorder.HasValue("bsd_warehouse"))
                        sub_product["bsd_warehouse"] = returnorder["bsd_warehouse"];
                    sub_product["bsd_type"] = new OptionSetValue(861450004);
                    sub_product["bsd_suborder"] = new EntityReference("bsd_suborder", suborder_id);
                    sub_product["bsd_productid"] = orderproduct["bsd_productid"];
                    sub_product["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                    sub_product["bsd_priceperunit"] = orderproduct["bsd_giatruocthue"];
                    sub_product["bsd_orderquantity"] = orderproduct["bsd_orderquantity"];
                    sub_product["bsd_shipquantity"] = orderproduct["bsd_orderquantity"];
                    sub_product["bsd_standardquantity"] = orderproduct["bsd_standardquantity"];
                    sub_product["bsd_totalquantity"] = orderproduct["bsd_totalquantity"];
                    sub_product["bsd_remainningquantity"] = 0m;                         ////
                    sub_product["bsd_shippingprice"] = new Money(0m);                              ////
                    sub_product["bsd_porterprice"] = new Money(0m);                                ////
                    sub_product["bsd_shippedquantity"] = 0m;                            ////
                    sub_product["bsd_residualquantity"] = orderproduct["bsd_shipquantity"];////
                    sub_product["bsd_deliverystatus"] = new OptionSetValue(861450000);
                    sub_product["bsd_giatruocthue"] = orderproduct["bsd_giatruocthue"];
                    sub_product["bsd_giasauthue"] = orderproduct["bsd_giasauthue"];
                    sub_product["bsd_amount"] = orderproduct["bsd_amount"];
                    if (orderproduct.HasValue("bsd_itemsalestaxgroup")) sub_product["bsd_itemsalestaxgroup"] = orderproduct["bsd_itemsalestaxgroup"];
                    else throw new Exception(orderproduct["bsd_name"]+" item sales tax group is not null");
                    if (orderproduct.HasValue("bsd_unit")) sub_product["bsd_unit"] = orderproduct["bsd_unit"];
                    if (orderproduct.HasValue("bsd_usingtax")) sub_product["bsd_usingtax"] = orderproduct["bsd_usingtax"];
                    if (orderproduct.HasValue("bsd_currencyexchange"))
                    {
                        //throw new Exception("ok");
                        sub_product["bsd_currencyexchangecurrency"] = (Money)orderproduct["bsd_currencyexchange"];
                        sub_product["bsd_currencyexchange"] = ((Money)orderproduct["bsd_currencyexchange"]).Value; //return order datatype is money, suborder datatype is decimal
                    }
                    if (returnorder.HasValue("transactioncurrencyid"))
                    {
                        sub_product["transactioncurrencyid"] = returnorder["transactioncurrencyid"];
                    }
                    //throw new Exception("8");
                    if (orderproduct.HasValue("bsd_itemsalestax"))
                    {
                        sub_product["bsd_itemsalestax"] = orderproduct["bsd_itemsalestax"];
                    }
                    if (orderproduct.HasValue("bsd_tax")) sub_product["bsd_tax"] = orderproduct["bsd_tax"];
                    if (orderproduct.HasValue("bsd_vatprice")) sub_product["bsd_vatprice"] = orderproduct["bsd_vatprice"];
                    //throw new Exception("9");
                    if (orderproduct.HasValue("bsd_extendedamount")) sub_product["bsd_extendedamount"] = orderproduct["bsd_extendedamount"];

                    myService.service.Create(sub_product);

                    #endregion

                }
                #endregion

                #region Update suborder
                Entity suborder_update = myService.service.Retrieve(SubOrder.LogicalName, suborder_id, new ColumnSet(true));
                UpdateSubOrder(suborder_update);
                #endregion


                UpdateReturnOrderStatus(returnorder.Id);
                // throw new Exception("10");
                myService.context.OutputParameters["ReturnId"] = suborder_id.ToString();

            }

            #endregion

            #region Create Suborder type SaleReplace
            else if (myService.context.MessageName == "bsd_Action_CreateSubOrder_SaleReplace")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();

                // returnorder
                Entity returnorder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                Entity SubOrder = new Entity("bsd_suborder");

                string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_returnorderproduct'>
                                            <all-attributes />
                                            <filter type='and'>
                                              <condition attribute='bsd_returnorderid' operator='eq' uitype='bsd_returnorder' value='{0}' />
                                            </filter>
                                          </entity>
                                        </fetch>", target.Id);
                EntityCollection list_orderproduct = myService.service.RetrieveMultiple(new FetchExpression(xml));

                #region Tao Suborder 
                myService.SetState(target.Id, target.LogicalName, 0, 1);

                SubOrder["bsd_type"] = new OptionSetValue(861450005);
                SubOrder["bsd_deliveryfrom"] = new OptionSetValue(861450000);
                SubOrder["bsd_date"] = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                SubOrder["bsd_returnorder"] = new EntityReference(returnorder.LogicalName, returnorder.Id);

                SubOrder["bsd_potentialcustomer"] = returnorder["bsd_potentialcustomer"];

                Entity account = myService.service.Retrieve("account", ((EntityReference)returnorder["bsd_potentialcustomer"]).Id, new ColumnSet(true));
                if (account.HasValue("bsd_timeship")) SubOrder["bsd_timeship"] = account["bsd_timeship"];

                if (returnorder.HasValue("transactioncurrencyid")) SubOrder["transactioncurrencyid"] = returnorder["transactioncurrencyid"];
                if (returnorder.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addresscontractoffer"] = returnorder["bsd_addressinvoiceaccount"];
                if (returnorder.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addresscustomeraccount"] = returnorder["bsd_addressinvoiceaccount"];
                if (returnorder.HasValue("bsd_telephone")) SubOrder["bsd_telephone"] = returnorder["bsd_telephone"];
                if (returnorder.HasValue("bsd_taxregistration")) SubOrder["bsd_taxregistration"] = returnorder["bsd_taxregistration"];
                if (returnorder.HasValue("bsd_contact")) SubOrder["bsd_contact"] = returnorder["bsd_contact"];
                if (returnorder.HasValue("bsd_customercode")) SubOrder["bsd_customercode"] = returnorder["bsd_customercode"];
                if (returnorder.HasValue("bsd_pricelist")) SubOrder["bsd_pricelist"] = returnorder["bsd_pricelist"];
                if (returnorder.HasValue("bsd_exchangerate")) SubOrder["bsd_exchangeratevalue"] = returnorder["bsd_exchangerate"];
                //throw new Exception("1");

                if (returnorder.HasValue("bsd_shiptoaccount"))
                {
                    SubOrder["bsd_shiptoaccount"] = returnorder["bsd_shiptoaccount"];
                }
                else
                {
                    SubOrder["bsd_shiptoaccount"] = returnorder["bsd_potentialcustomer"];
                }
                if (returnorder.HasValue("bsd_contactshiptoaccount")) SubOrder["bsd_contactshiptoaccount"] = returnorder["bsd_contactshiptoaccount"];
                if (returnorder.HasValue("bsd_shiptoaddress")) SubOrder["bsd_shiptoaddress"] = returnorder["bsd_shiptoaddress"];
                if (returnorder.HasValue("bsd_date"))
                {
                    SubOrder["bsd_todate"] = returnorder["bsd_date"];
                    SubOrder["bsd_fromdate"] = returnorder["bsd_date"];
                    SubOrder["bsd_requestedshipdate"] = returnorder["bsd_date"];
                    SubOrder["bsd_requestedreceiptdate"] = returnorder["bsd_date"];
                }
                //if (returnorder.HasValue("bsd_fromdate")) SubOrder["bsd_fromdate"] = returnorder["bsd_fromdate"];                       /////  
                //if (returnorder.HasValue("bsd_todate")) SubOrder["bsd_todate"] = returnorder["bsd_todate"];                             /////
                //if (returnorder.HasValue("bsd_requestedshipdate")) SubOrder["bsd_requestedshipdate"] = returnorder["bsd_requestedshipdate"];                       /////  
                //if (returnorder.HasValue("bsd_requestedreceiptdate")) SubOrder["bsd_requestedreceiptdate"] = returnorder["bsd_requestedreceiptdate"];                             /////

                if (returnorder.HasValue("bsd_invoiceaccount")) SubOrder["bsd_invoiceaccount"] = returnorder["bsd_invoiceaccount"];
                if (returnorder.HasValue("bsd_customer")) SubOrder["bsd_invoicenameaccount"] = returnorder["bsd_customer"];
                if (returnorder.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addressinvoiceaccount"] = returnorder["bsd_addressinvoiceaccount"];
                if (returnorder.HasValue("bsd_contactinvoiceaccount")) SubOrder["bsd_contactinvoiceaccount"] = returnorder["bsd_contactinvoiceaccount"];        ////
                if (returnorder.HasValue("bsd_exchangerate")) SubOrder["bsd_exchangeratevalue"] = returnorder["bsd_exchangerate"];
                if (returnorder.HasValue("bsd_unitdefault")) SubOrder["bsd_unitdefault"] = returnorder["bsd_unitdefault"];
                #region Lấy Exchange Rate 
                /*
                decimal bsd_exchangeratevalue = 1m;

                Guid account_currency = ((EntityReference)returnorder["transactioncurrencyid"]).Id;

                Entity config_default = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_configdefault'>
                        <attribute name='bsd_configdefaultid' />
                        <attribute name='bsd_unitdefault' />
                        <attribute name='bsd_currencydefault' />
                        <attribute name='bsd_bankdefault' />
                      </entity>
                    </fetch>"))).Entities.First();

                Entity bsd_currencydefault = myService.service.Retrieve(((EntityReference)config_default["bsd_currencydefault"]).LogicalName, ((EntityReference)config_default["bsd_currencydefault"]).Id, new ColumnSet(true));
                Entity bsd_unitdefault = myService.service.Retrieve(((EntityReference)config_default["bsd_unitdefault"]).LogicalName, ((EntityReference)config_default["bsd_unitdefault"]).Id, new ColumnSet(true));
                Entity bsd_bankdefault = myService.service.Retrieve(((EntityReference)config_default["bsd_bankdefault"]).LogicalName, ((EntityReference)config_default["bsd_bankdefault"]).Id, new ColumnSet(true));
                SubOrder["bsd_currencydefault"] = config_default["bsd_currencydefault"];
                if (!account_currency.Equals(bsd_unitdefault.Id)) // nếu không bằng với unit default.
                {
                    EntityCollection list_exchangerate = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_exchangerate'>
                                <attribute name='bsd_exchangerateid' />
                                <attribute name='bsd_exchangerate' />
                                <order attribute='bsd_date' descending='true' />
                                    <order attribute='createdon' descending='true' />
                                <filter type='and'>
                                  <condition attribute='bsd_currencyfrom' operator='eq' uitype='transactioncurrency' value='{0}' />
                                  <condition attribute='bsd_currencyto' operator='eq' uitype='transactioncurrency' value='{1}' />
                                  <condition attribute='bsd_bankaccount' operator='eq' uitype='bsd_bankgroup' value='{2}' />
                                  <condition attribute='bsd_date' operator='on-or-before' value='{3}' />
                                </filter>
                              </entity>
                            </fetch>", account_currency
                        , bsd_currencydefault.Id
                        , bsd_bankdefault.Id,
                    myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service).Date.ToString("yyyy/MM/dd"))));

                    if (list_exchangerate.Entities.Any())
                    {
                        Entity ex_changerate = list_exchangerate.Entities.First();
                        SubOrder["bsd_exchangerate"] = new EntityReference(ex_changerate.LogicalName, ex_changerate.Id);
                        SubOrder["bsd_bank"] = new EntityReference(bsd_bankdefault.LogicalName, bsd_bankdefault.Id);
                        bsd_exchangeratevalue = (decimal)ex_changerate["bsd_exchangerate"];
                    }
                }
                else
                {
                    bsd_exchangeratevalue = 1m;
                }
                SubOrder["bsd_exchangeratevalue"] = bsd_exchangeratevalue;
                */
                #endregion
                decimal bsd_exchangeratevalue = 1m;
                if (returnorder.HasValue("bsd_exchangerate")) bsd_exchangeratevalue = (decimal)returnorder["bsd_exchangerate"];
                SubOrder["bsd_exchangeratevalue"] = bsd_exchangeratevalue;
                // throw new Exception("4");
                #region Chuyển shiping porter tax

                // warehousse

                //if (returnorder.HasValue("bsd_warehouse")) SubOrder["bsd_warehousefrom"] = returnorder["bsd_warehouse"];
                //if (returnorder.HasValue("bsd_warehouseaddress")) SubOrder["bsd_warehouseaddress"] = returnorder["bsd_warehouseaddress"];
                if (returnorder.HasValue("bsd_site")) SubOrder["bsd_site"] = returnorder["bsd_site"];
                if (returnorder.HasValue("bsd_siteaddress")) SubOrder["bsd_siteaddress"] = returnorder["bsd_siteaddress"];
                if (returnorder.HasValue("bsd_saletaxgroup")) SubOrder["bsd_saletaxgroup"] = returnorder["bsd_saletaxgroup"];
                if (returnorder.HasValue("bsd_requestporter")) SubOrder["bsd_requestporter"] = returnorder["bsd_requestporter"];
                if (returnorder.HasValue("bsd_transportation")) SubOrder["bsd_transportation"] = returnorder["bsd_transportation"];
                if (returnorder.HasValue("bsd_shippingdeliverymethod")) SubOrder["bsd_shippingdeliverymethod"] = returnorder["bsd_shippingdeliverymethod"];
                if (returnorder.HasValue("bsd_truckload")) SubOrder["bsd_truckload"] = returnorder["bsd_truckload"];
                if (returnorder.HasValue("bsd_unitshipping")) SubOrder["bsd_unitshipping"] = returnorder["bsd_unitshipping"];
                if (returnorder.HasValue("bsd_shippingpricelistname")) SubOrder["bsd_shippingpricelistname"] = returnorder["bsd_shippingpricelistname"];
                if (returnorder.HasValue("bsd_priceoftransportationn")) SubOrder["bsd_priceoftransportationn"] = returnorder["bsd_priceoftransportationn"];
                if (returnorder.HasValue("bsd_shippingporter")) SubOrder["bsd_shippingporter"] = returnorder["bsd_shippingporter"];
                if (returnorder.HasValue("bsd_porteroption")) SubOrder["bsd_porteroption"] = returnorder["bsd_porteroption"];
                if (returnorder.HasValue("bsd_priceofporter")) SubOrder["bsd_priceofporter"] = returnorder["bsd_priceofporter"];
                if (returnorder.HasValue("bsd_pricepotter")) SubOrder["bsd_pricepotter"] = returnorder["bsd_pricepotter"];
                if (returnorder.HasValue("bsd_porter")) SubOrder["bsd_porter"] = returnorder["bsd_porter"];

                #endregion

                //throw new Exception("5");
                Entity suborderold = myService.service.Retrieve("bsd_suborder", ((EntityReference)returnorder["bsd_findsuborder"]).Id, new ColumnSet(true));

                if (suborderold.HasValue("bsd_paymentterm"))
                {
                    SubOrder["bsd_paymentterm"] = suborderold["bsd_paymentterm"];
                    Entity bsd_paymentterm = myService.service.Retrieve(((EntityReference)suborderold["bsd_paymentterm"]).LogicalName, ((EntityReference)suborderold["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                    SubOrder["bsd_datept"] = new decimal((int)bsd_paymentterm["bsd_date"]);
                }
                if (suborderold.HasValue("bsd_paymentmethod")) SubOrder["bsd_paymentmethod"] = suborderold["bsd_paymentmethod"];
                if (suborderold.HasValue("bsd_duedate")) SubOrder["bsd_duedate"] = suborderold["bsd_duedate"];

                Guid suborder_id = myService.service.Create(SubOrder);

                // throw new Exception("ok order");
                #endregion

                #region "Tao Suborder product"
                foreach (var orderproduct in list_orderproduct.Entities)
                {
                    Entity product = myService.service.Retrieve("product", ((EntityReference)orderproduct["bsd_product"]).Id, new ColumnSet(true));
                    //throw new Exception("6");

                    #region tao sub product
                    Entity sub_product = new Entity("bsd_suborderproduct");
                    sub_product["bsd_name"] = product["name"];
                    sub_product["bsd_returnorder"] = target;                    /////
                    //throw new Exception("7");
                    if (returnorder.HasValue("bsd_warehouse"))
                        sub_product["bsd_warehouse"] = returnorder["bsd_warehouse"];
                    sub_product["bsd_type"] = new OptionSetValue(861450005);
                    sub_product["bsd_suborder"] = new EntityReference("bsd_suborder", suborder_id);
                    sub_product["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                    sub_product["bsd_productid"] = orderproduct["bsd_productid"];
                    sub_product["bsd_priceperunit"] = orderproduct["bsd_giatruocthue"];
                    sub_product["bsd_orderquantity"] = (-1) * (decimal)orderproduct["bsd_orderquantity"];
                    sub_product["bsd_shipquantity"] = (-1) * (decimal)orderproduct["bsd_orderquantity"];
                    sub_product["bsd_standardquantity"] = orderproduct["bsd_standardquantity"];
                    sub_product["bsd_totalquantity"] = (-1) * (decimal)orderproduct["bsd_totalquantity"];
                    sub_product["bsd_remainningquantity"] = 0m;                         ////
                    sub_product["bsd_shippingprice"] = new Money(0m);                              ////
                    sub_product["bsd_porterprice"] = new Money(0m);                                ////
                    sub_product["bsd_shippedquantity"] = 0m;                            ////
                    sub_product["bsd_residualquantity"] = orderproduct["bsd_shipquantity"]; ////
                    sub_product["bsd_deliverystatus"] = new OptionSetValue(861450000);
                    sub_product["bsd_giatruocthue"] = orderproduct["bsd_giatruocthue"];
                    sub_product["bsd_giasauthue"] = orderproduct["bsd_giasauthue"];
                    sub_product["bsd_amount"] = new Money((-1) * ((Money)orderproduct["bsd_amount"]).Value);
                    if (orderproduct.HasValue("bsd_unit")) sub_product["bsd_unit"] = orderproduct["bsd_unit"];
                    if (orderproduct.HasValue("bsd_usingtax")) sub_product["bsd_usingtax"] = orderproduct["bsd_usingtax"];
                    if (orderproduct.HasValue("bsd_currencyexchange"))
                    {
                        sub_product["bsd_currencyexchangecurrency"] = new Money((-1) * ((Money)orderproduct["bsd_currencyexchange"]).Value);
                        sub_product["bsd_currencyexchange"] = ((-1) * ((Money)orderproduct["bsd_currencyexchange"]).Value);
                    }
                    if (returnorder.HasValue("transactioncurrencyid"))
                    {
                        sub_product["transactioncurrencyid"] = returnorder["transactioncurrencyid"];
                    }
                    //throw new Exception("8");
                    if (orderproduct.HasValue("bsd_itemsalestaxgroup")) sub_product["bsd_itemsalestaxgroup"] = orderproduct["bsd_itemsalestaxgroup"];
                    if (orderproduct.HasValue("bsd_itemsalestax"))
                    {
                        sub_product["bsd_itemsalestax"] = orderproduct["bsd_itemsalestax"];
                    }
                    if (orderproduct.HasValue("bsd_tax")) sub_product["bsd_tax"] = new Money((-1) * ((Money)orderproduct["bsd_tax"]).Value);
                    if (orderproduct.HasValue("bsd_vatprice")) sub_product["bsd_vatprice"] = orderproduct["bsd_vatprice"];
                    //throw new Exception("9");
                    if (orderproduct.HasValue("bsd_extendedamount")) sub_product["bsd_extendedamount"] = new Money((-1) * ((Money)orderproduct["bsd_extendedamount"]).Value);

                    myService.service.Create(sub_product);
                    #endregion

                }
                #endregion

                #region Update suborder
                Entity suborder_update = myService.service.Retrieve(SubOrder.LogicalName, suborder_id, new ColumnSet(true));
                UpdateSubOrder(suborder_update);
                #endregion

                UpdateReturnOrderStatusSaleReplace(returnorder.Id);

                myService.context.OutputParameters["ReturnId"] = suborder_id.ToString();

            }
            #endregion

            #region Delete
            else if (myService.context.MessageName == "Delete")
            {

                EntityReference target = (EntityReference)myService.context.InputParameters["Target"];
                myService.StartService();
                Entity suborder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                if (suborder.HasValue("bsd_type"))
                {
                    int order_type = ((OptionSetValue)suborder["bsd_type"]).Value;
                    if (order_type == 861450004)        //chỉ xét type suborder return
                    {
                        if (suborder.HasValue("bsd_returnorder"))
                        {
                            Entity returnorder = new Entity("bsd_returnorder", ((EntityReference)suborder["bsd_returnorder"]).Id);
                            returnorder["bsd_status"] = new OptionSetValue(861450000);
                            myService.service.Update(returnorder);
                        }
                    }
                    if (order_type == 861450005)
                    {
                        if (suborder.HasValue("bsd_returnorder"))
                        {
                            Entity returnorder = new Entity("bsd_returnorder", ((EntityReference)suborder["bsd_returnorder"]).Id);
                            returnorder["bsd_statussalereplace"] = new OptionSetValue(861450000);
                            myService.service.Update(returnorder);
                        }
                    }
                }

            }
            #endregion
        }
        public EntityCollection PriceList(Guid bsd_potentialcustomerId)
        {
            string xml_suborder = string.Format(@"<fetch version='1.0' top='1' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_suborder'>
                                    <attribute name='bsd_name' />
                                    <attribute name='createdon' />
                                    <attribute name='bsd_type' />
                                    <attribute name='bsd_suborderid' />
                                    <attribute name='bsd_pricelist' />
                                    <order attribute='createdon' descending='true' />
                                    <filter type='and'>
                                          <condition attribute='bsd_pricelist' operator='not-null' />
                                            <condition attribute='bsd_potentialcustomer' operator='eq'  uitype='account' value='" + bsd_potentialcustomerId + @"' />
                                    </filter>
                                  </entity>
                                </fetch>");
            EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(xml_suborder));
            if (!list.Entities.Any())
            {
                xml_suborder = string.Format(@"<fetch version='1.0' top='1' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_suborder'>
                                    <attribute name='bsd_name' />
                                    <attribute name='createdon' />
                                    <attribute name='bsd_type' />
                                    <attribute name='bsd_suborderid' />
                                    <attribute name='bsd_pricelist' />
                                    <order attribute='createdon' descending='true' />
                                    <filter type='and'>
                                          <condition attribute='bsd_pricelist' operator='not-null' />
                                    </filter>
                                  </entity>
                                </fetch>");
                list = myService.service.RetrieveMultiple(new FetchExpression(xml_suborder));
            }
            return list;
        }
        public void UpdateSubOrder(Entity suborder)
        {

            decimal total_tax = 0m;
            decimal detail_amount = 0m;
            decimal total_amount = 0m;
            decimal total_currency_exchange = 0;

            EntityCollection list_suborderproduct = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);

            if (list_suborderproduct.Entities.Any())
            {
                foreach (var suborder_product in list_suborderproduct.Entities)
                {
                    if (suborder_product.HasValue("bsd_tax")) total_tax += ((Money)suborder_product["bsd_tax"]).Value;
                    detail_amount += ((Money)suborder_product["bsd_amount"]).Value;
                    total_amount += ((Money)suborder_product["bsd_extendedamount"]).Value;
                    if (suborder_product.HasValue("bsd_currencyexchangecurrency")) total_currency_exchange += ((Money)suborder_product["bsd_currencyexchangecurrency"]).Value;
                }
            }

            Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
            new_suborder["bsd_detailamount"] = new Money(detail_amount);
            new_suborder["bsd_totaltax"] = new Money(total_tax);
            new_suborder["bsd_totalamount"] = new Money(total_amount);
            new_suborder["bsd_totalcurrencyexchange"] = new Money(0); //vinhlh Money 09-01-2018
            //new_suborder["bsd_description"] = "Return order no resource";
            myService.Update(new_suborder);

        }

        public void UpdateReturnOrderStatus(Guid returnorderid)
        {
            Entity returnorder = new Entity("bsd_returnorder", returnorderid);
            returnorder["bsd_status"] = new OptionSetValue(861450001);
            myService.service.Update(returnorder);

        }
        public void UpdateReturnOrderStatusSaleReplace(Guid returnorderid)
        {
            Entity returnorder = new Entity("bsd_returnorder", returnorderid);
            returnorder["bsd_statussalereplace"] = new OptionSetValue(861450001);
            myService.service.Update(returnorder);
        }

    }
}
