using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace DeliveryPlugin
{
    public class DeliveryPlanTruck : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            MyService myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1) return;

            #region Create
            if (myService.context.MessageName == "Create")
            {
                // cùng biển số, cùng loại. thì sẽ gom lại thành 1 cái, lấy tài xế mới nhất ! 
                Entity target = myService.getTarget();
                target["bsd_goodsissuenotequantity"] = 0m;
                target["bsd_remaininggoodsissuenotequantity"] = target["bsd_quantity"];
                myService.StartService();

                #region Huy: Khai báo phục vụ update giá vận chuyển
                EntityReference ref_deliveryschedule = (EntityReference)target["bsd_deliveryplan"];
                EntityReference ref_scheduleproduct = (EntityReference)target["bsd_deliveryplanproduct"];
                Entity deliveryschedule = myService.service.Retrieve(ref_deliveryschedule.LogicalName, ref_deliveryschedule.Id, new ColumnSet(true));
                bool check_duplicate = false;
                int method = 0;
                decimal shippingcosts = target.HasValue("bsd_shippingcosts") ? ((Money)target["bsd_shippingcosts"]).Value : 0m;
                int deliverytruck_type = ((OptionSetValue)target["bsd_deliverytrucktype"]).Value;
                int type = ((OptionSetValue)deliveryschedule["bsd_type"]).Value;
                decimal new_qty = target.HasValue("bsd_quantity") ? (decimal)target["bsd_quantity"] : 0m;
                decimal old_quantity = 0m;
                #endregion

                StringBuilder sb = new StringBuilder();
                sb.Append("<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>");
                sb.Append("<entity name='bsd_deliveryplantruck'>");
                sb.Append("<attribute name='bsd_deliveryplantruckid' />");
                sb.Append("<attribute name='bsd_quantity' />");
                sb.Append("<attribute name='bsd_driver' />");
                sb.Append("<attribute name='bsd_shippingcosts' />");
                sb.Append("<filter type='and'>");
                sb.Append("<condition attribute='bsd_deliveryplanproduct' operator='eq' uitype='bsd_deliveryplanproduct' value='" + ((EntityReference)target["bsd_deliveryplanproduct"]).Id + "' />");
                sb.Append("<condition attribute='bsd_licenseplate' operator='eq' value='" + target["bsd_licenseplate"].ToString() + "' />");
                sb.Append("<condition attribute='bsd_status' operator='eq' value='861450001' />");
                sb.Append("<condition attribute='bsd_deliverytrucktype' operator='eq' value='" + deliverytruck_type + "' />");

                if (target.HasValue("bsd_carrierpartner"))
                {
                    Guid bsd_carrierpartner_id = ((EntityReference)target["bsd_carrierpartner"]).Id;
                    sb.Append("<condition attribute='bsd_carrierpartner' operator='eq' uitype='account' value='" + bsd_carrierpartner_id + "' />");
                }
                if (target.HasValue("bsd_shippingdeliverymethod"))
                {
                    method = ((OptionSetValue)target["bsd_shippingdeliverymethod"]).Value;
                    sb.Append("<condition attribute='bsd_shippingdeliverymethod' operator='eq' value='" + method + "' />");
                }
                if (target.HasValue("bsd_shippingoption"))
                {
                    bool shipping = ((bool)target["bsd_shippingoption"]);
                    sb.Append("<condition attribute='bsd_shippingoption' operator='eq' value='" + shipping + "' />");
                }
                if (target.HasValue("bsd_truckload"))
                {
                    Guid bsd_truckload = ((EntityReference)target["bsd_truckload"]).Id;
                    sb.Append("<condition attribute='bsd_truckload' uitype='bsd_truckload' operator='eq' value='" + bsd_truckload + "' />");
                }

                sb.Append("</filter>");
                sb.Append("</entity>");
                sb.Append("</fetch>");

                EntityCollection list_deliveryproducttruck = myService.service.RetrieveMultiple(new FetchExpression(sb.ToString()));
                if (list_deliveryproducttruck.Entities.Any())
                {
                    Entity deliverytruck = list_deliveryproducttruck.Entities.First();
                    Entity new_deliverytruck = new Entity(target.LogicalName, target.Id);
                    check_duplicate = true;
                    old_quantity = (decimal)deliverytruck["bsd_quantity"];
                    new_qty = (decimal)target["bsd_quantity"] + old_quantity;
                    target["bsd_quantity"] = new_qty;
                    target["bsd_remaininggoodsissuenotequantity"] = target["bsd_quantity"];
                    if (method == 861450000)//ton
                    {
                        decimal shippingcosts_duplicate = deliverytruck.HasValue("bsd_shippingcosts") ? ((Money)deliverytruck["bsd_shippingcosts"]).Value : 0m;
                        target["bsd_shippingcosts"] = new Money(((Money)target["bsd_shippingcosts"]).Value + shippingcosts_duplicate);

                    }
                    myService.service.Delete(deliverytruck.LogicalName, deliverytruck.Id);
                }
                #region Update delivery schedule product
                Entity ent_scheduleproduct = myService.service.Retrieve(ref_scheduleproduct.LogicalName, ref_scheduleproduct.Id,new ColumnSet("bsd_remainaddtruck"));
                decimal remainaddtruck = (decimal)ent_scheduleproduct["bsd_remainaddtruck"];
                ent_scheduleproduct["bsd_remainaddtruck"] = remainaddtruck + old_quantity - new_qty;
                myService.service.Update(ent_scheduleproduct);
                #endregion
                

                #region huy 3/11/2017: update total shipping price khi thêm xe
                if (type == 861450004 && deliverytruck_type == 861450002)//áp dụng cho return order + shipper
                {
                    method = ((OptionSetValue)target["bsd_shippingdeliverymethod"]).Value;
                    decimal total_ShipPrice = deliveryschedule.HasValue("bsd_totalshippingprice") ? ((Money)deliveryschedule["bsd_totalshippingprice"]).Value : 0m;
                    if (method == 861450000 || (method == 861450001 && !check_duplicate))
                    {
                        total_ShipPrice += shippingcosts;
                    }
                    Entity new_deliveryschedule = new Entity(deliveryschedule.LogicalName, deliveryschedule.Id);
                    new_deliveryschedule["bsd_totalshippingprice"] = new Money(total_ShipPrice);
                    myService.service.Update(new_deliveryschedule);
                }
                #endregion
            }
            #endregion

            #region update
            else if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_quantity"))
                    target["bsd_remaininggoodsissuenotequantity"] = target["bsd_quantity"];
                myService.StartService();

                Entity update_target = new Entity(target.LogicalName, target.Id);
                if (target.HasValue("bsd_quantity"))
                    update_target["bsd_remaininggoodsissuenotequantity"] = target["bsd_quantity"];
                myService.service.Update(update_target);

                #region Huy: Khai báo những thứ cần thiết cho update giá vận chuyển
                Entity PreImage = myService.context.PreEntityImages["PreImage"];
                Entity deliveryplantruck_target = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                EntityReference ref_deliveryschedule = (EntityReference)deliveryplantruck_target["bsd_deliveryplan"];
                EntityReference ref_scheduleproduct = (EntityReference)deliveryplantruck_target["bsd_deliveryplanproduct"];
                Entity deliveryschedule = myService.service.Retrieve(ref_deliveryschedule.LogicalName, ref_deliveryschedule.Id, new ColumnSet(true));
                bool check_duplicate = false;
                int method = 0;
                decimal new_qty = target.HasValue("bsd_quantity") ? (decimal)target["bsd_quantity"] : (decimal)PreImage["bsd_quantity"];
                decimal old_shippingcosts = PreImage.HasValue("bsd_shippingcosts") ? ((Money)PreImage["bsd_shippingcosts"]).Value : 0m;
                decimal new_shippingcost = target.ContainAndHasValue("bsd_shippingcosts") ? ((Money)target["bsd_shippingcosts"]).Value : 0m;
                decimal old_quantity = (decimal)PreImage["bsd_quantity"];
                int type = ((OptionSetValue)deliveryschedule["bsd_type"]).Value;
                int deliverytruck_type = target.ContainAndHasValue("bsd_deliverytrucktype") ? ((OptionSetValue)target["bsd_deliverytrucktype"]).Value : ((OptionSetValue)PreImage["bsd_deliverytrucktype"]).Value;
                #endregion
                StringBuilder sb = new StringBuilder();
                sb.Append("<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>");
                sb.Append("<entity name='bsd_deliveryplantruck'>");
                sb.Append("<attribute name='bsd_deliveryplantruckid' />");
                sb.Append("<attribute name='bsd_quantity' />");
                sb.Append("<attribute name='bsd_driver' />");
                sb.Append("<attribute name='bsd_shippingcosts' />");
                sb.Append("<filter type='and'>");
                sb.Append("<condition attribute='bsd_deliveryplanproduct' operator='eq' uitype='bsd_deliveryplanproduct' value='" + ((EntityReference)deliveryplantruck_target["bsd_deliveryplanproduct"]).Id + "' />");
                sb.Append("<condition attribute='bsd_licenseplate' operator='eq' value='" + deliveryplantruck_target["bsd_licenseplate"].ToString() + "' />");
                sb.Append("<condition attribute='bsd_status' operator='eq' value='861450001' />");
                sb.Append("<condition attribute='bsd_deliverytrucktype' operator='eq' value='" + deliverytruck_type + "' />");
                sb.Append("<condition attribute='bsd_deliveryplantruckid' operator='ne' uitype='bsd_deliveryplantruck' value='" + deliveryplantruck_target.Id + "' />");
                if (deliveryplantruck_target.HasValue("bsd_carrierpartner"))
                {
                    Guid bsd_carrierpartner_id = ((EntityReference)deliveryplantruck_target["bsd_carrierpartner"]).Id;
                    sb.Append("<condition attribute='bsd_carrierpartner' operator='eq' uitype='account' value='" + bsd_carrierpartner_id + "' />");
                }
                if (deliveryplantruck_target.HasValue("bsd_shippingdeliverymethod"))
                {
                    method = ((OptionSetValue)deliveryplantruck_target["bsd_shippingdeliverymethod"]).Value;
                    sb.Append("<condition attribute='bsd_shippingdeliverymethod' operator='eq' value='" + method + "' />");
                }
                if (target.HasValue("bsd_shippingoption"))
                {
                    bool shipping = ((bool)target["bsd_shippingoption"]);
                    sb.Append("<condition attribute='bsd_shippingoption' operator='eq' value='" + shipping + "' />");
                }
                if (target.HasValue("bsd_truckload"))
                {

                    Guid bsd_truckload = ((EntityReference)target["bsd_truckload"]).Id;
                    sb.Append("<condition attribute='bsd_truckload' uitype='bsd_truckload' operator='eq' value='" + bsd_truckload + "' />");
                }
                sb.Append("</filter>");
                sb.Append("</entity>");
                sb.Append("</fetch>");
                EntityCollection list_deliveryproducttruck = myService.service.RetrieveMultiple(new FetchExpression(sb.ToString()));
                
                if (list_deliveryproducttruck.Entities.Any())
                {
                    Entity deliveryplantruck = list_deliveryproducttruck.Entities.First();
                    old_quantity = (decimal)deliveryplantruck["bsd_quantity"];
                    Entity new_target = new Entity(target.LogicalName, target.Id);
                    new_qty = (decimal)deliveryplantruck_target["bsd_quantity"] + old_quantity;
                    new_target["bsd_quantity"] = (decimal)deliveryplantruck_target["bsd_quantity"] + old_quantity;
                    new_target["bsd_remaininggoodsissuenotequantity"] = new_target["bsd_quantity"];

                    #region Huy: cập nhật giá vận chuyển của xe
                    if (method == 861450000)//ton
                    {
                        decimal shippingcosts_duplicate = deliveryplantruck.HasValue("bsd_shippingcosts") ? ((Money)deliveryplantruck["bsd_shippingcosts"]).Value : 0m;
                        new_target["bsd_shippingcosts"] = new Money(((Money)new_target["bsd_shippingcosts"]).Value + shippingcosts_duplicate);
                    }
                    #endregion

                    myService.service.Update(new_target);
                    myService.service.Delete(deliveryplantruck.LogicalName, deliveryplantruck.Id);
                }

                #region Update delivery schedule product
                
                Entity ent_scheduleproduct = myService.service.Retrieve(ref_scheduleproduct.LogicalName, ref_scheduleproduct.Id, new ColumnSet("bsd_remainaddtruck"));
                decimal remainaddtruck = (decimal)ent_scheduleproduct["bsd_remainaddtruck"];
                ent_scheduleproduct["bsd_remainaddtruck"] = remainaddtruck + old_quantity - new_qty;
                myService.service.Update(ent_scheduleproduct);
                #endregion

                #region huy 3/11/2017: update total shipping price khi thêm xe
                if (type == 861450004 && deliverytruck_type == 861450002)//áp dụng cho return order + shipper
                {
                    method = target.ContainAndHasValue("bsd_shippingdeliverymethod") ? ((OptionSetValue)target["bsd_shippingdeliverymethod"]).Value : ((OptionSetValue)PreImage["bsd_shippingdeliverymethod"]).Value;

                    decimal total_ShipPrice = deliveryschedule.HasValue("bsd_totalshippingprice") ? ((Money)deliveryschedule["bsd_totalshippingprice"]).Value : 0m;
                    if (method == 861450000 || (method == 861450001 && !check_duplicate))
                    {
                        total_ShipPrice = total_ShipPrice - old_shippingcosts + new_shippingcost;
                    }
                    Entity new_deliveryschedule = new Entity(deliveryschedule.LogicalName, deliveryschedule.Id);
                    new_deliveryschedule["bsd_totalshippingprice"] = new Money(total_ShipPrice);
                    myService.service.Update(new_deliveryschedule);
                }
                #endregion
            }
            #endregion update

            #region Delete
            else if (myService.context.MessageName == "Delete")
            {
                EntityReference target = myService.getTargetEntityReference();
                myService.StartService();
                Entity PreImage = myService.context.PreEntityImages["PreImage"];
                Entity deliveryplantruck = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                int status = ((OptionSetValue)deliveryplantruck["bsd_status"]).Value;
                
                EntityReference ref_deliveryschedule = (EntityReference)deliveryplantruck["bsd_deliveryplan"];
                Entity deliveryschedule = myService.service.Retrieve(ref_deliveryschedule.LogicalName, ref_deliveryschedule.Id, new ColumnSet(true));
                int type = ((OptionSetValue)deliveryschedule["bsd_type"]).Value;
                int trucktype = ((OptionSetValue)PreImage["bsd_deliverytrucktype"]).Value;
                decimal pre_qty = (decimal)PreImage["bsd_quantity"];
                
                if (status == 861450000)
                {
                    throw new Exception("Delivery Request has been created. Cannot delete Truck !");
                }
                if (PreImage.HasValue("bsd_deliveryplanproduct"))
                {
                    EntityReference ref_scheduleproduct = (EntityReference)PreImage["bsd_deliveryplanproduct"];
                    Entity ent_scheduleproduct = myService.service.Retrieve(ref_scheduleproduct.LogicalName, ref_scheduleproduct.Id, new ColumnSet("bsd_remainaddtruck"));
                    decimal new_remainaddtruck = (decimal)ent_scheduleproduct["bsd_remainaddtruck"] + pre_qty;
                    ent_scheduleproduct["bsd_remainaddtruck"] = new_remainaddtruck;
                    myService.service.Update(ent_scheduleproduct);
                }
                #region huy 3/11/2017: update total shipping price khi xóa xe
                if (type == 861450004 && trucktype == 861450002)//áp dụng cho return order + shipper
                {
                    decimal total_ShipPric = deliveryschedule.HasValue("bsd_totalshippingprice") ? ((Money)deliveryschedule["bsd_totalshippingprice"]).Value : 0m;
                    decimal shippingcost = PreImage.HasValue("bsd_shippingcosts") ? ((Money)PreImage["bsd_shippingcosts"]).Value : 0m;
                    total_ShipPric -= shippingcost;

                    Entity new_deliveryschedule = new Entity(deliveryschedule.LogicalName, deliveryschedule.Id);
                    new_deliveryschedule["bsd_totalshippingprice"] = new Money(total_ShipPric);
                    myService.service.Update(new_deliveryschedule);
                }
                #endregion

            }
            #endregion 

        }
    }

    public class DeliveryPlanTruck_Service
    {
        private MyService myService;
        public DeliveryPlanTruck_Service(MyService myService)
        {
            this.myService = myService;
        }
        /// <summary>
        /// Gom lại những truck nào giống nhau thành 1 (sum quantity)
        /// </summary>
        public void JoinTruck(Guid DelivereScheduleId)
        {
            EntityCollection list_truck;
            #region Lấy list truck
            QueryExpression q = new QueryExpression("bsd_deliveryplantruck");
            q.ColumnSet = new ColumnSet(true);
            FilterExpression f = new FilterExpression(LogicalOperator.And);
            f.AddCondition(new ConditionExpression("bsd_deliveryplan", ConditionOperator.Equal, DelivereScheduleId));
            f.AddCondition(new ConditionExpression("bsd_status", ConditionOperator.Equal, 861450001));
            q.Criteria = f;
            list_truck = myService.service.RetrieveMultiple(q);
            #endregion

            int CountTruck = list_truck.Entities.Count;
            foreach (Entity deliveryplantruck_target in list_truck.Entities)
            {
                int deliverytruck_type = ((OptionSetValue)deliveryplantruck_target["bsd_deliverytrucktype"]).Value;

                StringBuilder sb = new StringBuilder();
                sb.Append("<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>");
                sb.Append("<entity name='bsd_deliveryplantruck'>");
                sb.Append("<attribute name='bsd_deliveryplantruckid' />");
                sb.Append("<attribute name='bsd_quantity' />");
                sb.Append("<attribute name='bsd_driver' />");
                sb.Append("<filter type='and'>");
                sb.Append("<condition attribute='bsd_deliveryplanproduct' operator='eq' uitype='bsd_deliveryplanproduct' value='" + ((EntityReference)deliveryplantruck_target["bsd_deliveryplanproduct"]).Id + "' />");
                sb.Append("<condition attribute='bsd_licenseplate' operator='eq' value='" + deliveryplantruck_target["bsd_licenseplate"].ToString() + "' />");
                sb.Append("<condition attribute='bsd_status' operator='eq' value='861450001' />");
                sb.Append("<condition attribute='bsd_deliverytrucktype' operator='eq' value='" + deliverytruck_type + "' />");
                sb.Append("<condition attribute='bsd_deliveryplantruckid' operator='ne' uitype='bsd_deliveryplantruck' value='" + deliveryplantruck_target.Id + "' />");
                if (deliveryplantruck_target.HasValue("bsd_carrierpartner"))
                {
                    Guid bsd_carrierpartner_id = ((EntityReference)deliveryplantruck_target["bsd_carrierpartner"]).Id;
                    sb.Append("<condition attribute='bsd_carrierpartner' operator='eq' uitype='account' value='" + bsd_carrierpartner_id + "' />");
                }
                if (deliveryplantruck_target.HasValue("bsd_shippingdeliverymethod"))
                {
                    int method = ((OptionSetValue)deliveryplantruck_target["bsd_shippingdeliverymethod"]).Value;
                    sb.Append("<condition attribute='bsd_shippingdeliverymethod' operator='eq' value='" + method + "' />");
                }
                if (deliveryplantruck_target.HasValue("bsd_shippingoption"))
                {
                    bool shipping = ((bool)deliveryplantruck_target["bsd_shippingoption"]);
                    sb.Append("<condition attribute='bsd_shippingoption' operator='eq' value='" + shipping + "' />");
                }
                if (deliveryplantruck_target.HasValue("bsd_truckload"))
                {
                    Guid bsd_truckload = ((EntityReference)deliveryplantruck_target["bsd_truckload"]).Id;
                    sb.Append("<condition attribute='bsd_truckload' uitype='bsd_truckload' operator='eq' value='" + bsd_truckload + "' />");
                }
                sb.Append("</filter>");
                sb.Append("</entity>");
                sb.Append("</fetch>");
                EntityCollection list_deliveryproducttruck = myService.service.RetrieveMultiple(new FetchExpression(sb.ToString()));
                if (list_deliveryproducttruck.Entities.Any())
                {
                    Entity deliveryplantruck = list_deliveryproducttruck.Entities.First();
                    decimal quantity = (decimal)deliveryplantruck["bsd_quantity"];
                    Entity new_target = new Entity(deliveryplantruck_target.LogicalName, deliveryplantruck_target.Id);
                    new_target["bsd_quantity"] = (decimal)deliveryplantruck_target["bsd_quantity"] + quantity;

                    myService.service.Update(new_target);
                    myService.service.Delete(deliveryplantruck.LogicalName, deliveryplantruck.Id);
                    break;
                }
            }
        }
    }
}