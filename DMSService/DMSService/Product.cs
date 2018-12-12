using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class Product
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string productnumber { get; set; }
        [DataMember]
        public string bsd_itemsalestaxgroup { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public string defaultuomid { get; set; }
        [DataMember]
        public string defaultuomscheduleid { get; set; }
        [DataMember]
        public string bsd_weight { get; set; }
        [DataMember]
        public string bsd_manufactory { get; set; }
        [DataMember]
        public string bsd_brand { get; set; }
        [DataMember]
        public string bsd_configuration { get; set; }
        [DataMember]
        public string bsd_packaging { get; set; }
        [DataMember]
        public string bsd_packing { get; set; }
        [DataMember]
        public string bsd_size { get; set; }
        [DataMember]
        public string bsd_style { get; set; }
        [DataMember]
        public string divisionname { get; set; }
        [DataMember]
        public string management { get; set; }
        [DataMember]
        public string company { get; set; }
        [DataMember]
        public string productsize2 { get; set; }
        [DataMember]
        public string productsize3 { get; set; }
        [DataMember]
        public string subcategory1 { get; set; }
        [DataMember]
        public string companybrand { get; set; }
        [DataMember]
        public string subbrandname { get; set; }
        [DataMember]
        public string grand { get; set; }
    }
}