using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace DMSService
{
    [DataContract]
    public class ImportDeclaration
    {
        [DataMember]
        public string bsd_importdeclaration { get; set; }
        [DataMember]
        public DateTime bsd_date { get; set; }
        [DataMember]
        public string Recid { get; set; }
        [DataMember]
        public string bsd_typedeclaration { get; set; }
        [DataMember]
        public string bsd_description { get; set; }
    }
}        //[DataMember]
        //[DataMember]
        //[DataMember]