using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Web;

namespace DMSService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "DMS" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select DMS.svc or DMS.svc.cs at the Solution Explorer and start debugging.
    public class DMS : IDMS
    {
        public void DoWork()
        {
        }

        public string Retrieve(string etn, string id, string[] cols)
        {
            return CrmProvider.RetrieveEntity(etn, id, cols);
        }

        public string RetrieveMul(string fetch)
        {
            return CrmProvider.RetrieveMultiple(fetch);
        }

        public string RetrieveMulPage(string fetch, int pageIndex, int pageSize)
        {
            return CrmProvider.RetrieveMultiPage(fetch, pageIndex, pageSize);
        }

        public Message SaveFormEn(Message data)
        {
            return CrmProvider.SaveForm(data);
        }

        public Message SaveFormListEn(Message data)
        {
            return CrmProvider.SaveListEntity(data);
        }
        public Message Delete(string etn, string id)
        {
            return CrmProvider.DeleteForm(etn, id);
        }

        public Message Test(Message data)
        {
            return CrmProvider.Test(data);
        }

        public string Say(string text, string text2)
        {
            return text;
        }

        public string UploadReturn()
        {
            string path = @"D:\\screen696x696.jpeg";
            if (File.Exists(path))
            {
                byte[] imageArray = File.ReadAllBytes(path);
                var ms = new MemoryStream(imageArray);
                Image returnImage = Image.FromStream(ms);
                returnImage.Save(filename: @"D:\\115656.jpeg");
            }
            string path2 = "../image/115656.jpeg";
            return path2;
        }
        public string uploadFile1(string param1, string param2, string param3)
        {
            try
            {
                // byte[] imageArray = File.ReadAllBytes(filepath);
                byte[] toBytes = Convert.FromBase64String(param1);
                FileStream targetStream = null;
                Stream sourceStream = new MemoryStream(toBytes);

                string uploadFolder = HttpContext.Current.Server.MapPath("/" + param2);
                string filePath = Path.Combine(uploadFolder, path2: param3);

                using (targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    //read from the input stream in 6K chunks
                    //and save to output stream
                    const int bufferLen = 32000000;
                    byte[] buffer = new byte[bufferLen];
                    int count = 0;
                    while ((count = sourceStream.Read(buffer, 0, bufferLen)) > 0)
                    {
                        targetStream.Write(buffer, 0, count);
                    }
                    targetStream.Close();
                    sourceStream.Close();
                    return "0";
                }
            }
            catch (Exception e)
            {

                return "-1";

            }


        }
        public string uploadFile(byte[] param1, string param2, string param3)
        {
            try
            {
                // byte[] imageArray = File.ReadAllBytes(filepath);
                //byte[] toBytes = Convert.FromBase64String(param1);
                FileStream targetStream = null;
                Stream sourceStream = new MemoryStream(param1);

                string uploadFolder = HttpContext.Current.Server.MapPath("/" + param2);
                string filePath = Path.Combine(uploadFolder, path2: param3);

                using (targetStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    //read from the input stream in 6K chunks
                    //and save to output stream
                    const int bufferLen = 32000000;
                    byte[] buffer = new byte[bufferLen];
                    int count = 0;
                    while ((count = sourceStream.Read(buffer, 0, bufferLen)) > 0)
                    {
                        targetStream.Write(buffer, 0, count);
                    }
                    targetStream.Close();
                    sourceStream.Close();
                    return "0";
                }
            }
            catch (Exception e)
            {

                return "-1";

            }


        }

        public string addorupdateEntity(string json, string org)
        {
            return CrmProvider.ConnectorSaveEntity(json, org);
        }

        public bool checkConditionField(string LogicalName, string fieldName, string Value, string org)
        {
            return CrmProvider.isvalidfileld(LogicalName, fieldName, Value, org);
        }

        public string retriveLKGuidid(string LogicalName, string fieldName, string Value, string org)
        {
            return CrmProvider.retriveLookup(LogicalName, fieldName, Value, org);
        }
        public string InsertIssueNote(GoodsIssueNote objissuenote, string org)
        {
            return CrmProvider.insertGoodIssueNoteJson(objissuenote, org);
        }
        public string insertGoodIssueNoteConsigment(GoodsIssueNote objissuenote, string org)
        {
            return CrmProvider.insertGoodIssueNoteConsigment(objissuenote, org);
        }
        public string insertAccount(Account objAccount, string org)
        {
            return CrmProvider.insertAccount(objAccount, org);
        }
        //insertAccount
        public string insertInvoicePackingList(InvoicePackingList obj, string org)
        {
            return CrmProvider.insertInvoicePackingList(obj, org);
        }
        public string insertInvoiceSubOrder(InvoiceSuborder obj, string org)
        {
            return CrmProvider.insertInvoiceSubOrder(obj, org);
        }
        public string insertWarehouse(Warehouse objwarehouse, string org)
        {
            return CrmProvider.insertWarehouse(objwarehouse, org);
        }
        public string insertSite(Site obj, string org)
        {
            return CrmProvider.insertSite(obj, org);
        }
        public string insertSaleTaxGroup(SaleTaxGroup obj, string org)
        {
            return CrmProvider.insertSaleTaxGroup(obj, org);
        }
        public string insertUnitConversion(UnitConversion obj, string org)
        {
            return CrmProvider.insertUnitConversion(obj, org);
        }
        public string insertSaleTaxCode(SalesTaxCode obj, string org)
        {
            return CrmProvider.insertSaleTaxCode(obj, org);
        }
        public string insertGroupUnit(GroupUnit obj, string org)
        {
            return CrmProvider.insertGroupUnit(obj, org);
        }
        public string insertUnit(Unit obj, string org)
        {
            return CrmProvider.insertUnit(obj, org);
        }
        public string insertProduct(Product obj, string org)
        {
            return CrmProvider.insertProduct(obj, org);
        }
        public string insertPaymentMethod(PaymentMethod obj, string org)
        {
            return CrmProvider.insertPaymentMethod(obj, org);
        }
        public string insertPaymentTerm(PaymentTerm obj, string org)
        {
            return CrmProvider.insertPaymentTerm(obj, org);
        }
        public string insertItemSaleTaxGroup(ItemSalesTaxGroup obj, string org)
        {
            return CrmProvider.insertItemSaleTaxGroup(obj, org);
        }
        public string insertReturnReasonCode(ReturnReasonCode obj, string org)
        {
            return CrmProvider.insertReturnReasonCode(obj, org);
        }
        public string insertWeightProduct(WeightProduct obj, string org)
        {
            return CrmProvider.insertWeightProduct(obj, org);
        }
        public string insertCurrency(Currency obj, string org)
        {
            return CrmProvider.insertCurrency(obj, org);
        }
        public string InsertExchangeRate(ExchangeRate obj, string org)
        {
            return CrmProvider.InsertExchangeRate(obj, org);
        }
        public string insertManufactory(Manufactory obj, string org)
        {
            return CrmProvider.insertManufactory(obj, org);
        }
        public bool DeleteManufactory(string Recid, string org)
        {
            return CrmProvider.DeleteManufactory(Recid, org);
        }
        public string insertSize(Size obj, string org)
        {
            return CrmProvider.insertSize(obj, org);
        }
        public string insertSizeJson(Size obj, string org)
        {
            return CrmProvider.insertSizeJson(obj, org);
        }
        public bool DeleteSize(string Recid, string org)
        {
            return CrmProvider.DeleteSize(Recid, org);
        }
        public string insertStyle(Style obj, string org)
        {
            return CrmProvider.insertStyle(obj, org);
        }
        public bool DeleteStyle(string Recid, string org)
        {
            return CrmProvider.DeleteStyle(Recid, org);
        }
        public string insertConfiguration(Configuration obj, string org)
        {
            return CrmProvider.insertConfiguration(obj, org);
        }
        public bool DeleteConfiguration(string Recid, string org)
        {
            return CrmProvider.DeleteConfiguration(Recid, org);
        }
        public string insertBrand(Brand obj, string org)
        {
            return CrmProvider.insertBrand(obj, org);
        }
        public bool DeleteBrand(string Recid, string org)
        {
            return CrmProvider.DeleteBrand(Recid, org);
        }
        public string insertPacking(Packing obj, string org)
        {
            return CrmProvider.insertPacking(obj, org);
        }
        public string insertTransferOrder(TransferOrder obj, string org)
        {
            return CrmProvider.insertTransferOrder(obj, org);
        }
        public bool DeleteTransferOrder(string Recid, string org)
        {
            return CrmProvider.DeleteTransferOrder(Recid, org);
        }
        public bool DeletePacking(string Recid, string org)
        {
            return CrmProvider.DeletePacking(Recid, org);
        }
        public string insertPackaging(Packaging obj, string org)
        {
            return CrmProvider.insertPackaging(obj, org);
        }
        public string insertImportDeclaration(ImportDeclaration
             obj, string org)
        {
            return CrmProvider.insertImportDeclaration(obj, org);
        }
        public bool DeletePackaging(string Recid, string org)
        {
            return CrmProvider.DeletePackaging(Recid, org);
        }
        public bool DeleteSaleTaxGroup(string Recid, string org)
        {
            return CrmProvider.DeleteSaleTaxGroup(Recid, org);
        }
        public bool DeleteItemSaleTaxGroup(string Recid, string org)
        {
            return CrmProvider.DeleteItemSaleTaxGroup(Recid, org);
        }
        public bool DeleteUnitConversion(string Recid, string org)
        {
            return CrmProvider.DeleteUnitConversion(Recid, org);
        }
        public bool DeleteSaleTaxCode(string Recid, string org)
        {
            return CrmProvider.DeleteSaleTaxCode(Recid, org);
        }
        public bool DeleteProduct(string Recid, string org)
        {
            return CrmProvider.DeleteProduct(Recid, org);
        }
        public bool DeleteWeightProduct(string Recid, string org)
        {
            return CrmProvider.DeleteWeightProduct(Recid, org);
        }
        public bool DeleteWarehouse(string Recid, string org)
        {
            return CrmProvider.DeleteWarehouse(Recid, org);
        }
        public bool DeleteSite(string Recid, string org)
        {
            return CrmProvider.DeleteSite(Recid, org);
        }
        public bool DeletePaymentMethod(string Recid, string org)
        {
            return CrmProvider.DeletePaymentMethod(Recid, org);
        }
        public bool DeletePaymentTerm(string Recid, string org)
        {
            return CrmProvider.DeletePaymentTerm(Recid, org);
        }
        public bool DeleteCurrency(string isocurrencycode, string org)
        {
            return CrmProvider.DeleteCurrency(isocurrencycode, org);
        }
        public bool DeleteExchangeRate(string Recid, string org)
        {
            return CrmProvider.DeleteExchangeRate(Recid, org);
        }
        public bool DeleteReturnReasonCode(string Recid, string org)
        {
            return CrmProvider.DeleteReturnReasonCode(Recid, org);
        }
        public bool DeleteImportDeclaration(string Recid, string org)
        {
            return CrmProvider.DeleteImportDeclaration(Recid, org);
        }
        public bool CancelPickingList(string pickingListID, string org)
        {
            return CrmProvider.CancelPickingList(pickingListID, org);
        }
        public string insertPurchaseOrder(TransferOrder obj, string org)
        {
            return CrmProvider.insertPurchaseOrder(obj, org);
        }
        public string CorrectPurchaseOrder(TransferOrder obj, string org)
        {
            return CrmProvider.CorrectPurchaseOrder(obj, org);
        }
        public bool CancelPurchaseOrder(string purchaseOrderId, string org)
        {
            return CrmProvider.CancelPurchaseOrder(purchaseOrderId, org);
        }

    }
}
