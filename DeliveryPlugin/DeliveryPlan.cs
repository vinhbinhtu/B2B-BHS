using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DeliveryPlugin.Service;

namespace DeliveryPlugin
{
    public class DeliveryPlan : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            if (myService.context.MessageName == "bsd_CreateDeliveryPlan")
            {
                string Issubmit = context.InputParameters["Issubmit"].ToString();
                // throw new Exception("okie");
                EntityReference target = myService.getTargetEntityReference();
                myService.StartService();
                Entity suborder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                if (!suborder.HasValue("bsd_requestedshipdate") || !suborder.HasValue("bsd_requestedreceiptdate"))
                {
                    throw new Exception("You must provice a value for Ship Date and Receipt Date");
                }
                if (myService.context.Depth > 1) return;

                #region check số lượng trên suborder
                EntityCollection list_noquantity = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_suborderproduct'>
                    <attribute name='bsd_suborderproductid' />
                    <filter type='and'>
                      <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='" + suborder.Id + @"' />
                      <condition attribute='bsd_shipquantity' operator='eq' value='0' />
                    </filter>
                  </entity>
                </fetch>");
                if (list_noquantity.Entities.Any()) "No Quantity To Create Delivery Schedule".Throw();
                #endregion

                #region Check existing
                EntityCollection list_deliveryplan = myService.RetrieveOneCondition("bsd_deliveryplan", "bsd_suborder", target.Id);
                if (list_deliveryplan.Entities.Any()) throw new Exception("Đã tạo rồi");
                #endregion

                Entity deliveryplan = new Entity("bsd_deliveryplan");
                deliveryplan["bsd_suborder"] = target;
                try
                {
                    deliveryplan["bsd_type"] = suborder["bsd_type"];
                    // vinhlh 21-12-2017 suborder Site Kí gửi
                    if (((OptionSetValue)suborder["bsd_typeorder"]).Value == 861450002)
                    {
                        EntityReference site_Consignment = getDefaultConfig();
                        deliveryplan["bsd_site"] = new EntityReference(site_Consignment.LogicalName, site_Consignment.Id);
                        //Entity Site_Consignment = myService.service.Retrieve(site_Consignment.LogicalName, site_Consignment.Id, new ColumnSet("bsd_address"));
                        //if (Site_Consignment.HasValue("bsd_address"))
                        //    deliveryplan["bsd_siteaddress"] = (EntityReference)Site_Consignment["bsd_address"];
                    }
                    //end vinhlh
                }
                catch { }
                NewEntity ne = new NewEntity(suborder, deliveryplan);
                ne.Set("bsd_order", true);
                ne.Set("bsd_quote", true);
                ne.Set("bsd_potentialcustomer", true);
                ne.Set("bsd_addresscustomeraccount", true);
                ne.Set("bsd_telephone", true);
                ne.Set("bsd_contact", true);
                ne.Set("bsd_requestedshipdate", true);
                ne.Set("bsd_requestedreceiptdate", true);
                ne.Set("bsd_confirmedreceiptdate", true);
                ne.Set("bsd_historyreceiptcustomer", true);
                ne.Set("bsd_shiptoaccount", "bsd_potentialcustomer", true);
                ne.Set("bsd_shiptoaddress", "bsd_shiptoaddress", true);
                // vinhlh 21-12-2017 suborder Site Kí gửi
                if (suborder.HasValue("bsd_typeorder"))
                {
                    if (((OptionSetValue)suborder["bsd_typeorder"]).Value != 861450002)
                    {
                        ne.Set("bsd_site", true);
                        //ne.Set("bsd_siteaddress", true);
                    }
                }
                else
                {
                    ne.Set("bsd_site", true);
                    //ne.Set("bsd_siteaddress", true);
                }
                //end vinhlh
                ne.Set("bsd_siteaddress", true);
                ne.Set("bsd_port", true);
                ne.Set("bsd_addressport", true);
                ne.Set("bsd_shippingaddress", true);
                ne.Set("bsd_shippingfromaddress", true);
                ne.Set("bsd_deliveryfrom", true);
                ne.Set("bsd_carrier", true);
                ne.Set("bsd_transportation", true);
                ne.Set("bsd_requestporter", true);

                Guid deliveryplan_id = myService.service.Create(deliveryplan);
                EntityCollection list_sub_product = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);
                if (list_sub_product.Entities.Any())
                {
                    EntityReference deliveryplan_ref = new EntityReference("bsd_deliveryplan", deliveryplan_id);
                    foreach (var item in list_sub_product.Entities)
                    {
                        Entity deliveryplan_product = new Entity("bsd_deliveryplanproduct");
                        deliveryplan_product["bsd_deliveryplan"] = deliveryplan_ref;
                        deliveryplan_product["bsd_unit"] = item["bsd_unit"];
                        deliveryplan_product["bsd_product"] = item["bsd_product"];
                        if (item.HasValue("bsd_productid")) deliveryplan_product["bsd_productid"] = item["bsd_productid"];
                        if (item.HasValue("bsd_descriptionproduct")) deliveryplan_product["bsd_descriptionproduct"] = item["bsd_descriptionproduct"];
                        if (deliveryplan_product.HasValue("bsd_orderquantity"))
                        {
                            deliveryplan_product["bsd_orderquantity"] = item["bsd_orderquantity"];
                        }

                        //huy
                        if (!item.HasValue("bsd_appendixcontract"))//hợp đồng không có phục lục thì tính bình thường
                        {
                            deliveryplan_product["bsd_shipquantity"] = item["bsd_shipquantity"];
                            deliveryplan_product["bsd_remainingquantity"] = item["bsd_shipquantity"];
                            deliveryplan_product["bsd_remainaddtruck"] = item["bsd_shipquantity"];
                        }
                        else//có phụ lục thì tính theo số lượng phụ lục + số lượng trong order
                        {
                            deliveryplan_product["bsd_shipquantity"] = item["bsd_newquantity"];
                            deliveryplan_product["bsd_remainingquantity"] = item["bsd_newquantity"];
                            deliveryplan_product["bsd_remainaddtruck"] = item["bsd_newquantity"];
                        }
                        //end huy
                        if (item.HasValue("bsd_standardquantity"))
                        {
                            deliveryplan_product["bsd_standardquantity"] = item["bsd_standardquantity"];
                        }
                        // setname.
                        Entity product = myService.service.Retrieve("product", ((EntityReference)item["bsd_product"]).Id, new ColumnSet("name"));
                        deliveryplan_product["bsd_name"] = product["name"];
                        // setname

                        myService.service.Create(deliveryplan_product);
                    }
                }
                else throw new Exception("No Product to create Delivery Plan !");

                #region cập nhật lai created delivery plan của suborder
                Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
                new_suborder["bsd_createddeliveryplan"] = true;
                myService.Update(new_suborder);
                #endregion

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
                    SuborderService.Stage_Aprrove(myService.service, entityName, RecordID, User, StageId, traversedpath, attributeName, attributeValue, approveperson, Approvedate, updatedStage);
                }
                #endregion

                myService.context.OutputParameters["ReturnId"] = deliveryplan_id.ToString();
            }

            #region delete
            else if (myService.context.MessageName == "Delete")
            {
                // Cập nhật suborder
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
                Entity deliveryplan = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_suborder"));
                Entity suborder = new Entity("bsd_suborder", ((EntityReference)deliveryplan["bsd_suborder"]).Id);
                suborder["bsd_createddeliveryplan"] = false;
                myService.service.Update(suborder);
            }
            #endregion
        }
        public EntityReference getDefaultConfig()
        {
            QueryExpression q = new QueryExpression("bsd_configdefault");
            q.ColumnSet = new ColumnSet("bsd_consignmentsites");
            Entity configdefault = myService.service.RetrieveMultiple(q)[0];
            if (!configdefault.HasValue("bsd_consignmentsites")) "You must provice a value for Consignment Sites (Config Default) !".Throw();

            return (EntityReference)configdefault["bsd_consignmentsites"];

        }
    }
}
