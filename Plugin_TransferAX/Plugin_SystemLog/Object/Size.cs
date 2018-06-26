using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_SystemLog
{
    public class Size
    {

        public string bsd_code { get; set; }
        public string bsd_name { get; set; }
        public string Recid { get; set; }
        public static Size JsonParse(string jsonObject)
        {
            Size size;
            using (MemoryStream DeSerializememoryStream = new MemoryStream())
            {
                //initialize DataContractJsonSerializer object and pass Student class type to it
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Size));

                //user stream writer to write JSON string data to memory stream
                StreamWriter writer = new StreamWriter(DeSerializememoryStream);
                writer.Write(jsonObject);
                writer.Flush();
                DeSerializememoryStream.Position = 0;
                //get the Desrialized data in object of type Student
                size = (Size)serializer.ReadObject(DeSerializememoryStream);
            }
            return size;
        }
        public static void Create(Size obj, IOrganizationService service)
        {
            Entity entity = new Entity("bsd_size");
            entity["bsd_codeax"] = obj.Recid;
            entity["bsd_name"] = obj.bsd_name;
            entity["bsd_code"] = obj.bsd_code;
            service.Create(entity);
        }
        public static void Update(Size obj, IOrganizationService service)
        {
            Entity entity = new Entity("bsd_size");
            entity["bsd_codeax"] = obj.Recid;
            entity["bsd_name"] = obj.bsd_name;
            entity["bsd_code"] = obj.bsd_code;
            service.Create(entity);
        }
        public static void Delete(Size obj, IOrganizationService service)
        {
            Entity entity = new Entity("bsd_size");
            entity["bsd_codeax"] = obj.Recid;
            entity["bsd_name"] = obj.bsd_name;
            entity["bsd_code"] = obj.bsd_code;
            service.Create(entity);
        }
    }
}
