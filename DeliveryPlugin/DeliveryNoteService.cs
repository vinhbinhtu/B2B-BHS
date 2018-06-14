using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DeliveryPlugin.Service;
using Microsoft.Crm.Sdk.Messages;
using Plugin;

namespace DeliveryPlugin
{
    public class DeliveryNoteService
    {
        public MyService myService;
        public DeliveryNoteService(MyService myService)
        {
            this.myService = myService;
        }
        public void Update_StatusDeliveryNote(Guid deliverynote_id)
        {
            Entity deliverynote = myService.service.Retrieve("bsd_deliverynote", deliverynote_id, new ColumnSet("bsd_confirmedreceiptdate", "bsd_deliverybill"));
            if (deliverynote.HasValue("bsd_confirmedreceiptdate"))
            {
                myService.StartService();
                #region Kiểm tra đa cập nhật hết thông tin chưa
                bool flag = true;
                EntityCollection list_deliverynote_product = myService.RetrieveOneCondition("bsd_deliverynoteproduct", "bsd_deliverynote", deliverynote.Id);
                foreach (var item in list_deliverynote_product.Entities)
                {
                    if (!item.Contains("bsd_netquantity") || item["bsd_netquantity"] == null)
                    {
                        flag = false;
                        break;
                    }
                }
                #endregion
                if (flag)
                {
                    bool DaGiaoHet = true;
                    #region cập nhật tình trạng cho sản phẩm trong phiếu giao hàng
                    decimal total_quantity = 0;
                    foreach (var item in list_deliverynote_product.Entities)
                    {
                        Entity new_deliverynote_product = new Entity(item.LogicalName, item.Id);
                        decimal quantity = (decimal)item["bsd_quantity"];
                        decimal net_quantity = (decimal)item["bsd_netquantity"];
                        total_quantity += net_quantity;
                        if (net_quantity == 0)
                        {
                            new_deliverynote_product["bsd_status"] = new OptionSetValue(861450003); // không nhận hàng
                            DaGiaoHet = false;
                        }
                        else if (quantity == net_quantity)
                        {
                            new_deliverynote_product["bsd_status"] = new OptionSetValue(861450002); // Giao hết
                        }
                        else
                        {
                            DaGiaoHet = false;
                            new_deliverynote_product["bsd_status"] = new OptionSetValue(861450001); // Giao 1 phần
                        }
                        myService.Update(new_deliverynote_product);
                    }
                    #endregion

                    #region cập nhật tình trạng giao hàng cho phiéu giao hàng
                    Entity new_deliverynote = new Entity(deliverynote.LogicalName, deliverynote.Id);
                    if (total_quantity == 0)
                    {
                        new_deliverynote["bsd_status"] = new OptionSetValue(861450003); // Không nhận
                    }
                    else if (DaGiaoHet)
                    {
                        new_deliverynote["bsd_status"] = new OptionSetValue(861450002);  //giao hết
                    }
                    else if (DaGiaoHet == false)
                    {
                        new_deliverynote["bsd_status"] = new OptionSetValue(861450001);// giao 1 phần
                    }
                    myService.Update(new_deliverynote);
                    #endregion

                    #region cập nhật ngày giao hàng cho suborder, delivery schedule, request delivery
                    Entity suborder = myService.service.RetrieveMultiple(new FetchExpression(
                    string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                      <entity name='bsd_suborder'>
                        <attribute name='bsd_suborderid' />
                        <attribute name='bsd_name' />
                        <attribute name='bsd_confirmedreceiptdate' />
                        <link-entity name='bsd_deliveryplan' from='bsd_suborder' to='bsd_suborderid' alias='aa'>
                          <attribute name='bsd_deliveryplanid' alias='deliveryplanid' />
                          <link-entity name='bsd_requestdelivery' from='bsd_deliveryplan' to='bsd_deliveryplanid' alias='ab'>
                            <attribute name='bsd_requestdeliveryid' alias='requestdeliveryid' />
                            <link-entity name='bsd_deliverynote' from='bsd_requestdelivery' to='bsd_requestdeliveryid' alias='ac'>
                              <filter type='and'>
                                <condition attribute='bsd_deliverynoteid' operator='eq' uitype='bsd_deliverynote' value='{0}' />
                              </filter>
                            </link-entity>
                          </link-entity>
                        </link-entity>
                      </entity>
                    </fetch>", deliverynote_id)))?.Entities?.First();

                    DateTime confirmedreceiptdate = (DateTime)deliverynote["bsd_confirmedreceiptdate"];

                    #region update suborder
                    if (suborder.HasValue("bsd_confirmedreceiptdate") == false)
                    {
                        Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
                        new_suborder["bsd_confirmedreceiptdate"] = confirmedreceiptdate;
                        myService.service.Update(new_suborder);
                    }
                    #endregion

                    #region update delivery schedule 
                    Guid deliveryscheduleid = Guid.Parse(((AliasedValue)suborder["deliveryplanid"]).Value.ToString());
                    Entity deliveryschedule = new Entity("bsd_deliveryplan", deliveryscheduleid);
                    deliveryschedule["bsd_confirmedreceiptdate"] = confirmedreceiptdate;
                    myService.Update(deliveryschedule);
                    #endregion

                    #region update request deliverry
                    Guid requestdeliveryid = Guid.Parse(((AliasedValue)suborder["requestdeliveryid"]).Value.ToString());
                    Entity requestdelivery = new Entity("bsd_requestdelivery", requestdeliveryid);
                    requestdelivery["bsd_confirmedreceiptdate"] = confirmedreceiptdate;
                    myService.Update(requestdelivery);
                    #endregion

                    #region update lại những phiếu xuât kho đã tạo ra phiếu giao hàng này : Good issute note
                    EntityCollection list_deliverybill = myService.RetrieveOneCondition("bsd_deliverybill", "bsd_deliverynote", deliverynote_id);
                    foreach (var deliverybill in list_deliverybill.Entities)
                    {
                        Entity update_deliverybill = new Entity(deliverybill.LogicalName, deliverybill.Id);
                        update_deliverybill["bsd_confirmedreceiptdate"] = confirmedreceiptdate;
                        myService.Update(update_deliverybill);
                    }
                    #endregion
                    #endregion
                }
            }
        }
        public void Update_SuborderProductStatus(Entity deliverynote_product, decimal preimage_net_quantity, decimal preimage_quantityorder, decimal preimage_quantityappendix)
        {
            decimal net_quantity = (decimal)deliverynote_product["bsd_netquantity"];
            //huy
            decimal net_quantityorder = deliverynote_product.HasValue("bsd_quantityorder") ? (decimal)deliverynote_product["bsd_quantityorder"] : 0;
            decimal net_quantityappendix = deliverynote_product.HasValue("bsd_quantityappendix") ? (decimal)deliverynote_product["bsd_quantityappendix"] : 0;
            //end huy


            EntityReference product_ref = (EntityReference)deliverynote_product["bsd_product"];

            Entity suborder = myService.service.RetrieveMultiple(new FetchExpression(
            string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
              <entity name='bsd_suborder'>
                <attribute name='bsd_suborderid' />
                <attribute name='bsd_name' />
                <attribute name='bsd_appendixcontract' />
                <attribute name='bsd_type' />
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
            </fetch>", deliverynote_product.Id))).Entities.First();

            int type = ((OptionSetValue)suborder["bsd_type"]).Value;
            Entity suborder_product = myService.FetchXml(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_suborderproduct'>
                <all-attributes />
                <filter type='and'>
                  <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='{0}' />
                  <condition attribute='bsd_product' operator='eq' uitype='product' value='{1}' />
                </filter>
              </entity>
            </fetch>", suborder.Id, product_ref.Id)).Entities.FirstOrDefault();

            decimal suborder_quantity = 0m;
            decimal shipped_quantity = 0m;
            decimal new_shipped_quantity = 0m;
            decimal remaining_quantity = 0m;
            Entity new_suborder_product = new Entity(suborder_product.LogicalName, suborder_product.Id);
            if (!suborder.HasValue("bsd_appendixcontract"))//k phu luc
            {
                suborder_quantity = (decimal)suborder_product["bsd_shipquantity"];
                shipped_quantity = (decimal)suborder_product["bsd_shippedquantity"];
                new_shipped_quantity = shipped_quantity - preimage_net_quantity + net_quantity;

                new_suborder_product["bsd_shippedquantity"] = new_shipped_quantity;
                remaining_quantity = suborder_quantity - new_shipped_quantity;
                new_suborder_product["bsd_residualquantity"] = remaining_quantity;
            }
            else
            {
                suborder_quantity = (decimal)suborder_product["bsd_shipquantity"];
                shipped_quantity = (decimal)suborder_product["bsd_shippedquantity"];
                new_shipped_quantity = shipped_quantity - preimage_quantityorder + net_quantityorder;

                //decimal suborder_quantity_appendix = (decimal)suborder_product["bsd_quantityappendix"];
                decimal shipped_quantity_appendix = suborder_product.HasValue("bsd_shippedquantityappendix") ? (decimal)suborder_product["bsd_shippedquantityappendix"] : 0;
                decimal new_shipped_quantity_appendix = shipped_quantity_appendix - preimage_quantityappendix + net_quantityappendix;
                new_suborder_product["bsd_shippedquantity"] = new_shipped_quantity;
                new_suborder_product["bsd_shippedquantityappendix"] = new_shipped_quantity_appendix;
                remaining_quantity = suborder_quantity - new_shipped_quantity - new_shipped_quantity_appendix;
                new_suborder_product["bsd_residualquantity"] = remaining_quantity;

            }


            int status = 0;

            if (remaining_quantity == 0)
            {
                status = 861450003; // giao hết
            }
            else if (remaining_quantity == suborder_quantity)
            {
                status = 861450004; // không nhận
            }
            else
            {
                status = 861450002;  // đã giao 1 phần
            }
            new_suborder_product["bsd_deliverystatus"] = new OptionSetValue(status);
            myService.Update(new_suborder_product);

            this.Update_Status_Suborder_Delvieryplan(suborder);
            this.Update_StatusOrderAndQuote(suborder_product, net_quantity, preimage_net_quantity, net_quantityorder, preimage_quantityorder, net_quantityappendix, preimage_quantityappendix);
        }
        public void Update_Status_Suborder_Delvieryplan(Entity suborder)
        {

            EntityCollection list_suborderproduct = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);

            decimal total_quantity = 0;
            decimal total_shipped = 0;
            int status = 0;

            if (!suborder.HasValue("bsd_appendixcontract"))//không có phụ lục
            {

                foreach (var item in list_suborderproduct.Entities)
                {
                    total_quantity += (decimal)item["bsd_shipquantity"];
                    total_shipped += (decimal)item["bsd_shippedquantity"];
                }


                if (total_shipped == 0)
                {
                    status = 861450004;
                    // Không nhận
                }
                else if (total_shipped == total_quantity)
                {
                    // Giao hết
                    status = 861450003;
                }
                else if (total_shipped < total_quantity)
                {
                    status = 861450002;
                    // giao 1 phần
                }
            }
            else// có phụ lục
            {

                foreach (var item in list_suborderproduct.Entities)
                {
                    total_quantity += (decimal)item["bsd_shipquantity"];

                    total_shipped += (decimal)item["bsd_shippedquantity"] + (item.HasValue("bsd_shippedquantityappendix") ? (decimal)item["bsd_shippedquantityappendix"] : 0m);

                }
                if (total_shipped == 0)
                {
                    status = 861450004;
                    // Không nhận
                }
                else if (total_shipped == total_quantity)
                {
                    // Giao hết
                    status = 861450003;
                }
                else if (total_shipped < total_quantity)
                {
                    status = 861450002;
                    // giao 1 phần
                }

            }
            Entity deliveryplan = myService.RetrieveOneCondition("bsd_deliveryplan", "bsd_suborder", suborder.Id)?.Entities?.First();
            Entity new_deliveryplan = new Entity(deliveryplan.LogicalName, deliveryplan.Id);
            Entity new_suborder = new Entity("bsd_suborder", suborder.Id);

            new_suborder["bsd_status"] = new OptionSetValue(status);
            new_deliveryplan["bsd_status"] = new OptionSetValue(status);
            myService.Update(new_suborder);
            myService.Update(new_deliveryplan);
        }
        public void Update_StatusOrderAndQuote(Entity suborder_product, decimal net_quantity, decimal preimage_net_quantity, decimal net_quantityorder, decimal preimage_quantityorder, decimal net_quantityappendix, decimal preimage_quantityappendix)
        {
            Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)suborder_product["bsd_suborder"]).Id, new ColumnSet(true));

            int type = ((OptionSetValue)suborder["bsd_type"]).Value;
            bool multipleaddress = (bool)suborder["bsd_multipleaddress"];

            #region quote
            if (type == 861450001) // quote
            {
                SuborderService suborderService = new SuborderService(myService);
                Entity quotedetail = suborderService.getQuoteDetailFromSuborderProduct(suborder_product, 2);
                Entity quote = myService.service.Retrieve("quote", ((EntityReference)quotedetail["quoteid"]).Id, new ColumnSet("bsd_havequantity"));

                bool have_quantity = (bool)quote["bsd_havequantity"];

                EntityReference quote_ref = (EntityReference)quotedetail["quoteid"];
                myService.SetState(quote_ref.Id, quote_ref.LogicalName, 0, 1); // mở ra để udpate

                decimal order_quantity = (multipleaddress && have_quantity == false) ? (decimal)quotedetail["bsd_suborderquantity"] : (decimal)quotedetail["quantity"];
                decimal shipped_quantity = (decimal)quotedetail["bsd_shippedquantity"];
                decimal new_shipped_quantity = shipped_quantity - preimage_net_quantity + net_quantity;

                Entity new_quote_product = new Entity(quotedetail.LogicalName, quotedetail.Id);
                new_quote_product["bsd_shippedquantity"] = new_shipped_quantity;
                decimal remaining_quantity = order_quantity - new_shipped_quantity;
                new_quote_product["bsd_residualquantity"] = remaining_quantity;

                int status = 0;
                if (remaining_quantity == 0)
                {
                    status = 861450003; // giao hết
                }
                else if (remaining_quantity == order_quantity)
                {
                    status = 861450004; // không nhận
                }
                else
                {
                    status = 861450002;  // đã giao 1 phần
                }

                new_quote_product["bsd_deliverystatus"] = new OptionSetValue(status);
                myService.Update(new_quote_product);

                #region won quote
                myService.SetState(quote_ref.Id, quote_ref.LogicalName, 1, 2);
                WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
                Entity quoteClose = new Entity("quoteclose");
                quoteClose.Attributes["quoteid"] = new EntityReference("quote", new Guid(quote_ref.Id.ToString()));
                quoteClose.Attributes["subject"] = "Quote Close" + DateTime.Now.ToString();
                winQuoteRequest.QuoteClose = quoteClose;
                winQuoteRequest.Status = new OptionSetValue(-1);
                myService.service.Execute(winQuoteRequest);
                #endregion

                if (have_quantity == false)
                {
                    SuborderService subService = new SuborderService(myService);
                    EntityReference product_ref = (EntityReference)quotedetail["productid"];
                    Entity totalline = subService.Get_LineTotal_QuoteProduct_Quantity(product_ref.Id, quote_ref.Id);
                    Update_DeliveryStatusTotalLine(totalline, preimage_net_quantity, net_quantity);
                }
            }
            #endregion

            #region appendix contract
            else if (type == 861450002 && suborder_product.HasValue("bsd_appendixcontract"))//huy: trường hợp có phụ lục
            {
                Entity appendixcontract = myService.service.Retrieve("bsd_appendixcontract", ((EntityReference)suborder_product["bsd_appendixcontract"]).Id, new ColumnSet(true));
                SuborderService suborderService = new SuborderService(myService);
                Entity salesorderdetail = suborderService.getSalesorderDetailFromSuborderProduct(suborder_product, 2);
                Entity salesorder = myService.service.Retrieve("salesorder", ((EntityReference)salesorderdetail["salesorderid"]).Id, new ColumnSet("bsd_havequantity"));
                Entity appendixdetail = suborderService.getAppendixContractdetailFromSuborderProduct(suborder_product, 2);

                bool have_quantity = (bool)salesorder["bsd_havequantity"];
                decimal order_quantity = (multipleaddress && have_quantity == false) ? (decimal)salesorderdetail["bsd_suborderquantity"] : (decimal)salesorderdetail["quantity"];
                decimal appendix_quantity = (multipleaddress && have_quantity == false) ? (decimal)appendixdetail["bsd_suborderquantityappendix"] : (decimal)appendixdetail["bsd_newquantity"];

                decimal shipped_quantity = (decimal)salesorderdetail["bsd_shippedquantity"];
                decimal shipped_quantity_appendix = appendixdetail.HasValue("bsd_shippedquantityappendix") ? (decimal)appendixdetail["bsd_shippedquantityappendix"] : 0;

                decimal new_shipped_quantity = shipped_quantity - preimage_quantityorder + net_quantityorder;
                decimal new_shipped_quantity_appendix = shipped_quantity_appendix - preimage_quantityappendix + net_quantityappendix;

                #region cập nhật ở hdkt
                Entity new_order_product = new Entity(salesorderdetail.LogicalName, salesorderdetail.Id);
                new_order_product["bsd_shippedquantity"] = new_shipped_quantity;
                decimal remaining_quantity = order_quantity - new_shipped_quantity;
                new_order_product["bsd_residualquantity"] = remaining_quantity;
                int status = 0;
                if (remaining_quantity == 0)
                {
                    status = 861450003; // giao hết
                }
                else if (remaining_quantity == order_quantity)
                {
                    status = 861450004; // không nhận
                }
                else
                {
                    status = 861450002;  // đã giao 1 phần
                }
                new_order_product["bsd_deliverystatus"] = new OptionSetValue(status);
                myService.Update(new_order_product);
                #endregion

                #region cập nhật ở phụ lục
                Entity new_appendix_product = new Entity(appendixdetail.LogicalName, appendixdetail.Id);
                new_appendix_product["bsd_shippedquantityappendix"] = new_shipped_quantity_appendix;
                decimal remaining_quantity_appendix = appendix_quantity - new_shipped_quantity_appendix;
                new_appendix_product["bsd_residualquantity"] = remaining_quantity_appendix;
                int status_appendix = 0;
                if (remaining_quantity_appendix == 0)
                {
                    status_appendix = 861450003; // giao hết
                }
                else if (remaining_quantity_appendix == appendix_quantity)
                {
                    status_appendix = 861450004; // không nhận
                }
                else
                {
                    status_appendix = 861450002;  // đã giao 1 phần
                }
                new_appendix_product["bsd_deliverystatus"] = new OptionSetValue(status_appendix);
                myService.Update(new_appendix_product);
                #endregion

                if (have_quantity == false)
                {

                    SuborderService subService = new SuborderService(myService);

                    EntityReference order_product_ref = (EntityReference)salesorderdetail["productid"];
                    EntityReference order_ref = (EntityReference)salesorderdetail["salesorderid"];
                    Entity totalline = subService.Get_LineTotal_OrderProduct_Quantity(order_product_ref.Id, order_ref.Id);
                    Update_DeliveryStatusTotalLine(totalline, preimage_quantityorder, net_quantityorder);
                }
            }
            #endregion

            #region order
            else if (type == 861450002)// order
            {
                SuborderService suborderService = new SuborderService(myService);

                Entity salesorderdetail = suborderService.getSalesorderDetailFromSuborderProduct(suborder_product, 2);
                Entity salesorder = myService.service.Retrieve("salesorder", ((EntityReference)salesorderdetail["salesorderid"]).Id, new ColumnSet("bsd_havequantity"));

                bool have_quantity = (bool)salesorder["bsd_havequantity"];

                decimal order_quantity = (multipleaddress && have_quantity == false) ? (decimal)salesorderdetail["bsd_suborderquantity"] : (decimal)salesorderdetail["quantity"];
                decimal shipped_quantity = (decimal)salesorderdetail["bsd_shippedquantity"];
                decimal new_shipped_quantity = shipped_quantity - preimage_net_quantity + net_quantity;

                Entity new_order_product = new Entity(salesorderdetail.LogicalName, salesorderdetail.Id);
                new_order_product["bsd_shippedquantity"] = new_shipped_quantity;
                decimal remaining_quantity = order_quantity - new_shipped_quantity;
                new_order_product["bsd_residualquantity"] = remaining_quantity;
                int status = 0;
                if (remaining_quantity == 0)
                {
                    status = 861450003; // giao hết
                }
                else if (remaining_quantity == order_quantity)
                {
                    status = 861450004; // không nhận
                }
                else
                {
                    status = 861450002;  // đã giao 1 phần
                }
                new_order_product["bsd_deliverystatus"] = new OptionSetValue(status);
                myService.Update(new_order_product);
                if (have_quantity == false)
                {
                    SuborderService subService = new SuborderService(myService);
                    EntityReference product_ref = (EntityReference)salesorderdetail["productid"];
                    EntityReference order_ref = (EntityReference)salesorderdetail["salesorderid"];
                    Entity totalline = subService.Get_LineTotal_OrderProduct_Quantity(product_ref.Id, order_ref.Id);
                    Update_DeliveryStatusTotalLine(totalline, preimage_net_quantity, net_quantity);
                }
            }
            #endregion
        }
        public void Update_DeliveryStatusTotalLine(Entity totalline, decimal preimage_net_quantity, decimal net_quantity)
        {

            decimal order_quantity = (decimal)totalline["bsd_quantity"];
            decimal shipped_quantity = (decimal)totalline["bsd_shippedquantity"];
            decimal new_shipped_quantity = shipped_quantity - preimage_net_quantity + net_quantity;

            Entity new_totalline = new Entity(totalline.LogicalName, totalline.Id);
            new_totalline["bsd_shippedquantity"] = new_shipped_quantity;
            decimal remaining_quantity = order_quantity - new_shipped_quantity;
            new_totalline["bsd_residualquantity"] = remaining_quantity;

            int status = 0;
            if (remaining_quantity == 0)
            {
                status = 861450003; // giao hết
            }
            else if (remaining_quantity == order_quantity)
            {
                status = 861450004; // không nhận
            }
            else
            {
                status = 861450002;  // đã giao 1 phần
            }

            new_totalline["bsd_deliverystatus"] = new OptionSetValue(status);
            if (totalline.LogicalName == "quotedetail")
            {
                EntityReference quote_ref = (EntityReference)totalline["quoteid"];
                myService.SetState(quote_ref.Id, quote_ref.LogicalName, 0, 1); // mở ra để udpate
                myService.Update(new_totalline);
                #region won quote
                myService.SetState(quote_ref.Id, quote_ref.LogicalName, 1, 2);
                WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
                Entity quoteClose = new Entity("quoteclose");
                quoteClose.Attributes["quoteid"] = new EntityReference("quote", new Guid(quote_ref.Id.ToString()));
                quoteClose.Attributes["subject"] = "Quote Close" + DateTime.Now.ToString();
                winQuoteRequest.QuoteClose = quoteClose;
                winQuoteRequest.Status = new OptionSetValue(-1);
                myService.service.Execute(winQuoteRequest);
                #endregion
            }
            else
            {
                myService.Update(new_totalline);
            }
        }

    }
}
