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
    public class DeliveryNoteProduct : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            MyService myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1)
                return;
            ITracingService tracing = myService.GetTracingService();

            #region Update
            if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();
                if (target.ContainAndHasValue("bsd_netquantity"))
                {
                    myService.StartService();
                    DeliveryNoteService deliverynoteService = new DeliveryNoteService(myService);
                    Entity deliverynote_product = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_deliverynote", "bsd_product", "bsd_netquantity", "bsd_quantityorder", "bsd_quantityappendix"));
                    EntityReference deliverynote_ref = (EntityReference)deliverynote_product["bsd_deliverynote"];

                    deliverynoteService.Update_StatusDeliveryNote(deliverynote_ref.Id);
                    Entity deliverynote = myService.service.Retrieve(deliverynote_ref.LogicalName, deliverynote_ref.Id, new ColumnSet(true));

                    var status = ((OptionSetValue)deliverynote["bsd_status"]).Value;
                    if (status != 861450000) // Nếu nó khác đang giao. tức là đã giao hàng rồi.
                    {
                        Entity PreImage = myService.context.PreEntityImages["PreImage"];
                        decimal preimage_net_quantity = (decimal)PreImage["bsd_netquantity"];
                        decimal preimage_quantityorder = PreImage.HasValue("bsd_quantityorder") ? (decimal)PreImage["bsd_quantityorder"] : 0;
                        decimal preimage_quantityappendix = PreImage.HasValue("bsd_quantityappendix") ? (decimal)PreImage["bsd_quantityappendix"] : 0;

                        deliverynoteService.Update_SuborderProductStatus(deliverynote_product, preimage_net_quantity, preimage_quantityorder, preimage_quantityappendix);
                    }
                }
            }
            #endregion

            #region Delete
            else if (myService.context.MessageName == "Delete_backup")
            {
                myService.StartService();
                EntityReference target = myService.getTargetEntityReference();
                Entity deliverynoteproduct = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet("bsd_netquantity", "bsd_deliverynote", "bsd_product"));
                EntityReference deliverynote_ref = (EntityReference)deliverynoteproduct["bsd_deliverynote"];
                Entity deliverynote = myService.service.Retrieve(deliverynote_ref.LogicalName, deliverynote_ref.Id, new ColumnSet("bsd_order", "bsd_status", "bsd_quote"));


                Entity suborder = myService.FetchXml(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                      <entity name='bsd_suborder'>
                        <attribute name='bsd_suborderid' />
                        <attribute name='bsd_name' />
                        <attribute name='bsd_appendixcontract' />
                        <attribute name='createdon' />
                        <order attribute='bsd_name' descending='false' />
                        <link-entity name='bsd_deliveryplan' from='bsd_suborder' to='bsd_suborderid' alias='be'>
                          <link-entity name='bsd_requestdelivery' from='bsd_deliveryplan' to='bsd_deliveryplanid' alias='bf'>
                            <link-entity name='bsd_deliverynote' from='bsd_requestdelivery' to='bsd_requestdeliveryid' alias='bg'>
                              <link-entity name='bsd_deliverynoteproduct' from='bsd_deliverynote' to='bsd_deliverynoteid' alias='bh'>
                                <filter type='and'>
                                  <condition attribute='bsd_deliverynoteproductid' operator='eq' uitype='bsd_deliverynoteproduct' value='{0}' />
                                </filter>
                              </link-entity>
                            </link-entity>
                          </link-entity>
                        </link-entity>
                      </entity>
                    </fetch>", deliverynoteproduct.Id))?.Entities?.First();

                DeliveryNoteService deliveryNoteService = new DeliveryNoteService(myService);
                deliveryNoteService.Update_Status_Suborder_Delvieryplan(suborder);
                // cập nhật từ xóa phiếu giao hàng 
            }
            #endregion
        }
    }
}
