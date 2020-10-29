using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportGroupAttendance
    {
        public static Attendance Translate( AttendanceDTO attendance, int groupId )
        {
            if ( attendance.Id <= 0 || !attendance.IsValid )
            {
                return null;
            }

            string note = string.Empty;
            if ( !string.IsNullOrWhiteSpace( attendance.Name ) )
            {
                note += $"Event Name: {attendance.Name.Trim()}";
            }

            if ( !string.IsNullOrWhiteSpace( attendance.Description ) )
            {
                if ( !string.IsNullOrWhiteSpace( note ) )
                {
                    note += ", ";
                }
                note += $"Event Description: {attendance.Description.Trim()}";
            }

            return new Attendance()
            {
                AttendanceId = attendance.Id,
                PersonId = attendance.PersonId,
                GroupId = attendance.GroupId,
                StartDateTime = attendance.StartsAt.Value,
                EndDateTime = attendance.EndsAt,
                Note = note
            };
        }
    }
}
