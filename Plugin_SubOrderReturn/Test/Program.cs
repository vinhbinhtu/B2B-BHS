using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            decimal f =decimal.Parse( "154980.22");
          Console.WriteLine( ( f*-1).ToString("N", new CultureInfo("is-IS")));
        }
    }
}
