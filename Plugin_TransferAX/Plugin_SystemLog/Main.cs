using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Plugin_SystemLog.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SystemLog
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
            if (context.MessageName == "Create")
            {
                Entity target = (Entity)context.InputParameters["Target"];
                try
                {
                    if (!target.HasValue("bsd_object")) throw new Exception("Object is not null");
                    if (!target.HasValue("bsd_method")) throw new Exception("Method is not null");
                    if (!target.HasValue("bsd_name")) throw new Exception("Entity name is not null");
                    string method = target["bsd_method"].ToString().Trim();
                    string entityName = target["bsd_name"].ToString().Trim();
                    if (entityName.ToLower() == "bsd_transferorder")
                    {
                        if (method.ToLower() == "create")
                        {
                            TransferOrder obj = TransferOrder.JsonParse(target["bsd_object"].ToString());
                            TransferOrder.Create(obj, service);
                        }
                        else if (method.ToLower() == "update")
                        {
                            TransferOrder obj = TransferOrder.JsonParse(target["bsd_object"].ToString());
                            TransferOrder.Update(obj, service);
                        }
                    }
                    else if (entityName.ToLower() == "bsd_size")
                    {
                        #region Example Size
                        if (method.ToLower() == "create")
                        {
                            Size obj = Size.JsonParse(target["bsd_object"].ToString());
                            Size.Create(obj, service);
                        }
                        else if (method.ToLower() == "update")
                        {
                            Size obj = Size.JsonParse(target["bsd_object"].ToString());
                            Size.Create(obj, service);
                        }
                        else if (method.ToLower() == "delete")
                        {
                            Size obj = Size.JsonParse(target["bsd_object"].ToString());
                            Size.Create(obj, service);
                        }
                        #endregion
                    }
                    else if (entityName.ToLower() == "bsd_deliverybill")
                    {
                        if (method.ToLower() == "create")
                        {
                            GoodsIssueNote obj = GoodsIssueNote.JsonParse(target["bsd_object"].ToString());
                            GoodsIssueNote.Create(obj, service);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("System Log: " + ex.Message);
                }
            }
        }

    }
}
