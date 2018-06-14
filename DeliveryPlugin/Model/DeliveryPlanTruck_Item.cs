using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryPlugin.Model
{
    public class DeliveryPlanTruck_Item
    {
        public DeliveryPlanTruck_Item()
        {
            bsd_driver = "";
            ShippingDeliveryMethod = 0;
            bsd_deliverytruck = bsd_carrierpartner = truckload = Guid.Empty;
            shipping_option = false;
        }
        public Guid Id { get; set; }
        public Guid? bsd_deliverytruck { get; set; }
        public Guid? bsd_carrierpartner { get; set; }
        public string bsd_licenseplate { get; set; }
        public string bsd_driver { get; set; }
        public string bsd_historyshipper { get; set; }
        public int bsd_deliverytrucktype { get; set; }
        public bool shipping_option { get; set; }
        public int ShippingDeliveryMethod { get; set; }
        public Guid truckload { get; set; }
    }
}
