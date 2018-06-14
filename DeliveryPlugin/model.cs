using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;

namespace DeliveryPlugin
{
    public class model : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {

            
            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1) return;
        }
    }
}
