using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class Currency
    {
        [DataMember]
        public string isocurrencycode { get; set; }
        [DataMember]
        public string currencyname { get; set; }
        [DataMember]
        public string currencysymbol { get; set; }
        [DataMember]
        public Int32 currencyprecision { get; set; }
        [DataMember]
        public decimal exchangerate { get; set; }
        
    }
}