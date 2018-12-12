using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SystemLog
{
    public static class Util
    {
        public static DateTime RetrieveLocalTimeFromUTCTime(DateTime utcTime, IOrganizationService service)
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
        private static int? RetrieveCurrentUsersSettings(IOrganizationService service)
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
        public static EntityReference getUnitDefault_Configdefault(IOrganizationService service)
        {
            //(EntityReference)configdefault["bsd_unitdefault"];
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                      <entity name='bsd_configdefault'>
                                        <attribute name='bsd_configdefaultid' />
                                        <attribute name='createdon' />
                                        <attribute name='bsd_unitdefault' />
                                        <order attribute='createdon' descending='true' />
                                      </entity>
                                    </fetch>";
            EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
            return (EntityReference)(list.Entities.First()["bsd_unitdefault"]);
        }
        public static decimal getFactor_UnitConversion(Entity product, Entity fromunit, EntityReference tounit, bool unitshipping = true)
        {
            decimal factor = 1;
            //if (!fromunit.Equals(tounit))
            //{
            //    string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
            //                  <entity name='bsd_unitconversions'>
            //                    <attribute name='bsd_factor' />
            //                    <filter type='and'>
            //                      <condition attribute='bsd_product' operator='eq' uitype='product' value='" + product.Id + @"' />
            //                      <condition attribute='bsd_fromunit' operator='eq' uitype='uom' value='" + fromunit.Id + @"' />
            //                      <condition attribute='bsd_tounit' operator='eq' uitype='uom' value='" + tounit.Id + @"' />
            //                      <condition attribute='statecode' operator='eq' value='0' />
            //                    </filter>
            //                  </entity>
            //                </fetch>";
            //    EntityCollection unitconversion = myService.FetchXml(xml);
            //    if (unitconversion.Entities.Any())
            //    {
            //        factor = (decimal)unitconversion.Entities.FirstOrDefault()["bsd_factor"];
            //    }
            //    else
            //    {
            //        if (unitshipping)
            //        {
            //            throw new Exception("Shipping Unit Conversion has not been defined !");
            //        }
            //        else
            //        {
            //            throw new Exception("Unit Conversion has not been defined !");
            //        }
            //    }
            //}
            return factor;
        }
        public static EntityCollection getShippingPriceList(DateTime date, string conditionOrder, string conditionMain, Guid itemRoute, Guid carrierPartner, IOrganizationService service)
        {
            string xml = "";
            try
            {
                xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_shippingpricelist'>
                                <attribute name='bsd_shippingpricelistid' />
                                <attribute name='bsd_name' />
                                <attribute name='bsd_priceunitporter' />
                                <attribute name='bsd_priceofton' />
                                <attribute name='bsd_pricetripporter' />
                                <attribute name='bsd_truckload' />
                                <attribute name='bsd_priceoftrip' />
                                " + conditionOrder + @"
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <filter type='or'>
                                    <filter type='and'>
                                      <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.Date.ToString("yyyy-MM-dd") + @"' />
                                      <condition attribute='bsd_effectiveto' operator='on-or-after' value='" + date.Date.ToString("yyyy-MM-dd") + @"' />
                                      <condition attribute='bsd_effectiveto' operator='not-null' />
                                    </filter>
                                    <filter type='and'>
                                      <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.Date.ToString("yyyy-MM-dd") + @"' />
                                      <condition attribute='bsd_effectiveto' operator='null' />
                                    </filter>
                                  </filter>
                                  <condition attribute='bsd_route' operator='eq' uiname='Route sTransfer Order' uitype='bsd_distance' value='" + itemRoute + @"' />
                                  <condition attribute='bsd_carrierpartners' operator='eq' uitype='account' value='" + carrierPartner + @"' />
                                  " + conditionMain + @"
                                </filter>
                              </entity>
                            </fetch>";
                EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
                return list;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private static EntityReference getUnitShipping_Configdefault(IOrganizationService service)
        {
            throw new NotImplementedException();
        }
        internal static string retrivestringvaluelookup(string LogicalName, string fieldName, string Value, IOrganizationService service)
        {
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            if (LogicalName != "uom")
            {
                fetchxmm += "<condition attribute='statecode' operator='eq' value='0' />";
            }
            fetchxmm += "<condition attribute='" + fieldName + "' operator='eq' value='" + Value + "' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }
        public static string getInvoiceSuborder(IOrganizationService service, string SuborderId, string CodeAx)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_invoiceax'>
                                <attribute name='bsd_invoiceaxid' />
                                <attribute name='bsd_name' />
                                <attribute name='createdon' />
                                <attribute name='bsd_serial' />
                                <attribute name='bsd_codeax' />
                                <attribute name='bsd_suborder' />
                                <order attribute='createdon' descending='true' />
                                <filter type='and'>
                                  <condition attribute='bsd_codeax' operator='eq' value='" + CodeAx.Trim() + @"' />
                                </filter>
                                <link-entity name='bsd_suborder' from='bsd_suborderid' to='bsd_suborder' alias='ab'>
                                  <filter type='and'>
                                    <condition attribute='bsd_suborderax' operator='eq' value='" + SuborderId.Trim() + @"' />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";

            EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
            if (list.Entities.Count() > 0)
            {
                return list.Entities.First().Id.ToString();
            }
            return null;

        }
        public static EntityCollection getRouteTypePurChaseOrder(IOrganizationService service, Guid toSiteId, string vendorId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_distance'>
                    <attribute name='bsd_distanceid' />
                    <filter type='and'>
                      <condition attribute='bsd_type' operator='eq' value='100000000' />
                         <condition attribute='bsd_vendorid' operator='eq' value='" + vendorId.Trim() + @"' />
                        <condition attribute='bsd_tosite' operator='eq'  uitype='bsd_site' value='" + toSiteId + @"' />
                      <condition attribute='statecode' operator='eq' value='0' />
                    </filter>
                  </entity>
                </fetch>";

            EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
            return list;

        }
        public static EntityCollection getReturnOrderBySalesOrder(IOrganizationService service, Guid SalesOrdeId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                  <entity name='bsd_suborder'>
                                    <attribute name='bsd_name' />
                                    <attribute name='createdon' />
                                    <attribute name='bsd_totalamount' />
                                    <attribute name='bsd_date' />
                                    <attribute name='statecode' />
                                    <attribute name='ownerid' />
                                    <attribute name='bsd_suborderid' />
                                    <attribute name='bsd_detailamount' />
                                    <attribute name='bsd_totaltax' />
                                    <order attribute='bsd_name' descending='false' />
                                     <filter type='and'>
                                      <condition attribute='statuscode' operator='eq' value='861450002' />
                                    </filter>
                                    <link-entity name='bsd_returnorder' from='bsd_returnorderid' to='bsd_returnorder' alias='ad'>
                                      <filter type='and'>
                                        <condition attribute='bsd_findsuborder' operator='eq'  uitype='bsd_suborder' value='"+ SalesOrdeId + @"' />
                                      </filter>
                                    </link-entity>
                                  </entity>
                                </fetch>";

            EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
            return list;

        }
        public static Entity getProduct(string productNumber, IOrganizationService service)
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
            if (!list.Entities.Any()) return null;
            return list.Entities.First();
        }
        public static Entity getAccount(string accountNumber, IOrganizationService service)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='account'>
                                <attribute name='name' />
                                <attribute name='primarycontactid' />
                                <attribute name='telephone1' />
                                <attribute name='accountid' />
                                <order attribute='name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='accountnumber' operator='eq' value='" + accountNumber.Trim() + @"' />
                                  <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection list = service.RetrieveMultiple(new FetchExpression(xml));
            if (!list.Entities.Any()) throw new Exception("Carrier partner " + accountNumber + " not found in CRM");
            return list.Entities.First();
        }
        public static Entity GetConfigDefault(IOrganizationService service)
        {
            return service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='bsd_configdefault'><all-attributes /></entity></fetch>")).Entities.First();
        }
        public static bool isvalidfileld(string LogicalName, string fieldName, string Value, IOrganizationService service)
        {
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            if (LogicalName != "uom")
            {
                fetchxmm += "<condition attribute='statecode' operator='eq' value='0' />";
            }
            fetchxmm += "<condition attribute='" + fieldName + "' operator='eq' value='" + Value + "' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string retrivestringvaluelookuplike(string LogicalName, string fieldName, string Value, IOrganizationService service)
        {
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            if (LogicalName != "uom")
            {
                fetchxmm += "<condition attribute='statecode' operator='eq' value='0' />";
            }
            fetchxmm += "<condition attribute='" + fieldName + "' operator='like' value='%" + Value + "%' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }

        public static string retriveLookup(string LogicalName, string fieldName, string Value, IOrganizationService service)
        {
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            if (LogicalName != "uom")
            {
                fetchxmm += "<condition attribute='statecode' operator='eq' value='0' />";
            }
            fetchxmm += "<condition attribute='" + fieldName + "' operator='eq' value='" + Value + "' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }
        public static decimal? GetFactor(IOrganizationService _service, Guid productid, Guid fromunitid, Guid tounitid)
        {
            if (fromunitid.Equals(tounitid))
            {
                return 1;
            }
            else
            {
                string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_unitconversions'>
                    <attribute name='bsd_unitconversionsid' />
                    <attribute name='bsd_tounit' />
                    <attribute name='bsd_fromunit' />
                    <attribute name='bsd_factor' />
                    <filter type='and'>
                      <condition attribute='bsd_product' operator='eq' uitype='product' value='" + productid + @"' />
                      <condition attribute='bsd_fromunit' operator='eq' uitype='uom' value='" + fromunitid + @"' />
                      <condition attribute='bsd_tounit' operator='eq' uitype='uom' value='" + tounitid + @"' />
                    </filter>
                  </entity>
                </fetch>";
                EntityCollection list_unitconversion = _service.RetrieveMultiple(new FetchExpression(xml));
                if (list_unitconversion.Entities.Any())
                {
                    return (decimal)list_unitconversion.Entities.First()["bsd_factor"];
                }
                else
                {
                    return null;
                }
            }
        }
        public static void Throw(this object obj)
        {
            throw new Exception(obj.ToString());
        }
    }

}
