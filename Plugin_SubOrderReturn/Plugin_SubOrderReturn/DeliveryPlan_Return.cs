using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;

namespace Plugin_SubOrderReturn
{
    public class DeliveryPlan_Return : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (myService.context.Depth > 1)
                return;

            ITracingService tracing = myService.GetTracingService();
            if (myService.context.MessageName == "bsd_Action_CreateDeliveryPlanReturnType")
            {
                string Issubmit = context.InputParameters["Issubmit"].ToString();
                // throw new Exception("okie");
                EntityReference target = myService.getTargetEntityReference();
                myService.StartService();
                Entity suborder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

                int subtype = ((OptionSetValue)suborder["bsd_type"]).Value;
                if (subtype == 861450004)
                {
                    string trace = "0";
                    try
                    {
                        #region
                        EntityCollection list_deliveryplan = myService.RetrieveOneCondition("bsd_deliveryplan", "bsd_suborder", target.Id);
                        if (list_deliveryplan.Entities.Any()) throw new Exception("Đã tạo rồi1");

                        Entity deliveryplan = new Entity("bsd_deliveryplan");
                        deliveryplan["bsd_suborder"] = target;
                        if (suborder.HasValue("bsd_type"))
                            deliveryplan["bsd_type"] = suborder["bsd_type"];
                        if (suborder.HasValue("bsd_transportation"))
                            deliveryplan["bsd_transportation"] = suborder["bsd_transportation"];
                        //throw new Exception("OK " + suborder["bsd_transportation"].ToString());
                        NewEntity ne = new NewEntity(suborder, deliveryplan);
                        trace = "1";
                        ne.Set("bsd_order", true);
                        ne.Set("bsd_quote", true);
                        ne.Set("bsd_potentialcustomer", true);
                        ne.Set("bsd_historyreceiptcustomer", true);
                        //ne.Set("bsd_timeship", true);
                        ne.Set("bsd_addresscustomeraccount", true);
                        ne.Set("bsd_telephone", true);
                        ne.Set("bsd_contact", true);
                        ne.Set("bsd_requestedshipdate", true);
                        ne.Set("bsd_requestedreceiptdate", true);
                        ne.Set("bsd_confirmedreceiptdate", true);
                        ne.Set("bsd_warehousefrom", true);
                        ne.Set("bsd_shiptoaddress", true);
                        ne.Set("bsd_site", true);
                        ne.Set("bsd_siteaddress", true);
                        trace = "2";
                        Guid deliveryplan_id = myService.service.Create(deliveryplan);
                        EntityCollection list_sub_product = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);
                        if (list_sub_product.Entities.Any())
                        {
                            trace = "2.1";
                            EntityReference deliveryplan_ref = new EntityReference("bsd_deliveryplan", deliveryplan_id);
                            foreach (var item in list_sub_product.Entities)
                            {
                                trace = "2.2";
                                Entity deliveryplan_product = new Entity("bsd_deliveryplanproduct");
                                deliveryplan_product["bsd_deliveryplan"] = deliveryplan_ref;
                                trace = "2.2";
                                deliveryplan_product["bsd_unit"] = item["bsd_unit"];
                                trace = "2.3";
                                deliveryplan_product["bsd_product"] = item["bsd_product"];
                                trace = "2.4";
                                if (deliveryplan_product.HasValue("bsd_orderquantity"))
                                {
                                    deliveryplan_product["bsd_orderquantity"] = Math.Abs((decimal)item["bsd_orderquantity"]);
                                }
                                trace = "2.5";
                                deliveryplan_product["bsd_shipquantity"] = Math.Abs((decimal)item["bsd_shipquantity"]);
                                deliveryplan_product["bsd_remainingquantity"] = Math.Abs((decimal)item["bsd_shipquantity"]);
                                deliveryplan_product["bsd_remainaddtruck"] = Math.Abs((decimal)item["bsd_shipquantity"]);
                                trace = "2.6";
                                if (item.HasValue("bsd_warehouse"))
                                    deliveryplan_product["bsd_warehouse"] = item["bsd_warehouse"];
                                trace = "2.7";
                                deliveryplan_product["bsd_standardquantity"] = item["bsd_standardquantity"];
                                trace = "2.8";
                                // setname.
                                Entity product = myService.service.Retrieve("product", ((EntityReference)item["bsd_product"]).Id, new ColumnSet(true));
                                if (product.HasValue("name"))
                                    deliveryplan_product["bsd_name"] = product["name"];
                                // setname
                                trace = "2.9";
                                myService.service.Create(deliveryplan_product);
                            }
                        }
                        trace = "3";
                        #region cập nhật lai created delivery plan của suborder
                        Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
                        new_suborder["bsd_createddeliveryplan"] = true;
                        myService.Update(new_suborder);
                        #endregion
                        trace = "4";
                        #region Phong 2-2-2018 Approve

                        if (Issubmit.ToLower().Trim().Equals("true"))
                        {
                            string entityName = context.InputParameters["Entity"].ToString();

                            Guid RecordID = Guid.Parse(context.InputParameters["ObjectId"].ToString());
                            Guid StageId = Guid.Parse(context.InputParameters["StageId"].ToString());
                            Guid User = Guid.Parse(context.InputParameters["Username"].ToString());
                            string attributeName = context.InputParameters["Attribute"].ToString();
                            string approveperson = context.InputParameters["Approveperson"].ToString();
                            string Approvedate = context.InputParameters["Approvedate"].ToString();
                            string traversedpath = context.InputParameters["StageId"].ToString();


                            int attributeValue = int.Parse(context.InputParameters["Value"].ToString());

                            //Guid gUserId = ((WhoAmIResponse)myService.service.Execute(new WhoAmIRequest())).UserId;

                            Entity updatedStage = new Entity(entityName);
                            Stage_Aprrove(myService.service, entityName, RecordID, User, StageId, traversedpath, attributeName, attributeValue, approveperson, Approvedate, updatedStage);
                        }
                        #endregion
                        myService.context.OutputParameters["ReturnId"] = deliveryplan_id.ToString();
                        #endregion


                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Plugin_SubOrderReturn:"+ ex.Message + trace);
                    }
                }

            }

        }
        public static void Stage_Aprrove(IOrganizationService service, string entityName, Guid RecordID, Guid User, Guid StageId, string traversedpath, string attributeName, int attributeValue
                                      , string approveperson, string Approvedate, Entity updatedStage)
        {
            updatedStage.Id = RecordID;
            updatedStage["stageid"] = StageId;
            updatedStage["traversedpath"] = traversedpath;
            updatedStage["bsd_stageid"] = StageId.ToString();
            updatedStage[attributeName] = new OptionSetValue(attributeValue);
            updatedStage["bsd_rejectnumber"] = "Approve";
            updatedStage["bsd_duyet"] = true;
            updatedStage["bsd_congno"] = true;
            updatedStage["" + approveperson + ""] = new EntityReference("systemuser", User);
            updatedStage["" + Approvedate + ""] = RetrieveLocalTimeFromUTCTimeStatic(DateTime.Now, service);
            service.Update(updatedStage);
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
        public static DateTime RetrieveLocalTimeFromUTCTimeStatic(DateTime utcTime, IOrganizationService service)
        {
            int? timeZoneCode = RetrieveCurrentUsersSettingsStatic(service);
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
        public int? RetrieveCurrentUsersSettings(IOrganizationService service)
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
        public static int? RetrieveCurrentUsersSettingsStatic(IOrganizationService service)
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
    }
}
