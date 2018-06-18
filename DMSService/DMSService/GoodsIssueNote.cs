using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class GoodsIssueNote
    {
        [DataMember]
        public string PackingslIp { get; set; }
        [DataMember]
        public string InvoiceAccount { get; set; }
        [DataMember]
        public string InvoicingName { get; set; }
        [DataMember]
        public string Address { get; set; }
        [DataMember]
        public string Site { get; set; }
        [DataMember]
        public string Warehouse { get; set; }
        [DataMember]
        public DateTime RequestShipDate { get; set; }
        [DataMember]
        public DateTime RequestreceiptDate { get; set; }
        [DataMember]
        public List<GoodsIssueNoteProduct> GoodsIssueNoteProduct{ get;set;}
        [DataMember]
        public string issuenoteax { get; set; }
        [DataMember]
        public string suborderid { get; set; }
    }
}