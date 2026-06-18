using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveAppointmentAttend(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAppointmentAttend> dbAA = db.TblAppointmentAttends.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAppointmentAttend> xnAA = new List<Models.TblAppointmentAttend>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAppointmentAttend xappt = new Models.TblAppointmentAttend();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        try
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    xappt.SiteCode = dr[c.ColumnName].ToString();
                                    xappt.LastModAt = runat;
                                    xappt.RowChkSum = int.Parse(dr["RowChkSum"].ToString());
                                    xappt.RowState = true;
                                    break;
                                case "aaid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xappt.AAId = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "aaaptid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xappt.aaaptID = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "aacltid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xappt.aacltid = int.Parse(dr[c.ColumnName].ToString());
                                        if (xappt.aacltid < 0) { xappt.RowState = false; }
                                    }
                                    break;
                                case "rowchksum":
                                    xappt.RowChkSum = int.Parse(dr[c.ColumnName].ToString());
                                    break;
                                case "aadtenrolled":
                                    if (dr[c.ColumnName].ToString().Length > 5)
                                    {
                                        xappt.aaDTENROLLED = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "aadtremoved":
                                    if (dr[c.ColumnName].ToString().Length > 5)
                                    {
                                        xappt.aaDTREMOVED = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(c.ColumnName.ToString() + " : " + dr[c.ColumnName].ToString());
                        }
                    }
                    Models.TblAppointmentAttend xapt = dbAA.FirstOrDefault(x => x.SiteCode == xappt.SiteCode && x.AAId == xappt.AAId);
                    if (xapt == null)
                    {
                        rc.RowsIns += 1;
                        xnAA.Add(xappt);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xapt.aaaptID = xappt.aaaptID;
                        xapt.aacltid = xappt.aacltid;
                        xapt.aaDTENROLLED = xappt.aaDTENROLLED;
                        xapt.aaDTREMOVED = xappt.aaDTREMOVED;
                        xapt.RowChkSum = xappt.RowChkSum;
                        xapt.LastModAt = xappt.LastModAt;
                        xapt.RowState = xappt.RowState;
                    }
                }
                db.SaveChanges();
                if (xnAA.Count > 0)
                {
                    db.TblAppointmentAttends.AddRange(xnAA);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }

        public Models.RCodes SaveAppointments(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAppointments> dbapts = db.TblAppointments.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAppointments> napts = new List<Models.TblAppointments>();
                foreach(var a in dbapts)
                {
                    a.RowState = false;
                }
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAppointments xappt = new Models.TblAppointments();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        try
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    xappt.SiteCode = dr[c.ColumnName].ToString();
                                    xappt.LastModAt = runat;
                                    xappt.RowChkSum = int.Parse(dr["RowChkSum"].ToString());
                                    xappt.RowState = true;
                                    break;
                                case "uniqueid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xappt.UniqueId = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    else { xappt.UniqueId = -1; }
                                    break;
                                case "lastmodat":
                                    xappt.LastModAt = runat;
                                    break;
                                case "rowchksum":
                                    xappt.RowChkSum = int.Parse(dr[c.ColumnName].ToString());
                                    break;
                                case "type":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xappt.Type = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "startdate":
                                    if (dr[c.ColumnName].ToString().Length > 5)
                                    {
                                        xappt.StartDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "enddate":
                                    if (dr[c.ColumnName].ToString().Length > 5)
                                    {
                                        xappt.EndDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "allday":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xappt.AllDay = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "subject":
                                    xappt.Subject = dr[c.ColumnName].ToString();
                                    break;
                                case "location":
                                    xappt.Location = dr[c.ColumnName].ToString();
                                    break;
                                case "description":
                                    xappt.Description = dr[c.ColumnName].ToString();
                                    break;
                                case "status":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.Status = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "label":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xappt.Label = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "resourceid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.ResourceId = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "resourceids":
                                    xappt.ResourceIds = dr[c.ColumnName].ToString();
                                    break;
                                case "reminderinfo":
                                    xappt.ReminderInfo = dr[c.ColumnName].ToString();
                                    break;
                                case "recurrenceinfo":
                                    xappt.RecurrenceInfo = dr[c.ColumnName].ToString();
                                    break;
                                case "precentcomplete":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.PercentComplete = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "groupname":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.GroupName = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "customfield1":
                                    xappt.CustomField1 = dr[c.ColumnName].ToString();
                                    break;
                                case "attendees":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.Attendees = bool.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "service":
                                    xappt.Service = dr[c.ColumnName].ToString();
                                    break;
                                case "servicemodifier":
                                    xappt.ServiceModifier = dr[c.ColumnName].ToString();
                                    break;
                                case "txtnote":
                                    xappt.TxtNote = dr[c.ColumnName].ToString();
                                    break;
                                case "area":
                                    xappt.Area = dr[c.ColumnName].ToString();
                                    break;
                                case "intakeappointmentmissed":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.IntakeAppointmentMissed = bool.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "salesforceid":
                                    xappt.SalesForceId = dr[c.ColumnName].ToString();
                                    break;
                                case "issalesforcesync":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.IsSalesForceSync = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "isthirdpartysync":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.IsThirdPartySync = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "appointmenttype":
                                    xappt.AppointmentType = dr[c.ColumnName].ToString();
                                    break;
                                case "isdropin":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.IsDropIn = bool.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "isschedule":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.IsSchedule = bool.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "noofparticipants":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.NoofParticipants = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "grouptimeallowed":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.GroupTimeAllowed = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                                case "graceperiod":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    { xappt.GracePeriod = int.Parse(dr[c.ColumnName].ToString()); }
                                    break;
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(c.ColumnName.ToString() + " : " + dr[c.ColumnName].ToString());
                        }
                    }
                    Models.TblAppointments xapt = dbapts.FirstOrDefault(x => x.SiteCode == xappt.SiteCode && x.UniqueId == xappt.UniqueId);
                    if (xapt == null)
                    {
                        rc.RowsIns += 1;
                        napts.Add(xappt);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xapt.RowChkSum = xappt.RowChkSum;
                        xapt.LastModAt = xappt.LastModAt;
                        xapt.RowState = xappt.RowState;
                        xapt.Type = xappt.Type;
                        xapt.StartDate = xappt.StartDate;
                        xapt.EndDate = xappt.EndDate;
                        xapt.AllDay = xappt.AllDay;
                        xapt.Subject = xappt.Subject;
                        xapt.Location = xappt.Location;
                        xapt.Description = xappt.Description;
                        xapt.Status = xappt.Status;
                        xapt.Label = xappt.Label;
                        xapt.ResourceId = xappt.ResourceId;
                        xapt.ResourceIds = xappt.ResourceIds;
                        xapt.ReminderInfo = xappt.ReminderInfo;
                        xapt.RecurrenceInfo = xappt.RecurrenceInfo;
                        xapt.PercentComplete = xappt.PercentComplete;
                        xapt.GroupName = xappt.GroupName;
                        xapt.CustomField1 = xappt.CustomField1;
                        xapt.Attendees = xappt.Attendees;
                        xapt.Service = xappt.Service;
                        xapt.ServiceModifier = xappt.ServiceModifier;
                        xapt.TxtNote = xappt.TxtNote;
                        xapt.Area = xappt.Area;
                        xapt.IntakeAppointmentMissed = xappt.IntakeAppointmentMissed;
                        xapt.SalesForceId = xappt.SalesForceId;
                        xapt.IsSalesForceSync = xappt.IsSalesForceSync;
                        xapt.IsThirdPartySync = xappt.IsThirdPartySync;
                        xapt.AppointmentType = xappt.AppointmentType;
                        xapt.IsDropIn = xappt.IsDropIn;
                        xapt.IsSchedule = xappt.IsSchedule;
                        xapt.NoofParticipants = xappt.NoofParticipants;
                        xapt.GroupTimeAllowed = xappt.GroupTimeAllowed;
                        xapt.GracePeriod = xappt.GracePeriod;
                    }
                }
                db.SaveChanges();
                if (napts.Count > 0)
                {
                    db.TblAppointments.AddRange(napts);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
    }
}
