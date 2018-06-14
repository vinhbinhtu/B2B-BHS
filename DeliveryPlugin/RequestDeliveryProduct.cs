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
    public class RequestDeliveryProduct : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            MyService myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1) return;


            if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_warehouse"))
                {
                    myService.StartService();
                    Entity request_delivery_product = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true)); // target

                    #region Lấy sản phẩm có cùng kho này, cùng yêu cầu, cùng sản phẩm.
                    string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_requestdeliveryproduct'>
                        <attribute name='bsd_requestdeliveryproductid' />
                        <attribute name='bsd_requestdelivery' />
                        <attribute name='bsd_product' />
                        <attribute name='bsd_quantity' />
                        <filter type='and'>
                          <condition attribute='bsd_requestdeliveryproductid' operator='ne' uitype='bsd_requestdeliveryproduct' value='{0}' />
                          <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='{1}' />
                          <condition attribute='bsd_product' operator='eq' uitype='product' value='{2}' />
                          <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='{3}' />
                        </filter>
                      </entity>
                    </fetch>", target.Id,
                    ((EntityReference)request_delivery_product["bsd_requestdelivery"]).Id,
                    ((EntityReference)request_delivery_product["bsd_product"]).Id,
                    ((EntityReference)target["bsd_warehouse"]).Id);

                    EntityCollection list_requestdeliveryproduct = myService.service.RetrieveMultiple(new FetchExpression(xml));
                    if (list_requestdeliveryproduct.Entities.Any())
                    {
                        var request_delivery_product_duplicate = list_requestdeliveryproduct.Entities.First();
                        Entity new_target = new Entity(target.LogicalName, target.Id);
                        new_target["bsd_quantity"] = (decimal)request_delivery_product["bsd_quantity"] + (decimal)request_delivery_product_duplicate["bsd_quantity"];
                        myService.Update(new_target);
                        // cộng dồn số lượng thằng cũ vào thằng target rồi xóa thằng cũ đi luôn. !
                        myService.Delete(request_delivery_product_duplicate.LogicalName, request_delivery_product_duplicate.Id);
                    }
                    #endregion
                }
            }
        }
    }
}
