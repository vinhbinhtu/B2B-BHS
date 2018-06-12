using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Action_Checkcreditlimit
{
    public class Main : IPlugin
    {
        private IOrganizationServiceFactory factory;
        public IOrganizationService service { get; set; }
        public IPluginExecutionContext context { get; set; }
        public string _userName = "";
        public string _passWord = "";
        public string _company = "";
        public string _port = "";
        public string _domain = "";
        public string Return = "";
        public string bsd_customercode = "";
        public void Execute(IServiceProvider serviceProvider)
        {

            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);
            if (context.MessageName == "bsd_Check_creditlimit")
            {
                int i = 0;
                try
                {
                    bool connect_ax = getConfigdefaultconnectax();
                    if (connect_ax == false)
                    {
                        throw new Exception("Thiếu khai báo connect ax");
                    }
                    EntityReference target = (EntityReference)context.InputParameters["Target"];
                    #region KHai báo service AX
                    NetTcpBinding binding = new NetTcpBinding();
                    //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                    binding.Name = "NetTcpBinding_BHS_BSD_CRMSERVICEAXService";
                    EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));
                    ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                    client.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                    client.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                    client.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                    ServiceReferenceAIF.CallContext contextService = new ServiceReferenceAIF.CallContext() { Company = _company };
                    #endregion

                    Entity suborder = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    if (suborder.HasValue("bsd_invoiceaccount"))
                    {
                        bsd_customercode = suborder["bsd_invoiceaccount"].ToString().Trim();
                        decimal s_Result = client.BHS_ValidateCustOpenBalanceOne(contextService, bsd_customercode);
                        // s_Result = 100000000;
                        Entity suborder_Update = new Entity(suborder.LogicalName, suborder.Id);

                        i = 5;
                        suborder_Update["bsd_olddebt"] = new Money(s_Result);
                        suborder_Update["bsd_olddebttext"] = String.Format("{0:#,###.####}", (new Money(s_Result)).Value);
                        i = 6;
                        decimal bsd_totalcurrencyexchange = ((Money)suborder["bsd_totalcurrencyexchange"]).Value;
                        suborder_Update["bsd_newdebt"] = new Money(s_Result + bsd_totalcurrencyexchange);
                        suborder_Update["bsd_newdebttext"] = String.Format("{0:#,###.####}", new Money(s_Result + bsd_totalcurrencyexchange).Value);
                        i = 7;
                        suborder_Update["bsd_submittedgrandtotal"] = new Money(s_Result + bsd_totalcurrencyexchange);
                        suborder_Update["bsd_submittedgrandtotaltext"] = String.Format("{0:#,###.####}", (new Money(s_Result + bsd_totalcurrencyexchange)).Value);
                        i = 8;
                        decimal bsd_creditlimit = 0m;
                        if (suborder.HasValue("bsd_creditlimit")) bsd_creditlimit = ((Money)suborder["bsd_creditlimit"]).Value;
                        //throw new Exception(s_Result.ToString() + "----" + suborder["bsd_totalcurrencyexchange"].ToString() + "-----" + ((decimal)suborder["bsd_totalcurrencyexchange"]).ToString());
                        if ((s_Result + bsd_totalcurrencyexchange) > bsd_creditlimit)
                        {
                            Return = "Customer exceeds credit limit";
                        }
                        else if ((s_Result + bsd_totalcurrencyexchange) <= bsd_creditlimit)
                        {

                            Return = "success";
                        }
                        i = 9;
                        service.Update(suborder_Update);
                        i = 16;

                    }
                    else
                    {
                        Return = "success";
                    }
                    // throw new Exception(Return);
                    context.OutputParameters["Return"] = Return;
                }
                catch (Exception ex)
                {
                   // context.OutputParameters["Return"] = "Customer exceeds credit limit";
                    throw new Exception("Error service AX check credit limit: " + ex.Message + i.ToString());
                }
            }
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
}
