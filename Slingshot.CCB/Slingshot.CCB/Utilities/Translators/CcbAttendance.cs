using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.CCB.Utilities.Translators
{
    public static class CcbAttendance
    {
        public static List<Attendance> Translate(XElement inputAttendance, List<EventDetail> eventDetails )
        {
            List<Attendance> attendanceList = new List<Attendance>();

            var attendanceDate = inputAttendance.Element( "occurrence" ).Value.AsDateTime().Value;
            var eventId = inputAttendance.Attribute( "id" ).Value.AsInteger();
            var locationId = eventDetails.Where( e => e.EventId == eventId ).Select( e => e.LocationId ).FirstOrDefault();
            var scheduleId = eventDetails.Where( e => e.EventId == eventId ).Select( e => e.ScheduleId ).FirstOrDefault();
            var groupId = eventDetails.Where( e => e.EventId == eventId ).Select( e => e.GroupId ).FirstOrDefault();

            foreach ( var attendee in inputAttendance.Element( "attendees" ).Elements( "attendee" ) )
            {
                var attendance = new Attendance();
                attendanceList.Add( attendance );
                attendance.PersonId = attendee.Attribute( "id" ).Value.AsInteger();
                attendance.StartDateTime = attendanceDate;
                attendance.LocationId = locationId;
                attendance.ScheduleId = scheduleId;
                attendance.GroupId = groupId;

                MD5 md5Hasher = MD5.Create();
                var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( $@"
 {attendance.PersonId}
 {attendance.StartDateTime}
 {attendance.LocationId}
 {attendance.ScheduleId}
 {attendance.GroupId}
" ) );
                attendance.AttendanceId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
            }

            return attendanceList;
        }
    }
}
