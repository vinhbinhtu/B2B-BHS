using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

namespace Plugin.Service
{
    public enum Message { Create, Update, Delete, Null };
    public class MyService
    {
        private IOrganizationServiceFactory factory;
        public IOrganizationService service { get; set; }
        public IPluginExecutionContext context { get; set; }
        public IServiceProvider serviceProvider { private get; set; }
        public Message ContextMessage
        {
            get
            {
                if (context.MessageName == "Create")
                {
                    return Message.Create;
                }
                else if (context.MessageName == "Update")
                {
                    return Message.Update;
                }
                else if (context.MessageName == "Delete")
                {
                    return Message.Delete;
                }
                else
                {
                    return Message.Null;
                }
            }
            set
            {
            }
        }
        public MyService(IServiceProvider _serviceProvider, bool start = false)
        {
            serviceProvider = _serviceProvider;
            context = _serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            if (start) this.StartService();
        }
        public void StartService()
        {
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);
        }

        public ITracingService GetTracingService()
        {
            return (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        }
        public Entity getTarget()
        {
            try
            {
                if (context.InputParameters["Target"] != null && context.InputParameters["Target"] is Entity)
                {
                    return context.InputParameters["Target"] as Entity;
                }
            }
            catch (Exception)
            { }
            return null;
        }



        public EntityReference getTargetEntityReference()
        {
            try
            {
                if (context.InputParameters["Target"] != null && context.InputParameters["Target"] is EntityReference)
                {
                    return context.InputParameters["Target"] as EntityReference;
                }
            }
            catch (Exception)
            { }
            return null;
        }

        public EntityCollection RetrieveOneCondition(string entityName, string column, string[] columnset, object value)
        {
            QueryExpression q = new QueryExpression(entityName);
            q.ColumnSet = new ColumnSet(columnset);
            FilterExpression filter = new FilterExpression();
            filter.AddCondition(new ConditionExpression(column, ConditionOperator.Equal, value));
            q.Criteria = filter;
            EntityCollection entc = service.RetrieveMultiple(q);
            return entc;
        }
        public EntityCollection RetrieveOneCondition(string entityName, string column, object value)
        {
            QueryExpression q = new QueryExpression(entityName);
            q.ColumnSet = new ColumnSet(true);
            FilterExpression filter = new FilterExpression();
            filter.AddCondition(new ConditionExpression(column, ConditionOperator.Equal, value));
            q.Criteria = filter;
            EntityCollection entc = service.RetrieveMultiple(q);
            return entc;
        }
        public EntityCollection RetrieveTwoCondition(string entityName, string[] columnset, string[] column, object[] value, LogicalOperator op)
        {
            QueryExpression q = new QueryExpression(entityName);
            q.ColumnSet = new ColumnSet(columnset);
            FilterExpression filter = new FilterExpression(op);
            for (int i = 0; i < column.Length; i++)
            {
                filter.AddCondition(new ConditionExpression(column[i], ConditionOperator.Equal, value[i]));
            }
            q.Criteria = filter;
            EntityCollection entc = service.RetrieveMultiple(q);
            return entc;
        }
        public EntityCollection RetrieveTwoCondition(string entityName, ColumnSet columnset, string[] column, object[] value, LogicalOperator op)
        {
            QueryExpression q = new QueryExpression(entityName);
            q.ColumnSet = columnset;
            FilterExpression filter = new FilterExpression(op);
            for (int i = 0; i < column.Length; i++)
            {
                filter.AddCondition(new ConditionExpression(column[i], ConditionOperator.Equal, value[i]));
            }
            q.Criteria = filter;
            EntityCollection entc = service.RetrieveMultiple(q);
            return entc;
        }
        public void Update(Entity entity)
        {
            service.Update(entity);
        }
        public void Delete(string localname, Guid id)
        {
            service.Delete(localname, id);
        }
        public DateTime RetrieveLocalTimeFromUTCTime(DateTime utcTime, IOrganizationService service)
        {

            int? timeZoneCode = RetrieveCurrentUsersSettings(service);


            if (!timeZoneCode.HasValue)
                throw new Exception("Can't find time zone code");



            var request = new LocalTimeFromUtcTimeRequest
            {
                TimeZoneCode = timeZoneCode.Value,
                UtcTime = utcTime.ToUniversalTime()
            };

            var response = (LocalTimeFromUtcTimeResponse)service.Execute(request);

            return response.LocalTime;
            //var utcTime = utcTime.ToString("MM/dd/yyyy HH:mm:ss");
            //var localDateOnly = response.LocalTime.ToString("dd-MM-yyyy");
        }

        private int? RetrieveCurrentUsersSettings(IOrganizationService service)
        {
            var currentUserSettings = service.RetrieveMultiple(
            new QueryExpression("usersettings")
            {
                ColumnSet = new ColumnSet("localeid", "timezonecode"),
                Criteria = new FilterExpression
                {
                    Conditions =
            {
            new ConditionExpression("systemuserid", ConditionOperator.EqualUserId)
            }
                }
            }).Entities[0].ToEntity<Entity>();

            return (int?)currentUserSettings.Attributes["timezonecode"];
        }


        public void SetState(Guid Id, string LogicalName, int state, int status)
        {
            service.Execute(new SetStateRequest()
            {
                EntityMoniker = new EntityReference
                {
                    Id = Id,
                    LogicalName = LogicalName
                },
                State = new OptionSetValue(state),
                Status = new OptionSetValue(status)
            });
        }
    }
}
