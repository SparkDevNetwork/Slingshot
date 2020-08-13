using Slingshot.Core.Model;
using Slingshot.F1.Utilities.SQL.DTO;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Slingshot.F1.Utilities.Translators.SQL
{
    public static class F1Attendance
    {
        public static Attendance Translate( AttendanceDTO attendance, List<int> uniqueAttendanceIds )
        {
            var slingshotAttendance = new Attendance();

            slingshotAttendance.PersonId = attendance.IndividualId;
            slingshotAttendance.GroupId = attendance.GroupId;
            slingshotAttendance.StartDateTime = attendance.StartDateTime;
            slingshotAttendance.EndDateTime = attendance.EndDateTime;
            slingshotAttendance.Note = attendance.Note;

            if ( attendance.AttendanceId != null )
            {
                //If F1 specifies the AttendanceId, try that, first.
                slingshotAttendance.AttendanceId = attendance.AttendanceId.Value;
            }

            if ( slingshotAttendance.AttendanceId == default( int ) || uniqueAttendanceIds.Contains( slingshotAttendance.AttendanceId ) )
            {
                //Use Hash to create Attendance ID
                MD5 md5Hasher = MD5.Create();
                string valueToHash = $"{ slingshotAttendance.PersonId }{ slingshotAttendance.GroupId }{ slingshotAttendance.StartDateTime }";
                var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( valueToHash ) );

                // This conversion turns the 128-bit hash into a 32-bit integer and then converts the value to
                // a positive number.  This drastically increases the odds of a collision.
                var attendanceId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                if ( attendanceId > 0 )
                {
                    slingshotAttendance.AttendanceId = attendanceId;
                }
            }

            if ( uniqueAttendanceIds.Contains( slingshotAttendance.AttendanceId ) )
            {
                //Hash collision, use a random number.
                var random = new Random();
                attendance.AttendanceId = GetNewRandomAttendanceId( uniqueAttendanceIds, random );
            }

            uniqueAttendanceIds.Add( slingshotAttendance.AttendanceId );

            return slingshotAttendance;
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
