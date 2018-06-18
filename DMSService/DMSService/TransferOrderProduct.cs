using System.Runtime.Serialization;

namespace DMSService
{
    [DataContract]
    public class TransferOrderProduct
    {
        [DataMember]
        public string productnumber { get; set; }
        [DataMember]
        public string bsd_unit { get; set; }
        [DataMember]
        public decimal bsd_quantity { get; set; }
        [DataMember]
        public string RecId { get; set; }
        [DataMember]
        public bool bsd_deliveryfee { get; set; }
        [DataMember]
        public string bsd_deliverymethod { get; set; }
        [DataMember]
        public string bsd_carrierpartner { get; set; }
        [DataMember]
        public string bsd_licenseplate { get; set; }
        [DataMember]
        public string bsd_driver { get; set; }
        [DataMember]
        public bool bsd_porter { get; set; }
    }
}