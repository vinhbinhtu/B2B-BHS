using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using Lib.Service;
using System.Data.SqlClient;
using System.Data;
using Microsoft.Crm.Sdk.Messages;

namespace SubOrder
{
    public class Suborder_Date : IPlugin
    {
        IOrganizationService service = null;
        IPluginExecutionContext context = null;
        public void Execute(IServiceProvider serviceProvider)
        {
            context = serviceProvider.GetService(typeof(IPluginExecutionContext)) as IPluginExecutionContext;
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            ITracingService traceService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            service = factory.CreateOrganizationService(context.UserId);
            if (context.Depth > 1) return;
            if (context.MessageName == "Create")
            {
                Entity target = (Entity)context.InputParameters["Target"];
                if (target != null && target is Entity)
                {
                    if (target.HasValue("bsd_date"))
                    {
                        Entity suborder = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        CheckDate(suborder);
                    }
                }
            }
            else if (context.MessageName == "Update")
            {
                traceService.Trace("*** Diệm ***");
                Entity target = (Entity)context.InputParameters["Target"];
                if (target != null && target is Entity)
                {
                    if (target.HasValue("bsd_skipplugin") && (bool)target["bsd_skipplugin"]) return;
                    if (target.HasValue("bsd_date"))
                    {
                        Entity suborder = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
                        CheckDate(suborder);
                    }
                }
                traceService.Trace("***End Diệm***");
            }
        }
        #region B2B :  Diệm 3h30pm 01/07/2017 Update workflow
        public void CheckDate(Entity suborder)
        {
            double time_closing = getConfigTimeClosing();
            DateTime date = DateTime.Now.AddDays(10000);
            date = (DateTime)suborder["bsd_date"];
            date = EnityUtilities.RetrieveLocalTimeFromUTCTime(date, service);
            DateTime datedistributor = date;
            date = date.AddHours(time_closing);
            //throw new Exception("time_closing: " + time_closing +"date: "+date.ToLocalTime() );
            // date = date.AddDays(1)
            //lay calendar holiday
            string datefull = date.ToString("yyyy-MM-dd");
            date = CalendarHoliday(datefull, date);


            var dayOfWeek = date.DayOfWeek;
            EntityCollection etccalendarworking = RetrieveZeroCondition("bsd_calendarworking", service);
            if (etccalendarworking.Entities.Any())
            {
                Entity calendarworking = etccalendarworking.Entities.First();
                bool monday, tuesday, wednesday, thursday, friday, saturday, sunday;
                monday = tuesday = wednesday = thursday = friday = saturday = sunday = false;
                if (calendarworking.HasValue("bsd_monday")) monday = (bool)calendarworking["bsd_monday"];
                if (calendarworking.HasValue("bsd_tuesday")) tuesday = (bool)calendarworking["bsd_tuesday"];
                if (calendarworking.HasValue("bsd_wednesday")) wednesday = (bool)calendarworking["bsd_wednesday"];
                if (calendarworking.HasValue("bsd_thursday")) thursday = (bool)calendarworking["bsd_thursday"];
                if (calendarworking.HasValue("bsd_friday")) friday = (bool)calendarworking["bsd_friday"];
                if (calendarworking.HasValue("bsd_saturday")) saturday = (bool)calendarworking["bsd_saturday"];
                if (calendarworking.HasValue("bsd_sunday")) sunday = (bool)calendarworking["bsd_sunday"];
                datefull = date.ToString("yyyy-MM-dd");
                string fetchxml_holiday_again = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                           <entity name='bsd_calendarholiday'>
                                             <attribute name='bsd_calendarholidayid' />
                                             <attribute name='bsd_name' />
                                             <attribute name='createdon' />
                                             <attribute name='bsd_startdate' />
                                             <attribute name='bsd_enddate' />
                                             <order attribute='bsd_name' descending='false' />
                                             <filter type='and'>
                                               <condition attribute='bsd_startdate' operator='on-or-before' value='{0}' />
                                               <condition attribute='bsd_enddate' operator='on-or-after' value='{1}' />
                                               <condition attribute='statecode' operator='eq' value='0' />
                                             </filter>
                                           </entity>
                                         </fetch>", datefull, datefull);
                EntityCollection etccalendarholiday_again = service.RetrieveMultiple(new FetchExpression(fetchxml_holiday_again));
                if (!etccalendarholiday_again.Entities.Any())
                {
                    while (true)
                    {
                        bool dowork = CheckDate(date, dayOfWeek, sunday, monday, tuesday, wednesday, thursday, friday, saturday);
                        if (dowork) break;
                        else if (!dowork) date = date.AddDays(1);
                    }
                }
            }
            DateTime nowdate = EnityUtilities.RetrieveLocalTimeFromUTCTime(DateTime.Now, service);
            Entity newdistributorshipping = new Entity(suborder.LogicalName, suborder.Id);
            newdistributorshipping["bsd_dateworkflow"] = date;
            service.Update(newdistributorshipping);
        }
        public bool CheckHourAndMin(DateTime startdate, DateTime enddate)
        {
            int starthour = startdate.Hour;
            int startmin = startdate.Minute;
            int endhour = enddate.Hour;
            int endmin = enddate.Minute;
            if (starthour == endhour && startmin == endmin) return true;
            return false;
        }
        public bool CheckDate(DateTime date, DayOfWeek dayOfWeek, bool sunday, bool monday, bool tuesday, bool wednesday, bool thursday, bool friday, bool saturday)
        {
            if (monday == true && date.DayOfWeek == DayOfWeek.Monday) return true;
            if (tuesday == true && date.DayOfWeek == DayOfWeek.Tuesday) return true;
            if (wednesday == true && date.DayOfWeek == DayOfWeek.Wednesday) return true;
            if (thursday == true && date.DayOfWeek == DayOfWeek.Thursday) return true;
            if (friday == true && date.DayOfWeek == DayOfWeek.Friday) return true;
            if (saturday == true && date.DayOfWeek == DayOfWeek.Saturday) return true;
            if (sunday == true && date.DayOfWeek == DayOfWeek.Sunday) return true;
            return false;
        }
        public DateTime CalendarHoliday(string datefull, DateTime date)
        {
            double dateholiday = 0;
            string fetchxml_holiday = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                          <entity name='bsd_calendarholiday'>
                                            <attribute name='bsd_calendarholidayid' />
                                            <attribute name='bsd_name' />
                                            <attribute name='createdon' />
                                            <attribute name='bsd_startdate' />
                                            <attribute name='bsd_enddate' />
                                            <order attribute='bsd_name' descending='false' />
                                            <filter type='and'>
                                              <condition attribute='bsd_startdate' operator='on-or-before' value='" + datefull + @"' />
                                              <condition attribute='bsd_enddate' operator='on-or-after' value='" + datefull + @"' />
                                            </filter>
                                          </entity>
                                        </fetch>";
            EntityCollection etccalendarholiday = service.RetrieveMultiple(new FetchExpression(fetchxml_holiday));
            if (etccalendarholiday.Entities.Any())
            {
                foreach (var calendarholiday in etccalendarholiday.Entities)
                {
                    DateTime enddate = DateTime.Now;
                    if (calendarholiday.HasValue("bsd_enddate")) enddate = (DateTime)calendarholiday["bsd_enddate"];
                    enddate = EnityUtilities.RetrieveLocalTimeFromUTCTime(enddate, service).Date;
                    DateTime datechange = EnityUtilities.RetrieveLocalTimeFromUTCTime(date, service).Date;
                    TimeSpan countdate = enddate - datechange;
                    dateholiday += countdate.TotalDays;
                }
                dateholiday += 1;
            }
            date = date.AddDays(dateholiday);
            return date;
        }
        public static EntityCollection RetrieveZeroCondition(string localname, IOrganizationService service)
        {
            QueryExpression q = new QueryExpression(localname);
            q.ColumnSet = new ColumnSet(true);
            FilterExpression filter = new FilterExpression();
            if (localname != "productpricelevel") filter.AddCondition(new ConditionExpression("statecode", ConditionOperator.Equal, 0));
            q.Criteria = filter;
            return service.RetrieveMultiple(q);
        }
        public double getConfigTimeClosing()
        {
            double i_result = 24;
            string xml = string.Format(@"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                                      <entity name='bsd_configdefault'>
                                        <attribute name='bsd_configdefaultid' />
                                        <attribute name='bsd_name' />
                                        <attribute name='createdon' />
                                        <attribute name='bsd_timeofclosing' />
                                        <order attribute='bsd_name' descending='false' />
                                      </entity>
                                    </fetch>");
            EntityCollection lst = service.RetrieveMultiple(new FetchExpression(xml));

            if (lst.Entities.Any())
            {

                if (lst.Entities.First().HasValue("bsd_timeofclosing"))
                {
                    i_result =double.Parse(lst.Entities.First()["bsd_timeofclosing"].ToString());
                }
            }
            return i_result;
        }
        #endregion
    }
}
