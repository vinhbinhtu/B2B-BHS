using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using Plugin.Service;

namespace DeliveryPlugin.Service
{
    public class RequestDeliveryService
    {
        public decimal GetWarehouseQuantity(Guid ProductId, Guid WarehouseId, IOrganizationService service)
        {
            Entity warehouseproduct = service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_warehourseproduct'>
                <attribute name='bsd_warehourseproductid' />
                <attribute name='bsd_quantity' />
                <order attribute='modifiedon' descending='true' />
                <filter type='and'>
                  <condition attribute='bsd_product' operator='eq' uitype='product' value='{0}' />
                  <condition attribute='bsd_warehouses' operator='eq' uitype='bsd_warehouseentity' value='{1}' />
                </filter>
              </entity>
            </fetch>",
            ProductId,
            WarehouseId
            )))?.Entities?.First();
            if (warehouseproduct != null)
            {
                decimal warehouse_quantity = warehouseproduct.HasValue("bsd_quantity") ? (decimal)warehouseproduct["bsd_quantity"] : 0;
                return warehouse_quantity;
            }
            else
            {
                return 0;
            }
        }
        public bool CheckStatusRequestDelivery(Guid RequestId, IOrganizationService service)
        {
            EntityCollection list = service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_requestdeliveryproduct'>
                <attribute name='bsd_requestdeliveryproductid' />
                <attribute name='bsd_warehousestatus' />
                <attribute name='bsd_quantity' />
                <order attribute='bsd_warehousestatus' descending='false' />
                <filter type='and'>
                  <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='{0}' />
                </filter>
              </entity>
            </fetch>", RequestId)));
            bool flag = true;
            foreach (var item in list.Entities)
            {
                if (item.HasValue("bsd_warehousestatus") && !(bool)item["bsd_warehousestatus"])
                {
                    flag = false;
                    break;
                }
            }
            return flag;
        }
        public void UpdateWarehouseStatusRequest(Guid RequestId, IOrganizationService service)
        {
            bool check = CheckStatusRequestDelivery(RequestId, service);
            Entity new_request = new Entity("bsd_requestdelivery", RequestId);
            new_request["bsd_warehousestatus"] = check;
            service.Update(new_request);
        }
        public bool Check_Kho(Guid ProductId, Guid WarehouseId, decimal quantity, IOrganizationService service)
        {
            EntityCollection list_warehouseproduct = service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_warehourseproduct'>
                <attribute name='bsd_warehourseproductid' />
                <attribute name='bsd_quantity' />
                <order attribute='modifiedon' descending='true' />
                <filter type='and'>
                  <condition attribute='bsd_product' operator='eq' uitype='product' value='{0}' />
                  <condition attribute='bsd_warehouses' operator='eq' uitype='bsd_warehouseentity' value='{1}' />
                </filter>
              </entity>
            </fetch>",
            ProductId,
            WarehouseId
            )));

            if (list_warehouseproduct.Entities.Any())
            {
                Entity warehouseproduct = list_warehouseproduct.Entities.First();
                decimal warehouse_quantity = warehouseproduct.HasValue("bsd_quantity") ? (decimal)warehouseproduct["bsd_quantity"] : 0;
                if (quantity > warehouse_quantity)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        public decimal CountProduct(Guid productid, Guid requestdeliveryid, MyService myService)
        {
            EntityCollection list_requestdeliveryproduct = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
            <entity name='bsd_requestdeliveryproduct'>
                <attribute name='bsd_requestdeliveryproductid' />
                <attribute name='bsd_quantity' />
                <filter type='and'>
                  <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + requestdeliveryid + @"' />
                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                </filter>
              </entity>
            </fetch>");
            if (list_requestdeliveryproduct.Entities.Any() && list_requestdeliveryproduct.Entities.First().HasValue("bsd_quantity"))
                return (decimal)list_requestdeliveryproduct.Entities.First()["bsd_quantity"];
            return 0m;
        }

    }
}
