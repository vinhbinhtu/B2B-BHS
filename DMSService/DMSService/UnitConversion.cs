using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class UnitConversion
    {
        [DataMember]
        public string productnumber { get; set; }
        [DataMember]
        public string bsd_name { get; set; }
        [DataMember]
        public string bsd_product { get; set; }
        [DataMember]
        public string bsd_fromunit { get; set; }
        [DataMember]
        public decimal bsd_factor { get; set; }
        [DataMember]
        public string bsd_tounit { get; set; }
        [DataMember]
        public string bsd_description { get; set; }
        [DataMember]
        public string Recid { get; set; }

    }
}