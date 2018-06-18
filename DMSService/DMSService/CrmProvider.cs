using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Xml;
using static DMSService.ExtensionList;
using System.Web.Script.Serialization;
using System.Data;
using System.Reflection;
using System.ServiceModel.Web;
using System.IO;
using System.Text;
using static DMSService.EntityClass;
using Microsoft.Crm.Sdk.Messages;

namespace DMSService
{
    internal static class CrmProvider
    {
        internal static string RetrieveEntity(string entityName, string id, string[] columns)
        {
            Message mss = new Message();
            mss.Status = "Success";
            try
            {
                CRMConnector crm = new CRMConnector();
                crm.ConnectToCrm();
                Entity en = crm.Service.Retrieve(entityName, Guid.Parse(id), new ColumnSet(columns));
                if (en != null)
                {
                    object en_tmp = new object();
                    Dictionary<string, object> attrs = new Dictionary<string, object>();
                    foreach (string key in en.Attributes.Keys)
                    {
                        object value = en.Attributes[key];
                        string dataType = value.GetType().Name;
                        string formattedValue = null;
                        en.FormattedValues.TryGetValue(key, out formattedValue);
                        if (string.IsNullOrEmpty(formattedValue))
                            formattedValue = value.ToString();
                        attrs.Add(key, new { value = value, formattedValue = formattedValue, dataType = dataType });
                    }
                    /*mss.Data*/
                    en_tmp = new { logicalName = en.LogicalName, id = en.Id, attributes = attrs };
                    mss.Data = JsonConvert.SerializeObject(en_tmp);
                }
            }
            catch (Exception ex)
            {
                mss.Status = "Error";
                mss.Content = ex.Message;
            }
            return JsonConvert.SerializeObject(mss);
        }

        internal static string RetrieveEntityByList(string entityName, string id, RootObject objectvalue)
        {
            string result = "";
            try
            {
                CRMConnector crm = new CRMConnector();
                crm.ConnectToCrm();
                Entity en = crm.Service.Retrieve(entityName, Guid.Parse(id), new ColumnSet(true));
                if (en != null)
                {
                    foreach (var item in objectvalue.master.fields)
                    {
                        if (en.Contains(item.fieldname))
                            result += en[item.fieldname].ToString() + ",";
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
            return result;
        }

        internal static string RetrieveEntity2(string entityName, string id, List<string> columns)
        {
            string result = "";
            try
            {
                CRMConnector crm = new CRMConnector();
                crm.ConnectToCrm();
                Entity en = crm.Service.Retrieve(entityName, Guid.Parse(id), new ColumnSet(columns.ToArray()));
                if (en != null)
                {
                    object en_tmp = new object();
                    object value = null;
                    string type;
                    foreach (string key in en.Attributes.Keys)
                    {
                        value = en.Attributes[key];
                        type = value.GetType().Name;
                        if (type.Equals("Money"))
                            value = ((Money)en.Attributes[key]).Value;
                        else if (type.Equals("EntityReference"))
                            continue;
                        else if (type.Equals("Guid"))
                            continue;
                        result += "," + value;

                        /*if (key.Equals("createdon"))
                        {
                            string dataType = value.GetType().Name;
                            string formattedValue = null;
                            en.FormattedValues.TryGetValue(key, out formattedValue);
                            if (string.IsNullOrEmpty(formattedValue))
                                formattedValue = value.ToString();
                            result += "," + value + "," + formattedValue;
                        }*/
                    }
                }
            }
            catch (Exception ex)
            { return ex.Message; }
            return result;
        }

        internal static string RetrieveMultiple(string fetch)
        {
            Message mss = new Message() { Status = "Success" };
            try
            {
                CRMConnector crm = new CRMConnector();
                crm.ConnectToCrm();
                EntityCollection etc = crm.Service.RetrieveMultiple(new FetchExpression(fetch));
                List<object> entities = new List<object>();
                foreach (Entity en in etc.Entities)
                {
                    Dictionary<string, object> attrs = new Dictionary<string, object>();
                    foreach (string key in en.Attributes.Keys)
                    {
                        object value = en.Attributes[key];
                        string dataType = value.GetType().Name;
                        string formattedValue = null;
                        en.FormattedValues.TryGetValue(key, out formattedValue);
                        if (string.IsNullOrEmpty(formattedValue))
                            formattedValue = value.ToString();
                        attrs.Add(key, new { value = value, formattedValue = formattedValue, dataType = dataType });
                    }
                    entities.Add(new { logicalName = en.LogicalName, id = en.Id, attributes = attrs });
                }
                mss.Data = JsonConvert.SerializeObject(entities);
            }
            catch (Exception ex)
            {
                mss.Status = "Error";
                mss.Content = ex.Message;
            }
            return JsonConvert.SerializeObject(mss);
        }

        internal static string RetrieveMultiPage(string fetch, int pageIndex, int pageSize)
        {
            Message mss = new Message() { Status = "Success" };
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(fetch);
                XmlNode xFetch = doc.FirstChild;
                XmlAttribute xCount = doc.CreateAttribute("count");
                xCount.Value = pageSize + "";
                xFetch.Attributes.Append(xCount);

                XmlAttribute xPage = doc.CreateAttribute("page");
                xPage.Value = pageIndex + "";
                xFetch.Attributes.Append(xPage);

                CRMConnector crm = new CRMConnector();
                crm.ConnectToCrm();
                EntityCollection etc = crm.Service.RetrieveMultiple(new FetchExpression(doc.InnerXml));
                List<object> entities = new List<object>();
                foreach (Entity en in etc.Entities)
                {
                    Dictionary<string, object> attrs = new Dictionary<string, object>();
                    foreach (string key in en.Attributes.Keys)
                    {
                        object value = en.Attributes[key];
                        string dataType = value.GetType().Name;
                        string formattedValue = null;
                        en.FormattedValues.TryGetValue(key, out formattedValue);
                        if (string.IsNullOrEmpty(formattedValue))
                            formattedValue = value.ToString();
                        attrs.Add(key, new { value = value, formattedValue = formattedValue, dataType = dataType });
                    }
                    entities.Add(new { logicalName = en.LogicalName, id = en.Id, attributes = attrs });
                }
                var page = new { more = etc.MoreRecords, pageIndex = pageIndex, pageSize = pageSize };
                mss.Data = JsonConvert.SerializeObject(new { entities = entities, page = page });
            }
            catch (Exception ex)
            {
                mss.Status = "Error";
                mss.Content = ex.Message;
            }
            return JsonConvert.SerializeObject(mss);
        }

        internal static DataTable ListSaveForm()
        {
            return null;
        }

        internal static Message SaveForm(Message data)
        {
            Message mss = new Message();
            try
            {
                mss.Status = "Success";
                if (data.Data != null && data.Data.Length > 0)
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc = JsonConvert.DeserializeXmlNode(data.Data, "entity");
                    XmlNode xid = xdoc.SelectSingleNode("entity/enId");
                    XmlNode xEn = xdoc.SelectSingleNode("entity/enName");
                    if (xEn == null && xEn.InnerXml.Trim().Length > 0)
                        throw new Exception("Entity name can't not be null!");
                    Entity en = new Entity(xEn.InnerText);
                    xdoc.FirstChild.RemoveChild(xEn);
                    bool isUpdate = false;
                    if (xid != null)
                    {
                        if (xid.InnerXml.Trim().Length > 0)
                        {
                            isUpdate = true;
                            en.Id = Guid.Parse(xid.InnerText);
                        }
                        xdoc.FirstChild.RemoveChild(xid);
                    }

                    XmlNode xEntity = xdoc.FirstChild;
                    List<string> listParams = new List<string>();
                    listParams.Add("createdon");
                    foreach (XmlNode node in xEntity.ChildNodes)
                    {
                        string type = node.SelectSingleNode("Type").InnerText;
                        XmlNode value = node.SelectSingleNode("Value");
                        if (type == "String")
                        {
                            if (node.Name.Equals("s2s_code")) listParams.Add(value.InnerText);
                            else en[node.Name] = value != null ? value.InnerText.Trim() : null;
                        }
                        else if (type == "Lookup")
                        {
                            if (value != null && value.ChildNodes.Count > 0)
                            {
                                string nameRef = value.SelectSingleNode("logicalName").InnerText.Trim();
                                string idRef = value.SelectSingleNode("id").InnerText.Trim();
                                if (!string.IsNullOrEmpty(idRef))
                                    en[node.Name] = new EntityReference(nameRef, Guid.Parse(idRef));
                                else en[node.Name] = null;
                            }
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Decimal")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = decimal.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Double")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = double.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Integer")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = int.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Money")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                if (node.Name.Equals("s2s_amount"))
                                    listParams.Add(value.InnerText);
                                else if (node.Name.Equals("s2s_priceafterpromotion"))
                                    listParams.Add(value.InnerText);
                                else if (node.Name.Equals("s2s_amountafterpromotion"))
                                    listParams.Add(value.InnerText);
                                else if (node.Name.Equals("s2s_priceinclustax"))
                                    listParams.Add(value.InnerText);
                                else en[node.Name] = new Money(decimal.Parse(value.InnerText));
                            else en[node.Name] = null;
                        }
                        else if (type == "Picklist")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = new OptionSetValue(int.Parse(value.InnerText));
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Boolean")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = bool.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "DateTime")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = DateTime.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Byte[]")
                        {
                            byte[] databye = System.Convert.FromBase64String(value.InnerText);
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = databye;
                            else
                                en[node.Name] = null;
                        }
                    }
                    CRMConnector crm = new CRMConnector();
                    crm.ConnectToCrm();
                    if (isUpdate)
                    {
                        crm.Service.Update(en);
                        mss.Status = "Success";
                        mss.Data = en.Id.ToString();
                    }
                    else
                    {
                        Guid guid = crm.Service.Create(en);
                        mss.Status = "Success";
                        mss.Data = guid.ToString() + RetrieveEntity2(xEn.InnerText, guid.ToString(), listParams);
                    }
                }
            }
            catch (Exception ex)
            {
                mss.Status = "Error";
                mss.Content = ex.Message;
            }
            return mss;
        }

        internal static string ConnectorSaveEntity(string json, string org)
        {
            try
            {
                //org = "B2B";
                EntityClass.RootJson result = JsonConvert.DeserializeObject<EntityClass.RootJson>(json);
                string logicalname = result.entity.entity;
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                Guid guidstring = new Guid();
                //case update
                if (result.typeAction == "Update")
                {
                    var xid = result.entity.fields.Where(obj => obj.type == "Guid").FirstOrDefault().value;
                    var updateentity = new Entity(logicalname, Guid.Parse(xid));
                    foreach (var fielden in result.entity.fields)
                    {
                        SetConnectorFields(updateentity, fielden);
                    }
                    crm.Service.Update(updateentity);
                    guidstring = Guid.Parse(xid);
                }
                //case insert
                else
                {
                    var entity = new Entity(logicalname);

                    foreach (var fielden in result.entity.fields)
                    {
                        SetConnectorFields(entity, fielden);
                    }
                    guidstring = crm.Service.Create(entity);

                }
                return guidstring.ToString();
            }
            catch (Exception ex)
            {
                return "error : " + ex.Message;
                throw;
            }
        }
        internal static bool isvalidfileld(string LogicalName, string fieldName, string Value, string org)
        {
            ////org = "B2B";
            CRMConnector crm = new CRMConnector();
            crm.speceficConnectToCrm(org);
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
            var entityCollection = crm.Service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Any())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        // Vinhlh 30-08-2017
        internal static string insertCurrency(Currency objcurrency, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objcurrency != null)
                {
                    if (string.IsNullOrEmpty(objcurrency.currencyname)) return "Currency name is not null";
                    if (string.IsNullOrEmpty(objcurrency.isocurrencycode)) return "Iso currencycode is not null";
                    //  if (string.IsNullOrEmpty(objcurrency.currencyprecision.ToString())) return "Currency precision is not null";
                    // if (string.IsNullOrEmpty(objcurrency.currencysymbol)) return "Currency symbol is not null";
                    // if (string.IsNullOrEmpty(objcurrency.exchangerate.ToString())) return "Exchange rate is not null";
                    string id = retriveLookup("transactioncurrency", "isocurrencycode", objcurrency.isocurrencycode, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("transactioncurrency", Guid.Parse(id));
                        entity["exchangerate"] = decimal.Parse("2");
                        entity["currencyname"] = objcurrency.currencyname;
                        entity["currencyprecision"] = objcurrency.currencyprecision;
                        entity["currencysymbol"] = objcurrency.currencysymbol;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        Entity entity = new Entity("transactioncurrency");
                        entity["exchangerate"] = decimal.Parse("2");
                        entity["isocurrencycode"] = objcurrency.isocurrencycode;
                        entity["currencyname"] = objcurrency.currencyname;
                        entity["currencyprecision"] = objcurrency.currencyprecision;
                        entity["currencysymbol"] = objcurrency.currencysymbol;
                        crm.Service.Create(entity);
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }

        internal static bool DeleteCurrency(string isocurrencycode, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (isocurrencycode != null)
                {

                    string id = retriveLookup("transactioncurrency", "isocurrencycode", isocurrencycode, org);
                    if (id != null)
                    {
                        crm.Service.Delete("transactioncurrency", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string InsertExchangeRate(ExchangeRate objexchangerate, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objexchangerate != null)
                {
                    if (string.IsNullOrEmpty(objexchangerate.Recid)) return "Recid is not null";
                    if (string.IsNullOrEmpty(objexchangerate.bsd_name)) return "Exchange rate name is not null";
                    //if (string.IsNullOrEmpty(objexchangerate.bsd_currencyfrom)) return "Iso currencycode from is not null";
                    //if (string.IsNullOrEmpty(objexchangerate.bsd_currencyto)) return "Iso currencycode to is not null";
                    //if (string.IsNullOrEmpty(objexchangerate.bsd_exchangerate.ToString())) return "Exchange rate is not null";
                    string id = retriveLookup("bsd_exchangerate", "bsd_codeax", objexchangerate.Recid, org);
                    if (id != null)
                    {

                        Entity entity = new Entity("bsd_exchangerate", Guid.Parse(id));
                        // entity["bsd_name"] = objexchangerate.bsd_name;
                        //
                        var xmlconfigdefault = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_configdefault'>
                                                    <attribute name='bsd_configdefaultid' />
                                                    <attribute name='createdon' />
                                                    <attribute name='bsd_bankdefault' />
                                                    <order attribute='createdon' descending='false' />
                                                  </entity>
                                                </fetch>";

                        var configdefault = crm.Service.RetrieveMultiple(new FetchExpression(xmlconfigdefault));
                        if (configdefault.Entities.Any())
                        {
                            // var bankdefault = (EntityReference)configdefault[0]["bsd_bankdefault"];
                            entity["bsd_bankaccount"] = (EntityReference)configdefault[0]["bsd_bankdefault"];
                        }
                        //
                        entity["bsd_date"] = objexchangerate.bsd_date;
                        entity["bsd_exchangerate"] = objexchangerate.bsd_exchangerate;
                        entity["bsd_currencyfrom"] = objexchangerate.bsd_currencyfrom;
                        entity["bsd_currencyto"] = objexchangerate.bsd_currencyto;
                        string bsd_currencyfrom = retriveLookup("transactioncurrency", "isocurrencycode", objexchangerate.bsd_currencyfrom, org);
                        if (bsd_currencyfrom != null)
                        {
                            entity["bsd_currencyfrom"] = new EntityReference("transactioncurrency", Guid.Parse(bsd_currencyfrom));
                        }
                        else
                        {
                            return "Iso currencycode from not found";
                        }
                        string bsd_currencyto = retriveLookup("transactioncurrency", "isocurrencycode", objexchangerate.bsd_currencyto, org);
                        if (bsd_currencyto != null)
                        {
                            entity["bsd_currencyto"] = new EntityReference("transactioncurrency", Guid.Parse(bsd_currencyto));
                        }
                        else
                        {
                            return "Iso currencycode to not found";
                        }
                        crm.Service.Update(entity);

                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_exchangerate", "bsd_name", objexchangerate.bsd_name, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_exchangerate", Guid.Parse(id));
                            // entity["bsd_name"] = objexchangerate.bsd_name;
                            //
                            var xmlconfigdefault = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_configdefault'>
                                                    <attribute name='bsd_configdefaultid' />
                                                    <attribute name='createdon' />
                                                    <attribute name='bsd_bankdefault' />
                                                    <order attribute='createdon' descending='false' />
                                                  </entity>
                                                </fetch>";

                            var configdefault = crm.Service.RetrieveMultiple(new FetchExpression(xmlconfigdefault));
                            if (configdefault.Entities.Any())
                            {
                                // var bankdefault = (EntityReference)configdefault[0]["bsd_bankdefault"];
                                entity["bsd_bankaccount"] = (EntityReference)configdefault[0]["bsd_bankdefault"];
                            }
                            //
                            entity["bsd_codeax"] = objexchangerate.Recid;
                            entity["bsd_date"] = objexchangerate.bsd_date;
                            entity["bsd_exchangerate"] = objexchangerate.bsd_exchangerate;
                            entity["bsd_currencyfrom"] = objexchangerate.bsd_currencyfrom;
                            entity["bsd_currencyto"] = objexchangerate.bsd_currencyto;
                            string bsd_currencyfrom = retriveLookup("transactioncurrency", "isocurrencycode", objexchangerate.bsd_currencyfrom, org);
                            if (bsd_currencyfrom != null)
                            {
                                entity["bsd_currencyfrom"] = new EntityReference("transactioncurrency", Guid.Parse(bsd_currencyfrom));
                            }
                            else
                            {
                                return "Iso currencycode fromnot found";
                            }
                            string bsd_currencyto = retriveLookup("transactioncurrency", "isocurrencycode", objexchangerate.bsd_currencyto, org);
                            if (bsd_currencyto != null)
                            {
                                entity["bsd_currencyto"] = new EntityReference("transactioncurrency", Guid.Parse(bsd_currencyto));
                            }
                            else
                            {
                                return "Iso currencycode to not found";
                            }
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_exchangerate");
                            var xmlconfigdefault = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_configdefault'>
                                                    <attribute name='bsd_configdefaultid' />
                                                    <attribute name='createdon' />
                                                    <attribute name='bsd_bankdefault' />
                                                    <order attribute='createdon' descending='false' />
                                                  </entity>
                                                </fetch>";

                            var configdefault = crm.Service.RetrieveMultiple(new FetchExpression(xmlconfigdefault));
                            if (configdefault.Entities.Any())
                            {
                                // var bankdefault = (EntityReference)configdefault[0]["bsd_bankdefault"];
                                entity["bsd_bankaccount"] = (EntityReference)configdefault[0]["bsd_bankdefault"];
                            }
                            entity["bsd_codeax"] = objexchangerate.Recid;
                            entity["bsd_date"] = objexchangerate.bsd_date;
                            // entity["bsd_name"] = objexchangerate.bsd_name;
                            entity["bsd_exchangerate"] = objexchangerate.bsd_exchangerate;
                            entity["bsd_currencyfrom"] = objexchangerate.bsd_currencyfrom;
                            entity["bsd_currencyto"] = objexchangerate.bsd_currencyto;
                            string bsd_currencyfrom = retriveLookup("transactioncurrency", "isocurrencycode", objexchangerate.bsd_currencyfrom, org);
                            if (bsd_currencyfrom != null)
                            {
                                entity["bsd_currencyfrom"] = new EntityReference("transactioncurrency", Guid.Parse(bsd_currencyfrom));
                            }
                            else
                            {
                                return "Sale tax code id not found";
                            }
                            string bsd_currencyto = retriveLookup("transactioncurrency", "isocurrencycode", objexchangerate.bsd_currencyto, org);
                            if (bsd_currencyto != null)
                            {
                                entity["bsd_currencyto"] = new EntityReference("transactioncurrency", Guid.Parse(bsd_currencyto));
                            }
                            else
                            {
                                return "Sale tax code id not found";
                            }
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteExchangeRate(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_exchangerate", "bsd_codeax", Recid, org);
                    if (id != null)
                    {
                        crm.Service.Delete("bsd_exchangerate", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertSaleTaxGroup(SaleTaxGroup objsaletaxgroup, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objsaletaxgroup != null)
                {
                    if (string.IsNullOrEmpty(objsaletaxgroup.Recid)) return "Sales tax group id is not null";
                    //if (string.IsNullOrEmpty(objsaletaxgroup.bsd_type.ToString())) return "Sales tax group type is not null";
                    string id = retriveLookup("bsd_saletaxgroup", "bsd_codeax", objsaletaxgroup.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_saletaxgroup", Guid.Parse(id));
                        entity["bsd_salestaxgroup"] = objsaletaxgroup.bsd_salestaxgroup;
                        entity["bsd_name"] = objsaletaxgroup.bsd_salestaxgroup;
                        //entity["bsd_type"] = new OptionSetValue(objsaletaxgroup.bsd_type);
                        entity["bsd_description"] = objsaletaxgroup.bsd_description;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_saletaxgroup", "bsd_salestaxgroup", objsaletaxgroup.bsd_salestaxgroup, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_saletaxgroup", Guid.Parse(id));
                            entity["bsd_codeax"] = objsaletaxgroup.Recid;
                            entity["bsd_salestaxgroup"] = objsaletaxgroup.bsd_salestaxgroup;
                            entity["bsd_name"] = objsaletaxgroup.bsd_salestaxgroup;
                            //entity["bsd_type"] = new OptionSetValue(objsaletaxgroup.bsd_type);
                            entity["bsd_description"] = objsaletaxgroup.bsd_description;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_saletaxgroup");
                            entity["bsd_codeax"] = objsaletaxgroup.Recid;
                            entity["bsd_salestaxgroup"] = objsaletaxgroup.bsd_salestaxgroup;
                            entity["bsd_name"] = objsaletaxgroup.bsd_name;
                            //entity["bsd_type"] = new OptionSetValue(objsaletaxgroup.bsd_type);
                            entity["bsd_description"] = objsaletaxgroup.bsd_description;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteSaleTaxGroup(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_saletaxgroup", "bsd_codeax", Recid, org);
                    if (id != null)
                    {
                        crm.Service.Delete("bsd_saletaxgroup", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertItemSaleTaxGroup(ItemSalesTaxGroup objitemsaletaxgroup, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objitemsaletaxgroup != null)
                {
                    if (string.IsNullOrEmpty(objitemsaletaxgroup.Recid) || string.IsNullOrEmpty(objitemsaletaxgroup.bsd_code)) return "Item sales tax group id is not null";
                    // if (string.IsNullOrEmpty(objitemsaletaxgroup.bsd_saletaxcode)) return "Sales tax code is not null";
                    string id = retriveLookup("bsd_itemsalestaxgroup", "bsd_codeax", objitemsaletaxgroup.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_itemsalestaxgroup", Guid.Parse(id));
                        entity["bsd_code"] = objitemsaletaxgroup.bsd_code;
                        entity["bsd_name"] = objitemsaletaxgroup.bsd_code;
                        entity["bsd_description"] = objitemsaletaxgroup.bsd_name;
                        entity["bsd_descriptionitem"] = objitemsaletaxgroup.bsd_description;
                        entity["bsd_percentageamount"] = objitemsaletaxgroup.bsd_percentageamount;
                        string bsd_saletaxcode = retriveLookup("bsd_saletaxcode", "bsd_name", objitemsaletaxgroup.bsd_saletaxcode, org);
                        if (bsd_saletaxcode != null)
                        {
                            entity["bsd_saletaxcode"] = new EntityReference("bsd_saletaxcode", Guid.Parse(bsd_saletaxcode));
                        }
                        //else
                        //{
                        //    return "Sale tax code id not found";
                        //}
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_itemsalestaxgroup", "bsd_code", objitemsaletaxgroup.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_itemsalestaxgroup", Guid.Parse(id));
                            entity["bsd_codeax"] = objitemsaletaxgroup.Recid;
                            entity["bsd_code"] = objitemsaletaxgroup.bsd_code;
                            entity["bsd_name"] = objitemsaletaxgroup.bsd_code;
                            entity["bsd_description"] = objitemsaletaxgroup.bsd_name;
                            entity["bsd_descriptionitem"] = objitemsaletaxgroup.bsd_description;
                            entity["bsd_percentageamount"] = objitemsaletaxgroup.bsd_percentageamount;
                            string bsd_saletaxcode = retriveLookup("bsd_saletaxcode", "bsd_name", objitemsaletaxgroup.bsd_saletaxcode, org);
                            if (bsd_saletaxcode != null)
                            {
                                entity["bsd_saletaxcode"] = new EntityReference("bsd_saletaxcode", Guid.Parse(bsd_saletaxcode));
                            }
                            //else
                            //{
                            //    return "Sale tax code id not found";
                            //}
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_itemsalestaxgroup");
                            entity["bsd_codeax"] = objitemsaletaxgroup.Recid;
                            entity["bsd_code"] = objitemsaletaxgroup.bsd_code;
                            entity["bsd_name"] = objitemsaletaxgroup.bsd_code;
                            entity["bsd_description"] = objitemsaletaxgroup.bsd_name;
                            entity["bsd_descriptionitem"] = objitemsaletaxgroup.bsd_description;
                            entity["bsd_percentageamount"] = objitemsaletaxgroup.bsd_percentageamount;
                            string bsd_saletaxcode = retriveLookup("bsd_saletaxcode", "bsd_name", objitemsaletaxgroup.bsd_saletaxcode, org);
                            if (bsd_saletaxcode != null)
                            {
                                entity["bsd_saletaxcode"] = new EntityReference("bsd_saletaxcode", Guid.Parse(bsd_saletaxcode));
                            }
                            //else
                            //{
                            //    return "Sale tax code id not found";
                            //}
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteItemSaleTaxGroup(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_itemsalestaxgroup", "bsd_codeax", Recid, org);
                    if (id != null)
                    {
                        crm.Service.Delete("bsd_itemsalestaxgroup", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertUnitConversion(UnitConversion objunitconversion, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objunitconversion != null)
                {

                    string id = retriveLookup("bsd_unitconversions", "bsd_codeax", objunitconversion.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_unitconversions", Guid.Parse(id));
                        entity["bsd_name"] = objunitconversion.bsd_name;
                        entity["bsd_factor"] = objunitconversion.bsd_factor;
                        entity["bsd_description"] = objunitconversion.bsd_description;
                        string bsd_product = retriveLookup("product", "productnumber", objunitconversion.bsd_product, org);
                        if (bsd_product != null)
                        {
                            entity["bsd_product"] = new EntityReference("product", Guid.Parse(bsd_product));
                        }
                        else
                        {
                            return "Product id not found";
                        }
                        string bsd_fromunit = retriveLookup("uom", "name", objunitconversion.bsd_fromunit, org);
                        if (bsd_fromunit != null)
                        {
                            entity["bsd_fromunit"] = new EntityReference("uom", Guid.Parse(bsd_fromunit));
                        }
                        else
                        {
                            return "Unit not found";
                        }
                        string bsd_tounit = retriveLookup("uom", "name", objunitconversion.bsd_tounit, org);
                        if (bsd_tounit != null)
                        {
                            entity["bsd_tounit"] = new EntityReference("uom", Guid.Parse(bsd_tounit));
                        }
                        else
                        {
                            return "Unit not found";
                        }
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        Entity entity = new Entity("bsd_unitconversions");
                        entity["bsd_codeax"] = objunitconversion.Recid;
                        entity["bsd_name"] = objunitconversion.bsd_name;
                        entity["bsd_factor"] = objunitconversion.bsd_factor;
                        entity["bsd_description"] = objunitconversion.bsd_description;
                        string bsd_product = retriveLookup("product", "productnumber", objunitconversion.bsd_product, org);
                        if (bsd_product != null)
                        {
                            entity["bsd_product"] = new EntityReference("product", Guid.Parse(bsd_product));
                        }
                        else
                        {
                            return "Product id not found";
                        }
                        string bsd_fromunit = retriveLookup("uom", "name", objunitconversion.bsd_fromunit, org);
                        if (bsd_fromunit != null)
                        {
                            entity["bsd_fromunit"] = new EntityReference("uom", Guid.Parse(bsd_fromunit));
                        }
                        else
                        {
                            return "Unit not found";
                        }
                        string bsd_tounit = retriveLookup("uom", "name", objunitconversion.bsd_tounit, org);
                        if (bsd_tounit != null)
                        {
                            entity["bsd_tounit"] = new EntityReference("uom", Guid.Parse(bsd_tounit));
                        }
                        else
                        {
                            return "Unit not found";
                        }
                        crm.Service.Create(entity);
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteUnitConversion(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_unitconversions", "bsd_codeax", Recid, org);
                    if (id != null)
                    {
                        crm.Service.Delete("bsd_unitconversions", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertReturnReasonCode(ReturnReasonCode objreturnReasonCode, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objreturnReasonCode != null)
                {
                    if (string.IsNullOrEmpty(objreturnReasonCode.Recid) || string.IsNullOrEmpty(objreturnReasonCode.bsd_code)) return "Return reason code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_returnreasoncode", "bsd_codeax", objreturnReasonCode.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_returnreasoncode", Guid.Parse(id));
                        entity["bsd_name"] = objreturnReasonCode.bsd_name;
                        entity["bsd_code"] = objreturnReasonCode.bsd_code;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_returnreasoncode", "bsd_code", objreturnReasonCode.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_returnreasoncode", Guid.Parse(id));
                            entity["bsd_codeax"] = objreturnReasonCode.Recid;
                            entity["bsd_name"] = objreturnReasonCode.bsd_name;
                            entity["bsd_code"] = objreturnReasonCode.bsd_code;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_returnreasoncode");
                            entity["bsd_codeax"] = objreturnReasonCode.Recid;
                            entity["bsd_name"] = objreturnReasonCode.bsd_name;
                            entity["bsd_code"] = objreturnReasonCode.bsd_code;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteReturnReasonCode(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_returnreasoncode", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_returnreasoncode", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertSaleTaxCode(SalesTaxCode objsaletaxcode, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objsaletaxcode != null)
                {
                    if (string.IsNullOrEmpty(objsaletaxcode.Recid) || string.IsNullOrEmpty(objsaletaxcode.bsd_name)) return "Sales tax code id is not null";
                    if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_saletaxcode", "bsd_codeax", objsaletaxcode.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_saletaxcode", Guid.Parse(id));
                        entity["bsd_name"] = objsaletaxcode.bsd_name;
                        entity["bsd_percentageamount"] = objsaletaxcode.bsd_percentageamount;
                        entity["bsd_description"] = objsaletaxcode.bsd_description;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_saletaxcode", "bsd_name", objsaletaxcode.bsd_name, org);
                        if (id != null)
                        {
                            // Xữ lý dữ liệu cũ
                            Entity entity = new Entity("bsd_saletaxcode", Guid.Parse(id));
                            entity["bsd_codeax"] = objsaletaxcode.Recid;
                            entity["bsd_name"] = objsaletaxcode.bsd_name;
                            entity["bsd_percentageamount"] = objsaletaxcode.bsd_percentageamount;
                            entity["bsd_description"] = objsaletaxcode.bsd_description;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_saletaxcode");
                            entity["bsd_codeax"] = objsaletaxcode.Recid;
                            entity["bsd_name"] = objsaletaxcode.bsd_name;
                            entity["bsd_percentageamount"] = objsaletaxcode.bsd_percentageamount;
                            entity["bsd_description"] = objsaletaxcode.bsd_description;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }

        internal static bool DeleteSaleTaxCode(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_saletaxcode", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_saletaxcode", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertGroupUnit(GroupUnit objgroupunit, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objgroupunit != null)
                {
                    if (string.IsNullOrEmpty(objgroupunit.name)) return "Group unit name is not null";
                    if (string.IsNullOrEmpty(objgroupunit.baseuomname)) return "Base group unit is not null";
                    string id = retriveLookup("uomschedule", "name", objgroupunit.name, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("uomschedule", Guid.Parse(id));
                        entity["name"] = objgroupunit.name;
                        entity["baseuomname"] = objgroupunit.baseuomname;
                        entity["description"] = objgroupunit.id;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        Entity entity = new Entity("uomschedule", Guid.Parse(id));
                        entity["name"] = objgroupunit.name;
                        entity["baseuomname"] = objgroupunit.baseuomname;
                        entity["description"] = objgroupunit.id;
                        crm.Service.Create(entity);
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static string insertUnit(Unit objunit, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objunit != null)
                {
                    if (string.IsNullOrEmpty(objunit.name)) return "Unit name is not null";
                    if (string.IsNullOrEmpty(objunit.quantity.ToString())) return "Unit quantity is not null";
                    string id = retriveLookup("uom", "name", objunit.name, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("uom", Guid.Parse(id));
                        entity["name"] = objunit.name;
                        entity["quantity"] = objunit.quantity;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        Entity entity = new Entity("uom", Guid.Parse(id));
                        entity["name"] = objunit.name;
                        entity["quantity"] = objunit.quantity;
                        crm.Service.Create(entity);
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static string insertProduct(Product objproduct, string org)
        {
            var xml = "";
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objproduct != null)
                {

                    if (string.IsNullOrEmpty(objproduct.productnumber)) return "Product number is not null";
                    // if (string.IsNullOrEmpty(objproduct.bsd_itemsalestaxgroup)) return "Sales tax code id is not null";
                    //if (string.IsNullOrEmpty(objproduct.defaultuomscheduleid)) return "Default uom schedule id is not null";
                    //if (string.IsNullOrEmpty(objproduct.defaultuomid)) return "Default uom id is not null";
                    string id = retriveLookupProduct("product", "productnumber", objproduct.productnumber.Trim(), org);
                    if (id != null)
                    {
                        #region Update

                        Entity entity = new Entity("product", Guid.Parse(id));
                        entity["name"] = objproduct.name;
                        entity["description"] = objproduct.description;
                        string bsd_itemsalestaxgroupid = retriveLookup("bsd_itemsalestaxgroup", "bsd_name", objproduct.bsd_itemsalestaxgroup, org);
                        if (bsd_itemsalestaxgroupid != null)
                        {
                            entity["bsd_itemsalestaxgroup"] = new EntityReference("bsd_itemsalestaxgroup", Guid.Parse(bsd_itemsalestaxgroupid));
                        }
                        string uomscheduleid = retriveLookup("uomschedule", "name", objproduct.defaultuomscheduleid, org);
                        if (uomscheduleid != null)
                        {
                            entity["defaultuomscheduleid"] = new EntityReference("uomschedule", Guid.Parse(uomscheduleid));
                        }
                        //else
                        //{
                        //    return "Group Unit not found";
                        //}
                        #region uom
                        xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                     <entity name='uom'>
                                                    <attribute name='uomid' />
                                                    <attribute name='quantity' />
                                                    <filter type='and'>
                                                         <condition attribute='uomscheduleid' operator='eq' uitype='uomschedule' value='" + uomscheduleid + @"' />
                                                            <condition attribute = 'name' operator= 'eq' uitype = 'uom' value = '" + objproduct.defaultuomid + @"' />
                                                    </filter>
                                                   </entity>
                                                  </fetch>";

                        var lst_uom = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                        if (lst_uom.Entities.Any())
                        {
                            // var bankdefault = (EntityReference)configdefault[0]["bsd_bankdefault"];
                            Entity uom = lst_uom.Entities.First();
                            entity["defaultuomid"] = new EntityReference(uom.LogicalName, uom.Id); ;
                        }
                        //else
                        //{
                        //    return "Unit not found";
                        //}
                        #endregion
                        #region Customize
                        string bsd_weight = retriveLookup("bsd_weightproduct", "bsd_code", objproduct.bsd_weight.Trim(), org);
                        if (bsd_weight != null)
                        {
                            entity["bsd_weight"] = new EntityReference("bsd_weightproduct", Guid.Parse(bsd_weight));
                        }
                        //if (bsd_weight != null)
                        //{
                        //    Entity entity_weight = new Entity("bsd_weightproduct", Guid.Parse(bsd_weight));
                        //    entity_weight["bsd_code"] = objproduct.bsd_weight;
                        //    entity_weight["bsd_name"] = objproduct.bsd_weight;
                        //    crm.Service.Update(entity_weight);
                        //    entity["bsd_weight"] = new EntityReference("bsd_weightproduct", Guid.Parse(bsd_weight));
                        //}
                        //else
                        //{
                        //    Guid id_weight = Guid.NewGuid();
                        //    Entity entity_weight = new Entity("bsd_weightproduct", id_weight);
                        //    entity_weight["bsd_codeax"] = objproduct.productnumber.Trim();
                        //    entity_weight["bsd_code"] = objproduct.bsd_weight;
                        //    entity_weight["bsd_name"] = objproduct.bsd_weight;
                        //    crm.Service.Create(entity_weight);
                        //    entity["bsd_weight"] = new EntityReference("bsd_weightproduct", id_weight);
                        //}
                        string bsd_manufactory = retriveLookup("bsd_manufactory", "bsd_code", objproduct.bsd_manufactory, org);
                        if (bsd_manufactory != null)
                        {
                            entity["bsd_manufactory"] = new EntityReference("bsd_manufactory", Guid.Parse(bsd_manufactory));
                        }
                        string bsd_size = retriveLookup("bsd_size", "bsd_code", objproduct.bsd_size, org);
                        if (bsd_size != null)
                        {
                            entity["bsd_size"] = new EntityReference("bsd_size", Guid.Parse(bsd_size));
                        }
                        string bsd_style = retriveLookup("bsd_style", "bsd_code", objproduct.bsd_style, org);
                        if (bsd_style != null)
                        {
                            entity["bsd_style"] = new EntityReference("bsd_style", Guid.Parse(bsd_style));
                        }
                        string bsd_brand = retriveLookup("bsd_brand", "bsd_code", objproduct.bsd_brand, org);
                        if (bsd_brand != null)
                        {
                            entity["bsd_brand"] = new EntityReference("bsd_brand", Guid.Parse(bsd_brand));
                        }
                        string bsd_packing = retriveLookup("bsd_packing", "bsd_code", objproduct.bsd_packing, org);
                        if (bsd_packing != null)
                        {
                            entity["bsd_packing"] = new EntityReference("bsd_packing", Guid.Parse(bsd_packing));
                        }
                        string bsd_packaging = retriveLookup("bsd_packaging", "bsd_code", objproduct.bsd_packaging, org);
                        if (bsd_packaging != null)
                        {
                            entity["bsd_packaging"] = new EntityReference("bsd_packaging", Guid.Parse(bsd_packaging));
                        }
                        string bsd_configuration = retriveLookup("bsd_configuration", "bsd_code", objproduct.bsd_configuration, org);
                        if (bsd_configuration != null)
                        {
                            entity["bsd_configuration"] = new EntityReference("bsd_configuration", Guid.Parse(bsd_configuration));
                        }
                        #endregion
                        crm.Service.Update(entity);
                        return "success";
                        #endregion
                    }
                    else
                    {
                        #region Insert
                        Entity entity = new Entity("product");
                        entity["productnumber"] = objproduct.productnumber;
                        entity["name"] = objproduct.name;
                        entity["description"] = objproduct.description;
                        string bsd_itemsalestaxgroupid = retriveLookup("bsd_itemsalestaxgroup", "bsd_name", objproduct.bsd_itemsalestaxgroup, org);
                        if (bsd_itemsalestaxgroupid != null)
                        {
                            entity["bsd_itemsalestaxgroup"] = new EntityReference("bsd_itemsalestaxgroup", Guid.Parse(bsd_itemsalestaxgroupid));
                        }
                        //else
                        //{
                        //    return "Item sales tax group not found";
                        //}
                        string uomscheduleid = null;
                        if (objproduct.defaultuomscheduleid != null)
                        {
                            uomscheduleid = retriveLookup("uomschedule", "name", objproduct.defaultuomscheduleid, org);
                            if (uomscheduleid != null)
                            {
                                entity["defaultuomscheduleid"] = new EntityReference("uomschedule", Guid.Parse(uomscheduleid));
                            }
                        }
                        //else
                        //{
                        //    return "Group Unit not found";
                        //}
                        #region uom
                        if (objproduct.defaultuomid != null)
                        {
                            xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                     <entity name='uom'>
                                                    <attribute name='uomid'/>
                                                    <attribute name='quantity'/>
                                                    <filter type='and'>
                                                         <condition attribute='uomscheduleid' operator='eq' uitype='uomschedule' value='" + uomscheduleid + @"' />
                                                            <condition attribute = 'name' operator= 'eq' uitype = 'uom' value = '" + objproduct.defaultuomid + @"' />
                                                    </filter>
                                                   </entity>
                                                  </fetch>";
                            var lst_uom = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                            if (lst_uom.Entities.Any())
                            {
                                // var bankdefault = (EntityReference)configdefault[0]["bsd_bankdefault"];
                                Entity uom = lst_uom.Entities.First();
                                entity["defaultuomid"] = new EntityReference(uom.LogicalName, uom.Id); ;
                            }
                        }
                        //else
                        //{
                        //    return "Unit not found";
                        //}
                        //string uomid = retriveLookup("uom", "name", objproduct.defaultuomid, org);
                        //if (uomid != null)
                        //{
                        //    entity["defaultuomid"] = new EntityReference("uom", Guid.Parse(uomid));
                        //}

                        #endregion
                        #region Customize
                        string bsd_weight = retriveLookup("bsd_weightproduct", "bsd_code", objproduct.bsd_weight.Trim(), org);
                        if (bsd_weight != null)
                        {
                            entity["bsd_weight"] = new EntityReference("bsd_weightproduct", Guid.Parse(bsd_weight));
                        }
                        //if (bsd_weight != null)
                        //{
                        //    Entity entity_weight = new Entity("bsd_weightproduct", Guid.Parse(bsd_weight));
                        //    entity_weight["bsd_code"] = objproduct.bsd_weight;
                        //    entity_weight["bsd_name"] = objproduct.bsd_weight;
                        //    crm.Service.Update(entity_weight);
                        //    entity["bsd_weight"] = new EntityReference("bsd_weightproduct", Guid.Parse(bsd_weight));
                        //}
                        //else
                        //{
                        //    Guid id_weight = Guid.NewGuid();
                        //    Entity entity_weight = new Entity("bsd_weightproduct", id_weight);
                        //    entity_weight["bsd_codeax"] = objproduct.productnumber.Trim();
                        //    entity_weight["bsd_code"] = objproduct.bsd_weight;
                        //    entity_weight["bsd_name"] = objproduct.bsd_weight;
                        //    crm.Service.Create(entity_weight);
                        //    entity["bsd_weight"] = new EntityReference("bsd_weightproduct", id_weight);
                        //}
                        string bsd_manufactory = retriveLookup("bsd_manufactory", "bsd_code", objproduct.bsd_manufactory, org);
                        if (bsd_manufactory != null)
                        {
                            entity["bsd_manufactory"] = new EntityReference("bsd_manufactory", Guid.Parse(bsd_manufactory));
                        }
                        string bsd_size = retriveLookup("bsd_size", "bsd_code", objproduct.bsd_size, org);
                        if (bsd_size != null)
                        {
                            entity["bsd_size"] = new EntityReference("bsd_size", Guid.Parse(bsd_size));
                        }
                        string bsd_style = retriveLookup("bsd_style", "bsd_code", objproduct.bsd_style, org);
                        if (bsd_style != null)
                        {
                            entity["bsd_style"] = new EntityReference("bsd_style", Guid.Parse(bsd_style));
                        }
                        string bsd_brand = retriveLookup("bsd_brand", "bsd_code", objproduct.bsd_brand, org);
                        if (bsd_brand != null)
                        {
                            entity["bsd_brand"] = new EntityReference("bsd_brand", Guid.Parse(bsd_brand));
                        }
                        string bsd_packing = retriveLookup("bsd_packing", "bsd_code", objproduct.bsd_packing, org);
                        if (bsd_packing != null)
                        {
                            entity["bsd_packing"] = new EntityReference("bsd_packing", Guid.Parse(bsd_packing));
                        }
                        string bsd_packaging = retriveLookup("bsd_packaging", "bsd_code", objproduct.bsd_packaging, org);
                        if (bsd_packaging != null)
                        {
                            entity["bsd_packaging"] = new EntityReference("bsd_packaging", Guid.Parse(bsd_packaging));
                        }
                        string bsd_configuration = retriveLookup("bsd_configuration", "bsd_code", objproduct.bsd_configuration, org);
                        if (bsd_configuration != null)
                        {
                            entity["bsd_configuration"] = new EntityReference("bsd_configuration", Guid.Parse(bsd_configuration));
                        }
                        #endregion
                        Guid idproduct = crm.Service.Create(entity);
                        Entity newproduct_update = new Entity("product", idproduct);
                        SetStateRequest setStateRequest = new SetStateRequest()
                        {
                            EntityMoniker = new EntityReference
                            {
                                Id = newproduct_update.Id,
                                LogicalName = newproduct_update.LogicalName
                            },
                            State = new OptionSetValue(0),
                            Status = new OptionSetValue(1)
                        };
                        crm.Service.Execute(setStateRequest);
                        crm.Service.Update(newproduct_update);
                        return "success";
                        #endregion
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error CRM" + ex.Message + "objproduct.defaultuomscheduleid" + objproduct.defaultuomscheduleid + "objproduct.defaultuomid" + objproduct.defaultuomid;// + " xml:" + xml;
                throw;
            }
        }
        internal static bool DeleteProduct(string ItemId, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (ItemId != null)
                {
                    string id = retriveLookupProduct("product", "productnumber", ItemId.Trim(), org);
                    if (id != null)
                    {
                        crm.Service.Delete("product", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        //vinhlh insert Account 1-6-2018
        internal static string insertAccount(Account objAccount, string org)
        {
            string trace = "0";
            try
            {
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);

                if (objAccount != null)
                {
                    trace = "1";
                    if (string.IsNullOrEmpty(objAccount.accountnumber)) return "Account number is not null";
                    string id = retriveLookup("account", "accountnumber", objAccount.accountnumber, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("account", Guid.Parse(id));
                        #region Account entity
                        trace = "2";
                        if (objAccount.name != null)
                            entity["name"] = objAccount.name;
                        trace = "3";
                        if (objAccount.bsd_taxregistration != null)
                            entity["bsd_taxregistration"] = objAccount.bsd_taxregistration;
                        trace = "4";
                        if (objAccount.bsd_saletaxgroup.Trim() != null && objAccount.bsd_saletaxgroup.Trim() != "")
                        {
                            string guid = retriveLookup("bsd_saletaxgroup", "bsd_salestaxgroup", objAccount.bsd_saletaxgroup, org);
                            if (guid != null)
                            {
                                entity["bsd_saletaxgroup"] = new EntityReference("bsd_saletaxgroup", Guid.Parse(guid));
                            }
                            else return "Sales tax group not found";
                        }
                        trace = "5";
                        if (objAccount.bsd_accountgroup.Trim() != null && objAccount.bsd_accountgroup.Trim() != "")
                        {
                            string guid = retriveLookup("bsd_groupaccount", "bsd_name", objAccount.bsd_accountgroup, org);
                            if (guid != null)
                            {
                                entity["bsd_accountgroup"] = new EntityReference("bsd_groupaccount", Guid.Parse(guid));
                            }
                            else return "Account group not found";
                        }
                        trace = "6";
                        if (objAccount.bsd_paymentterm.Trim() != null && objAccount.bsd_paymentterm.Trim() != "")
                        {
                            string guid = retriveLookup("bsd_paymentterm", "bsd_termofpayment", objAccount.bsd_paymentterm, org);
                            if (guid != null)
                            {
                                entity["bsd_paymentterm"] = new EntityReference("bsd_paymentterm", Guid.Parse(guid));
                            }
                            else return "Payment term not found";
                        }
                        trace = "7";
                        if (objAccount.bsd_paymentmethod.Trim() != null && objAccount.bsd_paymentmethod.Trim() != "")
                        {
                            string guid = retriveLookup("bsd_methodofpayment", "bsd_methodofpayment", objAccount.bsd_paymentmethod, org);
                            if (guid != null)
                            {
                                entity["bsd_paymentmethod"] = new EntityReference("bsd_methodofpayment", Guid.Parse(guid));
                            }
                            else return "Method of payment not found";
                        }
                        trace = "8";
                        #endregion
                        Random rnd = new Random();
                        entity["address2_city"] = (rnd.Next(1, 1000000)).ToString();
                        trace = "9";
                        crm.Service.Update(entity);
                        return "success";
                    }
                }
            }
            catch (Exception ex)
            {
                return "CRM " + ex.Message + trace;
            }
            return "success";
        }
        //end vinhlh
        internal static string insertWeightProduct(WeightProduct objWeightProduct, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objWeightProduct != null)
                {
                    if (string.IsNullOrEmpty(objWeightProduct.Recid) || string.IsNullOrEmpty(objWeightProduct.bsd_code) || string.IsNullOrEmpty(objWeightProduct.bsd_name)) return "Weight product id is not null";
                    string id = retriveLookup("bsd_weightproduct", "bsd_codeax", objWeightProduct.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_weightproduct", Guid.Parse(id));
                        entity["bsd_code"] = objWeightProduct.bsd_code;
                        entity["bsd_name"] = objWeightProduct.bsd_name;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        Entity entity = new Entity("bsd_weightproduct");
                        entity["bsd_codeax"] = objWeightProduct.Recid;
                        entity["bsd_code"] = objWeightProduct.bsd_code;
                        entity["bsd_name"] = objWeightProduct.bsd_name;
                        crm.Service.Create(entity);
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteWeightProduct(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {
                    string id = retriveLookup("bsd_weightproduct", "bsd_codeax", Recid, org);
                    if (id != null)
                    {
                        crm.Service.Delete("bsd_weightproduct", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        // End Vinhlh
        //vinhlh 11-09-2017
        internal static string insertSize(Size obj, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (string.IsNullOrEmpty(obj.Recid) || string.IsNullOrEmpty(obj.bsd_code)) return "Size code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_size", "bsd_codeax", obj.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_size", Guid.Parse(id));
                        entity["bsd_name"] = obj.bsd_name;
                        entity["bsd_code"] = obj.bsd_code;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_size", "bsd_code", obj.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_size", Guid.Parse(id));
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_size");
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteSize(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_size", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_size", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertImportDeclaration(ImportDeclaration obj, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (string.IsNullOrEmpty(obj.Recid) || string.IsNullOrEmpty(obj.bsd_importdeclaration)) return "Code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_importdeclaration", "bsd_codeax", obj.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_importdeclaration", Guid.Parse(id));
                        entity["bsd_name"] = obj.bsd_importdeclaration;
                        entity["bsd_importdeclaration"] = obj.bsd_importdeclaration;
                        entity["bsd_typedeclaration"] = obj.bsd_typedeclaration;
                        entity["bsd_description"] = obj.bsd_description;
                        entity["bsd_date"] = obj.bsd_date;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_importdeclaration", "bsd_importdeclaration", obj.bsd_importdeclaration, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_importdeclaration", Guid.Parse(id));
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_importdeclaration;
                            entity["bsd_importdeclaration"] = obj.bsd_importdeclaration;
                            entity["bsd_typedeclaration"] = obj.bsd_typedeclaration;
                            entity["bsd_description"] = obj.bsd_description;
                            entity["bsd_date"] = obj.bsd_date;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_importdeclaration");
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_importdeclaration;
                            entity["bsd_importdeclaration"] = obj.bsd_importdeclaration;
                            entity["bsd_typedeclaration"] = obj.bsd_typedeclaration;
                            entity["bsd_description"] = obj.bsd_description;
                            entity["bsd_date"] = obj.bsd_date;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteImportDeclaration(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {
                    string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                          <entity name='bsd_suborder'>
                                            <attribute name='bsd_name' />
                                            <attribute name='createdon' />
                                            <attribute name='bsd_suborderid' />
                                            <order attribute='bsd_name' descending='false' />
                                            <link-entity name='bsd_bsd_suborder_bsd_importdeclaration' from='bsd_suborderid' to='bsd_suborderid' visible='false' intersect='true'>
                                              <link-entity name='bsd_importdeclaration' from='bsd_importdeclarationid' to='bsd_importdeclarationid' alias='ad'>
                                                <filter type='and'>
                                                  <condition attribute='bsd_codeax' operator='eq' value='" + Recid.Trim() + @"' />
                                                </filter>
                                              </link-entity>
                                            </link-entity>
                                          </entity>
                                        </fetch>";
                    var lst = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                    if (lst.Entities.Any())
                    {

                        return false;

                    }
                    string id = retriveLookup("bsd_importdeclaration", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_importdeclaration", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertStyle(Style obj, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (string.IsNullOrEmpty(obj.Recid) || string.IsNullOrEmpty(obj.bsd_code)) return "code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_style", "bsd_codeax", obj.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_style", Guid.Parse(id));
                        entity["bsd_name"] = obj.bsd_name;
                        entity["bsd_code"] = obj.bsd_code;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_style", "bsd_code", obj.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_style", Guid.Parse(id));
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_style");
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteStyle(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_style", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_style", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertManufactory(Manufactory obj, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (string.IsNullOrEmpty(obj.Recid) || string.IsNullOrEmpty(obj.bsd_code)) return "code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_manufactory", "bsd_codeax", obj.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_manufactory", Guid.Parse(id));
                        entity["bsd_name"] = obj.bsd_name;
                        entity["bsd_code"] = obj.bsd_code;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_manufactory", "bsd_code", obj.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_manufactory", Guid.Parse(id));
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_manufactory");
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteManufactory(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_manufactory", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_manufactory", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertConfiguration(Configuration obj, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (string.IsNullOrEmpty(obj.Recid) || string.IsNullOrEmpty(obj.bsd_code)) return "code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_configuration", "bsd_codeax", obj.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_configuration", Guid.Parse(id));
                        entity["bsd_name"] = obj.bsd_name;
                        entity["bsd_code"] = obj.bsd_code;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_configuration", "bsd_code", obj.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_configuration", Guid.Parse(id));
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_configuration");
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteConfiguration(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_configuration", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_configuration", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertBrand(Brand obj, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (string.IsNullOrEmpty(obj.Recid) || string.IsNullOrEmpty(obj.bsd_code)) return "code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_brand", "bsd_codeax", obj.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_brand", Guid.Parse(id));
                        entity["bsd_name"] = obj.bsd_name;
                        entity["bsd_code"] = obj.bsd_code;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_brand", "bsd_code", obj.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_brand", Guid.Parse(id));
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_brand");
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeleteBrand(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_brand", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_brand", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertPacking(Packing obj, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (string.IsNullOrEmpty(obj.Recid) || string.IsNullOrEmpty(obj.bsd_code)) return "code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_packing", "bsd_codeax", obj.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_packing", Guid.Parse(id));
                        entity["bsd_name"] = obj.bsd_name;
                        entity["bsd_code"] = obj.bsd_code;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_packing", "bsd_code", obj.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_packing", Guid.Parse(id));
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_packing");
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeletePacking(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_packing", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_packing", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertPackaging(Packaging obj, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (string.IsNullOrEmpty(obj.Recid) || string.IsNullOrEmpty(obj.bsd_code)) return "code id is not null";
                    // if (string.IsNullOrEmpty(objsaletaxcode.bsd_percentageamount.ToString())) return "Sales tax group percent tage amount is not null";
                    string id = retriveLookup("bsd_packaging", "bsd_codeax", obj.Recid, org);
                    if (id != null)
                    {
                        Entity entity = new Entity("bsd_packaging", Guid.Parse(id));
                        entity["bsd_name"] = obj.bsd_name;
                        entity["bsd_code"] = obj.bsd_code;
                        crm.Service.Update(entity);
                        return "success";
                    }
                    else
                    {
                        id = retriveLookup("bsd_packaging", "bsd_code", obj.bsd_code, org);
                        if (id != null)
                        {
                            Entity entity = new Entity("bsd_packaging", Guid.Parse(id));
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Update(entity);
                        }
                        else
                        {
                            Entity entity = new Entity("bsd_packaging");
                            entity["bsd_codeax"] = obj.Recid;
                            entity["bsd_name"] = obj.bsd_name;
                            entity["bsd_code"] = obj.bsd_code;
                            crm.Service.Create(entity);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
        }
        internal static bool DeletePackaging(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {

                    string id = retriveLookup("bsd_packaging", "bsd_codeax", Recid, org);
                    if (id != null)
                    {

                        crm.Service.Delete("bsd_packaging", Guid.Parse(id));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        //end vinhlh 11-09-2017
        internal static string insertWarehouse(Warehouse objwarehouse, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (string.IsNullOrEmpty(objwarehouse.bsd_warehouseid) || string.IsNullOrEmpty(objwarehouse.Recid)) return "Warehouse id is not null";
                if (string.IsNullOrEmpty(objwarehouse.bsd_site)) return "Site id is not null";
                if (string.IsNullOrEmpty(objwarehouse.bsd_companyname)) return "Company id is not null";
                if (string.IsNullOrEmpty(objwarehouse.bsd_address)) return "Company address is not null";
                string warhouseid = retriveLookup("bsd_warehouseentity", "bsd_codeax", objwarehouse.Recid, org);
                if (warhouseid != null)
                {

                    Entity updateEntity = new Entity("bsd_warehouseentity", Guid.Parse(warhouseid));
                    string siteid = retriveLookup("bsd_site", "bsd_code", objwarehouse.bsd_site, org);
                    if (siteid != null)
                    {
                        updateEntity["bsd_site"] = new EntityReference("bsd_site", Guid.Parse(siteid));
                    }
                    else
                    {
                        return "siteid not found";
                    }
                    string accountid = retriveLookup("account", "accountnumber", objwarehouse.bsd_companyname, org);
                    if (accountid != null)
                    {

                        updateEntity["bsd_companyname"] = new EntityReference("account", Guid.Parse(accountid));
                    }
                    else
                    {
                        return "Company name not found";
                    }

                    Entity address = crm.Service.Retrieve("bsd_address", Guid.Parse(objwarehouse.bsd_address), new ColumnSet(true));
                    if (address != null)
                    {
                        updateEntity["bsd_address"] = new EntityReference(address.LogicalName, address.Id);
                    }
                    else
                    {
                        return "Company Address not found";
                    }
                    updateEntity["bsd_warehouseid"] = objwarehouse.bsd_warehouseid;
                    updateEntity["bsd_name"] = objwarehouse.bsd_warehouseid;
                    updateEntity["bsd_description"] = objwarehouse.bsd_description;
                    crm.Service.Update(updateEntity);
                    return "success";


                }
                else
                {
                    Entity CreateEntity = new Entity("bsd_warehouseentity");
                    string siteid = retriveLookup("bsd_site", "bsd_code", objwarehouse.bsd_site, org);
                    if (siteid != null)
                    {
                        CreateEntity["bsd_site"] = new EntityReference("bsd_site", Guid.Parse(siteid));

                    }
                    else
                    {
                        return "siteid not found";
                    }
                    string accountid = retriveLookup("account", "accountnumber", objwarehouse.bsd_companyname, org);
                    if (accountid != null)
                    {
                        CreateEntity["bsd_companyname"] = new EntityReference("account", Guid.Parse(accountid));
                    }
                    else
                    {
                        return "Company name not found";
                    }
                    Entity address = crm.Service.Retrieve("bsd_address", Guid.Parse(objwarehouse.bsd_address), new ColumnSet(true));
                    if (address != null)
                    {
                        CreateEntity["bsd_address"] = new EntityReference(address.LogicalName, address.Id);
                    }
                    else
                    {
                        return "Company Address not found";
                    }
                    CreateEntity["bsd_warehouseid"] = objwarehouse.bsd_warehouseid;
                    CreateEntity["bsd_name"] = objwarehouse.bsd_warehouseid;
                    CreateEntity["bsd_description"] = objwarehouse.bsd_description;
                    crm.Service.Create(CreateEntity);
                    return "success";
                }
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
            // return "success";
        }
        internal static bool DeleteWarehouse(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                string warhouseid = retriveLookup("bsd_warehouseentity", "bsd_codeax", Recid, org);
                if (warhouseid != null)
                {
                    crm.Service.Delete("bsd_warehouseentity", Guid.Parse(warhouseid));
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
            // return "success";
        }
        internal static string insertSite(Site objSite, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (string.IsNullOrEmpty(objSite.bsd_code) || string.IsNullOrEmpty(objSite.Recid)) return "Site id is not null";
                //if (string.IsNullOrEmpty(objSite.bsd_companyname)) return "Company id is not null";
                // if (string.IsNullOrEmpty(objSite.bsd_address)) return "Company address is not null";
                string siteid = retriveLookup("bsd_site", "bsd_codeax", objSite.Recid, org);
                if (siteid != null)
                {
                    Entity entityUpdate = new Entity("bsd_site", Guid.Parse(siteid));
                    entityUpdate["bsd_code"] = objSite.bsd_code;
                    entityUpdate["bsd_name"] = objSite.bsd_code;
                    string accountid = retriveLookup("account", "accountnumber", objSite.bsd_companyname, org);
                    if (accountid != null)
                    {
                        entityUpdate["bsd_companyname"] = new EntityReference("account", Guid.Parse(accountid));
                    }
                    //else
                    //{
                    //    return "Company name not found";
                    //}
                    Entity address = crm.Service.Retrieve("bsd_address", Guid.Parse(objSite.bsd_address), new ColumnSet(true));
                    if (address != null)
                    {
                        entityUpdate["bsd_address"] = new EntityReference(address.LogicalName, address.Id);
                    }
                    //else
                    //{
                    //    return "Company Address not found";
                    //}
                    crm.Service.Update(entityUpdate);
                    return "success";

                }
                else
                {
                    siteid = retriveLookup("bsd_site", "bsd_code", objSite.bsd_code, org);
                    if (siteid != null)
                    {
                        Entity entityUpdate = new Entity("bsd_site", Guid.Parse(siteid));
                        entityUpdate["bsd_codeax"] = objSite.Recid;
                        entityUpdate["bsd_code"] = objSite.bsd_code;
                        entityUpdate["bsd_name"] = objSite.bsd_code;
                        string accountid = retriveLookup("account", "accountnumber", objSite.bsd_companyname, org);
                        if (accountid != null)
                        {
                            entityUpdate["bsd_companyname"] = new EntityReference("account", Guid.Parse(accountid));
                        }
                        //else
                        //{
                        //    return "Company name not found";
                        //}
                        Entity address = crm.Service.Retrieve("bsd_address", Guid.Parse(objSite.bsd_address), new ColumnSet(true));
                        if (address != null)
                        {
                            entityUpdate["bsd_address"] = new EntityReference(address.LogicalName, address.Id);
                        }
                        //else
                        //{
                        //    return "Company Address not found";
                        //}
                        crm.Service.Update(entityUpdate);
                    }
                    else
                    {

                        Entity entityCreate = new Entity("bsd_site");
                        entityCreate["bsd_codeax"] = objSite.Recid;
                        entityCreate["bsd_code"] = objSite.bsd_code;
                        entityCreate["bsd_name"] = objSite.bsd_code;
                        string accountid = retriveLookup("account", "accountnumber", objSite.bsd_companyname, org);
                        if (accountid != null)
                        {
                            entityCreate["bsd_companyname"] = new EntityReference("account", Guid.Parse(accountid));
                        }
                        else
                        {
                            return "Company name not found";
                        }
                        Entity address = crm.Service.Retrieve("bsd_address", Guid.Parse(objSite.bsd_address), new ColumnSet(true));
                        if (address != null)
                        {
                            entityCreate["bsd_address"] = new EntityReference(address.LogicalName, address.Id);
                        }
                        else
                        {
                            return "Company Address not found";
                        }
                        crm.Service.Create(entityCreate);

                    }
                    return "success";
                }
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
            // return "success";
        }
        internal static bool DeleteSite(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                string siteid = retriveLookup("bsd_site", "bsd_codeax", Recid, org);
                if (siteid != null)
                {
                    crm.Service.Delete("bsd_site", Guid.Parse(siteid));

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
            // return "success";
        }
        internal static string insertPaymentMethod(PaymentMethod objpaymentmethod, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objpaymentmethod != null)
                {
                    if (string.IsNullOrEmpty(objpaymentmethod.Recid)) return "Method of payment is not null";
                    string paymenttermid = retriveLookup("bsd_methodofpayment", "bsd_codeax", objpaymentmethod.Recid, org);
                    if (paymenttermid != null)
                    {

                        Entity entityUpdate = new Entity("bsd_methodofpayment", Guid.Parse(paymenttermid));
                        entityUpdate["bsd_methodofpayment"] = objpaymentmethod.bsd_methodofpayment;
                        entityUpdate["bsd_name"] = objpaymentmethod.bsd_methodofpayment;
                        entityUpdate["bsd_description"] = objpaymentmethod.bsd_description;
                        crm.Service.Update(entityUpdate);
                        return "success";
                    }
                    else
                    {
                        paymenttermid = retriveLookup("bsd_methodofpayment", "bsd_methodofpayment", objpaymentmethod.bsd_methodofpayment, org);
                        if (paymenttermid != null)
                        {
                            Entity entityUpdate = new Entity("bsd_methodofpayment", Guid.Parse(paymenttermid));
                            entityUpdate["bsd_codeax"] = objpaymentmethod.Recid;
                            entityUpdate["bsd_methodofpayment"] = objpaymentmethod.bsd_methodofpayment;
                            entityUpdate["bsd_name"] = objpaymentmethod.bsd_methodofpayment;
                            entityUpdate["bsd_description"] = objpaymentmethod.bsd_description;
                            crm.Service.Update(entityUpdate);
                        }
                        else
                        {
                            Entity entityCreate = new Entity("bsd_methodofpayment");
                            entityCreate["bsd_codeax"] = objpaymentmethod.Recid;
                            entityCreate["bsd_methodofpayment"] = objpaymentmethod.bsd_methodofpayment;
                            entityCreate["bsd_name"] = objpaymentmethod.bsd_methodofpayment;
                            entityCreate["bsd_description"] = objpaymentmethod.bsd_description;
                            crm.Service.Create(entityCreate);
                        }
                        return "success";
                    }
                }
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
            return "success";
        }
        internal static bool DeletePaymentMethod(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {
                    string paymenttermid = retriveLookup("bsd_methodofpayment", "bsd_codeax", Recid, org);
                    if (paymenttermid != null)
                    {
                        crm.Service.Delete("bsd_methodofpayment", Guid.Parse(paymenttermid));
                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
        }
        internal static string insertPaymentTerm(PaymentTerm objpaymentterm, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (objpaymentterm != null)
                {
                    if (string.IsNullOrEmpty(objpaymentterm.Recid)) return "Payment term is not null";
                    string paymenttermid = retriveLookup("bsd_paymentterm", "bsd_codeax", objpaymentterm.Recid, org);
                    if (paymenttermid != null)
                    {

                        Entity entityUpdate = new Entity("bsd_paymentterm", Guid.Parse(paymenttermid));
                        entityUpdate["bsd_termofpayment"] = objpaymentterm.bsd_termofpayment;
                        entityUpdate["bsd_name"] = objpaymentterm.bsd_termofpayment;
                        entityUpdate["bsd_date"] = objpaymentterm.bsd_date;
                        entityUpdate["bsd_description"] = objpaymentterm.bsd_description;
                        crm.Service.Update(entityUpdate);
                        return "success";

                    }
                    else
                    {
                        paymenttermid = retriveLookup("bsd_paymentterm", "bsd_termofpayment", objpaymentterm.bsd_termofpayment, org);
                        if (paymenttermid != null)
                        {
                            Entity entityUpdate = new Entity("bsd_paymentterm", Guid.Parse(paymenttermid));
                            entityUpdate["bsd_codeax"] = objpaymentterm.Recid;
                            entityUpdate["bsd_termofpayment"] = objpaymentterm.bsd_termofpayment;
                            entityUpdate["bsd_name"] = objpaymentterm.bsd_termofpayment;
                            entityUpdate["bsd_date"] = objpaymentterm.bsd_date;
                            entityUpdate["bsd_description"] = objpaymentterm.bsd_description;
                            crm.Service.Update(entityUpdate);
                        }
                        else
                        {
                            Entity entityCreate = new Entity("bsd_paymentterm");
                            entityCreate["bsd_codeax"] = objpaymentterm.Recid;
                            entityCreate["bsd_termofpayment"] = objpaymentterm.bsd_termofpayment;
                            entityCreate["bsd_name"] = objpaymentterm.bsd_termofpayment;
                            entityCreate["bsd_date"] = objpaymentterm.bsd_date;
                            entityCreate["bsd_description"] = objpaymentterm.bsd_description;
                            crm.Service.Create(entityCreate);
                        }
                        return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error " + ex.Message;
                throw;
            }
            //   return "succes";
        }
        internal static bool DeletePaymentTerm(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {
                    string paymenttermid = retriveLookup("bsd_paymentterm", "bsd_codeax", Recid, org);
                    if (paymenttermid != null)
                    {
                        crm.Service.Delete("bsd_paymentterm", Guid.Parse(paymenttermid));

                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
            //   return "succes";
        }
        internal static string insertGoodIssueNote(GoodsIssueNote objissuenote, string org)
        {
            var guidGoodissueNote = new Guid();
            var guidDeliveryNote = new Guid();
            int count_goodsissuenoteproduct = 0;
            int count_deliverynoteproduct = 0;
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                Entity suborder = null;
                Entity deliveryplan = null;
                Entity deliverynote = null;
                Entity en_requestdelivery = null;
                var flag = false;
                string requestdeliveryid = "";
                if (objissuenote != null)
                {
                    if (objissuenote.suborderid != null)
                    {
                        string fetchorderid = retriveLookup("bsd_suborder", "bsd_suborderax", objissuenote.suborderid, org);
                        if (fetchorderid != null)
                        {
                            #region Return Order
                            suborder = crm.Service.Retrieve("bsd_suborder", new Guid(fetchorderid), new ColumnSet(true));
                            if (suborder.Contains("bsd_type"))
                            {
                                OptionSetValue type = ((OptionSetValue)suborder["bsd_type"]);
                                if (type.Value == 861450004)
                                {
                                    if (suborder.Contains("bsd_returnorder"))
                                    {
                                        EntityReference returnorder = (EntityReference)suborder["bsd_returnorder"];
                                        Entity entityupdate = new Entity(returnorder.LogicalName, returnorder.Id);
                                        entityupdate["bsd_status"] = new OptionSetValue(861450002);
                                        crm.Service.Update(entityupdate);
                                        return "succces";
                                    }
                                    else
                                    {
                                        return "return Order is transfer faleld b2c";
                                    }

                                }
                            }
                            #endregion
                        }
                        //  return count.ToString();
                    }
                    // objissuenote = new GoodsIssueNote();
                    flag = isvalidfileld("bsd_deliverybill", "bsd_issuenoteax", objissuenote.PackingslIp, org);
                    if (flag == false)
                    {
                        #region Sales Order tạo Good Issues Note and Cập nhật Request Delivery and Tạo Delivery Note
                        Entity request_delivery = null;
                        #region Tạo mới Good issuse Note
                        Entity en = new Entity("bsd_deliverybill", guidGoodissueNote);
                        var fetchAccount = retriveLookup("account", "accountnumber", objissuenote.InvoiceAccount, org);
                        var fetchSite = retriveLookup("bsd_site", "bsd_code", objissuenote.Site, org);
                        // var fetchwarehouseentity = retriveLookup("bsd_warehouseentity", "bsd_warehouseid", objissuenote.Warehouse, org);
                        if (!string.IsNullOrEmpty(fetchAccount))
                        {
                            en["bsd_customer"] = new EntityReference("account", Guid.Parse(fetchAccount));
                        }

                        if (!string.IsNullOrEmpty(fetchSite))
                        {
                            en["bsd_site"] = new EntityReference("bsd_site", Guid.Parse(fetchSite));
                            string xmlWarehouseGood = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(fetchSite) + @"' />
                                                                      <condition attribute='bsd_warehouseid' operator='eq' value='" + objissuenote.Warehouse + @"' />
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                            var WarehouseGood = crm.Service.RetrieveMultiple(new FetchExpression(xmlWarehouseGood));
                            if (WarehouseGood.Entities.Any())
                            {

                                en["bsd_warehouse"] = new EntityReference("bsd_warehouseentity", WarehouseGood.Entities.First().Id);

                            }
                            else
                            {
                                return "Warehouse does not exist";
                            }
                        }
                        else
                        {
                            return "Site does not exist";
                        }

                        if (objissuenote.RequestShipDate != null)
                        {
                            en["bsd_requestedshipdate"] = objissuenote.RequestShipDate;
                        }
                        if (objissuenote.RequestreceiptDate != null)
                        {
                            en["bsd_requestedreceiptdate"] = objissuenote.RequestreceiptDate;
                        }
                        if (objissuenote.PackingslIp != null)
                        {
                            en["bsd_issuenoteax"] = objissuenote.PackingslIp;

                        }
                        if (objissuenote.issuenoteax != null)
                        {
                            #region Cập nhật lại Request Delivery
                            requestdeliveryid = retrivestringvaluelookuplike("bsd_requestdelivery", "bsd_pickinglistax", objissuenote.issuenoteax.Trim(), org);
                            if (requestdeliveryid != null)
                            {
                                request_delivery = crm.Service.Retrieve("bsd_requestdelivery", Guid.Parse(requestdeliveryid), new ColumnSet(true));
                                en_requestdelivery = new Entity(request_delivery.LogicalName, request_delivery.Id);
                                en_requestdelivery["bsd_createddeliverybill"] = true;
                                en_requestdelivery["bsd_createddeliverynote"] = true;
                                en["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                                en["bsd_shiptoaddress"] = (EntityReference)request_delivery["bsd_shiptoaddress"];
                                en["bsd_deliveryplan"] = (EntityReference)request_delivery["bsd_deliveryplan"];
                            }
                            else
                            {
                                return "Packing Slip not remember in crm";
                            }
                            #endregion
                        }
                        en["bsd_createddeliverynote"] = true;

                        #endregion
                        #region tạo delivery Note
                        EntityReference rf_deliveryplan = (EntityReference)request_delivery["bsd_deliveryplan"];
                        deliveryplan = crm.Service.Retrieve(rf_deliveryplan.LogicalName, rf_deliveryplan.Id, new ColumnSet(true));
                        deliverynote = new Entity("bsd_deliverynote", guidDeliveryNote);
                        bool request_porter = (bool)suborder["bsd_requestporter"];
                        bool request_shipping = (bool)suborder["bsd_transportation"];
                        bool shippingoption = request_delivery.HasValue("bsd_shippingoption") ? (bool)request_delivery["bsd_shippingoption"] : false;
                        Entity shipping_pricelist = null;

                        decimal total_shipping_price = 0m;
                        if (shippingoption)
                        {
                            EntityReference shipping_pricelist_ref = (EntityReference)request_delivery["bsd_shippingpricelist"];
                            shipping_pricelist = crm.Service.Retrieve(shipping_pricelist_ref.LogicalName, shipping_pricelist_ref.Id, new ColumnSet(true));
                        }
                        deliverynote["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                        deliverynote["bsd_customer"] = new EntityReference("account", Guid.Parse(fetchAccount));

                        var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
                        fetchxmm += "<entity name='account'>";
                        fetchxmm += "<all-attributes />";
                        fetchxmm += "<filter type='and'>";                      
                        fetchxmm += "<condition attribute=' accountnumber ' operator='like' value='%" + objissuenote.InvoiceAccount + "%' />";
                        fetchxmm += "</filter>";
                        fetchxmm += "</entity>";
                        fetchxmm += "</fetch>";
                        var entityCollection = crm.Service.RetrieveMultiple(new FetchExpression(fetchxmm));
                        if (entityCollection.Entities.Count() > 0)
                        {
                            Entity account = entityCollection.Entities.First();
                            if (account.HasValue("name"))
                            {
                                deliverynote["bsd_historyreceiptcustomer"] = account["name"];
                            }
                          
                        }

                        int request_deliverytype = ((OptionSetValue)request_delivery["bsd_type"]).Value;
                        deliverynote["bsd_packinglistax"] = objissuenote.PackingslIp;
                        if (request_deliverytype == 861450001)
                        {
                            deliverynote["bsd_quote"] = request_delivery["bsd_quote"];
                        }
                        else if (request_deliverytype == 861450002)
                        {
                            deliverynote["bsd_order"] = request_delivery["bsd_order"];
                        }
                        deliverynote["bsd_type"] = new OptionSetValue(request_deliverytype);
                        deliverynote["bsd_date"] = request_delivery["bsd_date"];
                        if (request_delivery.HasValue("bsd_deliverytrucktype")) deliverynote["bsd_deliverytrucktype"] = request_delivery["bsd_deliverytrucktype"];
                        if (request_delivery.HasValue("bsd_deliverytruck")) deliverynote["bsd_deliverytruck"] = request_delivery["bsd_deliverytruck"];
                        if (request_delivery.HasValue("bsd_carrierpartner")) deliverynote["bsd_carrierpartner"] = request_delivery["bsd_carrierpartner"];
                        if (request_delivery.HasValue("bsd_historycarrierpartner")) deliverynote["bsd_historycarrierpartner"] = request_delivery["bsd_historycarrierpartner"];
                        if (request_delivery.HasValue("bsd_licenseplate")) deliverynote["bsd_licenseplate"] = request_delivery["bsd_licenseplate"];
                        if (request_delivery.HasValue("bsd_driver")) deliverynote["bsd_driver"] = request_delivery["bsd_driver"];
                        if (suborder.HasValue("bsd_carrier")) deliverynote["bsd_carrier"] = suborder["bsd_carrier"];
                        deliverynote["bsd_requestedshipdate"] = deliveryplan["bsd_requestedshipdate"];
                        deliverynote["bsd_requestedreceiptdate"] = deliveryplan["bsd_requestedreceiptdate"];
                        deliverynote["bsd_shiptoaddress"] = request_delivery["bsd_shiptoaddress"];
                        deliverynote["bsd_istaketrip"] = request_delivery["bsd_istaketrip"];
                        deliverynote["bsd_site"] = request_delivery["bsd_site"];
                        deliverynote["bsd_siteaddress"] = request_delivery["bsd_siteaddress"];
                        if (request_delivery.HasValue("bsd_shippingpricelist"))
                        {
                            deliverynote["bsd_shippingpricelist"] = request_delivery["bsd_shippingpricelist"];
                        }
                        if (request_delivery.HasValue("bsd_shippingoption"))
                        {
                            deliverynote["bsd_shippingoption"] = request_delivery["bsd_shippingoption"];
                        }
                        guidDeliveryNote = crm.Service.Create(deliverynote);
                        en["bsd_deliverynote"] = new EntityReference(deliverynote.LogicalName, guidDeliveryNote);
                        guidGoodissueNote = crm.Service.Create(en);
                        crm.Service.Update(en_requestdelivery);

                        #endregion
                        if (objissuenote.GoodsIssueNoteProduct != null)
                        {
                            #region Tạo Product Line Good issuse note and Product Line Request Delivery
                            if (objissuenote.GoodsIssueNoteProduct.Count > 0)
                            {
                                Entity chldGoodIssueNote = new Entity("bsd_deliveryproductbill");
                                Entity chldDeliveryNote = new Entity("bsd_deliverynoteproduct");
                                foreach (var objGoodsIssueNoteProduct in objissuenote.GoodsIssueNoteProduct)
                                {
                                    var fetchproduct = retriveLookup("product", "productnumber", objGoodsIssueNoteProduct.productnumber, org);
                                    Entity retrieve = crm.Service.Retrieve("product", Guid.Parse(fetchproduct), new ColumnSet(true));
                                    EntityReference uom = (EntityReference)retrieve["defaultuomid"];
                                    if (!string.IsNullOrEmpty(fetchproduct))
                                    {
                                        #region tạo Good issue Note Product
                                        chldGoodIssueNote["bsd_name"] = retrieve["name"];
                                        chldGoodIssueNote["bsd_deliverybill"] = new EntityReference("bsd_deliverybill", guidGoodissueNote);
                                        chldGoodIssueNote["bsd_product"] = new EntityReference("product", Guid.Parse(fetchproduct));
                                        chldGoodIssueNote["bsd_productid"] = objGoodsIssueNoteProduct.productnumber;
                                        chldGoodIssueNote["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                                        chldGoodIssueNote["bsd_uomid"] = new EntityReference(uom.LogicalName, uom.Id);
                                        chldGoodIssueNote["bsd_quantity"] = objGoodsIssueNoteProduct.Quantity;
                                        chldGoodIssueNote["bsd_netquantity"] = objGoodsIssueNoteProduct.Quantity;
                                        //  var  fetchwarehouseentityProduct = retriveLookup("bsd_warehouseentity", "bsd_warehouseid", objGoodsIssueNoteProduct.Warehouse, org);
                                        string xmlWarehouse = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(fetchSite) + @"' />
                                                                      <condition attribute='bsd_warehouseid' operator='eq' value='" + objGoodsIssueNoteProduct.Warehouse + @"' />
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                                        var Warehouse = crm.Service.RetrieveMultiple(new FetchExpression(xmlWarehouse));
                                        if (Warehouse.Entities.Any())
                                        {
                                            var WarehouseEntity = Warehouse.Entities.First();
                                            chldGoodIssueNote["bsd_warehouse"] = new EntityReference("bsd_warehouseentity", WarehouseEntity.Id);
                                            //get request product delivery 
                                            string xmlproductrequestdilivery = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                                  <entity name='bsd_requestdeliveryproduct'>
                                                                                    <attribute name='bsd_requestdeliveryproductid' />
                                                                                    <attribute name='bsd_name' />
                                                                                    <attribute name='bsd_quantity' />
                                                                                    <attribute name='bsd_netquantity' />
                                                                                    <attribute name='bsd_remainingquantity' />
                                                                                    <order attribute='bsd_name' descending='false' />
                                                                                    <filter type='and'>
                                                                                      <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                                                      <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                                                                      <condition attribute='bsd_warehouse' operator='eq' uitype='bsd_warehouseentity' value='" + WarehouseEntity.Id + @"' />
                                                                                    </filter>
                                                                                  </entity>
                                                                                </fetch>";
                                            var requestdelivery = crm.Service.RetrieveMultiple(new FetchExpression(xmlproductrequestdilivery));
                                            if (requestdelivery.Entities.Any())
                                            {
                                                var request = requestdelivery.Entities.First();
                                                var entityupdate = new Entity(request.LogicalName, request.Id);
                                                //decimal quantity_reqpro = (decimal)request["bsd_quantity"];
                                                decimal old_netquantity = (decimal)request["bsd_netquantity"];
                                                decimal old_remainingquantity = (decimal)request["bsd_remainingquantity"];
                                                entityupdate["bsd_netquantity"] = old_netquantity + objGoodsIssueNoteProduct.Quantity;
                                                entityupdate["bsd_remainingquantity"] = old_remainingquantity - objGoodsIssueNoteProduct.Quantity;
                                                crm.Service.Update(entityupdate);

                                                #region Update Delivery Schedule Truck
                                                string xml_truck = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                                                          <entity name='bsd_deliveryplantruck'>
                                                                            <attribute name='bsd_deliveryplantruckid' />
                                                                            <attribute name='bsd_quantity' />
                                                                            <attribute name='bsd_remaininggoodsissuenotequantity' />
                                                                            <attribute name='bsd_goodsissuenotequantity' />
                                                                            <filter type='and'>
                                                                              <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                                                            </filter>
                                                                            <link-entity name='bsd_requestdeliverydeliveryplantruck' from='bsd_deliveryplantruck' to='bsd_deliveryplantruckid' alias='ae'>
                                                                              <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='af'>
                                                                                <filter type='and'>
                                                                                  <condition attribute='bsd_requestdeliveryid' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                                                </filter>
                                                                              </link-entity>
                                                                            </link-entity>
                                                                          </entity>
                                                                        </fetch>";
                                                Entity ent_truck = crm.Service.RetrieveMultiple(new FetchExpression(xml_truck)).Entities.FirstOrDefault();
                                                //decimal quantity = (decimal)ent_truck["bsd_quantity"];
                                                decimal remaininggoodsissuenotequantity = (decimal)ent_truck["bsd_remaininggoodsissuenotequantity"];
                                                decimal goodsissuenotequantity = (decimal)ent_truck["bsd_goodsissuenotequantity"];
                                                decimal new_remaininggoodsissuenotequantity = remaininggoodsissuenotequantity - objGoodsIssueNoteProduct.Quantity;//0
                                                Entity update_truck = new Entity(ent_truck.LogicalName, ent_truck.Id);
                                                update_truck["bsd_goodsissuenotequantity"] = goodsissuenotequantity + objGoodsIssueNoteProduct.Quantity;
                                                update_truck["bsd_remaininggoodsissuenotequantity"] = new_remaininggoodsissuenotequantity;
                                                crm.Service.Update(update_truck);
                                                #endregion

                                                #region Update delivery Schedule Product: error
                                                //string xml_scheduleproduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                //                                  <entity name='bsd_deliveryplanproduct'>
                                                //                                    <attribute name='bsd_deliveryplanproductid' />
                                                //                                    <attribute name='bsd_shipquantity' />
                                                //                                    <attribute name='bsd_remainingquantity' />
                                                //                                    <attribute name='bsd_remainaddtruck' />
                                                //                                    <filter type='and'>
                                                //                                      <condition attribute='bsd_deliveryplan' operator='eq' uitype='bsd_deliveryplan' value='" + rf_deliveryplan.Id + @"' />
                                                //                                      <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                                //                                    </filter>
                                                //                                  </entity>
                                                //                                </fetch>";
                                                //Entity ent_schedulepro = crm.Service.RetrieveMultiple(new FetchExpression(xml_scheduleproduct)).Entities.FirstOrDefault();
                                                //decimal schedulepro_remainqty = (decimal)ent_schedulepro["bsd_remainingquantity"];
                                                //decimal schedulepro_remainaddtruck = (decimal)ent_schedulepro["bsd_remainaddtruck"];
                                                //Entity update_schedulepro = new Entity(ent_schedulepro.LogicalName, ent_schedulepro.Id);
                                                //update_schedulepro["bsd_remainingquantity"] = schedulepro_remainqty - objGoodsIssueNoteProduct.Quantity;
                                                //update_schedulepro["bsd_remainaddtruck"] = schedulepro_remainaddtruck + new_remaininggoodsissuenotequantity;//0 + 2200
                                                //crm.Service.Update(update_schedulepro);
                                                #endregion
                                            }
                                            crm.Service.Create(chldGoodIssueNote);
                                            count_goodsissuenoteproduct++;
                                        }
                                        else
                                        {
                                            try
                                            {
                                                crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                                crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                                            }
                                            catch (Exception ex1) { }
                                            throw new Exception("Warehouse does not exist");
                                        }
                                        #endregion
                                        #region Kiểm tra đã tạo delivery note product chưa
                                        string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <all-attributes/>
                                                                <order attribute='bsd_name' descending='false' />
                                                                <filter type='and'>
                                                                  <condition attribute='statecode' operator='eq' value='0' />
                                                                  <condition attribute='bsd_deliverynote' operator='eq'  uitype='bsd_deliverynote' value='" + guidDeliveryNote + @"' />
                                                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + fetchproduct + @"' />
                                                                </filter>
                                                              </entity>
                                                            </fetch>";
                                        var lst_DeliveryNoteProduct = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                                        if (lst_DeliveryNoteProduct.Entities.Any())
                                        {
                                            #region Update Delivery Note
                                            decimal total_quantity = 0m;
                                            decimal bsd_quantity = 0m;
                                            decimal standard_quantity = 1m;
                                            Entity DeliveryNoteProduct = lst_DeliveryNoteProduct.Entities.First();
                                            standard_quantity = (Decimal)DeliveryNoteProduct["bsd_standardquantity"];
                                            total_quantity = (Decimal)DeliveryNoteProduct["bsd_totalquantity"];
                                            bsd_quantity = (Decimal)DeliveryNoteProduct["bsd_quantity"];
                                            Entity DeliveryNoteProduct_Update = new Entity(DeliveryNoteProduct.LogicalName, DeliveryNoteProduct.Id);
                                            DeliveryNoteProduct_Update["bsd_totalquantity"] = total_quantity + (objGoodsIssueNoteProduct.Quantity * standard_quantity);
                                            DeliveryNoteProduct_Update["bsd_quantity"] = bsd_quantity + objGoodsIssueNoteProduct.Quantity;
                                            crm.Service.Update(DeliveryNoteProduct_Update);
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Tạo Delivery Note Product
                                            decimal price_shipping_per_unit = 0m;
                                            #region 2. Tính vận chuyển ton
                                            if (shippingoption && request_shipping && ((OptionSetValue)shipping_pricelist["bsd_deliverymethod"]).Value == 861450000)
                                            {
                                                decimal price_shipping = 0m;
                                                // có bốc xếp
                                                if (request_porter && shipping_pricelist.HasValue("bsd_priceunitporter"))
                                                {
                                                    price_shipping = ((Money)shipping_pricelist["bsd_priceunitporter"]).Value; // Giá đã gồm bốc xếp
                                                }
                                                else
                                                {
                                                    if (shipping_pricelist.HasValue("bsd_priceofton"))
                                                    {
                                                        price_shipping = ((Money)shipping_pricelist["bsd_priceofton"]).Value; // Giá không bốc xếp
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                                            crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                                                        }
                                                        catch (Exception ex1) { }
                                                        "You must provide a value for Price Unit (Shipping Price List)".Throw();
                                                    }
                                                }

                                                EntityReference unit_shipping = (EntityReference)shipping_pricelist["bsd_unit"];
                                                decimal? factor_productunit_shippingunit = DMSService.Util.GetFactor(crm.Service, retrieve.Id, uom.Id, unit_shipping.Id);

                                                if (factor_productunit_shippingunit == null)
                                                {
                                                    try
                                                    {
                                                        crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                                        crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                                                    }
                                                    catch (Exception ex1) { }
                                                    throw new Exception("Shipping Unit Conversion has not been defined !");
                                                }
                                                if (factor_productunit_shippingunit.HasValue)
                                                {
                                                    price_shipping_per_unit = price_shipping * factor_productunit_shippingunit.Value;
                                                }
                                            }
                                            #endregion
                                            chldDeliveryNote["bsd_name"] = retrieve["name"];
                                            chldDeliveryNote["bsd_product"] = new EntityReference("product", Guid.Parse(fetchproduct));
                                            chldDeliveryNote["bsd_productid"] = objGoodsIssueNoteProduct.productnumber;
                                            chldDeliveryNote["bsd_unit"] = new EntityReference(uom.LogicalName, uom.Id);

                                            // bsd_standardquantity vinhlh 24-01-2018
                                            #region 1. Tinh quantity
                                            decimal standard_quantity = 1m;
                                            decimal total_quantity = 0m;
                                            EntityReference product_unit = new EntityReference(uom.LogicalName, uom.Id);
                                            EntityReference unit_default_ref = (EntityReference)Util.GetConfigDefault(crm.Service)["bsd_unitdefault"];
                                            decimal? factor = Util.GetFactor(crm.Service, Guid.Parse(fetchproduct), product_unit.Id, unit_default_ref.Id);
                                            if (factor.HasValue)
                                            {
                                                standard_quantity = factor.Value;
                                                total_quantity = factor.Value * objGoodsIssueNoteProduct.Quantity;
                                            }
                                            else throw new Exception("Unit Convertion not created !");
                                            #endregion
                                            // end vinhlh
                                            chldDeliveryNote["bsd_standardquantity"] = standard_quantity;
                                            chldDeliveryNote["bsd_totalquantity"] = total_quantity;
                                            chldDeliveryNote["bsd_quantity"] = objGoodsIssueNoteProduct.Quantity;
                                            chldDeliveryNote["bsd_netquantity"] = 0m;
                                            chldDeliveryNote["bsd_shippingprice"] = new Money(price_shipping_per_unit);
                                            chldDeliveryNote["bsd_shippingcosts"] = new Money(price_shipping_per_unit * objGoodsIssueNoteProduct.Quantity);
                                            chldDeliveryNote["bsd_deliverynote"] = new EntityReference("bsd_deliverynote", guidDeliveryNote);
                                            crm.Service.Create(chldDeliveryNote);
                                            count_deliverynoteproduct++;
                                            #endregion
                                        }
                                        #endregion

                                    }

                                }
                                #region Update delivery Schedule Product
                                EntityCollection etc_scheaddtruck = crm.Service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                                      <entity name='bsd_deliveryplantruck'>
                                                        <attribute name='bsd_deliveryplantruckid' />
                                                        <attribute name='bsd_deliveryplanproduct' />
                                                        <attribute name='bsd_goodsissuenotequantity' />
                                                        <attribute name='bsd_remaininggoodsissuenotequantity' />
                                                        <link-entity name='bsd_requestdeliverydeliveryplantruck' from='bsd_deliveryplantruck' to='bsd_deliveryplantruckid' alias='ae'>
                                                          <filter type='and'>
                                                            <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                          </filter>
                                                        </link-entity>
                                                      </entity>
                                                    </fetch>"));
                                foreach (var item_scheduletruck in etc_scheaddtruck.Entities)
                                {
                                    decimal goodsissuenotequantity = (decimal)item_scheduletruck["bsd_goodsissuenotequantity"];
                                    decimal remaininggoodsissuenotequantity = (decimal)item_scheduletruck["bsd_remaininggoodsissuenotequantity"];
                                    EntityCollection etc_schepro = crm.Service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='bsd_deliveryplanproduct'>
                                                <attribute name='bsd_deliveryplanproductid' />
                                                <attribute name='bsd_remainingquantity' />
                                                <attribute name='bsd_remainaddtruck' />
                                                <filter type='and'>
                                                  <condition attribute='bsd_deliveryplanproductid' operator='eq' uitype='bsd_deliveryplanproduct' value='" + ((EntityReference)item_scheduletruck["bsd_deliveryplanproduct"]).Id + @"' />
                                                </filter>
                                              </entity>
                                            </fetch>"));
                                    foreach (var item_scheplanpro in etc_schepro.Entities)
                                    {
                                        decimal old_remainingquantity = (decimal)item_scheplanpro["bsd_remainingquantity"];
                                        decimal old_remainaddruck = (decimal)item_scheplanpro["bsd_remainaddtruck"];
                                        decimal new_remainaddtruck = old_remainaddruck + remaininggoodsissuenotequantity;
                                        decimal new_remainingquantity = old_remainingquantity - goodsissuenotequantity;
                                        Entity update_schepro = new Entity(item_scheplanpro.LogicalName, item_scheplanpro.Id);
                                        update_schepro["bsd_remainingquantity"] = new_remainingquantity;
                                        update_schepro["bsd_remainaddtruck"] = new_remainaddtruck;
                                        crm.Service.Update(update_schepro);
                                    }
                                }
                                #endregion
                                #region Tính giá vận chuyển là Trip.
                                if (shippingoption && request_shipping && ((OptionSetValue)shipping_pricelist["bsd_deliverymethod"]).Value == 861450001)
                                {
                                    decimal price_shipping = 0m;
                                    // nếu porter Yes
                                    if (request_porter && shipping_pricelist.HasValue("bsd_pricetripporter"))
                                    {
                                        price_shipping = ((Money)shipping_pricelist["bsd_pricetripporter"]).Value; // Giá đã gồm bốc xếp
                                    }
                                    else
                                    {
                                        if (shipping_pricelist.HasValue("bsd_priceoftrip"))
                                        {
                                            price_shipping = ((Money)shipping_pricelist["bsd_priceoftrip"]).Value; // Giá không bốc xếp
                                        }
                                        else
                                        {

                                            try
                                            {
                                                crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                                crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                                            }
                                            catch (Exception ex1) { }
                                            "You must provide a value for Price Trip (Shipping Price List)".Throw();
                                        }
                                    }
                                    var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_deliverynoteproduct'>
                                                    <attribute name='bsd_deliverynoteproductid' />
                                                    <attribute name='bsd_totalquantity' />
                                                    <attribute name='bsd_standardquantity' />
                                                    <attribute name='bsd_quantity' />
                                                    <filter type='and'>
                                                      <condition attribute='bsd_deliverynote' operator='eq' value='" + guidDeliveryNote + @"' />
                                                    </filter>
                                                  </entity>
                                                </fetch>";
                                    EntityCollection list_product = crm.Service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
                                    decimal total_quotedetail_quantity = 0m;
                                    list_product.Entities.ToList().ForEach(x => total_quotedetail_quantity += (decimal)x["bsd_totalquantity"]);
                                    foreach (var item in list_product.Entities)
                                    {
                                        decimal item_standardquantity = (decimal)item["bsd_standardquantity"];
                                        Entity deliverynoteproduct = new Entity(item.LogicalName, item.Id);
                                        decimal shippingprice = price_shipping / total_quotedetail_quantity * item_standardquantity;
                                        decimal quantity = (decimal)item["bsd_quantity"];
                                        deliverynoteproduct["bsd_shippingprice"] = new Money(shippingprice);
                                        deliverynoteproduct["bsd_shippingcosts"] = new Money(shippingprice * quantity);
                                        crm.Service.Update(deliverynoteproduct);
                                    }
                                }
                                #endregion
                                #region Cập nhật lại tổng giá vận chuyển
                                if (request_delivery.HasValue("bsd_shippingoption") && (bool)request_delivery["bsd_shippingoption"])
                                {
                                    decimal total_shippingprice = 0m;
                                    var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <attribute name='bsd_deliverynoteproductid' />
                                                                <attribute name='bsd_shippingprice' />
                                                                <attribute name='bsd_quantity' />
                                                                <filter type='and'>
                                                                  <condition attribute='bsd_deliverynote' operator='eq' value='" + guidDeliveryNote + @"' />
                                                                </filter>
                                                              </entity>
                                                            </fetch>";
                                    EntityCollection list_product = crm.Service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
                                    foreach (var item in list_product.Entities)
                                    {
                                        decimal quantity = (decimal)item["bsd_quantity"];
                                        total_shippingprice += ((Money)item["bsd_shippingprice"]).Value * quantity;
                                    }
                                    Entity new_deliverynote = new Entity("bsd_deliverynote", guidDeliveryNote);
                                    new_deliverynote["bsd_priceshipping"] = new Money(total_shippingprice);
                                    crm.Service.Update(new_deliverynote);
                                }
                                #endregion
                                #region Cập nhật lại suborder + delivery plan : tình trạng : Đang giao
                                var deliveryplan_status = ((OptionSetValue)deliveryplan["bsd_status"]).Value;
                                var suborder_status = ((OptionSetValue)deliveryplan["bsd_status"]).Value;

                                if (deliveryplan_status == 861450000 && suborder_status == 861450000)
                                {
                                    Entity new_deliveryplan = new Entity(deliveryplan.LogicalName, deliveryplan.Id);
                                    Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
                                    new_suborder["bsd_status"] = new OptionSetValue(861450001);
                                    new_deliveryplan["bsd_status"] = new OptionSetValue(861450001);
                                    crm.Service.Update(new_deliveryplan);
                                    crm.Service.Update(new_suborder);

                                    #region Cập nhật suborder product
                                    var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_suborderproduct'>
                                                    <attribute name='bsd_suborderproductid' />
                                                    <attribute name='bsd_name' />
                                                    <attribute name='createdon' />
                                                    <order attribute='bsd_name' descending='false' />
                                                    <filter type='and'>
                                                      <condition attribute='bsd_suborder' operator='eq'  uitype='bsd_suborder' value='" + suborder.Id + @"' />
                                                    </filter>
                                                  </entity>
                                                </fetch>";
                                    EntityCollection list_suborder = crm.Service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
                                    // EntityCollection list_suborder = crm.Service.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);
                                    foreach (var suborder_product in list_suborder.Entities)
                                    {
                                        Entity n = new Entity(suborder_product.LogicalName, suborder_product.Id);
                                        n["bsd_deliverystatus"] = new OptionSetValue(861450001);
                                        crm.Service.Update(n);
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                            #endregion
                        }
                        else
                        {
                            try
                            {
                                crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                            }
                            catch (Exception ex1) { }
                            throw new Exception(objissuenote.PackingslIp + " product does not exist");
                        }
                        #endregion
                    }
                    else return objissuenote.PackingslIp + " is existed CRM";
                }
            }
            catch (Exception ex)
            {
                #region try catch
                if (guidGoodissueNote != Guid.Empty)
                {
                    CRMConnector crm = new CRMConnector();
                    crm.speceficConnectToCrm(org);
                    try
                    {
                        crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                        crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                    }
                    catch (Exception ex1) { }
                }
                return "error : " + ex.Message;
                throw;
                #endregion
            }

            #region check create goods issue note, goods issue note product, delivery note, delivery note product
            try
            {
                CRMConnector crm = new CRMConnector();
                crm.speceficConnectToCrm(org);

                try
                {
                    if (guidDeliveryNote.Equals(Guid.Empty) || guidGoodissueNote.Equals(Guid.Empty))
                    {
                        if (!guidDeliveryNote.Equals(Guid.Empty))
                        {
                            crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                        }
                        if (!guidGoodissueNote.Equals(Guid.Empty))
                        {
                            crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                        }
                    }
                    else
                    {
                        EntityCollection etc_deliverynoteproduct = crm.Service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='bsd_deliverynoteproduct'>
                                                <attribute name='bsd_deliverynoteproductid' />
                                                <filter type='and'>
                                                  <condition attribute='bsd_deliverynote' operator='eq' uiname='PGH.1804-2665' uitype='bsd_deliverynote' value='" + guidDeliveryNote + @"' />
                                                  <condition attribute='statecode' operator='eq' value='0' />
                                                </filter>
                                              </entity>
                                            </fetch>"));
                        EntityCollection etc_goodsissuenoteproduct = crm.Service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='bsd_deliveryproductbill'>
                                                <attribute name='bsd_deliveryproductbillid' />
                                                <filter type='and'>
                                                  <condition attribute='bsd_deliverybill' operator='eq' uiname='PXK.1804-2737' uitype='bsd_deliverybill' value='" + guidGoodissueNote + @"' />
                                                  <condition attribute='statecode' operator='eq' value='0' />
                                                </filter>
                                              </entity>
                                            </fetch>"));
                        if (etc_deliverynoteproduct.Entities.Count != count_deliverynoteproduct || etc_goodsissuenoteproduct.Entities.Count != count_goodsissuenoteproduct)
                        {
                            crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                            crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                        }
                    }
                }
                catch (Exception ex1) { }
            }
            catch (Exception ex)
            {
                return "error : " + ex.Message;
                throw;
            }
            #endregion
            return "succces";
        }
        //vinhlh 12-22-2017 Move out AX suborder kí gửi
        internal static string insertGoodIssueNoteConsigment(GoodsIssueNote objissuenote, string org)
        {
            var guidGoodissueNote = new Guid();
            var guidDeliveryNote = new Guid();
            int count_goodsissuenoteproduct = 0;
            int count_deliverynoteproduct = 0;
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                Entity suborder = null;
                Entity deliveryplan = null;
                Entity deliverynote = null;
                Entity en_requestdelivery = null;
                var flag = false;
                string requestdeliveryid = "";
                if (objissuenote != null)
                {

                    flag = isvalidfileld("bsd_deliverybill", "bsd_issuenoteax", objissuenote.PackingslIp, org);
                    if (flag == false)
                    {
                        #region Sales Order tạo Good Issues Note and Cập nhật Request Delivery and Tạo Delivery Note
                        Entity request_delivery = null;
                        #region Tạo mới Good issuse Note
                        Entity en = new Entity("bsd_deliverybill", guidGoodissueNote);
                        Guid fetchAccount = Guid.Empty; //= retriveLookup("account", "accountnumber", objissuenote.InvoiceAccount, org);
                        var fetchSite = retriveLookup("bsd_site", "bsd_code", objissuenote.Site, org);
                        // var fetchwarehouseentity = retriveLookup("bsd_warehouseentity", "bsd_warehouseid", objissuenote.Warehouse, org);
                        //if (!string.IsNullOrEmpty(fetchAccount))
                        //{
                        //    en["bsd_customer"] = new EntityReference("account", Guid.Parse(fetchAccount));
                        //}
                        #region
                        if (!string.IsNullOrEmpty(fetchSite))
                        {
                            en["bsd_site"] = new EntityReference("bsd_site", Guid.Parse(fetchSite));
                            string xmlWarehouseGood = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(fetchSite) + @"' />
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                            var WarehouseGood = crm.Service.RetrieveMultiple(new FetchExpression(xmlWarehouseGood));
                            if (WarehouseGood.Entities.Any())
                            {

                                en["bsd_warehouse"] = new EntityReference("bsd_warehouseentity", WarehouseGood.Entities.First().Id);

                            }
                            else
                            {
                                return "Warehouse does not exist";
                            }
                        }
                        else
                        {
                            return "Site does not exist";
                        }
                        #endregion

                        if (objissuenote.PackingslIp != null)
                        {
                            en["bsd_issuenoteax"] = objissuenote.PackingslIp;

                        }
                        if (objissuenote.issuenoteax != null)
                        {
                            #region Cập nhật lại Request Delivery
                            requestdeliveryid = retrivestringvaluelookuplike("bsd_requestdelivery", "bsd_pickinglistax", objissuenote.issuenoteax.Trim(), org);
                            if (requestdeliveryid != null)
                            {
                                request_delivery = crm.Service.Retrieve("bsd_requestdelivery", Guid.Parse(requestdeliveryid), new ColumnSet(true));
                                en_requestdelivery = new Entity(request_delivery.LogicalName, request_delivery.Id);
                                en_requestdelivery["bsd_createddeliverybill"] = true;
                                en_requestdelivery["bsd_createddeliverynote"] = true;

                                en["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                                en["bsd_shiptoaddress"] = (EntityReference)request_delivery["bsd_shiptoaddress"];
                                en["bsd_deliveryplan"] = (EntityReference)request_delivery["bsd_deliveryplan"];
                                deliveryplan = crm.Service.Retrieve(((EntityReference)request_delivery["bsd_deliveryplan"]).LogicalName, ((EntityReference)request_delivery["bsd_deliveryplan"]).Id, new ColumnSet(true));
                                en["bsd_requestedshipdate"] = deliveryplan["bsd_requestedshipdate"];
                                en["bsd_requestedreceiptdate"] = deliveryplan["bsd_requestedreceiptdate"];
                                en["bsd_customer"] = (EntityReference)deliveryplan["bsd_potentialcustomer"];
                                fetchAccount = ((EntityReference)deliveryplan["bsd_potentialcustomer"]).Id;
                                EntityReference suborder_Rf = (EntityReference)deliveryplan["bsd_suborder"];
                                suborder = crm.Service.Retrieve(suborder_Rf.LogicalName, suborder_Rf.Id, new ColumnSet(true));


                            }
                            else
                            {
                                return "Packing Slip not remember in crm";
                            }
                            #endregion
                        }
                        en["bsd_createddeliverynote"] = true;

                        #endregion
                        #region tạo delivery Note
                        EntityReference rf_deliveryplan = (EntityReference)request_delivery["bsd_deliveryplan"];
                        deliveryplan = crm.Service.Retrieve(rf_deliveryplan.LogicalName, rf_deliveryplan.Id, new ColumnSet(true));
                        deliverynote = new Entity("bsd_deliverynote", guidDeliveryNote);
                        bool request_porter = (bool)suborder["bsd_requestporter"];
                        bool request_shipping = (bool)suborder["bsd_transportation"];
                        bool shippingoption = request_delivery.HasValue("bsd_shippingoption") ? (bool)request_delivery["bsd_shippingoption"] : false;
                        Entity shipping_pricelist = null;

                        decimal total_shipping_price = 0m;

                        if (shippingoption)
                        {
                            EntityReference shipping_pricelist_ref = (EntityReference)request_delivery["bsd_shippingpricelist"];
                            shipping_pricelist = crm.Service.Retrieve(shipping_pricelist_ref.LogicalName, shipping_pricelist_ref.Id, new ColumnSet(true));
                        }
                        deliverynote["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                        deliverynote["bsd_customer"] = new EntityReference("account", fetchAccount);
                        int request_deliverytype = ((OptionSetValue)request_delivery["bsd_type"]).Value;
                        deliverynote["bsd_packinglistax"] = objissuenote.PackingslIp;
                        #region get Invoice in Suborder 08-03-2018 vinhlh 
                        string xmlInvoiceSuborder = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_invoiceax'>
                                            <all-attributes />
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='" + suborder.Id + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";
                        var lstInvoiceSuborder = crm.Service.RetrieveMultiple(new FetchExpression(xmlInvoiceSuborder));
                        if (lstInvoiceSuborder.Entities.Any())
                        {
                            var InvoiceSuborder = lstInvoiceSuborder.Entities.First();
                            deliverynote["bsd_invoicenumberax"] = new EntityReference(InvoiceSuborder.LogicalName, InvoiceSuborder.Id);
                            if (InvoiceSuborder.HasValue("bsd_invoicedate"))
                                deliverynote["bsd_invoicedate"] = InvoiceSuborder["bsd_invoicedate"];
                            deliverynote["bsd_invoiceno"] = InvoiceSuborder["bsd_name"];
                        }
                        #endregion end get Invoice in Suborder 08-03-2018 vinhlh 
                        if (request_deliverytype == 861450001)
                        {
                            deliverynote["bsd_quote"] = request_delivery["bsd_quote"];
                        }
                        else if (request_deliverytype == 861450002)
                        {
                            deliverynote["bsd_order"] = request_delivery["bsd_order"];
                        }
                        deliverynote["bsd_type"] = new OptionSetValue(request_deliverytype);
                        deliverynote["bsd_date"] = request_delivery["bsd_date"];
                        if (request_delivery.HasValue("bsd_deliverytrucktype")) deliverynote["bsd_deliverytrucktype"] = request_delivery["bsd_deliverytrucktype"];
                        if (request_delivery.HasValue("bsd_deliverytruck")) deliverynote["bsd_deliverytruck"] = request_delivery["bsd_deliverytruck"];
                        if (request_delivery.HasValue("bsd_carrierpartner")) deliverynote["bsd_carrierpartner"] = request_delivery["bsd_carrierpartner"];
                        if (request_delivery.HasValue("bsd_licenseplate")) deliverynote["bsd_licenseplate"] = request_delivery["bsd_licenseplate"];
                        if (request_delivery.HasValue("bsd_driver")) deliverynote["bsd_driver"] = request_delivery["bsd_driver"];
                        if (suborder.HasValue("bsd_carrier")) deliverynote["bsd_carrier"] = suborder["bsd_carrier"];
                        deliverynote["bsd_requestedshipdate"] = deliveryplan["bsd_requestedshipdate"];
                        deliverynote["bsd_requestedreceiptdate"] = deliveryplan["bsd_requestedreceiptdate"];
                        deliverynote["bsd_shiptoaddress"] = request_delivery["bsd_shiptoaddress"];
                        deliverynote["bsd_istaketrip"] = request_delivery["bsd_istaketrip"];
                        deliverynote["bsd_site"] = request_delivery["bsd_site"];
                        deliverynote["bsd_siteaddress"] = request_delivery["bsd_siteaddress"];
                        if (request_delivery.HasValue("bsd_shippingpricelist"))
                        {
                            deliverynote["bsd_shippingpricelist"] = request_delivery["bsd_shippingpricelist"];
                        }
                        if (request_delivery.HasValue("bsd_shippingoption"))
                        {
                            deliverynote["bsd_shippingoption"] = request_delivery["bsd_shippingoption"];
                        }
                        guidDeliveryNote = crm.Service.Create(deliverynote);
                        en["bsd_deliverynote"] = new EntityReference(deliverynote.LogicalName, guidDeliveryNote);
                        guidGoodissueNote = crm.Service.Create(en);
                        crm.Service.Update(en_requestdelivery);
                        #endregion
                        if (objissuenote.GoodsIssueNoteProduct != null)
                        {
                            #region Tạo Product Line Good issuse note and Product Line Request Delivery
                            if (objissuenote.GoodsIssueNoteProduct.Count > 0)
                            {
                                Entity chldGoodIssueNote = new Entity("bsd_deliveryproductbill");
                                Entity chldDeliveryNote = new Entity("bsd_deliverynoteproduct");
                                foreach (var objGoodsIssueNoteProduct in objissuenote.GoodsIssueNoteProduct)
                                {
                                    var fetchproduct = retriveLookup("product", "productnumber", objGoodsIssueNoteProduct.productnumber, org);
                                    Entity retrieve = crm.Service.Retrieve("product", Guid.Parse(fetchproduct), new ColumnSet(true));
                                    EntityReference uom = (EntityReference)retrieve["defaultuomid"];
                                    if (!string.IsNullOrEmpty(fetchproduct))
                                    {
                                        #region tạo Good issue Note Product
                                        chldGoodIssueNote["bsd_name"] = retrieve["name"];
                                        chldGoodIssueNote["bsd_deliverybill"] = new EntityReference("bsd_deliverybill", guidGoodissueNote);
                                        chldGoodIssueNote["bsd_product"] = new EntityReference("product", Guid.Parse(fetchproduct));
                                        chldGoodIssueNote["bsd_productid"] = objGoodsIssueNoteProduct.productnumber;
                                        chldGoodIssueNote["bsd_requestdelivery"] = new EntityReference(request_delivery.LogicalName, request_delivery.Id);
                                        chldGoodIssueNote["bsd_uomid"] = new EntityReference(uom.LogicalName, uom.Id);
                                        chldGoodIssueNote["bsd_quantity"] = Math.Abs(objGoodsIssueNoteProduct.Quantity);
                                        chldGoodIssueNote["bsd_netquantity"] = Math.Abs(objGoodsIssueNoteProduct.Quantity);
                                        //  var  fetchwarehouseentityProduct = retriveLookup("bsd_warehouseentity", "bsd_warehouseid", objGoodsIssueNoteProduct.Warehouse, org);
                                        // <condition attribute='bsd_warehouseid' operator='eq' value='" + objGoodsIssueNoteProduct.Warehouse + @"' />
                                        string xmlWarehouse = "";
                                        if (objGoodsIssueNoteProduct.Warehouse != null)
                                        {
                                            #region 
                                            xmlWarehouse = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(fetchSite) + @"' />
                                                                       <condition attribute='bsd_warehouseid' operator='eq' value='" + objGoodsIssueNoteProduct.Warehouse.Trim() + @"' /> 
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                                            #endregion
                                        }
                                        else
                                        {
                                            #region
                                            xmlWarehouse = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(fetchSite) + @"' />
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                                            #endregion
                                        }
                                        var Warehouse = crm.Service.RetrieveMultiple(new FetchExpression(xmlWarehouse));
                                        if (Warehouse.Entities.Any())
                                        {
                                            var WarehouseEntity = Warehouse.Entities.First();
                                            chldGoodIssueNote["bsd_warehouse"] = new EntityReference("bsd_warehouseentity", WarehouseEntity.Id);
                                            //get request product delivery 

                                            string xmlproductrequestdilivery = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                                  <entity name='bsd_requestdeliveryproduct'>
                                                                                    <attribute name='bsd_requestdeliveryproductid' />
                                                                                    <attribute name='bsd_name' />
                                                                                    <attribute name='bsd_quantity' />
                                                                                    <attribute name='bsd_netquantity' />
                                                                                    <attribute name='bsd_remainingquantity' />
                                                                                    <order attribute='bsd_name' descending='false' />
                                                                                    <filter type='and'>
                                                                                      <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                                                      <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                                                                      <condition attribute='bsd_warehouseconsignment' operator='eq' uitype='bsd_warehouseentity' value='" + WarehouseEntity.Id + @"' />
                                                                                    </filter>
                                                                                  </entity>
                                                                                </fetch>";
                                            var requestdelivery = crm.Service.RetrieveMultiple(new FetchExpression(xmlproductrequestdilivery));
                                            if (requestdelivery.Entities.Any())
                                            {

                                                var request = requestdelivery.Entities.First();
                                                var entityupdate = new Entity(request.LogicalName, request.Id);
                                                //decimal quantity_reqpro = (decimal)request["bsd_quantity"];
                                                decimal old_netquantity = (decimal)request["bsd_netquantity"];
                                                decimal old_remainingquantity = (decimal)request["bsd_remainingquantity"];
                                                entityupdate["bsd_netquantity"] = old_netquantity + objGoodsIssueNoteProduct.Quantity;
                                                entityupdate["bsd_remainingquantity"] = old_remainingquantity - objGoodsIssueNoteProduct.Quantity;
                                                crm.Service.Update(entityupdate);

                                                #region Update Delivery Schedule Truck
                                                string xml_truck = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                                                          <entity name='bsd_deliveryplantruck'>
                                                                            <attribute name='bsd_deliveryplantruckid' />
                                                                            <attribute name='bsd_quantity' />
                                                                            <attribute name='bsd_remaininggoodsissuenotequantity' />
                                                                            <attribute name='bsd_goodsissuenotequantity' />
                                                                            <filter type='and'>
                                                                              <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                                                            </filter>
                                                                            <link-entity name='bsd_requestdeliverydeliveryplantruck' from='bsd_deliveryplantruck' to='bsd_deliveryplantruckid' alias='ae'>
                                                                              <link-entity name='bsd_requestdelivery' from='bsd_requestdeliveryid' to='bsd_requestdelivery' alias='af'>
                                                                                <filter type='and'>
                                                                                  <condition attribute='bsd_requestdeliveryid' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                                                </filter>
                                                                              </link-entity>
                                                                            </link-entity>
                                                                          </entity>
                                                                        </fetch>";
                                                Entity ent_truck = crm.Service.RetrieveMultiple(new FetchExpression(xml_truck)).Entities.FirstOrDefault();
                                                //decimal quantity = (decimal)ent_truck["bsd_quantity"];
                                                decimal remaininggoodsissuenotequantity = (decimal)ent_truck["bsd_remaininggoodsissuenotequantity"];
                                                decimal goodsissuenotequantity = (decimal)ent_truck["bsd_goodsissuenotequantity"];
                                                decimal new_remaininggoodsissuenotequantity = remaininggoodsissuenotequantity - objGoodsIssueNoteProduct.Quantity;
                                                Entity update_truck = new Entity(ent_truck.LogicalName, ent_truck.Id);
                                                update_truck["bsd_goodsissuenotequantity"] = goodsissuenotequantity + objGoodsIssueNoteProduct.Quantity;
                                                update_truck["bsd_remaininggoodsissuenotequantity"] = new_remaininggoodsissuenotequantity;
                                                crm.Service.Update(update_truck);
                                                #endregion

                                                #region Update delivery Schedule Product
                                                //string xml_scheduleproduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                //                                  <entity name='bsd_deliveryplanproduct'>
                                                //                                    <attribute name='bsd_deliveryplanproductid' />
                                                //                                    <attribute name='bsd_shipquantity' />
                                                //                                    <attribute name='bsd_remainingquantity' />
                                                //                                    <attribute name='bsd_remainaddtruck' />
                                                //                                    <filter type='and'>
                                                //                                      <condition attribute='bsd_deliveryplan' operator='eq' uitype='bsd_deliveryplan' value='" + rf_deliveryplan.Id + @"' />
                                                //                                      <condition attribute='bsd_productid' operator='eq' value='" + objGoodsIssueNoteProduct.productnumber.Trim() + @"' />
                                                //                                    </filter>
                                                //                                  </entity>
                                                //                                </fetch>";
                                                //Entity ent_schedulepro = crm.Service.RetrieveMultiple(new FetchExpression(xml_scheduleproduct)).Entities.FirstOrDefault();
                                                //decimal schedulepro_remainqty = (decimal)ent_schedulepro["bsd_remainingquantity"];
                                                //decimal schedulepro_remainaddtruck = (decimal)ent_schedulepro["bsd_remainaddtruck"];
                                                //Entity update_schedulepro = new Entity(ent_schedulepro.LogicalName, ent_schedulepro.Id);
                                                //update_schedulepro["bsd_remainingquantity"] = schedulepro_remainqty - objGoodsIssueNoteProduct.Quantity;
                                                //update_schedulepro["bsd_remainaddtruck"] = schedulepro_remainaddtruck + new_remaininggoodsissuenotequantity;
                                                //crm.Service.Update(update_schedulepro);
                                                #endregion

                                            }
                                            crm.Service.Create(chldGoodIssueNote);
                                            count_goodsissuenoteproduct++;
                                        }
                                        else
                                        {
                                            try
                                            {
                                                crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                                crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                                            }
                                            catch (Exception ex1) { }
                                            throw new Exception("Warehouse does not exist");
                                        }

                                        #endregion
                                        #region Kiểm tra đã tạo delivery note product chưa
                                        string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <all-attributes/>
                                                                <order attribute='bsd_name' descending='false' />
                                                                <filter type='and'>
                                                                  <condition attribute='statecode' operator='eq' value='0' />
                                                                  <condition attribute='bsd_deliverynote' operator='eq'  uitype='bsd_deliverynote' value='" + guidDeliveryNote + @"' />
                                                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + fetchproduct + @"' />
                                                                </filter>
                                                              </entity>
                                                            </fetch>";
                                        var lst_DeliveryNoteProduct = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                                        if (lst_DeliveryNoteProduct.Entities.Any())
                                        {
                                            #region Update Delivery Note
                                            decimal total_quantity = 0m;
                                            decimal bsd_quantity = 0m;
                                            decimal standard_quantity = 1m;
                                            Entity DeliveryNoteProduct = lst_DeliveryNoteProduct.Entities.First();
                                            standard_quantity = (Decimal)DeliveryNoteProduct["bsd_standardquantity"];
                                            total_quantity = (Decimal)DeliveryNoteProduct["bsd_totalquantity"];
                                            bsd_quantity = (Decimal)DeliveryNoteProduct["bsd_quantity"];
                                            Entity DeliveryNoteProduct_Update = new Entity(DeliveryNoteProduct.LogicalName, DeliveryNoteProduct.Id);
                                            DeliveryNoteProduct_Update["bsd_totalquantity"] = total_quantity + (objGoodsIssueNoteProduct.Quantity * standard_quantity);
                                            DeliveryNoteProduct_Update["bsd_quantity"] = bsd_quantity + objGoodsIssueNoteProduct.Quantity;
                                            crm.Service.Update(DeliveryNoteProduct_Update);
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Tạo Delivery Note Product
                                            decimal price_shipping_per_unit = 0m;
                                            #region 2. Tính vận chuyển ton
                                            if (shippingoption && request_shipping && ((OptionSetValue)shipping_pricelist["bsd_deliverymethod"]).Value == 861450000)
                                            {
                                                decimal price_shipping = 0m;
                                                // có bốc xếp
                                                if (request_porter && shipping_pricelist.HasValue("bsd_priceunitporter"))
                                                {
                                                    price_shipping = ((Money)shipping_pricelist["bsd_priceunitporter"]).Value; // Giá đã gồm bốc xếp
                                                }
                                                else
                                                {
                                                    if (shipping_pricelist.HasValue("bsd_priceofton"))
                                                    {
                                                        price_shipping = ((Money)shipping_pricelist["bsd_priceofton"]).Value; // Giá không bốc xếp
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                                            crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                                                        }
                                                        catch (Exception ex1) { }
                                                        "You must provide a value for Price Unit (Shipping Price List)".Throw();
                                                    }
                                                }

                                                EntityReference unit_shipping = (EntityReference)shipping_pricelist["bsd_unit"];
                                                decimal? factor_productunit_shippingunit = DMSService.Util.GetFactor(crm.Service, retrieve.Id, uom.Id, unit_shipping.Id);

                                                if (factor_productunit_shippingunit == null)
                                                {
                                                    try
                                                    {
                                                        crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                                        crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                                                    }
                                                    catch (Exception ex1) { }
                                                    throw new Exception("Shipping Unit Conversion has not been defined !");
                                                }
                                                if (factor_productunit_shippingunit.HasValue)
                                                {
                                                    price_shipping_per_unit = price_shipping * factor_productunit_shippingunit.Value;
                                                }
                                            }
                                            #endregion
                                            chldDeliveryNote["bsd_name"] = retrieve["name"];
                                            chldDeliveryNote["bsd_product"] = new EntityReference("product", Guid.Parse(fetchproduct));
                                            chldDeliveryNote["bsd_productid"] = objGoodsIssueNoteProduct.productnumber;
                                            chldDeliveryNote["bsd_unit"] = new EntityReference(uom.LogicalName, uom.Id);

                                            // bsd_standardquantity vinhlh 24-01-2018
                                            #region 1. Tinh quantity
                                            decimal standard_quantity = 1m;
                                            decimal total_quantity = 0m;
                                            EntityReference product_unit = new EntityReference(uom.LogicalName, uom.Id);
                                            EntityReference unit_default_ref = (EntityReference)Util.GetConfigDefault(crm.Service)["bsd_unitdefault"];
                                            decimal? factor = Util.GetFactor(crm.Service, Guid.Parse(fetchproduct), product_unit.Id, unit_default_ref.Id);
                                            if (factor.HasValue)
                                            {
                                                standard_quantity = factor.Value;
                                                total_quantity = factor.Value * objGoodsIssueNoteProduct.Quantity;
                                            }
                                            else throw new Exception("Unit Convertion not created !");
                                            #endregion
                                            // end vinhlh
                                            chldDeliveryNote["bsd_standardquantity"] = standard_quantity;
                                            chldDeliveryNote["bsd_totalquantity"] = total_quantity;
                                            chldDeliveryNote["bsd_quantity"] = objGoodsIssueNoteProduct.Quantity;
                                            chldDeliveryNote["bsd_netquantity"] = 0m;
                                            chldDeliveryNote["bsd_shippingprice"] = new Money(price_shipping_per_unit);
                                            chldDeliveryNote["bsd_shippingcosts"] = new Money(price_shipping_per_unit * objGoodsIssueNoteProduct.Quantity);
                                            chldDeliveryNote["bsd_deliverynote"] = new EntityReference("bsd_deliverynote", guidDeliveryNote);
                                            crm.Service.Create(chldDeliveryNote);
                                            count_deliverynoteproduct++;
                                            #endregion
                                        }
                                        #endregion
                                    }

                                }
                                #region Update delivery Schedule Product
                                EntityCollection etc_scheaddtruck = crm.Service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                                      <entity name='bsd_deliveryplantruck'>
                                                        <attribute name='bsd_deliveryplantruckid' />
                                                        <attribute name='bsd_deliveryplanproduct' />
                                                        <attribute name='bsd_goodsissuenotequantity' />
                                                        <attribute name='bsd_remaininggoodsissuenotequantity' />
                                                        <link-entity name='bsd_requestdeliverydeliveryplantruck' from='bsd_deliveryplantruck' to='bsd_deliveryplantruckid' alias='ae'>
                                                          <filter type='and'>
                                                            <condition attribute='bsd_requestdelivery' operator='eq' uitype='bsd_requestdelivery' value='" + Guid.Parse(requestdeliveryid) + @"' />
                                                          </filter>
                                                        </link-entity>
                                                      </entity>
                                                    </fetch>"));
                                foreach (var item_scheduletruck in etc_scheaddtruck.Entities)
                                {
                                    decimal goodsissuenotequantity = (decimal)item_scheduletruck["bsd_goodsissuenotequantity"];
                                    decimal remaininggoodsissuenotequantity = (decimal)item_scheduletruck["bsd_remaininggoodsissuenotequantity"];
                                    EntityCollection etc_schepro = crm.Service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='bsd_deliveryplanproduct'>
                                                <attribute name='bsd_deliveryplanproductid' />
                                                <attribute name='bsd_remainingquantity' />
                                                <attribute name='bsd_remainaddtruck' />
                                                <filter type='and'>
                                                  <condition attribute='bsd_deliveryplanproductid' operator='eq' uitype='bsd_deliveryplanproduct' value='" + ((EntityReference)item_scheduletruck["bsd_deliveryplanproduct"]).Id + @"' />
                                                </filter>
                                              </entity>
                                            </fetch>"));
                                    foreach (var item_scheplanpro in etc_schepro.Entities)
                                    {
                                        decimal old_remainingquantity = (decimal)item_scheplanpro["bsd_remainingquantity"];
                                        decimal old_remainaddruck = (decimal)item_scheplanpro["bsd_remainaddtruck"];
                                        decimal new_remainaddtruck = old_remainaddruck + remaininggoodsissuenotequantity;
                                        decimal new_remainingquantity = old_remainingquantity - goodsissuenotequantity;
                                        Entity update_schepro = new Entity(item_scheplanpro.LogicalName, item_scheplanpro.Id);
                                        update_schepro["bsd_remainingquantity"] = new_remainingquantity;
                                        update_schepro["bsd_remainaddtruck"] = new_remainaddtruck;
                                        crm.Service.Update(update_schepro);
                                    }
                                }
                                #endregion

                                #region Tính giá vận chuyển là Trip.
                                if (shippingoption && request_shipping && ((OptionSetValue)shipping_pricelist["bsd_deliverymethod"]).Value == 861450001)
                                {
                                    decimal price_shipping = 0m;
                                    // nếu porter Yes
                                    if (request_porter && shipping_pricelist.HasValue("bsd_pricetripporter"))
                                    {
                                        price_shipping = ((Money)shipping_pricelist["bsd_pricetripporter"]).Value; // Giá đã gồm bốc xếp
                                    }
                                    else
                                    {
                                        if (shipping_pricelist.HasValue("bsd_priceoftrip"))
                                        {
                                            price_shipping = ((Money)shipping_pricelist["bsd_priceoftrip"]).Value; // Giá không bốc xếp
                                        }
                                        else
                                        {

                                            try
                                            {
                                                crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                                crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                                            }
                                            catch (Exception ex1) { }
                                            "You must provide a value for Price Trip (Shipping Price List)".Throw();
                                        }
                                    }
                                    var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_deliverynoteproduct'>
                                                    <attribute name='bsd_deliverynoteproductid' />
                                                    <attribute name='bsd_totalquantity' />
                                                    <attribute name='bsd_standardquantity' />
                                                    <attribute name='bsd_quantity' />
                                                    <filter type='and'>
                                                      <condition attribute='bsd_deliverynote' operator='eq' value='" + guidDeliveryNote + @"' />
                                                    </filter>
                                                  </entity>
                                                </fetch>";
                                    EntityCollection list_product = crm.Service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
                                    decimal total_quotedetail_quantity = 0m;
                                    list_product.Entities.ToList().ForEach(x => total_quotedetail_quantity += (decimal)x["bsd_totalquantity"]);
                                    foreach (var item in list_product.Entities)
                                    {
                                        decimal item_standardquantity = (decimal)item["bsd_standardquantity"];
                                        Entity deliverynoteproduct = new Entity(item.LogicalName, item.Id);
                                        decimal shippingprice = price_shipping / total_quotedetail_quantity * item_standardquantity;
                                        decimal quantity = (decimal)item["bsd_quantity"];
                                        deliverynoteproduct["bsd_shippingprice"] = new Money(shippingprice);
                                        deliverynoteproduct["bsd_shippingcosts"] = new Money(shippingprice * quantity);
                                        crm.Service.Update(deliverynoteproduct);
                                    }
                                }
                                #endregion
                                #region Cập nhật lại tổng giá vận chuyển
                                if (request_delivery.HasValue("bsd_shippingoption") && (bool)request_delivery["bsd_shippingoption"])
                                {
                                    decimal total_shippingprice = 0m;
                                    var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                              <entity name='bsd_deliverynoteproduct'>
                                                                <attribute name='bsd_deliverynoteproductid' />
                                                                <attribute name='bsd_shippingprice' />
                                                                <attribute name='bsd_quantity' />
                                                                <filter type='and'>
                                                                  <condition attribute='bsd_deliverynote' operator='eq' value='" + guidDeliveryNote + @"' />
                                                                </filter>
                                                              </entity>
                                                            </fetch>";
                                    EntityCollection list_product = crm.Service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
                                    foreach (var item in list_product.Entities)
                                    {
                                        decimal quantity = (decimal)item["bsd_quantity"];
                                        total_shippingprice += ((Money)item["bsd_shippingprice"]).Value * quantity;
                                    }
                                    Entity new_deliverynote = new Entity("bsd_deliverynote", guidDeliveryNote);
                                    new_deliverynote["bsd_priceshipping"] = new Money(total_shippingprice);
                                    crm.Service.Update(new_deliverynote);
                                }
                                #endregion
                                #region Cập nhật lại suborder + delivery plan : tình trạng : Đang giao
                                var deliveryplan_status = ((OptionSetValue)deliveryplan["bsd_status"]).Value;
                                var suborder_status = ((OptionSetValue)deliveryplan["bsd_status"]).Value;

                                if (deliveryplan_status == 861450000 && suborder_status == 861450000)
                                {
                                    Entity new_deliveryplan = new Entity(deliveryplan.LogicalName, deliveryplan.Id);
                                    Entity new_suborder = new Entity(suborder.LogicalName, suborder.Id);
                                    new_suborder["bsd_status"] = new OptionSetValue(861450001);
                                    new_deliveryplan["bsd_status"] = new OptionSetValue(861450001);
                                    crm.Service.Update(new_deliveryplan);
                                    crm.Service.Update(new_suborder);

                                    #region Cập nhật suborder product
                                    var fetchDeliveryProduct = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='bsd_suborderproduct'>
                                                    <attribute name='bsd_suborderproductid' />
                                                    <attribute name='bsd_name' />
                                                    <attribute name='createdon' />
                                                    <order attribute='bsd_name' descending='false' />
                                                    <filter type='and'>
                                                      <condition attribute='bsd_suborder' operator='eq'  uitype='bsd_suborder' value='" + suborder.Id + @"' />
                                                    </filter>
                                                  </entity>
                                                </fetch>";
                                    EntityCollection list_suborder = crm.Service.RetrieveMultiple(new FetchExpression(fetchDeliveryProduct));
                                    // EntityCollection list_suborder = crm.Service.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborder.Id);
                                    foreach (var suborder_product in list_suborder.Entities)
                                    {
                                        Entity n = new Entity(suborder_product.LogicalName, suborder_product.Id);
                                        n["bsd_deliverystatus"] = new OptionSetValue(861450001);
                                        crm.Service.Update(n);
                                    }
                                    #endregion
                                }
                                #endregion
                            }
                            #endregion
                        }
                        else
                        {
                            try
                            {
                                crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                                crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                            }
                            catch (Exception ex1) { }
                            throw new Exception(objissuenote.PackingslIp + " product does not exist");
                        }
                        #endregion
                    }
                    else return objissuenote.PackingslIp + " is existed CRM";
                }
            }
            catch (Exception ex)
            {
                #region try catch
                if (guidGoodissueNote != Guid.Empty)
                {
                    CRMConnector crm = new CRMConnector();
                    crm.speceficConnectToCrm(org);
                    try
                    {
                        crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                        crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                    }
                    catch (Exception ex1) { }
                }
                return "error : " + ex.Message;
                throw;
                #endregion
            }

            #region check create goods issue note, goods issue note product, delivery note, delivery note product
            try
            {
                CRMConnector crm = new CRMConnector();
                crm.speceficConnectToCrm(org);

                try
                {
                    if (guidDeliveryNote.Equals(Guid.Empty) || guidGoodissueNote.Equals(Guid.Empty))
                    {
                        if (!guidDeliveryNote.Equals(Guid.Empty))
                        {
                            crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                        }
                        if (!guidGoodissueNote.Equals(Guid.Empty))
                        {
                            crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                        }
                    }
                    else
                    {
                        EntityCollection etc_deliverynoteproduct = crm.Service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='bsd_deliverynoteproduct'>
                                                <attribute name='bsd_deliverynoteproductid' />
                                                <filter type='and'>
                                                  <condition attribute='bsd_deliverynote' operator='eq' uiname='PGH.1804-2665' uitype='bsd_deliverynote' value='" + guidDeliveryNote + @"' />
                                                  <condition attribute='statecode' operator='eq' value='0' />
                                                </filter>
                                              </entity>
                                            </fetch>"));
                        EntityCollection etc_goodsissuenoteproduct = crm.Service.RetrieveMultiple(new FetchExpression(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                              <entity name='bsd_deliveryproductbill'>
                                                <attribute name='bsd_deliveryproductbillid' />
                                                <filter type='and'>
                                                  <condition attribute='bsd_deliverybill' operator='eq' uiname='PXK.1804-2737' uitype='bsd_deliverybill' value='" + guidGoodissueNote + @"' />
                                                  <condition attribute='statecode' operator='eq' value='0' />
                                                </filter>
                                              </entity>
                                            </fetch>"));
                        if (etc_deliverynoteproduct.Entities.Count != count_deliverynoteproduct || etc_goodsissuenoteproduct.Entities.Count != count_goodsissuenoteproduct)
                        {
                            crm.Service.Delete("bsd_deliverynote", guidDeliveryNote);
                            crm.Service.Delete("bsd_deliverybill", guidGoodissueNote);
                        }
                    }
                }
                catch (Exception ex1) { }
            }
            catch (Exception ex)
            {
                return "error : " + ex.Message;
                throw;
            }
            #endregion

            return "succces";
        }

        //end vinhlh 22-12-2017
        internal static bool CancelPickingList(string pickingListID, string org)
        {
            try
            {
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (pickingListID != null)
                {

                    string id = retriveLookup("bsd_requestdelivery", "bsd_pickinglistax", pickingListID.Trim(), org);
                    if (id != null)
                    {
                        // throw new Exception("okid");
                        Entity requestdelivery = crm.Service.Retrieve("bsd_requestdelivery", Guid.Parse(id), new ColumnSet(true));
                        Entity requestdelivery_Update = new Entity("bsd_requestdelivery", Guid.Parse(id));
                        requestdelivery_Update["bsd_description"] = "Không đủ hàng xuất";
                        SetStateRequest setStateRequest = new SetStateRequest()
                        {
                            EntityMoniker = new EntityReference
                            {
                                Id = requestdelivery_Update.Id,
                                LogicalName = requestdelivery_Update.LogicalName
                            },
                            State = new OptionSetValue(1),
                            Status = new OptionSetValue(2)
                        };
                        crm.Service.Execute(setStateRequest);
                        crm.Service.Update(requestdelivery_Update);
                        #region

                        string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_requestdeliverydeliveryplantruck'>
                                            <attribute name='bsd_requestdeliverydeliveryplantruckid' />
                                            <attribute name='bsd_name' />
                                            <attribute name='createdon' />
                                            <attribute name='bsd_deliveryplantruck' />
                                            <attribute name='createdonbehalfby' />
                                            <attribute name='createdby' />
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_requestdelivery' operator='eq'  uitype='bsd_requestdelivery' value='" + requestdelivery.Id + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";
                        var lstEntity = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                        if (lstEntity.Entities.Any())
                        {
                            foreach (var requestdeliverydeliveryplantruck in lstEntity.Entities)
                            {
                                EntityReference Rf_Deliveryplantruck = (EntityReference)requestdeliverydeliveryplantruck["bsd_deliveryplantruck"];
                                Entity Deliveryplantruck = new Entity(Rf_Deliveryplantruck.LogicalName, Rf_Deliveryplantruck.Id);
                                Deliveryplantruck["bsd_status"] = new OptionSetValue(861450001);
                                crm.Service.Update(Deliveryplantruck);
                                crm.Service.Delete(Deliveryplantruck.LogicalName, Deliveryplantruck.Id);
                            }
                        }

                        #endregion
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        //vinhlh 1/5/2017
        public static void JoinTruck(Guid DelivereScheduleId, string org)
        {
            var crm = new CRMConnector();
            crm.speceficConnectToCrm(org);
            EntityCollection list_truck;
            #region Lấy list truck
            QueryExpression q = new QueryExpression("bsd_deliveryplantruck");
            q.ColumnSet = new ColumnSet(true);
            FilterExpression f = new FilterExpression(LogicalOperator.And);
            f.AddCondition(new ConditionExpression("bsd_deliveryplan", ConditionOperator.Equal, DelivereScheduleId));
            // f.AddCondition(new ConditionExpression("bsd_status", ConditionOperator.Equal, 861450001));
            q.Criteria = f;
            list_truck = crm.Service.RetrieveMultiple(q);
            #endregion

            int CountTruck = list_truck.Entities.Count;
            foreach (Entity deliveryplantruck_target in list_truck.Entities)
            {
                int deliverytruck_type = ((OptionSetValue)deliveryplantruck_target["bsd_deliverytrucktype"]).Value;

                StringBuilder sb = new StringBuilder();
                sb.Append("<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>");
                sb.Append("<entity name='bsd_deliveryplantruck'>");
                sb.Append("<attribute name='bsd_deliveryplantruckid' />");
                sb.Append("<attribute name='bsd_quantity' />");
                sb.Append("<attribute name='bsd_driver' />");
                sb.Append("<filter type='and'>");
                sb.Append("<condition attribute='bsd_deliveryplanproduct' operator='eq' uitype='bsd_deliveryplanproduct' value='" + ((EntityReference)deliveryplantruck_target["bsd_deliveryplanproduct"]).Id + "' />");
                sb.Append("<condition attribute='bsd_licenseplate' operator='eq' value='" + deliveryplantruck_target["bsd_licenseplate"].ToString() + "' />");
                sb.Append("<condition attribute='bsd_status' operator='eq' value='861450001' />");
                sb.Append("<condition attribute='bsd_deliverytrucktype' operator='eq' value='" + deliverytruck_type + "' />");
                sb.Append("<condition attribute='bsd_deliveryplantruckid' operator='ne' uitype='bsd_deliveryplantruck' value='" + deliveryplantruck_target.Id + "' />");
                if (deliveryplantruck_target.HasValue("bsd_carrierpartner"))
                {
                    Guid bsd_carrierpartner_id = ((EntityReference)deliveryplantruck_target["bsd_carrierpartner"]).Id;
                    sb.Append("<condition attribute='bsd_carrierpartner' operator='eq' uitype='account' value='" + bsd_carrierpartner_id + "' />");
                }
                //if (deliveryplantruck_target.HasValue("bsd_shippingdeliverymethod"))
                //{
                //    int method = ((OptionSetValue)deliveryplantruck_target["bsd_shippingdeliverymethod"]).Value;
                //    sb.Append("<condition attribute='bsd_shippingdeliverymethod' operator='eq' value='" + method + "' />");
                //}
                //if (deliveryplantruck_target.HasValue("bsd_shippingoption"))
                //{
                //    bool shipping = ((bool)deliveryplantruck_target["bsd_shippingoption"]);
                //    sb.Append("<condition attribute='bsd_shippingoption' operator='eq' value='" + shipping + "' />");
                //}
                //if (deliveryplantruck_target.HasValue("bsd_truckload"))
                //{
                //    Guid bsd_truckload = ((EntityReference)deliveryplantruck_target["bsd_truckload"]).Id;
                //    sb.Append("<condition attribute='bsd_truckload' uitype='bsd_truckload' operator='eq' value='" + bsd_truckload + "' />");
                //}
                sb.Append("</filter>");
                sb.Append("</entity>");
                sb.Append("</fetch>");
                EntityCollection list_deliveryproducttruck = crm.Service.RetrieveMultiple(new FetchExpression(sb.ToString()));
                if (list_deliveryproducttruck.Entities.Any())
                {
                    Entity deliveryplantruck = list_deliveryproducttruck.Entities.First();
                    decimal quantity = (decimal)deliveryplantruck["bsd_quantity"];
                    Entity new_target = new Entity(deliveryplantruck_target.LogicalName, deliveryplantruck_target.Id);
                    new_target["bsd_quantity"] = (decimal)deliveryplantruck_target["bsd_quantity"] + quantity;

                    crm.Service.Update(new_target);
                    crm.Service.Delete(deliveryplantruck.LogicalName, deliveryplantruck.Id);
                    break;
                }
            }
        }
        //end vinhlh 05-01-2017
        //vinhlh 08-11-2017
        internal static string insertInvoicePackingList(InvoicePackingList obj, string org)
        {
            try
            {
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj == null) return "InvoicePackingList is not null";
                if (string.IsNullOrEmpty(obj.bsd_invoiceno.Trim()) && string.IsNullOrEmpty(obj.bsd_invoicenumber.Trim())) return "Invoice No. or Invoice Number is not null";
                foreach (var item in obj.PackingList)
                {
                    string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                    if (deliverynoteID != null)
                    {
                        Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                        deliverynote["bsd_invoiceno"] = obj.bsd_invoiceno.Trim();
                        deliverynote["bsd_invoicenumber"] = obj.bsd_invoicenumber.Trim();
                        crm.Service.Update(deliverynote);
                    }

                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "success";
        }
        internal static string insertInvoiceSubOrder(InvoiceSuborder obj, string org)
        {

            string i = "3";
            try
            {

                var crm = new CRMConnector();
                var pac = new PackingList();
                crm.speceficConnectToCrm(org);
                throw new Exception("ok");
                // if (obj == null) return "InvoiceSuborder is not null";
                if (string.IsNullOrEmpty(obj.Serial) || string.IsNullOrEmpty(obj.Invoice)) return "Serial or Invoice No. is not null";
                if (string.IsNullOrEmpty(obj.SuborderID)) return "Sales Order ID is not null";

                string xml_suborder = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_suborder'>
                                            <attribute name='bsd_type' />          
                                            <attribute name='bsd_order' />           
                                             <attribute name='bsd_quote' />                              
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_suborderax' operator='eq'  value='" + obj.SuborderID + @"' />
                                            </filter>
                                            <link-entity name='salesorder' from='salesorderid' to='bsd_order' visible='false' link-type='outer' alias='a_3c826f1b54bce61193f1000c29d47eab'>
                                              <attribute name='bsd_type' />
                                            </link-entity>
                                          </entity>
                                        </fetch>";

                var etcsuborderre = crm.Service.RetrieveMultiple(new FetchExpression(xml_suborder));

                if (etcsuborderre.Entities.Any())
                {

                    Entity suborderref = etcsuborderre.Entities.First();

                    #region order
                    //Type:Order
                    if (((OptionSetValue)suborderref["bsd_type"]).Value == 861450002)
                    {

                        if (suborderref.ContainAndHasValue("bsd_order"))
                        {
                            string xml_order = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                  <entity name='salesorder'>
                                                    <attribute name='name' />
                                                    <attribute name='customerid' />
                                                    <attribute name='statuscode' />
                                                    <attribute name='totalamount' />
                                                    <attribute name='salesorderid' />
                                                    <attribute name='bsd_type' />
                                                    <attribute name='bsd_contracttype' />
                                                    <order attribute='name' descending='false' />
                                                    <filter type='and'>
                                                      <condition attribute='salesorderid' operator='eq'  uitype='salesorder' value='" + ((EntityReference)suborderref["bsd_order"]).Id.ToString() + @"' />
                                                    </filter>
                                                  </entity>
                                                </fetch>";
                            var etcorder = crm.Service.RetrieveMultiple(new FetchExpression(xml_order));
                            if (etcorder.Entities.Any())
                            {
                                Entity order = etcorder.Entities.First();
                                //Contract type:Domestic sales - type:Economic contract,type:direct expot or on-spot export
                                #region Đơn hàng xuất khẩu
                                if ((((OptionSetValue)order["bsd_contracttype"]).Value != 861450000 && ((OptionSetValue)order["bsd_type"]).Value == 861450001) || ((OptionSetValue)order["bsd_type"]).Value == 100000001 || ((OptionSetValue)order["bsd_type"]).Value == 100000000)
                                {

                                    string invoiceSubOrderId = retrivestringvaluelookup("bsd_invoiceax", "bsd_codeax", obj.Serial.Trim() + "-" + obj.Invoice.Trim(), org);
                                    //throw new Exception( invoiceSubOrderId);

                                    if (invoiceSubOrderId != null)
                                    {
                                        Entity invoiceSubOrder = crm.Service.Retrieve("bsd_invoiceax", Guid.Parse(invoiceSubOrderId), new ColumnSet(true));
                                        EntityReference suborder_rf = (EntityReference)invoiceSubOrder["bsd_suborder"];
                                        Entity suborder = crm.Service.Retrieve(suborder_rf.LogicalName, suborder_rf.Id, new ColumnSet("bsd_suborderax"));
                                        if (suborder.HasValue("bsd_suborderax"))
                                        {

                                            foreach (var item in obj.PackingList)
                                            {
                                               
                                                    if (item.PackingListID.Equals("") || item.PackingListID.Equals(" ") || item.PackingListID.Equals("abc")) continue;
                                                string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                          <entity name='bsd_suborder'>
                                            <attribute name='bsd_name' />
                                            <order attribute='bsd_name' descending='false' />
                                            <link-entity name='salesorder' from='salesorderid' to='bsd_order' visible='false' link-type='outer' alias='a_3c826f1b54bce61193f1000c29d47eab'>
                                              <attribute name='bsd_type' />
                                            </link-entity>
                                            <link-entity name='bsd_deliveryplan' from='bsd_suborder' to='bsd_suborderid' alias='do'>
                                              <link-entity name='bsd_requestdelivery' from='bsd_deliveryplan' to='bsd_deliveryplanid' alias='dp'>
                                                <link-entity name='bsd_deliverynote' from='bsd_requestdelivery' to='bsd_requestdeliveryid' alias='dq'>
                                                  <filter type='and'>
                                                    <condition attribute='bsd_packinglistax' operator='eq' value='" + item.PackingListID.Trim() + @"' />
                                                  </filter>
                                                </link-entity>
                                              </link-entity>
                                            </link-entity>
                                          </entity>
                                        </fetch>";

                                                var etcsuborder = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                                                if (etcsuborder.Entities.Any())
                                                {
                                                    string xmlinvoice1 = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_invoiceax'>
                                                        <attribute name='bsd_invoiceaxid' />
                                                        <attribute name='bsd_name' />
                                                        <attribute name='createdon' />
                                                        <attribute name='bsd_suborder' />
                                                        <order attribute='bsd_name' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_name' operator='eq' value='" + obj.Invoice.Trim() + @"' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>";
                                                    var etcinvoicesuborder1 = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice1));
                                                    if (etcinvoicesuborder1.Entities.Any())
                                                    {
                                                        Entity subordercallagain = etcsuborder.Entities.First();

                                                        Entity suborderinvoice = etcinvoicesuborder1.Entities.First();

                                                        string s_subordercallagain = subordercallagain["bsd_name"].ToString();
                                                        string s_suborderinvoice = ((EntityReference)suborderinvoice["bsd_suborder"]).Name.ToString();
                                                        if (!s_subordercallagain.Equals(s_suborderinvoice))
                                                        {
                                                            return "Invoice suborder existed in CRM";
                                                        }

                                                    }

                                                }

                                            }
                                            //if (suborder["bsd_suborderax"].ToString().Trim() == obj.SuborderID.Trim()) return "Invoice suborder existed in CRM";
                                        }
                                    }

                                    #region Insert
                                    string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);

                                    //return "{" + Guid.Parse(SuborderID).ToString().ToUpper() + "}";
                                    string xmlinvoice = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_invoiceax'>
                                                        <attribute name='bsd_invoiceaxid' />
                                                        <attribute name='bsd_name' />
                                                        <attribute name='createdon' />
                                                        <attribute name='bsd_suborder' />
                                                        <order attribute='bsd_name' descending='false' />
                                                        <filter type='and'>
                                                             <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='" + "{" + Guid.Parse(SuborderID).ToString().ToUpper() + "}" + @"' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>";

                                    var etcinvoicesuborder = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice));

                                    if (!etcinvoicesuborder.Entities.Any())
                                    {
                                        Entity entity = new Entity("bsd_invoiceax");
                                        entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                                        i = "1.1";
                                        entity["bsd_invoicedate"] = obj.InvoiceDate;
                                        i = "1.2";
                                        if (obj.CustomerCode != null)
                                            entity["bsd_accountid"] = obj.CustomerCode;
                                        i = "1.3";
                                        if (obj.Serial != null)
                                            entity["bsd_serial"] = obj.Serial;
                                        i = "1.4";
                                        if (obj.Invoice != null)
                                            entity["bsd_name"] = obj.Invoice;
                                        i = "1.5";
                                        if (obj.Description != null)
                                            entity["bsd_description"] = obj.Description;
                                        i = "1.6";
                                        if (obj.TotalAmount.ToString() != null)
                                            entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                                        i = "1.7";
                                        if (obj.TotalTax.ToString() != null)
                                            entity["bsd_totaltax"] = new Money(obj.TotalTax);
                                        if (obj.ExchangeRate.ToString() != null)
                                            i = "1.8";
                                        entity["bsd_exchangerate"] = obj.ExchangeRate;
                                        if (obj.ExtendedAmount.ToString() != null)
                                            entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                                        i = "1.9";
                                        entity["bsd_paymentdate"] = obj.PaymentDate;

                                        i = "2";
                                        #region entity lookup
                                        //  obj.SuborderID = "SO-000738";
                                        //string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                                        if (SuborderID != null)
                                        {

                                            entity["bsd_suborder"] = new EntityReference("bsd_suborder", Guid.Parse(SuborderID));
                                            Entity suborder = crm.Service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date"));
                                            if (suborder.HasValue("bsd_date"))
                                                entity["bsd_date"] = suborder["bsd_date"];
                                        }
                                        else return "Suborder " + obj.SuborderID + " not found in CRM";

                                        string CustomerCode = retriveLookup("account", "accountnumber", obj.CustomerCode, org);
                                        if (CustomerCode != null)
                                        {
                                            entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                                        }
                                        string Currency = retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, org);

                                        if (Currency != null)
                                        {
                                            entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                                        }

                                        else
                                            return "Iso currencycode " + obj.Currency + " not found CRM";

                                        i = "102";
                                        Guid idInvoice = crm.Service.Create(entity);
                                        #endregion


                                        //foreach (var item in obj.PackingList)
                                        //{
                                        //    if (item.PackingListID.Equals("") || item.PackingListID.Equals(" ") || item.PackingListID.Equals(null)) continue;
                                        //    string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                                        //    if (deliverynoteID != null)
                                        //    {
                                        //        Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                                        //        deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                                        //        deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                                        //        deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                                        //        crm.Service.Update(deliverynote);
                                        //    }
                                        //}


                                    }
                                    if (etcinvoicesuborder.Entities.Any())
                                    {
                                        Entity entity = new Entity("bsd_invoiceax", Guid.Parse(etcinvoicesuborder.Entities.First().Id.ToString()));
                                        string pack = "";
                                        foreach (var item in obj.PackingList)
                                        {
                                            if (item.PackingListID.Equals("") || item.PackingListID.Equals(" ") || item.PackingListID.Equals("abc")) continue;
                                            pack = pack  + item.PackingListID.Trim() + "-";
                                        }
                                        foreach (var item in obj.PackingList)
                                        {
                                            if (item.PackingListID.Equals("") || item.PackingListID.Equals(" ") || item.PackingListID.Equals("abc"))
                                            {
                                                entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                                                i = "1.1";
                                                entity["bsd_invoicedate"] = obj.InvoiceDate;
                                                i = "1.2";
                                                if (obj.CustomerCode != null)
                                                    entity["bsd_accountid"] = obj.CustomerCode;
                                                i = "1.3";
                                                if (obj.Serial != null)
                                                    entity["bsd_serial"] = obj.Serial;
                                                i = "1.4";
                                                if (obj.Invoice != null)
                                                    entity["bsd_name"] = obj.Invoice;
                                                i = "1.5";
                                                if (obj.Description != null)
                                                    entity["bsd_description"] = obj.Description;
                                                i = "1.6";
                                                if (obj.TotalAmount.ToString() != null)
                                                    entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                                                i = "1.7";
                                                if (obj.TotalTax.ToString() != null)
                                                    entity["bsd_totaltax"] = new Money(obj.TotalTax);
                                                if (obj.ExchangeRate.ToString() != null)
                                                    i = "1.8";
                                                entity["bsd_exchangerate"] = obj.ExchangeRate;
                                                if (obj.ExtendedAmount.ToString() != null)
                                                    entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                                                i = "1.9";
                                                entity["bsd_paymentdate"] = obj.PaymentDate;

                                                i = "2";
                                                #region entity lookup
                                                //  obj.SuborderID = "SO-000738";
                                                //string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                                                if (SuborderID != null)
                                                {
                                                    entity["bsd_suborder"] = new EntityReference("bsd_suborder", Guid.Parse(SuborderID));
                                                    Entity suborder = crm.Service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date"));
                                                    if (suborder.HasValue("bsd_date"))
                                                        entity["bsd_date"] = suborder["bsd_date"];
                                                }
                                                else return "Suborder " + obj.SuborderID + " not found in CRM";

                                                string CustomerCode = retriveLookup("account", "accountnumber", obj.CustomerCode, org);
                                                if (CustomerCode != null)
                                                {
                                                    entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                                                }
                                                string Currency = retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, org);
                                                if (Currency != null)
                                                {
                                                    entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                                                }
                                                else
                                                    return "Iso currencycode " + obj.Currency + " not found CRM";
                                                i = "501";
                                                #endregion                                                                                                           
                                                crm.Service.Update(entity);
                                            }

                                        }
                                        Entity invoicenew = crm.Service.Retrieve(etcinvoicesuborder.Entities.First().LogicalName, etcinvoicesuborder.Entities.First().Id, new ColumnSet(true));
                                        {
                                            if (invoicenew.HasValue("bsd_packingslip"))
                                            {
                                                Entity inoivceaxnew = new Entity(etcinvoicesuborder.Entities.First().LogicalName, etcinvoicesuborder.Entities.First().Id);
                                                inoivceaxnew["bsd_packingslip"] = invoicenew["bsd_packingslip"].ToString()+pack;
                                                crm.Service.Update(inoivceaxnew);
                                            }
                                            if (!invoicenew.HasValue("bsd_packingslip"))
                                            {
                                                Entity inoivceaxnew = new Entity(etcinvoicesuborder.Entities.First().LogicalName, etcinvoicesuborder.Entities.First().Id);
                                                inoivceaxnew["bsd_packingslip"] = pack;
                                                crm.Service.Update(inoivceaxnew);
                                            }
                                        }
                                       
                                        foreach (var item in obj.PackingList)
                                        {

                                            if (item.PackingListID.Equals("") || item.PackingListID.Equals(" ") || item.PackingListID.Equals("abc")) continue;
                                            string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                                            if (deliverynoteID != null)
                                            {
                                                Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                                                deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                                                Entity invoice = etcinvoicesuborder.Entities.First();
                                                if (obj.Invoice.Equals(invoice["bsd_name"]))
                                                {
                                                    deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, Guid.Parse(invoice.Id.ToString()));
                                                }

                                                deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                                                crm.Service.Update(deliverynote);
                                            }

                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                #region Đơn hàng ko xuất khẩu
                                //return ((OptionSetValue)order["bsd_type"]).Value.ToString() + " - "+((OptionSetValue)order["bsd_contracttype"]).Value.ToString();
                                if ((((OptionSetValue)order["bsd_contracttype"]).Value == 861450000 && ((OptionSetValue)order["bsd_type"]).Value == 861450001) || ((OptionSetValue)order["bsd_type"]).Value == 100000002 || ((OptionSetValue)order["bsd_type"]).Value == 861450002)
                                {

                                    string invoiceSubOrderId = retrivestringvaluelookup("bsd_invoiceax", "bsd_codeax", obj.Serial.Trim() + "-" + obj.Invoice.Trim(), org);
                                    if (invoiceSubOrderId != null)
                                    {
                                        Entity invoiceSubOrder = crm.Service.Retrieve("bsd_invoiceax", Guid.Parse(invoiceSubOrderId), new ColumnSet(true));
                                        EntityReference suborder_rf = (EntityReference)invoiceSubOrder["bsd_suborder"];
                                        Entity suborder = crm.Service.Retrieve(suborder_rf.LogicalName, suborder_rf.Id, new ColumnSet("bsd_suborderax"));
                                        if (suborder.HasValue("bsd_suborderax") && obj.PackingList.Any())
                                        {
                                            foreach (var item in obj.PackingList)
                                            {
                                                string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                          <entity name='bsd_suborder'>
                                            <attribute name='bsd_name' />
                                            <order attribute='bsd_name' descending='false' />
                                            <link-entity name='salesorder' from='salesorderid' to='bsd_order' visible='false' link-type='outer' alias='a_3c826f1b54bce61193f1000c29d47eab'>
                                              <attribute name='bsd_type' />
                                            </link-entity>
                                            <link-entity name='bsd_deliveryplan' from='bsd_suborder' to='bsd_suborderid' alias='do'>
                                              <link-entity name='bsd_requestdelivery' from='bsd_deliveryplan' to='bsd_deliveryplanid' alias='dp'>
                                                <link-entity name='bsd_deliverynote' from='bsd_requestdelivery' to='bsd_requestdeliveryid' alias='dq'>
                                                  <filter type='and'>
                                                    <condition attribute='bsd_packinglistax' operator='eq' value='" + item.PackingListID.Trim() + @"' />
                                                  </filter>
                                                </link-entity>
                                              </link-entity>
                                            </link-entity>
                                          </entity>
                                        </fetch>";
                                                var etcsuborder = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                                                if (etcsuborder.Entities.Any())
                                                {
                                                    string xmlinvoice1 = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_invoiceax'>
                                                        <attribute name='bsd_invoiceaxid' />
                                                        <attribute name='bsd_name' />
                                                        <attribute name='createdon' />
                                                        <attribute name='bsd_suborder' />
                                                        <order attribute='bsd_name' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_name' operator='eq' value='" + obj.Invoice.Trim() + @"' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>";
                                                    var etcinvoicesuborder1 = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice1));
                                                    if (etcinvoicesuborder1.Entities.Any())
                                                    {
                                                        Entity subordercallagain = etcsuborder.Entities.First();

                                                        Entity suborderinvoice = etcinvoicesuborder1.Entities.First();

                                                        string s_subordercallagain = subordercallagain["bsd_name"].ToString();
                                                        string s_suborderinvoice = ((EntityReference)suborderinvoice["bsd_suborder"]).Name.ToString();
                                                        if (!s_subordercallagain.Equals(s_suborderinvoice))
                                                        {
                                                            return "Invoice suborder existed in CRM";
                                                        }

                                                    }

                                                }

                                            }
                                            //if (suborder["bsd_suborderax"].ToString().Trim() == obj.SuborderID.Trim()) return "Invoice suborder existed in CRM";
                                        }
                                    }
                                    #region Insert

                                    string xmlinvoice = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_invoiceax'>
                                                        <attribute name='bsd_invoiceaxid' />
                                                        <attribute name='bsd_name' />
                                                        <attribute name='createdon' />
                                                        <attribute name='bsd_suborder' />
                                                        <order attribute='bsd_name' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_name' operator='eq' value='" + obj.Invoice.Trim() + @"' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>";
                                    var etcinvoicesuborder = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice));
                                    if (!etcinvoicesuborder.Entities.Any())
                                    {

                                        Entity entity = new Entity("bsd_invoiceax");
                                        entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                                        i = "1.1";
                                        // entity["bsd_date"] = obj.Date;
                                        entity["bsd_invoicedate"] = obj.InvoiceDate;
                                        i = "1.2";
                                        if (obj.CustomerCode != null)
                                            entity["bsd_accountid"] = obj.CustomerCode;
                                        i = "1.3";
                                        if (obj.Serial != null)
                                            entity["bsd_serial"] = obj.Serial;
                                        i = "1.4";
                                        if (obj.Invoice != null)
                                            entity["bsd_name"] = obj.Invoice;
                                        i = "1.5";
                                        if (obj.Description != null)
                                            entity["bsd_description"] = obj.Description;
                                        i = "1.6";
                                        if (obj.TotalAmount.ToString() != null)
                                            entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                                        i = "1.7";
                                        if (obj.TotalTax.ToString() != null)
                                            entity["bsd_totaltax"] = new Money(obj.TotalTax);
                                        if (obj.ExchangeRate.ToString() != null)
                                            i = "1.8";
                                        entity["bsd_exchangerate"] = obj.ExchangeRate;
                                        if (obj.ExtendedAmount.ToString() != null)
                                            entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                                        i = "1.9";
                                        entity["bsd_paymentdate"] = obj.PaymentDate;
                                        //entity["bsd_description"] = obj.Description;
                                        i = "2";
                                        #region entity lookup
                                        //  obj.SuborderID = "SO-000738";
                                        string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                                        if (SuborderID != null)
                                        {
                                            entity["bsd_suborder"] = new EntityReference("bsd_suborder", Guid.Parse(SuborderID));
                                            Entity suborder = crm.Service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date"));
                                            if (suborder.HasValue("bsd_date"))
                                                entity["bsd_date"] = suborder["bsd_date"];
                                        }
                                        else return "Suborder " + obj.SuborderID + " not found in CRM";

                                        string CustomerCode = retriveLookup("account", "accountnumber", obj.CustomerCode, org);
                                        if (CustomerCode != null)
                                        {
                                            entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                                        }
                                        string Currency = retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, org);
                                        if (Currency != null)
                                        {
                                            entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                                        }
                                        else
                                            return "Iso currencycode " + obj.Currency + " not found CRM";
                                        i = "3";
                                        string pack = "";
                                        foreach (var item in obj.PackingList)
                                        {
                                            pack = pack + item.PackingListID.Trim() + "- ";
                                        }
                                        entity["bsd_packingslip"] = pack;
                                        Guid idInvoice = crm.Service.Create(entity);
                                        if (obj.PackingList.Count > 0)
                                        {
                                            if (obj.PackingList.Any())
                                            {
                                                foreach (var item in obj.PackingList)
                                                {
                                                    if (item.PackingListID == null) continue;
                                                    string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                                                    if (deliverynoteID != null)
                                                    {
                                                        Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                                                        deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                                                        deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                                                        deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                                                        crm.Service.Update(deliverynote);
                                                    }
                                                }
                                            }

                                        }
                                    }
                                    if (etcinvoicesuborder.Entities.Any())
                                    {
                                        string pack = "";
                                        foreach (var item in obj.PackingList)
                                        {
                                            pack = pack + item.PackingListID.Trim() + "- ";
                                        }
                                        Entity inoivceaxnew = new Entity(etcinvoicesuborder.Entities.First().LogicalName, etcinvoicesuborder.Entities.First().Id);
                                        inoivceaxnew["bsd_packingslip"] = pack;
                                        crm.Service.Update(inoivceaxnew);
                                        if (obj.PackingList.Count > 0)
                                        {
                                            if (obj.PackingList.Any())
                                            {
                                                foreach (var item in obj.PackingList)
                                                {
                                                    if (item.PackingListID == null) continue;
                                                    Entity suborderinvoice = etcinvoicesuborder.Entities.First();                                       
                                                    string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                                                    if (deliverynoteID != null)
                                                    {
                                                        Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                                                        deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                                                        deliverynote["bsd_invoicenumberax"] = new EntityReference(suborderinvoice.LogicalName, suborderinvoice.Id);
                                                        deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                                                        crm.Service.Update(deliverynote);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    //}
                                    //}
                                    #endregion
                                    //if (obj.PackingList.Any())
                                    //{
                                    //    foreach (var item in obj.PackingList)
                                    //    {
                                    //        //i += item.PackingListID.Trim()+":";
                                    //        string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                                    //        if (deliverynoteID != null)
                                    //        {
                                    //            Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                                    //            deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                                    //            deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                                    //            deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                                    //            crm.Service.Update(deliverynote);
                                    //        }

                                    //    }
                                    //}
                                    i = "4";
                                    #endregion
                                }
                                #endregion
                            }
                        }
                    }
                    #endregion

                    #region quote
                    //Type:Quote
                    //if (((OptionSetValue)suborderref["bsd_type"]).Value == 861450001)
                    //{

                    //    if (suborderref.ContainAndHasValue("bsd_quote"))
                    //    {
                    //        string xml_quote = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    //                              <entity name='quote'>
                    //                                <attribute name='name' />
                    //                                <attribute name='customerid' />
                    //                                <attribute name='statecode' />
                    //                                <attribute name='totalamount' />
                    //                                <attribute name='quoteid' />
                    //                                <attribute name='createdon' />
                    //                                <attribute name='bsd_quotationtype' />
                    //                                <order attribute='name' descending='false' />
                    //                                <filter type='and'>
                    //                                  <condition attribute='bsd_quote' operator='eq' uiname='' uitype='quote' value='" + ((EntityReference)suborderref["bsd_quote"]).Id.ToString() + @"' />
                    //                                </filter>
                    //                              </entity>
                    //                            </fetch>";
                    //        var etcquote = crm.Service.RetrieveMultiple(new FetchExpression(xml_quote));
                    //        if (etcquote.Entities.Any())
                    //        {
                    //            Entity quote = etcquote.Entities.First();
                    //            #region Đơn hàng xuất khẩu         
                    //            //Quotation Type: domesstic sales,  molasses sales
                    //            if (((OptionSetValue)quote["bsd_quotationtype"]).Value != 861450003 && ((OptionSetValue)quote["bsd_quotationtype"]).Value != 861450000)
                    //            {

                    //                string invoiceSubOrderId = retrivestringvaluelookup("bsd_invoiceax", "bsd_codeax", obj.Serial.Trim() + "-" + obj.Invoice.Trim(), org);
                    //                if (invoiceSubOrderId != null)
                    //                {
                    //                    Entity invoiceSubOrder = crm.Service.Retrieve("bsd_invoiceax", Guid.Parse(invoiceSubOrderId), new ColumnSet(true));
                    //                    EntityReference suborder_rf = (EntityReference)invoiceSubOrder["bsd_suborder"];
                    //                    Entity suborder = crm.Service.Retrieve(suborder_rf.LogicalName, suborder_rf.Id, new ColumnSet("bsd_suborderax"));
                    //                    if (suborder.HasValue("bsd_suborderax") && obj.PackingList.Any())
                    //                    {

                    //                        foreach (var item in obj.PackingList)
                    //                        {
                    //                            if (item.PackingListID == null) continue;
                    //                            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                    //                      <entity name='bsd_suborder'>
                    //                        <attribute name='bsd_name' />
                    //                        <order attribute='bsd_name' descending='false' />
                    //                        <link-entity name='salesorder' from='salesorderid' to='bsd_order' visible='false' link-type='outer' alias='a_3c826f1b54bce61193f1000c29d47eab'>
                    //                          <attribute name='bsd_type' />
                    //                        </link-entity>
                    //                        <link-entity name='bsd_deliveryplan' from='bsd_suborder' to='bsd_suborderid' alias='do'>
                    //                          <link-entity name='bsd_requestdelivery' from='bsd_deliveryplan' to='bsd_deliveryplanid' alias='dp'>
                    //                            <link-entity name='bsd_deliverynote' from='bsd_requestdelivery' to='bsd_requestdeliveryid' alias='dq'>
                    //                              <filter type='and'>
                    //                                <condition attribute='bsd_packinglistax' operator='eq' value='" + item.PackingListID.Trim() + @"' />
                    //                              </filter>
                    //                            </link-entity>
                    //                          </link-entity>
                    //                        </link-entity>
                    //                      </entity>
                    //                    </fetch>";
                    //                            var etcsuborder = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                    //                            if (etcsuborder.Entities.Any())
                    //                            {
                    //                                string xmlinvoice1 = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    //                                  <entity name='bsd_invoiceax'>
                    //                                    <attribute name='bsd_invoiceaxid' />
                    //                                    <attribute name='bsd_name' />
                    //                                    <attribute name='createdon' />
                    //                                    <attribute name='bsd_suborder' />
                    //                                    <order attribute='bsd_name' descending='false' />
                    //                                    <filter type='and'>
                    //                                      <condition attribute='bsd_name' operator='eq' value='" + obj.Invoice.Trim() + @"' />
                    //                                    </filter>
                    //                                  </entity>
                    //                                </fetch>";
                    //                                var etcinvoicesuborder1 = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice1));
                    //                                if (etcinvoicesuborder1.Entities.Any())
                    //                                {
                    //                                    Entity subordercallagain = etcsuborder.Entities.First();

                    //                                    Entity suborderinvoice = etcinvoicesuborder1.Entities.First();

                    //                                    string s_subordercallagain = subordercallagain["bsd_name"].ToString();
                    //                                    string s_suborderinvoice = ((EntityReference)suborderinvoice["bsd_suborder"]).Name.ToString();
                    //                                    if (!s_subordercallagain.Equals(s_suborderinvoice))
                    //                                    {
                    //                                        return "Invoice suborder existed in CRM";
                    //                                    }

                    //                                }

                    //                            }

                    //                        }
                    //                        //if (suborder["bsd_suborderax"].ToString().Trim() == obj.SuborderID.Trim()) return "Invoice suborder existed in CRM";
                    //                    }
                    //                }
                    //                #region Insert

                    //                string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                    //                string xmlinvoice = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    //                                  <entity name='bsd_invoiceax'>
                    //                                    <attribute name='bsd_invoiceaxid' />
                    //                                    <attribute name='bsd_name' />
                    //                                    <attribute name='createdon' />
                    //                                    <attribute name='bsd_suborder' />
                    //                                    <order attribute='bsd_name' descending='false' />
                    //                                    <filter type='and'>
                    //                                      <condition attribute='bsd_suborder' operator='ep' value='" + SuborderID + @"' />
                    //                                    </filter>
                    //                                  </entity>
                    //                                </fetch>";
                    //                var etcinvoicesuborder = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice));
                    //                if (!etcinvoicesuborder.Entities.Any())
                    //                {
                    //                    Entity entity = new Entity("bsd_invoiceax");
                    //                    entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                    //                    i = "1.1";
                    //                    // entity["bsd_date"] = obj.Date;
                    //                    entity["bsd_invoicedate"] = obj.InvoiceDate;
                    //                    i = "1.2";
                    //                    if (obj.CustomerCode != null)
                    //                        entity["bsd_accountid"] = obj.CustomerCode;
                    //                    i = "1.3";
                    //                    if (obj.Serial != null)
                    //                        entity["bsd_serial"] = obj.Serial;
                    //                    i = "1.4";
                    //                    if (obj.Invoice != null)
                    //                        entity["bsd_name"] = obj.Invoice;
                    //                    i = "1.5";
                    //                    if (obj.Description != null)
                    //                        entity["bsd_description"] = obj.Description;
                    //                    i = "1.6";
                    //                    if (obj.TotalAmount.ToString() != null)
                    //                        entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                    //                    i = "1.7";
                    //                    if (obj.TotalTax.ToString() != null)
                    //                        entity["bsd_totaltax"] = new Money(obj.TotalTax);
                    //                    if (obj.ExchangeRate.ToString() != null)
                    //                        i = "1.8";
                    //                    entity["bsd_exchangerate"] = obj.ExchangeRate;
                    //                    if (obj.ExtendedAmount.ToString() != null)
                    //                        entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                    //                    i = "1.9";
                    //                    entity["bsd_paymentdate"] = obj.PaymentDate;
                    //                    //entity["bsd_description"] = obj.Description;
                    //                    i = "2";
                    //                    #region entity lookup
                    //                    //  obj.SuborderID = "SO-000738";
                    //                    //string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                    //                    if (SuborderID != null)
                    //                    {
                    //                        entity["bsd_suborder"] = new EntityReference("bsd_suborder", Guid.Parse(SuborderID));
                    //                        Entity suborder = crm.Service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date"));
                    //                        if (suborder.HasValue("bsd_date"))
                    //                            entity["bsd_date"] = suborder["bsd_date"];
                    //                    }
                    //                    else return "Suborder " + obj.SuborderID + " not found in CRM";

                    //                    string CustomerCode = retriveLookup("account", "accountnumber", obj.CustomerCode, org);
                    //                    if (CustomerCode != null)
                    //                    {
                    //                        entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                    //                    }
                    //                    string Currency = retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, org);
                    //                    if (Currency != null)
                    //                    {
                    //                        entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                    //                    }
                    //                    else
                    //                        return "Iso currencycode " + obj.Currency + " not found CRM";
                    //                    i = "3";
                    //                    #endregion
                    //                    Guid idInvoice = crm.Service.Create(entity);
                    //                    if (obj.PackingList.Count > 0)
                    //                    {
                    //                        if (obj.PackingList.Any())
                    //                        {
                    //                            foreach (var item in obj.PackingList)
                    //                            {
                    //                                if (item.PackingListID == null) continue;
                    //                                string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                    //                                if (deliverynoteID != null)
                    //                                {
                    //                                    Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                    //                                    deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                    //                                    deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                    //                                    deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                    //                                    crm.Service.Update(deliverynote);
                    //                                }
                    //                            }
                    //                        }

                    //                    }
                    //                }
                    //                else
                    //                {
                    //                    Entity entity = new Entity("bsd_invoiceax", Guid.Parse(etcinvoicesuborder.Entities.First().Id.ToString()));
                    //                    foreach (var item in obj.PackingList)
                    //                    {
                    //                        if (item.PackingListID == null)
                    //                        {
                    //                            entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                    //                            i = "1.1";
                    //                            // entity["bsd_date"] = obj.Date;
                    //                            entity["bsd_invoicedate"] = obj.InvoiceDate;
                    //                            i = "1.2";
                    //                            if (obj.CustomerCode != null)
                    //                                entity["bsd_accountid"] = obj.CustomerCode;
                    //                            i = "1.3";
                    //                            if (obj.Serial != null)
                    //                                entity["bsd_serial"] = obj.Serial;
                    //                            i = "1.4";
                    //                            if (obj.Invoice != null)
                    //                                entity["bsd_name"] = obj.Invoice;
                    //                            i = "1.5";
                    //                            if (obj.Description != null)
                    //                                entity["bsd_description"] = obj.Description;
                    //                            i = "1.6";
                    //                            if (obj.TotalAmount.ToString() != null)
                    //                                entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                    //                            i = "1.7";
                    //                            if (obj.TotalTax.ToString() != null)
                    //                                entity["bsd_totaltax"] = new Money(obj.TotalTax);
                    //                            if (obj.ExchangeRate.ToString() != null)
                    //                                i = "1.8";
                    //                            entity["bsd_exchangerate"] = obj.ExchangeRate;
                    //                            if (obj.ExtendedAmount.ToString() != null)
                    //                                entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                    //                            i = "1.9";
                    //                            entity["bsd_paymentdate"] = obj.PaymentDate;
                    //                            //entity["bsd_description"] = obj.Description;
                    //                            i = "2";
                    //                            #region entity lookup
                    //                            //  obj.SuborderID = "SO-000738";
                    //                            //string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                    //                            if (SuborderID != null)
                    //                            {
                    //                                entity["bsd_suborder"] = new EntityReference("bsd_suborder", Guid.Parse(SuborderID));
                    //                                Entity suborder = crm.Service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date"));
                    //                                if (suborder.HasValue("bsd_date"))
                    //                                    entity["bsd_date"] = suborder["bsd_date"];
                    //                            }
                    //                            else return "Suborder " + obj.SuborderID + " not found in CRM";

                    //                            string CustomerCode = retriveLookup("account", "accountnumber", obj.CustomerCode, org);
                    //                            if (CustomerCode != null)
                    //                            {
                    //                                entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                    //                            }
                    //                            string Currency = retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, org);
                    //                            if (Currency != null)
                    //                            {
                    //                                entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                    //                            }
                    //                            else
                    //                                return "Iso currencycode " + obj.Currency + " not found CRM";
                    //                            i = "3";
                    //                            #endregion
                    //                            crm.Service.Update(entity);
                    //                        }
                    //                    }
                    //                    if (obj.PackingList.Count > 0)
                    //                    {
                    //                        if (obj.PackingList.Any())
                    //                        {
                    //                            foreach (var item in obj.PackingList)
                    //                            {
                    //                                if (item.PackingListID == null) continue;
                    //                                string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                    //                                if (deliverynoteID != null)
                    //                                {
                    //                                    Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                    //                                    deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                    //                                    Entity invoice = etcinvoicesuborder.Entities.First();
                    //                                    //if (obj.Invoice.Equals(invoice["bsd_name"]))
                    //                                    //{
                    //                                    deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, Guid.Parse(etcinvoicesuborder.Entities.First().Id.ToString()));
                    //                                    //}
                    //                                    deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                    //                                    crm.Service.Update(deliverynote);
                    //                                }
                    //                            }
                    //                        }
                    //                    }
                    //                }
                    //                //if (obj.PackingList.Any())
                    //                //{
                    //                //    foreach (var item in obj.PackingList)
                    //                //    {
                    //                //        //i += item.PackingListID.Trim()+":";
                    //                //        string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                    //                //        if (deliverynoteID != null)
                    //                //        {
                    //                //            Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                    //                //            deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                    //                //            deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                    //                //            deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                    //                //            crm.Service.Update(deliverynote);
                    //                //        }

                    //                //    }
                    //                //}
                    //                i = "4";
                    //                #endregion
                    //            }
                    //            #endregion

                    //            #region Đơn hàng ko xuất khẩu
                    //            else
                    //            {
                    //                string invoiceSubOrderId = retrivestringvaluelookup("bsd_invoiceax", "bsd_codeax", obj.Serial.Trim() + "-" + obj.Invoice.Trim(), org);
                    //                if (invoiceSubOrderId != null)
                    //                {
                    //                    Entity invoiceSubOrder = crm.Service.Retrieve("bsd_invoiceax", Guid.Parse(invoiceSubOrderId), new ColumnSet(true));
                    //                    EntityReference suborder_rf = (EntityReference)invoiceSubOrder["bsd_suborder"];
                    //                    Entity suborder = crm.Service.Retrieve(suborder_rf.LogicalName, suborder_rf.Id, new ColumnSet("bsd_suborderax"));
                    //                    if (suborder.HasValue("bsd_suborderax") && obj.PackingList.Any())
                    //                    {
                    //                        foreach (var item in obj.PackingList)
                    //                        {
                    //                            if (item.PackingListID == null) continue;
                    //                            string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                    //                      <entity name='bsd_suborder'>
                    //                        <attribute name='bsd_name' />
                    //                        <order attribute='bsd_name' descending='false' />
                    //                        <link-entity name='salesorder' from='salesorderid' to='bsd_order' visible='false' link-type='outer' alias='a_3c826f1b54bce61193f1000c29d47eab'>
                    //                          <attribute name='bsd_type' />
                    //                        </link-entity>
                    //                        <link-entity name='bsd_deliveryplan' from='bsd_suborder' to='bsd_suborderid' alias='do'>
                    //                          <link-entity name='bsd_requestdelivery' from='bsd_deliveryplan' to='bsd_deliveryplanid' alias='dp'>
                    //                            <link-entity name='bsd_deliverynote' from='bsd_requestdelivery' to='bsd_requestdeliveryid' alias='dq'>
                    //                              <filter type='and'>
                    //                                <condition attribute='bsd_packinglistax' operator='eq' value='" + item.PackingListID.Trim() + @"' />
                    //                              </filter>
                    //                            </link-entity>
                    //                          </link-entity>
                    //                        </link-entity>
                    //                      </entity>
                    //                    </fetch>";
                    //                            var etcsuborder = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                    //                            if (etcsuborder.Entities.Any())
                    //                            {
                    //                                string xmlinvoice1 = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    //                                  <entity name='bsd_invoiceax'>
                    //                                    <attribute name='bsd_invoiceaxid' />
                    //                                    <attribute name='bsd_name' />
                    //                                    <attribute name='createdon' />
                    //                                    <attribute name='bsd_suborder' />
                    //                                    <order attribute='bsd_name' descending='false' />
                    //                                    <filter type='and'>
                    //                                      <condition attribute='bsd_name' operator='eq' value='" + obj.Invoice.Trim() + @"' />
                    //                                    </filter>
                    //                                  </entity>
                    //                                </fetch>";
                    //                                var etcinvoicesuborder1 = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice1));
                    //                                if (etcinvoicesuborder1.Entities.Any())
                    //                                {
                    //                                    Entity subordercallagain = etcsuborder.Entities.First();

                    //                                    Entity suborderinvoice = etcinvoicesuborder1.Entities.First();

                    //                                    string s_subordercallagain = subordercallagain["bsd_name"].ToString();
                    //                                    string s_suborderinvoice = ((EntityReference)suborderinvoice["bsd_suborder"]).Name.ToString();
                    //                                    if (!s_subordercallagain.Equals(s_suborderinvoice))
                    //                                    {
                    //                                        return "Invoice suborder existed in CRM";
                    //                                    }

                    //                                }

                    //                            }

                    //                        }
                    //                        //if (suborder["bsd_suborderax"].ToString().Trim() == obj.SuborderID.Trim()) return "Invoice suborder existed in CRM";
                    //                    }
                    //                }
                    //                #region Insert

                    //                string xmlinvoice = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                    //                                  <entity name='bsd_invoiceax'>
                    //                                    <attribute name='bsd_invoiceaxid' />
                    //                                    <attribute name='bsd_name' />
                    //                                    <attribute name='createdon' />
                    //                                    <attribute name='bsd_suborder' />
                    //                                    <order attribute='bsd_name' descending='false' />
                    //                                    <filter type='and'>
                    //                                      <condition attribute='bsd_name' operator='eq' value='" + obj.Invoice.Trim() + @"' />
                    //                                    </filter>
                    //                                  </entity>
                    //                                </fetch>";
                    //                var etcinvoicesuborder = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice));
                    //                if (!etcinvoicesuborder.Entities.Any())
                    //                {

                    //                    Entity entity = new Entity("bsd_invoiceax");
                    //                    entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                    //                    i = "1.1";
                    //                    // entity["bsd_date"] = obj.Date;
                    //                    entity["bsd_invoicedate"] = obj.InvoiceDate;
                    //                    i = "1.2";
                    //                    if (obj.CustomerCode != null)
                    //                        entity["bsd_accountid"] = obj.CustomerCode;
                    //                    i = "1.3";
                    //                    if (obj.Serial != null)
                    //                        entity["bsd_serial"] = obj.Serial;
                    //                    i = "1.4";
                    //                    if (obj.Invoice != null)
                    //                        entity["bsd_name"] = obj.Invoice;
                    //                    i = "1.5";
                    //                    if (obj.Description != null)
                    //                        entity["bsd_description"] = obj.Description;
                    //                    i = "1.6";
                    //                    if (obj.TotalAmount.ToString() != null)
                    //                        entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                    //                    i = "1.7";
                    //                    if (obj.TotalTax.ToString() != null)
                    //                        entity["bsd_totaltax"] = new Money(obj.TotalTax);
                    //                    if (obj.ExchangeRate.ToString() != null)
                    //                        i = "1.8";
                    //                    entity["bsd_exchangerate"] = obj.ExchangeRate;
                    //                    if (obj.ExtendedAmount.ToString() != null)
                    //                        entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                    //                    i = "1.9";
                    //                    entity["bsd_paymentdate"] = obj.PaymentDate;
                    //                    //entity["bsd_description"] = obj.Description;
                    //                    i = "2";
                    //                    #region entity lookup
                    //                    //  obj.SuborderID = "SO-000738";
                    //                    string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                    //                    if (SuborderID != null)
                    //                    {
                    //                        entity["bsd_suborder"] = new EntityReference("bsd_suborder", Guid.Parse(SuborderID));
                    //                        Entity suborder = crm.Service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date"));
                    //                        if (suborder.HasValue("bsd_date"))
                    //                            entity["bsd_date"] = suborder["bsd_date"];
                    //                    }
                    //                    else return "Suborder " + obj.SuborderID + " not found in CRM";

                    //                    string CustomerCode = retriveLookup("account", "accountnumber", obj.CustomerCode, org);
                    //                    if (CustomerCode != null)
                    //                    {
                    //                        entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                    //                    }
                    //                    string Currency = retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, org);
                    //                    if (Currency != null)
                    //                    {
                    //                        entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                    //                    }
                    //                    else
                    //                        return "Iso currencycode " + obj.Currency + " not found CRM";
                    //                    i = "3";

                    //                    Guid idInvoice = crm.Service.Create(entity);
                    //                    if (obj.PackingList.Count > 0)
                    //                    {
                    //                        if (obj.PackingList.Any())
                    //                        {
                    //                            foreach (var item in obj.PackingList)
                    //                            {
                    //                                if (item.PackingListID == null) continue;
                    //                                string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                    //                                if (deliverynoteID != null)
                    //                                {
                    //                                    Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                    //                                    deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                    //                                    deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                    //                                    deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                    //                                    crm.Service.Update(deliverynote);
                    //                                }
                    //                            }
                    //                        }
                    //                    }
                    //                }
                    //                if (etcinvoicesuborder.Entities.Any())
                    //                {
                    //                    Entity suborderinvoice = etcinvoicesuborder.Entities.First();
                    //                    if (obj.PackingList.Count > 0)
                    //                    {
                    //                        if (obj.PackingList.Any())
                    //                        {
                    //                            foreach (var item in obj.PackingList)
                    //                            {
                    //                                if (item.PackingListID == null) continue;
                    //                                string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                    //                                if (deliverynoteID != null)
                    //                                {
                    //                                    Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                    //                                    deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                    //                                    deliverynote["bsd_invoicenumberax"] = new EntityReference(suborderinvoice.LogicalName, suborderinvoice.Id);
                    //                                    deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                    //                                    crm.Service.Update(deliverynote);
                    //                                }
                    //                            }
                    //                        }
                    //                    }
                    //                }
                    //                #endregion
                    //                //if (obj.PackingList.Any())
                    //                //{
                    //                //    foreach (var item in obj.PackingList)
                    //                //    {
                    //                //        //i += item.PackingListID.Trim()+":";
                    //                //        string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                    //                //        if (deliverynoteID != null)
                    //                //        {
                    //                //            Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                    //                //            deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                    //                //            deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                    //                //            deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                    //                //            crm.Service.Update(deliverynote);
                    //                //        }

                    //                //    }
                    //                //}
                    //                i = "4";
                    //                #endregion
                    //            }
                    //            #endregion
                    //        }
                    //    }
                    //}
                    #endregion

                    #region TH còn lại
                    if (((OptionSetValue)suborderref["bsd_type"]).Value != 861450002)
                    {

                        string invoiceSubOrderId = retrivestringvaluelookup("bsd_invoiceax", "bsd_codeax", obj.Serial.Trim() + "-" + obj.Invoice.Trim(), org);
                        if (invoiceSubOrderId != null && (((OptionSetValue)suborderref["bsd_type"]).Value != 861450004 || ((OptionSetValue)suborderref["bsd_type"]).Value != 861450005))
                        {
                            Entity invoiceSubOrder = crm.Service.Retrieve("bsd_invoiceax", Guid.Parse(invoiceSubOrderId), new ColumnSet(true));
                            EntityReference suborder_rf = (EntityReference)invoiceSubOrder["bsd_suborder"];
                            Entity suborder = crm.Service.Retrieve(suborder_rf.LogicalName, suborder_rf.Id, new ColumnSet("bsd_suborderax"));
                            if (suborder.HasValue("bsd_suborderax"))
                            {
                                foreach (var item in obj.PackingList)
                                {
                                    string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
                                          <entity name='bsd_suborder'>
                                            <attribute name='bsd_name' />
                                            <order attribute='bsd_name' descending='false' />
                                            <link-entity name='salesorder' from='salesorderid' to='bsd_order' visible='false' link-type='outer' alias='a_3c826f1b54bce61193f1000c29d47eab'>
                                              <attribute name='bsd_type' />
                                            </link-entity>
                                            <link-entity name='bsd_deliveryplan' from='bsd_suborder' to='bsd_suborderid' alias='do'>
                                              <link-entity name='bsd_requestdelivery' from='bsd_deliveryplan' to='bsd_deliveryplanid' alias='dp'>
                                                <link-entity name='bsd_deliverynote' from='bsd_requestdelivery' to='bsd_requestdeliveryid' alias='dq'>
                                                  <filter type='and'>
                                                    <condition attribute='bsd_packinglistax' operator='eq' value='" + item.PackingListID.Trim() + @"' />
                                                  </filter>
                                                </link-entity>
                                              </link-entity>
                                            </link-entity>
                                          </entity>
                                        </fetch>";
                                    var etcsuborder = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                                    if (etcsuborder.Entities.Any())
                                    {
                                        string xmlinvoice = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_invoiceax'>
                                                        <attribute name='bsd_invoiceaxid' />
                                                        <attribute name='bsd_name' />
                                                        <attribute name='createdon' />
                                                        <attribute name='bsd_suborder' />
                                                        <order attribute='bsd_name' descending='false' />
                                                        <filter type='and'>
                                                          <condition attribute='bsd_name' operator='eq' value='" + obj.Invoice.Trim() + @"' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>";
                                        var etcinvoicesuborder = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice));
                                        if (etcinvoicesuborder.Entities.Any())
                                        {
                                            Entity subordercallagain = etcsuborder.Entities.First();

                                            Entity suborderinvoice = etcinvoicesuborder.Entities.First();

                                            string s_subordercallagain = subordercallagain["bsd_name"].ToString();
                                            string s_suborderinvoice = ((EntityReference)suborderinvoice["bsd_suborder"]).Name.ToString();
                                            if (!s_subordercallagain.Equals(s_suborderinvoice))
                                            {
                                                return "Invoice suborder existed in CRM";
                                            }

                                        }

                                    }

                                }
                                //if (suborder["bsd_suborderax"].ToString().Trim() == obj.SuborderID.Trim()) return "Invoice suborder existed in CRM";
                            }
                        }
                        #region Insert
                        string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                        if (obj.PackingList.Any())
                        {
                            string pack = "";
                            foreach (var item in obj.PackingList)
                            {
                                pack = pack + "- " + item.PackingListID.Trim();
                            }   
                            foreach (var item in obj.PackingList)
                            {
                                string xmlinvoice = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                      <entity name='bsd_invoiceax'>
                                                        <attribute name='bsd_invoiceaxid' />
                                                        <attribute name='bsd_name' />
                                                        <attribute name='createdon' />
                                                        <attribute name='bsd_suborder' />
                                                        <order attribute='bsd_name' descending='false' />
                                                        <filter type='and'>
                                                        <condition attribute='bsd_suborder' operator='eq' uitype='bsd_suborder' value='" + "{" + Guid.Parse(SuborderID).ToString().ToUpper() + "}" + @"' />
                                                        <condition attribute='bsd_name' operator='eq' value='" + obj.Invoice.Trim() + @"' />
                                                        </filter>
                                                      </entity>
                                                    </fetch>";
                                var etcinvoicesuborder = crm.Service.RetrieveMultiple(new FetchExpression(xmlinvoice));
                                if (!etcinvoicesuborder.Entities.Any())
                                {
                                    Entity entity = new Entity("bsd_invoiceax");
                                    entity["bsd_codeax"] = obj.Serial.Trim() + "-" + obj.Invoice.Trim();
                                    i = "1.1";
                                    // entity["bsd_date"] = obj.Date;
                                    entity["bsd_invoicedate"] = obj.InvoiceDate;
                                    i = "1.2";
                                    if (obj.CustomerCode != null)
                                        entity["bsd_accountid"] = obj.CustomerCode;
                                    i = "1.3";
                                    if (obj.Serial != null)
                                        entity["bsd_serial"] = obj.Serial;
                                    i = "1.4";
                                    if (obj.Invoice != null)
                                        entity["bsd_name"] = obj.Invoice;
                                    i = "1.5";
                                    if (obj.Description != null)
                                        entity["bsd_description"] = obj.Description;
                                    i = "1.6";
                                    if (obj.TotalAmount.ToString() != null)
                                        entity["bsd_totalamount"] = new Money(obj.TotalAmount);
                                    i = "1.7";
                                    if (obj.TotalTax.ToString() != null)
                                        entity["bsd_totaltax"] = new Money(obj.TotalTax);
                                    if (obj.ExchangeRate.ToString() != null)
                                        i = "1.8";
                                    entity["bsd_exchangerate"] = obj.ExchangeRate;
                                    if (obj.ExtendedAmount.ToString() != null)
                                        entity["bsd_extendedamount"] = new Money(obj.ExtendedAmount);
                                    i = "1.9";
                                    entity["bsd_paymentdate"] = obj.PaymentDate;
                                    //entity["bsd_description"] = obj.Description;
                                    i = "2";
                                    #region entity lookup
                                    //  obj.SuborderID = "SO-000738";
                                    //string SuborderID = retriveLookup("bsd_suborder", "bsd_suborderax", obj.SuborderID, org);
                                    if (SuborderID != null)
                                    {
                                        entity["bsd_suborder"] = new EntityReference("bsd_suborder", Guid.Parse(SuborderID));
                                        Entity suborder = crm.Service.Retrieve("bsd_suborder", Guid.Parse(SuborderID), new ColumnSet("bsd_date"));
                                        if (suborder.HasValue("bsd_date"))
                                            entity["bsd_date"] = suborder["bsd_date"];
                                    }
                                    else return "Suborder " + obj.SuborderID + " not found in CRM";

                                    string CustomerCode = retriveLookup("account", "accountnumber", obj.CustomerCode, org);
                                    if (CustomerCode != null)
                                    {
                                        entity["bsd_account"] = new EntityReference("account", Guid.Parse(CustomerCode));
                                    }
                                    string Currency = retriveLookup("transactioncurrency", "isocurrencycode", obj.Currency, org);
                                    if (Currency != null)
                                    {
                                        entity["transactioncurrencyid"] = new EntityReference("transactioncurrency", Guid.Parse(Currency));
                                    }
                                    else
                                        return "Iso currencycode " + obj.Currency + " not found CRM";
                                    i = "3";
                                    entity["bsd_packingslip"] = pack;
                                    Guid idInvoice = crm.Service.Create(entity);
                                    if (item.PackingListID == null) continue;
                                    string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                                    if (deliverynoteID != null)
                                    {
                                        Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                                        deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                                        deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                                        deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                                        crm.Service.Update(deliverynote);
                                    }
                                }
                                if (etcinvoicesuborder.Entities.Any())
                                {
                                 
                                    Entity inoivceaxnew = new Entity(etcinvoicesuborder.Entities.First().LogicalName, etcinvoicesuborder.Entities.First().Id);
                                    inoivceaxnew["bsd_packingslip"] = pack;
                                    crm.Service.Update(inoivceaxnew);
                                    Entity suborderinvoice = etcinvoicesuborder.Entities.First();
                                    if (item.PackingListID == null) continue;
                                    string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                                    if (deliverynoteID != null)
                                    {
                                        Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                                        deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                                        deliverynote["bsd_invoicenumberax"] = new EntityReference(suborderinvoice.LogicalName, suborderinvoice.Id);
                                        deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                                        crm.Service.Update(deliverynote);
                                    }

                                }
                            }
                        }
                        #endregion
                        //if (obj.PackingList.Any())
                        //{
                        //    foreach (var item in obj.PackingList)
                        //    {
                        //        //i += item.PackingListID.Trim()+":";
                        //        string deliverynoteID = retrivestringvaluelookup("bsd_deliverynote", "bsd_packinglistax", item.PackingListID.Trim(), org);
                        //        if (deliverynoteID != null)
                        //        {
                        //            Entity deliverynote = new Entity("bsd_deliverynote", Guid.Parse(deliverynoteID));
                        //            deliverynote["bsd_invoiceno"] = obj.Invoice.Trim();
                        //            deliverynote["bsd_invoicenumberax"] = new EntityReference(entity.LogicalName, idInvoice);
                        //            deliverynote["bsd_invoicedate"] = obj.InvoiceDate;
                        //            crm.Service.Update(deliverynote);
                        //        }

                        //    }
                        //}
                        i = "4";
                        #endregion
                    }
                    #endregion
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "CRM " + ex.Message + i;
            }
            // return "success";i
        }
        //vinhlh 02-11-2017
        internal static string insertTransferOrder(TransferOrder obj, string org)
        {
            Guid bsd_trasferorderid = Guid.NewGuid();
            int i = 0;
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (obj != null)
                {
                    if (!obj.TransferOrderProduct.Any()) return "Transfer Order Product is not null";
                    if (string.IsNullOrEmpty(obj.RecId)) return "RecId is not null";
                    string id = retriveLookup("bsd_transferorder", "bsd_codeax", obj.RecId, org);
                    if (id != null)
                    {
                        #region Update
                        try
                        {
                            foreach (var item in obj.TransferOrderProduct)
                            {
                                string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_transferorderproduct'>
                                            <attribute name='bsd_transferorderproductid' />
                                            <attribute name='bsd_name' />
                                            <attribute name='bsd_quantity' />
                                            <attribute name='createdon' />
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_productid' operator='eq' value='" + item.productnumber.Trim() + @"' />
                                              <condition attribute='bsd_transferorder' operator='eq'  uitype='bsd_transferorder' value='" + id.Trim() + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";
                                var transferOrderProduct = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                                if (transferOrderProduct.Entities.Any())
                                {

                                    Entity en = new Entity("bsd_transferorderproduct", transferOrderProduct.Entities.First().Id);
                                    //if ((transferOrderProduct.Entities.First().HasValue("bsd_quantity")))
                                    //    en["bsd_quantity"] = int.Parse(transferOrderProduct.Entities.First()["bsd_quantity"].ToString()) + int.Parse(item.bsd_quantity.ToString());
                                    //else
                                    en["bsd_quantity"] = int.Parse(item.bsd_quantity.ToString());
                                    crm.Service.Update(en);

                                }
                                else
                                {
                                    #region Create 
                                    Entity en = new Entity("bsd_transferorderproduct");
                                    // en["bsd_codeax"] = item.RecId;
                                    en["bsd_quantity"] = int.Parse(item.bsd_quantity.ToString());
                                    en["bsd_transferorder"] = new EntityReference("bsd_transferorder", Guid.Parse(id));
                                    string productid = retriveLookupProduct("product", "productnumber", item.productnumber.Trim(), org);
                                    #region
                                    if (productid != null)
                                    {
                                        en["bsd_product"] = new EntityReference("product", Guid.Parse(productid));
                                        en["bsd_productid"] = item.productnumber.Trim();
                                        en["bsd_name"] = item.productnumber.Trim();
                                        Entity pro = crm.Service.Retrieve("product", Guid.Parse(productid), new ColumnSet("defaultuomid"));
                                        en["bsd_unit"] = (EntityReference)pro["defaultuomid"];
                                        en["bsd_deliveryfee"] = true;
                                        if (item.bsd_deliverymethod != null)
                                        {
                                            if (item.bsd_deliverymethod.ToString() == "Ton")
                                                en["bsd_deliverymethod"] = new OptionSetValue(861450000);
                                            else en["bsd_deliverymethod"] = new OptionSetValue(861450001);
                                        }
                                        en["bsd_licenseplate"] = item.bsd_licenseplate;
                                        en["bsd_driver"] = item.bsd_driver;
                                        if (!string.IsNullOrEmpty(item.bsd_porter.ToString()))
                                            en["bsd_porter"] = item.bsd_porter;
                                        #region Lookup Nhà vân chuyển
                                        string bsd_carrierpartner = retriveLookup("account", "accountnumber", item.bsd_carrierpartner, org);
                                        if (bsd_carrierpartner != null)
                                        {
                                            en["bsd_carrierpartner"] = new EntityReference("account", Guid.Parse(bsd_carrierpartner));
                                        }
                                        // else return "Carrier partner " + item.bsd_carrierpartner + " does not exist CRM";
                                        #endregion
                                        crm.Service.Create(en);
                                    }

                                    #endregion

                                    #endregion
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            return "Transfer order product error 456:" + ex.Message;
                        }
                        #endregion
                    }
                    else
                    {
                        id = retriveLookup("bsd_transferorder", "bsd_name", obj.bsd_name, org);
                        if (id != null)
                        {
                            #region Update
                            try
                            {
                                foreach (var item in obj.TransferOrderProduct)
                                {
                                    string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_transferorderproduct'>
                                            <attribute name='bsd_transferorderproductid' />
                                            <attribute name='bsd_name' />
                                            <attribute name='bsd_quantity' />
                                            <attribute name='createdon' />
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_productid' operator='eq' value='" + item.productnumber.Trim() + @"' />
                                              <condition attribute='bsd_transferorder' operator='eq'  uitype='bsd_transferorder' value='" + id.Trim() + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";
                                    var transferOrderProduct = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                                    if (transferOrderProduct.Entities.Any())
                                    {
                                        #region Update
                                        Entity en = new Entity("bsd_transferorderproduct", transferOrderProduct.Entities.First().Id);
                                        //if ((transferOrderProduct.Entities.First().HasValue("bsd_quantity")))
                                        //    en["bsd_quantity"] = int.Parse(transferOrderProduct.Entities.First()["bsd_quantity"].ToString()) + int.Parse(item.bsd_quantity.ToString());
                                        //else
                                        en["bsd_quantity"] = int.Parse(item.bsd_quantity.ToString());
                                        crm.Service.Update(en);
                                        #endregion

                                    }
                                    else
                                    {
                                        #region Create 
                                        Entity en = new Entity("bsd_transferorderproduct");
                                        // en["bsd_codeax"] = item.RecId;
                                        en["bsd_quantity"] = int.Parse(item.bsd_quantity.ToString());
                                        en["bsd_transferorder"] = new EntityReference("bsd_transferorder", Guid.Parse(id));
                                        string productid = retriveLookupProduct("product", "productnumber", item.productnumber.Trim(), org);
                                        #region
                                        if (productid != null)
                                        {
                                            en["bsd_product"] = new EntityReference("product", Guid.Parse(productid));
                                            en["bsd_productid"] = item.productnumber.Trim();
                                            en["bsd_name"] = item.productnumber.Trim();
                                            Entity pro = crm.Service.Retrieve("product", Guid.Parse(productid), new ColumnSet("defaultuomid"));
                                            en["bsd_unit"] = (EntityReference)pro["defaultuomid"];
                                            en["bsd_deliveryfee"] = true;
                                            if (item.bsd_deliverymethod != null)
                                            {
                                                if (item.bsd_deliverymethod.ToString() == "Ton")
                                                    en["bsd_deliverymethod"] = new OptionSetValue(861450000);
                                                else en["bsd_deliverymethod"] = new OptionSetValue(861450001);
                                            }
                                            en["bsd_licenseplate"] = item.bsd_licenseplate;
                                            en["bsd_driver"] = item.bsd_driver;
                                            if (!string.IsNullOrEmpty(item.bsd_porter.ToString()))
                                                en["bsd_porter"] = item.bsd_porter;
                                            #region Lookup Nhà vân chuyển
                                            string bsd_carrierpartner = retriveLookup("account", "accountnumber", item.bsd_carrierpartner, org);
                                            if (bsd_carrierpartner != null)
                                            {
                                                en["bsd_carrierpartner"] = new EntityReference("account", Guid.Parse(bsd_carrierpartner));
                                            }
                                            // else return "Carrier partner " + item.bsd_carrierpartner + " does not exist CRM";
                                            #endregion
                                            crm.Service.Create(en);
                                        }

                                        #endregion

                                        #endregion
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                return "Transfer order product error 789:" + ex.Message;
                            }
                            #endregion
                        }
                        else
                        {
                            i = 1;
                            Entity entity = new Entity("bsd_transferorder");
                            i = 2;
                            #region Create

                            entity["bsd_name"] = obj.bsd_name;
                            i = 3;
                            if (!string.IsNullOrEmpty(obj.bsd_shipdate.ToString()))
                                entity["bsd_shipdate"] = obj.bsd_shipdate;
                            i = 4;
                            if (!string.IsNullOrEmpty(obj.bsd_receiptdate.ToString()))
                                entity["bsd_receiptdate"] = obj.bsd_receiptdate;
                            i = 5;
                            entity["bsd_codeax"] = obj.RecId;
                            // if (!string.IsNullOrEmpty(obj.bsd_deliveryfee.ToString()))


                            entity["bsd_description"] = obj.bsd_description;
                            #region Lookup Entity
                            #region Tosite and ToWareHouse
                            string bsd_tosite = retriveLookup("bsd_site", "bsd_code", obj.bsd_tosite, org);
                            if (bsd_tosite != null)
                            {
                                entity["bsd_tosite"] = new EntityReference("bsd_site", Guid.Parse(bsd_tosite));
                                string xmlWarehouseGood = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(bsd_tosite) + @"' />
                                                                      <condition attribute='bsd_warehouseid' operator='eq' value='" + obj.bsd_towarehouse + @"' />
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                                var WarehouseGood = crm.Service.RetrieveMultiple(new FetchExpression(xmlWarehouseGood));
                                if (WarehouseGood.Entities.Any())
                                {

                                    entity["bsd_towarehouse"] = new EntityReference("bsd_warehouseentity", WarehouseGood.Entities.First().Id);

                                }
                                else return "To warehouse " + obj.bsd_towarehouse + " does not exist CRM";

                            }
                            else return "To site " + obj.bsd_tosite + " does not exist CRM";
                            #endregion
                            #region From Site and From Warehouse
                            string bsd_fromsite = retriveLookup("bsd_site", "bsd_code", obj.bsd_fromsite, org);
                            if (bsd_fromsite != null)
                            {
                                entity["bsd_fromsite"] = new EntityReference("bsd_site", Guid.Parse(bsd_fromsite));

                                string xmlWarehouseGood = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                                                  <entity name='bsd_warehouseentity'>
                                                                    <attribute name='bsd_warehouseentityid' />
                                                                    <attribute name='bsd_name' />
                                                                    <attribute name='createdon' />
                                                                    <order attribute='bsd_name' descending='false' />
                                                                    <filter type='and'>
                                                                      <condition attribute='bsd_site' operator='eq'  uitype='bsd_site' value='" + Guid.Parse(bsd_fromsite) + @"' />
                                                                      <condition attribute='bsd_warehouseid' operator='eq' value='" + obj.bsd_fromwarehouse + @"' />
                                                                    </filter>
                                                                  </entity>
                                                                </fetch>";
                                var WarehouseGood = crm.Service.RetrieveMultiple(new FetchExpression(xmlWarehouseGood));
                                if (WarehouseGood.Entities.Any())
                                {

                                    entity["bsd_fromwarehouse"] = new EntityReference("bsd_warehouseentity", WarehouseGood.Entities.First().Id);

                                }
                                else return "From warehouse " + obj.bsd_fromwarehouse + " does not exist CRM";

                            }
                            else return "From site " + obj.bsd_fromsite + " does not exist CRM";
                            #endregion

                            #endregion
                            i = 6;
                            bsd_trasferorderid = crm.Service.Create(entity);
                            i = 7;
                            string trace = "0";
                            string product = "";
                            try
                            {
                                foreach (var item in obj.TransferOrderProduct)
                                {
                                    i++;
                                    Entity en = new Entity("bsd_transferorderproduct");
                                    // en["bsd_codeax"] = item.RecId;
                                    en["bsd_quantity"] = int.Parse(item.bsd_quantity.ToString());
                                    en["bsd_transferorder"] = new EntityReference("bsd_transferorder", bsd_trasferorderid);
                                    string productid = retriveLookupProduct("product", "productnumber", item.productnumber.Trim(), org);
                                    trace = "1";
                                    #region
                                    if (productid != null)
                                    {
                                        trace = "2";
                                        product = item.productnumber.Trim();
                                        en["bsd_product"] = new EntityReference("product", Guid.Parse(productid));
                                        en["bsd_productid"] = item.productnumber.Trim();
                                        en["bsd_name"] = item.productnumber.Trim();
                                        Entity pro = crm.Service.Retrieve("product", Guid.Parse(productid), new ColumnSet("defaultuomid"));
                                        en["bsd_unit"] = (EntityReference)pro["defaultuomid"];
                                        en["bsd_deliveryfee"] = true;
                                        trace = "3";
                                        if (item.bsd_deliverymethod != null)
                                        {
                                            if (item.bsd_deliverymethod.ToString() == "Ton")
                                                en["bsd_deliverymethod"] = new OptionSetValue(861450000);
                                            else en["bsd_deliverymethod"] = new OptionSetValue(861450001);
                                        }
                                        en["bsd_licenseplate"] = item.bsd_licenseplate;
                                        en["bsd_driver"] = item.bsd_driver;
                                        if (!string.IsNullOrEmpty(item.bsd_porter.ToString()))
                                            en["bsd_porter"] = item.bsd_porter;
                                        trace = "4";
                                        #region Lookup Nhà vân chuyển
                                        string bsd_carrierpartner = retriveLookup("account", "accountnumber", item.bsd_carrierpartner, org);
                                        if (bsd_carrierpartner != null)
                                        {
                                            en["bsd_carrierpartner"] = new EntityReference("account", Guid.Parse(bsd_carrierpartner));
                                        }
                                        else
                                        {
                                            crm.Service.Delete("bsd_transferorder", bsd_trasferorderid);
                                            return "Carrier partner " + item.bsd_carrierpartner + " does not exist CRM";

                                        }
                                        trace = "5";
                                        #endregion
                                    }
                                    else
                                    {

                                        throw new Exception("Product " + item.productnumber.Trim() + " does not exsit CRM");
                                    }
                                    #endregion
                                    trace = "6";
                                    crm.Service.Create(en);
                                    trace = "7";
                                }
                            }
                            catch (Exception ex)
                            {
                                crm.Service.Delete("bsd_transferorder", bsd_trasferorderid);
                                return "Transfer order product error 123:" + ex.Message + " trace: " + trace + " product: " + product;
                            }

                            #endregion
                        }
                        // return "success";
                    }
                }
                return "success";
            }
            catch (Exception ex)
            {
                return "error CRM: " + ex.Message + i.ToString();
                throw;
            }
        }
        internal static bool DeleteTransferOrder(string Recid, string org)
        {
            try
            {
                //org = "B2B";
                var crm = new CRMConnector();
                crm.speceficConnectToCrm(org);
                if (Recid != null)
                {
                    string paymenttermid = retriveLookup("bsd_transferorder", "bsd_codeax", Recid, org);
                    if (paymenttermid != null)
                    {
                        crm.Service.Delete("bsd_transferorder", Guid.Parse(paymenttermid));

                    }

                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
            //   return "succes";
        }
        internal static string retriveLookup(string LogicalName, string fieldName, string Value, string org)
        {
            CRMConnector crm = new CRMConnector();
            crm.speceficConnectToCrm(org);
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            //if (LogicalName != "uom")
            //{
            //    fetchxmm += "<condition attribute='statecode' operator='eq' value='0' />";
            //}
            fetchxmm += "<condition attribute='" + fieldName + "' operator='eq' value='" + Value + "' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = crm.Service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }
        internal static string retriveLookupProduct(string LogicalName, string fieldName, string Value, string org)
        {
            CRMConnector crm = new CRMConnector();
            crm.speceficConnectToCrm(org);
            var fetchxmm = "<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>";
            fetchxmm += "<entity name='" + LogicalName + "'>";
            fetchxmm += "<all-attributes />";
            fetchxmm += "<filter type='and'>";
            fetchxmm += "<condition attribute='" + fieldName + "' operator='eq' value='" + Value + "' />";
            fetchxmm += "</filter>";
            fetchxmm += "</entity>";
            fetchxmm += "</fetch>";
            var entityCollection = crm.Service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }
        internal static string retrivestringvaluelookup(string LogicalName, string fieldName, string Value, string org)
        {
            CRMConnector crm = new CRMConnector();
            crm.speceficConnectToCrm(org);
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
            var entityCollection = crm.Service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }
        internal static string retrivestringvaluelookuplike(string LogicalName, string fieldName, string Value, string org)
        {
            CRMConnector crm = new CRMConnector();
            crm.speceficConnectToCrm(org);
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
            var entityCollection = crm.Service.RetrieveMultiple(new FetchExpression(fetchxmm));
            if (entityCollection.Entities.Count() > 0)
            {
                return entityCollection.Entities.First().Id.ToString();
            }
            return null;
        }
        internal static Message DeleteForm(string etn, string id)
        {
            Message mss = new Message();
            try
            {
                CRMConnector crm = new CRMConnector();
                crm.ConnectToCrm();
                crm.Service.Delete(etn, Guid.Parse(id));

                mss.Status = "Success";
                mss.Data = id;
            }
            catch (Exception ex)
            {
                mss.Status = "Error";
                mss.Content = ex.Message;
            }
            return mss;
        }
        ////Dang 06/16/2017
        internal static Message SaveListEntity(Message data)
        {
            Message mss = new Message();
            try
            {
                mss.Status = "Success";
                if (data.Data != null && data.Data.Length > 0)
                {
                    //  bool isUpdate = false;

                    ExtensionList.RootObject result = JsonConvert.DeserializeObject<ExtensionList.RootObject>(data.Data);
                    var ro = new RootObject();
                    ro.master = new Master();
                    ro.details = new List<Detail>();
                    ro.master.fields = new List<Field>();


                    var crm = new CRMConnector();
                    crm.ConnectToCrm();
                    if (result.type == "update")
                    {
                        string id = "";
                        ExtensionList.RootObject resultUpdate = JsonConvert.DeserializeObject<ExtensionList.RootObject>(data.Data);
                        if (resultUpdate.master != null)
                        {
                            id = resultUpdate.master.fields.Where(obj => obj.type == "Guid").FirstOrDefault().value;
                            var entity = new Entity(resultUpdate.master.entity, Guid.Parse(id));

                            foreach (var item in resultUpdate.master.fields)
                            {
                                entity = SetFields(entity, item);
                            }
                            crm.Service.Update(entity);
                        }
                        if (resultUpdate.details.Count > 0)
                        {
                            foreach (var details in resultUpdate.details)
                            {
                                // var entitydetails = new Entity(details.entity, Guid.Parse(id));

                                if (details.lines.Count > 0)
                                {
                                    foreach (var line in details.lines)
                                    {
                                        string detailsid = line.Where(obj => obj.type == "Guid").FirstOrDefault().value;
                                        var entitydetails = new Entity(details.entity, Guid.Parse(detailsid));
                                        foreach (var field in line)
                                        {
                                            entitydetails = SetFields(entitydetails, field);
                                        }
                                        crm.Service.Update(entitydetails);
                                    }
                                }
                            }
                        }
                        //if (resultUpdate.detail.lines.Count > 0)
                        //{
                        //    foreach (var lines in resultUpdate.detail.lines)
                        //    {
                        //        var entitydetails = new Entity(resultUpdate.master.entity, Guid.Parse(id));
                        //        foreach (var fi in lines)
                        //        {
                        //            entitydetails = SetFields(entitydetails, fi);
                        //        }

                        //    }
                        //}
                        var fullentity = new Entity();
                        var guidid = new Guid();
                        if (!string.IsNullOrEmpty(id))
                        {
                            guidid = Guid.Parse(id);
                        }
                        string resultjson = ReturnJson(result, crm, fullentity, guidid);
                        //end
                        mss.Status = "Success";
                        mss.Data = resultjson;

                    }
                    else if (result.type == "delete")
                    {
                        //string id = result.master.fields.Find(x => x.type == "Guid").value;
                        //crm.Service.Delete(en.LogicalName, Guid.Parse(id));

                        ////if (result.detail.entity.)
                        //foreach (var item in result.detail.lines)
                        //{
                        //    foreach (var initem in item)
                        //    {
                        //        crm.Service.Delete(result.detail.entity, Guid.Parse(id));
                        //    }
                        //}
                        //mss.Status = "Success";
                        //mss.Data = id;
                    }
                    else
                    {
                        Guid guid = new Guid();
                        var en = new Entity();
                        result = JsonConvert.DeserializeObject<ExtensionList.RootObject>(data.Data);
                        if (result.master != null)
                        {
                            string xid = result.master.fields.Find(x => x.type == "Guid").oldvalue;
                            string xName = result.master.entity;
                            en = new Entity(xName);
                            foreach (var item in result.master.fields)
                            {
                                en = SetFields(en, item);
                            }
                            guid = crm.Service.Create(en);
                        }
                        #region details
                        if (result.details.Count > 0)
                        {
                            var guidchild = new Guid();
                            var enchild = new Entity();

                            foreach (var item in result.details)
                            {
                                if (item.lines.Count > 0)
                                {
                                    string xNamechild = item.entity;

                                    foreach (var pr in item.lines)
                                    {
                                        enchild = new Entity(xNamechild);
                                        foreach (var ch in pr)
                                        {
                                            if (ch.type == "String")
                                            {
                                                enchild[ch.fieldname] = ch.value != null ? ch.value.ToString().Trim() : null;
                                            }
                                            else if (ch.type == "Picklist")
                                            {
                                                if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                    enchild[ch.fieldname] = new OptionSetValue(int.Parse(ch.value));
                                                else
                                                    enchild[ch.fieldname] = null;
                                            }
                                            else if (ch.type == "Decimal")
                                            {
                                                if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                    enchild[ch.fieldname] = decimal.Parse(ch.value);
                                                else
                                                    enchild[ch.fieldname] = null;
                                            }
                                            else if (ch.type == "Boolean")
                                            {
                                                if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                    enchild[ch.fieldname] = bool.Parse(ch.value);
                                                else
                                                    enchild[ch.fieldname] = null;
                                            }
                                            else if (ch.type == "DateTime")
                                            {
                                                if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                    enchild[ch.fieldname] = DateTime.Parse(ch.value);
                                                else
                                                    enchild[ch.fieldname] = null;
                                            }
                                            else if (ch.type == "Lookup")
                                            {

                                                if (ch.oldvalue != null)
                                                {
                                                    //idRef = guid.ToString();
                                                    ch.value = guid.ToString();
                                                }
                                                if (ch.value != null && ch.value.Length > 0)
                                                {
                                                    string idRef = "";
                                                    idRef = ch.value.Trim();
                                                    string nameRef = ch.entity.Trim();
                                                    if (!string.IsNullOrEmpty(idRef))
                                                        enchild[ch.fieldname] = new EntityReference(nameRef, Guid.Parse(idRef));
                                                    else enchild[ch.fieldname] = null;
                                                }
                                                else
                                                    enchild[ch.fieldname] = null;
                                            }
                                            else if (ch.type == "Money")
                                            {
                                                if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                    enchild[ch.fieldname] = new Money(decimal.Parse(ch.value));
                                                else enchild[ch.fieldname] = null;
                                            }
                                            else if (ch.type == "Integer")
                                            {
                                                if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                    enchild[ch.fieldname] = Int32.Parse(ch.value);
                                                else
                                                    enchild[ch.fieldname] = null;
                                            }
                                        }
                                        bool isupdate = false;

                                        if (xNamechild == "bsd_definepromotioncostomer")
                                        {
                                            var ojbguid = pr.Where(x => x.type == "Guid").SingleOrDefault();
                                            if (ojbguid.value != null)
                                            {
                                                // Entity ent = new Entity(xName, Guid.Parse(ojbguid.value));
                                                isupdate = true;
                                                foreach (var ch in pr)
                                                {
                                                    if (ch.type == "String")
                                                    {
                                                        enchild[ch.fieldname] = ch.value != null ? ch.value.ToString().Trim() : null;
                                                    }
                                                    else if (ch.type == "Picklist")
                                                    {
                                                        if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                            enchild[ch.fieldname] = new OptionSetValue(int.Parse(ch.value));
                                                        else
                                                            enchild[ch.fieldname] = null;
                                                    }
                                                    else if (ch.type == "Decimal")
                                                    {
                                                        if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                            enchild[ch.fieldname] = decimal.Parse(ch.value);
                                                        else
                                                            enchild[ch.fieldname] = null;
                                                    }
                                                    else if (ch.type == "Boolean")
                                                    {
                                                        if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                            enchild[ch.fieldname] = bool.Parse(ch.value);
                                                        else
                                                            enchild[ch.fieldname] = null;
                                                    }
                                                    else if (ch.type == "DateTime")
                                                    {
                                                        if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                            enchild[ch.fieldname] = DateTime.Parse(ch.value);
                                                        else
                                                            enchild[ch.fieldname] = null;
                                                    }
                                                    else if (ch.type == "Lookup")
                                                    {

                                                        if (ch.oldvalue != null)
                                                        {
                                                            //idRef = guid.ToString();
                                                            ch.value = guid.ToString();
                                                        }
                                                        if (ch.value != null && ch.value.Length > 0)
                                                        {
                                                            string idRef = "";
                                                            idRef = ch.value.Trim();
                                                            string nameRef = ch.entity.Trim();
                                                            if (!string.IsNullOrEmpty(idRef))
                                                                enchild[ch.fieldname] = new EntityReference(nameRef, Guid.Parse(idRef));
                                                            else enchild[ch.fieldname] = null;

                                                        }
                                                        else
                                                            enchild[ch.fieldname] = null;
                                                    }
                                                    else if (ch.type == "Money")
                                                    {
                                                        if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                            enchild[ch.fieldname] = new Money(decimal.Parse(ch.value));
                                                        else enchild[ch.fieldname] = null;
                                                    }
                                                    else if (ch.type == "Integer")
                                                    {
                                                        if (ch.value != null && !string.IsNullOrEmpty(ch.value.Trim()))
                                                            enchild[ch.fieldname] = Int32.Parse(ch.value);
                                                        else
                                                            enchild[ch.fieldname] = null;
                                                    }
                                                }
                                            }

                                        }
                                        if (isupdate == true)
                                        {
                                            crm.Service.Update(enchild);
                                        }
                                        else
                                        {
                                            guidchild = new Guid();
                                            if (enchild != null)
                                            {
                                                guidchild = crm.Service.Create(enchild);
                                            }
                                        }
                                        // set
                                        pr.Where(x => x.type == "Guid").FirstOrDefault().value = guidchild.ToString();


                                    }
                                    if (en.LogicalName != null)
                                    {
                                        //check quantity in stock case order dms
                                        if (CheckConditonFieldsAfterCreated(en, enchild, guid, guidchild, crm) == false)
                                        {
                                            var updateOrderdms = new Entity(en.LogicalName, guid);
                                            updateOrderdms["bsd_statusorder"] = new OptionSetValue(value: 861450002);
                                            //result.master.fields.Where(x => x.fieldname == "s2s_statusorder").SingleOrDefault().value = "861450002";
                                            crm.Service.Update(updateOrderdms);
                                        }
                                        ConditionModifyEntities(en, guid, crm);
                                    }
                                    string resultjson = ReturnJson(result, crm, en, guid);
                                    //end
                                    mss.Status = "Success";
                                    mss.Data = resultjson;

                                }
                            }
                        }

                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                mss.Status = "Error";
                mss.Content = ex.Message;
            }
            return mss;
        }
        internal static void ConditionModifyEntities(Entity parent, Guid guid, CRMConnector crm)
        {
            switch (parent.LogicalName)
            {
                case "bsd_orderdms":
                    if (parent["bsd_deliverytype"].ToString() == "861450001")
                    {
                        var RFemployee = (EntityReference)parent["bsd_employee"];
                        string xmlproductdms = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_orderproductdms'>
                                            <attribute name='bsd_orderproductdmsid' />
                                            <attribute name='bsd_name' />
                                            <attribute name='createdon' />
                                            <attribute name='bsd_productid' />
                                            <attribute name='bsd_uomid' />
                                            <attribute name='bsd_quantity' />
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_orderdms' operator='eq' uitype='bsd_orderdms' value='" + guid + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";
                        EntityCollection etcorderproductdms = crm.Service.RetrieveMultiple(new FetchExpression(xmlproductdms));
                        foreach (var orderproductdms in etcorderproductdms.Entities)
                        {
                            EntityReference RFproduct = (EntityReference)orderproductdms["bsd_productid"];
                            EntityReference RFunit = (EntityReference)orderproductdms["bsd_uomid"];
                            var quantity = (decimal)orderproductdms["bsd_quantity"];
                            var fetchxml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='bsd_vehiclewarehousedetails'>
                                <attribute name='bsd_vehiclewarehousedetailsid' />
                                <attribute name='bsd_unit' />
                                <attribute name='bsd_restquantity' />
                                <attribute name='bsd_quantity' />
                                <attribute name='bsd_product' />
                                <order attribute='bsd_product' descending='false' />
                                <filter type='and'>
                                  <condition attribute='bsd_product' operator='eq' uitype='product' value='" + RFproduct.Id + @"' />
                                  <condition attribute='bsd_unit' operator='eq' uitype='uom' value='" + RFunit.Id + @"' />
                                </filter>
                                <link-entity name='bsd_vehiclewarehouse' from='bsd_vehiclewarehouseid' to='bsd_vehiclewarehouse' alias='ab'>
                                  <filter type='and'>
                                    <condition attribute='bsd_employee' operator='eq' uitype='bsd_employee' value='" + RFemployee.Id + @"' />
                                  </filter>
                                </link-entity>
                              </entity>
                            </fetch>";
                            EntityCollection etcvehiclewarehousedetails = crm.Service.RetrieveMultiple(new FetchExpression(fetchxml));
                            if (etcvehiclewarehousedetails.Entities.Count > 0)
                            {
                                Entity vehiclewarehousedetails = etcvehiclewarehousedetails.Entities.First();
                                var quantityvhc = (decimal)vehiclewarehousedetails["bsd_quantity"];
                                var quantityrestvhc = (decimal)vehiclewarehousedetails["bsd_restquantity"];
                                Entity updatevehiclewarehousedetails = new Entity(vehiclewarehousedetails.LogicalName, vehiclewarehousedetails.Id);

                                updatevehiclewarehousedetails["bsd_restquantity"] = quantityrestvhc - quantity;
                                updatevehiclewarehousedetails["bsd_quantity"] = quantityvhc - quantity;
                                crm.Service.Update(updatevehiclewarehousedetails);
                            }
                        }

                    }
                    break;
                default:
                    break;
            };
        }
        internal static bool CheckConditonFieldsAfterCreated(Entity parent, Entity child, Guid guid, Guid chidlguid, CRMConnector crm)
        {
            Entity ent = crm.Service.Retrieve(parent.LogicalName, guid, new ColumnSet(true));
            //                ent["bsd_status"]
            var entchild = new Entity(child.LogicalName, chidlguid);
            switch (parent.LogicalName)
            {
                case "bsd_orderdms":
                    string xmlproductdms = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_orderproductdms'>
                                            <attribute name='bsd_orderproductdmsid' />
                                            <attribute name='bsd_name' />
                                            <attribute name='createdon' />
                                            <attribute name='bsd_quantity' />
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_orderdms' operator='eq' uitype='bsd_orderdms' value='" + guid + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";
                    EntityCollection orderproductdms = crm.Service.RetrieveMultiple(new FetchExpression(xmlproductdms));
                    foreach (var productdms in orderproductdms.Entities)
                    {
                        var refwarehouse = (EntityReference)ent["bsd_warehouse"];
                        decimal quantity = decimal.Parse(productdms["bsd_quantity"].ToString());
                        Entity getWarehouse = crm.Service.Retrieve(refwarehouse.LogicalName, refwarehouse.Id, new ColumnSet(true));
                        string xml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_warehourseproductdms'>
                                            <attribute name='bsd_warehourseproductdmsid' />
                                            <attribute name='bsd_name' />
                                            <attribute name='createdon' />
                                            <attribute name='bsd_quantity' />
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_warehouses' operator='eq' uitype='bsd_warehousedms' value='" + getWarehouse.Id + @"' />
                                              <condition attribute='bsd_quantity' operator='gt' value='" + (decimal)productdms["bsd_quantity"] + @"' />
                                              <condition attribute='bsd_name' operator='eq' value='" + productdms["bsd_name"].ToString() + @"' />
                                               <condition attribute='statecode' operator='eq' value='0' />
                                            </filter>
                                          </entity>
                                        </fetch>";
                        EntityCollection products = crm.Service.RetrieveMultiple(new FetchExpression(xml));
                        if (products.Entities.Count > 0)
                        {
                            return true;

                        }
                        else
                        {
                            return false;
                        }
                    }
                    break;
                default:
                    break;
            }
            return true;
        }

        internal static string ReturnJson(RootObject objvalue, CRMConnector crm, Entity en, Guid guid)
        {
            try
            {
                //var jsonSerialiser = new JavaScriptSerializer();
                var ro = new RootObject();
                //ro.details = new List<Detail>();
                //ro.master.fields = new List<Field>();
                //result list
                var fieldList = new List<Field>();
                if (objvalue.master != null)
                {
                    en = crm.Service.Retrieve(objvalue.master.entity, guid, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
                    ro.master = new Master();
                    ro.master.entity = en.LogicalName;
                    foreach (var fields in objvalue.master.fields)
                    {
                        if (fields.type == "Guid")
                        {
                            var field = new Field();
                            string oldvalue = objvalue.master.fields.Where(x => x.type == "Guid").SingleOrDefault().oldvalue;
                            field.type = "Guid";
                            field.fieldname = fields.fieldname;
                            field.value = guid.ToString();
                            field.oldvalue = oldvalue;

                            fieldList.Add(field);
                        }
                        if (fields.fieldname == "s2s_code")
                        {

                            var field = new Field();
                            string value = objvalue.master.fields.Where(x => x.fieldname == "s2s_code").SingleOrDefault().value;
                            field.type = "string";
                            field.fieldname = value;
                            Entity getcode = crm.Service.Retrieve(objvalue.master.entity, guid, new ColumnSet(true));
                            field.value = getcode[value].ToString();
                            // var fieldList = new List<Field>();
                            fieldList.Add(field);

                        }
                        else if (fields.fieldname == "s2s_statusorder")
                        {
                            var field = new Field();
                            // var field = new Field();
                            // string oldvalue = result.master.fields.Where(x => x.fieldname == item.fieldname).SingleOrDefault().value;
                            field.type = "string";
                            field.fieldname = fields.value;
                            Entity getcode = crm.Service.Retrieve(objvalue.master.entity, guid, new ColumnSet(true));
                            field.value = ((OptionSetValue)getcode[fields.value]).Value.ToString();
                            // var fieldList = new List<Field>();
                            fieldList.Add(field);
                        }
                        else if (fields.fieldname == "s2s_status")
                        {
                            var field = new Field();
                            // var field = new Field();
                            // string oldvalue = result.master.fields.Where(x => x.fieldname == item.fieldname).SingleOrDefault().value;
                            field.type = "string";
                            field.fieldname = fields.value;
                            Entity getcode = crm.Service.Retrieve(objvalue.master.entity, guid, new ColumnSet(true));
                            field.value = ((OptionSetValue)getcode[fields.value]).Value.ToString();
                            // var fieldList = new List<Field>();
                            fieldList.Add(field);
                        }
                        else if (fields.fieldname == "s2s_statusdeliverydills2s")
                        {
                            var field = new Field();
                            // var field = new Field();
                            // string oldvalue = result.master.fields.Where(x => x.fieldname == item.fieldname).SingleOrDefault().value;
                            field.type = "string";
                            field.fieldname = fields.value;
                            Entity getcode = crm.Service.Retrieve(objvalue.master.entity, guid, new ColumnSet(true));
                            field.value = ((OptionSetValue)getcode[fields.value]).Value.ToString();
                            // var fieldList = new List<Field>();
                            fieldList.Add(field);
                        }

                    }
                    ro.master.fields = fieldList;
                }
                //set details
                var details = new List<Detail>();
                foreach (var item in objvalue.details)
                {
                    var detail = new Detail();
                    detail.entity = item.entity;
                    var lines = new List<List<Field>>();
                    foreach (var line in item.lines)
                    {
                        // var filedsorderguid = 
                        foreach (var fields in line)
                        {
                            var field = new Field();
                            if (fields.type == "Guid")
                            {
                                var fieldListlines = new List<Field>();
                                field.type = "Guid";
                                field.fieldname = fields.fieldname;
                                field.value = fields.value;
                                field.oldvalue = fields.oldvalue;
                                fieldListlines.Add(field);
                                // ro.detail.lines.Add(fieldListlines);   
                                lines.Add(fieldListlines);
                                break;
                            }
                        }

                    }
                    detail.lines = lines;
                    details.Add(detail);
                }
                ro.details = details;
                string json = string.Format("{0}", JsonConvert.SerializeObject(ro));
                return json;
            }
            catch (Exception ex)
            {
                return ex.Message;
                throw;
            }

        }
        public static DataTable ToDataTable<T>(List<T> items)
        {
            var dataTable = new DataTable(typeof(T).Name);

            //Get all the properties
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in Props)
            {
                //Defining type of data column gives proper data table 
                var type = (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(prop.PropertyType) : prop.PropertyType);
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name, type);
            }
            foreach (T item in items)
            {
                var values = new object[Props.Length];
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null);
                }
                dataTable.Rows.Add(values);
            }
            //put a breakpoint here and check datatable
            return dataTable;
        }
        internal static Entity SetConnectorFields(Entity en, fieldentity item)
        {
            if (item.type == "String")
            {
                en[item.fieldname] = item.value != null ? item.value.Trim() : null;

            }
            else if (item.type == "Picklist")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = new OptionSetValue(int.Parse(item.value));
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "Decimal")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = decimal.Parse(item.value);
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "Boolean")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = bool.Parse(item.value);
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "DateTime")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = DateTime.Parse(item.value);
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "Lookup")
            {
                if (item.value != null && item.value.Length > 0)
                {
                    string nameRef = item.entityReferenceName.Trim();
                    string idRef = item.value.Trim();
                    if (!string.IsNullOrEmpty(idRef))
                        en[item.fieldname] = new EntityReference(nameRef, Guid.Parse(idRef));
                    else en[item.fieldname] = null;
                }
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "Money")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = new Money(decimal.Parse(item.value));
                else en[item.fieldname] = null;
            }
            return en;
        }
        internal static Entity SetFields(Entity en, Field item)
        {
            if (item.type == "String")
            {
                if (item.fieldname.Equals("s2s_code"))
                {
                    //do nothing
                }
                else if (item.fieldname == "s2s_statusorder")
                {
                    //do nothing
                }
                else if (item.fieldname == "s2s_status")
                {
                    //do nothing
                }
                else if (item.fieldname == "s2s_statusdeliverydills2s")
                {
                    //do nothing
                }
                else en[item.fieldname] = item.value != null ? item.value.Trim() : null;

            }
            else if (item.type == "Picklist")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = new OptionSetValue(int.Parse(item.value));
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "Decimal")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = decimal.Parse(item.value);
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "Boolean")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = bool.Parse(item.value);
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "DateTime")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = DateTime.Parse(item.value);
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "Lookup")
            {
                if (item.value != null && item.value.Length > 0)
                {
                    string nameRef = item.entity.Trim();
                    string idRef = item.value.Trim();
                    if (!string.IsNullOrEmpty(idRef))
                        en[item.fieldname] = new EntityReference(nameRef, Guid.Parse(idRef));
                    else en[item.fieldname] = null;
                }
                else
                    en[item.fieldname] = null;
            }
            else if (item.type == "Money")
            {
                if (item.value != null && !string.IsNullOrEmpty(item.value.Trim()))
                    en[item.fieldname] = new Money(decimal.Parse(item.value));
                else en[item.fieldname] = null;
            }
            return en;
        }
        internal static Message Test(Message data)
        {
            Message mss = new Message();
            try
            {
                mss.Status = "Success";
                if (data.Data != null && data.Data.Length > 0)
                {
                    XmlDocument xdoc = new XmlDocument();
                    xdoc = JsonConvert.DeserializeXmlNode(data.Data, "entity");
                    XmlNode xid = xdoc.SelectSingleNode("entity/enId");
                    XmlNode xEn = xdoc.SelectSingleNode("entity/enName");
                    if (xEn == null && xEn.InnerXml.Trim().Length > 0)
                        throw new Exception("Entity name can't not be null!");
                    Entity en = new Entity(xEn.InnerText);
                    xdoc.FirstChild.RemoveChild(xEn);
                    bool isUpdate = false;
                    if (xid != null)
                    {
                        if (xid.InnerXml.Trim().Length > 0)
                        {
                            isUpdate = true;
                            en.Id = Guid.Parse(xid.InnerText);
                        }
                        xdoc.FirstChild.RemoveChild(xid);
                    }
                    EntityReference bu = null;

                    XmlNode xEntity = xdoc.FirstChild;
                    string code = "";
                    foreach (XmlNode node in xEntity.ChildNodes)
                    {
                        string type = node.SelectSingleNode("Type").InnerText;
                        XmlNode value = node.SelectSingleNode("Value");
                        if (type == "String")
                        {
                            if (node.Name.Equals("s2s_code"))
                            {
                                code = value.InnerText;
                            }
                            else
                            {
                                en[node.Name] = value != null ? value.InnerText.Trim() : null;
                            }
                        }
                        else if (type == "Lookup")
                        {
                            if (value != null && value.ChildNodes.Count > 0)
                            {
                                string nameRef = value.SelectSingleNode("logicalName").InnerText.Trim();
                                string idRef = value.SelectSingleNode("id").InnerText.Trim();
                                //if (idRef.IndexOf('{') < 0)
                                //    idRef = idRef;
                                if (!string.IsNullOrEmpty(idRef))
                                    en[node.Name] = new EntityReference(nameRef, Guid.Parse(idRef));
                            }
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Decimal")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = decimal.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Double")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = double.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Integer")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = int.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Money")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = new Money(decimal.Parse(value.InnerText));
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Picklist")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = new OptionSetValue(int.Parse(value.InnerText));
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Boolean")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = bool.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "DateTime")
                        {
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = DateTime.Parse(value.InnerText);
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Byte[]")
                        {
                            byte[] databye = System.Convert.FromBase64String(value.InnerText);
                            if (value != null && !string.IsNullOrEmpty(value.InnerText.Trim()))
                                en[node.Name] = databye;
                            else
                                en[node.Name] = null;
                        }
                        else if (type == "Image")
                        {
                            string s = value.InnerText;
                            s += "";
                        }
                    }

                    mss.Status = "Success";
                    mss.Data = "123-123-123-123";
                }
            }
            catch (Exception ex)
            {
                mss.Status = "Error";
                mss.Content = ex.Message;
            }
            return mss;
        }
    }
}