using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class ItemSalesTaxGroup
    {
        [DataMember]
        public string bsd_name { get; set; }
        [DataMember]
        public string bsd_code { get; set; }
        [DataMember]
        public string bsd_descriptionitem { get; set; }
        [DataMember]
        public string bsd_saletaxcode { get; set; }
        [DataMember]
        public decimal bsd_percentageamount { get; set; }
        [DataMember]
        public string bsd_description { get; set; }
        [DataMember]
        public string Recid { get; set; }
    }
}