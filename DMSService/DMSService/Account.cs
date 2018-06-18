using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class Account
    {
        //bsd_brand
        [DataMember]
        public string accountnumber { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string bsd_accountgroup { get; set; }
        [DataMember]
        public string bsd_accounttype { get; set; }
        [DataMember]
        public string bsd_taxregistration { get; set; }
        [DataMember]
        public string bsd_saletaxgroup { get; set; }
        [DataMember]
        public string bsd_paymentterm { get; set; }
        [DataMember]
        public string bsd_paymentmethod { get; set; }
       
    }
}        //[DataMember]
        //[DataMember]
        //[DataMember]