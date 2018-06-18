using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class InvoiceSuborder
    {
        [DataMember]
        public string RecId { get; set; }
        [DataMember]
        public string SuborderID { get; set; }
        [DataMember]
        public string Serial { get; set; }
        [DataMember]
        public string CustomerCode { get; set; }
        [DataMember]
        public string CustomerName { get; set; }
        [DataMember]
        public string Invoice { get; set; }
        [DataMember]
        public string Warehouse { get; set; }
        [DataMember]
        public DateTime InvoiceDate { get; set; }
        [DataMember]
        public DateTime Date { get; set; }
        [DataMember]
        public decimal TotalAmount { get; set; }
        [DataMember]
        public decimal TotalTax { get; set; }
        [DataMember]
        public decimal ExtendedAmount { get; set; }
        [DataMember]
        public decimal ExchangeRate { get; set; }
        [DataMember]
        public DateTime PaymentDate { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Currency { get; set; }
        [DataMember]
        public List<PackingList> PackingList { get; set; }
    }
}