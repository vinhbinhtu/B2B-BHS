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
        public string _userName = "";
        public string _passWord = "";
        public string _company = "";
        public string _port = "";
        public string _domain = "";
        public void Execute(IServiceProvider serviceProvider)
        {
            string ReturnIdAX = "";
            //string _userName = "crm21";
            //string _passWord = "bsd@123";
            //string _company = "BHS";
            //string _port = "192.168.68.31:8201";
            //string _domain = "BSD.LOCAL";
            //string _userName = Utilites._userName;
            //string _passWord = Utilites._passWord;
            //string _company = Utilites._company;
            //string _port = Utilites._port;
            //string _domain = Utilites._domain;
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);
            string trace = "0";
            try
            {
                if (context.MessageName == "bsd_TransferAX")
                {
                    bool connect_ax = getConfigdefaultconnectax();
                    if (connect_ax == false)
                    {
                        throw new Exception("Thiếu khai báo connect ax");
                    }
                    //  throw new Exception("okie");
                    #region Connector AX Suborder
                    //Connector AX
                    EntityReference target = (EntityReference)context.InputParameters["Target"];
                    try
                    {
                        List<Product> lstProduct = new List<Product>();
                        DateTime bsd_duedate = DateTime.Now;
                        bool bsd_typeorder = false;
                        // string bsd_customerpo = "";
                        decimal bsd_exchangeratevalue = 1m;
                        decimal bsd_totalcurrencyexchange = 0m;
                        decimal bsd_totalamount = 0m;
                        string bsd_economiccontractno = "";
                        string bsd_potentialcustomer = "";//lookup
                        Guid bsd_potentialcustomerid=Guid.Empty;
                        string bsd_accountgroup = "OV";
                        string DimensionAtt = "BH_SalesAgreement";
                        string DimensionAttCustomer = "BH_Customer";
                        string RMA_Number = "";
                        string bsd_addressinvoiceaccount_Guid = "";
                        string bsd_type = "SalesOrder";
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
                        Random rnd = new Random();
                        string bsd_customerpo = "CRMAX";
                        string bsd_site = "BHS";
                        string bsd_saletaxgroup = "";
                        string bsd_itemsalestaxgroup = "NON-VAT";
                        string bsd_importdeclaration = "";
                        AxdType_DimensionAttributeValueSet dimAttValueSet = new AxdType_DimensionAttributeValueSet();
                        AxdType_DimensionAttributeValue dimAttValue = new AxdType_DimensionAttributeValue();
                        AxdType_DimensionAttributeValue dimAttValueCustomer = new AxdType_DimensionAttributeValue();
                        //ReturnOrderService.AxdType_DimensionAttributeValueSet dimAttValueSetReturnOrder = new ReturnOrderService.AxdType_DimensionAttributeValueSet();
                        //ReturnOrderService.AxdType_DimensionAttributeValue dimAttValueReturnOrder = new ReturnOrderService.AxdType_DimensionAttributeValue();
                        //ReturnOrderService.AxdType_DimensionAttributeValue dimAttValueReturnOrderCustomer = new ReturnOrderService.AxdType_DimensionAttributeValue();
                        //ReturnOrderService.AxdType_DimensionAttributeValue[] lstDemesionReturnOrder = new ReturnOrderService.AxdType_DimensionAttributeValue[2];
                        AxdType_DimensionAttributeValue[] lstDemesion = new AxdType_DimensionAttributeValue[2];
                        DateTime bsd_requestedshipdate = DateTime.Now;
                        DateTime bsd_requestedreceiptdate = DateTime.Now;
                        DateTime bsd_confirmedreceiptdate = DateTime.Now;
                        DateTime ShippingDateConfirmed = DateTime.Now;//Chưa có
                        Entity suborder = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));// lấy suborder của b2c
                        #region KHai bao service AX
                        NetTcpBinding binding = new NetTcpBinding();
                        //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                        binding.Name = "NetTcpBinding_BHS_BSD_CRMSERVICEAXService";
                        EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));
                        ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                        client.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                        client.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                        client.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                        ServiceReferenceAIF.CallContext context = new ServiceReferenceAIF.CallContext() { Company = _company };
                        //throw new Exception("okie");
                        //
                        //string s = client.TestMethod(context);
                        //throw new Exception(s);
                        //
                        //throw new Exception("okie12");
                        NetTcpBinding bindingSaleOrder = new NetTcpBinding();
                        //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF// SalesSalesOrderService_Group
                        //  bindingSaleOrder.Name = "NetTcpBinding_SalesOrderService";
                        //EndpointAddress endpointSaleOrder = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/SalesOrderService"));
                        bindingSaleOrder.Name = "NetTcpBinding_SalesSalesOrderService_Group";
                        EndpointAddress endpointSaleOrder = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/SalesSalesOrderService_Group"));
                        SalesOrderServiceClient proxy = new SalesOrderServiceClient(bindingSaleOrder, endpointSaleOrder);
                        proxy.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                        proxy.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                        proxy.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                        CallContext contextSaleOrder = new CallContext() { Company = _company };
                        //net.tcp://192.168.68.31:8201/DynamicsAx/Services/BHS_ReturnReturnOrderInServiceGroup
                        //NetTcpBinding bindingReturnOrder = new NetTcpBinding();
                        //bindingReturnOrder.Name = "NetTcpBinding_ReturnOrderInService";
                        //EndpointAddress endpointReturnOrder = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/BHS_ReturnReturnOrderInServiceGroup"));
                        //ReturnOrderService.ReturnOrderInServiceClient proxyReturnOrder = new ReturnOrderService.ReturnOrderInServiceClient(bindingReturnOrder, endpointReturnOrder);
                        //proxyReturnOrder.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                        //proxyReturnOrder.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                        //proxyReturnOrder.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                        //  ReturnOrderService.CallContext contextReturnOrder = new ReturnOrderService.CallContext() { Company = _company };
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
                        #region Import Declaration
                        EntityCollection lstImportDeclaration = getImportDeclaration(suborder.Id.ToString());
                        if (lstImportDeclaration.Entities.Any())
                        {
                            int i = 0;
                            foreach (var item in lstImportDeclaration.Entities)
                            {
                                if (item.HasValue("bsd_codeax"))
                                {
                                    if (i == 0) bsd_importdeclaration = item["bsd_importdeclaration"].ToString().Trim();
                                    else bsd_importdeclaration += ";" + item["bsd_importdeclaration"].ToString().Trim();
                                    i++;
                                }
                            }
                        }
                        //throw new Exception(bsd_importdeclaration);
                        #endregion
                        #region set Data Transfer AX Suborder
                        //set Data Transfer AX

                        if (suborder.HasValue("bsd_type"))
                        {
                            if (((OptionSetValue)suborder["bsd_type"]).Value == 861450004) //861,450,004 return Order
                                bsd_type = "ReturnOrder";
                            if (((OptionSetValue)suborder["bsd_type"]).Value == 861450005) //861,450,005 Change Order
                                bsd_type = "ChangeOrder";

                        }
                        if (suborder.HasValue("bsd_exchangeratevalue"))
                        {
                            bsd_exchangeratevalue = (decimal)suborder["bsd_exchangeratevalue"];
                        }
                        if (suborder.HasValue("bsd_totalcurrencyexchange"))
                        {
                            bsd_totalcurrencyexchange = ((Money)suborder["bsd_totalcurrencyexchange"]).Value;
                        }
                        if (suborder.HasValue("bsd_totalamount"))
                            bsd_totalamount = ((Money)suborder["bsd_totalamount"]).Value;
                        if (suborder.HasValue("bsd_duedate"))
                        {
                            bsd_duedate = DateTime.Parse(suborder["bsd_duedate"].ToString());
                            if (suborder.HasValue("bsd_paymentdate")) bsd_duedate = DateTime.Parse(suborder["bsd_paymentdate"].ToString());
                        }
                        if (suborder.HasValue("bsd_name"))
                        {
                            bsd_name = suborder["bsd_name"].ToString();
                            //  bsd_customerpo = suborder["bsd_name"].ToString();
                        }
                        if (suborder.HasValue("bsd_invoiceaccount"))
                            bsd_invoiceaccount = suborder["bsd_invoiceaccount"].ToString();

                        if (suborder.HasValue("bsd_potentialcustomer"))
                        {
                            //bsd_shiptoaccount = suborder["bsd_shiptoaccount"].ToString();
                            EntityReference rf_Entity = (EntityReference)suborder["bsd_potentialcustomer"];
                            bsd_potentialcustomerid = rf_Entity.Id;
                            Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                            if (Entity.HasValue("name"))
                                bsd_potentialcustomer = Entity["name"].ToString();
                            if (Entity.HasValue("bsd_accountgroup"))
                            {
                                rf_Entity = (EntityReference)Entity["bsd_accountgroup"];
                                Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                                if (Entity.HasValue("bsd_name"))
                                    bsd_accountgroup = Entity["bsd_name"].ToString();
                            }
                        }
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
                            bsd_addressinvoiceaccount_Guid = rf_addressinvoiceaccount.Id.ToString();
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
                        if (suborder.HasValue("transactioncurrencyid"))
                        {
                            EntityReference rf_Entity = (EntityReference)suborder["transactioncurrencyid"];
                            Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("isocurrencycode"));
                            if (Entity.HasValue("isocurrencycode"))
                                bsd_currencydefault = Entity["isocurrencycode"].ToString();
                        }
                        //throw new Exception(bsd_currencydefault);
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
                        dimAttValueCustomer.Name = DimensionAttCustomer;
                        dimAttValueCustomer.Value = bsd_customercode;
                        lstDemesion.SetValue(dimAttValueCustomer, 0);
                        //dimAttValueReturnOrderCustomer.Name = DimensionAttCustomer;
                        // dimAttValueReturnOrderCustomer.Value = bsd_customerpo;
                        // lstDemesionReturnOrder.SetValue(dimAttValueReturnOrderCustomer, 0);
                        if (suborder.HasValue("bsd_order"))
                        {
                            EntityReference rf_Entity = (EntityReference)suborder["bsd_order"];
                            Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                            if (Entity.HasValue("bsd_economiccontractno"))
                            {

                                FinancialDimensionServices.DimensionValueContract contract = new FinancialDimensionServices.DimensionValueContract();
                                contract.parmDimensionAttribute = DimensionAtt;
                                contract.parmValue = Entity["bsd_economiccontractno"].ToString();
                                if (Entity.HasValue("bsd_ordername"))
                                    contract.parmDescription = Entity["bsd_ordername"].ToString();
                                if (Entity.HasValue("bsd_date"))
                                    contract.parmActiveFrom = DateTime.Parse(Entity["bsd_date"].ToString());
                                contract.parmActiveTo = new DateTime(1900, 1, 1);
                                // contract.parmActiveTo = DateTime.Now.AddDays(3);
                                //
                                proxyDimension.createDimensionValue(contextDimension, contract);

                                dimAttValue.Name = DimensionAtt;
                                dimAttValue.Value = Entity["bsd_economiccontractno"].ToString();
                                bsd_economiccontractno = Entity["bsd_economiccontractno"].ToString();
                                lstDemesion.SetValue(dimAttValue, 1);
                                //dimAttValueSet.Values = new AxdType_DimensionAttributeValue[2] { dimAttValue , dimAttValueCustomer };
                                //dimAttValueReturnOrder.Name = DimensionAtt;
                                // dimAttValueReturnOrder.Value = Entity["bsd_economiccontractno"].ToString();
                                // lstDemesionReturnOrder.SetValue(dimAttValueReturnOrder, 1);
                                // dimAttValueSetReturnOrder.Values = new ReturnOrderService.AxdType_DimensionAttributeValue[2] { dimAttValueReturnOrder , dimAttValueReturnOrderCustomer };
                                // dimAttValueSetReturnOrder.Values = new ReturnOrderService.AxdType_DimensionAttributeValue[] { dimAttValueReturnOrder };

                            }

                        }
                        else if (!suborder.HasValue("bsd_order"))
                        {

                            FinancialDimensionServices.DimensionValueContract contract = new FinancialDimensionServices.DimensionValueContract();
                            contract.parmDimensionAttribute = DimensionAtt;
                            contract.parmValue = "0000";
                            contract.parmDescription = "0000";

                            contract.parmActiveFrom = DateTime.Parse(DateTime.Now.ToString());
                            contract.parmActiveTo = new DateTime(1900, 1, 1);
                            // contract.parmActiveTo = DateTime.Now.AddDays(3);
                            //
                            proxyDimension.createDimensionValue(contextDimension, contract);

                            dimAttValue.Name = DimensionAtt;
                            dimAttValue.Value = "0000";
                            bsd_economiccontractno = "0000";
                            lstDemesion.SetValue(dimAttValue, 1);
                            //dimAttValueSet.Values = new AxdType_DimensionAttributeValue[2] { dimAttValue , dimAttValueCustomer };
                            //dimAttValueReturnOrder.Name = DimensionAtt;
                            // dimAttValueReturnOrder.Value = Entity["bsd_economiccontractno"].ToString();
                            // lstDemesionReturnOrder.SetValue(dimAttValueReturnOrder, 1);
                            // dimAttValueSetReturnOrder.Values = new ReturnOrderService.AxdType_DimensionAttributeValue[2] { dimAttValueReturnOrder , dimAttValueReturnOrderCustomer };
                            // dimAttValueSetReturnOrder.Values = new ReturnOrderService.AxdType_DimensionAttributeValue[] { dimAttValueReturnOrder };
                        }
                        dimAttValueSet.Values = lstDemesion;
                        //throw new Exception(lstDemesion[0].Value.ToString());
                        // dimAttValueSetReturnOrder.Values = lstDemesionReturnOrder;
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

                            bool b_flat = true;
                            if (b_flat)
                            {

                                #region call service mới 16-01-2018 vinhlh call luannguyenAX
                                #region get SalesOrder in AX
                                EntityReference rf_Entity = (EntityReference)suborder["bsd_returnorder"];
                                string SalesId_Return = "";
                                Entity entityReturnOrder = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                                RMA_Number = entityReturnOrder["bsd_name"].ToString().Trim();
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
                                        //returnOrder = setSalesTableReturnOrder(bsd_currencydefault, bsd_customercode, bsd_invoiceaccount, bsd_invoicenameaccount, bsd_paymentterm, bsd_paymentmethod, bsd_shiptoaccountname, bsd_returnreasoncode, bsd_date, bsd_site, RMA_Number);

                                        #region set List Product 
                                        EntityCollection lstSubOrderProduct = getSubOrderProductB2C(target.Id.ToString());
                                        string lst_SalesLine = "";
                                        if (lstSubOrderProduct.Entities.Any())
                                        {
                                            int i = 0;
                                            //    ReturnOrderService.AxdEntity_SalesLine[] lstreturnSalesLine = new ReturnOrderService.AxdEntity_SalesLine[lstSubOrderProduct.Entities.Count];
                                            #region list Product
                                            foreach (Entity SubOrderProduct in lstSubOrderProduct.Entities)
                                            {

                                                string bsd_productid = ""; decimal bsd_shipquantity = 0m; string bsd_unit = ""; decimal bsd_priceperunit = 0m; decimal bsd_amount = 0m; decimal bsd_discount = 0m; decimal bsd_discountpercent = 0m;
                                                Entity b2c_SubOrderProduct = service.Retrieve(SubOrderProduct.LogicalName, SubOrderProduct.Id, new ColumnSet(true));
                                                //if (b2c_SubOrderProduct.HasValue("bsd_product"))
                                                //{
                                                //    EntityReference rf_Entity_product = (EntityReference)b2c_SubOrderProduct["bsd_product"];
                                                //    Entity Product = service.Retrieve(rf_Entity_product.LogicalName, rf_Entity_product.Id, new ColumnSet(true));
                                                //    if (Product.HasValue("bsd_itemsalestaxgroup"))
                                                //    {
                                                //        rf_Entity = (EntityReference)Product["bsd_itemsalestaxgroup"];
                                                //        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                                //        if (Entity.HasValue("bsd_code"))
                                                //            bsd_itemsalestaxgroup = Entity["bsd_code"].ToString();
                                                //    }
                                                //    if (Product.HasValue("productnumber"))
                                                //    {
                                                //        bsd_productid = Product["productnumber"].ToString();
                                                //    }
                                                //}
                                                //huy: 10/4/2018
                                                if (b2c_SubOrderProduct.HasValue("bsd_itemsalestaxgroup"))
                                                {
                                                    rf_Entity = (EntityReference)b2c_SubOrderProduct["bsd_itemsalestaxgroup"];
                                                    Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                                    if (Entity.HasValue("bsd_code"))
                                                    {
                                                        bsd_itemsalestaxgroup = Entity["bsd_code"].ToString();
                                                    }
                                                }
                                                //end huy: 9/4/2018
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
                                                if (i == 0) lst_SalesLine = bsd_productid.Trim() + ":" + bsd_unit + ":" + Math.Abs(bsd_priceperunit) + ":" + bsd_amount + ":" + Math.Abs(bsd_shipquantity) + ":" + bsd_site.Trim() + ":" + bsd_saletaxgroup.Trim() + ":" + bsd_itemsalestaxgroup.Trim();
                                                else lst_SalesLine += ";" + bsd_productid.Trim() + ":" + bsd_unit + ":" + Math.Abs(bsd_priceperunit) + ":" + bsd_amount + ":" + Math.Abs(bsd_shipquantity) + ":" + bsd_site.Trim() + ":" + bsd_saletaxgroup.Trim() + ":" + bsd_itemsalestaxgroup.Trim();
                                                i++;
                                                //salesLine = setSalesLineReturnOrder(bsd_productid, bsd_shipquantity, bsd_unit, bsd_priceperunit, bsd_amount, bsd_saletaxgroup, bsd_itemsalestaxgroup, bsd_site);
                                            }
                                            #endregion

                                        }
                                        //throw new Exception(bsd_customercode + "-" + bsd_returnreasoncode + "-" + RMA_Number + "-" + bsd_date.AddHours(7) + "-" + entity["bsd_suborderax"].ToString() + "-" + bsd_addressinvoiceaccount_Guid + "-" + bsd_invoicenameaccount + "-" + lst_SalesLine +"-"+ bsd_totalamount*bsd_exchangeratevalue + "--"+ bsd_exchangeratevalue);

                                        string s_Result = client.BHS_CreateReturnSalesOrder(context, bsd_customercode, bsd_returnreasoncode, "", RMA_Number, bsd_date.AddHours(7), entity["bsd_suborderax"].ToString(), bsd_addressinvoiceaccount_Guid, bsd_invoicenameaccount, lst_SalesLine, bsd_totalamount * bsd_exchangeratevalue, bsd_exchangeratevalue);
                                        if (!s_Result.Contains("true")) throw new Exception(s_Result);
                                        SalesId_Return = s_Result.Replace("true", "");
                                        #endregion

                                        #region Create SalesOrder AX

                                        //try
                                        //{
                                        //    client.UpdateAddressSalesOrder(context, bsd_shiptoaddressid, bsd_addressinvoiceaccount_Guid, bsd_invoicenameaccount, SalesId_Return, bsd_date.AddHours(7), true, bsd_type, bsd_economiccontractno, bsd_exchangeratevalue, bsd_importdeclaration, bsd_typeorder, bsd_totalcurrencyexchange);

                                        //}
                                        //catch (Exception ex)
                                        //{
                                        //    // Delele Sales Order
                                        //    // proxyReturnOrder.delete(contextReturnOrder, returnedSalesOrderEntityKey);
                                        //    throw new Exception("Update error: " + ex.Message);
                                        //}
                                        Entity subOrderUpd = new Entity(target.LogicalName, target.Id);
                                        subOrderUpd["bsd_suborderax"] = SalesId_Return;
                                        service.Update(subOrderUpd);
                                        ReturnIdAX = SalesId_Return;
                                        #endregion


                                    }
                                    else
                                    {
                                        throw new Exception("Return Order created by SubOrder does not transfer AX");
                                    }
                                }
                                else
                                {
                                    // throw new Exception("Return order does not found base Suborder");
                                    #region Đơn hàng trả ko rõ nguôn gốc
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
                                    //rf_Entity = (EntityReference)entityReturnOrder["bsd_findsuborder"];
                                    // Entity entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_suborderax"));

                                    //returnOrder = setSalesTableReturnOrder(bsd_currencydefault, bsd_customercode, bsd_invoiceaccount, bsd_invoicenameaccount, bsd_paymentterm, bsd_paymentmethod, bsd_shiptoaccountname, bsd_returnreasoncode, bsd_date, bsd_site, RMA_Number);

                                    #region set List Product 
                                    EntityCollection lstSubOrderProduct = getSubOrderProductB2C(target.Id.ToString());
                                    string lst_SalesLine = "";
                                    if (lstSubOrderProduct.Entities.Any())
                                    {
                                        int i = 0;
                                        //    ReturnOrderService.AxdEntity_SalesLine[] lstreturnSalesLine = new ReturnOrderService.AxdEntity_SalesLine[lstSubOrderProduct.Entities.Count];
                                        #region list Product
                                        foreach (Entity SubOrderProduct in lstSubOrderProduct.Entities)
                                        {

                                            string bsd_productid = ""; decimal bsd_shipquantity = 0m; string bsd_unit = ""; decimal bsd_priceperunit = 0m; decimal bsd_amount = 0m; decimal bsd_discount = 0m; decimal bsd_discountpercent = 0m;
                                            Entity b2c_SubOrderProduct = service.Retrieve(SubOrderProduct.LogicalName, SubOrderProduct.Id, new ColumnSet(true));
                                            if (b2c_SubOrderProduct.HasValue("bsd_itemsalestaxgroup"))
                                            {
                                                rf_Entity = (EntityReference)b2c_SubOrderProduct["bsd_itemsalestaxgroup"];
                                                Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                                if (Entity.HasValue("bsd_code"))
                                                {
                                                    bsd_itemsalestaxgroup = Entity["bsd_code"].ToString();
                                                }
                                            }
                                            //end huy: 9/4/2018
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
                                            if (i == 0) lst_SalesLine = bsd_productid.Trim() + ":" + bsd_unit + ":" + Math.Abs(bsd_priceperunit) + ":" + bsd_amount + ":" + Math.Abs(bsd_shipquantity) + ":" + bsd_site.Trim() + ":" + bsd_saletaxgroup.Trim() + ":" + bsd_itemsalestaxgroup.Trim();
                                            else lst_SalesLine += ";" + bsd_productid.Trim() + ":" + bsd_unit + ":" + Math.Abs(bsd_priceperunit) + ":" + bsd_amount + ":" + Math.Abs(bsd_shipquantity) + ":" + bsd_site.Trim() + ":" + bsd_saletaxgroup.Trim() + ":" + bsd_itemsalestaxgroup.Trim();
                                            i++;
                                            //salesLine = setSalesLineReturnOrder(bsd_productid, bsd_shipquantity, bsd_unit, bsd_priceperunit, bsd_amount, bsd_saletaxgroup, bsd_itemsalestaxgroup, bsd_site);
                                        }
                                        #endregion

                                    }
                                    //throw new Exception(bsd_customercode + "-" + bsd_returnreasoncode + "-" + RMA_Number + "-" + bsd_date.AddHours(7) + "-" + entity["bsd_suborderax"].ToString() + "-" + bsd_addressinvoiceaccount_Guid + "-" + bsd_invoicenameaccount + "-" + lst_SalesLine +"-"+ bsd_totalamount*bsd_exchangeratevalue + "--"+ bsd_exchangeratevalue);
                                    // throw new Exception("Ex");
                                    string bsd_suborderax = getSubOrderHistoryExample(bsd_potentialcustomerid);
                                    string s_Result = client.BHS_CreateReturnSalesOrder(context, bsd_customercode, bsd_returnreasoncode, "", RMA_Number, bsd_date.AddHours(7), bsd_suborderax, bsd_addressinvoiceaccount_Guid, bsd_invoicenameaccount, lst_SalesLine, bsd_totalamount * bsd_exchangeratevalue, bsd_exchangeratevalue);
                                    if (!s_Result.Contains("true")) throw new Exception(s_Result);
                                    SalesId_Return = s_Result.Replace("true", "");
                                    #endregion

                                    #region Create SalesOrder AX
                                    Entity subOrderUpd = new Entity(target.LogicalName, target.Id);
                                    subOrderUpd["bsd_suborderax"] = SalesId_Return;
                                    service.Update(subOrderUpd);
                                    ReturnIdAX = SalesId_Return;
                                    #endregion



                                    #endregion
                                }
                                #endregion
                                #endregion
                            }

                        }
                        else
                        {
                            #region         Trasfer suborder                                                                  
                            //  Guid suborderB2b_Id = Guid.NewGuid();
                            if (suborder.HasValue("bsd_typeorder"))
                            {
                                if (((OptionSetValue)suborder["bsd_typeorder"]).Value == 861450002)
                                    bsd_typeorder = true;
                            }

                            #region set Data Transfer AX Suborder Product
                            salesTable = setSalesTableSalesOrder(bsd_currencydefault, bsd_customercode, bsd_invoiceaccount, bsd_potentialcustomer, bsd_paymentterm, bsd_paymentmethod, bsd_shiptoaccountname, ShippingDateConfirmed, bsd_requestedshipdate, bsd_confirmedreceiptdate, bsd_requestedreceiptdate, bsd_site, bsd_duedate, bsd_accountgroup, bsd_name, bsd_customerpo);
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
                                    decimal bsd_giatruocthue = 0m;
                                    decimal bsd_amount = 0m;
                                    decimal bsd_discount = 0m;
                                    decimal bsd_discountpercent = 0m;
                                    Entity b2c_SubOrderProduct = service.Retrieve(SubOrderProduct.LogicalName, SubOrderProduct.Id, new ColumnSet(true));
                                    //if (b2c_SubOrderProduct.HasValue("bsd_product"))
                                    //{
                                    //    EntityReference rf_Entity_product = (EntityReference)b2c_SubOrderProduct["bsd_product"];
                                    //    Entity Product = service.Retrieve(rf_Entity_product.LogicalName, rf_Entity_product.Id, new ColumnSet(true));
                                    //    if (Product.HasValue("bsd_itemsalestaxgroup"))
                                    //    {
                                    //        EntityReference rf_Entity = (EntityReference)Product["bsd_itemsalestaxgroup"];
                                    //        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                    //        if (Entity.HasValue("bsd_code"))
                                    //        {
                                    //            bsd_itemsalestaxgroup = Entity["bsd_code"].ToString();
                                    //        }
                                    //    }
                                    //    if (string.IsNullOrEmpty(bsd_productid))
                                    //    {
                                    //        bsd_productid = Product["productnumber"].ToString();
                                    //    }
                                    //}
                                    //huy: 9/4/2018
                                    if (b2c_SubOrderProduct.HasValue("bsd_itemsalestaxgroup"))
                                    {
                                        EntityReference rf_Entity = (EntityReference)b2c_SubOrderProduct["bsd_itemsalestaxgroup"];
                                        Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                        if (Entity.HasValue("bsd_code"))
                                        {
                                            bsd_itemsalestaxgroup = Entity["bsd_code"].ToString();
                                        }
                                    }
                                    //end huy: 9/4/2018
                                    if (b2c_SubOrderProduct.HasValue("bsd_productid"))
                                        bsd_productid = b2c_SubOrderProduct["bsd_productid"].ToString();
                                    if (b2c_SubOrderProduct.HasValue("bsd_shipquantity"))
                                        bsd_shipquantity = decimal.Parse(b2c_SubOrderProduct["bsd_shipquantity"].ToString());
                                    if (b2c_SubOrderProduct.HasValue("bsd_unit"))
                                    {
                                        EntityReference rf_addressinvoiceaccount = (EntityReference)b2c_SubOrderProduct["bsd_unit"];
                                        Entity Entity = service.Retrieve(rf_addressinvoiceaccount.LogicalName, rf_addressinvoiceaccount.Id, new ColumnSet("name"));
                                        if (Entity.HasValue("name"))
                                            bsd_unit = Entity["name"].ToString();
                                    }
                                    if (b2c_SubOrderProduct.HasValue("bsd_giatruocthue"))
                                        bsd_giatruocthue = ((Money)b2c_SubOrderProduct["bsd_giatruocthue"]).Value;
                                    if (b2c_SubOrderProduct.HasValue("bsd_amount"))
                                        bsd_amount = ((Money)b2c_SubOrderProduct["bsd_amount"]).Value;

                                    string result = client.checkSalesTable(context, bsd_name, bsd_invoiceaccount, bsd_customercode, bsd_shiptoaccount, bsd_paymentterm, bsd_paymentmethod, bsd_currencydefault, bsd_productid, bsd_site, "", bsd_customerpo, bsd_saletaxgroup, bsd_itemsalestaxgroup, "");
                                    if (result != "0") throw new Exception(result);

                                    AxdEntity_SalesLine salesLine = new AxdEntity_SalesLine();
                                    salesLine = setSalesLineSalesOrder(bsd_productid, bsd_shipquantity, bsd_unit, bsd_giatruocthue, bsd_amount, bsd_saletaxgroup, bsd_itemsalestaxgroup, bsd_site);
                                    Product product = new Product(bsd_productid, bsd_giatruocthue, bsd_amount);
                                    lstProduct.Add(product);
                                    lstLine[i] = salesLine;
                                    i++;
                                }
                                #endregion
                                salesTable.SalesLine = lstLine;

                            }
                            #endregion
                            #region Create SalesOrder AX
                            salesOrder.SalesTable = new AxdEntity_SalesTable[1] { salesTable };
                            // Call the create method on the service passing in the document.
                            trace = "1";
                            EntityKey[] returnedSalesOrderEntityKey = proxy.create(contextSaleOrder, salesOrder);
                            trace = "2";
                            // The create method returns an EntityKey which contains the ID of the sales order.

                            EntityKey returnedSalesOrder = (EntityKey)returnedSalesOrderEntityKey.GetValue(0);

                            try
                            {
                                client.UpdateAddressSalesOrder(context, bsd_shiptoaddressid, bsd_addressinvoiceaccount_Guid, bsd_invoicenameaccount, returnedSalesOrder.KeyData[0].Value, DateTime.Now, false, bsd_type, bsd_economiccontractno, bsd_exchangeratevalue, bsd_importdeclaration, bsd_typeorder, bsd_totalcurrencyexchange);
                                foreach (Product productUp in lstProduct)
                                {
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
                    bool connect_ax = getConfigdefaultconnectax();
                    if (connect_ax == false)
                    {
                        throw new Exception("Thiếu khai báo connect ax");
                    }
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

                                EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));

                                ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                                client.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                                client.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                                client.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                                ServiceReferenceAIF.CallContext context = new ServiceReferenceAIF.CallContext() { Company = _company };

                                #endregion
                                if (RequestDelivery.HasValue("bsd_site"))
                                {

                                    EntityReference Site_fr = (EntityReference)RequestDelivery["bsd_site"];
                                    if (getConsignmentSitesConfigDefault(Site_fr.Id))
                                    {

                                        #region  site kí gửi
                                        DateTime bsd_date = DateTime.Now;
                                        if (RequestDelivery.HasValue("bsd_date")) bsd_date = (DateTime)RequestDelivery["bsd_date"];
                                        Entity Entity_site = service.Retrieve(Site_fr.LogicalName, Site_fr.Id, new ColumnSet("bsd_code"));
                                        if (Entity_site.HasValue("bsd_code"))
                                            Site = Entity_site["bsd_code"].ToString();
                                        else throw new Exception("Site SubOrder does not Exist");
                                        EntityCollection lst_Entity = getRequestDeliveryProduct(RequestDelivery.Id);
                                        if (lst_Entity.Entities.Any())
                                        {
                                            string WareHouse = "";
                                            lstProduct = "";
                                            int i = 0;
                                            #region kho kí gửi
                                            //EntityCollection lst_Warehouse = getConsignmentWareHouse(Entity_site.Id);
                                            //if (lst_Warehouse.Entities.Any())
                                            //{

                                            //    // rf_Entity = (EntityReference)RequestDeliveryProduct["bsd_warehouseconsignment"];
                                            //    Entity Entity = lst_Warehouse.Entities.First();
                                            //    if (Entity.HasValue("bsd_warehouseid")) WareHouse = Entity["bsd_warehouseid"].ToString().Trim();
                                            //}
                                            //else throw new Exception("Warehouse does not exist in Consignment  Site");
                                            #endregion
                                            foreach (var item in lst_Entity.Entities)
                                            {

                                                Entity RequestDeliveryProduct = service.Retrieve(item.LogicalName, item.Id, new ColumnSet(true));
                                                if (!RequestDeliveryProduct.HasValue("bsd_warehouse")) throw new Exception("Does not select Warehouse");
                                                if (!RequestDeliveryProduct.HasValue("bsd_quantity")) throw new Exception("Does not select Quantity");
                                                Entity warehouse = service.Retrieve(((EntityReference)RequestDeliveryProduct["bsd_warehouse"]).LogicalName, ((EntityReference)RequestDeliveryProduct["bsd_warehouse"]).Id, new ColumnSet(true));
                                                WareHouse = warehouse["bsd_warehouseid"].ToString();
                                                Entity site = service.Retrieve(((EntityReference)warehouse["bsd_site"]).LogicalName, ((EntityReference)warehouse["bsd_site"]).Id, new ColumnSet("bsd_code"));
                                                Site = site["bsd_code"].ToString();
                                                if (RequestDeliveryProduct.HasValue("bsd_productid"))
                                                {
                                                    // Site = "BHS";
                                                    // WareHouse = "KD01";
                                                    if (i == 0)
                                                        lstProduct = RequestDeliveryProduct["bsd_productid"].ToString().Trim() + ":" + Site + ":" + WareHouse + ":" + RequestDeliveryProduct["bsd_quantity"].ToString().Trim() + ":" + bsd_date;
                                                    else
                                                        lstProduct += ";" + RequestDeliveryProduct["bsd_productid"].ToString().Trim() + ":" + Site + ":" + WareHouse + ":" + RequestDeliveryProduct["bsd_quantity"].ToString().Trim() + ":" + bsd_date;
                                                    i++;
                                                }
                                                else
                                                {
                                                    throw new Exception("Can not transfer AX over becaus product not found");
                                                }
                                            }
                                            if (!String.IsNullOrEmpty(lstProduct))
                                            {
                                                // throw new Exception("Suborder: " + RequestDelivery["bsd_name"].ToString().Trim() + "Sales Line: " + lstProduct);
                                                string s_Result = client.BHS_CreateMoveOut(context, RequestDelivery["bsd_name"].ToString().Trim(), bsd_date, lstProduct);
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
                                        #endregion
                                    }
                                    else
                                    {

                                        #region Site bình thường tạo pickking list

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
                                                    if (!RequestDeliveryProduct.HasValue("bsd_warehouse")) throw new Exception("Does not select Warehouse");
                                                    if (!RequestDeliveryProduct.HasValue("bsd_quantity")) throw new Exception("Does not select Quantity");
                                                    rf_Entity = (EntityReference)RequestDeliveryProduct["bsd_warehouse"];
                                                    Entity Entity = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_warehouseid"));

                                                    EntityReference rf_Entity_product = (EntityReference)RequestDeliveryProduct["bsd_product"];

                                                    if (rf_Entity_product.Id == Guid.Parse(flatId))
                                                    {
                                                        if (k == 0)
                                                            lstProduct = Product["productnumber"].ToString().Trim() + ":" + Site + ":" + Entity["bsd_warehouseid"].ToString().Trim() + ":" + RequestDeliveryProduct["bsd_quantity"].ToString().Trim() + ":" + i;
                                                        else
                                                            lstProduct += ";" + Product["productnumber"].ToString().Trim() + ":" + Site + ":" + Entity["bsd_warehouseid"].ToString().Trim() + ":" + RequestDeliveryProduct["bsd_quantity"].ToString().Trim() + ":" + i;
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
                                            // throw new Exception("okie");
                                            if (!String.IsNullOrEmpty(lstProduct) && !String.IsNullOrEmpty(lstProdduct_total_quantity))
                                            {
                                                //throw new Exception("Suborder: " + SubOrderAXNumber + "Sales Line: " + lstProduct+ "Sales Line total:"+ lstProdduct_total_quantity);
                                                //throw new Exception(context + " ta ne");
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
                                        #endregion
                                    }
                                }
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
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
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
        public string getSubOrderHistoryExample(Guid customerAccountID)
        {
            string bsd_suborderax = "";
            string xml = @"<fetch version='1.0' output-format='xml-platform' top='1' mapping='logical' distinct='false'>
                                          <entity name='bsd_suborder'>
                                            <attribute name='bsd_name' />
                                            <attribute name='createdon' />
                                            <attribute name='bsd_suborderid' />
                                            <attribute name='bsd_suborderax' />
                                            <order attribute='createdon' descending='true' />
                                            <filter type='and'>
                                              <condition attribute='bsd_potentialcustomer' operator='eq'  uitype='account' value='"+customerAccountID+@"' />
                                              <condition attribute='bsd_suborderax' operator='not-null' />
                                              <condition attribute='bsd_type' operator='in'>
                                                <value>861450000</value>
                                                <value>861450002</value>
                                                <value>861450001</value>
                                              </condition>
                                            </filter>
                                          </entity>
                                        </fetch>";
            EntityCollection list_bhstradingAccountb2b = service.RetrieveMultiple(new FetchExpression(xml));
            if (list_bhstradingAccountb2b.Entities.Any())
            {
                bsd_suborderax = list_bhstradingAccountb2b.Entities.First()["bsd_suborderax"].ToString().Trim();
            }
            else throw new Exception("Customer Account not found Suborder on CRM");
            return bsd_suborderax;
        }
        public EntityCollection getImportDeclaration(string subOrderId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                          <entity name='bsd_importdeclaration'>
                            <all-attributes />
                            <order attribute='bsd_name' descending='false' />
                            <link-entity name='bsd_bsd_suborder_bsd_importdeclaration' from='bsd_importdeclarationid' to='bsd_importdeclarationid' visible='false' intersect='true'>
                              <link-entity name='bsd_suborder' from='bsd_suborderid' to='bsd_suborderid' alias='ag'>
                                <filter type='and'>
                                  <condition attribute='bsd_suborderid' operator='eq' uitype='bsd_suborder' value='" + subOrderId + @"' />
                                </filter>
                              </link-entity>
                            </link-entity>
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
          string bsd_shiptoaccountname, DateTime ShippingDateConfirmed, DateTime bsd_requestedshipdate, DateTime bsd_confirmedreceiptdate, DateTime bsd_requestedreceiptdate, string bsd_site, DateTime bsd_duedate, string bsd_accountgroup, string suborderid, string bsd_customerpo)
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
            salesTable.ShippingDateConfirmed = bsd_requestedshipdate;
            salesTable.ShippingDateRequested = bsd_requestedshipdate;
            salesTable.ReceiptDateConfirmed = bsd_confirmedreceiptdate;
            salesTable.ReceiptDateRequested = bsd_requestedreceiptdate;
            salesTable.FixedDueDateSpecified = true;
            salesTable.FixedDueDate = bsd_duedate.AddHours(7);
            salesTable.SalesType = AxdEnum_SalesType.Sales;
            salesTable.CustomerRef = suborderid;
            //salesTable.FixedExchRate = 9999;
            //salesTable.FixedExchRateSpecified = true;
            //throw new Exception("SalesType: "+ salesTable.SalesType);
            salesTable.InventSiteId = bsd_site;
            salesTable.CustGroup = bsd_accountgroup;
            salesTable.QuotationId = "";
            Random rnd = new Random();
            salesTable.PurchOrderFormNum = bsd_customerpo;//rnd.Next(100000).ToString();
            return salesTable;
        }
        //public ReturnOrderService.AxdEntity_SalesTable setSalesTableReturnOrder(string bsd_currencydefault, string bsd_customercode, string bsd_invoiceaccount, string bsd_name,
        //   string bsd_paymentterm, string bsd_paymentmethod,
        // string bsd_shiptoaccountname, string bsd_returnreasoncode, DateTime bsd_date, string bsd_site, string RMA_Number,string bsd_accountgroup)
        //{
        //    ReturnOrderService.AxdEntity_SalesTable salesTable = new ReturnOrderService.AxdEntity_SalesTable();
        //    salesTable.CurrencyCode = bsd_currencydefault;
        //    salesTable.CustAccount = bsd_customercode;
        //    salesTable.InvoiceAccount = bsd_invoiceaccount;
        //    //salesTable.CustAccount = "A00002";
        //    // salesTable.InvoiceAccount = "A00002";
        //    salesTable.LanguageId = "en-us";
        //    salesTable.SalesName = bsd_name;
        //    salesTable.Payment = bsd_paymentterm;
        //    salesTable.PaymMode = bsd_paymentmethod;
        //    //  salesTable.DeliveryName = bsd_shiptoaccount;
        //    salesTable.DeliveryName = bsd_shiptoaccountname;
        //    salesTable.InventSiteId = bsd_site;
        //    salesTable.ReturnReasonCodeId = bsd_returnreasoncode;
        //    salesTable.ReturnDeadline = bsd_date;
        //    salesTable.CustGroup = bsd_accountgroup;
        //    salesTable.QuotationId = "";
        //    salesTable.ReturnItemNum = RMA_Number;//.Replace("RMA.","");
        //    return salesTable;
        //}
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
        //public ReturnOrderService.AxdEntity_SalesTable mapSalesTable(AxdEntity_SalesTable _SalesOrderTable, string bsd_returnreasoncode, DateTime bsd_date, string RMA_Number)
        //{
        //    ReturnOrderService.AxdEntity_SalesTable returnOrder = new ReturnOrderService.AxdEntity_SalesTable();
        //    returnOrder.CurrencyCode = _SalesOrderTable.CurrencyCode;
        //    returnOrder.CustAccount = _SalesOrderTable.CustAccount;
        //    returnOrder.InvoiceAccount = _SalesOrderTable.InvoiceAccount;
        //    returnOrder.LanguageId = _SalesOrderTable.LanguageId;
        //    returnOrder.Payment = _SalesOrderTable.Payment;
        //    returnOrder.SalesName = _SalesOrderTable.SalesName;
        //    //returnOrder.ReturnReasonCodeId = "Credit";
        //    returnOrder.ReturnReasonCodeId = bsd_returnreasoncode;
        //    returnOrder.ReturnDeadline = bsd_date;
        //    returnOrder.InventSiteId = _SalesOrderTable.InventSiteId;
        //    returnOrder.CustGroup = _SalesOrderTable.CustGroup;
        //    returnOrder.QuotationId = _SalesOrderTable.QuotationId;
        //    returnOrder.ReturnItemNum = RMA_Number;//.Replace("RMA.", "");
        //    return returnOrder;
        //}
        //public ReturnOrderService.AxdEntity_SalesLine setSalesLineReturnOrder(string bsd_productid, decimal bsd_shipquantity, string bsd_unit, decimal bsd_priceperunit, decimal bsd_amount, string bsd_saletaxgroup, string bsd_itemsalestaxgroup, string bsd_site)
        //{
        //    ReturnOrderService.AxdEntity_SalesLine salesLine = new ReturnOrderService.AxdEntity_SalesLine();
        //    salesLine.ItemId = bsd_productid;
        //    // salesLine.SalesQty = bsd_shipquantity;
        //    salesLine.SalesUnit = bsd_unit;
        //    salesLine.SalesPrice = bsd_priceperunit;
        //    salesLine.LineAmount = bsd_amount;
        //    salesLine.ExpectedRetQty = bsd_shipquantity;
        //    salesLine.ExpectedRetQtySpecified = true;
        //    //  throw new Exception("okie"+ bsd_saletaxgroup +"-"+ bsd_itemsalestaxgroup);
        //    salesLine.TaxGroup = bsd_saletaxgroup;
        //    salesLine.TaxItemGroup = bsd_itemsalestaxgroup;
        //    ReturnOrderService.AxdEntity_InventDim inventDim = new ReturnOrderService.AxdEntity_InventDim();
        //    inventDim.InventColorId = "01";
        //    inventDim.InventSiteId = bsd_site;
        //    salesLine.InventDim = new ReturnOrderService.AxdEntity_InventDim[1] { inventDim };
        //    return salesLine;
        //}
        public bool getConsignmentSitesConfigDefault(Guid SiteId)
        {
            //vinhlh 21-12-2017 site kí gửi
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_configdefault'>
                                    <attribute name='bsd_configdefaultid' />
                                    <attribute name='bsd_name' />
                                    <attribute name='createdon' />
                                    <order attribute='bsd_name' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='bsd_consignmentsites' operator='eq' uitype='bsd_site' value='" + SiteId + @"' />
                                    </filter>
                                  </entity>
                                </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            if (lst.Entities.Any())
            {
                return true;
            }
            return false;
        }
        public EntityCollection getConsignmentWareHouse(Guid SiteId)
        {
            //vinhlh 21-12-2017 site kí gửi
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_warehouseentity'>
                        <attribute name='bsd_warehouseentityid' />
                        <attribute name='bsd_name' />
                        <attribute name='bsd_warehouseid' />
                        <attribute name='createdon' />
                        <order attribute='bsd_name' descending='false' />
                        <filter type='and'>
                          <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + SiteId + @"' />
                          <condition attribute='statuscode' operator='eq' value='1' />
                        </filter>
                      </entity>
                    </fetch>";
            return service.RetrieveMultiple(new FetchExpression(xml));

            // return false;
        }
        public bool getConfigdefaultconnectax()
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
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
                </fetch>";
            Entity configdefault = service.RetrieveMultiple(new FetchExpression(xml)).Entities.FirstOrDefault();
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
