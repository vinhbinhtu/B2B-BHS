using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Action_CancelSuborderChielfOfficer
{
    public class Main : IPlugin
    {
        private IOrganizationServiceFactory factory;
        public IOrganizationService service { get; set; }
        public IPluginExecutionContext context { get; set; }
        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);
            try
            {
                bool connect_ax = getConfigdefaultconnectax();
                if (connect_ax == false)
                {
                    throw new Exception("Thiếu khai báo connect ax");
                }
           
                //vinhlh 26-12-2017
                #region KHai báo service AX
                NetTcpBinding binding = new NetTcpBinding();
                //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                binding.Name = "NetTcpBinding_BHS_BSD_CRMSERVICEAXService";
                EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://" + Utilites._port + "/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));
                ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                client.ClientCredentials.Windows.ClientCredential.Domain = Utilites._domain;
                client.ClientCredentials.Windows.ClientCredential.UserName = Utilites._userName;
                client.ClientCredentials.Windows.ClientCredential.Password = Utilites._passWord;
                ServiceReferenceAIF.CallContext contextService = new ServiceReferenceAIF.CallContext() { Company = Utilites._company };
                #endregion

                #region bsd_Action_CloseSubOrder
                EntityReference rf;
                EntityReference target = (EntityReference)context.InputParameters["Target"];
                Entity suborder = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));


          

                SetStateRequest setStateRequest = new SetStateRequest()
                {
                    EntityMoniker = new EntityReference
                    {
                        Id = suborder.Id,
                        LogicalName = suborder.LogicalName
                    },
                    State = new OptionSetValue(1),
                    //B2C
                    //Status = new OptionSetValue(861450009)
                    //B2B
                    Status = new OptionSetValue(861450007)

                };
                service.Execute(setStateRequest);
                //Đơn hàng là đơn hàng trả
                if (((OptionSetValue)suborder["bsd_type"]).Value == 861450004 && suborder.HasValue("bsd_returnorder"))
                {
                    Entity returnorder = service.Retrieve("bsd_returnorder", ((EntityReference)suborder["bsd_returnorder"]).Id, new ColumnSet(true));
                    returnorder["bsd_description"] = "Return Order had been canceled";
                    service.Update(returnorder);
                    SetStateRequest setStateRequestreturn = new SetStateRequest()
                    {
                        EntityMoniker = new EntityReference
                        {
                            Id = returnorder.Id,
                            LogicalName = returnorder.LogicalName
                        },
                        State = new OptionSetValue(1),
                        Status = new OptionSetValue(2)

                    };
                    service.Execute(setStateRequestreturn);
                }
                //
                #region 27-12-2017 call service AX
                if (suborder.Contains("bsd_suborderax") && suborder["bsd_suborderax"] != null)
                {
                    string s_Result = client.BHS_CancelSalesOrder(contextService, suborder["bsd_suborderax"].ToString().Trim());
                    if (s_Result != "Success") throw new Exception("AX " + s_Result);
                }
                #endregion
                context.OutputParameters["Return"] = "success";
              
                #endregion

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
                Utilites._userName = configdefault["bsd_usernameax"].ToString();
                Utilites._passWord = configdefault["bsd_passwordax"].ToString();
                Utilites._company = configdefault["bsd_company"].ToString();
                Utilites._port = configdefault["bsd_portax"].ToString();
                Utilites._domain = configdefault["bsd_domain"].ToString();
                return true;
            }
            else
            {
                return false;
            }

        }

    }
}
