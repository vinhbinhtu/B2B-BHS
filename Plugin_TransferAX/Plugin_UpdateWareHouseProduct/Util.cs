using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin_UpdateWareHouseProduct
{
    public static class Util
    {
        public static void Throw(this object obj)
        {
            throw new Exception(obj.ToString());
        }
    }
    
}
