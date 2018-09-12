using Microsoft.Xrm.Sdk.Client;
using System;
using System.ServiceModel.Description;
using System.Configuration;

namespace DMSService
{
    internal class CRMConnector
    {
        private OrganizationServiceProxy _service;
        public OrganizationServiceProxy Service { get { return _service; } }
        public void ConnectToCrm()
        {
            try
            {
                string port = GetConfig("port");
                if (port == "80")
                    port = "";
                else
                    port = ":" + port;
                Uri crmUrl = new Uri(string.Format("{0}://{1}{2}/{3}/{4}"
                //Uri crmUrl = new Uri(string.Format("{0}://{1}{2}/{3}"
                    , GetConfig("protocol")
                    , GetConfig("server")
                    , port
                    , GetConfig("org")
                    , GetConfig("servicePath")));
                ClientCredentials credential = new ClientCredentials();
                credential.UserName.UserName = GetConfig("userName");
                credential.UserName.Password = GetConfig("password");
                _service = new OrganizationServiceProxy(crmUrl, null, credential, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public void speceficConnectToCrm(string org)
        {
            try
            {
              //org = "B2B";// cái này dùng cho con nhà minh, qua con khach hàng comment lại rồi build lại dll
                string port = GetConfig("port");
                if (port == "80")
                    port = "";
                else
                    port = ":" + port;
                Uri crmUrl = new Uri(string.Format("{0}://{1}{2}/{3}/{4}"
                //Uri crmUrl = new Uri(string.Format("{0}://{1}{2}/{3}"
                    , GetConfig("protocol")
                    , GetConfig("server")
                    , port
                    , org
                    , GetConfig("servicePath")));
                // Uri crmUrl = new Uri("https://crmtrainning.ttcsugar.com.vn/BHSAX/XRMServices/2011/Organization.svc");
                ClientCredentials credential = new ClientCredentials();
                credential.UserName.UserName = GetConfig("userName");
                credential.UserName.Password = GetConfig("password");
                _service = new OrganizationServiceProxy(crmUrl, null, credential, null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private string GetConfig(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
