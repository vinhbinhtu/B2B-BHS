using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_TransferAX_None
{
    public class Main : IPlugin
    {
        private IOrganizationServiceFactory factory;
        public IOrganizationService service { get; set; }
        public IPluginExecutionContext context { get; set; }

        // public IOrganizationService b2bservice { get; set; }

        public void Execute(IServiceProvider serviceProvider)
        {
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
                throw new Exception("okie");
            }
        }
    }
}
