using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_DeleteMasterDataAX.Service
{
    public class Util
    {
        public static Entity Convert(Entity from, string id)
        {
            Entity to = new Entity(from.LogicalName);
            foreach (var attr in from.Attributes.Where(x => x.Key != id))
            {
                if (from.Contains(attr.Key) && from[attr.Key] != null)
                {
                    to[attr.Key] = from[attr.Key];
                }
            }
            return to;
        }
        public static string getRole()
        {

            //        var xml = ['<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">',
            //    '<entity name="role">',
            //' <attribute name="parentrootroleid"/>',
            //        ' <link-entity name="systemuserroles" from="roleid" to="roleid">',
            //             '<filter>',
            //               ' <condition attribute="systemuserid" operator="eq" value="92E088A8-7329-E711-93F4-000C2958218C" />',
            //            '</filter>',
            //         '</link-entity>',
            //'</entity>',
            //'</fetch>'].join('');

            return "Nhân viên";

        }
    }

    public static class Util2
    {
        public static bool HasValue(this Entity en, string s)
        {
            return en.Contains(s) && en[s] != null;
        }
    }
}
