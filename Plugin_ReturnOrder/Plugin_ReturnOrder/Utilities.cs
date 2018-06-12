using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
namespace Plugin_ReturnOrder
{
    public static class Utilities
    {
        public static bool HasValue(this Entity target, string attributes)
        {
            if (target.Contains(attributes) && target[attributes] != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
