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
    public class TransferOrder
    {
        public static IOrganizationService service;
        public string bsd_name { get; set; }

        public DateTime bsd_shipdate { get; set; }

        public DateTime bsd_receiptdate { get; set; }

        public string bsd_tosite { get; set; }

        public string bsd_towarehouse { get; set; }

        public string bsd_fromsite { get; set; }

        public string bsd_description { get; set; }


        public string bsd_fromwarehouse { get; set; }


        public List<TransferOrderProduct> TransferOrderProduct { get; set; }

        public string RecId { get; set; }
        public string bsd_fromaddressid { get; set; }
        public string bsd_toaddressid { get; set; }
        public static void Create(TransferOrder obj, IOrganizationService myService)
        {
            string entityName = "bsd_transferorder";
            service = myService;
            Entity transferOrder = new Entity(entityName);
            transferOrder["bsd_type"] = new OptionSetValue(861450001);//Purchase Order Type
            transferOrder["bsd_name"] = obj.bsd_name;
            if (obj.bsd_receiptdate != null)
                transferOrder["bsd_receiptdate"] = obj.bsd_receiptdate;
            if (obj.bsd_shipdate != null)
                transferOrder["bsd_shipdate"] = obj.bsd_shipdate;
            Guid transferOrderId = service.Create(transferOrder);
            foreach (var item in obj.TransferOrderProduct)
            {
                Entity transferOrderProduct = new Entity("bsd_transferorderproduct");
                transferOrderProduct["bsd_transferorder"] = new EntityReference(entityName, transferOrderId);
                Entity product = getProduct(item.productnumber.Trim());
                transferOrderProduct["bsd_name"] = product["name"];
                transferOrderProduct["bsd_product"] = new EntityReference(product.LogicalName, product.Id);
                transferOrderProduct["bsd_productid"] = item.productnumber.Trim().ToUpper();
                transferOrderProduct["bsd_unit"] = (EntityReference)product["defaultuomid"];
                transferOrderProduct["bsd_quantity"] = item.bsd_quantity;
                transferOrderProduct["bsd_deliveryfee"] = true;//item.bsd_deliveryfee;
                transferOrderProduct["bsd_porter"] = item.bsd_porter;
                transferOrderProduct["bsd_driver"] = item.bsd_driver;
                if (item.bsd_carrierpartner != null)
                {
                    Entity bsd_carrierpartner = getAccount(item.bsd_carrierpartner);
                    transferOrderProduct["bsd_carrierpartner"] = new EntityReference(bsd_carrierpartner.LogicalName, bsd_carrierpartner.Id);
                }
                else throw new Exception("Carrier partner is not null");
                // transferOrderProduct["bsd_carrierpartner"] = item.bsd_carrierpartner;
                transferOrderProduct["bsd_licenseplate"] = item.bsd_licenseplate;
                int bsd_deliverymethod = 861450000;//Ton
                if (item.bsd_deliverymethod.ToLower() == "trip" || item.bsd_deliverymethod.ToLower() == "861450001") bsd_deliverymethod = 861450001;
                transferOrderProduct["bsd_deliverymethod"] = new OptionSetValue(bsd_deliverymethod);
                service.Create(transferOrderProduct);
            }

        }
        public static void Update(TransferOrder obj, IOrganizationService myService)
        {
            string entityName = "bsd_transferorder";
            service = myService;
            string transferOrderid = Util.retriveLookup(entityName, "bsd_name", obj.bsd_name.Trim(), service);
            if (transferOrderid != null)
            {
                foreach (var item in obj.TransferOrderProduct)
                {
                    string transferOrderProductid = Util.retriveLookup("bsd_transferorderproduct", "bsd_productid",item.productnumber.Trim().ToUpper(), service);
                    if (transferOrderProductid != null)
                    {
                        Entity TransferOrderProduct_Update = new Entity("bsd_transferorderproduct",Guid.Parse(transferOrderProductid));
                        TransferOrderProduct_Update["bsd_quantity"] = item.bsd_quantity;
                        service.Update(TransferOrderProduct_Update);
                    }
                }
               
            }
            else throw new Exception("Purchase Order Id not found in CRM");
        }
        public static Entity getProduct(string productNumber)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='product'>
                            <attribute name='name' />
                            <attribute name='productnumber' />
                            <attribute name='description' />
                            <attribute name='statecode' />
                            <attribute name='defaultuomscheduleid' />
                            <attribute name='defaultuomid' />
                            <order attribute='productnumber' descending='false' />
                            <filter type='and'>
                              <condition attribute='productnumber' operator='eq' value='" + productNumber.Trim() + @"' />
                              <condition attribute='statecode' operator='eq' value='0' />
                            </filter>
                          </entity>
                        </fetch>";
            EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
            if (!list.Entities.Any()) throw new Exception(productNumber + " not found in CRM");
            return list.Entities.First();
        }
        public static Entity getAccount(string accountNumber)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='product'>
                                <attribute name='name' />
                                <attribute name='productnumber' />
                                <attribute name='description' />
                                <attribute name='statecode' />
                                <order attribute='productnumber' descending='false' />
                                <filter type='and'>
                                  <condition attribute='productnumber' operator='eq' value='" + accountNumber.Trim() + @"' />
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
            if (!list.Entities.Any()) throw new Exception("Carrier partner " + accountNumber + " not found in CRM");
            return list.Entities.First();
        }
        public static TransferOrder JsonParse(string jsonObject)
        {
            TransferOrder obj;
            using (MemoryStream DeSerializememoryStream = new MemoryStream())
            {
                //initialize DataContractJsonSerializer object and pass Student class type to it
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(TransferOrder));

                //user stream writer to write JSON string data to memory stream
                StreamWriter writer = new StreamWriter(DeSerializememoryStream);
                writer.Write(jsonObject);
                writer.Flush();
                DeSerializememoryStream.Position = 0;
                //get the Desrialized data in object of type Student
                obj = (TransferOrder)serializer.ReadObject(DeSerializememoryStream);
            }
            return obj;
        }
    }

    public class TransferOrderProduct
    {

        public string productnumber { get; set; }

        public string bsd_unit { get; set; }

        public decimal bsd_quantity { get; set; }

        public string RecId { get; set; }

        public bool bsd_deliveryfee { get; set; }

        public string bsd_deliverymethod { get; set; }

        public string bsd_carrierpartner { get; set; }

        public string bsd_licenseplate { get; set; }

        public string bsd_driver { get; set; }

        public bool bsd_porter { get; set; }
    }
}
