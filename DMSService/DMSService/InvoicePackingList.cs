using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class InvoicePackingList
    {
        [DataMember]
        public string bsd_invoicenumber { get; set; }
        [DataMember]
        public string bsd_invoiceno { get; set; }
        [DataMember]

        public List<PackingList> PackingList { get; set; }
    }
}