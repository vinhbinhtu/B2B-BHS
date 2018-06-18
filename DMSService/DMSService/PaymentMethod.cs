using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class PaymentMethod
    {
        [DataMember]
        public string bsd_methodofpayment { get; set; }
        [DataMember]
        public string bsd_description { get; set; }
        [DataMember]
        public string Recid { get; set; }
    }
}