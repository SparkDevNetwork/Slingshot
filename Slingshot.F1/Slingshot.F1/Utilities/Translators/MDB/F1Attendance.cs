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
        public static Attendance Translate( DataRow row, List<int> uniqueAttendanceIds )
        {
            var attendance = new Attendance();

            attendance.PersonId = row.Field<int>( "Individual_ID" );
            attendance.GroupId = row.Field<int>( "GroupId" );
            attendance.StartDateTime = row.Field<DateTime>( "StartDateTime" );
            attendance.EndDateTime = row.Field<DateTime?>( "EndDateTime" );
            attendance.Note = row.Field<string>( "Note" );

            if ( row.Field<int?>( "Attendance_ID" ) != null )
            {
                //If F1 specifies the AttendanceId, try that, first.
                attendance.AttendanceId = row.Field<int>( "Attendance_ID" );
            }

            if ( attendance.AttendanceId == default( int ) || uniqueAttendanceIds.Contains( attendance.AttendanceId ) )
            {
                //Use Hash to create Attendance ID
                MD5 md5Hasher = MD5.Create();
                string valueToHash = $"{ attendance.PersonId }{ attendance.GroupId }{ attendance.StartDateTime }";
                var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( valueToHash ) );
                var attendanceId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                if ( attendanceId > 0 )
                {
                    attendance.AttendanceId = attendanceId;
                }
            }

            if ( uniqueAttendanceIds.Contains( attendance.AttendanceId ) )
            {
                //ToDo: Should review this funtionality.  The randomly assigned Attendance ID will NOT be the same if this data is re-exported.
                //As an, alternative, we could simply exclude this attendance row (return null).
                var random = new Random();
                attendance.AttendanceId = GetNewRandomAttendanceId(uniqueAttendanceIds, random);
            }

            uniqueAttendanceIds.Add( attendance.AttendanceId );

            return attendance;
        }

        private static int GetNewRandomAttendanceId( List<int> uniqueAttendanceIds, Random random, int iterationCount = 0 )
        {
            if ( iterationCount >= 10000 )
            {
                throw new Exception( "Random number loop stuck." );
            }

            int value = random.Next();
            if ( uniqueAttendanceIds.Contains( value ) )
            {
                value = GetNewRandomAttendanceId( uniqueAttendanceIds, random, iterationCount + 1 );
            }

            return value;
        }
    }
}
