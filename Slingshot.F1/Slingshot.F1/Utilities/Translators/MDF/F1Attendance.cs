using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1Attendance
    {
        public static Attendance Translate( DataRow row )
        {
            var attendance = new Attendance();

            attendance.PersonId = row.Field<int>( "Individual_ID" );
            attendance.GroupId = row.Field<int>( "GroupId" );
            attendance.StartDateTime = row.Field<DateTime>( "StartDateTime" );
            attendance.EndDateTime = row.Field<DateTime?>( "EndDateTime" );
            attendance.Note = row.Field<string>( "Note" );

            //Use Hash to create Attendance ID
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( attendance.PersonId + attendance.GroupId + attendance.StartDateTime.ToString() ) );
            var attendanceId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
            if ( attendanceId > 0 )
            {
                attendance.AttendanceId = attendanceId;
            }

            return attendance;
        }
    }
}
