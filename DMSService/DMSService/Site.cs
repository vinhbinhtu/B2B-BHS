using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class Site
    {
        [DataMember]
        public string bsd_code { get; set; }
        [DataMember]
        public string bsd_name { get; set; }
        [DataMember]
        public string bsd_companyname { get; set; }
        [DataMember]
        public string bsd_address { get; set; }
        [DataMember]
        public string Recid { get; set; }
    }
}