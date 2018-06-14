using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Service;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Client;

namespace DeliveryPlugin
{
    public class SubOrderDebt : IPlugin
    {
        MyService myService;
        //  công nợ và tồn kho . khi submit lần đầu và khi bị reject lại xong submite
        public void Execute(IServiceProvider serviceProvider)
        {
            myService = new MyService(serviceProvider);

            if (myService.context.MessageName == "Update")
            {
                Entity target = myService.getTarget();
                if (target.HasValue("bsd_skipplugin") && (bool)target["bsd_skipplugin"]) return;
                myService.StartService();
                if (!target.HasValue("statuscode")) return;

                Service.SuborderService subService = new Service.SuborderService(myService);

                Entity suborder = myService.service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                Entity pre_suborder = myService.context.PreEntityImages["PreImage"];
                int pre_statuscode = ((OptionSetValue)pre_suborder["statuscode"]).Value;
                int new_statuscode = ((OptionSetValue)target["statuscode"]).Value;
                if (pre_statuscode == new_statuscode) return;

                Entity configdefault = myService.FetchXml(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                  <entity name='bsd_configdefault'>
                    <attribute name='bsd_configdefaultid' />
                    <attribute name='createdon' />
                    <attribute name='bsd_checkcustomerbalance' />
                    <order attribute='createdon' descending='true' />
                  </entity>
                </fetch>").Entities.FirstOrDefault();
                bool check_customerbalance = configdefault.HasValue("bsd_checkcustomerbalance") ? (bool)configdefault["bsd_checkcustomerbalance"] : true;
                if (!check_customerbalance) return;

                EntityReference owner = (EntityReference)suborder["ownerid"];
                string role = subService.GetRoleName(owner.Id, myService.service);

                EntityReference account_ref = (EntityReference)suborder["bsd_potentialcustomer"]; ;
                Entity customerdebt = subService.GetCustomerDebtByTimeAndAccount(account_ref.Id, DateTime.Now);
                if (customerdebt == null) throw new Exception("Account has no Customer Debt");
                decimal current_debt = customerdebt.HasValue("bsd_newdebt") ? ((Money)customerdebt["bsd_newdebt"]).Value : 0m;
                decimal grand_total = ((Money)suborder["bsd_totalcurrencyexchange"]).Value;

                #region Kiểm tra submit lần đầu.
                bool staff_submit = pre_statuscode == 861450000 && new_statuscode == 861450003 && role == "Nhân Viên"; // Staff Create => Manager Pendding Approval
                bool manager_submit = pre_statuscode == 861450001 && new_statuscode == 861450004 && role == "Trưởng Phòng"; // Manager Create => Chief Offcie Pendding Approval

                if (staff_submit || manager_submit)
                {
                    #region Cập nhật công nợ
                    Entity new_customerdebt = new Entity(customerdebt.LogicalName, customerdebt.Id);
                    decimal new_debt = current_debt + grand_total;
                    new_customerdebt["bsd_newdebt"] = new Money(new_debt);
                    myService.service.Update(new_customerdebt);
                    #endregion

                    #region cập nhật submitted quantity và submitted customerdebt
                    Entity update_suborder = new Entity(suborder.LogicalName, suborder.Id);
                    update_suborder["bsd_submittedgrandtotal"] = new Money(grand_total);
                    update_suborder["bsd_submittedgrandtotaltext"] = grand_total.DecimalToStringHideSymbol();
                    update_suborder["bsd_customerdebt"] = new EntityReference(customerdebt.LogicalName, customerdebt.Id);
                    update_suborder["bsd_submittedcustomerdebt"] = new EntityReference(customerdebt.LogicalName, customerdebt.Id);
                    myService.Update(update_suborder);
                    #endregion

                    Update_SuborderProduct_SubmitedQuantity(suborder.Id);
                }
                else
                {

                    // lần 2
                    bool staff_submit_2 = pre_statuscode == 861450005 && new_statuscode == 861450003 && role == "Nhân Viên"; // Manager Unapprove => Manager Pendding Approval
                    bool manager_submit_2 = pre_statuscode == 861450006 && new_statuscode == 861450004 && role == "Trưởng Phòng"; // Chief office unapprove => Chief Offcie Pendding Approval
                    if (staff_submit_2 || manager_submit_2)
                    {
                        EntityReference submitted_customerdebt_ref = (EntityReference)suborder["bsd_submittedcustomerdebt"];
                        decimal submitted_grandtotal = ((Money)suborder["bsd_submittedgrandtotal"]).Value;

                        #region Cập nhật công nợ
                        Entity new_customerdebt = new Entity(customerdebt.LogicalName, customerdebt.Id);
                        decimal new_debt = 0m;

                        // Nếu cái đã submit lên trước khi reject bằng với bảng công nợ đang submit. thì trừ submited_grandtotal
                        if (submitted_customerdebt_ref.Id.Equals(customerdebt.Id))
                        {
                            new_debt = (current_debt - submitted_grandtotal) + grand_total;
                        }
                        else // ngược lại nó khác nhau thì không trừ cái đã submit, mà chỉ cộng cái grand total mới.
                        {
                            new_debt = current_debt + grand_total;
                        }

                        new_customerdebt["bsd_newdebt"] = new Money(new_debt);
                        myService.service.Update(new_customerdebt);
                        #endregion

                        #region cập nhật submitted quantity vaf submitted customerdebt
                        Entity update_suborder = new Entity(suborder.LogicalName, suborder.Id);
                        update_suborder["bsd_submittedgrandtotal"] = new Money(grand_total);
                        update_suborder["bsd_submittedgrandtotaltext"] = grand_total.DecimalToStringHideSymbol();
                        update_suborder["bsd_customerdebt"] = new EntityReference(customerdebt.LogicalName, customerdebt.Id);
                        update_suborder["bsd_submittedcustomerdebt"] = new EntityReference(customerdebt.LogicalName, customerdebt.Id);
                        myService.Update(update_suborder);
                        #endregion

                        Update_SuborderProduct_SubmitedQuantity(suborder.Id);
                    }
                }
                #endregion
            }
        }

        public void Update_SuborderProduct_SubmitedQuantity(Guid suborderid)
        {
            EntityCollection list_suborderproduct = myService.RetrieveOneCondition("bsd_suborderproduct", "bsd_suborder", suborderid);
            foreach (var item in list_suborderproduct.Entities)
            {
                Entity update_suborderproduct = new Entity(item.LogicalName, item.Id);
                update_suborderproduct["bsd_submittedquantity"] = item["bsd_totalquantity"];
                myService.Update(update_suborderproduct);
            }
        }
    }
}
