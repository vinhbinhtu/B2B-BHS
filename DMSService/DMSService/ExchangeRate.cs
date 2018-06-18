using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class ExchangeRate
    {
        [DataMember]
        public string bsd_name { get; set; }
        [DataMember]
        public DateTime bsd_date { get; set; }
        [DataMember]
        public string bsd_bankaccount { get; set; }
        [DataMember]
        public string Recid { get; set; }
        [DataMember]
        public string bsd_currencyfrom { get; set; }
        [DataMember]
        public string bsd_currencyto { get; set; }
        [DataMember]
        public Decimal bsd_exchangerate { get; set; }
    }
}