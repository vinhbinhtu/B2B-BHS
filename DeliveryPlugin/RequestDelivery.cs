using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using DeliveryPlugin.Service;
using DeliveryPlugin.Model;
using Microsoft.Crm.Sdk.Messages;

namespace DeliveryPlugin
{
    public class RequestDelivery : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
             myService = new MyService(serviceProvider);
            string trace = "";
            try
            {
                #region bsd_CreateRequestDelivery
                if (myService.context.MessageName == "bsd_CreateRequestDelivery")
                {
                    if (myService.context.Depth > 1) return;
                    EntityReference target = myService.getTargetEntityReference();
                    myService.StartService();
                    RequestDeliveryService requestDeliveryService = new RequestDeliveryService();

                    Entity deliveryplan = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    DateTime date = DateTime.Now;

                    Entity new_request = new Entity("bsd_requestdelivery");
                    int deliveryplan_type = ((OptionSetValue)deliveryplan["bsd_type"]).Value;
                    int bsd_status = ((OptionSetValue)deliveryplan["bsd_status"]).Value;
                    if (bsd_status == 861450003)
                    {
                        throw new Exception("Can't create Request Delivery because Delivery Status is fullfil");
                    }
                    if (deliveryplan_type == 861450001)
                    {
                        new_request["bsd_type"] = new OptionSetValue(861450001);
                        new_request["bsd_quote"] = deliveryplan["bsd_quote"];
                    }
                    else if (deliveryplan_type == 861450002)
                    {
                        new_request["bsd_type"] = new OptionSetValue(861450002);
                        new_request["bsd_order"] = deliveryplan["bsd_order"];
                    }
                    trace = "2";

                    new_request["bsd_deliveryplan"] = target;
                    trace = "2.1";
                    if (deliveryplan.HasValue("bsd_potentialcustomer"))
                        new_request["bsd_account"] = deliveryplan["bsd_potentialcustomer"];
                    trace = "2.2";
                    if (deliveryplan.HasValue("bsd_type"))
                        new_request["bsd_type"] = deliveryplan["bsd_type"];
                    trace = "2.3";
                    if (deliveryplan.HasValue("bsd_shiptoaddress"))
                        new_request["bsd_shiptoaddress"] = deliveryplan["bsd_shiptoaddress"];
                    trace = "2.4";
                    //if (deliveryplan.HasValue("bsd_siteaddress"))
                    new_request["bsd_date"] = date;
                    trace = "2.5";
                    if (deliveryplan.HasValue("bsd_site"))
                        new_request["bsd_site"] = deliveryplan["bsd_site"];
                    trace = "2.6";
                    if (deliveryplan.HasValue("bsd_siteaddress"))
                        new_request["bsd_siteaddress"] = deliveryplan["bsd_siteaddress"];
                    trace = "2.7";
                    if (deliveryplan.HasValue("bsd_deliveryfrom"))
                        new_request["bsd_deliveryfrom"] = deliveryplan["bsd_deliveryfrom"];
                    trace = "2.8";
                    if (deliveryplan.HasValue("bsd_shippingfromaddress"))
                        new_request["bsd_shippingfromaddress"] = deliveryplan["bsd_shippingfromaddress"];
                    trace = "2.9";
                    if (deliveryplan.HasValue("bsd_shippingaddress"))
                        new_request["bsd_shippingaddress"] = deliveryplan["bsd_shippingaddress"];
                    trace = "3";
                    if (deliveryplan.HasValue("bsd_port")) new_request["bsd_port"] = deliveryplan["bsd_port"];
                    if (deliveryplan.HasValue("bsd_addressport")) new_request["bsd_addressport"] = deliveryplan["bsd_addressport"];
                    if (deliveryplan.HasValue("bsd_historyreceiptcustomer")) new_request["bsd_historyreceiptcustomer"] = deliveryplan["bsd_historyreceiptcustomer"];
                    trace = "4";
                   // throw new Exception("ikei");
                    #region lấy danh sách devliery plan truck.
                    EntityCollection list_deliveryplan_truck = myService.service.RetrieveMultiple(new FetchExpression(string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_deliveryplantruck'>
                    <attribute name='bsd_deliveryplantruckid' />
                    <attribute name='bsd_licenseplate' />
                    <attribute name='bsd_product' />
                    <attribute name='bsd_unit' />
                    <attribute name='bsd_warehouse' />
                    <attribute name='bsd_quantity' />
                    <attribute name='bsd_deliverytrucktype' />
                    <attribute name='bsd_deliverytruck' />
                    <attribute name='bsd_carrierpartner' />
                    <attribute name='bsd_driver' />
                    <attribute name='bsd_carrierpartner' />
                    <attribute name='bsd_shippingdeliverymethod' />
                    <attribute name='bsd_shippingoption' />
                    <attribute name='bsd_productid' />
                    <attribute name='bsd_descriptionproduct' />
                    <attribute name='bsd_truckload' />
                    <attribute name='bsd_freeitem' />
                    <attribute name='bsd_historyshipper' />
                    <filter type='and'>
                      <condition attribute='bsd_deliveryplan' operator='eq' uitype='bsd_deliveryplan' value='{0}' />
                      <condition attribute='bsd_status' operator='eq' value='861450001' />
                    </filter>
                  </entity>
                </fetch>", target.Id)));
                    #endregion
                    trace = "5";
                    #region Chuyển từ EntityCollection -> List.
                    List<DeliveryPlanTruck_Item> list_deliveryplantruck_item = new List<DeliveryPlanTruck_Item>();
                    foreach (var deliveryplan_truck in list_deliveryplan_truck.Entities)
                    {
                        DeliveryPlanTruck_Item item = new DeliveryPlanTruck_Item();
                        item.Id = deliveryplan_truck.Id;
                        item.bsd_deliverytrucktype = ((OptionSetValue)deliveryplan_truck["bsd_deliverytrucktype"]).Value;
                        item.bsd_licenseplate = deliveryplan_truck["bsd_licenseplate"].ToString();

                        if (deliveryplan_truck.HasValue("bsd_carrierpartner"))
                            item.bsd_carrierpartner = ((EntityReference)deliveryplan_truck["bsd_carrierpartner"]).Id;

                        if (deliveryplan_truck.HasValue("bsd_deliverytruck"))
                            item.bsd_deliverytruck = ((EntityReference)deliveryplan_truck["bsd_deliverytruck"]).Id;

                        if (deliveryplan_truck.HasValue("bsd_driver"))
                            item.bsd_driver = deliveryplan_truck["bsd_driver"].ToString();

                        if (deliveryplan_truck.HasValue("bsd_historyshipper"))
                            item.bsd_historyshipper = deliveryplan_truck["bsd_historyshipper"].ToString();

                        if (deliveryplan_truck.HasValue("bsd_shippingdeliverymethod"))
                            item.ShippingDeliveryMethod = ((OptionSetValue)deliveryplan_truck["bsd_shippingdeliverymethod"]).Value;
                        if (deliveryplan_truck.HasValue("bsd_truckload"))
                            item.truckload = ((EntityReference)deliveryplan_truck["bsd_truckload"]).Id;

                        item.shipping_option = (bool)deliveryplan_truck["bsd_shippingoption"];
                        list_deliveryplantruck_item.Add(item);
                    }
                    #endregion
                    trace = "6";
                    list_deliveryplantruck_item = list_deliveryplantruck_item.GroupBy(i => new { i.bsd_licenseplate, i.bsd_deliverytrucktype, i.bsd_carrierpartner, i.ShippingDeliveryMethod, i.truckload }, (key, group) => group.First()).ToList();
                    trace = "7";
                    foreach (var item in list_deliveryplantruck_item)
                    {
                        trace = "7.1";
                        new_request["bsd_deliverytrucktype"] = new OptionSetValue(item.bsd_deliverytrucktype);
                        trace = "7.2";
                        if (item.bsd_deliverytruck != Guid.Empty)
                        {
                            new_request["bsd_deliverytruck"] = new EntityReference("bsd_deliverytruck", (Guid)item.bsd_deliverytruck);
                        }
                        trace = "7.3";
                        if (item.bsd_carrierpartner != Guid.Empty)
                        {
                            new_request["bsd_carrierpartner"] = new EntityReference("account", (Guid)item.bsd_carrierpartner);
                        }
                        trace = "7.4";
                        if (item.bsd_licenseplate != null)
                        {
                            new_request["bsd_licenseplate"] = item.bsd_licenseplate;
                        }
                        trace = "7.5";
                        if (item.ShippingDeliveryMethod != 0)
                        {
                            new_request["bsd_shippingdeliverymethod"] = new OptionSetValue(item.ShippingDeliveryMethod);
                        }
                        if (item.bsd_historyshipper != null)
                        {
                            new_request["bsd_historycarrierpartner"] = item.bsd_historyshipper;
                        }
                        trace = "7.6";
                        if (item.truckload != Guid.Empty)
                        {
                            new_request["bsd_truckload"] = new EntityReference("bsd_truckload", item.truckload);
                        }
                        trace = "7.7";
                        new_request["bsd_shippingoption"] = item.shipping_option;
                        trace = "7.8";
                        new_request["bsd_driver"] = item.bsd_driver;
                        trace = "7.9";
                        new_request["bsd_warehousestatus"] = false;
                        trace = "7.10";
                        #region lấy giá vận chuyển
                        if (item.shipping_option)
                        {
                            trace = "7.10.1";
                            EntityReference suborder_ref = (EntityReference)deliveryplan["bsd_suborder"];
                            Entity suborder = myService.service.Retrieve(suborder_ref.LogicalName, suborder_ref.Id, new ColumnSet(true));
                            bool request_porter = (bool)suborder["bsd_requestporter"];
                            trace = "7.10.2";
                            EntityReference from_address = (EntityReference)deliveryplan["bsd_shippingfromaddress"];
                            trace = "7.10.3";
                            EntityReference to_address = (EntityReference)deliveryplan["bsd_shippingaddress"];
                            trace = "7.10.4";
                            EntityCollection list_shippingpricelist = null;
                            trace = "7.10.5";
                            //huy
                            #region Huy - lấy danh sách zone để lấy giá vận chuyển
                            Entity toaddress_detail = myService.service.Retrieve(to_address.LogicalName, to_address.Id, new ColumnSet(true));

                            EntityReference province_toaddress = null;
                            EntityReference district_toaddress = null;
                            EntityReference ward_toaddress = null;
                            trace = "7.10.5.1";
                            if (toaddress_detail.HasValue("bsd_province")) province_toaddress = (EntityReference)toaddress_detail["bsd_province"];
                            if (toaddress_detail.HasValue("bsd_district")) district_toaddress = (EntityReference)toaddress_detail["bsd_district"];
                            if (toaddress_detail.HasValue("bsd_ward")) ward_toaddress = (EntityReference)toaddress_detail["bsd_ward"];
                            trace = "7.10.5.2";

                            #region
                            EntityCollection list_zone = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'> 
                            <entity name='bsd_zone'>  
                            <attribute name='bsd_zoneid' />   
                                <attribute name='bsd_name' />  
                                <attribute name='bsd_province' />   
                                <filter type='and'>     
                                    <condition attribute='statecode' operator='eq' value='0' />    
                                    <filter type='or'>        
                                        <filter type='and'>     
                                            <condition entityname='address' attribute='bsd_addressid' operator='eq' uitype='bsd_address' value='"+ to_address.Id + @"' />    
                                        </filter>   
                                        <filter type='and'>            
                                            <condition attribute='bsd_province' operator='not-null' />        
                                            <condition entityname='district' attribute='bsd_districtid' operator='not-null' />
                                            <condition attribute='bsd_province' operator='eq' uitype='bsd_province' value='" + province_toaddress.Id + @"' />        
                                            <condition entityname='district' attribute='bsd_districtid' operator='eq'  uitype='bsd_district' value='" + district_toaddress.Id + @"' />       
                                            <condition entityname='ward' attribute='bsd_wardid' operator='eq'  uitype='bsd_ward' value='" + ward_toaddress.Id + @"' />      
                                        </filter>         
                                        <filter type='and'>     
                                            <condition attribute='bsd_province' operator='not-null' />         
                                            <condition attribute='bsd_province' operator='eq' uitype='bsd_province' value='" + province_toaddress.Id + @"' />        
                                            <condition entityname='district' attribute='bsd_districtid' operator='eq'  uitype='bsd_district' value='" + district_toaddress.Id + @"' />   
                                            <condition entityname='ward' attribute='bsd_wardid' operator='null' />        
                                        </filter>          
                                        <filter type='and'>     
                                            <condition attribute='bsd_province' operator='eq'  uitype='bsd_province' value='" + province_toaddress.Id + @"' />     
                                            <condition entityname='district' attribute='bsd_districtid' operator='null' />
                                            <condition entityname='ward' attribute='bsd_wardid' operator='null' />           
                                        </filter> 
                                    </filter>    
                                </filter>  
                                <link-entity name='bsd_bsd_zone_bsd_district' from='bsd_zoneid' to='bsd_zoneid' visible='false' intersect='true' link-type='outer'>     
                                    <link-entity name='bsd_district' from='bsd_districtid' to='bsd_districtid' alias='district' link-type='outer'>  
                                        <attribute name='bsd_districtid' />      
                                    </link-entity>   
                                </link-entity>  
                                <link-entity name='bsd_bsd_zone_bsd_ward' from='bsd_zoneid' to='bsd_zoneid' visible='false' intersect='true' link-type='outer'>   
                                    <link-entity name='bsd_ward' from='bsd_wardid' to='bsd_wardid' alias='ward' link-type='outer'>   
                                        <attribute name='bsd_wardid' />     
                                    </link-entity>   
                                </link-entity>  
                                <link-entity name='bsd_bsd_zone_bsd_address' from='bsd_zoneid' to='bsd_zoneid' visible='false' intersect='true' link-type='outer'>
                                    <link-entity name='bsd_address' from='bsd_addressid' to='bsd_addressid' alias='address' link-type='outer'>     
                                        <attribute name='bsd_addressid' />   
                                    </link-entity>     
                                </link-entity>  
                            </entity> 
                        </fetch>");
                            #endregion
                            trace = "7.10.5.3";
                            string zoneid_address = "";     //chứa id của zone chứa address
                            string zoneid_ward = "";        //chứa id của zone chứa Ward, district, province
                            string zoneid_district = "";    //chứa id của zone chứa District, province
                            string zoneid_province = "";    //chứa id của zone chỉ chứa Province
                            if (list_zone.Entities.Any())
                            {

                                #region
                                foreach (var zone in list_zone.Entities)
                                {
                                    if (zone.HasValue("address.bsd_addressid"))
                                    {
                                        zoneid_address += "<value uitype='bsd_zone'>{" + zone.Id + "}</value>"; ;
                                    }
                                    else if (zone.HasValue("ward.bsd_wardid"))
                                    {
                                        zoneid_ward += "<value uitype='bsd_zone'>{" + zone.Id + "}</value>";
                                    }
                                    else if (zone.HasValue("district.bsd_districtid") && zone.HasValue("ward.bsd_wardid") == false)
                                    {
                                        zoneid_district += "<value uitype='bsd_zone'>{" + zone.Id + "}</value>";
                                    }
                                    else if (zone.HasValue("district.bsd_districtid") == false && zone.HasValue("ward.bsd_wardid") == false)
                                    {
                                        zoneid_province += "<value uitype='bsd_zone'>{" + zone.Id + "}</value>";
                                    }
                                }
                                #endregion
                            }
                            trace = "7.10.5.4";
                            string[] list_zoneid = { zoneid_address, zoneid_ward, zoneid_district, zoneid_province };//mảng chứa id zone theo mức độ ưu tiên ward>district>province
                            if (list_zoneid[0] == "" && list_zoneid[1] == "" && list_zoneid[2] == "" && list_zoneid[3] == "")
                            {
                                throw new Exception("The Zone Has Not Been Defined !");
                            }
                            trace = "7.10.5.5";
                            #endregion end huy
                            trace = "7.10.6";
                            if (item.ShippingDeliveryMethod == 861450000) // ton
                            {
                                #region
                                EntityReference unit = (EntityReference)Plugin.Util.GetConfigDefault(myService.service)["bsd_unitshippingdefault"];
                                for (int i = 0; i < list_zoneid.Length; i++)
                                {
                                    if (list_zoneid[i] != null && list_zoneid[i] != "")
                                    {
                                        if (request_porter)//fetch giá vận chuyển method: ton, request porter : true
                                        {
                                            list_shippingpricelist = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> 
                                        <entity name='bsd_shippingpricelist'> 
                                            <attribute name='bsd_shippingpricelistid' /> 
                                            <attribute name='bsd_name' />   
                                            <attribute name='createdon' />  
                                            <attribute name='bsd_priceunitporter' />  
                                            <attribute name='bsd_priceofton' />   
                                            <attribute name='bsd_deliverymethod' /> 
                                            <order attribute='bsd_priceunitporter' descending='false' /> 
                                            <order attribute='bsd_priceofton' descending='false' />   
                                            <filter type='and'>     
                                                <condition attribute='statecode' operator='eq' value='0' />
                                                <condition attribute='bsd_unit' operator='eq' uitype='uom' value='" + unit.Id + @"' /> 
                                                <condition attribute='bsd_deliverymethod' operator='eq' value='861450000' />   
                                                <condition attribute='bsd_carrierpartners' operator='eq' uitype='account' value='" + item.bsd_carrierpartner + @"' />
                                                <filter type='or'>      
                                                    <filter type='and'>   
                                                        <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.ToCrmFormat() + @"' />  
                                                        <condition attribute='bsd_effectiveto' operator='on-or-after' value='" + date.ToCrmFormat() + @"' />   
                                                        <condition attribute='bsd_effectiveto' operator='not-null' />  
                                                    </filter>       
                                                    <filter type='and'>      
                                                        <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.ToCrmFormat() + @"' />  
                                                        <condition attribute='bsd_effectiveto' operator='null' /> 
                                                    </filter>      
                                                </filter>    
                                            </filter>   
                                            <link-entity name='bsd_distance' from='bsd_distanceid' to='bsd_route' alias='ae'> 
                                                <filter type='and'> 
                                                    <condition attribute='bsd_siteaddress' operator='eq' uitype='bsd_address' value='" + from_address.Id + @"' />    
                                                </filter>     
                                                <link-entity name='bsd_zone' from='bsd_zoneid' to='bsd_zone' alias='br'>
                                                    <filter type='and'>       
                                                        <condition attribute='bsd_zoneid' operator='in'>
                                                            " + list_zoneid[i] + @"
                                                        </condition> 
                                                    </filter>    
                                                </link-entity>    
                                            </link-entity>  
                                        </entity> 
                                    </fetch>");
                                        }
                                        else //fetch giá vận chuyển method: ton, request porter : false
                                        {
                                            list_shippingpricelist = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> 
                                        <entity name='bsd_shippingpricelist'> 
                                            <attribute name='bsd_shippingpricelistid' /> 
                                            <attribute name='bsd_name' />   
                                            <attribute name='createdon' />  
                                            <attribute name='bsd_priceunitporter' />  
                                            <attribute name='bsd_priceofton' />   
                                            <attribute name='bsd_deliverymethod' /> 
                                            <order attribute='bsd_priceofton' descending='false' />   
                                            <filter type='and'>     
                                                <condition attribute='statecode' operator='eq' value='0' />
                                                <condition attribute='bsd_unit' operator='eq' uitype='uom' value='" + unit.Id + @"' /> 
                                                <condition attribute='bsd_deliverymethod' operator='eq' value='861450000' />   
                                                <condition attribute='bsd_carrierpartners' operator='eq' uitype='account' value='" + item.bsd_carrierpartner + @"' />
                                                <filter type='or'>      
                                                    <filter type='and'>   
                                                        <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.ToCrmFormat() + @"' />  
                                                        <condition attribute='bsd_effectiveto' operator='on-or-after' value='" + date.ToCrmFormat() + @"' />   
                                                        <condition attribute='bsd_effectiveto' operator='not-null' />  
                                                    </filter>       
                                                    <filter type='and'>      
                                                        <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.ToCrmFormat() + @"' />  
                                                        <condition attribute='bsd_effectiveto' operator='null' /> 
                                                    </filter>      
                                                </filter>    
                                            </filter>   
                                            <link-entity name='bsd_distance' from='bsd_distanceid' to='bsd_route' alias='ae'> 
                                                <filter type='and'> 
                                                    <condition attribute='bsd_siteaddress' operator='eq' uitype='bsd_address' value='" + from_address.Id + @"' />    
                                                </filter>     
                                                <link-entity name='bsd_zone' from='bsd_zoneid' to='bsd_zone' alias='br'>
                                                    <filter type='and'>       
                                                        <condition attribute='bsd_zoneid' operator='in'>
                                                            " + list_zoneid[i] + @"
                                                        </condition> 
                                                    </filter>    
                                                </link-entity>    
                                            </link-entity>  
                                        </entity> 
                                    </fetch>");
                                        }
                                        if (list_shippingpricelist.Entities.Any())
                                        {
                                            break;
                                        }
                                    }
                                }
                                #endregion
                                trace = "7.10.7";
                            }
                            else // trip
                            {

                                #region
                                for (int i = 0; i < list_zoneid.Length; i++)
                                {
                                    if (list_zoneid[i] != null && list_zoneid[i] != "")
                                    {
                                        if (request_porter)//fetch giá vận chuyển method: trip, request porter : true
                                        {
                                            list_shippingpricelist = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> 
                                        <entity name='bsd_shippingpricelist'> 
                                            <attribute name='bsd_shippingpricelistid' />   
                                            <attribute name='bsd_name' />   
                                            <attribute name='createdon' />    
                                            <attribute name='bsd_pricetripporter' />    
                                            <attribute name='bsd_priceoftrip' />   
                                            <attribute name='bsd_deliverymethod' /> 
                                            <order attribute='bsd_pricetripporter' descending='false' /> 
                                            <order attribute='bsd_priceoftrip' descending='false' />    
                                            <filter type='and'>      
                                                <condition attribute='statecode' operator='eq' value='0' />  
                                                <condition attribute='bsd_truckload' operator='eq' uitype='bsd_truckload' value='" + item.truckload + @"' /> 
                                                <condition attribute='bsd_deliverymethod' operator='eq' value='861450001' />
                                                <condition attribute='bsd_carrierpartners' operator='eq' uitype='account' value='" + item.bsd_carrierpartner + @"' />
                                                <filter type='or'>     
                                                    <filter type='and'>     
                                                        <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.ToCrmFormat() + @"' />   
                                                        <condition attribute='bsd_effectiveto' operator='on-or-after' value='" + date.ToCrmFormat() + @"' /> 
                                                        <condition attribute='bsd_effectiveto' operator='not-null' />    
                                                    </filter>      
                                                    <filter type='and'>    
                                                        <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.ToCrmFormat() + @"' />         
                                                        <condition attribute='bsd_effectiveto' operator='null' />      
                                                    </filter>   
                                                </filter>   
                                            </filter>   
                                            <link-entity name='bsd_distance' from='bsd_distanceid' to='bsd_route' alias='ae'>      
                                                <filter type='and'>     
                                                    <condition attribute='bsd_siteaddress' operator='eq' uitype='bsd_address' value='" + from_address.Id + @"' />  
                                                </filter>    
                                                <link-entity name='bsd_zone' from='bsd_zoneid' to='bsd_zone' alias='br'>  
                                                    <filter type='and'>       
                                                        <condition attribute='bsd_zoneid' operator='in'>
                                                            " + list_zoneid[i] + @"
                                                        </condition> 
                                                    </filter>     
                                                </link-entity> 
                                            </link-entity> 
                                        </entity> 
                                    </fetch>");
                                        }
                                        else//fetch giá vận chuyển method: trip, request porter : false
                                        {
                                            list_shippingpricelist = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'> 
                                        <entity name='bsd_shippingpricelist'> 
                                            <attribute name='bsd_shippingpricelistid' />   
                                            <attribute name='bsd_name' />   
                                            <attribute name='createdon' />    
                                            <attribute name='bsd_pricetripporter' />    
                                            <attribute name='bsd_priceoftrip' />   
                                            <attribute name='bsd_deliverymethod' /> 
                                            <order attribute='bsd_priceoftrip' descending='false' />    
                                            <filter type='and'>      
                                                <condition attribute='statecode' operator='eq' value='0' />  
                                                <condition attribute='bsd_truckload' operator='eq' uitype='bsd_truckload' value='" + item.truckload + @"' /> 
                                                <condition attribute='bsd_deliverymethod' operator='eq' value='861450001' />   
                                                <condition attribute='bsd_carrierpartners' operator='eq' uitype='account' value='" + item.bsd_carrierpartner + @"' />
                                                <filter type='or'>     
                                                    <filter type='and'>     
                                                        <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.ToCrmFormat() + @"' />   
                                                        <condition attribute='bsd_effectiveto' operator='on-or-after' value='" + date.ToCrmFormat() + @"' /> 
                                                        <condition attribute='bsd_effectiveto' operator='not-null' />    
                                                    </filter>      
                                                    <filter type='and'>    
                                                        <condition attribute='bsd_effectivefrom' operator='on-or-before' value='" + date.ToCrmFormat() + @"' />         
                                                        <condition attribute='bsd_effectiveto' operator='null' />      
                                                    </filter>   
                                                </filter>   
                                            </filter>   
                                            <link-entity name='bsd_distance' from='bsd_distanceid' to='bsd_route' alias='ae'>      
                                                <filter type='and'>     
                                                    <condition attribute='bsd_siteaddress' operator='eq' uitype='bsd_address' value='" + from_address.Id + @"' />  
                                                </filter>    
                                                <link-entity name='bsd_zone' from='bsd_zoneid' to='bsd_zone' alias='br'>  
                                                    <filter type='and'>       
                                                        <condition attribute='bsd_zoneid' operator='in'>
                                                            " + list_zoneid[i] + @"
                                                        </condition> 
                                                    </filter>     
                                                </link-entity> 
                                            </link-entity> 
                                        </entity> 
                                    </fetch>");
                                        }
                                        if (list_shippingpricelist.Entities.Any())
                                        {
                                            break;
                                        }
                                    }
                                }
                                #endregion
                                trace = "7.10.8";
                            }
                            trace = "7.10.9";
                            if (list_shippingpricelist != null && list_shippingpricelist.Entities.Any())
                            {
                                var shippingpricelist = list_shippingpricelist.Entities.First();
                                new_request["bsd_shippingpricelist"] = new EntityReference(shippingpricelist.LogicalName, shippingpricelist.Id);
                            }
                            else
                            {
                                throw new Exception("Shipping Price List Has Not Been Defined !");
                            }
                            trace = "7.10.10";
                        }


                        #endregion
                        trace = "7.11";
                        Guid new_request_id = myService.service.Create(new_request);
                        foreach (var i in list_deliveryplan_truck.Entities)
                        {
                            if (item.bsd_licenseplate == i["bsd_licenseplate"].ToString()
                                && item.bsd_deliverytrucktype == ((OptionSetValue)i["bsd_deliverytrucktype"]).Value
                                && item.shipping_option == (bool)i["bsd_shippingoption"]
                                && item.ShippingDeliveryMethod == (i.HasValue("bsd_shippingdeliverymethod") ? ((OptionSetValue)i["bsd_shippingdeliverymethod"]).Value : 0)
                                && item.truckload == (i.HasValue("bsd_truckload") ? ((EntityReference)i["bsd_truckload"]).Id : Guid.Empty)
                                 && item.bsd_carrierpartner == (i.HasValue("bsd_carrierpartner") ? ((EntityReference)i["bsd_carrierpartner"]).Id : Guid.Empty)
                                )
                            {
                                Entity new_requestdelivery_product = new Entity("bsd_requestdeliveryproduct");
                                new_requestdelivery_product["bsd_requestdelivery"] = new EntityReference("bsd_requestdelivery", new_request_id);
                                new_requestdelivery_product["bsd_uomid"] = i["bsd_unit"];
                                new_requestdelivery_product["bsd_product"] = i["bsd_product"];
                                if (i.HasValue("bsd_productid")) new_requestdelivery_product["bsd_productid"] = i["bsd_productid"];
                                if (i.HasValue("bsd_descriptionproduct")) new_requestdelivery_product["bsd_descriptionproduct"] = i["bsd_descriptionproduct"];
                                #region setname.
                                Entity product = myService.service.Retrieve("product", ((EntityReference)i["bsd_product"]).Id, new ColumnSet("name"));
                                new_requestdelivery_product["bsd_name"] = product["name"];
                                #endregion setname
                                new_requestdelivery_product["bsd_quantity"] = i["bsd_quantity"];
                                new_requestdelivery_product["bsd_remainingquantity"] = i["bsd_quantity"];
                                new_requestdelivery_product["bsd_netquantity"] = 0m;
                                new_requestdelivery_product["bsd_deliveryplantruck"] = new EntityReference(i.LogicalName, i.Id);
                                //new_requestdelivery_product["bsd_warehousestatus"] = requestDeliveryService.Check_Kho(product.Id, ((EntityReference)i["bsd_warehouse"]).Id, (decimal)i["bsd_quantity"], myService.service);
                                new_requestdelivery_product["bsd_warehousestatus"] = false;

                                if (i.HasValue("bsd_freeitem"))
                                    new_requestdelivery_product["bsd_freeitem"] = i["bsd_freeitem"];
                                myService.service.Create(new_requestdelivery_product);

                                #region Tạo Quantity Detail.
                                Entity new_requestdeliveryquantitydetail = new Entity("bsd_requestdeliveryquantitydetail");
                                new_requestdeliveryquantitydetail["bsd_requestdelivery"] = new EntityReference("bsd_requestdelivery", new_request_id);
                                new_requestdeliveryquantitydetail["bsd_product"] = i["bsd_product"];
                                new_requestdeliveryquantitydetail["bsd_quantity"] = i["bsd_quantity"];
                                new_requestdeliveryquantitydetail["bsd_freeitem"] = i["bsd_freeitem"];
                                myService.service.Create(new_requestdeliveryquantitydetail);
                                #endregion

                                #region Cập nhật Status Delivery Plan Truck.
                                Entity new_i = new Entity(i.LogicalName, i.Id);
                                new_i["bsd_status"] = new OptionSetValue(861450000);
                                myService.Update(new_i);
                                #endregion

                                #region tạo Request Delivery Delivery Plan Truck. // để lưu lại request này dc tạo bằng những plan truck nào.
                                Entity requestdeliverydeliveryplantruck = new Entity("bsd_requestdeliverydeliveryplantruck");
                                requestdeliverydeliveryplantruck["bsd_requestdelivery"] = new EntityReference("bsd_requestdelivery", new_request_id);
                                requestdeliverydeliveryplantruck["bsd_deliveryplantruck"] = new EntityReference(i.LogicalName, i.Id);
                                myService.service.Create(requestdeliverydeliveryplantruck);
                                #endregion


                                
                            }
                        }
                        #region Create Check Warehouse Vehicles auto
                        EntityCollection checkWarehouseVehicles = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                <entity name='bsd_checkwarehousevehicles'>
                                <attribute name='bsd_checkwarehousevehiclesid'/>
                                <filter type='and'>
                                    <condition attribute='statuscode' operator='eq' value='1'/>
                                    <condition attribute='bsd_autoload' operator='eq' value='1'/>
                                </filter></entity></fetch>");

                        if (checkWarehouseVehicles != null && checkWarehouseVehicles.Entities != null && checkWarehouseVehicles.Entities.Count > 0)
                        {
                            EntityReference ref_requestdelivery, ref_checkwarehousevehicles;
                            ref_requestdelivery = new EntityReference("bsd_requestdelivery", new_request_id);
                            foreach (Entity record in checkWarehouseVehicles.Entities)
                            {
                                ref_checkwarehousevehicles = new EntityReference("bsd_checkwarehousevehicles", (Guid)record["bsd_checkwarehousevehiclesid"]);
                                this.AssociateManyToManyEntityRecords(ref_requestdelivery, ref_checkwarehousevehicles, "bsd_requestdelivery_checkwarehousevehicle");
                            }
                        }
                        #endregion
                        trace = "7.12";
                    }
                    trace = "8";
                    #region Câp nhật delivery schedule
                    Entity deliveryschedule = new Entity(deliveryplan.LogicalName, deliveryplan.Id);
                    deliveryschedule["bsd_createdrequestdelivery"] = true;
                    myService.Update(deliveryschedule);
                    #endregion
                    trace = "9";
                }
                #endregion

                #region Update
                else if (myService.context.MessageName == "Update")
                {
                    myService.StartService();
                    Entity target = myService.getTarget();
                    Entity request = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    if (target.HasValue("bsd_licenseplate") || target.HasValue("bsd_driver")) // có cập nhật lại biển số.
                    {
                        Entity request_preimage = myService.getImage("PreImage"); // dùng preimage để lấy liensepate trước khi thay đổi
                        EntityReference deliveryplan_ref = (EntityReference)request["bsd_deliveryplan"];
                        string licenseplate = request_preimage["bsd_licenseplate"].ToString();
                        int deliverytrucktype = ((OptionSetValue)request["bsd_deliverytrucktype"]).Value;

                        StringBuilder sb_filter = new StringBuilder();
                        if (deliverytrucktype == 861450002) // shipper thì có 3 field ở dưới
                        {
                            EntityReference bsd_carrierpartner_ref = (EntityReference)request["bsd_carrierpartner"];
                            sb_filter.Append("<condition attribute='bsd_carrierpartner' operator='eq' uitype='account' value='" + bsd_carrierpartner_ref.Id + "' />");

                            int method = ((OptionSetValue)request["bsd_shippingdeliverymethod"]).Value;
                            sb_filter.Append("<condition attribute='bsd_shippingdeliverymethod' operator='eq' value='" + method + "' />");

                            if (request.HasValue("bsd_truckload"))
                            {
                                EntityReference truckload_ref = (EntityReference)request["bsd_truckload"];
                                sb_filter.Append("<condition attribute='bsd_truckload' operator='eq' uiname='10 Tấn' uitype='bsd_truckload' value='" + truckload_ref.Id + "' />");
                            }
                        }

                        EntityCollection list_deliveryplan_truck = myService.FetchXml("<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>"
                         + "<entity name='bsd_deliveryplantruck'>"
                            + "<all-attributes />"
                            + "<filter type='and'>"
                              + "<condition attribute='bsd_deliveryplan' operator='eq' uitype='bsd_deliveryplan' value='" + deliveryplan_ref.Id + @"' />"
                              + "<condition attribute='bsd_status' operator='eq' value='861450000' />"
                              + "<condition attribute='bsd_licenseplate' operator='eq' value='" + licenseplate + "' />"
                              + "<condition attribute='bsd_deliverytrucktype' operator='eq' value='" + deliverytrucktype + "' />"
                            + sb_filter.ToString()
                            + "</filter>"
                          + "</entity>"
                        + "</fetch>");

                        if (list_deliveryplan_truck.Entities.Any())
                        {
                            foreach (Entity deliveryplan_truck in list_deliveryplan_truck.Entities)
                            {
                                Entity update_deliveryplan_truck = new Entity(deliveryplan_truck.LogicalName, deliveryplan_truck.Id);
                                if (target.HasValue("bsd_licenseplate"))
                                {
                                    update_deliveryplan_truck["bsd_licenseplate"] = target["bsd_licenseplate"];
                                }
                                if (target.HasValue("bsd_driver"))
                                {
                                    update_deliveryplan_truck["bsd_driver"] = target["bsd_driver"];
                                }
                                myService.Update(update_deliveryplan_truck);
                            }
                        }
                    }
                }
                #endregion

                #region Delete
                else if (myService.context.MessageName == "Delete")
                {
                    myService.StartService();
                    EntityReference target = myService.getTargetEntityReference();
                    Entity pre_image = myService.context.PreEntityImages["PreImage"];
                    EntityReference deliveryscheulde_ref = (EntityReference)pre_image["bsd_deliveryplan"];
                    Entity deliveryscheulde = new Entity(deliveryscheulde_ref.LogicalName, deliveryscheulde_ref.Id);
                    deliveryscheulde["bsd_createdrequestdelivery"] = false;
                    myService.Update(deliveryscheulde);

                    #region Jion những truck giống nhau lại
                    DeliveryPlanTruck_Service deliveryPlanTruck_Service = new DeliveryPlanTruck_Service(myService);
                    deliveryPlanTruck_Service.JoinTruck(deliveryscheulde_ref.Id);
                    #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + trace);
            }
        }
        public bool AssociateManyToManyEntityRecords(EntityReference moniker1, EntityReference moniker2, string strEntityRelationshipName)
        {
            try
            {
                AssociateEntitiesRequest request = new AssociateEntitiesRequest();
                request.Moniker1 = new EntityReference { Id = moniker1.Id, LogicalName = moniker1.LogicalName };
                request.Moniker2 = new EntityReference { Id = moniker2.Id, LogicalName = moniker2.LogicalName };
                request.RelationshipName = strEntityRelationshipName;
                myService.service.Execute(request);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }

}
