using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SystemLog.Object
{
    public class InvoiceSuborder
    {
        public static IOrganizationService service;
        #region attributes
        public string RecId { get; set; }

        public string SuborderID { get; set; }

        public string Serial { get; set; }

        public string CustomerCode { get; set; }

        public string CustomerName { get; set; }

        public string Invoice { get; set; }

        public string Warehouse { get; set; }

        public DateTime InvoiceDate { get; set; }

        public DateTime Date { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal TotalTax { get; set; }

        public decimal ExtendedAmount { get; set; }

        public decimal ExchangeRate { get; set; }

        public DateTime PaymentDate { get; set; }

        public string Description { get; set; }

        public string Currency { get; set; }

        public List<PackingList> PackingList { get; set; }
        #endregion
        public static InvoiceSuborder JsonParse(string jsonObject)

        {
            /// throw new Exception(jsonObject);
            InvoiceSuborder obj;
            using (MemoryStream DeSerializememoryStream = new MemoryStream())
            {
                //initialize DataContractJsonSerializer object and pass Student class type to it
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(InvoiceSuborder));

                //user stream writer to write JSON string data to memory stream
                StreamWriter writer = new StreamWriter(DeSerializememoryStream);
                writer.Write(jsonObject);
                writer.Flush();
                DeSerializememoryStream.Position = 0;
                //get the Desrialized data in object of type Student
                obj = (InvoiceSuborder)serializer.ReadObject(DeSerializememoryStream);
            }
            return obj;
        }
        public static void Create(InvoiceSuborder obj, IOrganizationService myService)
        {
            try
            {
                bool flat = true;
                //throw new Exception("okie");
                Guid idInvoice = Guid.Empty;
                string entityName = "bsd_invoiceax";
                service = myService;
               
                if (string.IsNullOrEmpty(obj.Serial) || string.IsNullOrEmpty(obj.Invoice)) throw new Exception("Serial or Invoice No. is not null");
                //string invoiceSubOrderId = Util.retrivestringvaluelookup("bsd_invoiceax", "bsd_codeax", obj.Serial.Trim() + "-" + obj.Invoice.Trim() +"-"+obj.SuborderID.Trim(), service);
                string invoiceSubOrderId = Util.getInvoiceSuborder(service, obj.SuborderID.Trim(), obj.Serial.Trim() + "-" + obj.Invoice.Trim());
                
                if (invoiceSubOrderId != null)
                {
                    idInvoice = Guid.Parse(invoiceSubOrderId);
                    Entity invoiceSubOrder = service.Retrieve("bsd_invoiceax", Guid.Parse(invoiceSubOrderId), new ColumnSet(true));
                    EntityReference suborder_rf = (EntityReference)invoiceSubOrder["bsd_suborder"];
                    Entity suborder = service.Retrieve(suborder_rf.LogicalName, suborder_rf.Id, new ColumnSet("bsd_suborderax"));
                    if (suborder.HasValue("bsd_suborderax"))
                    {
                        //Trường hợp đơn hàng xuất khẩu cập nhật invoice suborder
                        flat = false;
                    }
                }
                else // Nhiều Return Order cho 1 Invoice
                {
                   
                    #region Nhiều Return Order cho 1 Invoice
                    string SuborderID = Util.retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID.Trim(), service);
                  
                    if (SuborderID != null)
                    {
                        Entity suborder = service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date", "bsd_type", "bsd_returnorder"));
                        if (suborder.HasValue("bsd_type"))
                        {
                            //throw new Exception("jdj:"+((OptionSetValue) suborder["bsd_type"]).Value);
                            if (((OptionSetValue)suborder["bsd_type"]).Value ==861450004)//type Return Order
                            {
                                //throw new Exception("dd");
                                //invoiceSubOrderId = Util.retrivestringvaluelookup("bsd_invoiceax", "bsd_codeax", obj.Serial.Trim() + "-" + obj.Invoice.Trim(), service);
                                //if (invoiceSubOrderId != null)
                                //{
                                //Tìm suborder đơn hàng bán với hóa đơn bán
                                // idInvoice = Guid.Parse(invoiceSubOrderId);
                                EntityReference returnorder_Rf = (EntityReference)suborder["bsd_returnorder"];
                                Entity returnorder = service.Retrieve(returnorder_Rf.LogicalName, returnorder_Rf.Id, new ColumnSet("bsd_findsuborder"));
                                EntityReference suborder_rf = (EntityReference)returnorder["bsd_findsuborder"];
                                #region lấy danh sách đơn hàng trả thuộc đơn hàng bán
                                EntityCollection lstReturnOrder = Util.getReturnOrderBySalesOrder(service, suborder_rf.Id);
                               // throw new Exception("Count:" + suborder_rf.Id +"----"+ lstReturnOrder.Entities.Count);
                                if (lstReturnOrder.Entities.Count > 1)
                                {
                                    foreach (var item in lstReturnOrder.Entities)
                                    {
                                        #region Insert invoice Suborder
                                        Entity entity = new Entity(entityName);
                                        entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                                        // entity["bsd_date"] = obj.Date;
                                        entity["bsd_invoicedate"] = obj.InvoiceDate;
                                        if (obj.CustomerCode != null)
                                            entity["bsd_accountid"] = obj.CustomerCode;
                                        if (obj.Serial != null)
                                            entity["bsd_serial"] = obj.Serial;
                                        if (obj.Invoice != null)
                                            entity["bsd_name"] = obj.Invoice;
                                        if (obj.Description != null)
                                            entity["bsd_description"] = obj.Description;
                                        if (obj.TotalAmount.ToString() != null)
                                            entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                                        if (obj.TotalTax.ToString() != null)
                                            entity["bsd_totaltax"] = new Money(obj.TotalTax);
                                        entity["bsd_exchangerate"] = 1m;
                                        if (obj.ExchangeRate.ToString() != null)
                                            entity["bsd_exchangerate"] = obj.ExchangeRate;
                                        if (obj.ExtendedAmount.ToString() != null)
                                            entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                                        entity["bsd_paymentdate"] = obj.PaymentDate;

                                        #region gán gia trị từ suborder sang invoice
                                        if (item.HasValue("bsd_detailamount"))
                                            entity["bsd_extendedamount"] = (Money)item["bsd_detailamount"];
                                        if (item.HasValue("bsd_totaltax"))
                                            entity["bsd_totaltax"] = (Money)item["bsd_totaltax"];
                                        if (item.HasValue("bsd_totalamount"))
                                            entity["bsd_totalamount"] = (Money)item["bsd_totalamount"];
                                        entity["bsd_suborder"] = new EntityReference("bsd_suborder", item.Id);
                                        if (item.HasValue("bsd_date"))
                                            entity["bsd_date"] = item["bsd_date"];
                                        #endregion
                                        #region entity lookup
                                        string CustomerCode = Util.retriveLookup("account", "accountnumber", obj.CustomerCode, service);
                                        if (CustomerCode != null)
                                        {
                                            entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                                        }
                                        string Currency = Util.retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, service);
                                        if (Currency != null)
                                        {
                                            entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                                        }
                                        else
                                            throw new Exception("Iso currencycode " + obj.Currency + " not found CRM");

                                        #endregion
                                        idInvoice = service.Create(entity);


                                        #endregion
                                    }
                                    flat = false;
                                }
                                #endregion
                                //suborder = service.Retrieve(suborder_rf.LogicalName, suborder_rf.Id, new ColumnSet("bsd_suborderax"));
                                //}
                            }
                        }
                    }
                    else throw new Exception("Suborder " + obj.SuborderID + " not found in CRM");
                    #endregion
                }
                if (flat == true)
                {
                    #region Insert invoice Suborder
                    Entity entity = new Entity(entityName);
                    entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                    // entity["bsd_date"] = obj.Date;
                    entity["bsd_invoicedate"] = obj.InvoiceDate;
                    if (obj.CustomerCode != null)
                        entity["bsd_accountid"] = obj.CustomerCode;
                    if (obj.Serial != null)
                        entity["bsd_serial"] = obj.Serial;
                    if (obj.Invoice != null)
                        entity["bsd_name"] = obj.Invoice;
                    if (obj.Description != null)
                        entity["bsd_description"] = obj.Description;
                    if (obj.TotalAmount.ToString() != null)
                        entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                    if (obj.TotalTax.ToString() != null)
                        entity["bsd_totaltax"] = new Money(obj.TotalTax);
                    entity["bsd_exchangerate"] = 1m;
                    if (obj.ExchangeRate.ToString() != null)
                        entity["bsd_exchangerate"] = obj.ExchangeRate;
                    if (obj.ExtendedAmount.ToString() != null)
                        entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                    entity["bsd_paymentdate"] = obj.PaymentDate;
                    #region entity lookup
                    string SuborderID = Util.retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, service);
                    if (SuborderID != null)
                    {
                        entity["bsd_suborder"] = new EntityReference("bsd_suborder", Guid.Parse(SuborderID));
                        Entity suborder = service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date"));
                        if (suborder.HasValue("bsd_date"))
                            entity["bsd_date"] = suborder["bsd_date"];
                    }
                    else throw new Exception("Suborder " + obj.SuborderID + " not found in CRM");

                    string CustomerCode = Util.retriveLookup("account", "accountnumber", obj.CustomerCode, service);
                    if (CustomerCode != null)
                    {
                        entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                    }
                    string Currency = Util.retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, service);
                    if (Currency != null)
                    {
                        entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                    }
                    else
                        throw new Exception("Iso currencycode " + obj.Currency + " not found CRM");

                    #endregion
                    idInvoice = service.Create(entity);


                    #endregion
                }
                #region Update Invoice into Delivery Note
                if (obj.PackingList.Any())
                {
                    string bsd_packingslip = "";
                    int i = 0;
                    foreach (var item in obj.PackingList)
                    {
                        if (item != null)
                        {
                            string deliverynoteID = Util.retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), service);
                            if (deliverynoteID != null)
                            {
                                if (i == 0)
                                    bsd_packingslip += item.PackingListID;
                                else bsd_packingslip += "-" + item.PackingListID;
                                Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                                deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                                deliverynote["bsd_invoicenumberax"] = new EntityReference(entityName, idInvoice);
                                deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                                service.Update(deliverynote);
                            }

                        }
                        i++;
                    }
                    Entity invoice_Update = new Entity(entityName, idInvoice);
                    invoice_Update["bsd_packingslip"] = bsd_packingslip;
                    service.Update(invoice_Update);
                }
                #endregion

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
    }

    public class PackingList
    {
        public string PackingListID { get; set; }
    }
}
