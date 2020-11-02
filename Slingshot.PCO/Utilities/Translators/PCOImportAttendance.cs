using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportAttendance
    {
        public static Dictionary<string, string> ReadAttributes( CheckInDTO checkIn )
        {
            var attributes = new Dictionary<string, string>();

            if ( checkIn.Id <= 0 || checkIn.PersonId <= 0 )
            {
                return attributes;
            }

            if ( !string.IsNullOrWhiteSpace( checkIn.MedicalNotes ) )
            {
                attributes.Add( PCOApi.CheckinAttributeKey.MedicalNotes, checkIn.MedicalNotes );
            }

            if ( !string.IsNullOrWhiteSpace( checkIn.EmergencyContactName ) )
            {
                attributes.Add( PCOApi.CheckinAttributeKey.EmergencyContactName, checkIn.EmergencyContactName );
            }

            if ( !string.IsNullOrWhiteSpace( checkIn.EmergencyContactPhoneNumber ) )
            {
                attributes.Add( PCOApi.CheckinAttributeKey.EmergencyContactPhone, checkIn.EmergencyContactPhoneNumber.CleanNumber() );
            }

            return attributes;
        }

        public static Attendance Translate( CheckInDTO checkIn, int minAttendanceIdValue )
        {
            if ( checkIn.Id <= 0 || checkIn.PersonId <= 0 )
            {
                return null;
            }

            return new Attendance()
            {
                AttendanceId = checkIn.Id + minAttendanceIdValue,
                PersonId = checkIn.PersonId,
                ScheduleId = checkIn.Event?.Id,
                StartDateTime = checkIn.CreatedAt.Value,
                EndDateTime = checkIn.CheckedOutAt
            };
        }

        #region CleanNumber() Method

        private static Regex digitsOnly = new Regex(@"[^\d]");

        /// <summary>
        /// Removes non-numeric characters from a provided number
        /// </summary>
        /// <param name="number">A <see cref="System.String"/> containing the phone number to clean.</param>
        /// <returns>A <see cref="System.String"/> containing the phone number with all non numeric characters removed. </returns>
        private static string CleanNumber( this string number )
        {
            if ( !string.IsNullOrEmpty( number ) )
            {
                return digitsOnly.Replace( number, string.Empty );
            }
            else
            {
                return string.Empty;
            }
        }

        #endregion CleanNumber() Method
    }
}
