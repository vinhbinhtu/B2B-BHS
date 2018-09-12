using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugin_DeleteMasterDataAX.Service;
using Plugin_DeleteMasterDataAX.ServiceReferenceAIF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_DeleteMasterDataAX
{
    public class Main : IPlugin
    {
        MyService myService;
        public string _userName = "";
        public string _passWord = "";
        public string _company = "";
        public string _port = "";
        public string _domain = "";
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            myService = new MyService(serviceProvider);
            //string _userName = "s.ttctech";
            //string _passWord = "AX@tct2017";
            //string _company = "102";
            //string _port = "10.33.21.1:8201";
            //string _domain = "SUG.TTCG.LAN";
            //string _userName = "s.ttctech";
            //string _passWord = "AX@tct2017";
            //string _company = "102";
            //string _port = "10.33.21.1:8201";
            //string _domain = "SUG.TTCG.LAN";       
            myService.StartService();
            bool connect_ax = getConfigdefaultconnectax();
            if (connect_ax == false)
            {
                throw new Exception("Thiếu khai báo connect ax");
            }
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
            if (myService.context.MessageName == "Delete")
            {
                #region Delete
                try
                {



                    Entity PreImage = (Entity)context.PreEntityImages["PreImage"];

                    if (PreImage != null)
                    {

                        // throw new Exception("detele");
                        // Entity Industrial = myService.service.Retrieve(PreImage.LogicalName, PreImage.Id, new ColumnSet(true));
                        // throw new Exception("okie" + PreImage.Id + "___" + PreImage.LogicalName);

                        if (PreImage.LogicalName == "bsd_bankaccount")
                        {
                            #region bsd_bankaccount

                            string bsd_account = "", bsd_name = "";
                            if (PreImage.HasValue("bsd_name"))
                            {
                                bsd_name = PreImage["bsd_name"].ToString();
                            }
                            if (PreImage.HasValue("bsd_account"))
                            {
                                EntityReference rf_Entity = (EntityReference)PreImage["bsd_account"];
                                Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("accountnumber"));
                                if (en.HasValue("accountnumber"))
                                    bsd_account = en["accountnumber"].ToString();
                            }
                            bool b_Result = client.BHS_BankAccount_Delete(contextService, bsd_name, bsd_account);
                            if (b_Result == false) throw new Exception("Can not delete because it's exist transaction AX or not find");
                            #endregion

                        }
                        else if (PreImage.LogicalName == "bsd_groupaccount")
                        {
                            #region bsd_accountgroup
                            string bsd_name = "";
                            if (PreImage.HasValue("bsd_name"))
                            {
                                bsd_name = PreImage["bsd_name"].ToString();
                            }
                            // throw new Exception(bsd_name);
                            // bool b_delele = getAccount(PreImage.Id);
                            // throw new Exception("delete" + b_delele);
                            bool b_Result = client.BHS_CustGroup_Delete(contextService, bsd_name);

                            if (b_Result == false)
                                throw new Exception("Can not delete because it's exist transaction AX or not find");
                            #endregion
                        }
                        else if (PreImage.LogicalName == "bsd_address")
                        {
                            #region bsd_address
                            if (PreImage.HasValue("bsd_account"))
                            {
                                EntityReference rf_entity = (EntityReference)PreImage["bsd_account"];
                                Entity en = myService.service.Retrieve(rf_entity.LogicalName, rf_entity.Id, new ColumnSet("accountnumber"));
                                string b_Result = client.BHS_DelectAddress(contextService, PreImage.Id.ToString(), en["accountnumber"].ToString());
                                if (b_Result != "0") throw new Exception("Can not delete because it's exist transaction AX or not find");
                            }
                            #endregion
                        }
                        else if (PreImage.LogicalName == "account")
                        {
                            #region account
                            string accountnumber = "";
                            if (PreImage.HasValue("accountnumber"))
                            {
                                accountnumber = PreImage["accountnumber"].ToString();
                            }
                            bool b_Result = client.BHS_CustAccount_Delete(contextService, accountnumber);
                            if (b_Result == false)
                                throw new Exception("Can not delete because it's exist transaction AX or not find or not find");
                            #endregion
                        }
                        else if (PreImage.LogicalName == "bsd_returnreasoncode")
                        {
                            #region
                            string bsd_code = "";
                            string bsd_returnreasongroup = "";
                            if (PreImage.HasValue("bsd_code"))
                            {
                                bsd_code = PreImage["bsd_code"].ToString();
                            }
                            if (PreImage.HasValue("bsd_returnreasongroup"))
                            {
                                EntityReference rf_Entity = (EntityReference)PreImage["bsd_returnreasongroup"];
                                Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                if (en.HasValue("bsd_code"))
                                    bsd_returnreasongroup = en["bsd_code"].ToString();
                            }
                            bool b_Result = client.BHS_ReasonCode_Delete(contextService, bsd_code, bsd_returnreasongroup);
                            if (b_Result == false) throw new Exception("Can not delete because it's exist transaction AX or not find");
                            #endregion
                        }
                        else if (PreImage.LogicalName == "bsd_returnreasongroup")
                        {
                            string bsd_code = "";
                            if (PreImage.HasValue("bsd_code"))
                            {
                                bsd_code = PreImage["bsd_code"].ToString();
                                bool b_Result = client.BHS_ReasonCodeGroup_Delete(contextService, bsd_code);
                                if (b_Result == false) throw new Exception("Can not delete because it's exist transaction AX or not find");
                            }
                        }

                    }

                    //  throw new Exception("okie");

                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                #endregion
            }
            else if (myService.context.MessageName == "Create")
            {
               
                #region Create
                Entity target = (Entity)context.InputParameters["Target"];
                if (target.LogicalName == "bsd_bankaccount")
                {
                    #region bankaccount
                    Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    string bsd_bankid = "", bsd_account = "", bsd_bankgroup = "", bsd_brand = "", bsd_swiftcode = "", bsd_name = "";
                    if (entity.HasValue("bsd_name")) bsd_name = entity["bsd_name"].ToString();
                    if (entity.HasValue("bsd_bankid")) bsd_bankid = entity["bsd_bankid"].ToString();
                    if (entity.HasValue("bsd_brand")) bsd_brand = entity["bsd_brand"].ToString();
                    if (entity.HasValue("bsd_swiftcode")) bsd_swiftcode = entity["bsd_swiftcode"].ToString();
                    if (entity.HasValue("bsd_account"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["bsd_account"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("accountnumber"));
                        if (en.HasValue("accountnumber"))
                            bsd_account = en["accountnumber"].ToString();
                    }
                    if (entity.HasValue("bsd_bankgroup"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["bsd_bankgroup"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                        if (en.HasValue("bsd_name"))
                            bsd_bankgroup = en["bsd_name"].ToString();
                    }
                    if (!string.IsNullOrEmpty(bsd_bankgroup) && !string.IsNullOrEmpty(bsd_account) && !string.IsNullOrEmpty(bsd_brand) && !string.IsNullOrEmpty(bsd_bankid))
                    {
                        string s = client.BHS_BankAccount_InsertUpdate(contextService, bsd_name, bsd_account, bsd_bankgroup + " - " + bsd_brand, "", bsd_swiftcode);
                        if (!s.Contains("successful"))
                        {
                            throw new Exception(s);
                        }
                    }
                    //else
                    //{
                    //    throw new Exception("null value");
                    //}
                    #endregion
                }
                if (target.LogicalName == "bsd_groupaccount")
                {
                    #region bsd_groupaccount
                    Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    string bsd_name = "", bsd_accountname = "";
                    if (entity.HasValue("bsd_accountname")) bsd_accountname = entity["bsd_accountname"].ToString();
                    if (entity.HasValue("bsd_name")) bsd_name = entity["bsd_name"].ToString();
                    if (!string.IsNullOrEmpty(bsd_accountname) && !string.IsNullOrEmpty(bsd_name))
                    {
                        //Call service insert
                        string s = client.BHS_CustGroup_InsertUpdate(contextService, bsd_name, bsd_accountname);
                        if (!s.Contains("successful"))
                        {
                            throw new Exception(s);
                        }
                    }
                    #endregion
                }
                if (target.LogicalName == "bsd_address")
                {
                    // throw new Exception("okie");
                    #region bsd_address
                    Entity entityAddress = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    int bsd_accounttype = 0;
                    string bsd_purpose = "", bsd_country = "", bsd_key = "", bsd_province = "", bsd_district = "", bsd_street = "", bsd_ward = "", bsd_account = "", bsd_taxregistration = "";
                    #region set Value

                    if (entityAddress.HasValue("bsd_account"))
                    {
                        //bsd_shiptoaccount = suborder["bsd_shiptoaccount"].ToString();
                        EntityReference rf_Entity = (EntityReference)entityAddress["bsd_account"];
                        Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                        if (Entity.HasValue("accountnumber"))
                        {
                            bsd_account = Entity["accountnumber"].ToString();
                            //  checkAddress = client.BHS_ValidateAddressAccount(contextService, bsd_account, target.Id.ToString());
                        }
                        if (Entity.HasValue("bsd_taxregistration"))
                        {
                            bsd_taxregistration = Entity["bsd_taxregistration"].ToString();
                        }
                        if (Entity.HasValue("bsd_accounttype"))
                        {
                            bsd_accounttype = ((OptionSetValue)Entity["bsd_accounttype"]).Value;
                            //if (bsd_accounttype == 861450001)
                            //{
                            //    throw new Exception("Accout type shipper address");
                            //}
                        }
                    }
                    #region Insert Address

                    if (entityAddress.HasValue("bsd_purpose_tmpvalue"))
                    {
                        bsd_purpose = entityAddress["bsd_purpose_tmpvalue"].ToString();
                        if (bsd_purpose.Contains("Business"))
                            bsd_purpose = bsd_purpose.Replace("Business", "9");
                        if (bsd_purpose.Contains("Delivery"))
                            bsd_purpose = bsd_purpose.Replace("Delivery", "2");
                        if (bsd_purpose.Contains("Invoice"))
                            bsd_purpose = bsd_purpose.Replace("Invoice", "1");
                        if (bsd_purpose.Contains("Other"))
                            bsd_purpose = bsd_purpose.Replace("Other", "8");
                        if (bsd_purpose.Contains("Billing Address"))
                            bsd_purpose = bsd_purpose.Replace("Billing Address", "7");

                        // throw new Exception(bsd_purpose);
                    }
                    if (entityAddress.HasValue("bsd_key"))
                    {
                        bsd_key = entityAddress["bsd_key"].ToString();
                    }
                    //if (entityAddress.HasValue("bsd_region"))
                    //{
                    //    bsd_region = entityAddress["bsd_region"].ToString();
                    //}
                    if (entityAddress.HasValue("bsd_country"))
                    {
                        EntityReference rf_Entity = (EntityReference)entityAddress["bsd_country"];
                        Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_id"));
                        if (Entity.HasValue("bsd_id"))
                            bsd_country = Entity["bsd_id"].ToString();
                    }
                    if (entityAddress.HasValue("bsd_province"))
                    {
                        EntityReference rf_Entity = (EntityReference)entityAddress["bsd_province"];
                        Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                        if (Entity.HasValue("bsd_name"))
                            bsd_province = Entity["bsd_name"].ToString();
                    }
                    if (entityAddress.HasValue("bsd_district"))
                    {
                        EntityReference rf_Entity = (EntityReference)entityAddress["bsd_district"];
                        Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                        if (Entity.HasValue("bsd_name"))
                            bsd_district = Entity["bsd_name"].ToString();
                    }
                    if (entityAddress.HasValue("bsd_ward"))
                    {
                        EntityReference rf_Entity = (EntityReference)entityAddress["bsd_ward"];
                        Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                        if (Entity.HasValue("bsd_name"))
                            bsd_ward = Entity["bsd_name"].ToString();
                    }
                    if (entityAddress.HasValue("bsd_name"))
                    {
                        bsd_street = entityAddress["bsd_name"].ToString();
                    }
                    // throw new Exception(target.Id.ToString());
                    if (!bsd_purpose.Contains("1")) bsd_taxregistration = "";
                    string s_Result = client.BHS_InsertAddress(contextService, target.Id.ToString(), bsd_account, bsd_street, bsd_ward, bsd_district, bsd_province, bsd_country, bool.Parse(bsd_key), bsd_purpose, bsd_taxregistration);
                    if (s_Result != "0")
                        throw new Exception(s_Result);

                    #endregion

                    #endregion
                    #endregion
                }
                if (target.LogicalName == "account")
                {
                 
                    #region account
                    decimal bsd_creditlimit = 0m;
                    bool bsd_accounttype = false;
                    Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    string accountnumber = "", name = "", parentaccountname = "", bsd_paymentterm = "", bsd_paymentmethod = "", parentaccountid = "", bsd_taxregistration = "", bsd_saletaxgroup = "", bsd_accountgroup = "", transactioncurrencyid = "";
                    if (entity.HasValue("accountnumber")) accountnumber = entity["accountnumber"].ToString();
                    if (entity.HasValue("name")) name = entity["name"].ToString();
                    if (entity.HasValue("bsd_taxregistration")) bsd_taxregistration = entity["bsd_taxregistration"].ToString();
                    if (entity.HasValue("bsd_creditlimit"))
                    {

                        bsd_creditlimit = (decimal)entity["bsd_creditlimit"];
                    }
                    if (entity.HasValue("bsd_accountgroup"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["bsd_accountgroup"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                        if (en.HasValue("bsd_name"))
                            bsd_accountgroup = en["bsd_name"].ToString();
                    }
                    if (entity.HasValue("bsd_accounttype"))
                    {
                        if (((OptionSetValue)entity["bsd_accounttype"]).Value == 861450001) bsd_accounttype = true;
                    }
                    if (entity.HasValue("transactioncurrencyid"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["transactioncurrencyid"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("isocurrencycode"));
                        if (en.HasValue("isocurrencycode"))
                            transactioncurrencyid = en["isocurrencycode"].ToString();
                    }
                    if (entity.HasValue("bsd_saletaxgroup"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["bsd_saletaxgroup"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_salestaxgroup"));
                        if (en.HasValue("bsd_salestaxgroup"))
                            bsd_saletaxgroup = en["bsd_salestaxgroup"].ToString();
                    }
                    if (entity.HasValue("bsd_paymentterm"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["bsd_paymentterm"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_termofpayment"));
                        if (en.HasValue("bsd_termofpayment"))
                            bsd_paymentterm = en["bsd_termofpayment"].ToString();
                    }
                    if (entity.HasValue("bsd_paymentmethod"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["bsd_paymentmethod"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_methodofpayment"));
                        if (en.HasValue("bsd_methodofpayment"))
                            bsd_paymentmethod = en["bsd_methodofpayment"].ToString();
                    }
                    if (entity.HasValue("parentaccountid"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["parentaccountid"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                        if (en.HasValue("accountnumber"))
                            parentaccountid = en["accountnumber"].ToString();
                        if (en.HasValue("name"))
                            parentaccountname = en["name"].ToString();
                    }
                    if (string.IsNullOrEmpty(bsd_accountgroup))
                    {
                        bsd_accountgroup = this.getAccountgroup();
                    }
                    if (!string.IsNullOrEmpty(accountnumber) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(transactioncurrencyid) && !string.IsNullOrEmpty(bsd_saletaxgroup) && !string.IsNullOrEmpty(bsd_accountgroup))
                    {
                        string s = "";
                        //if (bsd_accounttype != 861450001)
                        //{
                        s = client.BHS_CustAccount_InsertUpdate(contextService, accountnumber, name, bsd_accountgroup, transactioncurrencyid, bsd_taxregistration, bsd_saletaxgroup, parentaccountid, parentaccountname, bsd_paymentterm, bsd_paymentmethod, bsd_creditlimit, bsd_accounttype);
                        //}
                        //else
                        //{
                        //    try
                        //    {
                        //        s = client.BHS_VendAccount_InsertUpdate(contextService, accountnumber, name, "ShipperCRM", transactioncurrencyid, bsd_saletaxgroup, parentaccountid, parentaccountname, bsd_paymentterm, bsd_paymentmethod);
                        //    }
                        //    catch (Exception ex1)
                        //    {
                        //        throw new Exception("error: " + ex1.Message);
                        //    }
                        //}
                        if (!s.Contains("successful"))
                        {
                            throw new Exception(s);
                        }
                    }
                    #endregion
                }
                if (target.LogicalName == "bsd_returnreasoncode")
                {
                    //throw new Exception("okie");
                    #region bsd_returnreasoncode
                    Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    string bsd_code = "", bsd_name = "", bsd_returnreasongroup = "";
                    if (entity.HasValue("bsd_name")) bsd_name = entity["bsd_name"].ToString();
                    if (entity.HasValue("bsd_code")) bsd_code = entity["bsd_code"].ToString();
                    if (entity.HasValue("bsd_returnreasongroup"))
                    {
                        EntityReference rf_Entity = (EntityReference)entity["bsd_returnreasongroup"];
                        Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                        if (en.HasValue("bsd_code"))
                            bsd_returnreasongroup = en["bsd_code"].ToString();
                    }
                    if (!string.IsNullOrEmpty(bsd_name) && !string.IsNullOrEmpty(bsd_code))
                    {
                        //Call service
                        string s = client.BHS_ResonCode_InsertUpdate(contextService, bsd_code.Trim(), bsd_returnreasongroup.Trim(), bsd_name.Trim());
                        if (!s.Contains("successful"))
                        {
                            throw new Exception(s);
                        }
                    }
                    #endregion
                }
                if (target.LogicalName == "bsd_returnreasongroup")
                {
                    #region bsd_returnreasongroup
                    Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    string bsd_code = "", bsd_name = "";
                    if (entity.HasValue("bsd_name")) bsd_name = entity["bsd_name"].ToString();
                    if (entity.HasValue("bsd_code")) bsd_code = entity["bsd_code"].ToString();
                    if (!string.IsNullOrEmpty(bsd_name) && !string.IsNullOrEmpty(bsd_code))
                    {
                        //Call service
                        string s = client.BHS_ResonCodeGroup_InsertUpdate(contextService, bsd_code, bsd_name);
                        if (!s.Contains("successful"))
                        {
                            throw new Exception(s);
                        }
                    }
                    #endregion
                }
                #endregion
            }
            else if (myService.context.MessageName == "Update")
            {
                #region Update
                Entity target = (Entity)context.InputParameters["Target"];
                if (target.LogicalName == "bsd_bankaccount")
                {
                    if (!target.HasValue("statecode"))
                    {
                        #region bankaccount
                        Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        string bsd_bankid = "", bsd_account = "", bsd_bankgroup = "", bsd_brand = "", bsd_swiftcode = "", bsd_name = "";
                        if (entity.HasValue("bsd_name")) bsd_name = entity["bsd_name"].ToString();
                        if (entity.HasValue("bsd_bankid")) bsd_bankid = entity["bsd_bankid"].ToString();
                        if (entity.HasValue("bsd_brand")) bsd_brand = entity["bsd_brand"].ToString();
                        if (entity.HasValue("bsd_swiftcode")) bsd_swiftcode = entity["bsd_swiftcode"].ToString();
                        if (entity.HasValue("bsd_account"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["bsd_account"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("accountnumber"));
                            if (en.HasValue("accountnumber"))
                                bsd_account = en["accountnumber"].ToString();
                        }
                        if (entity.HasValue("bsd_bankgroup"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["bsd_bankgroup"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                            if (en.HasValue("bsd_name"))
                                bsd_bankgroup = en["bsd_name"].ToString();
                        }
                        if (!string.IsNullOrEmpty(bsd_bankgroup) && !string.IsNullOrEmpty(bsd_account) && !string.IsNullOrEmpty(bsd_brand) && !string.IsNullOrEmpty(bsd_bankid))
                        {
                            string s = client.BHS_BankAccount_InsertUpdate(contextService, bsd_name, bsd_account, bsd_bankgroup + " - " + bsd_brand, "", bsd_swiftcode);
                            if (!s.Contains("successful"))
                            {
                                throw new Exception(s);
                            }
                        }
                        //else
                        //{
                        //    throw new Exception("null value");
                        //}
                        #endregion
                    }
                }
                if (target.LogicalName == "bsd_groupaccount")
                {
                    if (!target.HasValue("statecode"))
                    {
                        #region bsd_groupaccount
                        Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        string bsd_name = "", bsd_accountname = "";
                        if (entity.HasValue("bsd_accountname")) bsd_accountname = entity["bsd_accountname"].ToString();
                        if (entity.HasValue("bsd_name")) bsd_name = entity["bsd_name"].ToString();
                        if (!string.IsNullOrEmpty(bsd_accountname) && !string.IsNullOrEmpty(bsd_name))
                        {
                            //Call service insert
                            string s = client.BHS_CustGroup_InsertUpdate(contextService, bsd_name, bsd_accountname);
                            if (!s.Contains("successful"))
                            {
                                throw new Exception(s);
                            }
                        }
                        #endregion
                    }
                }
                if (target.LogicalName == "bsd_address")
                {
                    // throw new Exception("okie");
                    if (!target.HasValue("statecode"))
                    {
                        #region bsd_address
                        Entity entityAddress = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        int bsd_accounttype = 0;
                        string bsd_purpose = "", bsd_country = "", bsd_key = "", bsd_province = "", bsd_district = "", bsd_street = "", bsd_ward = "", bsd_account = "", bsd_taxregistration = "";
                        #region set Value

                        if (entityAddress.HasValue("bsd_account"))
                        {
                            //bsd_shiptoaccount = suborder["bsd_shiptoaccount"].ToString();
                            EntityReference rf_Entity = (EntityReference)entityAddress["bsd_account"];
                            Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                            if (Entity.HasValue("accountnumber"))
                            {
                                bsd_account = Entity["accountnumber"].ToString();
                                //  checkAddress = client.BHS_ValidateAddressAccount(contextService, bsd_account, target.Id.ToString());
                            }
                            if (Entity.HasValue("bsd_taxregistration"))
                            {
                                bsd_taxregistration = Entity["bsd_taxregistration"].ToString();
                            }
                            if (Entity.HasValue("bsd_accounttype"))
                            {
                                bsd_accounttype = ((OptionSetValue)Entity["bsd_accounttype"]).Value;
                                //if (bsd_accounttype == 861450001)
                                //{
                                //    throw new Exception("Accout type shipper address");
                                //}
                            }
                        }
                        #region Insert Address

                        if (entityAddress.HasValue("bsd_purpose_tmpvalue"))
                        {
                            bsd_purpose = entityAddress["bsd_purpose_tmpvalue"].ToString();
                            if (bsd_purpose.Contains("Business"))
                                bsd_purpose = bsd_purpose.Replace("Business", "9");
                            if (bsd_purpose.Contains("Delivery"))
                                bsd_purpose = bsd_purpose.Replace("Delivery", "2");
                            if (bsd_purpose.Contains("Invoice"))
                                bsd_purpose = bsd_purpose.Replace("Invoice", "1");
                            if (bsd_purpose.Contains("Other"))
                                bsd_purpose = bsd_purpose.Replace("Other", "8");
                            if (bsd_purpose.Contains("Billing Address"))
                                bsd_purpose = bsd_purpose.Replace("Billing Address", "7");
                            // throw new Exception(bsd_purpose);
                        }
                        if (entityAddress.HasValue("bsd_key"))
                        {
                            bsd_key = entityAddress["bsd_key"].ToString();
                        }
                        //if (entityAddress.HasValue("bsd_region"))
                        //{
                        //    bsd_region = entityAddress["bsd_region"].ToString();
                        //}
                        if (entityAddress.HasValue("bsd_country"))
                        {
                            EntityReference rf_Entity = (EntityReference)entityAddress["bsd_country"];
                            Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_id"));
                            if (Entity.HasValue("bsd_id"))
                                bsd_country = Entity["bsd_id"].ToString();
                        }
                        if (entityAddress.HasValue("bsd_province"))
                        {
                            EntityReference rf_Entity = (EntityReference)entityAddress["bsd_province"];
                            Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                            if (Entity.HasValue("bsd_name"))
                                bsd_province = Entity["bsd_name"].ToString();
                        }
                        if (entityAddress.HasValue("bsd_district"))
                        {
                            EntityReference rf_Entity = (EntityReference)entityAddress["bsd_district"];
                            Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                            if (Entity.HasValue("bsd_name"))
                                bsd_district = Entity["bsd_name"].ToString();
                        }
                        if (entityAddress.HasValue("bsd_ward"))
                        {
                            EntityReference rf_Entity = (EntityReference)entityAddress["bsd_ward"];
                            Entity Entity = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                            if (Entity.HasValue("bsd_name"))
                                bsd_ward = Entity["bsd_name"].ToString();
                        }
                        if (entityAddress.HasValue("bsd_name"))
                        {
                            bsd_street = entityAddress["bsd_name"].ToString();
                        }
                        // throw new Exception(target.Id.ToString());
                        if (!bsd_purpose.Contains("1")) bsd_taxregistration = "";
                        string s_Result = client.BHS_InsertAddress(contextService, target.Id.ToString(), bsd_account, bsd_street, bsd_ward, bsd_district, bsd_province, bsd_country, bool.Parse(bsd_key), bsd_purpose, bsd_taxregistration);
                        if (s_Result != "0")
                            throw new Exception(s_Result);

                        #endregion

                        #endregion
                        #endregion
                    }
                }
                if (target.LogicalName == "account")
                {
                    if (!target.HasValue("statecode"))
                    {
                        #region account
                        decimal bsd_creditlimit = 0m;
                        bool bsd_accounttype = false;
                        Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        string accountnumber = "", name = "", parentaccountname = "", bsd_paymentterm = "", bsd_paymentmethod = "", parentaccountid = "", bsd_taxregistration = "", bsd_saletaxgroup = "", bsd_accountgroup = "", transactioncurrencyid = "";
                        if (entity.HasValue("accountnumber")) accountnumber = entity["accountnumber"].ToString();
                        if (entity.HasValue("name")) name = entity["name"].ToString();
                        if (entity.HasValue("bsd_taxregistration")) bsd_taxregistration = entity["bsd_taxregistration"].ToString();
                        if (entity.HasValue("bsd_creditlimit"))
                        {

                            bsd_creditlimit = (decimal)entity["bsd_creditlimit"];
                        }
                        if (entity.HasValue("bsd_accountgroup"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["bsd_accountgroup"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_name"));
                            if (en.HasValue("bsd_name"))
                                bsd_accountgroup = en["bsd_name"].ToString();
                        }
                        if (entity.HasValue("bsd_accounttype"))
                        {
                            if (((OptionSetValue)entity["bsd_accounttype"]).Value == 861450001) bsd_accounttype = true;
                        }
                        if (entity.HasValue("transactioncurrencyid"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["transactioncurrencyid"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("isocurrencycode"));
                            if (en.HasValue("isocurrencycode"))
                                transactioncurrencyid = en["isocurrencycode"].ToString();
                        }
                        if (entity.HasValue("bsd_saletaxgroup"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["bsd_saletaxgroup"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_salestaxgroup"));
                            if (en.HasValue("bsd_salestaxgroup"))
                                bsd_saletaxgroup = en["bsd_salestaxgroup"].ToString();
                        }
                        if (entity.HasValue("bsd_paymentterm"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["bsd_paymentterm"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_termofpayment"));
                            if (en.HasValue("bsd_termofpayment"))
                                bsd_paymentterm = en["bsd_termofpayment"].ToString();
                        }
                        if (entity.HasValue("bsd_paymentmethod"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["bsd_paymentmethod"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_methodofpayment"));
                            if (en.HasValue("bsd_methodofpayment"))
                                bsd_paymentmethod = en["bsd_methodofpayment"].ToString();
                        }
                        if (entity.HasValue("parentaccountid"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["parentaccountid"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet(true));
                            if (en.HasValue("accountnumber"))
                                parentaccountid = en["accountnumber"].ToString();
                            if (en.HasValue("name"))
                                parentaccountname = en["name"].ToString();
                        }
                        if (string.IsNullOrEmpty(bsd_accountgroup))
                        {
                            bsd_accountgroup = this.getAccountgroup();
                        }
                        if (!target.HasValue("address2_city"))
                        {
                            if (!string.IsNullOrEmpty(accountnumber) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(transactioncurrencyid) && !string.IsNullOrEmpty(bsd_saletaxgroup) && !string.IsNullOrEmpty(bsd_accountgroup))
                            {
                                string s = "";
                                //if (bsd_accounttype != 861450001)
                                //{
                                s = client.BHS_CustAccount_InsertUpdate(contextService, accountnumber, name, bsd_accountgroup, transactioncurrencyid, bsd_taxregistration, bsd_saletaxgroup, parentaccountid, parentaccountname, bsd_paymentterm, bsd_paymentmethod, bsd_creditlimit, bsd_accounttype);
                                //}
                                //else
                                //{
                                //    try
                                //    {
                                //        s = client.BHS_VendAccount_InsertUpdate(contextService, accountnumber, name, "ShipperCRM", transactioncurrencyid, bsd_saletaxgroup, parentaccountid, parentaccountname, bsd_paymentterm, bsd_paymentmethod);
                                //    }
                                //    catch (Exception ex1)
                                //    {
                                //        throw new Exception("error: " + ex1.Message);
                                //    }
                                //}
                                if (!s.Contains("successful"))
                                {
                                    throw new Exception(s);
                                }

                            }
                            else throw new Exception("data null");
                        }
                        #endregion
                    }
                }
                if (target.LogicalName == "bsd_returnreasoncode")
                {
                    if (!target.HasValue("statecode"))
                    {
                        #region bsd_returnreasoncode
                        Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        string bsd_code = "", bsd_name = "", bsd_returnreasongroup = "";
                        if (entity.HasValue("bsd_name")) bsd_name = entity["bsd_name"].ToString();
                        if (entity.HasValue("bsd_code")) bsd_code = entity["bsd_code"].ToString();
                        if (entity.HasValue("bsd_returnreasongroup"))
                        {
                            EntityReference rf_Entity = (EntityReference)entity["bsd_returnreasongroup"];
                            Entity en = myService.service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                            if (en.HasValue("bsd_code"))
                                bsd_returnreasongroup = en["bsd_code"].ToString();
                        }
                        if (!string.IsNullOrEmpty(bsd_name) && !string.IsNullOrEmpty(bsd_code))
                        {
                            //Call service
                            string s = client.BHS_ResonCode_InsertUpdate(contextService, bsd_code.Trim(), bsd_returnreasongroup.Trim(), bsd_name.Trim());
                            if (!s.Contains("successful"))
                            {
                                throw new Exception(s);
                            }
                        }
                        #endregion
                    }
                }
                if (target.LogicalName == "bsd_returnreasongroup")
                {
                    if (!target.HasValue("statecode"))
                    {
                        #region bsd_returnreasongroup
                        Entity entity = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        string bsd_code = "", bsd_name = "";
                        if (entity.HasValue("bsd_name")) bsd_name = entity["bsd_name"].ToString();
                        if (entity.HasValue("bsd_code")) bsd_code = entity["bsd_code"].ToString();
                        if (!string.IsNullOrEmpty(bsd_name) && !string.IsNullOrEmpty(bsd_code))
                        {
                            //Call service
                            string s = client.BHS_ResonCodeGroup_InsertUpdate(contextService, bsd_code, bsd_name);
                            if (!s.Contains("successful"))
                            {
                                throw new Exception(s);
                            }
                        }
                        #endregion
                    }
                }
                #endregion
            }


        }
        public bool getAccount(Guid AccountGroupId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                <attribute name='name' />
                                <attribute name='primarycontactid' />
                                <order attribute='name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='bsd_accountgroup' operator='eq' uiname='A' uitype='bsd_groupaccount' value='{84C2FD71-0A33-E711-9413-000C2958218C}' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection list_bhstradingAccountb2b = myService.service.RetrieveMultiple(new FetchExpression(xml));
            return list_bhstradingAccountb2b.Entities.Any();
        }
        public string getAccountgroup()
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false' count='1'>
                          <entity name='bsd_groupaccount'>
                            <attribute name='bsd_groupaccountid' />
                            <attribute name='bsd_name' />
                            <attribute name='createdon' />
                            <order attribute='bsd_name' descending='false' />
                          </entity>
                        </fetch>";
            EntityCollection list_bhstradingAccountb2b = myService.service.RetrieveMultiple(new FetchExpression(xml));
            return list_bhstradingAccountb2b.Entities.First()["bsd_name"].ToString();
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
