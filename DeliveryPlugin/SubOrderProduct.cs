using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk.Messages;
using DeliveryPlugin.Service;

namespace DeliveryPlugin
{
    public class SubOrderProduct : IPlugin
    {
        MyService myService;
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);
            if (myService.context.Depth > 1) return;

            #region Create
            if (myService.context.MessageName == "Create")
            {
                myService.StartService();
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_skipplugin") && (bool)target["bsd_skipplugin"]) return;
                Entity suborder_product = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                Guid suborderid = ((EntityReference)suborder_product["bsd_suborder"]).Id;
                if (checkSubmit_SubOder(suborderid))
                {
                    throw new Exception("Can't add suborder product!");
                }
                #region Kiểm tra tạo từ b2c
                Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)suborder_product["bsd_suborder"]).Id, new ColumnSet("bsd_fromb2c"));
                if (suborder.HasValue("bsd_fromb2c") && (bool)suborder["bsd_fromb2c"])
                {

                    return;
                }
                #endregion
                SuborderService suborderService = new SuborderService(myService);
                suborderService.Create_Update_Suborder_Product(suborder_product, 1, true);

                // lấy suborderproduct của suborder này, và khác thằng hiện tại !
                QueryExpression q = new QueryExpression("bsd_suborderproduct");
                q.ColumnSet = new ColumnSet(true);
                FilterExpression f = new FilterExpression();
                f.AddCondition(new ConditionExpression("bsd_suborder", ConditionOperator.Equal, ((EntityReference)suborder_product["bsd_suborder"]).Id));
                f.AddCondition(new ConditionExpression("bsd_suborderproductid", ConditionOperator.NotEqual, suborder_product.Id));
                q.Criteria = f;

                EntityCollection list_suborderproduct = myService.service.RetrieveMultiple(q);
                // cập nhật ở trên kia rồi mới xuống dưới tính
                if (list_suborderproduct.Entities.Any())
                {
                    // Cập nhật lại
                    suborder_product = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                    Entity old_suborder = list_suborderproduct.Entities.First();
                    decimal tax_old = old_suborder.HasValue("bsd_itemsalestax") ? (decimal)old_suborder["bsd_itemsalestax"] : 0;
                    decimal tax_new = suborder_product.HasValue("bsd_itemsalestax") ? (decimal)suborder_product["bsd_itemsalestax"] : 0;
                    if (tax_new != tax_old)
                    {
                        throw new Exception("Tax dont match");
                    }
                }
            }
            #endregion

            #region update
            if (myService.context.MessageName == "Update")
            {
                myService.StartService();
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_skipplugin") && (bool)target["bsd_skipplugin"]) return;
                Entity suborderproduct = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                Guid suborderid = ((EntityReference)suborderproduct["bsd_suborder"]).Id;
                if (checkSubmit_SubOder(suborderid))
                {
                    //throw new Exception("Can't edit suborder product!");
                }
                if (!suborderproduct.HasValue("bsd_appendixcontract"))
                {
                    Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)suborderproduct["bsd_suborder"]).Id, new ColumnSet("bsd_fromb2c", "bsd_duyet", "statuscode"));

                    int statuscode = ((OptionSetValue)suborder["statuscode"]).Value;
                    bool duyet = suborder.HasValue("bsd_duyet") ? (bool)suborder["bsd_duyet"] : false;
                    if (duyet == true || statuscode == 861450002) return;

                    #region Kiểm tra tạo từ b2c
                    if (suborder.HasValue("bsd_fromb2c") && (bool)suborder["bsd_fromb2c"])
                    {
                        return;
                    }
                    #endregion

                    var type = ((OptionSetValue)suborderproduct["bsd_type"]).Value;
                    if (type == 861450000 || type == 861450001 || type == 861450002)
                    {
                        SuborderService suborderService = new SuborderService(myService);
                        Entity pre_image = myService.context.PreEntityImages["PreImage"];
                        decimal pre_quantity = (decimal)pre_image["bsd_shipquantity"];
                        suborderService.Create_Update_Suborder_Product(suborderproduct, 2, true, pre_quantity);
                    }
                }
            }
            #endregion

            #region Delete
            else if (myService.context.MessageName == "Delete")
            {
                myService.StartService();
                Entity suborderproduct = myService.context.PreEntityImages["PreImage"];

                Entity suborder = myService.service.Retrieve("bsd_suborder", ((EntityReference)suborderproduct["bsd_suborder"]).Id, new ColumnSet("statecode", "bsd_fromb2c"));

                int status = ((OptionSetValue)suborder["statecode"]).Value;
                if (status == 1) return;

                if (!suborderproduct.HasValue("bsd_appendixcontract")) // bawngf null thi chay
                {
                    #region Kiểm tra tạo từ b2c
                    if (suborder.HasValue("bsd_fromb2c") && (bool)suborder["bsd_fromb2c"])
                    {
                        return;
                    }
                    #endregion

                    int type = ((OptionSetValue)suborderproduct["bsd_type"]).Value;
                    if (type == 861450000 || type == 861450001 || type == 861450002)
                    {
                        SuborderService suborderService = new SuborderService(myService);
                        suborderService.DeleteSuborderProduct(suborderproduct);
                    }
                }
            }
            #endregion
        }

        public bool checkSubmit_SubOder(Guid suborderid)
        {
            bool checksubmit = false;
            Entity suborder = myService.service.Retrieve("bsd_suborder", suborderid, new ColumnSet(true));
            int status_reason = ((OptionSetValue)suborder["statuscode"]).Value;
            bool check_staffapprove = suborder.HasValue("bsd_tennhanvienduyet") ? true : false;
            bool check_managerapprove = suborder.HasValue("bsd_tentruongphongduyet") ? true : false;
            bool check_approve = (bool)suborder["bsd_duyet"];

            if (!check_approve && ((!check_staffapprove && status_reason == 861450000) ||//nhan vien create
                    (!check_staffapprove && !check_managerapprove && status_reason == 861450001) ||//truong phong create
                    (check_staffapprove && status_reason == 861450005) ||//nhanvien create + truong phong unapprove
                    (!check_staffapprove && check_managerapprove && status_reason == 861450006)))// truongphong create + giamdoc unapprove
            {
                checksubmit = false;
            }
            else
            {
                checksubmit = true;
            }

            return checksubmit;
        }
    }
}
