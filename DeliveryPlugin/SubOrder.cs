using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DeliveryPlugin.Service;
using Microsoft.Crm.Sdk.Messages;
using Plugin;
using DeliveryPlugin.Model;
using System.ServiceModel;
using DeliveryPlugin.ServiceReferenceAIF;

namespace DeliveryPlugin
{
    public class SubOrder : IPlugin
    {
        private MyService myService;
        public string _userName = "";
        public string _passWord = "";
        public string _company = "";
        public string _port = "";
        public string _domain = "";
        public void Execute(IServiceProvider serviceProvider)
        {
            string log = "start";
            myService = new MyService(serviceProvider);

            if (myService.context.Depth > 1) return;
            myService.StartService();
         
            #region bsd_Action_CreateSubOrder
            if (myService.context.MessageName == "bsd_Action_CreateSubOrder")
            {
                try
                {
                    #region
                    myService.StartService();
                    EntityReference target = myService.getTargetEntityReference();
                    Entity order = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

                    int order_type = ((OptionSetValue)order["bsd_type"]).Value;
                    
                    #region Chặn xuất khẩu
                    //if (order_type == 100000000 || order_type == 100000001)
                    //{
                    //    throw new Exception("Do not create Sub Order from Contract!");
                    //}
                    #endregion

                    if (order_type == 861450003 || order_type == 861450002) return;
                    bool multiple_address = (bool)order["bsd_multipleaddress"];
                    if (multiple_address) return;
                    EntityCollection list_orderproduct = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='salesorderdetail'>
                                    <all-attributes />
                                    <filter type='and'>
                                      <condition attribute='salesorderid' operator='eq' uitype='salesorder' value='" + target.Id + @"' />
                                      <condition attribute='bsd_remainingquantity' operator='gt' value='0' />
                                    </filter>
                                  </entity>
                                </fetch>");
                    log = "start itemsalestax";
                    #region Tạo list custorm, để lấy distinct những line product nào có usingtax và itemsalestax trùng nhau !
                    List<OrderProduct_Custom> list_orderproduct_customer = new List<OrderProduct_Custom>();
                    foreach (var orderproduct in list_orderproduct.Entities)
                    {
                        OrderProduct_Custom op = new OrderProduct_Custom();
                        op.Item_Sales_Tax = (orderproduct.HasValue("bsd_itemsalestax")) ? (decimal)orderproduct["bsd_itemsalestax"] : 0;
                        op.UsingTax = (bool)orderproduct["bsd_usingtax"];
                        op.ShippingYesNo = (bool)orderproduct["bsd_usingshipping"];
                        if (multiple_address)
                        {
                            op.DeliveryFrom = ((OptionSetValue)orderproduct["bsd_deliveryfrom"]).Value;
                            op.Site = ((EntityReference)orderproduct["bsd_site"]).Id;
                            op.SiteAddress = ((EntityReference)orderproduct["bsd_siteaddress"]).Id;
                            op.ShippingFromAddress = ((EntityReference)orderproduct["bsd_shippingfromaddress"]).Id;
                            op.ShippingAddress = ((EntityReference)orderproduct["bsd_shippingaddress"]).Id;
                            op.ReceiptCustomer = ((EntityReference)orderproduct["bsd_shiptoaccount"]).Id;
                            op.CustomerAddress = ((EntityReference)orderproduct["bsd_shiptoaddress"]).Id;

                            if (orderproduct.HasValue("bsd_port")) op.Port = ((EntityReference)orderproduct["bsd_port"]).Id;
                            if (orderproduct.HasValue("bsd_addressport")) op.AddressPort = ((EntityReference)orderproduct["bsd_addressport"]).Id;
                        }

                        list_orderproduct_customer.Add(op);
                    }

                    if (multiple_address)
                    {
                        list_orderproduct_customer = list_orderproduct_customer.GroupBy(i => new { i.Item_Sales_Tax, i.UsingTax, i.ShippingYesNo, i.Site, i.DeliveryFrom, i.ReceiptCustomer, i.CustomerAddress, i.ShippingFromAddress, i.ShippingAddress }, (key, group) => group.First()).ToList();
                    }
                    else
                    {
                        list_orderproduct_customer = list_orderproduct_customer.GroupBy(i => new { i.Item_Sales_Tax, i.UsingTax, i.ShippingYesNo }, (key, group) => group.First()).ToList();
                    }
                    #endregion
                    log = "end itemsalestax";
                    foreach (var orderproduct_cus in list_orderproduct_customer)
                    {
                        log = "start create suborder";
                        #region Tạo Sub Order
                        Entity account = myService.service.Retrieve("account", ((EntityReference)order["customerid"]).Id, new ColumnSet(true));
                        Entity SubOrder = new Entity("bsd_suborder");
                        SubOrder["bsd_type"] = new OptionSetValue(861450002);
                        SubOrder["bsd_date"] = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                        SubOrder["bsd_order"] = new EntityReference(order.LogicalName, order.Id);
                        SubOrder["bsd_potentialcustomer"] = order["customerid"];
                        SubOrder["bsd_multipleaddress"] = multiple_address;
                        log = "create suborder 1";
                        if (order.HasValue("bsd_economiccontract")) SubOrder["bsd_economiccontract"] = order["bsd_economiccontract"];
                        log = "create suborder 2";
                        if (order.HasValue("transactioncurrencyid")) SubOrder["transactioncurrencyid"] = order["transactioncurrencyid"];
                        log = "create suborder 3";
                        if (order.HasValue("bsd_address")) SubOrder["bsd_addresscontractoffer"] = order["bsd_address"];
                        log = "create suborder 4";
                        if (order.HasValue("bsd_telephone")) SubOrder["bsd_telephone"] = order["bsd_telephone"];
                        log = "create suborder 5";
                        if (order.HasValue("bsd_taxregistration")) SubOrder["bsd_taxregistration"] = order["bsd_taxregistration"];
                        log = "create suborder 6";
                        if (order.HasValue("bsd_contact")) SubOrder["bsd_contact"] = order["bsd_contact"];
                        log = "create suborder 7";
                        if (order.HasValue("bsd_customercode")) SubOrder["bsd_customercode"] = order["bsd_customercode"];
                        log = "create suborder 8";
                        if (order.HasValue("pricelevelid")) SubOrder["bsd_pricelist"] = order["pricelevelid"];
                        log = "create suborder 9";
                        if (order.HasValue("bsd_billtoaddress")) SubOrder["bsd_billtoaddress"] = order["bsd_billtoaddress"];
                        log = "create suborder 10";
                        if (order.HasValue("bsd_deliverymethod")) SubOrder["bsd_deliverymethod"] = order["bsd_deliverymethod"];
                        log = "create suborder 11";
                        if (order.HasValue("bsd_accompanyingdocuments")) SubOrder["bsd_requireddocuments"] = order["bsd_accompanyingdocuments"];
                        log = "create suborder 12";
                        if (order.HasValue("shipto_freighttermscode")) SubOrder["bsd_shiptofreightterms"] = order["shipto_freighttermscode"];
                        log = "create suborder 13";
                        // 1 địa chỉ thì lấy địa chỉ từ màn hình quote
                        SubOrder["bsd_deliveryfrom"] = order["bsd_deliveryfrom"];
                        log = "create suborder 14";
                        SubOrder["bsd_site"] = order["bsd_site"];
                        log = "create suborder 15";
                        SubOrder["bsd_siteaddress"] = order["bsd_siteaddress"];
                        log = "create suborder 16";
                        SubOrder["bsd_shippingfromaddress"] = order["bsd_shippingfromaddress"];
                        log = "create suborder 17";
                        SubOrder["bsd_shippingaddress"] = order["bsd_shippingaddress"];
                        log = "create suborder 18";
                        SubOrder["bsd_shiptoaccount"] = order["bsd_shiptoaccount"];
                        log = "create suborder 19";
                        SubOrder["bsd_shiptoaddress"] = order["bsd_shiptoaddress"];
                        log = "create suborder 20";
                        if (order.HasValue("bsd_port"))
                        {
                            SubOrder["bsd_port"] = order["bsd_port"];
                            SubOrder["bsd_addressport"] = order["bsd_addressport"];
                        }
                        log = "create suborder 21";
                        if (order.HasValue("bsd_fromdate")) SubOrder["bsd_fromdate"] = order["bsd_fromdate"];
                        log = "create suborder 22";
                        if (order.HasValue("bsd_factory")) SubOrder["bsd_factory"] = order["bsd_factory"];
                        log = "create suborder 23";
                        if (order.HasValue("bsd_todate")) SubOrder["bsd_todate"] = order["bsd_todate"];
                        log = "create suborder 24";
                        if (order.HasValue("bsd_paymentterm"))
                        {
                            SubOrder["bsd_paymentterm"] = order["bsd_paymentterm"];
                            Entity bsd_paymentterm = myService.service.Retrieve(((EntityReference)order["bsd_paymentterm"]).LogicalName, ((EntityReference)order["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                            log = "create suborder 24.1";
                            SubOrder["bsd_datept"] = new decimal((int)bsd_paymentterm["bsd_date"]);
                            log = "create suborder 24.2";
                        }
                        log = "create suborder 25";
                        if (order.HasValue("bsd_paymentmethod")) SubOrder["bsd_paymentmethod"] = order["bsd_paymentmethod"];
                        log = "create suborder 26";
                        if (order.HasValue("bsd_invoiceaccount")) SubOrder["bsd_invoiceaccount"] = order["bsd_invoiceaccount"];
                        log = "create suborder 27";
                        if (order.HasValue("bsd_invoicenameaccount")) SubOrder["bsd_invoicenameaccount"] = order["bsd_invoicenameaccount"];
                        log = "create suborder 28";
                        if (order.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addressinvoiceaccount"] = order["bsd_addressinvoiceaccount"];
                        log = "create suborder 29";
                        if (order.HasValue("bsd_contactinvoiceaccount")) SubOrder["bsd_contactinvoiceaccount"] = order["bsd_contactinvoiceaccount"];
                        log = "create suborder 30";
                        if (order.HasValue("bsd_address")) SubOrder["bsd_addresscustomeraccount"] = order["bsd_address"];
                        log = "create suborder 31";
                        SubOrder["bsd_unitdefault"] = order["bsd_unitdefault"];
                        log = "create suborder 32";
                        SubOrder["bsd_priceincludeshippingporter"] = order["bsd_priceincludeshippingporter"];
                        log = "create suborder 33";

                        if (order.HasValue("bsd_historycustomername")) SubOrder["bsd_historycustomeraccount"] = order["bsd_historycustomername"];
                        if (order.HasValue("bsd_historyinvoicename")) SubOrder["bsd_historyinvoicename"] = order["bsd_historyinvoicename"];
                        if (order.HasValue("bsd_historyreceiptcustomer")) SubOrder["bsd_historyreceiptcustomer"] = order["bsd_historyreceiptcustomer"];

                        #region Lấy Exchange Rate 

                        decimal bsd_exchangeratevalue = 1m;

                        Guid account_currency = ((EntityReference)order["transactioncurrencyid"]).Id;

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
                        if (!account_currency.Equals(bsd_currencydefault.Id)) // nếu không bằng với unit default.
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
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>", account_currency
                                , bsd_currencydefault.Id
                                , bsd_bankdefault.Id,
                            myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service).ToString("yyyy-MM-dd"))));

                            if (list_exchangerate.Entities.Any())
                            {
                                Entity ex_changerate = list_exchangerate.Entities.First();
                                SubOrder["bsd_exchangerate"] = new EntityReference(ex_changerate.LogicalName, ex_changerate.Id);
                                SubOrder["bsd_bank"] = new EntityReference(bsd_bankdefault.LogicalName, bsd_bankdefault.Id);
                                log = "create suborder 33.1";
                                bsd_exchangeratevalue = (decimal)ex_changerate["bsd_exchangerate"];
                            }
                            else
                            {
                                //throw new Exception(account_currency + "----" + bsd_currencydefault.Id);
                                throw new Exception("Please create a new Exchage rate");
                            }
                        }
                        else
                        {
                            bsd_exchangeratevalue = 1m;
                        }
                        log = "create suborder 34";
                        SubOrder["bsd_exchangeratevalue"] = bsd_exchangeratevalue;
                        log = "create suborder 35";
                        #endregion

                        #region Due date
                        DateTime suborder_date = DateTime.Now;
                        DateTime? due_date = null;
                        int date_paymentterm = 0;
                        if (account.HasValue("bsd_paymentterm")) // 15Days
                        {
                            Entity bsd_paymentterm = myService.service.Retrieve("bsd_paymentterm", ((EntityReference)account["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                            date_paymentterm = (int)bsd_paymentterm["bsd_date"];
                        }
                        if (account.HasValue("bsd_paymentday"))
                        {
                            int payment_day_ofmonth = (int)account["bsd_paymentday"]; // ngày trả nợ trong tháng
                            DateTime tong = suborder_date.AddDays(date_paymentterm); // ngày đơn hàng + ngày trả sau.
                            if (tong.Day > payment_day_ofmonth) // tổng lớn hơn ngày phải trả. -> dời qua tháng sau, lấy ngày phải trả.
                            {
                                due_date = new DateTime(tong.Year, tong.AddMonths(1).Month, 1);
                                due_date = ((DateTime)due_date).AddDays(payment_day_ofmonth - 1);
                            }
                            else
                            {
                                due_date = new DateTime(tong.Year, tong.Month, 1);
                                due_date = ((DateTime)due_date).AddDays(payment_day_ofmonth - 1);
                            }
                        }
                        else
                        {
                            due_date = suborder_date.AddDays(date_paymentterm);
                        }
                        due_date = myService.RetrieveLocalTimeFromUTCTime((DateTime)due_date, myService.service);
                        if (((DateTime)due_date).DayOfWeek == DayOfWeek.Saturday)
                        {
                            due_date = ((DateTime)due_date).AddDays(2);
                        }
                        else if (((DateTime)due_date).DayOfWeek == DayOfWeek.Sunday)
                        {
                            due_date = ((DateTime)due_date).AddDays(1);
                        }
                        SubOrder["bsd_duedate"] = due_date;
                        #endregion

                        #region Chuyển shiping porter tax
                        if (order.HasValue("bsd_saletaxgroup")) SubOrder["bsd_saletaxgroup"] = order["bsd_saletaxgroup"];
                        if (order.HasValue("bsd_requestporter")) SubOrder["bsd_requestporter"] = order["bsd_requestporter"];
                        if (order.HasValue("bsd_transportation")) SubOrder["bsd_transportation"] = order["bsd_transportation"];
                        if (order.HasValue("bsd_shippingdeliverymethod")) SubOrder["bsd_shippingdeliverymethod"] = order["bsd_shippingdeliverymethod"];
                        if (order.HasValue("bsd_truckload")) SubOrder["bsd_truckload"] = order["bsd_truckload"];
                        if (order.HasValue("bsd_unitshipping")) SubOrder["bsd_unitshipping"] = order["bsd_unitshipping"];
                        if (order.HasValue("bsd_shippingpricelistname")) SubOrder["bsd_shippingpricelistname"] = order["bsd_shippingpricelistname"];
                        if (order.HasValue("bsd_priceoftransportationn")) SubOrder["bsd_priceoftransportationn"] = order["bsd_priceoftransportationn"];
                        if (order.HasValue("bsd_shippingporter")) SubOrder["bsd_shippingporter"] = order["bsd_shippingporter"];
                        if (order.HasValue("bsd_porteroption")) SubOrder["bsd_porteroption"] = order["bsd_porteroption"];
                        if (order.HasValue("bsd_priceofporter")) SubOrder["bsd_priceofporter"] = order["bsd_priceofporter"];
                        if (order.HasValue("bsd_pricepotter")) SubOrder["bsd_pricepotter"] = order["bsd_pricepotter"];
                        if (order.HasValue("bsd_porter")) SubOrder["bsd_porter"] = order["bsd_porter"];
                        #endregion

                        Guid suborder_id = myService.service.Create(SubOrder);

                        #endregion
                        log = "end create suborder";
                        log = "start create suborder product";
                        #region Tạo Suborder Product và update lại giá trên suborder

                        decimal grandtotal = 0m;
                        foreach (var orderproduct in list_orderproduct.Entities)
                        {
                            log = "create suborder product 1";
                            bool condition =
                            orderproduct_cus.UsingTax == (bool)orderproduct["bsd_usingtax"]
                            && orderproduct_cus.ShippingYesNo == (bool)orderproduct["bsd_usingshipping"]
                            && orderproduct_cus.Item_Sales_Tax == (orderproduct.HasValue("bsd_itemsalestax") ? (decimal)orderproduct["bsd_itemsalestax"] : 0);
                            log = "create suborder product 2";
                            if (multiple_address)
                            {
                               
                                condition = condition
                                && (orderproduct_cus.DeliveryFrom == ((OptionSetValue)orderproduct["bsd_deliveryfrom"]).Value
                                && orderproduct_cus.ShippingFromAddress == ((EntityReference)orderproduct["bsd_shippingfromaddress"]).Id
                                && orderproduct_cus.ShippingAddress == ((EntityReference)orderproduct["bsd_shippingaddress"]).Id
                                && orderproduct_cus.ReceiptCustomer == ((EntityReference)orderproduct["bsd_shiptoaccount"]).Id
                                && orderproduct_cus.CustomerAddress == ((EntityReference)orderproduct["bsd_shiptoaddress"]).Id
                               );
                            }

                            if (condition)
                            {
                                bool con = true;
                                decimal remaining_quantity = (decimal)orderproduct["bsd_remainingquantity"];
                                while (con == true)
                                {
                                    con = false;
                                    log = "create suborder product 3";
                                    Entity product = myService.service.Retrieve("product", ((EntityReference)orderproduct["productid"]).Id, new ColumnSet(true));
                                    log = "create suborder product 4";
                                    decimal order_quantity = (decimal)orderproduct["quantity"];
                                    log = "create suborder product 5";
                                    decimal suborder_quantity = (decimal)orderproduct["bsd_suborderquantity"];
                                    log = "create suborder product 6";
                                    decimal price_per_unit = ((Money)orderproduct["priceperunit"]).Value;
                                    log = "create suborder product 7";
                                    decimal shippingprice = ((decimal)orderproduct["bsd_giashipsauthue_full"]);
                                    log = "create suborder product 8";
                                    decimal porter_price = ((decimal)orderproduct["bsd_porterprice_full"]);
                                    log = "create suborder product 9";
                                    decimal vat = orderproduct.HasValue("bsd_vatprice_full") ? ((decimal)orderproduct["bsd_vatprice_full"]) : 0m;
                                    log = "create suborder product 10";
                                    decimal tax = vat * remaining_quantity;
                                    decimal giatruocthue = price_per_unit + shippingprice + porter_price;
                                    decimal giasauthue = giatruocthue + vat;
                                    decimal amount = giatruocthue * remaining_quantity;
                                    decimal extendedamount = giasauthue * remaining_quantity;
                                    decimal currency_exchange = bsd_exchangeratevalue * extendedamount;
                                    grandtotal += currency_exchange;
                                    //if (currency_exchange > 100000000000)
                                    //{
                                    //    con = true;
                                    //    remaining_quantity = 0m; 
                                    //}
                                    //else
                                    {

                                        #region Tạo sub product
                                        Entity sub_product = new Entity("bsd_suborderproduct");
                                        log = "create suborder product 11";
                                        sub_product["bsd_name"] = product["name"];
                                        sub_product["bsd_type"] = new OptionSetValue(861450002);
                                        sub_product["bsd_order"] = target;
                                        sub_product["bsd_suborder"] = new EntityReference("bsd_suborder", suborder_id);
                                        sub_product["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                                        if (orderproduct.HasValue("bsd_productid")) sub_product["bsd_productid"] = orderproduct["bsd_productid"];
                                        if (orderproduct.HasValue("bsd_descriptionproduct")) sub_product["bsd_descriptionproduct"] = orderproduct["bsd_descriptionproduct"];
                                        log = "create suborder product 12";
                                        sub_product["bsd_priceperunit"] = new Money(price_per_unit);
                                        log = "create suborder product 13";
                                        sub_product["bsd_orderquantity"] = orderproduct["quantity"];
                                        log = "create suborder product 14";
                                        sub_product["bsd_standardquantity"] = orderproduct["bsd_standardquantity"];
                                        log = "create suborder product 15";
                                        sub_product["bsd_totalquantity"] = orderproduct["bsd_totalquantity"];
                                        log = "create suborder product 16";
                                        sub_product["bsd_shippedquantity"] = 0m;
                                        log = "create suborder product 17";
                                        sub_product["bsd_residualquantity"] = remaining_quantity;
                                        log = "create suborder product 18";
                                        sub_product["bsd_giatruocthue"] = new Money(giatruocthue);
                                        log = "create suborder product 19";
                                        sub_product["bsd_giasauthue"] = new Money(giasauthue);
                                        log = "create suborder product 20";
                                        sub_product["bsd_amount"] = new Money(amount);
                                        log = "create suborder product 21";
                                        sub_product["bsd_unit"] = orderproduct["uomid"];
                                        log = "create suborder product 22";
                                        sub_product["bsd_usingtax"] = orderproduct["bsd_usingtax"];
                                        log = "create suborder product 23";
                                        sub_product["bsd_currencyexchangecurrency"] = new Money(currency_exchange);
                                        log = "create suborder product 24";
                                        sub_product["bsd_currencyexchangetext"] = currency_exchange.DecimalToStringHideSymbol();
                                        log = "create suborder product 25";
                                        sub_product["transactioncurrencyid"] = order["transactioncurrencyid"];
                                        log = "create suborder product 26";
                                        sub_product["bsd_shippingprice"] = new Money(shippingprice);
                                        log = "create suborder product 27";
                                        sub_product["bsd_porterprice"] = new Money(porter_price);
                                        log = "create suborder product 28";
                                        sub_product["bsd_shipquantity"] = remaining_quantity;
                                        log = "create suborder product 29";
                                        sub_product["bsd_newquantity"] = remaining_quantity;
                                        log = "create suborder product 30";
                                        sub_product["bsd_extendedamount"] = new Money(extendedamount);
                                        log = "create suborder product 31";
                                        if (orderproduct.HasValue("bsd_itemsalestaxgroup"))
                                        {
                                            sub_product["bsd_itemsalestaxgroup"] = orderproduct["bsd_itemsalestaxgroup"];
                                        }
                                        if (orderproduct.HasValue("bsd_itemsalestax"))
                                        {
                                            sub_product["bsd_itemsalestax"] = orderproduct["bsd_itemsalestax"];
                                        }
                                        log = "create suborder product 32";
                                        if (orderproduct_cus.UsingTax)
                                        {
                                            log = "create suborder product 32.1";
                                            sub_product["bsd_tax"] = new Money(tax);
                                            log = "create suborder product 32.2";
                                            sub_product["bsd_vatprice"] = new Money(vat);
                                        }
                                        else
                                        {
                                            log = "create suborder product 32.3";
                                            sub_product["bsd_tax"] = null;
                                            log = "create suborder product 32.4";
                                            sub_product["bsd_vatprice"] = null;
                                        }
                                        log = "create suborder product 33";
                                        myService.service.Create(sub_product);
                                        log = "create suborder product 34";
                                        #endregion

                                        #region cập nhật orderproduct
                                        Entity new_orderproduct = new Entity(orderproduct.LogicalName, orderproduct.Id);
                                        decimal new_suborderquantity = suborder_quantity + remaining_quantity;
                                        new_orderproduct["bsd_suborderquantity"] = new_suborderquantity;
                                        new_orderproduct["bsd_remainingquantity"] = order_quantity - new_suborderquantity;
                                        log = "create suborder product 36";
                                        myService.service.Update(new_orderproduct);
                                        log = "create suborder product 36";
                                        #endregion
                                    }
                                }
                            }
                        }

                        #region cập nhật số lượng = 0 cho tất cả suborder nếu grand total vượt quá
                        //Huy: khóa lại, vì đã sửa field decimal -> currency, cần mở ra để tạo đơn hàng giá trị lớn
                        //if (grandtotal > 100000000000)
                        //{
                        //    SuborderService subService = new SuborderService(myService);
                        //    EntityCollection list_product = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder_id);
                        //    foreach (var suborder_product in list_product.Entities)
                        //    {
                        //        // pre quantity để trừ đi số lượng ở trên kia đã gán mà chưa gán lại = 0 khi grand total quá trăm tỷ.
                        //        decimal current_shipquantity = (decimal)suborder_product["bsd_shipquantity"];
                        //        suborder_product["bsd_shipquantity"] = 0m;
                        //        subService.Create_Update_Suborder_Product(suborder_product, 2, false, current_shipquantity);
                        //    }
                        //}
                        #endregion

                        #region Update suborder
                        Entity suborder_update = myService.service.Retrieve(SubOrder.LogicalName, suborder_id, new ColumnSet(true));
                        new SuborderService(myService).UpdateSubOrder(suborder_update);
                        #endregion

                        #endregion
                        log = "end create suborder product";
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message + " log details " + log);
                }
            }
            #endregion

            #region bsd_Action_QuoteToSuborder
            else if (myService.context.MessageName == "bsd_Action_QuoteToSuborder")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
                // quote
                Entity order = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                int quote_type = ((OptionSetValue)order["bsd_quotationtype"]).Value;

                #region Kiểm tra tạo hợp đồng chưa !
                EntityCollection list_order = myService.RetrieveOneCondition("salesorder", "quoteid", order.Id);
                if (list_order.Entities.Any()) throw new Exception("Economic Contract or Other Contract has been created for this Quote !");
                #endregion

                #region Chặn xuất khẩu
                if (quote_type == 861450001 || quote_type == 861450004)
                {
                    throw new Exception("Do not create Sub Order from Quote!");
                }
                #endregion

                if (quote_type != 861450000 && quote_type != 861450001 && quote_type != 861450004) return;
                bool multiple_address = (bool)order["bsd_multipleaddress"];
                if (multiple_address) return;
                EntityCollection list_orderproduct = myService.service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='quotedetail'>
                                            <all-attributes />
                                            <filter type='and'>
                                              <condition attribute='quoteid' operator='eq' uitype='quote' value='" + target.Id + @"' />
                                              <condition attribute='bsd_remainingquantity' operator='gt' value='0' />
                                            </filter>
                                          </entity>
                                        </fetch>"));

                #region Tạo list custorm, để lấy distinct những line product nào có usingtax và itemsalestax trùng nhau !
                List<OrderProduct_Custom> list_orderproduct_customer = new List<OrderProduct_Custom>();
                foreach (var orderproduct in list_orderproduct.Entities)
                {

                    OrderProduct_Custom op = new OrderProduct_Custom();
                    op.Item_Sales_Tax = (orderproduct.HasValue("bsd_itemsalestax")) ? (decimal)orderproduct["bsd_itemsalestax"] : 0;
                    op.UsingTax = (bool)orderproduct["bsd_usingtax"];
                    op.ShippingYesNo = (bool)orderproduct["bsd_usingshipping"];
                    if (multiple_address)
                    {
                        op.DeliveryFrom = ((OptionSetValue)orderproduct["bsd_deliveryfrom"]).Value;
                        op.Site = ((EntityReference)orderproduct["bsd_site"]).Id;
                        op.SiteAddress = ((EntityReference)orderproduct["bsd_siteaddress"]).Id;
                        op.ShippingFromAddress = ((EntityReference)orderproduct["bsd_shippingfromaddress"]).Id;
                        op.ShippingAddress = ((EntityReference)orderproduct["bsd_shippingaddress"]).Id;
                        op.ReceiptCustomer = ((EntityReference)orderproduct["bsd_partnerscompany"]).Id;
                        op.CustomerAddress = ((EntityReference)orderproduct["bsd_shiptoaddress"]).Id;

                        if (orderproduct.HasValue("bsd_port")) op.Port = ((EntityReference)orderproduct["bsd_port"]).Id;
                        if (orderproduct.HasValue("bsd_addressport")) op.AddressPort = ((EntityReference)orderproduct["bsd_addressport"]).Id;

                    }
                    list_orderproduct_customer.Add(op);
                }

                if (multiple_address)
                {
                    list_orderproduct_customer = list_orderproduct_customer.GroupBy(i => new { i.Item_Sales_Tax, i.UsingTax, i.ShippingYesNo, i.Site, i.DeliveryFrom, i.ReceiptCustomer, i.CustomerAddress, i.ShippingFromAddress, i.ShippingAddress }, (key, group) => group.First()).ToList();
                }
                else
                {
                    list_orderproduct_customer = list_orderproduct_customer.GroupBy(i => new { i.Item_Sales_Tax, i.UsingTax, i.ShippingYesNo }, (key, group) => group.First()).ToList();
                }

                #endregion

                foreach (var orderproduct_cus in list_orderproduct_customer)
                {
                    #region Tạo Sub Order
                    Entity account = myService.service.Retrieve("account", ((EntityReference)order["customerid"]).Id, new ColumnSet(true));

                    myService.SetState(target.Id, target.LogicalName, 0, 1); // Quote draff
                    Entity SubOrder = new Entity("bsd_suborder");
                    SubOrder["bsd_type"] = new OptionSetValue(861450001);
                    SubOrder["bsd_date"] = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                    SubOrder["bsd_quote"] = new EntityReference(order.LogicalName, order.Id);
                    SubOrder["bsd_potentialcustomer"] = order["customerid"];
                    SubOrder["bsd_multipleaddress"] = multiple_address;
                    if (order.HasValue("bsd_economiccontract")) SubOrder["bsd_economiccontract"] = order["bsd_economiccontract"];
                    if (order.HasValue("transactioncurrencyid")) SubOrder["transactioncurrencyid"] = order["transactioncurrencyid"];
                    if (order.HasValue("bsd_address")) SubOrder["bsd_addresscontractoffer"] = order["bsd_address"];
                    if (order.HasValue("bsd_telephone")) SubOrder["bsd_telephone"] = order["bsd_telephone"];
                    if (order.HasValue("bsd_taxregistration")) SubOrder["bsd_taxregistration"] = order["bsd_taxregistration"];
                    if (order.HasValue("bsd_contact")) SubOrder["bsd_contact"] = order["bsd_contact"];

                    if (order.HasValue("bsd_customercode")) SubOrder["bsd_customercode"] = order["bsd_customercode"];
                    if (order.HasValue("pricelevelid")) SubOrder["bsd_pricelist"] = order["pricelevelid"];
                    if (order.HasValue("bsd_billtoaddress")) SubOrder["bsd_billtoaddress"] = order["bsd_billtoaddress"];
                    if (order.HasValue("bsd_deliverymethod")) SubOrder["bsd_deliverymethod"] = order["bsd_deliverymethod"];
                    if (order.HasValue("bsd_accompanyingdocument")) SubOrder["bsd_requireddocuments"] = order["bsd_accompanyingdocument"];
                    if (order.HasValue("shipto_freighttermscode")) SubOrder["bsd_shiptofreightterms"] = order["shipto_freighttermscode"];

                    // 1 địa chỉ thì lấy địa chỉ từ màn hình quote
                    SubOrder["bsd_deliveryfrom"] = order["bsd_deliveryfrom"];
                    SubOrder["bsd_site"] = order["bsd_site"];
                    SubOrder["bsd_siteaddress"] = order["bsd_siteaddress"];
                    SubOrder["bsd_shippingfromaddress"] = order["bsd_shippingfromaddress"];
                    SubOrder["bsd_shippingaddress"] = order["bsd_shippingaddress"];
                    SubOrder["bsd_shiptoaccount"] = order["bsd_partnerscompany"];
                    SubOrder["bsd_shiptoaddress"] = order["bsd_shiptoaddress"];
                    if (order.HasValue("bsd_port"))
                    {
                        SubOrder["bsd_port"] = order["bsd_port"];
                        SubOrder["bsd_addressport"] = order["bsd_addressport"];
                    }

                    if (order.HasValue("bsd_fromdate")) SubOrder["bsd_fromdate"] = order["bsd_fromdate"];
                    if (order.HasValue("bsd_factory")) SubOrder["bsd_factory"] = order["bsd_factory"];
                    if (order.HasValue("bsd_todate")) SubOrder["bsd_todate"] = order["bsd_todate"];

                    if (order.HasValue("bsd_paymentterm"))
                    {
                        SubOrder["bsd_paymentterm"] = order["bsd_paymentterm"];
                        Entity bsd_paymentterm = myService.service.Retrieve(((EntityReference)order["bsd_paymentterm"]).LogicalName, ((EntityReference)order["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                        SubOrder["bsd_datept"] = new decimal((int)bsd_paymentterm["bsd_date"]);
                    }

                    if (order.HasValue("bsd_paymentmethod")) SubOrder["bsd_paymentmethod"] = order["bsd_paymentmethod"];
                    if (order.HasValue("bsd_invoiceaccount")) SubOrder["bsd_invoiceaccount"] = order["bsd_invoiceaccount"];
                    if (order.HasValue("bsd_invoicenameaccount")) SubOrder["bsd_invoicenameaccount"] = order["bsd_invoicenameaccount"];
                    if (order.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addressinvoiceaccount"] = order["bsd_addressinvoiceaccount"];
                    if (order.HasValue("bsd_contactinvoiceaccount")) SubOrder["bsd_contactinvoiceaccount"] = order["bsd_contactinvoiceaccount"];
                    if (order.HasValue("bsd_address")) SubOrder["bsd_addresscustomeraccount"] = order["bsd_address"];
                    if (order.HasValue("bsd_unitdefault")) SubOrder["bsd_unitdefault"] = order["bsd_unitdefault"];
                    SubOrder["bsd_priceincludeshippingporter"] = order["bsd_priceincludeshippingporter"];

                    if (order.HasValue("bsd_historycustomername")) SubOrder["bsd_historycustomeraccount"] = order["bsd_historycustomername"];
                    if (order.HasValue("bsd_historyinvoicename")) SubOrder["bsd_historyinvoicename"] = order["bsd_historyinvoicename"];
                    if (order.HasValue("bsd_historyreceiptcustomer")) SubOrder["bsd_historyreceiptcustomer"] = order["bsd_historyreceiptcustomer"];

                    #region Lấy Exchange Rate 

                    decimal bsd_exchangeratevalue = 1m;

                    Guid account_currency = ((EntityReference)order["transactioncurrencyid"]).Id;

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

                    if (!account_currency.Equals(bsd_currencydefault.Id)) // nếu không bằng với unit default.
                    {
                        EntityCollection list_exchangerate = myService.FetchXml(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
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
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>", account_currency
                            , bsd_currencydefault.Id
                            , bsd_bankdefault.Id,
                        myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service).ToString("yyyy-MM-dd")));

                        if (list_exchangerate.Entities.Any())
                        {
                            Entity ex_changerate = list_exchangerate.Entities.First();
                            SubOrder["bsd_exchangerate"] = new EntityReference(ex_changerate.LogicalName, ex_changerate.Id);
                            SubOrder["bsd_bank"] = new EntityReference(bsd_bankdefault.LogicalName, bsd_bankdefault.Id);
                            bsd_exchangeratevalue = (decimal)ex_changerate["bsd_exchangerate"];
                        }
                        else
                        {
                            throw new Exception("Please create a new Exchage rate");
                        }
                    }
                    else
                    {
                        bsd_exchangeratevalue = 1m;
                    }
                    SubOrder["bsd_exchangeratevalue"] = bsd_exchangeratevalue;
                    #endregion

                    #region Due date
                    DateTime suborder_date = DateTime.Now;
                    DateTime? due_date = null;
                    int date_paymentterm = 0;
                    if (account.HasValue("bsd_paymentterm")) // 15Days
                    {
                        Entity bsd_paymentterm = myService.service.Retrieve("bsd_paymentterm", ((EntityReference)account["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                        date_paymentterm = (int)bsd_paymentterm["bsd_date"];
                    }
                    if (account.HasValue("bsd_paymentday"))
                    {
                        int payment_day_ofmonth = (int)account["bsd_paymentday"]; // ngày trả nợ trong tháng
                        DateTime tong = suborder_date.AddDays(date_paymentterm); // ngày đơn hàng + ngày trả sau.
                        if (tong.Day > payment_day_ofmonth) // tổng lớn hơn ngày phải trả. -> dời qua tháng sau, lấy ngày phải trả.
                        {
                            due_date = new DateTime(tong.Year, tong.AddMonths(1).Month, 1);
                            due_date = ((DateTime)due_date).AddDays(payment_day_ofmonth - 1);
                        }
                        else
                        {
                            due_date = new DateTime(tong.Year, tong.Month, 1);
                            due_date = ((DateTime)due_date).AddDays(payment_day_ofmonth - 1);
                        }
                    }
                    else
                    {
                        due_date = suborder_date.AddDays(date_paymentterm);
                    }
                    due_date = myService.RetrieveLocalTimeFromUTCTime((DateTime)due_date, myService.service);
                    if (((DateTime)due_date).DayOfWeek == DayOfWeek.Saturday)
                    {
                        due_date = ((DateTime)due_date).AddDays(2);
                    }
                    else if (((DateTime)due_date).DayOfWeek == DayOfWeek.Sunday)
                    {
                        due_date = ((DateTime)due_date).AddDays(1);
                    }
                    SubOrder["bsd_duedate"] = due_date;
                    #endregion

                    #region Chuyển shiping porter tax
                    if (order.HasValue("bsd_saletaxgroup")) SubOrder["bsd_saletaxgroup"] = order["bsd_saletaxgroup"];
                    if (order.HasValue("bsd_requestporter")) SubOrder["bsd_requestporter"] = order["bsd_requestporter"];
                    if (order.HasValue("bsd_transportation")) SubOrder["bsd_transportation"] = order["bsd_transportation"];
                    if (order.HasValue("bsd_shippingdeliverymethod")) SubOrder["bsd_shippingdeliverymethod"] = order["bsd_shippingdeliverymethod"];
                    if (order.HasValue("bsd_truckload")) SubOrder["bsd_truckload"] = order["bsd_truckload"];
                    if (order.HasValue("bsd_unitshipping")) SubOrder["bsd_unitshipping"] = order["bsd_unitshipping"];
                    if (order.HasValue("bsd_shippingpricelistname")) SubOrder["bsd_shippingpricelistname"] = order["bsd_shippingpricelistname"];
                    if (order.HasValue("bsd_priceoftransportationn")) SubOrder["bsd_priceoftransportationn"] = order["bsd_priceoftransportationn"];
                    if (order.HasValue("bsd_shippingporter")) SubOrder["bsd_shippingporter"] = order["bsd_shippingporter"];
                    if (order.HasValue("bsd_porteroption")) SubOrder["bsd_porteroption"] = order["bsd_porteroption"];
                    if (order.HasValue("bsd_priceofporter")) SubOrder["bsd_priceofporter"] = order["bsd_priceofporter"];
                    if (order.HasValue("bsd_pricepotter")) SubOrder["bsd_pricepotter"] = order["bsd_pricepotter"];
                    if (order.HasValue("bsd_porter")) SubOrder["bsd_porter"] = order["bsd_porter"];
                    //bsd_porter
                    #endregion

                    Guid suborder_id = myService.service.Create(SubOrder);

                    #endregion

                    #region Tạo Suborder Product và update lại giá trên suborder

                    decimal grandtotal = 0m;

                    foreach (var orderproduct in list_orderproduct.Entities)
                    {
                        bool condition =
                            orderproduct_cus.UsingTax == (bool)orderproduct["bsd_usingtax"]
                            && orderproduct_cus.ShippingYesNo == (bool)orderproduct["bsd_usingshipping"]
                            && orderproduct_cus.Item_Sales_Tax == (orderproduct.HasValue("bsd_itemsalestax") ? (decimal)orderproduct["bsd_itemsalestax"] : 0);
                        if (multiple_address)
                        {
                            condition = condition
                            && (orderproduct_cus.DeliveryFrom == ((OptionSetValue)orderproduct["bsd_deliveryfrom"]).Value
                            && orderproduct_cus.ShippingFromAddress == ((EntityReference)orderproduct["bsd_shippingfromaddress"]).Id
                            && orderproduct_cus.ShippingAddress == ((EntityReference)orderproduct["bsd_shippingaddress"]).Id
                            && orderproduct_cus.ReceiptCustomer == ((EntityReference)orderproduct["bsd_partnerscompany"]).Id
                            && orderproduct_cus.CustomerAddress == ((EntityReference)orderproduct["bsd_shiptoaddress"]).Id
                            );
                        }

                        if (condition)
                        {
                            bool con = true;
                            decimal remaining_quantity = (decimal)orderproduct["bsd_remainingquantity"];
                            while (con == true)
                            {
                                con = false;
                                Entity product = myService.service.Retrieve("product", ((EntityReference)orderproduct["productid"]).Id, new ColumnSet(true));
                                decimal order_quantity = (decimal)orderproduct["quantity"];
                                decimal suborder_quantity = (decimal)orderproduct["bsd_suborderquantity"];
                                decimal price_per_unit = ((Money)orderproduct["priceperunit"]).Value;
                                decimal shippingprice = ((decimal)orderproduct["bsd_giashipsauthue_full"]);
                                decimal porter_price = ((decimal)orderproduct["bsd_porterprice_full"]);
                                decimal vat = orderproduct.HasValue("bsd_vatprice_full") ? ((decimal)orderproduct["bsd_vatprice_full"]) : 0m;
                                decimal tax = vat * remaining_quantity;
                                decimal giatruocthue = price_per_unit + shippingprice + porter_price;
                                decimal giasauthue = giatruocthue + vat;
                                decimal amount = giatruocthue * remaining_quantity;
                                decimal extendedamount = giasauthue * remaining_quantity;
                                decimal currency_exchange = bsd_exchangeratevalue * extendedamount;
                                grandtotal += currency_exchange;

                                if (currency_exchange > 100000000000)
                                {
                                    con = true;
                                    remaining_quantity = 0m;
                                }
                                else
                                {
                                    #region Tạo sub product
                                    Entity sub_product = new Entity("bsd_suborderproduct");
                                    sub_product["bsd_name"] = product["name"];
                                    sub_product["bsd_quote"] = target;
                                    sub_product["bsd_type"] = new OptionSetValue(861450001);
                                    sub_product["bsd_suborder"] = new EntityReference("bsd_suborder", suborder_id);
                                    sub_product["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                                    if (orderproduct.HasValue("bsd_productid")) sub_product["bsd_productid"] = orderproduct["bsd_productid"];
                                    if (orderproduct.HasValue("bsd_descriptionproduct")) sub_product["bsd_descriptionproduct"] = orderproduct["bsd_descriptionproduct"];
                                    sub_product["bsd_priceperunit"] = new Money(price_per_unit);
                                    sub_product["bsd_orderquantity"] = orderproduct["quantity"];
                                    sub_product["bsd_standardquantity"] = orderproduct["bsd_standardquantity"];
                                    sub_product["bsd_totalquantity"] = orderproduct["bsd_totalquantity"];
                                    sub_product["bsd_shippedquantity"] = 0m;
                                    sub_product["bsd_residualquantity"] = remaining_quantity;
                                    sub_product["bsd_giatruocthue"] = new Money(giatruocthue);
                                    sub_product["bsd_giasauthue"] = new Money(giasauthue);
                                    sub_product["bsd_amount"] = new Money(amount);
                                    sub_product["bsd_unit"] = orderproduct["uomid"];
                                    sub_product["bsd_usingtax"] = orderproduct["bsd_usingtax"];
                                    sub_product["bsd_currencyexchangecurrency"] = new Money(currency_exchange);
                                    sub_product["bsd_currencyexchangetext"] = currency_exchange.DecimalToStringHideSymbol();
                                    sub_product["transactioncurrencyid"] = order["transactioncurrencyid"];
                                    sub_product["bsd_shippingprice"] = new Money(shippingprice);
                                    sub_product["bsd_porterprice"] = new Money(porter_price);
                                    sub_product["bsd_shipquantity"] = remaining_quantity;
                                    sub_product["bsd_newquantity"] = remaining_quantity;
                                    sub_product["bsd_extendedamount"] = new Money(extendedamount);
                                    if (orderproduct.HasValue("bsd_itemsalestaxgroup"))
                                    {
                                        sub_product["bsd_itemsalestaxgroup"] = orderproduct["bsd_itemsalestaxgroup"];
                                    }
                                    if (orderproduct.HasValue("bsd_itemsalestax"))
                                    {
                                        sub_product["bsd_itemsalestax"] = orderproduct["bsd_itemsalestax"];
                                    }

                                    if (orderproduct_cus.UsingTax)
                                    {
                                        sub_product["bsd_tax"] = new Money(tax);
                                        sub_product["bsd_vatprice"] = new Money(vat);
                                    }
                                    else
                                    {
                                        sub_product["bsd_tax"] = null;
                                        sub_product["bsd_vatprice"] = null;
                                    }
                                    myService.service.Create(sub_product);
                                    #endregion

                                    #region cập nhật quote product
                                    Entity new_orderproduct = new Entity(orderproduct.LogicalName, orderproduct.Id);
                                    decimal new_suborderquantity = suborder_quantity + remaining_quantity;
                                    new_orderproduct["bsd_suborderquantity"] = suborder_quantity + remaining_quantity;
                                    new_orderproduct["bsd_remainingquantity"] = order_quantity - new_suborderquantity;
                                    myService.service.Update(new_orderproduct);
                                    #endregion
                                }
                            }
                        }
                    }

                    bool won_quote = false;
                    // won_quote laf d
                    #region cập nhật số lượng = 0 cho tất cả suborder nếu grand total vượt quá
                    if (grandtotal > 100000000000)
                    {
                        SuborderService subService = new SuborderService(myService);
                        EntityCollection list_product = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder_id);
                        foreach (var suborder_product in list_product.Entities)
                        {
                            // pre quantity để trừ đi số lượng ở trên kia đã gán mà chưa gán lại = 0 khi grand total quá trăm tỷ.
                            decimal current_shipquantity = (decimal)suborder_product["bsd_shipquantity"];
                            suborder_product["bsd_shipquantity"] = 0m;
                            subService.Create_Update_Suborder_Product(suborder_product, 2, false, current_shipquantity);
                            won_quote = true;
                        }
                    }
                    #endregion

                    #region Update suborder
                    Entity suborder_update = myService.service.Retrieve(SubOrder.LogicalName, suborder_id, new ColumnSet(true));
                    new SuborderService(myService).UpdateSubOrder(suborder_update);
                    #endregion

                    #endregion

                    if (won_quote == false)
                    {
                        // nếu trong cái if grand total ko chạy vào, thì có nghĩa là chưa chạy vào hàm update suborder, là quote chưa won.
                        #region won quote
                        myService.SetState(order.Id, order.LogicalName, 1, 2);
                        WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
                        Entity quoteClose = new Entity("quoteclose");
                        quoteClose["quoteid"] = new EntityReference("quote", order.Id);
                        quoteClose["subject"] = "Quote Close" + DateTime.Now.ToString();
                        winQuoteRequest.QuoteClose = quoteClose;
                        winQuoteRequest.Status = new OptionSetValue(-1);
                        myService.service.Execute(winQuoteRequest);
                        #endregion
                    }
                }
            }
            #endregion

            #region bsd_Action_CreateSuborderMultipleAddress_Order
            if (myService.context.MessageName == "bsd_Action_CreateSuborderMultipleAddress_Order")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
                Entity order = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                Entity totalline = null;
                int order_type = ((OptionSetValue)order["bsd_type"]).Value;

                #region Chặn xuất khẩu
                //if (order_type == 100000000 || order_type == 100000001)
                //{
                //    throw new Exception("Do not create Sub Order from Contract!");
                //}
                #endregion

                SuborderService suborderService = new SuborderService(myService);
                if (order_type == 861450003 || order_type == 861450002) return;

                Guid salesorderdetail_id = Guid.Parse(myService.context.InputParameters["SalesorderDetailId"].ToString());

                Entity orderproduct = myService.service.Retrieve("salesorderdetail", salesorderdetail_id, new ColumnSet(true));
                bool have_quantity = (bool)order["bsd_havequantity"];
                decimal totalline_remaining_quantity = 0m;

                #region kiểm tra có line tổng không và nếu có thì kiểm tra số lượng.
                if (have_quantity == false)
                {
                    totalline = suborderService.Get_LineTotal_OrderProduct_Quantity(((EntityReference)orderproduct["productid"]).Id, order.Id);
                    if (totalline == null)
                    {
                        throw new Exception("Product quantities are no longer enough to create SubOrder.. !");
                    }
                    else
                    {
                        totalline_remaining_quantity = (decimal)totalline["bsd_remainingquantity"];
                    }
                }
                else
                {
                    totalline_remaining_quantity = (decimal)orderproduct["bsd_remainingquantity"];
                }
                if (totalline_remaining_quantity == 0)
                {
                    throw new Exception("Product quantities are no longer enough to create SubOrder. !");
                }
                #endregion

                OrderProduct_Custom orderproduct_cus = new OrderProduct_Custom();
                orderproduct_cus.Item_Sales_Tax = (orderproduct.HasValue("bsd_itemsalestax")) ? (decimal)orderproduct["bsd_itemsalestax"] : 0;
                orderproduct_cus.UsingTax = (bool)orderproduct["bsd_usingtax"];
                orderproduct_cus.ShippingYesNo = (bool)orderproduct["bsd_usingshipping"];
                orderproduct_cus.DeliveryFrom = ((OptionSetValue)orderproduct["bsd_deliveryfrom"]).Value;
                orderproduct_cus.Site = ((EntityReference)orderproduct["bsd_site"]).Id;
                orderproduct_cus.SiteAddress = ((EntityReference)orderproduct["bsd_siteaddress"]).Id;
                orderproduct_cus.ShippingFromAddress = ((EntityReference)orderproduct["bsd_shippingfromaddress"]).Id;
                orderproduct_cus.ShippingAddress = ((EntityReference)orderproduct["bsd_shippingaddress"]).Id;
                orderproduct_cus.ReceiptCustomer = ((EntityReference)orderproduct["bsd_shiptoaccount"]).Id;
                orderproduct_cus.CustomerAddress = ((EntityReference)orderproduct["bsd_shiptoaddress"]).Id;

                if (orderproduct.HasValue("bsd_port")) orderproduct_cus.Port = ((EntityReference)orderproduct["bsd_port"]).Id;
                if (orderproduct.HasValue("bsd_addressport")) orderproduct_cus.AddressPort = ((EntityReference)orderproduct["bsd_addressport"]).Id;

                #region Tạo Sub Order
                Entity account = myService.service.Retrieve("account", ((EntityReference)order["customerid"]).Id, new ColumnSet(true));
                Entity SubOrder = new Entity("bsd_suborder");
                SubOrder["bsd_type"] = new OptionSetValue(861450002);
                SubOrder["bsd_date"] = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                SubOrder["bsd_order"] = new EntityReference(order.LogicalName, order.Id);
                SubOrder["bsd_potentialcustomer"] = order["customerid"];
                SubOrder["bsd_multipleaddress"] = true;
                if (order.HasValue("bsd_economiccontract")) SubOrder["bsd_economiccontract"] = order["bsd_economiccontract"];
                if (order.HasValue("transactioncurrencyid")) SubOrder["transactioncurrencyid"] = order["transactioncurrencyid"];
                if (order.HasValue("bsd_address")) SubOrder["bsd_addresscontractoffer"] = order["bsd_address"];
                if (order.HasValue("bsd_telephone")) SubOrder["bsd_telephone"] = order["bsd_telephone"];
                if (order.HasValue("bsd_taxregistration")) SubOrder["bsd_taxregistration"] = order["bsd_taxregistration"];
                if (order.HasValue("bsd_contact")) SubOrder["bsd_contact"] = order["bsd_contact"];

                if (order.HasValue("bsd_customercode")) SubOrder["bsd_customercode"] = order["bsd_customercode"];
                if (order.HasValue("pricelevelid")) SubOrder["bsd_pricelist"] = order["pricelevelid"];
                if (order.HasValue("bsd_billtoaddress")) SubOrder["bsd_billtoaddress"] = order["bsd_billtoaddress"];
                if (order.HasValue("bsd_deliverymethod")) SubOrder["bsd_deliverymethod"] = order["bsd_deliverymethod"];
                if (order.HasValue("bsd_accompanyingdocuments")) SubOrder["bsd_requireddocuments"] = order["bsd_accompanyingdocuments"];
                if (order.HasValue("shipto_freighttermscode")) SubOrder["bsd_shiptofreightterms"] = order["shipto_freighttermscode"];

                SubOrder["bsd_deliveryfrom"] = new OptionSetValue(orderproduct_cus.DeliveryFrom);
                SubOrder["bsd_site"] = new EntityReference("bsd_site", orderproduct_cus.Site);
                SubOrder["bsd_siteaddress"] = new EntityReference("bsd_address", orderproduct_cus.SiteAddress);
                SubOrder["bsd_shiptoaccount"] = new EntityReference("account", orderproduct_cus.ReceiptCustomer);
                SubOrder["bsd_shiptoaddress"] = new EntityReference("bsd_address", orderproduct_cus.CustomerAddress);
                SubOrder["bsd_shippingfromaddress"] = new EntityReference("bsd_address", orderproduct_cus.ShippingFromAddress);
                SubOrder["bsd_shippingaddress"] = new EntityReference("bsd_address", orderproduct_cus.ShippingAddress);
                if (orderproduct_cus.DeliveryFrom == 861450001 || orderproduct_cus.DeliveryFrom == 861450002) //bhs - port || port -customer address
                {
                    SubOrder["bsd_port"] = new EntityReference("account", orderproduct_cus.Port);
                    SubOrder["bsd_addressport"] = new EntityReference("bsd_address", orderproduct_cus.AddressPort);
                }



                if (order.HasValue("bsd_fromdate")) SubOrder["bsd_fromdate"] = order["bsd_fromdate"];
                if (order.HasValue("bsd_factory")) SubOrder["bsd_factory"] = order["bsd_factory"];
                if (order.HasValue("bsd_todate")) SubOrder["bsd_todate"] = order["bsd_todate"];

                if (order.HasValue("bsd_paymentterm"))
                {
                    SubOrder["bsd_paymentterm"] = order["bsd_paymentterm"];
                    Entity bsd_paymentterm = myService.service.Retrieve(((EntityReference)order["bsd_paymentterm"]).LogicalName, ((EntityReference)order["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                    SubOrder["bsd_datept"] = new decimal((int)bsd_paymentterm["bsd_date"]);
                }
                if (order.HasValue("bsd_paymentmethod")) SubOrder["bsd_paymentmethod"] = order["bsd_paymentmethod"];

                if (order.HasValue("bsd_invoiceaccount")) SubOrder["bsd_invoiceaccount"] = order["bsd_invoiceaccount"];
                if (order.HasValue("bsd_invoicenameaccount")) SubOrder["bsd_invoicenameaccount"] = order["bsd_invoicenameaccount"];
                if (order.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addressinvoiceaccount"] = order["bsd_addressinvoiceaccount"];
                if (order.HasValue("bsd_contactinvoiceaccount")) SubOrder["bsd_contactinvoiceaccount"] = order["bsd_contactinvoiceaccount"];

                if (order.HasValue("bsd_address")) SubOrder["bsd_addresscustomeraccount"] = order["bsd_address"];
                SubOrder["bsd_unitdefault"] = order["bsd_unitdefault"];
                SubOrder["bsd_priceincludeshippingporter"] = order["bsd_priceincludeshippingporter"];
                if (order.HasValue("bsd_historycustomername")) SubOrder["bsd_historycustomeraccount"] = order["bsd_historycustomername"];
                if (order.HasValue("bsd_historyinvoicename")) SubOrder["bsd_historyinvoicename"] = order["bsd_historyinvoicename"];
                if (orderproduct.HasValue("bsd_historyreceiptcustomer")) SubOrder["bsd_historyreceiptcustomer"] = orderproduct["bsd_historyreceiptcustomer"];
                #region Lấy Exchange Rate 

                decimal bsd_exchangeratevalue = 1m;

                Guid account_currency = ((EntityReference)order["transactioncurrencyid"]).Id;

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
                if (!account_currency.Equals(bsd_currencydefault.Id)) // nếu không bằng với unit default.
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
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>", account_currency
                        , bsd_currencydefault.Id
                        , bsd_bankdefault.Id,
                    myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service).ToString("yyyy-MM-dd"))));

                    if (list_exchangerate.Entities.Any())
                    {
                        Entity ex_changerate = list_exchangerate.Entities.First();
                        SubOrder["bsd_exchangerate"] = new EntityReference(ex_changerate.LogicalName, ex_changerate.Id);
                        SubOrder["bsd_bank"] = new EntityReference(bsd_bankdefault.LogicalName, bsd_bankdefault.Id);
                        bsd_exchangeratevalue = (decimal)ex_changerate["bsd_exchangerate"];
                    }
                    else
                    {
                        //throw new Exception(account_currency + "----" + bsd_currencydefault.Id);
                        throw new Exception("Please create a new Exchage rate");
                    }
                }
                else
                {
                    bsd_exchangeratevalue = 1m;
                }
                SubOrder["bsd_exchangeratevalue"] = bsd_exchangeratevalue;
                #endregion

                #region Due date
                DateTime suborder_date = DateTime.Now;
                DateTime? due_date = null;
                int date_paymentterm = 0;
                if (account.HasValue("bsd_paymentterm")) // 15Days
                {
                    Entity bsd_paymentterm = myService.service.Retrieve("bsd_paymentterm", ((EntityReference)account["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                    date_paymentterm = (int)bsd_paymentterm["bsd_date"];
                }
                if (account.HasValue("bsd_paymentday"))
                {
                    int payment_day_ofmonth = (int)account["bsd_paymentday"]; // ngày trả nợ trong tháng
                    DateTime tong = suborder_date.AddDays(date_paymentterm); // ngày đơn hàng + ngày trả sau.
                    if (tong.Day > payment_day_ofmonth) // tổng lớn hơn ngày phải trả. -> dời qua tháng sau, lấy ngày phải trả.
                    {
                        due_date = new DateTime(tong.Year, tong.AddMonths(1).Month, 1);
                        due_date = ((DateTime)due_date).AddDays(payment_day_ofmonth - 1);
                    }
                    else
                    {
                        due_date = new DateTime(tong.Year, tong.Month, 1);
                        due_date = ((DateTime)due_date).AddDays(payment_day_ofmonth - 1);
                    }
                }
                else
                {
                    due_date = suborder_date.AddDays(date_paymentterm);
                }
                due_date = myService.RetrieveLocalTimeFromUTCTime((DateTime)due_date, myService.service);
                if (((DateTime)due_date).DayOfWeek == DayOfWeek.Saturday)
                {
                    due_date = ((DateTime)due_date).AddDays(2);
                }
                else if (((DateTime)due_date).DayOfWeek == DayOfWeek.Sunday)
                {
                    due_date = ((DateTime)due_date).AddDays(1);
                }
                SubOrder["bsd_duedate"] = due_date;
                #endregion

                #region Chuyển shiping porter tax
                if (order.HasValue("bsd_saletaxgroup")) SubOrder["bsd_saletaxgroup"] = order["bsd_saletaxgroup"];
                if (order.HasValue("bsd_requestporter")) SubOrder["bsd_requestporter"] = order["bsd_requestporter"];
                if (order.HasValue("bsd_transportation")) SubOrder["bsd_transportation"] = order["bsd_transportation"];
                if (order.HasValue("bsd_shippingdeliverymethod")) SubOrder["bsd_shippingdeliverymethod"] = order["bsd_shippingdeliverymethod"];
                if (order.HasValue("bsd_truckload")) SubOrder["bsd_truckload"] = order["bsd_truckload"];
                if (order.HasValue("bsd_unitshipping")) SubOrder["bsd_unitshipping"] = order["bsd_unitshipping"];
                if (order.HasValue("bsd_shippingpricelistname")) SubOrder["bsd_shippingpricelistname"] = order["bsd_shippingpricelistname"];
                if (order.HasValue("bsd_priceoftransportationn")) SubOrder["bsd_priceoftransportationn"] = order["bsd_priceoftransportationn"];
                if (order.HasValue("bsd_shippingporter")) SubOrder["bsd_shippingporter"] = order["bsd_shippingporter"];
                if (order.HasValue("bsd_porteroption")) SubOrder["bsd_porteroption"] = order["bsd_porteroption"];
                if (order.HasValue("bsd_priceofporter")) SubOrder["bsd_priceofporter"] = order["bsd_priceofporter"];
                if (order.HasValue("bsd_pricepotter")) SubOrder["bsd_pricepotter"] = order["bsd_pricepotter"];
                if (order.HasValue("bsd_porter")) SubOrder["bsd_porter"] = order["bsd_porter"];
                #endregion

                #region Shipping + Porter
                var shipping = (bool)orderproduct["bsd_shippingoption"];
                if (shipping)
                {
                    SubOrder["bsd_transportation"] = true;
                    SubOrder["bsd_requestporter"] = orderproduct["bsd_requestporter"];
                    SubOrder["bsd_shippingdeliverymethod"] = orderproduct["bsd_shippingdeliverymethod"];

                    if (orderproduct.HasValue("bsd_truckload"))
                    {
                        SubOrder["bsd_truckload"] = orderproduct["bsd_truckload"];
                    }

                    if (orderproduct.HasValue("bsd_unitshipping"))
                    {
                        SubOrder["bsd_unitshipping"] = orderproduct["bsd_unitshipping"];
                    }

                    SubOrder["bsd_shippingpricelistname"] = orderproduct["bsd_shippingpricelist"];

                    SubOrder["bsd_priceoftransportationn"] = orderproduct["bsd_shippingpricelistvalue"];
                    SubOrder["bsd_shippingporter"] = orderproduct["bsd_shippingporter"];

                }

                SubOrder["bsd_porteroption"] = orderproduct["bsd_porteroption"];
                if (orderproduct.HasValue("bsd_portermethod"))
                {
                    SubOrder["bsd_portermethod"] = orderproduct["bsd_portermethod"];
                }
                if (orderproduct.HasValue("bsd_porterpricelist"))
                {
                    SubOrder["bsd_priceofporter"] = orderproduct["bsd_porterpricelist"];
                }
                if (orderproduct.HasValue("bsd_porterpricelistvalue"))
                {
                    SubOrder["bsd_pricepotter"] = orderproduct["bsd_porterpricelistvalue"];
                }

                if (orderproduct.HasValue("bsd_portertype"))
                {
                    SubOrder["bsd_porter"] = orderproduct["bsd_portertype"];
                }
                #endregion

                Guid suborder_id = myService.service.Create(SubOrder);

                #endregion

                #region Tạo Suborder Product và update lại giá trên suborder
                bool condition =
                orderproduct_cus.UsingTax == (bool)orderproduct["bsd_usingtax"]
                && orderproduct_cus.ShippingYesNo == (bool)orderproduct["bsd_usingshipping"]
                && orderproduct_cus.Item_Sales_Tax == (orderproduct.HasValue("bsd_itemsalestax") ? (decimal)orderproduct["bsd_itemsalestax"] : 0);

                condition = condition
                && (orderproduct_cus.DeliveryFrom == ((OptionSetValue)orderproduct["bsd_deliveryfrom"]).Value
                && orderproduct_cus.ShippingFromAddress == ((EntityReference)orderproduct["bsd_shippingfromaddress"]).Id
                && orderproduct_cus.ShippingAddress == ((EntityReference)orderproduct["bsd_shippingaddress"]).Id
                && orderproduct_cus.ReceiptCustomer == ((EntityReference)orderproduct["bsd_shiptoaccount"]).Id
                && orderproduct_cus.CustomerAddress == ((EntityReference)orderproduct["bsd_shiptoaddress"]).Id
               );

                if (condition)
                {
                    bool con = true;
                    while (con == true)
                    {
                        con = false;
                        Entity product = myService.service.Retrieve("product", ((EntityReference)orderproduct["productid"]).Id, new ColumnSet(true));
                        decimal order_quantity = (decimal)orderproduct["quantity"];
                        decimal standard_quantity = (decimal)orderproduct["bsd_standardquantity"];
                        decimal price_per_unit = ((Money)orderproduct["priceperunit"]).Value;
                        decimal shippingprice = ((decimal)orderproduct["bsd_giashipsauthue_full"]);
                        decimal porter_price = ((decimal)orderproduct["bsd_porterprice_full"]);
                        decimal vat = orderproduct.HasValue("bsd_vatprice_full") ? ((decimal)orderproduct["bsd_vatprice_full"]) : 0m;
                        decimal tax = vat * totalline_remaining_quantity;
                        decimal giatruocthue = price_per_unit + shippingprice + porter_price;
                        decimal giasauthue = giatruocthue + vat;
                        decimal amount = giatruocthue * totalline_remaining_quantity;
                        decimal extendedamount = giasauthue * totalline_remaining_quantity;
                        decimal currency_exchange = bsd_exchangeratevalue * extendedamount;

                        if (currency_exchange > 100000000000)
                        {
                            con = true;
                            totalline_remaining_quantity = 0m;
                        }
                        else
                        {
                            #region Tạo sub product
                            Entity sub_product = new Entity("bsd_suborderproduct");
                            sub_product["bsd_name"] = product["name"];
                            sub_product["bsd_type"] = new OptionSetValue(861450002);
                            sub_product["bsd_order"] = target;
                            sub_product["bsd_suborder"] = new EntityReference("bsd_suborder", suborder_id);
                            sub_product["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                            if (orderproduct.HasValue("bsd_productid")) sub_product["bsd_productid"] = orderproduct["bsd_productid"];
                            if (orderproduct.HasValue("bsd_descriptionproduct")) sub_product["bsd_descriptionproduct"] = orderproduct["bsd_descriptionproduct"];
                            sub_product["bsd_priceperunit"] = new Money(price_per_unit);
                            sub_product["bsd_orderquantity"] = order_quantity;
                            sub_product["bsd_standardquantity"] = standard_quantity;
                            sub_product["bsd_totalquantity"] = standard_quantity * totalline_remaining_quantity;
                            sub_product["bsd_shippedquantity"] = 0m;
                            sub_product["bsd_residualquantity"] = totalline_remaining_quantity;
                            sub_product["bsd_giatruocthue"] = new Money(giatruocthue);
                            sub_product["bsd_giasauthue"] = new Money(giasauthue);
                            sub_product["bsd_amount"] = new Money(amount);
                            sub_product["bsd_unit"] = orderproduct["uomid"];
                            sub_product["bsd_usingtax"] = orderproduct["bsd_usingtax"];
                            sub_product["bsd_currencyexchangecurrency"] = new Money(currency_exchange);
                            sub_product["bsd_currencyexchangetext"] = currency_exchange.DecimalToStringHideSymbol();
                            sub_product["transactioncurrencyid"] = order["transactioncurrencyid"];
                            sub_product["bsd_shippingprice"] = new Money(shippingprice);
                            sub_product["bsd_porterprice"] = new Money(porter_price);
                            sub_product["bsd_shipquantity"] = totalline_remaining_quantity;
                            sub_product["bsd_newquantity"] = totalline_remaining_quantity;
                            sub_product["bsd_extendedamount"] = new Money(extendedamount);
                            if (orderproduct.HasValue("bsd_itemsalestaxgroup"))
                            {
                                sub_product["bsd_itemsalestaxgroup"] = orderproduct["bsd_itemsalestaxgroup"];
                            }
                            if (orderproduct.HasValue("bsd_itemsalestax"))
                            {
                                sub_product["bsd_itemsalestax"] = orderproduct["bsd_itemsalestax"];
                            }

                            if (orderproduct_cus.UsingTax)
                            {
                                sub_product["bsd_tax"] = new Money(tax);
                                sub_product["bsd_vatprice"] = new Money(vat);
                            }
                            else
                            {
                                sub_product["bsd_tax"] = null;
                                sub_product["bsd_vatprice"] = null;
                            }

                            myService.service.Create(sub_product);
                            #endregion

                            if (have_quantity == false)
                            {
                                #region cập nhật total line
                                Entity new_totalline = new Entity(totalline.LogicalName, totalline.Id);
                                decimal totalline_quantinty = (decimal)totalline["bsd_quantity"];
                                decimal totalline_suborderquantinty = (decimal)totalline["bsd_suborderquantity"];
                                decimal totalline_newsuborderquantity = totalline_suborderquantinty + totalline_remaining_quantity;

                                decimal totalline_shippedquantity = (decimal)totalline["bsd_shippedquantity"];

                                new_totalline["bsd_remainingquantity"] = totalline_quantinty - totalline_newsuborderquantity;
                                new_totalline["bsd_suborderquantity"] = totalline_newsuborderquantity;
                                new_totalline["bsd_residualquantity"] = totalline_quantinty - totalline_shippedquantity;
                                myService.service.Update(new_totalline);
                                #endregion

                                #region update line soluong 0
                                Entity new_orderproduct = new Entity(orderproduct.LogicalName, orderproduct.Id);

                                decimal orderproduct_standardquantity = (decimal)orderproduct["bsd_standardquantity"];
                                decimal old_sunborderquantity = (decimal)orderproduct["bsd_suborderquantity"];
                                decimal new_suborderquantity = old_sunborderquantity + totalline_remaining_quantity;
                                new_orderproduct["bsd_totalquantity"] = orderproduct_standardquantity * new_suborderquantity;
                                new_orderproduct["bsd_suborderquantity"] = new_suborderquantity;
                                new_orderproduct["bsd_residualquantity"] = new_suborderquantity;
                                new_orderproduct["bsd_shippedquantity"] = 0m;
                                myService.Update(new_orderproduct);
                                #endregion
                            }
                            else
                            {
                                #region cập nhật orderproduct
                                decimal suborder_quantity = (decimal)orderproduct["bsd_suborderquantity"];
                                Entity new_orderproduct = new Entity(orderproduct.LogicalName, orderproduct.Id);
                                decimal new_suborderquantity = suborder_quantity + totalline_remaining_quantity;
                                new_orderproduct["bsd_suborderquantity"] = new_suborderquantity;
                                new_orderproduct["bsd_remainingquantity"] = order_quantity - new_suborderquantity;
                                myService.service.Update(new_orderproduct);
                                #endregion
                            }
                        }
                    }
                }

                #region Update suborder
                Entity suborder_update = myService.service.Retrieve(SubOrder.LogicalName, suborder_id, new ColumnSet(true));
                new SuborderService(myService).UpdateSubOrder(suborder_update);
                #endregion

                #endregion

                myService.context.OutputParameters["SuborderId"] = suborder_id.ToString();
            }
            #endregion

            #region bsd_Action_CreateSuborderMultipleAddress_Quote
            else if (myService.context.MessageName == "bsd_Action_CreateSuborderMultipleAddress_Quote")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
                // quote
                Entity order = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                Entity totalline = null;
                int quote_type = ((OptionSetValue)order["bsd_quotationtype"]).Value;

                #region Kiểm tra tạo hợp đồng chưa !
                EntityCollection list_order = myService.RetrieveOneCondition("salesorder", "quoteid", order.Id);
                if (list_order.Entities.Any()) throw new Exception("Economic Contract or Other Contract has been created for this Quote !");
                #endregion

                #region Chặn xuất khẩu
                if (quote_type == 861450001 || quote_type == 861450004)
                {
                    throw new Exception("Do not create Sub Order from Quote!");
                }
                #endregion

                SuborderService suborderService = new SuborderService(myService);

                if (quote_type != 861450000 && quote_type != 861450001 && quote_type != 861450004) return;

                Guid salesorderdetail_id = Guid.Parse(myService.context.InputParameters["QuoteDetailId"].ToString());
                Entity orderproduct = myService.service.Retrieve("quotedetail", salesorderdetail_id, new ColumnSet(true));
                bool have_quantity = (bool)order["bsd_havequantity"];
                decimal totalline_remaining_quantity = 0m;

                #region kiểm tra có line tổng không và nếu có thì kiểm tra số lượng.
                if (have_quantity == false)
                {
                    totalline = suborderService.Get_LineTotal_QuoteProduct_Quantity(((EntityReference)orderproduct["productid"]).Id, order.Id);
                    if (totalline == null)
                    {
                        throw new Exception("Product quantities are no longer enough to create SubOrder.. !");
                    }
                    else
                    {
                        totalline_remaining_quantity = (decimal)totalline["bsd_remainingquantity"];
                    }
                }
                else
                {
                    totalline_remaining_quantity = (decimal)orderproduct["bsd_remainingquantity"];
                }
                if (totalline_remaining_quantity == 0)
                {
                    throw new Exception("Product quantities are no longer enough to create SubOrder. !");
                }
                #endregion

                OrderProduct_Custom orderproduct_cus = new OrderProduct_Custom();
                orderproduct_cus.Item_Sales_Tax = (orderproduct.HasValue("bsd_itemsalestax")) ? (decimal)orderproduct["bsd_itemsalestax"] : 0;
                orderproduct_cus.UsingTax = (bool)orderproduct["bsd_usingtax"];
                orderproduct_cus.ShippingYesNo = (bool)orderproduct["bsd_usingshipping"];
                orderproduct_cus.DeliveryFrom = ((OptionSetValue)orderproduct["bsd_deliveryfrom"]).Value;
                orderproduct_cus.Site = ((EntityReference)orderproduct["bsd_site"]).Id;
                orderproduct_cus.SiteAddress = ((EntityReference)orderproduct["bsd_siteaddress"]).Id;
                orderproduct_cus.ShippingFromAddress = ((EntityReference)orderproduct["bsd_shippingfromaddress"]).Id;
                orderproduct_cus.ShippingAddress = ((EntityReference)orderproduct["bsd_shippingaddress"]).Id;
                orderproduct_cus.ReceiptCustomer = ((EntityReference)orderproduct["bsd_partnerscompany"]).Id;
                orderproduct_cus.CustomerAddress = ((EntityReference)orderproduct["bsd_shiptoaddress"]).Id;

                if (orderproduct.HasValue("bsd_port")) orderproduct_cus.Port = ((EntityReference)orderproduct["bsd_port"]).Id;
                if (orderproduct.HasValue("bsd_addressport")) orderproduct_cus.AddressPort = ((EntityReference)orderproduct["bsd_addressport"]).Id;

                #region Tạo Sub Order
                Entity account = myService.service.Retrieve("account", ((EntityReference)order["customerid"]).Id, new ColumnSet(true));

                myService.SetState(target.Id, target.LogicalName, 0, 1); // Quote draff
                Entity SubOrder = new Entity("bsd_suborder");
                SubOrder["bsd_type"] = new OptionSetValue(861450001);
                SubOrder["bsd_date"] = myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service);
                SubOrder["bsd_quote"] = new EntityReference(order.LogicalName, order.Id);
                SubOrder["bsd_potentialcustomer"] = order["customerid"];
                SubOrder["bsd_multipleaddress"] = true;
                if (order.HasValue("bsd_economiccontract")) SubOrder["bsd_economiccontract"] = order["bsd_economiccontract"];
                if (order.HasValue("transactioncurrencyid")) SubOrder["transactioncurrencyid"] = order["transactioncurrencyid"];
                if (order.HasValue("bsd_address")) SubOrder["bsd_addresscontractoffer"] = order["bsd_address"];
                if (order.HasValue("bsd_telephone")) SubOrder["bsd_telephone"] = order["bsd_telephone"];
                if (order.HasValue("bsd_taxregistration")) SubOrder["bsd_taxregistration"] = order["bsd_taxregistration"];
                if (order.HasValue("bsd_contact")) SubOrder["bsd_contact"] = order["bsd_contact"];

                if (order.HasValue("bsd_customercode")) SubOrder["bsd_customercode"] = order["bsd_customercode"];
                if (order.HasValue("pricelevelid")) SubOrder["bsd_pricelist"] = order["pricelevelid"];
                if (order.HasValue("bsd_billtoaddress")) SubOrder["bsd_billtoaddress"] = order["bsd_billtoaddress"];
                if (order.HasValue("bsd_deliverymethod")) SubOrder["bsd_deliverymethod"] = order["bsd_deliverymethod"];
                if (order.HasValue("bsd_accompanyingdocument")) SubOrder["bsd_requireddocuments"] = order["bsd_accompanyingdocument"];
                if (order.HasValue("shipto_freighttermscode")) SubOrder["bsd_shiptofreightterms"] = order["shipto_freighttermscode"];
                // nhiều địa chỉ thì gom thành 1 địa chỉ trên line
                SubOrder["bsd_deliveryfrom"] = new OptionSetValue(orderproduct_cus.DeliveryFrom);
                SubOrder["bsd_site"] = new EntityReference("bsd_site", orderproduct_cus.Site);
                SubOrder["bsd_siteaddress"] = new EntityReference("bsd_address", orderproduct_cus.SiteAddress);
                SubOrder["bsd_shiptoaccount"] = new EntityReference("account", orderproduct_cus.ReceiptCustomer);
                SubOrder["bsd_shiptoaddress"] = new EntityReference("bsd_address", orderproduct_cus.CustomerAddress);
                SubOrder["bsd_shippingfromaddress"] = new EntityReference("bsd_address", orderproduct_cus.ShippingFromAddress);
                SubOrder["bsd_shippingaddress"] = new EntityReference("bsd_address", orderproduct_cus.ShippingAddress);
                if (orderproduct_cus.DeliveryFrom == 861450001 || orderproduct_cus.DeliveryFrom == 861450002) //bhs - port || port -customer address
                {
                    SubOrder["bsd_port"] = new EntityReference("account", orderproduct_cus.Port);
                    SubOrder["bsd_addressport"] = new EntityReference("bsd_address", orderproduct_cus.AddressPort);
                }

                if (order.HasValue("bsd_fromdate")) SubOrder["bsd_fromdate"] = order["bsd_fromdate"];
                if (order.HasValue("bsd_factory")) SubOrder["bsd_factory"] = order["bsd_factory"];
                if (order.HasValue("bsd_todate")) SubOrder["bsd_todate"] = order["bsd_todate"];

                if (order.HasValue("bsd_paymentterm"))
                {
                    SubOrder["bsd_paymentterm"] = order["bsd_paymentterm"];
                    Entity bsd_paymentterm = myService.service.Retrieve(((EntityReference)order["bsd_paymentterm"]).LogicalName, ((EntityReference)order["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                    SubOrder["bsd_datept"] = new decimal((int)bsd_paymentterm["bsd_date"]);
                }

                if (order.HasValue("bsd_paymentmethod")) SubOrder["bsd_paymentmethod"] = order["bsd_paymentmethod"];
                if (order.HasValue("bsd_invoiceaccount")) SubOrder["bsd_invoiceaccount"] = order["bsd_invoiceaccount"];
                if (order.HasValue("bsd_invoicenameaccount")) SubOrder["bsd_invoicenameaccount"] = order["bsd_invoicenameaccount"];
                if (order.HasValue("bsd_addressinvoiceaccount")) SubOrder["bsd_addressinvoiceaccount"] = order["bsd_addressinvoiceaccount"];
                if (order.HasValue("bsd_contactinvoiceaccount")) SubOrder["bsd_contactinvoiceaccount"] = order["bsd_contactinvoiceaccount"];
                if (order.HasValue("bsd_address")) SubOrder["bsd_addresscustomeraccount"] = order["bsd_address"];
                if (order.HasValue("bsd_unitdefault")) SubOrder["bsd_unitdefault"] = order["bsd_unitdefault"];
                SubOrder["bsd_priceincludeshippingporter"] = order["bsd_priceincludeshippingporter"];
                if (order.HasValue("bsd_historycustomername")) SubOrder["bsd_historycustomeraccount"] = order["bsd_historycustomername"];
                if (order.HasValue("bsd_historyinvoicename")) SubOrder["bsd_historyinvoicename"] = order["bsd_historyinvoicename"];
                if (orderproduct.HasValue("bsd_historyreceiptcustomer")) SubOrder["bsd_historyreceiptcustomer"] = orderproduct["bsd_historyreceiptcustomer"];
                #region Lấy Exchange Rate 

                decimal bsd_exchangeratevalue = 1m;

                Guid account_currency = ((EntityReference)order["transactioncurrencyid"]).Id;

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

                if (!account_currency.Equals(bsd_currencydefault.Id)) // nếu không bằng với unit default.
                {
                    EntityCollection list_exchangerate = myService.FetchXml(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
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
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>", account_currency
                        , bsd_currencydefault.Id
                        , bsd_bankdefault.Id,
                    myService.RetrieveLocalTimeFromUTCTime(DateTime.Now, myService.service).ToString("yyyy-MM-dd")));

                    if (list_exchangerate.Entities.Any())
                    {
                        Entity ex_changerate = list_exchangerate.Entities.First();
                        SubOrder["bsd_exchangerate"] = new EntityReference(ex_changerate.LogicalName, ex_changerate.Id);
                        SubOrder["bsd_bank"] = new EntityReference(bsd_bankdefault.LogicalName, bsd_bankdefault.Id);
                        bsd_exchangeratevalue = (decimal)ex_changerate["bsd_exchangerate"];
                    }
                    else
                    {
                        throw new Exception("Please create a new Exchage rate");
                    }
                }
                else
                {
                    bsd_exchangeratevalue = 1m;
                }
                SubOrder["bsd_exchangeratevalue"] = bsd_exchangeratevalue;
                #endregion

                #region Due date
                DateTime suborder_date = DateTime.Now;
                DateTime? due_date = null;
                int date_paymentterm = 0;
                if (account.HasValue("bsd_paymentterm")) // 15Days
                {
                    Entity bsd_paymentterm = myService.service.Retrieve("bsd_paymentterm", ((EntityReference)account["bsd_paymentterm"]).Id, new ColumnSet("bsd_date"));
                    date_paymentterm = (int)bsd_paymentterm["bsd_date"];
                }
                if (account.HasValue("bsd_paymentday"))
                {
                    int payment_day_ofmonth = (int)account["bsd_paymentday"]; // ngày trả nợ trong tháng
                    DateTime tong = suborder_date.AddDays(date_paymentterm); // ngày đơn hàng + ngày trả sau.
                    if (tong.Day > payment_day_ofmonth) // tổng lớn hơn ngày phải trả. -> dời qua tháng sau, lấy ngày phải trả.
                    {
                        due_date = new DateTime(tong.Year, tong.AddMonths(1).Month, 1);
                        due_date = ((DateTime)due_date).AddDays(payment_day_ofmonth - 1);
                    }
                    else
                    {
                        due_date = new DateTime(tong.Year, tong.Month, 1);
                        due_date = ((DateTime)due_date).AddDays(payment_day_ofmonth - 1);
                    }
                }
                else
                {
                    due_date = suborder_date.AddDays(date_paymentterm);
                }
                due_date = myService.RetrieveLocalTimeFromUTCTime((DateTime)due_date, myService.service);
                if (((DateTime)due_date).DayOfWeek == DayOfWeek.Saturday)
                {
                    due_date = ((DateTime)due_date).AddDays(2);
                }
                else if (((DateTime)due_date).DayOfWeek == DayOfWeek.Sunday)
                {
                    due_date = ((DateTime)due_date).AddDays(1);
                }
                SubOrder["bsd_duedate"] = due_date;
                #endregion

                #region Chuyển shiping porter tax
                if (order.HasValue("bsd_saletaxgroup")) SubOrder["bsd_saletaxgroup"] = order["bsd_saletaxgroup"];
                if (order.HasValue("bsd_requestporter")) SubOrder["bsd_requestporter"] = order["bsd_requestporter"];
                if (order.HasValue("bsd_transportation")) SubOrder["bsd_transportation"] = order["bsd_transportation"];
                if (order.HasValue("bsd_shippingdeliverymethod")) SubOrder["bsd_shippingdeliverymethod"] = order["bsd_shippingdeliverymethod"];
                if (order.HasValue("bsd_truckload")) SubOrder["bsd_truckload"] = order["bsd_truckload"];
                if (order.HasValue("bsd_unitshipping")) SubOrder["bsd_unitshipping"] = order["bsd_unitshipping"];
                if (order.HasValue("bsd_shippingpricelistname")) SubOrder["bsd_shippingpricelistname"] = order["bsd_shippingpricelistname"];
                if (order.HasValue("bsd_priceoftransportationn")) SubOrder["bsd_priceoftransportationn"] = order["bsd_priceoftransportationn"];
                if (order.HasValue("bsd_shippingporter")) SubOrder["bsd_shippingporter"] = order["bsd_shippingporter"];
                if (order.HasValue("bsd_porteroption")) SubOrder["bsd_porteroption"] = order["bsd_porteroption"];
                if (order.HasValue("bsd_priceofporter")) SubOrder["bsd_priceofporter"] = order["bsd_priceofporter"];
                if (order.HasValue("bsd_pricepotter")) SubOrder["bsd_pricepotter"] = order["bsd_pricepotter"];
                if (order.HasValue("bsd_porter")) SubOrder["bsd_porter"] = order["bsd_porter"];
                //bsd_porter
                #endregion

                #region Shipping + Porter
                var shipping = (bool)orderproduct["bsd_shippingoption"];
                if (shipping)
                {
                    SubOrder["bsd_transportation"] = true;
                    SubOrder["bsd_requestporter"] = orderproduct["bsd_requestporter"];
                    SubOrder["bsd_shippingdeliverymethod"] = orderproduct["bsd_shippingdeliverymethod"];

                    if (orderproduct.HasValue("bsd_truckload"))
                    {
                        SubOrder["bsd_truckload"] = orderproduct["bsd_truckload"];
                    }

                    if (orderproduct.HasValue("bsd_unitshipping"))
                    {
                        SubOrder["bsd_unitshipping"] = orderproduct["bsd_unitshipping"];
                    }

                    SubOrder["bsd_shippingpricelistname"] = orderproduct["bsd_shippingpricelist"];

                    SubOrder["bsd_priceoftransportationn"] = orderproduct["bsd_shippingpricelistvalue"];
                    SubOrder["bsd_shippingporter"] = orderproduct["bsd_shippingporter"];

                }

                SubOrder["bsd_porteroption"] = orderproduct["bsd_porteroption"];
                if (orderproduct.HasValue("bsd_portermethod"))
                {
                    SubOrder["bsd_portermethod"] = orderproduct["bsd_portermethod"];
                }
                if (orderproduct.HasValue("bsd_porterpricelist"))
                {
                    SubOrder["bsd_priceofporter"] = orderproduct["bsd_porterpricelist"];
                }
                if (orderproduct.HasValue("bsd_porterpricelistvalue"))
                {
                    SubOrder["bsd_pricepotter"] = orderproduct["bsd_porterpricelistvalue"];
                }

                if (orderproduct.HasValue("bsd_portertype"))
                {
                    SubOrder["bsd_porter"] = orderproduct["bsd_portertype"];
                }
                #endregion

                Guid suborder_id = myService.service.Create(SubOrder);

                #endregion

                #region Tạo Suborder Product và update lại giá trên suborder
                bool condition =
                        orderproduct_cus.UsingTax == (bool)orderproduct["bsd_usingtax"]
                        && orderproduct_cus.ShippingYesNo == (bool)orderproduct["bsd_usingshipping"]
                        && orderproduct_cus.Item_Sales_Tax == (orderproduct.HasValue("bsd_itemsalestax") ? (decimal)orderproduct["bsd_itemsalestax"] : 0);

                condition = condition
                   && (orderproduct_cus.DeliveryFrom == ((OptionSetValue)orderproduct["bsd_deliveryfrom"]).Value
                   && orderproduct_cus.ShippingFromAddress == ((EntityReference)orderproduct["bsd_shippingfromaddress"]).Id
                   && orderproduct_cus.ShippingAddress == ((EntityReference)orderproduct["bsd_shippingaddress"]).Id
                   && orderproduct_cus.ReceiptCustomer == ((EntityReference)orderproduct["bsd_partnerscompany"]).Id
                   && orderproduct_cus.CustomerAddress == ((EntityReference)orderproduct["bsd_shiptoaddress"]).Id
                   );

                if (condition)
                {
                    bool con = true;
                    while (con == true)
                    {
                        con = false;
                        Entity product = myService.service.Retrieve("product", ((EntityReference)orderproduct["productid"]).Id, new ColumnSet(true));
                        decimal order_quantity = (decimal)orderproduct["quantity"];
                        decimal standard_quantity = (decimal)orderproduct["bsd_standardquantity"];
                        decimal price_per_unit = ((Money)orderproduct["priceperunit"]).Value;
                        decimal shippingprice = ((decimal)orderproduct["bsd_giashipsauthue_full"]);
                        decimal porter_price = ((decimal)orderproduct["bsd_porterprice_full"]);
                        decimal vat = orderproduct.HasValue("bsd_vatprice_full") ? ((decimal)orderproduct["bsd_vatprice_full"]) : 0m;
                        decimal tax = vat * totalline_remaining_quantity;
                        decimal giatruocthue = price_per_unit + shippingprice + porter_price;
                        decimal giasauthue = giatruocthue + vat;
                        decimal amount = giatruocthue * totalline_remaining_quantity;
                        decimal extendedamount = giasauthue * totalline_remaining_quantity;
                        decimal currency_exchange = bsd_exchangeratevalue * extendedamount;
                        if (currency_exchange > 100000000000)
                        {
                            con = true;
                            totalline_remaining_quantity = 0m;
                        }
                        else
                        {
                            #region Tạo sub product
                            Entity sub_product = new Entity("bsd_suborderproduct");
                            sub_product["bsd_name"] = product["name"];
                            sub_product["bsd_quote"] = target;
                            sub_product["bsd_type"] = new OptionSetValue(861450001);
                            sub_product["bsd_suborder"] = new EntityReference("bsd_suborder", suborder_id);
                            sub_product["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                            if (orderproduct.HasValue("bsd_productid")) sub_product["bsd_productid"] = orderproduct["bsd_productid"];
                            if (orderproduct.HasValue("bsd_descriptionproduct")) sub_product["bsd_descriptionproduct"] = orderproduct["bsd_descriptionproduct"];
                            sub_product["bsd_priceperunit"] = new Money(price_per_unit);
                            sub_product["bsd_orderquantity"] = order_quantity;
                            sub_product["bsd_standardquantity"] = standard_quantity;
                            sub_product["bsd_totalquantity"] = standard_quantity * totalline_remaining_quantity;
                            sub_product["bsd_shippedquantity"] = 0m;
                            sub_product["bsd_residualquantity"] = totalline_remaining_quantity;
                            sub_product["bsd_giatruocthue"] = new Money(giatruocthue);
                            sub_product["bsd_giasauthue"] = new Money(giasauthue);
                            sub_product["bsd_amount"] = new Money(amount);
                            sub_product["bsd_unit"] = orderproduct["uomid"];
                            sub_product["bsd_usingtax"] = orderproduct["bsd_usingtax"];
                            sub_product["bsd_currencyexchangecurrency"] = new Money(currency_exchange);
                            sub_product["bsd_currencyexchangetext"] = currency_exchange.DecimalToStringHideSymbol();
                            sub_product["transactioncurrencyid"] = order["transactioncurrencyid"];
                            sub_product["bsd_shippingprice"] = new Money(shippingprice);
                            sub_product["bsd_porterprice"] = new Money(porter_price);
                            sub_product["bsd_shipquantity"] = totalline_remaining_quantity;
                            sub_product["bsd_newquantity"] = totalline_remaining_quantity;
                            sub_product["bsd_extendedamount"] = new Money(extendedamount);
                            if (orderproduct.HasValue("bsd_itemsalestaxgroup"))
                            {
                                sub_product["bsd_itemsalestaxgroup"] = orderproduct["bsd_itemsalestaxgroup"];
                            }
                            if (orderproduct.HasValue("bsd_itemsalestax"))
                            {
                                sub_product["bsd_itemsalestax"] = orderproduct["bsd_itemsalestax"];
                            }

                            if (orderproduct_cus.UsingTax)
                            {
                                sub_product["bsd_tax"] = new Money(tax);
                                sub_product["bsd_vatprice"] = new Money(vat);
                            }
                            else
                            {
                                sub_product["bsd_tax"] = null;
                                sub_product["bsd_vatprice"] = null;
                            }
                            myService.service.Create(sub_product);
                            #endregion

                            if (have_quantity == false)
                            {
                                #region cập nhật total line
                                Entity new_totalline = new Entity(totalline.LogicalName, totalline.Id);
                                decimal totalline_quantinty = (decimal)totalline["bsd_quantity"];
                                decimal totalline_suborderquantinty = (decimal)totalline["bsd_suborderquantity"];
                                decimal totalline_newsuborderquantity = totalline_suborderquantinty + totalline_remaining_quantity;

                                decimal totalline_shippedquantity = (decimal)totalline["bsd_shippedquantity"];

                                new_totalline["bsd_remainingquantity"] = totalline_quantinty - totalline_newsuborderquantity;
                                new_totalline["bsd_suborderquantity"] = totalline_newsuborderquantity;
                                new_totalline["bsd_residualquantity"] = totalline_quantinty - totalline_shippedquantity;
                                myService.service.Update(new_totalline);
                                #endregion

                                #region update line soluong 0
                                Entity new_orderproduct = new Entity(orderproduct.LogicalName, orderproduct.Id);

                                decimal orderproduct_standardquantity = (decimal)orderproduct["bsd_standardquantity"];
                                decimal old_sunborderquantity = (decimal)orderproduct["bsd_suborderquantity"];
                                decimal new_suborderquantity = old_sunborderquantity + totalline_remaining_quantity;
                                new_orderproduct["bsd_totalquantity"] = orderproduct_standardquantity * new_suborderquantity;
                                new_orderproduct["bsd_suborderquantity"] = new_suborderquantity;
                                new_orderproduct["bsd_residualquantity"] = new_suborderquantity;
                                new_orderproduct["bsd_shippedquantity"] = 0m;
                                myService.Update(new_orderproduct);
                                #endregion
                            }
                            else
                            {
                                #region cập nhật quote product
                                decimal suborder_quantity = (decimal)orderproduct["bsd_suborderquantity"];
                                Entity new_orderproduct = new Entity(orderproduct.LogicalName, orderproduct.Id);
                                decimal new_suborderquantity = suborder_quantity + totalline_remaining_quantity;
                                new_orderproduct["bsd_suborderquantity"] = new_suborderquantity;
                                new_orderproduct["bsd_remainingquantity"] = order_quantity - new_suborderquantity;
                                myService.service.Update(new_orderproduct);
                                #endregion
                            }
                        }
                    }
                }

                #region Update suborder
                Entity suborder_update = myService.service.Retrieve(SubOrder.LogicalName, suborder_id, new ColumnSet(true));
                new SuborderService(myService).UpdateSubOrder(suborder_update);
                #endregion

                #endregion

                #region won quote
                myService.SetState(order.Id, order.LogicalName, 1, 2);
                WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
                Entity quoteClose = new Entity("quoteclose");
                quoteClose.Attributes["quoteid"] = new EntityReference("quote", new Guid(order.Id.ToString()));
                quoteClose.Attributes["subject"] = "Quote Close" + DateTime.Now.ToString();
                winQuoteRequest.QuoteClose = quoteClose;
                winQuoteRequest.Status = new OptionSetValue(-1);
                myService.service.Execute(winQuoteRequest);
                #endregion

                myService.context.OutputParameters["SuborderId"] = suborder_id.ToString();
            }
            #endregion

            #region Update
            else if (myService.context.MessageName == "Update")
            {
               
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_skipplugin") && (bool)target["bsd_skipplugin"]) return;

                myService.StartService();
                Entity suborder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

                int statuscode = ((OptionSetValue)suborder["statuscode"]).Value;
                bool duyet = suborder.HasValue("bsd_duyet") ? (bool)suborder["bsd_duyet"] : false;
                if (duyet == true || statuscode == 861450002) return;

                #region Kiểm tra tạo từ b2c
                if (suborder.HasValue("bsd_fromb2c") && (bool)suborder["bsd_fromb2c"])
                {
                    return;
                }
                #endregion

                int type = ((OptionSetValue)suborder["bsd_type"]).Value;
                if (type == 861450000)
                {
                    SuborderService suborderService = new SuborderService(myService);
                    EntityCollection list_suborderproduct = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);
                    if (list_suborderproduct.Entities.Any())
                    {
                        foreach (var suborder_product in list_suborderproduct.Entities)
                        {
                            suborderService.Create_Update_Suborder_Product(suborder_product, 1, false);
                        }
                        suborderService.UpdateSubOrder(suborder);
                    }
                }

            }
            #endregion

            #region Delete
            else if (myService.context.MessageName == "Delete")
            {
                EntityReference target = (EntityReference)myService.context.InputParameters["Target"];
                myService.StartService();
                Entity suborder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

                int status = ((OptionSetValue)suborder["statecode"]).Value;
                if (status == 1) return;
                #region Kiểm tra tạo từ b2c
                if (suborder.HasValue("bsd_fromb2c") && (bool)suborder["bsd_fromb2c"])
                {
                    return;
                }
                #endregion

                if (suborder.HasValue("bsd_tennhanvienduyet") || suborder.HasValue("bsd_tentruongphongduyet") || suborder.HasValue("bsd_tencapthamquyenduyet"))
                {
                    throw new Exception("This suborder is approved !");
                }
                if ((bool)suborder["bsd_duyet"])
                {
                    throw new Exception("Suborder has been approved, not deleted !");
                }
            }
            #endregion

            #region bsd_Action_CheckSiteQuantity
            else if (myService.context.MessageName == "bsd_Action_CheckSiteQuantity")
            {
                
               

                //try { 
                #region vinhlh 22-12-2017 check ton kho
                #region config production AX
                //string _userName = "s.ttctech";
                //string _passWord = "AX@tct2017";
                //string _company = "102";
                //string _port = "10.33.21.1:8201";
                //string _domain = "SUG.TTCG.LAN";
                #endregion

                #region config Local AX
                //string _userName = "crm21";
                //string _passWord = "bsd@123";
                //string _company = "BHS";
                //string _port = "192.168.68.31:8201";
                //string _domain = "BSD.LOCAL";
                #endregion

                #region config training AX
                //string _userName = "bsd01";
                //string _passWord = "bsd@123";
                //string _company = "102";
                //string _port = "10.33.3.25:8201";
                //string _domain = "dynamics.LOCAL";
                #endregion
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
                Entity suborder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                Entity account = myService.service.Retrieve("account", ((EntityReference)suborder["bsd_potentialcustomer"]).Id, new ColumnSet(true));
                Entity configdefault = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_configdefault'>
                    <attribute name='bsd_configdefaultid' />
                    <attribute name='createdon' />
                    <attribute name='bsd_checkinventory' />
                    <order attribute='createdon' descending='true' />
                  </entity>
                </fetch>").Entities.FirstOrDefault();
                bool connect_ax = getConfigdefaultconnectax();
                List<ThongTinTonKho> list_thongtintonkho = new List<ThongTinTonKho>();
                if (connect_ax == false)
                {
                    ThongTinTonKho thongtin = new ThongTinTonKho();
                    thongtin.ProductName = "Thiếu khai báo connect ax";
                    list_thongtintonkho.Add(thongtin);
                    myService.context.OutputParameters["Result"] = -1;
                    var Serializedresult = Util.JSONSerialize(list_thongtintonkho);
                   // throw new Exception(thongtin.ProductName);
                    throw new Exception(Serializedresult);
                }
                bool check_inventory = configdefault.HasValue("bsd_checkinventory") ? (bool)configdefault["bsd_checkinventory"] : true;
                bool check_inventory_account = account.HasValue("bsd_checkinventory") ? (bool)account["bsd_checkinventory"] : true;

                //string lst_CheckProductAX = "";
                //int i = 0;
                var suborder_type = ((OptionSetValue)suborder["bsd_type"]).Value;
                if (suborder_type != 861450003 && suborder_type != 861450004  && check_inventory && check_inventory_account) // Khác mật rỉ
                {
                    DateTime date = (DateTime)suborder["bsd_date"];
                    //if (((OptionSetValue)suborder["statuscode"]).Value == 861450000 || ((OptionSetValue)suborder["statuscode"]).Value == 861450001)
                    if (true)
                    {
                        string s_Result = "";
                        #region KHai báo service AX
                        NetTcpBinding binding = new NetTcpBinding();
                        //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                        binding.Name = "NetTcpBinding_BHS_BSD_CRMSERVICEAXService";
                        EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));
                        ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                        client.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                        client.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                        client.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                        CallContext contextService = new CallContext() { Company = _company };
                        #endregion
                        #region kiểm tra ton kho
                        EntityReference site = (EntityReference)suborder["bsd_site"];
                        Entity en_Site = myService.service.Retrieve(site.LogicalName, site.Id, new ColumnSet("bsd_code"));
                        EntityCollection list_suborderproduct = myService.FetchXml(
                            @"<fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='false'>',
                                   <entity name='bsd_suborderproduct'>
                                         <attribute name='bsd_suborderproductid' />
                                         <attribute name='bsd_totalquantity' />
                                         <attribute name='bsd_product' />
                                         <filter type='and'>
                                               <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='" + suborder.Id + @"' />  
                                          </filter>                                                                 
                                    </entity>         
                            </fetch>");
                        int i = 0;
                        string lst_CheckProductAX = "";
                        string lst_Product = "";
                        foreach (var suborderproduct in list_suborderproduct.Entities)
                        {
                            #region list Product
                            decimal suborder_quantity = (decimal)suborderproduct["bsd_totalquantity"];
                            //decimal warehouse_quantity = 0m;
                            EntityReference product_ref = (EntityReference)suborderproduct["bsd_product"];
                            Entity product = (myService.service.Retrieve(product_ref.LogicalName, product_ref.Id, new ColumnSet(true)));
                            lst_Product = product["productnumber"].ToString().Trim() + ":" + en_Site["bsd_code"].ToString().Trim() + ":" + "null" + ":" + suborder_quantity;
                            if (i == 0) lst_CheckProductAX = lst_Product;
                            else lst_CheckProductAX += ";" + lst_Product.Trim();
                            i++;
                            #region vinhlh 23-10-2017
                            /*

                            EntityCollection list_warehouse = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>',
                              <entity name='bsd_warehouseentity'>
                                      <attribute name='bsd_warehouseentityid' />
                                      <filter type='and'>
                                           <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + site.Id + @"' />
                                      </filter>
                                  </entity>
                              </fetch>");
                            foreach (var warehouse in list_warehouse.Entities)
                            {
                                EntityCollection list_warehouseproduct = myService.FetchXml(
                                    @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                       <entity name='bsd_warehourseproduct'>
                                           <attribute name='bsd_warehourseproductid' />
                                           <attribute name='bsd_unitconvertion' />
                                           <attribute name='bsd_date' />
                                           <order attribute='modifiedon' descending='true' />
                                           <filter type='and'>
                                               <condition attribute='statecode' operator='eq' value='0' />
                                               <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product_ref.Id + @"' />
                                               <condition attribute='bsd_warehouses' operator='eq' uitype='bsd_warehouseentity' value='" + warehouse.Id + @"' />
                                           </filter>
                                           </entity>
                                    </fetch>");
                                if (list_warehouseproduct.Entities.Any())
                                {
                                    Entity warehouse_product = list_warehouseproduct.Entities.First();
                                    warehouse_quantity += (decimal)warehouse_product["bsd_unitconvertion"];
                                }
                            }

                            decimal submit_quantity = this.Lay_So_Luong_San_Pham_Da_Su_dung(suborder.Id, site.Id, product_ref.Id); // số lượng đã có trong suborder submit
                            decimal can_using_quantity = warehouse_quantity - submit_quantity; // số lượng khả dụng.
                            if (suborder_quantity > can_using_quantity)
                            {
                                if (can_using_quantity < 0) can_using_quantity = 0; // nếu số lượng có thể sử dụng nhỏ hơn không thì cho bằng 0 luôn.

                                ThongTinTonKho thongtin = new ThongTinTonKho();
                                thongtin.ProductName = product["name"].ToString();
                                thongtin.AvailableQuantity = can_using_quantity;
                                thongtin.SuborderQuantity = suborder_quantity;
                                list_thongtintonkho.Add(thongtin);
                                myService.context.OutputParameters["Result"] = -1;
                            }
                             */
                            #endregion
                            #endregion
                        }
                        #region Call service
                        try
                        {
                            s_Result = client.BHS_ValidateOnHand(contextService, lst_CheckProductAX);
                            //throw new Exception(s_Result+";"+ lst_CheckProductAX);
                        }
                        catch (Exception ex)
                        {
                            ThongTinTonKho thongtin = new ThongTinTonKho();
                            thongtin.ProductName = ex.Message;
                            list_thongtintonkho.Add(thongtin);
                            myService.context.OutputParameters["Result"] = -1;
                            if (list_thongtintonkho.Any())
                            {
                                var Serializedresult = Util.JSONSerialize(list_thongtintonkho);
                                throw new Exception(Serializedresult);
                            }
                            // throw new Exception("Service AX: " + ex.Message);
                        }
                        // throw new Exception(s_Result);
                        string[] lstProduct_Result = new string[] { };
                        string[] lstitem = new string[] { };
                        lstProduct_Result = s_Result.Split(';');
                        // throw new Exception(s_Result);
                        foreach (string item in lstProduct_Result)
                        {
                            lstitem = item.Split(':');
                            decimal SuborderQuantity = Convert.ToDecimal(lstitem[4]);
                            decimal AvailableQuantity = Convert.ToDecimal(lstitem[5]);
                            decimal total_SuborderQuantity_Approve = getToTalQuantitySuborderProduct(lstitem[0].ToString(), suborder.Id);
                            if ((SuborderQuantity + total_SuborderQuantity_Approve) > AvailableQuantity)
                            {
                                ThongTinTonKho thongtin = new ThongTinTonKho();
                                thongtin.ProductName = lstitem[1];
                                thongtin.AvailableQuantity = AvailableQuantity - total_SuborderQuantity_Approve;
                                thongtin.SuborderQuantity = SuborderQuantity;
                                thongtin.SynQuantity = AvailableQuantity;
                                list_thongtintonkho.Add(thongtin);
                                myService.context.OutputParameters["Result"] = -1;
                            }
                        }

                        #endregion
                        #endregion
                    }
                }
                else
                {
                    myService.context.OutputParameters["Result"] = 1;
                }
                //ThongTinTonKho thongtin = new ThongTinTonKho();
                //thongtin.ProductName = "123";
                //thongtin.AvailableQuantity = 200;
                //thongtin.SuborderQuantity = 10;
                //list_thongtintonkho.Add(thongtin);
                if (list_thongtintonkho.Any())
                {
                    myService.context.OutputParameters["Result"] = -1;
                    var Serializedresult = Util.JSONSerialize(list_thongtintonkho);
                    throw new Exception(Serializedresult);
                }
                else myService.context.OutputParameters["Result"] = 1;

                #endregion
                // }
                //catch (Exception ex)
                //{
                //    throw new Exception("CheckSiteQuantity: " + ex.Message);
                //}
            }
            #endregion

        }
        public decimal Lay_So_Luong_San_Pham_Da_Su_dung(Guid suborderid, Guid siteid, Guid productid)
        {
            // lấy số lượng sản phẩm trong site đã submit
            decimal quantity_used = 0m;

            #region 4 status reason
            EntityCollection statusreason = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_suborderproduct'>
                <attribute name='bsd_suborderproductid' />
                <attribute name='bsd_totalquantity' />
                <attribute name='bsd_submittedquantity' />
                <filter type='and'>
                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                </filter>
                <link-entity name='bsd_suborder' from='bsd_suborderid' to='bsd_suborder' alias='ac'>
                      <filter type='and'>
                            <condition attribute='statecode' operator='eq' value='0' />
                            <condition attribute='statuscode' operator='in'>
                                  <value>861450004</value>
                                  <value>861450003</value>
                                  <value>861450005</value>
                                  <value>861450006</value>
                            </condition>
                            <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + siteid + @"' />
                            <condition attribute='bsd_suborderid' operator='ne' uitype='bsd_suborder' value='" + suborderid + @"' />
                      </filter>
                </link-entity>
              </entity>
            </fetch>");
            if (statusreason.Entities.Any())
            {
                foreach (var sp in statusreason.Entities)
                {
                    if (sp.HasValue("bsd_submittedquantity"))
                    {
                        quantity_used += (decimal)sp["bsd_submittedquantity"];
                    }
                    else
                    {
                        quantity_used += (decimal)sp["bsd_totalquantity"];
                    }
                }
            }
            #endregion

            EntityCollection capthamquyenduyet = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_suborderproduct'>
                <attribute name='bsd_suborderproductid' />
                <attribute name='bsd_totalquantity' />
                <filter type='and'>
                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                </filter>
                <link-entity name='bsd_suborder' from='bsd_suborderid' to='bsd_suborder' alias='ac'>
                      <attribute name='bsd_status' alias='deliverystatus' />
                      <filter type='and'>
                            <condition attribute='statecode' operator='eq' value='0' />
                            <condition attribute='statuscode' operator='in'>
                                  <value>861450002</value>
                            </condition>
                            <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + siteid + @"' />
                            <condition attribute='bsd_suborderid' operator='ne' uitype='bsd_suborder' value='" + suborderid + @"' />
                      </filter>
                </link-entity>
              </entity>
            </fetch>");

            foreach (var sp in capthamquyenduyet.Entities)
            {
                int deliverystatus = ((OptionSetValue)((AliasedValue)sp["deliverystatus"]).Value).Value;
                if (deliverystatus == 861450000) // trạng thái của suborder là chưa giao
                {
                    // lấy phiếu xuất kho
                    EntityCollection list_deliveryproductbill = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_deliveryproductbill'>
                        <attribute name='bsd_deliveryproductbillid' />
                        <attribute name='bsd_netquantity' />
                        <attribute name='bsd_quantity' />
                        <filter type='and'>
                          <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                        </filter>
                        <link-entity name='bsd_deliverybill' from='bsd_deliverybillid' to='bsd_deliverybill' alias='aa'>
                           <filter type='and'>
                                <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + siteid + @"' />
                           </filter>
                          <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='ab'>
                            <link-entity name='bsd_deliveryplan' from='bsd_deliveryplanid' to='bsd_deliveryplan' alias='ac'>
                              <filter type='and'>
                                <condition attribute='bsd_suborder' operator='ne' uitype='bsd_suborder' value='" + suborderid + @"' />
                              </filter>
                            </link-entity>
                          </link-entity>
                        </link-entity>
                      </entity>
                    </fetch>");
                    if (list_deliveryproductbill.Entities.Any()) // chưa giao đã tạo phiếu xuất kho
                    {
                        // nếu đã tạo phiếu xuất kho tihf công dồn lại net quantity
                        foreach (var deliveryproductbill in list_deliveryproductbill.Entities)
                        {
                            decimal standard_quantity = Lay_Standard_Quantity_FromDeliveryProductbill(productid, deliveryproductbill.Id);
                            quantity_used += (standard_quantity * (decimal)deliveryproductbill["bsd_quantity"]);
                        }
                    }
                    else // chưa giao và chưa tạo phiếu xuất kho
                    {
                        // giữ
                        quantity_used += (decimal)sp["bsd_totalquantity"];
                    }
                }
                else // đã tạo p hiếu giao hàng
                {
                    DateTime? last_import_site = Lay_Thoi_Gian_Import_Gan_Nhat_Trong_Site(productid, siteid);
                    if (last_import_site.HasValue == false) throw new Exception("Please import product to warehouse !");
                    DateTime date = (DateTime)last_import_site;

                    string date_str = date.Year + "-" + (date.Month < 10 ? "0" + date.Month : date.Month.ToString()) + "-" + (date.Day < 10 ? "0" + date.Day : date.Day.ToString()) + " " + date.Hour + ":" + date.Minute + ":" + date.Second;
                    // lấy phiếu giao hàng có thời gian tạo sau thời gian import để giữ lại 

                    EntityCollection list_deliverynoteproduct =
                    myService.FetchXml(
                    @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_deliverynoteproduct'>
                                <attribute name='bsd_deliverynoteproductid' />
                                <attribute name='bsd_quantity' />
                                <filter type='and'>
                                      <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                                      <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                                <link-entity name='bsd_deliverynote' from='bsd_deliverynoteid' to='bsd_deliverynote' alias='ah'>
                                      <filter type='and'>
                                            <condition attribute='createdon' operator='ge' value='" + date_str + @"' />
                                            <condition attribute='statecode' operator='eq' value='0' />
                                            <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + siteid + @"' />
                                            <condition attribute='statecode' operator='eq' value='0' />
                                            <condition attribute='bsd_status' operator='in'>
                                                  <value>861450002</value>
                                                  <value>861450000</value>
                                                  <value>861450001</value>
                                                  <value>861450003</value>
                                            </condition>
                                      </filter>
                                      <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='ai'>
                                            <link-entity name='bsd_deliveryplan' from='bsd_deliveryplanid' to='bsd_deliveryplan' alias='aj'>
                                                  <filter type='and'>
                                                        <condition attribute='bsd_suborder' operator='ne' uitype='bsd_suborder' value='" + suborderid + @"' />
                                                  </filter>
                                            </link-entity>
                                      </link-entity>
                                </link-entity>
                          </entity>
                    </fetch>");
                    if (list_deliverynoteproduct.Entities.Any())
                    {
                        foreach (var deliverynoteproduct in list_deliverynoteproduct.Entities)
                        {
                            decimal standar_quantity = Lay_Standard_Quantity_FromDeliveryNoteProduct(productid, deliverynoteproduct.Id);
                            quantity_used += (standar_quantity * (decimal)deliverynoteproduct["bsd_quantity"]);
                        }
                    }
                }
            }
            return quantity_used;
        }
        public DateTime? Lay_Thoi_Gian_Import_Gan_Nhat_Trong_Site(Guid productid, Guid siteid)
        {
            EntityCollection list = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_warehourseproduct'>
                <attribute name='bsd_warehourseproductid' />
                <attribute name='bsd_name' />
                <attribute name='createdon' />
                <attribute name='modifiedon' />
                <order attribute='modifiedon' descending='true' />
                <filter type='and'>
                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                </filter>
                <link-entity name='bsd_warehouseentity' from='bsd_warehouseentityid' to='bsd_warehouses' alias='ag'>
                  <filter type='and'>
                    <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + siteid + @"' />
                  </filter>
                </link-entity>
              </entity>
            </fetch>");
            if (list.Entities.Any())
            {
                DateTime date = (DateTime)list.Entities.First()["modifiedon"];
                date = myService.RetrieveLocalTimeFromUTCTime(date, myService.service);
                return date;
            }
            else
            {
                return null;
            }
        }
        public decimal Lay_Standard_Quantity_FromDeliveryProductbill(Guid productid, Guid deliveryproductbillid)
        {
            EntityCollection list = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
              <entity name='bsd_deliveryproductbill'>
                <attribute name='bsd_deliveryproductbillid' />
                <filter type='and'>
                  <condition attribute='bsd_deliveryproductbillid' operator='eq' uitype='bsd_deliveryproductbill' value='" + deliveryproductbillid + @"' />
                </filter>
                <link-entity name='bsd_deliverybill' from='bsd_deliverybillid' to='bsd_deliverybill' alias='aq'>
                  <link-entity name='bsd_deliveryplan' from='bsd_deliveryplanid' to='bsd_deliveryplan' alias='ar'>
                    <link-entity name='bsd_suborder' from='bsd_suborderid' to='bsd_suborder' alias='as'>
                      <link-entity name='bsd_suborderproduct' from='bsd_suborder' to='bsd_suborderid' alias='at'>
                        <attribute name='bsd_standardquantity' alias='factor' />
                        <filter type='and'>
                          <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                        </filter>
                      </link-entity>
                    </link-entity>
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>");
            if (list.Entities.Any())
            {
                decimal factor_alias = (decimal)(((AliasedValue)list.Entities.First()["factor"]).Value);
                return factor_alias;
            }
            else
            {
                return 0;
            }
        }
        public decimal Lay_Standard_Quantity_FromDeliveryNoteProduct(Guid productid, Guid deliverynoteproductid)
        {
            EntityCollection list = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
              <entity name='bsd_deliverynoteproduct'>
                <attribute name='bsd_deliverynoteproductid' />
                <filter type='and'>
                  <condition attribute='bsd_deliverynoteproductid' operator='eq' uitype='bsd_deliverynoteproduct' value='" + deliverynoteproductid + @"' />
                </filter>
                <link-entity name='bsd_deliverynote' from='bsd_deliverynoteid' to='bsd_deliverynote' alias='af'>
                  <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='ag'>
                    <link-entity name='bsd_deliveryplan' from='bsd_deliveryplanid' to='bsd_deliveryplan' alias='ah'>
                      <link-entity name='bsd_suborder' from='bsd_suborderid' to='bsd_suborder' alias='ai'>
                        <link-entity name='bsd_suborderproduct' from='bsd_suborder' to='bsd_suborderid' alias='aj'>
                          <attribute name='bsd_standardquantity' alias='factor' />
                          <filter type='and'>
                            <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                          </filter>
                        </link-entity>
                      </link-entity>
                    </link-entity>
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>");
            if (list.Entities.Any())
            {
                decimal factor_alias = (decimal)(((AliasedValue)list.Entities.First()["factor"]).Value);
                return factor_alias;
            }
            {
                return 0;
            }
        }
        public decimal getToTalQuantitySuborderProduct(string productId, Guid suborderId)
        {
            decimal result = 0m;
            string xml_getpricelistbybhstrading = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_suborderproduct'>
                                <attribute name='bsd_suborderproductid' />
                                <attribute name='bsd_name' />
                                <attribute name='bsd_suborder' />
                                 <attribute name='createdon' />
                                <attribute name='bsd_shipquantity' />
                                <order attribute='bsd_name' descending='false' />
                                <filter type='and'>
                                     <condition attribute='bsd_productid' operator='eq' value='" + productId.Trim() + @"' />
                                        <condition attribute='bsd_suborder' operator='ne'  uitype='bsd_suborder' value='" + suborderId + @"' />
                                </filter>
                                <link-entity name='bsd_suborder' from='bsd_suborderid' to='bsd_suborder' alias='aa'>
                                  <attribute name='bsd_tennhanvienduyet' />
                                  <filter type='and'>
                                    <condition attribute='statuscode' operator='in'>
                                      <value>861450004</value>
                                      <value>861450006</value>
                                      <value>861450003</value>
                                    </condition>
                                    <condition attribute='statecode' operator='eq' value='0' />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";
            EntityCollection lst_SubOrderProduct = myService.service.RetrieveMultiple(new FetchExpression(xml_getpricelistbybhstrading));

            if (lst_SubOrderProduct.Entities.Count > 0)
            {
                foreach (var item in lst_SubOrderProduct.Entities)
                {
                    // Console.WriteLine("bsd_shipquantity: " + item["bsd_shipquantity"]);
                    Entity suborder = myService.service.Retrieve(((EntityReference)item["bsd_suborder"]).LogicalName, ((EntityReference)item["bsd_suborder"]).Id, new ColumnSet(true));
                    if (item.HasValue("bsd_shipquantity"))
                    {
                        if (suborder.HasValue("bsd_tennhanvienduyet"))
                            result += (decimal)item["bsd_shipquantity"];
                        else if (((OptionSetValue)suborder["statuscode"]).Value != 861450006)
                            result += (decimal)item["bsd_shipquantity"];
                    }
                }
            }
            return result;
        }

        public bool getConfigdefaultconnectax()
        {
            Entity configdefault = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_configdefault'>
                    <attribute name='bsd_configdefaultid' />
                    <attribute name='createdon' />
                    <attribute name='bsd_checkinventory' />
                     <attribute name='bsd_usernameax' />
                     <attribute name='bsd_passwordax' /> 
                    <attribute name='bsd_company' />
                     <attribute name='bsd_portax' />
                     <attribute name='bsd_domain' />
                    <order attribute='createdon' descending='true' />
                  </entity>
                </fetch>").Entities.FirstOrDefault();
            if (configdefault.HasValue("bsd_usernameax") && configdefault.HasValue("bsd_passwordax") && configdefault.HasValue("bsd_company")
                && configdefault.HasValue("bsd_portax") && configdefault.HasValue("bsd_domain"))
            {
                _userName = configdefault["bsd_usernameax"].ToString();
                _passWord = configdefault["bsd_passwordax"].ToString();
                _company = configdefault["bsd_company"].ToString();
                _port = configdefault["bsd_portax"].ToString();
                _domain = configdefault["bsd_domain"].ToString();
                return true;
            }
            else
            {
                return false;
            }
            
        }
    }
}
