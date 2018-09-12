using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_CloseSubOrder
{
    public class Main : IPlugin
    {
        private IOrganizationServiceFactory factory;
        public IOrganizationService service { get; set; }
        public IPluginExecutionContext context { get; set; }
        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = factory.CreateOrganizationService(context.UserId);
            try
            {
                //vinhlh 26-12-2017
                bool connect_ax = getConfigdefaultconnectax();
                if (connect_ax == false)
                {
                    throw new Exception("Thiếu khai báo connect ax");
                }
                #region KHai báo service AX
                NetTcpBinding binding = new NetTcpBinding();
                //net.tcp://AOS_SERVICE_HOST/DynamicsAx/Services/AXConectorAIF
                binding.Name = "NetTcpBinding_BHS_BSD_CRMSERVICEAXService";
                EndpointAddress endpoint = new EndpointAddress(new Uri("net.tcp://" + Utilites._port + "/DynamicsAx/Services/BHS_BSD_CRMSERVICEAXServiceGroup"));
                ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient client = new ServiceReferenceAIF.BHS_BSD_CRMSERVICEAXServiceClient(binding, endpoint);
                client.ClientCredentials.Windows.ClientCredential.Domain = Utilites._domain;
                client.ClientCredentials.Windows.ClientCredential.UserName = Utilites._userName;
                client.ClientCredentials.Windows.ClientCredential.Password = Utilites._passWord;
                ServiceReferenceAIF.CallContext contextService = new ServiceReferenceAIF.CallContext() { Company = Utilites._company };
                #endregion
                #region vinhlh 21-11-2017 
                if (context.MessageName == "bsd_Action_CloseSubOrder")
                {

                    #region bsd_Action_CloseSubOrder
                    EntityReference rf;
                    EntityReference target = (EntityReference)context.InputParameters["Target"];
                    Entity suborder = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    if (suborder.HasValue("bsd_type"))
                    {
                        if (((OptionSetValue)suborder["bsd_type"]).Value == 861450001) //861450001 Quote
                        {
                            #region Cập nhật lại số lượng còn lại Quote bsd_quote
                            if (suborder.HasValue("bsd_quote"))
                            {
                                rf = (EntityReference)suborder["bsd_quote"];
                                EntityCollection lst_suborderProduct = getsubOrderProduct(suborder.Id);

                                if (lst_suborderProduct.Entities.Any())
                                {
                                    this.SetState(rf.Id, rf.LogicalName, 0, 1);
                                    foreach (var suborderProduct in lst_suborderProduct.Entities)
                                    {
                                        EntityReference rf_product = (EntityReference)suborderProduct["bsd_product"];
                                        Entity qouteProduct = getQuoteProduct(rf.Id, rf_product.Id);
                                        Entity qouteProduct_Update = new Entity(qouteProduct.LogicalName, qouteProduct.Id);
                                        decimal bsd_remainingquantity = 0m; decimal bsd_shipquantity = 0m; decimal bsd_shippedquantity = 0m; decimal bsd_suborderquantity = 0m;
                                        if (qouteProduct.HasValue("bsd_remainingquantity")) bsd_remainingquantity = (decimal)qouteProduct["bsd_remainingquantity"];
                                        if (qouteProduct.HasValue("bsd_suborderquantity")) bsd_suborderquantity = (decimal)qouteProduct["bsd_suborderquantity"];
                                        if (suborderProduct.HasValue("bsd_shipquantity")) bsd_shipquantity = (decimal)suborderProduct["bsd_shipquantity"];
                                        if (suborderProduct.HasValue("bsd_shippedquantity")) bsd_shippedquantity = (decimal)suborderProduct["bsd_shippedquantity"];
                                        // throw new Exception((bsd_remainingquantity + (bsd_shipquantity - bsd_shippedquantity)).ToString());
                                        qouteProduct_Update["bsd_remainingquantity"] = bsd_remainingquantity + (bsd_shipquantity - bsd_shippedquantity);
                                        qouteProduct_Update["bsd_suborderquantity"] = bsd_suborderquantity - (bsd_shipquantity - bsd_shippedquantity);
                                        // qouteProduct_Update["bsd_remainingquantity"] = 0m;
                                        service.Update(qouteProduct_Update);

                                    }
                                    #region Won Quote
                                    this.SetState(rf.Id, rf.LogicalName, 1, 2);
                                    WinQuoteRequest winQuoteRequest = new WinQuoteRequest();
                                    Entity quoteClose = new Entity("quoteclose");
                                    quoteClose.Attributes["quoteid"] = new EntityReference("quote", rf.Id);
                                    quoteClose.Attributes["subject"] = "Quote Close" + DateTime.Now.ToString();
                                    winQuoteRequest.QuoteClose = quoteClose;
                                    winQuoteRequest.Status = new OptionSetValue(-1);
                                    service.Execute(winQuoteRequest);
                                    #endregion
                                }
                            }
                            #endregion
                        }
                        else if (((OptionSetValue)suborder["bsd_type"]).Value == 861450002)//861450002 Order
                        {
                            #region Cập nhật lại số lượng còn lại Sale Contract
                            if (suborder.HasValue("bsd_order"))
                            {
                                rf = (EntityReference)suborder["bsd_order"];
                                EntityCollection lst_suborderProduct = getsubOrderProduct(suborder.Id);
                                if (lst_suborderProduct.Entities.Any())
                                {
                                    foreach (var suborderProduct in lst_suborderProduct.Entities)
                                    {

                                        EntityReference rf_product = (EntityReference)suborderProduct["bsd_product"];

                                        Entity orderProduct = getOrderProduct(rf.Id, rf_product.Id);
                                        Entity orderProduct_Update = new Entity(orderProduct.LogicalName, orderProduct.Id);
                                        decimal bsd_remainingquantity = 0m; decimal bsd_shipquantity = 0m; decimal bsd_shippedquantity = 0m; decimal bsd_suborderquantity = 0m;
                                        if (orderProduct.HasValue("bsd_remainingquantity")) bsd_remainingquantity = (decimal)orderProduct["bsd_remainingquantity"];
                                        if (orderProduct.HasValue("bsd_suborderquantity")) bsd_suborderquantity = (decimal)orderProduct["bsd_suborderquantity"];
                                        if (suborderProduct.HasValue("bsd_shipquantity")) bsd_shipquantity = (decimal)suborderProduct["bsd_shipquantity"];
                                        if (suborderProduct.HasValue("bsd_shippedquantity")) bsd_shippedquantity = (decimal)suborderProduct["bsd_shippedquantity"];
                                        orderProduct_Update["bsd_remainingquantity"] = bsd_remainingquantity + (bsd_shipquantity - bsd_shippedquantity);
                                        orderProduct_Update["bsd_suborderquantity"] = bsd_suborderquantity - (bsd_shipquantity - bsd_shippedquantity);
                                        // orderProduct_Update["bsd_remainingquantity"] = 0m;
                                        service.Update(orderProduct_Update);
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    #region Cập nhật trạng thái suborder thành  Closed
                    Entity suborder_Update = new Entity(suborder.LogicalName, suborder.Id);
                    suborder_Update["statuscode"] = new OptionSetValue(861450008);
                    service.Update(suborder_Update);
                    //throw new Exception("okie"); 
                    #endregion
                    #region 27-12-2017 call service AX
                    if (suborder.Contains("bsd_suborderax") && suborder["bsd_suborderax"] != null)
                    {
                        string s_Result = client.BHS_CancelSalesOrder(contextService, suborder["bsd_suborderax"].ToString().Trim());
                        if (s_Result != "Success") throw new Exception("AX " + s_Result);
                    }
                    #endregion
                    #endregion
                }
                else if (context.MessageName == "bsd_Action_CloseDeliverySchedule")
                {
                    #region bsd_Action_CloseDeliverySchedule
                    EntityReference target = (EntityReference)context.InputParameters["Target"];
                    Entity deliverySchedule = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    EntityCollection lst_requestDelivery = getRequestDelivery(deliverySchedule.Id);
                    if (lst_requestDelivery.Entities.Any())
                    {
                        foreach (var requestDelivery in lst_requestDelivery.Entities)
                        {
                            EntityCollection lst_deliveryNote = getDeliveryNote(requestDelivery.Id);
                            if (lst_deliveryNote.Entities.Any())
                            {
                                foreach (var deliveryNote in lst_deliveryNote.Entities)
                                {
                                    #region Cập nhật lại Delivery Note trạng thái thành fullfil
                                    Entity deliveryNote_Update = new Entity(deliveryNote.LogicalName, deliveryNote.Id);
                                    deliveryNote_Update["bsd_status"] = new OptionSetValue(861450002);
                                    service.Update(deliveryNote_Update);
                                    #endregion
                                }
                            }
                        }
                    }
                    #region Cập nhật lại delivery Schedule trạng thái thành fullfil
                    Entity deliverySchedule_Update = new Entity(deliverySchedule.LogicalName, deliverySchedule.Id);
                    deliverySchedule_Update["bsd_status"] = new OptionSetValue(861450003);
                    service.Update(deliverySchedule_Update);
                    #endregion
                    #endregion
                }
                #endregion
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
        public EntityCollection getRequestDelivery(Guid deliverySchedule)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_requestdelivery'>
                                <attribute name='bsd_requestdeliveryid' />
                                <attribute name='bsd_name' />
                                <attribute name='createdon' />
                                <order attribute='bsd_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='bsd_deliveryplan' operator='eq'  uitype='bsd_deliveryplan' value='" + deliverySchedule + @"' />
                                  <condition attribute='bsd_createddeliverynote' operator='eq' value='1' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return lst;
        }
        public EntityCollection getDeliveryNote(Guid requestDelivery)
        {
            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_deliverynote'>
                                <attribute name='bsd_deliverynoteid' />
                                <attribute name='bsd_name' />
                                <attribute name='createdon' />
                                <order attribute='bsd_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='bsd_status' operator='ne' value='861450002' />
                                  <condition attribute='bsd_requestdelivery' operator='eq'  uitype='bsd_requestdelivery' value='" + requestDelivery + @"' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
            return lst;
        }
        public EntityCollection getsubOrderProduct(Guid suborder)
        {
            string xml = "";
            try
            {
                xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='bsd_suborderproduct'>
                             <all-attributes />
                            <order attribute='bsd_name' descending='false' />
                            <filter type='and'>
                              <condition attribute='bsd_suborder' operator='eq'  uitype='bsd_suborder' value='" + suborder + @"' />
                              <condition attribute='bsd_shippedquantity' operator='gt' value='0' />
                              <condition attribute='bsd_freeitem' operator='eq' value='0' />
                            </filter>
                          </entity>
                        </fetch>";
                EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
                return lst;
            }
            catch (Exception ex)
            {
                throw new Exception("getsubOrderProduct() " + ex.Message);
            }
        }
        public Entity getOrderProduct(Guid Order, Guid Product)
        {
            string xml = "";
            try
            {
                xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='salesorderdetail'>
                            <all-attributes />
                            <order attribute='productid' descending='false' />
                            <filter type='and'>
                              <condition attribute='salesorderid' operator='eq'  uitype='salesorder' value='" + Order + @"' />
                              <condition attribute='productid' operator='eq' uitype='product' value='" + Product + @"' />
                            </filter>
                          </entity>
                        </fetch>";
                EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
                return lst.Entities.First();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "xml:" + xml);
            }
        }
        public Entity getQuoteProduct(Guid Quote, Guid Product)
        {
            string xml = "";
            try
            {
                xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                      <entity name='quotedetail'>
                       <all-attributes />
                        <order attribute='productid' descending='false' />
                        <filter type='and'>
                          <condition attribute='quoteid' operator='eq' uitype='quote' value='" + Quote + @"' />
                          <condition attribute='productid' operator='eq'  uitype='product' value='" + Product + @"' />
                        </filter>
                      </entity>
                    </fetch>";
                EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));
                return lst.Entities.First();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + "xml:" + xml);
            }
        }
        public void SetState(Guid Id, string LogicalName, int state, int status)
        {
            service.Execute(new SetStateRequest()
            {
                EntityMoniker = new EntityReference
                {
                    Id = Id,
                    LogicalName = LogicalName
                },
                State = new OptionSetValue(state),
                Status = new OptionSetValue(status)
            });
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
            Entity configdefault = (service.RetrieveMultiple(new FetchExpression(xml))).Entities.FirstOrDefault();
            if (configdefault.HasValue("bsd_usernameax") && configdefault.HasValue("bsd_passwordax") && configdefault.HasValue("bsd_company")
                && configdefault.HasValue("bsd_portax") && configdefault.HasValue("bsd_domain"))
            {
                Utilites._userName = configdefault["bsd_usernameax"].ToString();
                Utilites._passWord = configdefault["bsd_passwordax"].ToString();
                Utilites._company = configdefault["bsd_company"].ToString();
                Utilites._port = configdefault["bsd_portax"].ToString();
                Utilites._domain = configdefault["bsd_domain"].ToString();
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
