using System.Runtime.Serialization;

namespace DMSService
{
    [DataContract]
    public class PackingList
    {
        [DataMember]
        public string PackingListID { get; set; }
        
    }
}