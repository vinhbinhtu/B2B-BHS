using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_UpdateWareHouseProduct
{
    public class Main : IPlugin
    {
        private IOrganizationServiceFactory factory;
        public IOrganizationService service { get; set; }
        public IPluginExecutionContext context { get; set; }

        // public IOrganizationService b2bservice { get; set; }
        public string _userName = "";
        public string _passWord = "";
        public string _company = "";
        public string _port = "";
        public string _domain = "";
        public void Execute(IServiceProvider serviceProvider)
        {

            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);
            if (context.MessageName == "bsd_Action_CheckUpdateWareHouseProduct")
            {
              
             
                EntityReference target = (EntityReference)context.InputParameters["Target"];
                try
                {

                 
                    bool connect_ax = getConfigdefaultconnectax();
                    if (connect_ax == false)
                    {
                        throw new Exception("Thiếu khai báo connect ax");
                    }
                    #region KHai báo service AX
                    NetTcpBinding binding = new NetTcpBinding();
                    //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                    binding.Name = "NetTcpBinding_BHS_BSD_CRMSERVICEAXService";
                    EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://" + _port + "/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));
                    ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                    client.ClientCredentials.Windows.ClientCredential.Domain = _domain;
                    client.ClientCredentials.Windows.ClientCredential.UserName = _userName;
                    client.ClientCredentials.Windows.ClientCredential.Password = _passWord;
                    ServiceReferenceAIF.CallContext contextService = new ServiceReferenceAIF.CallContext() { Company = _company };
                    #endregion
                    string bsd_site = "";
                    string productnumber = "";
                    string lst_CheckProductAX = "";
                    string lst_Product = "";
                    Entity requestDelivery = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    // throw new Exception("okie");
                    if (requestDelivery.HasValue("bsd_site"))
                    {
                        #region 
                        EntityReference rf_Entity = (EntityReference)requestDelivery["bsd_site"];

                        #endregion
                        EntityCollection lstWarehouse;
                        if (getConsignmentSitesConfigDefault(rf_Entity.Id))
                        {

                            lstWarehouse = getWareHouse();
                        }
                        else
                            lstWarehouse = getWareHouse(rf_Entity.Id);
                        #region 
                        EntityCollection lstRequestDeliveryProduct = getRequestDeliveryProduct(requestDelivery.Id);
                        //string s_Result1 = client.BHS_ValidateOnHand(contextService, "01D.01.013:102:KD01-102:0");
                        //throw new Exception(s_Result1);
                        if (lstWarehouse.Entities.Any() && lstRequestDeliveryProduct.Entities.Any())
                        {
                            //throw new Exception(lstWarehouse.Entities.Count.ToString());
                            foreach (Entity warehouse in lstWarehouse.Entities)
                            {
                              

                                
                                int i = 0;
                                rf_Entity = (EntityReference)warehouse["bsd_site"];
                                Entity en_Site = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                                if (en_Site.HasValue("bsd_code")) bsd_site = en_Site["bsd_code"].ToString().Trim();
                                foreach (Entity RequestDeliveryProduct in lstRequestDeliveryProduct.Entities)
                                {
                                    rf_Entity = (EntityReference)RequestDeliveryProduct["bsd_product"];
                                    Entity en = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("productnumber"));
                                    if (en.HasValue("productnumber")) productnumber = en["productnumber"].ToString().Trim();
                                    if (i == 0) lst_Product = productnumber + ":" + bsd_site + ":" + warehouse["bsd_warehouseid"].ToString().Trim() + ":0";
                                    else lst_Product += ";" + productnumber + ":" + bsd_site + ":" + warehouse["bsd_warehouseid"].ToString().Trim() + ":0";
                                    i++;
                                }

                                #region call Service Update Avalible WareHouse
                                string s_Result = "";
                                // throw new Exception("okie1" + lst_Product);
                                try
                                {
                                    s_Result = client.BHS_ValidateOnHand_RequestDelivery(contextService, lst_Product);
                                }
                                catch (Exception ex) { throw new Exception("Service:" + ex.Message + " lst_Product:" + lst_Product); }
                                //if (warehouse["bsd_warehouseid"].ToString().Trim() == "KD10") throw new Exception("okie:"+ s_Result);
                               // throw new Exception(s_Result);
                                string[] lstProduct_Result = new string[] { };
                                string[] lstitem = new string[] { };
                                lstProduct_Result = s_Result.Split(';');
                                // throw new Exception(s_Result);
                                foreach (string item in lstProduct_Result)
                                {
                                    lstitem = item.Split(':');
                                  
                                   decimal Quantity = Convert.ToDecimal(lstitem[4]);

                                    decimal QuantityRequestDeliveryProduct = 0m;
                                    //throw new Exception(QuantityRequestDeliveryProduct.ToString());
                                    //throw new Exception(QuantityRequestDeliveryProduct.ToString());
                                    EntityCollection wareHouse_Product = getWareHouseProduct(en_Site.Id, warehouse.Id, lstitem[0]);
                                    if (wareHouse_Product.Entities.Any())
                                    {
                                        //foreach (var en in wareHouse_Product.Entities)
                                        //{
                                        //    Entity wareHouse_Product_Update = new Entity(en.LogicalName, en.Id);
                                        //    wareHouse_Product_Update["bsd_date"] = DateTime.Now;
                                        //    wareHouse_Product_Update["bsd_quantity"] = Quantity;
                                        //    service.Update(wareHouse_Product_Update);
                                        //}
                                        foreach (var en in wareHouse_Product.Entities)
                                        {
                                            QuantityRequestDeliveryProduct = getQuantityRequestDeliveryProduct(lstitem[0], ((EntityReference)en["bsd_warehouses"]).Name);//productnumber
                                           // if (warehouse["bsd_warehouseid"].ToString().Trim() == "KD10") throw new Exception(QuantityRequestDeliveryProduct+"okie:" + s_Result);
                                            Entity wareHouse_Product_Update = new Entity(en.LogicalName, en.Id);
                                            wareHouse_Product_Update["bsd_date"] = DateTime.Now;
                                            //if (warehouse["bsd_warehouseid"].ToString().Trim() == "KD10") throw new Exception(QuantityRequestDeliveryProduct + "okie1:" + s_Result);
                                            if (Quantity < 0 && Quantity != 0)
                                            {

                                                wareHouse_Product_Update["bsd_quantity"] = (Quantity - QuantityRequestDeliveryProduct) * -1;
                                            }
                                            //else
                                            //{
                                            //    wareHouse_Product_Update["bsd_quantity"] = Quantity;
                                            //}
                                            //wareHouse_Product_Update["bsd_quantity"] = Quantity;
                                            if (Quantity == 0)
                                            {

                                                wareHouse_Product_Update["bsd_quantity"] = Quantity;
                                            }
                                            if (Quantity > 0)
                                            {


                                                wareHouse_Product_Update["bsd_quantity"] = Quantity - QuantityRequestDeliveryProduct;
                                                wareHouse_Product_Update["bsd_description"] = "AX Check WareHouse3";


                                            }
                                           
                                            service.Update(wareHouse_Product_Update);
                                        }

                                    }
                                    else
                                    {
                                        Entity wareHouse_Product_Create = new Entity("bsd_warehourseproduct", Guid.NewGuid());
                                        if (Quantity < 0 && Quantity != 0)
                                        {
                                            wareHouse_Product_Create["bsd_quantity"] = (Quantity - QuantityRequestDeliveryProduct) * -1;
                                        }
                                        if (Quantity == 0)
                                        {
                                            wareHouse_Product_Create["bsd_quantity"] = 0m;
                                        }
                                        if (Quantity > 0)
                                        {
                                            wareHouse_Product_Create["bsd_quantity"] = Quantity - QuantityRequestDeliveryProduct; 
                                        }
                                        //wareHouse_Product_Create["bsd_quantity"] = Quantity;
                                        wareHouse_Product_Create["bsd_name"] = lstitem[1].ToString();
                                        wareHouse_Product_Create["bsd_date"] = DateTime.Now;
                                        wareHouse_Product_Create["bsd_site"] = new EntityReference(en_Site.LogicalName, en_Site.Id);
                                        wareHouse_Product_Create["bsd_warehouses"] = new EntityReference(warehouse.LogicalName, warehouse.Id);
                                        wareHouse_Product_Create["bsd_productid"] = lstitem[0].ToString().Trim();
                                        Entity en_product = getProduct(lstitem[0].ToString().Trim());
                                        wareHouse_Product_Create["bsd_product"] = new EntityReference(en_product.LogicalName, en_product.Id);
                                        wareHouse_Product_Create["bsd_unit"] = (EntityReference)en_product["defaultuomid"];
                                        wareHouse_Product_Create["bsd_description"] = "AX Check WareHouse";
                                        if (Quantity>0)
                                        {
                                            wareHouse_Product_Create["bsd_description"] = "AX Check WareHouse1";
                                        }
                                        service.Create(wareHouse_Product_Create);
                                    }
                                }
                                #endregion
                            
                            }

                        }
                        #endregion
                    }
                    //string bsd_site = "";
                    //string productnumber = "";
                    //string lst_CheckProductAX = "";
                    //string lst_Product = "";
                    //Entity requestDelivery = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    //// throw new Exception("okie");
                    //if (requestDelivery.HasValue("bsd_site"))
                    //{
                    //    #region 
                    //    EntityReference rf_Entity = (EntityReference)requestDelivery["bsd_site"];

                    //    #endregion
                    //    EntityCollection lstWarehouse;
                    //    if (getConsignmentSitesConfigDefault(rf_Entity.Id))
                    //    {

                    //        lstWarehouse = getWareHouse();
                    //    }
                    //    else
                    //        lstWarehouse = getWareHouse(rf_Entity.Id);
                    //    #region 
                    //    EntityCollection lstRequestDeliveryProduct = getRequestDeliveryProduct(requestDelivery.Id);
                    //    //string s_Result1 = client.BHS_ValidateOnHand(contextService, "01D.01.013:102:KD01-102:0");
                    //    //throw new Exception(s_Result1);
                    //    if (lstWarehouse.Entities.Any() && lstRequestDeliveryProduct.Entities.Any())
                    //    {

                    //        foreach (Entity warehouse in lstWarehouse.Entities)
                    //        {
                    //            int i = 0;
                    //            rf_Entity = (EntityReference)warehouse["bsd_site"];
                    //            Entity en_Site = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("bsd_code"));
                    //            if (en_Site.HasValue("bsd_code")) bsd_site = en_Site["bsd_code"].ToString().Trim();
                    //            foreach (Entity RequestDeliveryProduct in lstRequestDeliveryProduct.Entities)
                    //            {
                    //                rf_Entity = (EntityReference)RequestDeliveryProduct["bsd_product"];
                    //                Entity en = service.Retrieve(rf_Entity.LogicalName, rf_Entity.Id, new ColumnSet("productnumber"));
                    //                if (en.HasValue("productnumber")) productnumber = en["productnumber"].ToString().Trim();
                    //                if (i == 0) lst_Product = productnumber + ":" + bsd_site + ":" + warehouse["bsd_warehouseid"].ToString().Trim() + ":0";
                    //                else lst_Product += ";" + productnumber + ":" + bsd_site + ":" + warehouse["bsd_warehouseid"].ToString().Trim() + ":0";
                    //                i++;
                    //            }

                    //            #region call Service Update Avalible WareHouse
                    //            string s_Result = "";
                    //            // throw new Exception("okie1" + lst_Product);
                    //            try
                    //            {
                    //                s_Result = client.BHS_ValidateOnHand_RequestDelivery(contextService, lst_Product);
                    //            }
                    //            catch (Exception ex) { throw new Exception("Service:" + ex.Message + " lst_Product:" + lst_Product); }
                    //            // if (warehouse["bsd_warehouseid"].ToString().Trim() == "KD01-102") throw new Exception("okie:"+ s_Result);
                    //            //throw new Exception(s_Result + "---" + lst_Product);
                    //            string[] lstProduct_Result = new string[] { };
                    //            string[] lstitem = new string[] { };
                    //            lstProduct_Result = s_Result.Split(';');
                    //            // throw new Exception(s_Result);
                    //            //throw new Exception(lstProduct_Result.Count().ToString() + "ta");
                    //            foreach (string item in lstProduct_Result)
                    //            {
                    //                lstitem = item.Split(':');

                    //                decimal Quantity = Convert.ToDecimal(lstitem[4]);

                    //                // decimal Quantity = 11000000;

                    //                EntityCollection wareHouse_Product = getWareHouseProduct(en_Site.Id, warehouse.Id, lstitem[0]);
                    //                throw new Exception(lstWarehouse.Entities.Count.ToString());
                    //                decimal QuantityRequestDeliveryProduct = getQuantityRequestDeliveryProduct(lstitem[0]);//productnumber
                    //                if (wareHouse_Product.Entities.Any())
                    //                {

                    //                    foreach (var en in wareHouse_Product.Entities)
                    //                    {
                    //                        Entity wareHouse_Product_Update = new Entity(en.LogicalName, en.Id);
                    //                        wareHouse_Product_Update["bsd_date"] = DateTime.Now;
                    //                        if (Quantity ==0)
                    //                        {
                    //                            wareHouse_Product_Update["bsd_quantity"] = QuantityRequestDeliveryProduct;
                    //                        }
                    //                        else
                    //                        {
                    //                            if (Quantity - QuantityRequestDeliveryProduct < 0)
                    //                            {
                    //                                wareHouse_Product_Update["bsd_quantity"] = (Quantity - QuantityRequestDeliveryProduct) * -1;
                    //                            }
                    //                            else
                    //                            {
                    //                                wareHouse_Product_Update["bsd_quantity"] = Quantity - QuantityRequestDeliveryProduct;
                    //                            }

                    //                        }


                    //                        service.Update(wareHouse_Product_Update);
                    //                    }

                    //                }
                    //                else
                    //                {

                    //                    Entity wareHouse_Product_Create = new Entity("bsd_warehourseproduct", Guid.NewGuid());
                    //                    if (Quantity == 0)
                    //                    {

                    //                        wareHouse_Product_Create["bsd_quantity"] = QuantityRequestDeliveryProduct;
                    //                    }
                    //                    else
                    //                    {
                    //                        if (Quantity - QuantityRequestDeliveryProduct < 0)
                    //                        {
                    //                            wareHouse_Product_Create["bsd_quantity"] = (Quantity - QuantityRequestDeliveryProduct)*-1;
                    //                        }
                    //                        else
                    //                        {
                    //                            wareHouse_Product_Create["bsd_quantity"] = Quantity - QuantityRequestDeliveryProduct;
                    //                        }

                    //                    }

                    //                    wareHouse_Product_Create["bsd_name"] = lstitem[1].ToString();
                    //                    wareHouse_Product_Create["bsd_date"] = DateTime.Now;
                    //                    wareHouse_Product_Create["bsd_site"] = new EntityReference(en_Site.LogicalName, en_Site.Id);
                    //                    wareHouse_Product_Create["bsd_warehouses"] = new EntityReference(warehouse.LogicalName, warehouse.Id);
                    //                    wareHouse_Product_Create["bsd_productid"] = lstitem[0].ToString().Trim();
                    //                    Entity en_product = getProduct(lstitem[0].ToString().Trim());
                    //                    wareHouse_Product_Create["bsd_product"] = new EntityReference(en_product.LogicalName, en_product.Id);
                    //                    wareHouse_Product_Create["bsd_unit"] = (EntityReference)en_product["defaultuomid"];
                    //                    wareHouse_Product_Create["bsd_description"] = "AX Check WareHouse";
                    //                    service.Create(wareHouse_Product_Create);
                    //                }
                    //            }
                    //            #endregion
                    //        }

                    //    }
                    //    #endregion
                    //}
                    context.OutputParameters["ReturnId"] = "true";
                }
                catch (Exception ex)
                {
                    throw new Exception("Can not check available Warehouse because: " + ex.Message);
                }
            }
        }
        public EntityCollection getRequestDeliveryProduct(Guid RequestDeliveryId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_requestdeliveryproduct'>
                               <all-attributes />
                                <filter type='and'>
                                  <condition attribute='bsd_requestdelivery' operator='eq'  uitype='bsd_requestdelivery' value='" + RequestDeliveryId + @"' />
                                    <condition attribute='statecode' operator='eq' value='0' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return lst;
        }
        public Entity getProduct(string productnumber)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='product'>
                                    <attribute name='name' />
                                    <attribute name='productnumber' />
                                    <attribute name='description' />
                                    <attribute name='statecode' />
                                    <attribute name='productstructure' />
                                    <attribute name='defaultuomid' />
                                    <order attribute='productnumber' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='productnumber' operator='eq' value='" + productnumber.Trim() + @"' />
                                    </filter>
                                  </entity>
                                </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return lst.Entities.First();
        }
        public EntityCollection getWareHouse(Guid SiteId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_warehouseentity'>
                        <attribute name='bsd_warehouseentityid' />
                        <attribute name='bsd_name' />
                        <attribute name='bsd_site' />
                        <attribute name='bsd_warehouseid' />
                        <attribute name='createdon' />
                        <order attribute='bsd_name' descending='false' />
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
                          <condition attribute='bsd_site' operator='eq' uitype='bsd_site' value='" + SiteId + @"' />
                        </filter>
                      </entity>
                    </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return lst;
        }
        public EntityCollection getWareHouse()
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_warehouseentity'>
                        <attribute name='bsd_warehouseentityid' />
                        <attribute name='bsd_name' />
                        <attribute name='bsd_warehouseid' />
                        <attribute name='bsd_site' />
                        <attribute name='createdon' />
                        <order attribute='bsd_name' descending='false' />
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                      </entity>
                    </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return lst;
        }
        public Guid getSubOrderSite()
        {
            Guid id = Guid.Empty;
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='bsd_warehouseentity'>
                        <attribute name='bsd_warehouseentityid' />
                        <attribute name='bsd_name' />
                        <attribute name='bsd_warehouseid' />
                        <attribute name='bsd_site' />
                        <attribute name='createdon' />
                        <order attribute='bsd_name' descending='false' />
                        <filter type='and'>
                          <condition attribute='statecode' operator='eq' value='0' />
                        </filter>
                      </entity>
                    </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return id;
        }
        public bool getConsignmentSitesConfigDefault(Guid SiteId)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                  <entity name='bsd_configdefault'>
                                    <attribute name='bsd_configdefaultid' />
                                    <attribute name='bsd_name' />
                                    <attribute name='createdon' />
                                    <order attribute='bsd_name' descending='false' />
                                    <filter type='and'>
                                      <condition attribute='bsd_consignmentsites' operator='eq' uitype='bsd_site' value='" + SiteId + @"' />
                                    </filter>
                                  </entity>
                                </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            if (lst.Entities.Any())
            {
                return true;
            }
            return false;
        }
        public EntityCollection getWareHouseProduct(Guid SiteId, Guid WareHouseId, string productnumber)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_warehourseproduct'>
                                <attribute name='bsd_warehourseproductid' />
                                <attribute name='bsd_warehouses' />
                                <attribute name='bsd_name' />
                                <attribute name='createdon' />
                                <order attribute='bsd_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + SiteId + @"' />
                                  <condition attribute='bsd_warehouses' operator='eq'  uitype='bsd_warehouseentity' value='" + WareHouseId + @"' />
                                  <condition attribute='bsd_productid' operator='eq' value='" + productnumber.Trim() + @"' />
                                </filter>
                              </entity>
                            </fetch>";
            //string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
            //                  <entity name='bsd_warehourseproduct'>
            //                    <attribute name='bsd_warehourseproductid' />
            //                    <attribute name='bsd_name' />
            //                    <attribute name='createdon' />
            //                    <order attribute='bsd_name' descending='false' />
            //                      <filter type='and'>           
            //                      <condition attribute='bsd_productid' operator='eq' value='" + productnumber.Trim() + @"' />
            //                    </filter>
            //                    <link-entity name='bsd_site' from='bsd_siteid' to='bsd_site' alias='aa'>
            //                      <filter type='and'>
            //                        <condition attribute='bsd_code' operator='eq' value='" + SiteId + @"' />
            //                      </filter>
            //                    </link-entity>
            //                    <link-entity name='bsd_warehouseentity' from='bsd_warehouseentityid' to='bsd_warehouses' alias='ab'>
            //                      <filter type='and'>
            //                        <condition attribute='bsd_warehouseid' operator='eq' value='" + WareHouseId + @"' />
            //                      </filter>
            //                    </link-entity>
            //                  </entity>
            //                    </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return lst;
        }
        public bool getConfigdefaultconnectax()
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_configdefault'>
                    <attribute name='bsd_configdefaultid' />
                    <attribute name='createdon' />
                    <attribute name='bsd_checkinventory' />
                     <attribute name='bsd_usernameax' />
                     <attribute name='bsd_passwordax' /> 
                    <attribute name='bsd_company' />
                     <attribute name='bsd_portax' />
                     <attribute name='bsd_domain' />
                    <order attribute='createdon' descending='true' />
                  </entity>
                </fetch>";
            Entity configdefault = service.RetrieveMultiple(new FetchExpression(xml)).Entities.FirstOrDefault();
            if (configdefault.HasValue("bsd_usernameax") && configdefault.HasValue("bsd_passwordax") && configdefault.HasValue("bsd_company")
                && configdefault.HasValue("bsd_portax") && configdefault.HasValue("bsd_domain"))
            {
                _userName = configdefault["bsd_usernameax"].ToString();
                _passWord = configdefault["bsd_passwordax"].ToString();
                _company = configdefault["bsd_company"].ToString();
                _port = configdefault["bsd_portax"].ToString();
                _domain = configdefault["bsd_domain"].ToString();
                return true;
            }
            else
            {
                return false;
            }

        }
        public decimal getQuantityRequestDeliveryProduct(string productnumber,string warhouse)
        {
            decimal result = 0m;
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_requestdeliveryproduct'>
                            <attribute name='bsd_requestdeliveryproductid' />
                            <attribute name='bsd_name' />
                            <attribute name='createdon' />
                            <attribute name='bsd_quantity' />
                            <order attribute='bsd_name' descending='false' />
                            <filter type='and'>
                              <condition attribute='bsd_warehousename' operator='like' value='%"+warhouse.Trim()+@"%' />
                               <condition attribute='bsd_productid' operator='eq' value='" + productnumber.Trim() + @"' />
                                <condition attribute='statecode' operator='eq' value='0' />
                            </filter>
                            <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='ac'>
                              <filter type='and'>
                                <condition attribute='bsd_pickinglistax' operator='null' />
                                <condition attribute='statecode' operator='eq' value='0' />
                              </filter>
                            </link-entity>
                          </entity>
                        </fetch>";
           
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            //throw new Exception(warhouse.ToString());
            if (lst.Entities.Any())
            {
              
                foreach (var item in lst.Entities)
                {
                    decimal quantity = item.Contains("bsd_quantity") && item["bsd_quantity"] != null ? (decimal)item["bsd_quantity"] : 0m;
                    result += quantity;
                }
            }
            return result;
        }
    }
}
