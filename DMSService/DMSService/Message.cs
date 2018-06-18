using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class Message
    {
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public string Data { get; set; }

        [DataMember]
        public string Content { get; set; }
    }
}