using System.Runtime.Serialization;

namespace DMSService
{
    [DataContract]
    public class GoodsIssueNoteProduct
    {
        [DataMember]
        public string productnumber { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public string Unit { get; set; }
        [DataMember]
        public decimal Quantity { get; set; }
        [DataMember]
        public string Delivered { get; set; }
        [DataMember]
        public string DeliverySchedule { get; set; }
        [DataMember]
        public string RequestDelivery { get; set; }
        [DataMember]
        public string Warehouse { get; set; }
    }
}