using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryPlugin.Model
{
    public class ThongTinTonKho
    {
        public string ProductName { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal SuborderQuantity { get; set; }
        public decimal SynQuantity { get; set; }
    }
}
