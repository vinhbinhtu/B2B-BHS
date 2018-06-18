using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class GroupUnit
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string baseuomname { get; set; }
        
    }
}