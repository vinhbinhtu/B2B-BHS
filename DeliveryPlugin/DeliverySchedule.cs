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
    public class DeliverySchedule : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1)
                return;
            if (myService.context.MessageName == "Create")
            {
                Entity target = myService.getTarget();
                myService.StartService();
                Guid productid = ((EntityReference)target["bsd_itemid"]).Id;
                Guid orderid = ((EntityReference)target["bsd_orderid"]).Id;
                Guid warehousefromid = ((EntityReference)target["bsd_warehousefrom"]).Id;
                Guid shiptoaddressid = ((EntityReference)target["bsd_shiptoaddress"]).Id;
                DateTime shipdate = myService.RetrieveLocalTimeFromUTCTime((DateTime)target["bsd_requestedshipdate"], myService.service);
                string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>,
                  <entity name='bsd_scheduledelivery'>,
                    <attribute name='bsd_scheduledeliveryid' />,
                    <attribute name='bsd_name' />,
                    <attribute name='createdon' />,
                    <order attribute='bsd_name' descending='false' />,
                    <filter type='and'>,
                      <condition attribute='bsd_itemid' operator='eq' uitype='product' value='{0}' />,
                      <condition attribute='bsd_orderid' operator='eq' uitype='salesorder' value='{1}' />,
                      <condition attribute='bsd_requestedshipdate' operator='on' value='{2}' />,
                      <condition attribute='bsd_warehousefrom' operator='eq' uitype='product' value='{3}' />,
                      <condition attribute='bsd_shiptoaddress' operator='eq' uitype='bsd_address' value='{4}' />,
                    </filter>,
                  </entity>,
                </fetch>", productid, orderid, shipdate, warehousefromid, shiptoaddressid);
                EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(xml));
                if (list.Entities.Any())
                {
                    throw new Exception("Đã tạo rồi Lịch giao hàng với cùng sản phẩm, cùng kho, cùng địa chỉ này  !");
                }
            }
            else if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();
                myService.StartService();
                if (target.Contains("bsd_requestedshipdate"))
                {
                    Entity deliveryschedule = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    Guid productid = ((EntityReference)deliveryschedule["bsd_itemid"]).Id;
                    Guid orderid = ((EntityReference)deliveryschedule["bsd_orderid"]).Id;
                    Guid warehousefromid = ((EntityReference)deliveryschedule["bsd_warehousefrom"]).Id;
                    Guid shiptoaddressid = ((EntityReference)deliveryschedule["bsd_shiptoaddress"]).Id;
                    DateTime shipdate = myService.RetrieveLocalTimeFromUTCTime((DateTime)deliveryschedule["bsd_requestedshipdate"], myService.service);
                    string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>,
                      <entity name='bsd_scheduledelivery'>,
                        <attribute name='bsd_scheduledeliveryid' />,
                        <attribute name='bsd_name' />,
                        <attribute name='createdon' />,
                        <order attribute='bsd_name' descending='false' />,
                        <filter type='and'>,
                          <condition attribute='bsd_itemid' operator='eq' uitype='product' value='{0}' />,
                          <condition attribute='bsd_orderid' operator='eq' uitype='salesorder' value='{1}' />,
                          <condition attribute='bsd_requestedshipdate' operator='on' value='{2}' />,
                          <condition attribute='bsd_warehousefrom' operator='eq' uitype='product' value='{3}' />,
                          <condition attribute='bsd_shiptoaddress' operator='eq' uitype='bsd_address' value='{4}' />,
                         '<condition attribute = 'bsd_scheduledeliveryid' operator= 'ne' uitype = 'bsd_scheduledelivery' value = '{5}' />'
                        </filter>,
                      </entity>,
                    </fetch>", productid, orderid, shipdate, warehousefromid, shiptoaddressid, deliveryschedule.Id);
                    EntityCollection list = myService.service.RetrieveMultiple(new FetchExpression(xml));
                    if (list.Entities.Count > 0)
                    {
                        throw new Exception("Đã tạo rồi !");
                    }
                }

            }
            //< condition attribute = 'bsd_scheduledeliveryid' operator= 'ne' uitype = 'bsd_scheduledelivery' value = '{4}' />,
            if (myService.context.MessageName == "Delete")
            {
                EntityReference target = myService.getTargetEntityReference();
                myService.StartService();
                EntityCollection ec_list_subproduct = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_deliveryschedule", target.Id);

                if (ec_list_subproduct.Entities.Any())
                {
                    throw new Exception("Không thể xóa Lịch giao hàng này vì lịch giao hàng này liên quan đến dữ liệu khác.");
                }
            }
        }
    }
}
