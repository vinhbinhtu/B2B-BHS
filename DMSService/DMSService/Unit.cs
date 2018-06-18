using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class Unit
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public decimal quantity { get; set; }
        [DataMember]
        public string baseuom { get; set; }
    }
}