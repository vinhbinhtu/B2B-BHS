using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class SaleTaxGroup
    {
        [DataMember]
        public string bsd_name { get; set; }
        [DataMember]
        public string Recid { get; set; }
        [DataMember]
        public string bsd_salestaxgroup { get; set; }
        [DataMember]
        public int bsd_type { get; set; }
        [DataMember]
        public string bsd_description { get; set; }
      
    }
}