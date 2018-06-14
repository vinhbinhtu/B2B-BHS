using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryPlugin.Model
{
    public class OrderProduct_Custom
    {
        public bool ShippingYesNo { get; set; }
        public OrderProduct_Custom()
        {
            DeliveryFrom = 0;
            Site = SiteAddress = ReceiptCustomer = ShippingAddress = ShippingFromAddress = CustomerAddress = Port = AddressPort = Guid.Empty;
            ShippingYesNo = false;
        }
        public bool UsingTax { get; set; }
        public decimal Item_Sales_Tax { get; set; }
        public bool iswarehouse { get; set; }
        public Guid ShippingFromAddress { get; set; } // Địa chỉ xuất
        public Guid ShippingAddress { get; set; } // địa chỉ giao
        public Guid Site { get; set; }
        public Guid SiteAddress { get; set; }
        public int DeliveryFrom { get; set; }
        public Guid ReceiptCustomer { get; set; }
        public Guid CustomerAddress { get; set; }
        public Guid Port { get; set; }
        public Guid AddressPort { get; set; }
    }
}
