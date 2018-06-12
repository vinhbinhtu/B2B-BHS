using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugin_TransferAX.SalesOrderService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_TransferAX
{
    public class Main : IPlugin
    {
        private IOrganizationServiceFactory factory;
        public IOrganizationService service { get; set; }
        public IPluginExecutionContext context { get; set; }

        // public IOrganizationService b2bservice { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
            string ReturnIdAX = "";
            //string _userName = "crm21";
            //string _passWord = "bsd@123";
            //string _company = "BHS";
            //string _port = "192.168.68.31:8201";
            //string _domain = "BSD.LOCAL";
            string _userName = "bsd01";
            string _passWord = "bsd@123";
            string _company = "1050";
            string _port = "10.33.3.25:8201";
            string _domain = "dynamics.LOCAL";
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);
            if (context.MessageName == "bsd_TransferAX")
            {
                #region Connector AX Suborder
               
                //Connector AX
                EntityReference target = (EntityReference)context.InputParameters["Target"];
                try
                {
                    List<Product> lstProduct = new List<Product>();
                    string bsd_shiptoaddressid = "";
                    string bsd_invoiceaccount = "";//
                    string bsd_invoicenameaccount = "";//lookup
                    string bsd_addressinvoiceaccount = "";//lookup
                    string bsd_description = "";//
                    string bsd_paymentterm = "";//lookup
                    string bsd_name = "";//
                    string bsd_currencydefault = "VND";//lookup
                    string bsd_bankaccount = "";//lookup
                    string bsd_customercode = "";//
                    string bsd_shiptoaccount = "";//lookup
                    string bsd_shiptoaccountname = "";//lookup
                    string bsd_paymentmethod = "";
                    string bsd_shiptoaddress = "";//lookup
                    string bsd_status = "";//
                    string bsd_customerpo = "CRM01";
                    string bsd_site = "BHS";
                    string bsd_saletaxgroup = "";
                    string bsd_itemsalestaxgroup = "";
                    AxdType_DimensionAttributeValueSet dimAttValueSet = new AxdType_DimensionAttributeValueSet();
                    AxdType_DimensionAttributeValue dimAttValue = new AxdType_DimensionAttributeValue();
                    ReturnOrderService.AxdType_DimensionAttributeValueSet dimAttValueSetReturnOrder = new ReturnOrderService.AxdType_DimensionAttributeValueSet();
                    ReturnOrderService.AxdType_DimensionAttributeValue dimAttValueReturnOrder = new ReturnOrderService.AxdType_DimensionAttributeValue();
                    DateTime bsd_requestedshipdate = DateTime.Now;
                    DateTime bsd_requestedreceiptdate = DateTime.Now;
                    DateTime bsd_confirmedreceiptdate = DateTime.Now;
                    DateTime ShippingDateConfirmed = DateTime.Now;//Chưa có
                    Entity suborder = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));// lấy suborder của b2c
                    #region KHai bao service AX
                    NetTcpBinding binding = new NetTcpBinding();
                    //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                    binding.Name = "NetTcpBinding_BHS_BSD_CRMSERVICEAXService";
                    EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://"+_port+"/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));
                    ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                    client.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                    client.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                    client.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                    ServiceReferenceAIF.CallContext context = new ServiceReferenceAIF.CallContext() { Company = _company };

                    //
                    //string s = client.TestMethod(context);
                    //throw new Exception(s);
                    //
                    NetTcpBinding bindingSaleOrder = new NetTcpBinding();
                    //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                    bindingSaleOrder.Name = "NetTcpBinding_SalesOrderService";
                    EndpointAddress endpointSaleOrder = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/SalesOrderService"));
                    SalesOrderServiceClient proxy = new SalesOrderServiceClient(bindingSaleOrder, endpointSaleOrder);
                    proxy.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                    proxy.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                    proxy.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                    CallContext contextSaleOrder = new CallContext() { Company = _company };
                    //net.tcp://192.168.68.31:8201/DynamicsAx/Services/BHS_ReturnReturnOrderInServiceGroup
                    NetTcpBinding bindingReturnOrder = new NetTcpBinding();
                    bindingReturnOrder.Name = "NetTcpBinding_ReturnOrderInService";
                    EndpointAddress endpointReturnOrder = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/BHS_ReturnReturnOrderInServiceGroup"));
                    ReturnOrderService.ReturnOrderInServiceClient proxyReturnOrder = new ReturnOrderService.ReturnOrderInServiceClient(bindingReturnOrder, endpointReturnOrder);
                    proxyReturnOrder.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                    proxyReturnOrder.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                    proxyReturnOrder.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                    ReturnOrderService.CallContext contextReturnOrder = new ReturnOrderService.CallContext() { Company = _company };
                    //net.tcp://192.168.68.31:8201/DynamicsAx/Services/FinancialDimensionServices
                    NetTcpBinding bindingDimension = new NetTcpBinding();
                    bindingDimension.Name = "NetTcpBinding_DimensionValueService";
                    EndpointAddress endpointDimension = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/FinancialDimensionServices"));
                    FinancialDimensionServices.DimensionValueServiceClient proxyDimension = new FinancialDimensionServices.DimensionValueServiceClient(bindingDimension, endpointDimension);
                    proxyDimension.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                    proxyDimension.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                    proxyDimension.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                    FinancialDimensionServices.CallContext contextDimension = new FinancialDimensionServices.CallContext() { Company = _company };
                    //
                    AxdSalesOrder salesOrder = new AxdSalesOrder();
                    // Create instances of the entities that are used in the service and
                    // set the needed fields on those entities.
                    AxdEntity_SalesTable salesTable = new AxdEntity_SalesTable();
                    #endregion
                    #region set Data Transfer AX Suborder
                    //set Data Transfer AX


                    if (suborder.HasValue("bsd_name"))
                    {
                        bsd_name = suborder["bsd_name"].ToString();
                        bsd_customerpo = suborder["bsd_name"].ToString();
                    }
                    if (suborder.HasValue("bsd_invoiceaccount"))
                        bsd_invoiceaccount = suborder["bsd_invoiceaccount"].ToString();
                    if (suborder.HasValue("bsd_invoicenameaccount"))
                    {
                        //bsd_shiptoaccount = suborder["bsd_shiptoaccount"].ToString();
                        EntityReference rf_Entity = (EntityReference)suborder["bsd_invoicenameaccount"];
                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("name"));
                        if (Entity.HasValue("name"))
                            bsd_invoicenameaccount = Entity["name"].ToString();

                    }
                    if (suborder.HasValue("bsd_saletaxgroup"))
                    {
                        EntityReference rf_Entity = (EntityReference)suborder["bsd_saletaxgroup"];
                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_salestaxgroup"));
                        if (Entity.HasValue("bsd_salestaxgroup"))
                            bsd_saletaxgroup = Entity["bsd_salestaxgroup"].ToString();
                    }
                    if (suborder.HasValue("bsd_addressinvoiceaccount"))
                    {
                        EntityReference rf_addressinvoiceaccount = (EntityReference)suborder["bsd_addressinvoiceaccount"];
                        Entity Address = service.Retrieve(rf_addressinvoiceaccount.LogicalName, rf_addressinvoiceaccount.Id, new ColumnSet("bsd_name"));
                        if (Address.HasValue("bsd_name"))
                            bsd_addressinvoiceaccount = Address["bsd_name"].ToString();
                    }

                    if (suborder.HasValue("bsd_description"))
                        bsd_description = suborder["bsd_description"].ToString();
                    if (suborder.HasValue("bsd_paymentterm"))
                    {
                        EntityReference rf_Entity = (EntityReference)suborder["bsd_paymentterm"];
                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_termofpayment"));
                        if (Entity.HasValue("bsd_termofpayment"))
                            bsd_paymentterm = Entity["bsd_termofpayment"].ToString();

                    }
                    if (suborder.HasValue("bsd_paymentmethod"))
                    {
                        EntityReference rf_Entity = (EntityReference)suborder["bsd_paymentmethod"];
                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_methodofpayment"));
                        if (Entity.HasValue("bsd_methodofpayment"))
                            bsd_paymentmethod = Entity["bsd_methodofpayment"].ToString();

                    }
                    //bsd_paymentmethod
                    if (suborder.HasValue("bsd_currencydefault"))
                    {
                        EntityReference rf_Entity = (EntityReference)suborder["bsd_currencydefault"];
                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("isocurrencycode"));
                        if (Entity.HasValue("isocurrencycode"))
                            bsd_currencydefault = Entity["isocurrencycode"].ToString();
                    }
                    if (suborder.HasValue("bsd_bankaccount"))
                        bsd_bankaccount = suborder["bsd_bankaccount"].ToString();
                    if (suborder.HasValue("bsd_customercode"))
                        bsd_customercode = suborder["bsd_customercode"].ToString();
                    if (suborder.HasValue("bsd_customerpo"))
                        bsd_customerpo = suborder["bsd_customerpo"].ToString();
                    if (suborder.HasValue("bsd_requestedshipdate"))
                        bsd_requestedshipdate = DateTime.Parse(suborder["bsd_requestedshipdate"].ToString());
                    if (suborder.HasValue("bsd_requestedreceiptdate"))
                        bsd_requestedreceiptdate = DateTime.Parse(suborder["bsd_requestedreceiptdate"].ToString());
                    if (suborder.HasValue("bsd_confirmedreceiptdate"))
                        bsd_confirmedreceiptdate = DateTime.Parse(suborder["bsd_confirmedreceiptdate"].ToString());
                    if (suborder.HasValue("bsd_shiptoaccount"))
                    {
                        //bsd_shiptoaccount = suborder["bsd_shiptoaccount"].ToString();
                        EntityReference rf_Entity = (EntityReference)suborder["bsd_shiptoaccount"];
                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                        if (Entity.HasValue("accountnumber"))
                            bsd_shiptoaccount = Entity["accountnumber"].ToString();
                        if (Entity.HasValue("name"))
                            bsd_shiptoaccountname = Entity["name"].ToString();

                    }
                    if (suborder.HasValue("bsd_site"))
                    {
                        //bsd_shiptoaccount = suborder["bsd_shiptoaccount"].ToString();
                        EntityReference rf_Entity = (EntityReference)suborder["bsd_site"];
                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                        if (Entity.HasValue("bsd_code"))
                            bsd_site = Entity["bsd_code"].ToString();

                    }
                    if (suborder.HasValue("bsd_shiptoaddress"))
                    {
                        // bsd_shiptoaddress = suborder["bsd_shiptoaddress"].ToString();
                        EntityReference rf_addressinvoiceaccount = (EntityReference)suborder["bsd_shiptoaddress"];
                        Entity Address = service.Retrieve(rf_addressinvoiceaccount.LogicalName, rf_addressinvoiceaccount.Id, new ColumnSet(true));
                        if (Address.HasValue("bsd_name"))
                            bsd_shiptoaddress = Address["bsd_name"].ToString();
                    }

                    //End
                    #endregion
                    // string s_ResultSalesTable = client.SalesTableInAX(context, bsd_name, bsd_invoiceaccount, bsd_customercode, bsd_addressinvoiceaccount, bsd_description, bsd_paymentmethod, bsd_paymentterm, "typeofpayment", bsd_currencydefault, bsd_bankaccount, bsd_customercode, bsd_requestedshipdate, bsd_requestedreceiptdate, ShippingDateConfirmed, bsd_confirmedreceiptdate, bsd_description, bsd_customerpo, bsd_status, bsd_shiptoaccount, bsd_shiptoaddress, "SL0001", bsd_shiptoaccount, bsd_site,"");
                    #region set Data Dimensions
                    if (suborder.HasValue("bsd_order"))
                    {
                        EntityReference rf_Entity = (EntityReference)suborder["bsd_order"];
                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                        if (Entity.HasValue("ordernumber"))
                        {
                            string DimensionAtt = "BH_SalesAgreement";
                            FinancialDimensionServices.DimensionValueContract contract = new FinancialDimensionServices.DimensionValueContract();
                            contract.parmDimensionAttribute = DimensionAtt;
                            contract.parmValue = Entity["ordernumber"].ToString();
                            if (Entity.HasValue("bsd_ordername"))
                                contract.parmDescription = Entity["bsd_ordername"].ToString();
                            if (Entity.HasValue("bsd_date"))
                                contract.parmActiveFrom = DateTime.Parse(Entity["bsd_date"].ToString());
                            contract.parmActiveTo = new DateTime(1900, 1, 1);
                            // contract.parmActiveTo = DateTime.Now.AddDays(3);
                            //
                            proxyDimension.createDimensionValue(contextDimension, contract);

                            dimAttValue.Name = DimensionAtt;
                            dimAttValue.Value = Entity["ordernumber"].ToString();
                            dimAttValueSet.Values = new AxdType_DimensionAttributeValue[1] { dimAttValue };
                            dimAttValueReturnOrder.Name = DimensionAtt;
                            dimAttValueReturnOrder.Value = Entity["ordernumber"].ToString();
                            dimAttValueSetReturnOrder.Values = new ReturnOrderService.AxdType_DimensionAttributeValue[1] { dimAttValueReturnOrder };


                        }
                    }
                    #endregion

                    #region check ShipAddress
                    EntityReference shipAddress;
                    if (suborder.HasValue("bsd_shiptoaddress"))
                    {
                        // bsd_shiptoaddress = suborder["bsd_shiptoaddress"].ToString();

                        shipAddress = (EntityReference)suborder["bsd_shiptoaddress"];
                        bsd_shiptoaddressid = shipAddress.Id.ToString();
                        string checkAddress = client.BHS_ValidateAddressAccount(context, bsd_shiptoaccount, shipAddress.Id.ToString());
                        if (checkAddress == "false") throw new Exception("Ship Address does not exist in AX");

                    }
                    // throw new Exception("okie");
                    #endregion
                    //throw new Exception(((OptionSetValue) suborder["bsd_type"]).Value.ToString() +"dd"+(((OptionSetValue)suborder["bsd_type"]).Value == 861450004).ToString());
                    // salesTable.SalesId = "DH1708-2299";
                    if (((OptionSetValue)suborder["bsd_type"]).Value == 861450004) //861,450,004
                    {
                        //   throw new Exception("okie2");
                        #region Transfer ReturnOrder 
                        if (suborder.HasValue("bsd_returnorder"))
                        {
                            #region get SalesOrder in AX
                            EntityReference rf_Entity = (EntityReference)suborder["bsd_returnorder"];
                            ReturnOrderService.AxdReturnOrderIn returnOrderClass = new ReturnOrderService.AxdReturnOrderIn();
                            ReturnOrderService.AxdEntity_SalesTable returnOrder = new ReturnOrderService.AxdEntity_SalesTable();
                            Entity entityReturnOrder = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                            if (entityReturnOrder.HasValue("bsd_findsuborder"))
                            {
                                DateTime bsd_date = new DateTime();
                                string bsd_returnreasoncode = "";//lookup
                                if (entityReturnOrder.HasValue("bsd_date"))
                                    bsd_date = (DateTime)entityReturnOrder["bsd_date"];
                                // throw new Exception(bsd_date.AddHours(7).ToString());
                                if (entityReturnOrder.HasValue("bsd_returnreasoncode"))
                                {
                                    rf_Entity = (EntityReference)entityReturnOrder["bsd_returnreasoncode"];
                                    Entity Entityreturnreasoncode = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                    if (Entityreturnreasoncode.HasValue("bsd_code"))
                                        bsd_returnreasoncode = Entityreturnreasoncode["bsd_code"].ToString();
                                }
                                rf_Entity = (EntityReference)entityReturnOrder["bsd_findsuborder"];
                                Entity entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_suborderax"));
                                if (entity.HasValue("bsd_suborderax"))
                                {
                                    //  throw new Exception("okie11");
                                    SalesOrderService.KeyField keyField = new SalesOrderService.KeyField() { Field = "SalesId", Value = entity["bsd_suborderax"].ToString().Trim() };
                                    SalesOrderService.EntityKey entityKey = new SalesOrderService.EntityKey();
                                    entityKey.KeyData = new SalesOrderService.KeyField[1] { keyField };
                                    SalesOrderService.AxdEntity_SalesTable _SalesOrderTable;
                                    // SalesOrderService.AxdEntity_SalesLine SalesLine;
                                    SalesOrderService.AxdSalesOrder _SalesOrderList;
                                    SalesOrderService.EntityKey[] entityKeys = new SalesOrderService.EntityKey[1] { entityKey };

                                    try
                                    {
                                        _SalesOrderList = proxy.read(contextSaleOrder, entityKeys);
                                        _SalesOrderTable = _SalesOrderList.SalesTable.First();
                                        #region set Value Return Order khi Sales Order được tìm thấy ở AX

                                        returnOrder = mapSalesTable(_SalesOrderTable, bsd_returnreasoncode, bsd_date.AddHours(7));
                                        if (suborder.HasValue("bsd_order"))
                                        {
                                            returnOrder.DefaultDimension = dimAttValueSetReturnOrder;
                                        }

                                        #endregion
                                    }
                                    catch (Exception ex)
                                    {
                                        // throw new Exception(ex.Message);

                                        returnOrder = setSalesTableReturnOrder(bsd_currencydefault, bsd_customercode, bsd_invoiceaccount, bsd_invoicenameaccount, bsd_paymentterm, bsd_paymentmethod, bsd_shiptoaccountname, bsd_returnreasoncode, bsd_date, bsd_site);

                                        if (suborder.HasValue("bsd_order"))
                                        {
                                            returnOrder.DefaultDimension = dimAttValueSetReturnOrder;
                                        }
                                    }

                                    #region set List Product 
                                    EntityCollection lstSubOrderProduct = getSubOrderProductB2C(target.Id.ToString());
                                    if (lstSubOrderProduct.Entities.Any())
                                    {
                                        int i = 0;
                                        ReturnOrderService.AxdEntity_SalesLine[] lstreturnSalesLine = new ReturnOrderService.AxdEntity_SalesLine[lstSubOrderProduct.Entities.Count];
                                        #region list Product
                                        foreach (Entity SubOrderProduct in lstSubOrderProduct.Entities)
                                        {

                                            string bsd_productid = ""; decimal bsd_shipquantity = 0m; string bsd_unit = ""; decimal bsd_priceperunit = 0m; decimal bsd_amount = 0m; decimal bsd_discount = 0m; decimal bsd_discountpercent = 0m;
                                            Entity b2c_SubOrderProduct = service.Retrieve(SubOrderProduct.LogicalName, SubOrderProduct.Id, new ColumnSet(true));
                                            if (b2c_SubOrderProduct.HasValue("bsd_product"))
                                            {
                                                EntityReference rf_Entity_product = (EntityReference)b2c_SubOrderProduct["bsd_product"];
                                                Entity Product = service.Retrieve(rf_Entity_product.LogicalName, rf_Entity_product.Id, new ColumnSet("bsd_itemsalestaxgroup"));
                                                if (Product.HasValue("bsd_itemsalestaxgroup"))
                                                {
                                                    rf_Entity = (EntityReference)Product["bsd_itemsalestaxgroup"];
                                                    Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                                    if (Entity.HasValue("bsd_code"))
                                                        bsd_itemsalestaxgroup = Entity["bsd_code"].ToString();
                                                }

                                            }
                                            if (b2c_SubOrderProduct.HasValue("bsd_productid"))
                                                bsd_productid = b2c_SubOrderProduct["bsd_productid"].ToString();
                                            if (b2c_SubOrderProduct.HasValue("bsd_shipquantity"))
                                                bsd_shipquantity = decimal.Parse(b2c_SubOrderProduct["bsd_shipquantity"].ToString());
                                            if (b2c_SubOrderProduct.HasValue("bsd_unit"))
                                            {
                                                // bsd_shiptoaddress = suborder["bsd_shiptoaddress"].ToString();
                                                EntityReference rf_addressinvoiceaccount = (EntityReference)b2c_SubOrderProduct["bsd_unit"];
                                                Entity Entity = service.Retrieve(rf_addressinvoiceaccount.LogicalName, rf_addressinvoiceaccount.Id, new ColumnSet("name"));
                                                if (Entity.HasValue("name"))
                                                    bsd_unit = Entity["name"].ToString();
                                            }
                                            if (b2c_SubOrderProduct.HasValue("bsd_priceperunit"))
                                                bsd_priceperunit = ((Money)b2c_SubOrderProduct["bsd_priceperunit"]).Value;
                                            if (b2c_SubOrderProduct.HasValue("bsd_amount"))
                                                bsd_amount = ((Money)b2c_SubOrderProduct["bsd_amount"]).Value;
                                            string result = client.checkSalesTable(context, bsd_name, bsd_invoiceaccount, bsd_customercode, bsd_shiptoaccount, bsd_paymentterm, bsd_paymentmethod, bsd_currencydefault, bsd_productid, bsd_site, "", bsd_customerpo, bsd_saletaxgroup, bsd_itemsalestaxgroup, bsd_returnreasoncode);
                                            if (result != "0") throw new Exception(result);
                                            ReturnOrderService.AxdEntity_SalesLine salesLine = new ReturnOrderService.AxdEntity_SalesLine();
                                            salesLine = setSalesLineReturnOrder(bsd_productid, bsd_shipquantity, bsd_unit, bsd_priceperunit, bsd_amount, bsd_saletaxgroup, bsd_itemsalestaxgroup, bsd_site);
                                            // inventDim.InventLocationId = "KD01";
                                            Product product = new Product(bsd_productid, bsd_priceperunit, bsd_amount);
                                            lstProduct.Add(product);
                                            lstreturnSalesLine[i] = salesLine;
                                            i++;
                                        }
                                        #endregion
                                        returnOrder.SalesLine = lstreturnSalesLine;
                                    }

                                    #endregion
                                    ReturnOrderService.AxdEntity_SalesTable[] lstSalesTable = new ReturnOrderService.AxdEntity_SalesTable[1] { returnOrder };
                                    returnOrderClass.SalesTable = lstSalesTable;
                                    #region Create SalesOrder AX
                                    ReturnOrderService.EntityKey[] returnedSalesOrderEntityKey = proxyReturnOrder.create(contextReturnOrder, returnOrderClass);
                                    ReturnOrderService.EntityKey returnedSalesOrder = (ReturnOrderService.EntityKey)returnedSalesOrderEntityKey.GetValue(0);
                                    try
                                    {
                                        client.UpdateAddressSalesOrder(context, bsd_shiptoaddressid, returnedSalesOrder.KeyData[0].Value, bsd_date.AddHours(7), true);
                                        foreach (Product productUp in lstProduct)
                                        {
                                            // throw new Exception(productUp.SalesUnit.ToString() +"---"+ productUp.NetAmount.ToString());
                                            client.UpdateSalesPrice(context, productUp.ItemId, returnedSalesOrder.KeyData[0].Value, productUp.SalesUnit, productUp.NetAmount, 0, 0);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Delele Sales Order
                                        proxyReturnOrder.delete(contextReturnOrder, returnedSalesOrderEntityKey);
                                        throw new Exception("Update error: " + ex.Message);
                                    }
                                    Entity subOrderUpd = new Entity(target.LogicalName, target.Id);
                                    subOrderUpd["bsd_suborderax"] = returnedSalesOrder.KeyData[0].Value.ToString();
                                    service.Update(subOrderUpd);
                                    ReturnIdAX = returnedSalesOrder.KeyData[0].Value.ToString();
                                    #endregion


                                }
                            }
                            else
                            {
                                throw new Exception("Return order does not found base Suborder");
                            }
                            #endregion
                        }
                        else
                        {
                            throw new Exception("return Order B2C Transfer");
                        }
                        #endregion
                    }
                    else
                    {
                        // throw new Exception("okie3");
                        #region         Trasfer suborder                                                                  
                        //  Guid suborderB2b_Id = Guid.NewGuid();

                        #region set Data Transfer AX Suborder Product
                        salesTable = setSalesTableSalesOrder(bsd_currencydefault, bsd_customercode, bsd_invoiceaccount, bsd_invoicenameaccount, bsd_paymentterm, bsd_paymentmethod, bsd_shiptoaccountname, ShippingDateConfirmed, bsd_requestedshipdate, bsd_confirmedreceiptdate, bsd_requestedreceiptdate, bsd_site);
                        EntityCollection lstSubOrderProduct = getSubOrderProductB2C(target.Id.ToString());
                        if (lstSubOrderProduct.Entities.Any())
                        {
                            int i = 0;
                            AxdEntity_SalesLine[] lstLine = new AxdEntity_SalesLine[lstSubOrderProduct.Entities.Count];
                            #region set List Product
                            foreach (Entity SubOrderProduct in lstSubOrderProduct.Entities)
                            {

                                string bsd_productid = "";
                                decimal bsd_shipquantity = 0m;
                                string bsd_unit = "";
                                decimal bsd_priceperunit = 0m;
                                decimal bsd_amount = 0m;
                                decimal bsd_discount = 0m;
                                decimal bsd_discountpercent = 0m;
                                Entity b2c_SubOrderProduct = service.Retrieve(SubOrderProduct.LogicalName, SubOrderProduct.Id, new ColumnSet(true));
                                if (b2c_SubOrderProduct.HasValue("bsd_product"))
                                {
                                    EntityReference rf_Entity_product = (EntityReference)b2c_SubOrderProduct["bsd_product"];
                                    Entity Product = service.Retrieve(rf_Entity_product.LogicalName, rf_Entity_product.Id, new ColumnSet("bsd_itemsalestaxgroup"));
                                    if (Product.HasValue("bsd_itemsalestaxgroup"))
                                    {
                                        EntityReference rf_Entity = (EntityReference)Product["bsd_itemsalestaxgroup"];
                                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                        if (Entity.HasValue("bsd_code"))
                                        {
                                            bsd_itemsalestaxgroup = Entity["bsd_code"].ToString();

                                        }
                                    }

                                }
                                if (b2c_SubOrderProduct.HasValue("bsd_productid"))
                                    bsd_productid = b2c_SubOrderProduct["bsd_productid"].ToString();
                                if (b2c_SubOrderProduct.HasValue("bsd_shipquantity"))
                                    bsd_shipquantity = decimal.Parse(b2c_SubOrderProduct["bsd_shipquantity"].ToString());
                                if (b2c_SubOrderProduct.HasValue("bsd_unit"))
                                {
                                    // bsd_shiptoaddress = suborder["bsd_shiptoaddress"].ToString();
                                    EntityReference rf_addressinvoiceaccount = (EntityReference)b2c_SubOrderProduct["bsd_unit"];
                                    Entity Entity = service.Retrieve(rf_addressinvoiceaccount.LogicalName, rf_addressinvoiceaccount.Id, new ColumnSet("name"));
                                    if (Entity.HasValue("name"))
                                        bsd_unit = Entity["name"].ToString();
                                }
                                if (b2c_SubOrderProduct.HasValue("bsd_priceperunit"))
                                    bsd_priceperunit = ((Money)b2c_SubOrderProduct["bsd_priceperunit"]).Value;
                                if (b2c_SubOrderProduct.HasValue("bsd_amount"))
                                    bsd_amount = ((Money)b2c_SubOrderProduct["bsd_amount"]).Value;
                                string result = client.checkSalesTable(context, bsd_name, bsd_invoiceaccount, bsd_customercode, bsd_shiptoaccount, bsd_paymentterm, bsd_paymentmethod, bsd_currencydefault, bsd_productid, bsd_site, "", bsd_customerpo, bsd_saletaxgroup, bsd_itemsalestaxgroup, "");
                                if (result != "0") throw new Exception(result);

                                // 
                                AxdEntity_SalesLine salesLine = new AxdEntity_SalesLine();
                                salesLine = setSalesLineSalesOrder(bsd_productid, bsd_shipquantity, bsd_unit, bsd_priceperunit, bsd_amount, bsd_saletaxgroup, bsd_itemsalestaxgroup, bsd_site);
                                Product product = new Product(bsd_productid, bsd_priceperunit, bsd_amount);
                                lstProduct.Add(product);
                                lstLine[i] = salesLine;
                                i++;
                            }
                            #endregion
                            salesTable.SalesLine = lstLine;
                        }
                        if (suborder.HasValue("bsd_order"))
                        {
                            salesTable.DefaultDimension = dimAttValueSet;
                        }
                        #endregion

                        #region Create SalesOrder AX

                        // throw new Exception("salesTable" + salesTable.SalesLine.Count());
                        salesOrder.SalesTable = new AxdEntity_SalesTable[1] { salesTable };
                        // Call the create method on the service passing in the document.
                        EntityKey[] returnedSalesOrderEntityKey = proxy.create(contextSaleOrder, salesOrder);
                        // throw new Exception("okie");
                        // The create method returns an EntityKey which contains the ID of the sales order.
                        EntityKey returnedSalesOrder = (EntityKey)returnedSalesOrderEntityKey.GetValue(0);
                        try
                        {
                            client.UpdateAddressSalesOrder(context, bsd_shiptoaddressid, returnedSalesOrder.KeyData[0].Value, DateTime.Now, false);
                            foreach (Product productUp in lstProduct)
                            {
                                // throw new Exception(productUp.SalesUnit.ToString() +"---"+ productUp.NetAmount.ToString());
                                client.UpdateSalesPrice(context, productUp.ItemId, returnedSalesOrder.KeyData[0].Value, productUp.SalesUnit, productUp.NetAmount, 0, 0);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Delele Sales Order
                            proxy.delete(contextSaleOrder, returnedSalesOrderEntityKey);
                            throw new Exception("Update error: " + ex.Message);
                        }
                        Entity subOrderUpd = new Entity(target.LogicalName, target.Id);
                        subOrderUpd["bsd_suborderax"] = returnedSalesOrder.KeyData[0].Value.ToString();
                        service.Update(subOrderUpd);
                        ReturnIdAX = returnedSalesOrder.KeyData[0].Value.ToString();
                        // throw new Exception("The sales order created has a Sales ID of " + returnedSalesOrder.KeyData[0].Value);
                        #endregion
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                    // Console.WriteLine("ex :" + ex.Message);
                }

                //end Connector AX
                #endregion
            }
            else if (context.MessageName == "bsd_TransferAX_RequestDelivery")
            {
                #region Connector AX Request Delivery
                //Start Connector AX Request Delivery
                List<string> lstFlat = new List<string>();
                List<string> lstFlatProduct;
                string Site = "";
                string lstProduct = "";
                string lstProdduct_total_quantity = "";
                try
                {
                    string SubOrderAXNumber = "";
                    EntityReference target = (EntityReference)context.InputParameters["Target"];

                    try
                    {
                        Entity RequestDelivery = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        #region get Value Transfer AX
                        if (RequestDelivery.HasValue("bsd_deliveryplan"))
                        {

                            #region get SubOrderAXNumber 
                            EntityReference rf_Entity = (EntityReference)RequestDelivery["bsd_deliveryplan"];
                            Entity DeliveryPlan = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_suborder"));
                            if (DeliveryPlan.HasValue("bsd_suborder"))
                            {

                                rf_Entity = (EntityReference)DeliveryPlan["bsd_suborder"];
                                Entity SubOrder = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                                if (SubOrder.HasValue("bsd_suborderax"))
                                {
                                    SubOrderAXNumber = SubOrder["bsd_suborderax"].ToString();
                                }
                                else
                                {
                                    throw new Exception("Suborder does not remember Sales Order in AX");
                                }
                                if (SubOrder.HasValue("bsd_site"))
                                {
                                    rf_Entity = (EntityReference)SubOrder["bsd_site"];
                                    Entity Entity_site = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                    if (Entity_site.HasValue("bsd_code"))
                                        Site = Entity_site["bsd_code"].ToString();
                                    else throw new Exception("Site SubOrder does not Exist");
                                }
                            }
                            else
                            {
                                throw new Exception("Delivery Schedule does not remember SubOrder");
                            }

                            #endregion
                            #region Khai báo Service COnnetor AX
                            NetTcpBinding binding = new NetTcpBinding();
                            //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                            binding.Name = "NetTcpBinding_BHS_BSD_CRMSERVICEAXService";
                            EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://"+_port+"/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));
                            ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                            client.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                            client.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                            client.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                            ServiceReferenceAIF.CallContext context = new ServiceReferenceAIF.CallContext() { Company = _company };

                            #endregion
                            #region get Lines
                            EntityCollection lst_Entity = getRequestDeliveryProduct(RequestDelivery.Id);
                            //  int icount = 0;
                            //string s = "";
                            if (lst_Entity.Entities.Any())
                            {
                                foreach (Entity item in lst_Entity.Entities)
                                {
                                    Entity RequestDeliveryProduct = service.Retrieve(item.LogicalName, item.Id, new ColumnSet(true));
                                    if (RequestDeliveryProduct.HasValue("bsd_product"))
                                    {
                                        rf_Entity = (EntityReference)RequestDeliveryProduct["bsd_product"];
                                        if (!lstFlat.Any() || !lstFlat.Any(rf_Entity.Id.ToString().Contains))
                                        {
                                            lstFlat.Add(rf_Entity.Id.ToString());
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Can not transfer AX over becaus product not found");
                                    }
                                }
                                //  throw new Exception("lstFlat " + lstFlat.Count);
                                lstProduct = "";
                                int k = 0, j = 0;
                                foreach (var flatId in lstFlat)
                                {
                                    Entity Product = service.Retrieve("product", Guid.Parse(flatId), new ColumnSet("productnumber"));
                                    int i = 1;
                                    decimal sum_quantity_product = 0m;
                                    foreach (Entity item in lst_Entity.Entities)
                                    {

                                        Entity RequestDeliveryProduct = service.Retrieve(item.LogicalName, item.Id, new ColumnSet(true));
                                        rf_Entity = (EntityReference)RequestDeliveryProduct["bsd_warehouse"];
                                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_warehouseid"));
                                        EntityReference rf_Entity_product = (EntityReference)RequestDeliveryProduct["bsd_product"];

                                        if (rf_Entity_product.Id == Guid.Parse(flatId))
                                        {
                                            if (k == 0)
                                                lstProduct = RequestDeliveryProduct["bsd_productid"].ToString().Trim() + ":" + Site + ":" + Entity["bsd_warehouseid"].ToString().Trim() + ":" + RequestDeliveryProduct["bsd_quantity"].ToString().Trim() + ":" + i;
                                            else
                                                lstProduct += ";" + RequestDeliveryProduct["bsd_productid"].ToString().Trim() + ":" + Site + ":" + Entity["bsd_warehouseid"].ToString().Trim() + ":" + RequestDeliveryProduct["bsd_quantity"].ToString().Trim() + ":" + i;
                                            i++;
                                            k++;
                                            sum_quantity_product += (decimal)RequestDeliveryProduct["bsd_quantity"];
                                        }

                                    }
                                    if (j == 0)
                                        lstProdduct_total_quantity = Product["productnumber"].ToString().Trim() + ":" + sum_quantity_product.ToString();
                                    else
                                        lstProdduct_total_quantity += ";" + Product["productnumber"].ToString().Trim() + ":" + sum_quantity_product.ToString();
                                    j++;

                                }
                                if (!String.IsNullOrEmpty(lstProduct) && !String.IsNullOrEmpty(lstProdduct_total_quantity))
                                {
                                   throw new Exception("Suborder: " + SubOrderAXNumber + "Sales Line: " + lstProduct+ "Sales Line total:"+ lstProdduct_total_quantity);
                                    string s_Result = client.CreatePickingList(context, SubOrderAXNumber, lstProduct, lstProdduct_total_quantity);
                                    if (s_Result.Contains("true"))
                                    {
                                        RequestDelivery = service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_pickinglistax"));
                                        Entity entity = new Entity(RequestDelivery.LogicalName, RequestDelivery.Id);
                                        entity["bsd_pickinglistax"] = s_Result.Replace("true", "").Trim();
                                        service.Update(entity);
                                        ReturnIdAX = s_Result.Replace("true", "").Trim();
                                    }
                                    else
                                    {
                                        throw new Exception(s_Result);
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("Delivery Schedule does not exist list product");
                            }
                            #endregion

                        }
                        #endregion
                        else
                        {
                            throw new Exception("Request delivery does not remember Delivery Schedule");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }

                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                //end Connector AX Request Delivery
                #endregion
            }
            context.OutputParameters["ReturnId"] = ReturnIdAX.ToString();
        }
        public EntityCollection getSubOrderProductB2C(string subOrderId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_suborderproduct'>
                         <all-attributes />
                        <filter type='and'>
                          <condition attribute='bsd_suborder' operator='eq'  uitype='bsd_suborder' value='" + subOrderId + @"' />
                          <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                      </entity>
                    </fetch>";
            EntityCollection list_bhstradingAccountb2b = service.RetrieveMultiple(new FetchExpression(xml));
            return list_bhstradingAccountb2b;
        }
        public EntityCollection getRequestDeliveryProduct(Guid RequestDeliveryId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_requestdeliveryproduct'>
                               <all-attributes />
                                <filter type='and'>
                                  <condition attribute='bsd_requestdelivery' operator='eq'  uitype='bsd_requestdelivery' value='" + RequestDeliveryId + @"' />
                                    <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return lst;
        }
        public AxdEntity_SalesTable setSalesTableSalesOrder(string bsd_currencydefault, string bsd_customercode, string bsd_invoiceaccount, string bsd_name,
            string bsd_paymentterm, string bsd_paymentmethod,
          string bsd_shiptoaccountname, DateTime ShippingDateConfirmed, DateTime bsd_requestedshipdate, DateTime bsd_confirmedreceiptdate, DateTime bsd_requestedreceiptdate, string bsd_site)
        {
            AxdEntity_SalesTable salesTable = new AxdEntity_SalesTable();
            salesTable.CurrencyCode = bsd_currencydefault;
            salesTable.CustAccount = bsd_customercode;
            salesTable.InvoiceAccount = bsd_invoiceaccount;
            //salesTable.CustAccount = "A00002";
            // salesTable.InvoiceAccount = "A00002";
            salesTable.LanguageId = "en-us";
            salesTable.SalesName = bsd_name;
            salesTable.Payment = bsd_paymentterm;
            salesTable.PaymMode = bsd_paymentmethod;
            //  salesTable.DeliveryName = bsd_shiptoaccount;
            salesTable.DeliveryName = bsd_shiptoaccountname;
            salesTable.SalesStatus = AxdEnum_SalesStatus.Delivered;
            salesTable.ShippingDateConfirmed = bsd_requestedshipdate.AddHours(7);
            salesTable.ShippingDateRequested = bsd_requestedshipdate.AddHours(7);
            salesTable.ReceiptDateConfirmed = bsd_confirmedreceiptdate.AddHours(7);
            salesTable.ReceiptDateRequested = bsd_requestedreceiptdate.AddHours(7);
            salesTable.SalesType = AxdEnum_SalesType.Sales;
            //throw new Exception("SalesType: "+ salesTable.SalesType);
            salesTable.InventSiteId = bsd_site;
            salesTable.CustGroup = "OV";
            salesTable.QuotationId = "";
            Random rnd = new Random();
            salesTable.PurchOrderFormNum = rnd.Next(100000).ToString();
            return salesTable;
        }
        public ReturnOrderService.AxdEntity_SalesTable setSalesTableReturnOrder(string bsd_currencydefault, string bsd_customercode, string bsd_invoiceaccount, string bsd_name,
           string bsd_paymentterm, string bsd_paymentmethod,
         string bsd_shiptoaccountname, string bsd_returnreasoncode, DateTime bsd_date, string bsd_site)
        {
            ReturnOrderService.AxdEntity_SalesTable salesTable = new ReturnOrderService.AxdEntity_SalesTable();
            salesTable.CurrencyCode = bsd_currencydefault;
            salesTable.CustAccount = bsd_customercode;
            salesTable.InvoiceAccount = bsd_invoiceaccount;
            //salesTable.CustAccount = "A00002";
            // salesTable.InvoiceAccount = "A00002";
            salesTable.LanguageId = "en-us";
            salesTable.SalesName = bsd_name;
            salesTable.Payment = bsd_paymentterm;
            salesTable.PaymMode = bsd_paymentmethod;
            //  salesTable.DeliveryName = bsd_shiptoaccount;
            salesTable.DeliveryName = bsd_shiptoaccountname;
            salesTable.InventSiteId = bsd_site;
            salesTable.ReturnReasonCodeId = bsd_returnreasoncode;
            salesTable.ReturnDeadline = bsd_date;
            salesTable.CustGroup = "OV";
            salesTable.QuotationId = "";
            return salesTable;
        }
        public AxdEntity_SalesLine setSalesLineSalesOrder(string bsd_productid, decimal bsd_shipquantity, string bsd_unit, decimal bsd_priceperunit, decimal bsd_amount, string bsd_saletaxgroup, string bsd_itemsalestaxgroup, string bsd_site)
        {
            AxdEntity_SalesLine salesLine = new AxdEntity_SalesLine();
            salesLine.ItemId = bsd_productid;
            salesLine.SalesQty = bsd_shipquantity;
            salesLine.SalesUnit = bsd_unit;
            salesLine.SalesPrice = bsd_priceperunit;
            salesLine.LineAmount = bsd_amount;
            //  throw new Exception("okie"+ bsd_saletaxgroup +"-"+ bsd_itemsalestaxgroup);
            salesLine.TaxGroup = bsd_saletaxgroup;
            salesLine.TaxItemGroup = bsd_itemsalestaxgroup;
            AxdEntity_InventDim inventDim = new AxdEntity_InventDim();
            inventDim.InventColorId = "01";
            inventDim.InventSiteId = bsd_site;
            salesLine.InventDim = new AxdEntity_InventDim[1] { inventDim };
            return salesLine;
        }
        public ReturnOrderService.AxdEntity_SalesTable mapSalesTable(AxdEntity_SalesTable _SalesOrderTable, string bsd_returnreasoncode, DateTime bsd_date)
        {
            ReturnOrderService.AxdEntity_SalesTable returnOrder = new ReturnOrderService.AxdEntity_SalesTable();
            returnOrder.CurrencyCode = _SalesOrderTable.CurrencyCode;
            returnOrder.CustAccount = _SalesOrderTable.CustAccount;
            returnOrder.InvoiceAccount = _SalesOrderTable.InvoiceAccount;
            returnOrder.LanguageId = _SalesOrderTable.LanguageId;
            returnOrder.Payment = _SalesOrderTable.Payment;
            returnOrder.SalesName = _SalesOrderTable.SalesName;
            //returnOrder.ReturnReasonCodeId = "Credit";
            returnOrder.ReturnReasonCodeId = bsd_returnreasoncode;
            returnOrder.ReturnDeadline = bsd_date;
            returnOrder.InventSiteId = _SalesOrderTable.InventSiteId;
            returnOrder.CustGroup = _SalesOrderTable.CustGroup;
            returnOrder.QuotationId = _SalesOrderTable.QuotationId;
            return returnOrder;
        }
        public ReturnOrderService.AxdEntity_SalesLine setSalesLineReturnOrder(string bsd_productid, decimal bsd_shipquantity, string bsd_unit, decimal bsd_priceperunit, decimal bsd_amount, string bsd_saletaxgroup, string bsd_itemsalestaxgroup, string bsd_site)
        {
            ReturnOrderService.AxdEntity_SalesLine salesLine = new ReturnOrderService.AxdEntity_SalesLine();
            salesLine.ItemId = bsd_productid;
            // salesLine.SalesQty = bsd_shipquantity;
            salesLine.SalesUnit = bsd_unit;
            salesLine.SalesPrice = bsd_priceperunit;
            salesLine.LineAmount = bsd_amount;
            salesLine.ExpectedRetQty = bsd_shipquantity;
            salesLine.ExpectedRetQtySpecified = true;
            //  throw new Exception("okie"+ bsd_saletaxgroup +"-"+ bsd_itemsalestaxgroup);
            salesLine.TaxGroup = bsd_saletaxgroup;
            salesLine.TaxItemGroup = bsd_itemsalestaxgroup;
            ReturnOrderService.AxdEntity_InventDim inventDim = new ReturnOrderService.AxdEntity_InventDim();
            inventDim.InventColorId = "01";
            inventDim.InventSiteId = bsd_site;
            salesLine.InventDim = new ReturnOrderService.AxdEntity_InventDim[1] { inventDim };
            return salesLine;
        }
    }
    public class Product
    {
        public string ItemId { get; set; }
        public decimal SalesUnit { get; set; }
        public decimal NetAmount { get; set; }

        public Product(string _itemId, decimal _salesUnit, decimal _netAmount)
        {
            ItemId = _itemId;
            SalesUnit = _salesUnit;
            NetAmount = _netAmount;
        }
    }
}
