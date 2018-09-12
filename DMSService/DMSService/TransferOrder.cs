using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class TransferOrder
    {
        [DataMember]
        public string bsd_name { get; set; }
        [DataMember]
        public DateTime bsd_shipdate { get; set; }
        [DataMember]
        public DateTime bsd_receiptdate { get; set; }
        [DataMember]
        public string bsd_tosite { get; set; }
        [DataMember]
        public string bsd_towarehouse { get; set; }
        [DataMember]
        public string bsd_fromsite { get; set; }
        [DataMember]
        public string bsd_description { get; set; }

        [DataMember]
        public string bsd_fromwarehouse { get; set; }
      
        [DataMember]
        public List<TransferOrderProduct> TransferOrderProduct { get; set; }
        [DataMember]
        public string RecId { get; set; }
        [DataMember]
        public string bsd_fromaddressid { get; set; }
        [DataMember]
        public string bsd_toaddressid { get; set; }
        [DataMember]
        public string bsd_vendorid { get; set; }
        [DataMember]
        public string bsd_vendorname { get; set; }
        [DataMember]
        public string bsd_vendoraddress { get; set; }
        [DataMember]
        public string status { get; set; }

    }
}