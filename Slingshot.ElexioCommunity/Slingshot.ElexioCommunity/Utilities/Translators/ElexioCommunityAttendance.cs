using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.Core.Model;

namespace Slingshot.ElexioCommunity.Utilities.Translators
{
    public static class ElexioCommunityAttendance
    {
        public static Attendance Translate( dynamic importAttendance )
        {
            var attendance = new Attendance();

            attendance.PersonId = importAttendance.uid;
            attendance.StartDateTime = importAttendance.date;
            attendance.GroupId = importAttendance.gid;
            attendance.Note = importAttendance.reason;

            if ( importAttendance.present == 0 )
            {
                attendance.Note = "Did not attend.";
            }

            return attendance;
        }
    }
}
