using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class PaymentTerm
    {
        [DataMember]
        public string bsd_termofpayment { get; set; }
        [DataMember]
        public Int32 bsd_date { get; set; }
        [DataMember]
        public string bsd_description { get; set; }
        [DataMember]
        public string Recid { get; set; }

    }
}