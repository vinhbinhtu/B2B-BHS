using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Plugin.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryPlugin
{
    public class UomService
    {
        private decimal quantity;
        private IOrganizationService service;
        public UomService(IOrganizationService _service)
        {
            this.service = _service;
        }
        public decimal QuyDoiSangkg(Entity uom, decimal _quantity)
        {
            quantity = _quantity * (decimal)uom["quantity"];
            DeQuyQuantity(uom);
            return quantity;
        }
        private void DeQuyQuantity(Entity entity)
        {

            if (entity.HasValue("baseuom"))
            {
                EntityReference baseuomRef = ((EntityReference)entity["baseuom"]);
                Entity baseuom = service.Retrieve(baseuomRef.LogicalName, baseuomRef.Id, new ColumnSet(true));
                quantity = quantity * ((decimal)baseuom["quantity"]);
                DeQuyQuantity(baseuom);
            }
        }
    }
}
