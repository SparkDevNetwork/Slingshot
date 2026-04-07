using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.PCO.Models.DTO;
using Slingshot.PCO.Utilities.Translators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// PCO API Class - Attendance data export methods.
    /// </summary>
    public static partial class PCOApi
    {
        public static class CheckinAttributeKey
        {
            public static string MedicalNotes = "PCO_CheckIn_MedicalNote";
            public static string EmergencyContactName = "PCO_CheckIn_EmergencyContactName";
            public static string EmergencyContactPhone = "PCO_CheckIn_EmergencyContactPhone";
        }
        /// <summary>
        /// Api Endpoint Declarations.
        /// </summary>
        internal static partial class ApiEndpoint
        {
            internal const string API_CHECKINORGANIZATION = "/check-ins/v2";
            internal const string API_CHECKINS = "/check-ins/v2/check_ins";
        }

        /// <summary>
        /// Test access to the check-in API.
        /// </summary>
        /// <returns></returns>
        public static bool TestCheckInAccess()
        {
            var initalErrorValue = PCOApi.ErrorMessage;

            var response = ApiGet( ApiEndpoint.API_CHECKINORGANIZATION );

            PCOApi.ErrorMessage = initalErrorValue;

            return ( response != string.Empty );
        }

        #region ExportAttendance() and Related Methods

        public static void ExportAttendance( DateTime modifiedSince, DateTime rangeStart, DateTime rangeEnd )
        {
            var checkIns = GetCheckIns( modifiedSince, rangeStart, rangeEnd );
            if ( !checkIns.Any() )
            {
                return;
            }

            WriteCheckInPersonAttributes();

            var personAttributeCollection = new Dictionary<int, Dictionary<string, string>>();
            var schedules = new Dictionary<int, Schedule>();
            var locations = new Dictionary<int, Location>();

            foreach ( var checkIn in checkIns )
            {
                var importAttendance = PCOImportAttendance.Translate( checkIn, MaxGroupAttendanceId );
                if ( importAttendance == null )
                {
                    continue;
                }

                ImportPackage.WriteToPackage( importAttendance );

                // Add schedule (event) to collection as necessary.
                if ( checkIn.Event != null )
                {
                    if ( !schedules.ContainsKey( checkIn.Event.Id ) )
                    {
                        var importSchedule = PCOImportSchedule.Translate( checkIn.Event );
                        if ( importSchedule != null )
                        {
                            schedules.Add( importSchedule.Id, importSchedule );
                        }
                    }
                }

                // Add location to collection as necessary.
                if ( checkIn.Location != null )
                {
                    if ( !locations.ContainsKey( checkIn.Location.Id ) )
                    {
                        var importLocation = PCOImportLocation.Translate( checkIn.Location );
                        if ( importLocation != null )
                        {
                            locations.Add( importLocation.Id, importLocation );
                        }
                    }
                }

                // Get Person AttributeValues for this Check-In record.
                int personId = importAttendance.PersonId;
                Dictionary<string, string> personAttributes;
                if ( personAttributeCollection.ContainsKey( personId ) )
                {
                    personAttributes = personAttributeCollection[personId];
                }
                else
                {
                    personAttributes = new Dictionary<string, string>();
                    personAttributeCollection.Add( personId, personAttributes );
                }

                var attributeValues = PCOImportAttendance.ReadAttributes( checkIn );
                foreach ( var attributeKey in attributeValues.Keys )
                {
                    var attributeValue = attributeValues[attributeKey].Replace( ",", "&comma;" );
                    if ( personAttributes.ContainsKey( attributeKey ) )
                    {
                        personAttributes[attributeKey] = personAttributes[attributeKey] + "," + attributeValue;
                    }
                    else
                    {
                        personAttributes.Add( attributeKey, attributeValue );
                    }
                }
            }

            // Write all Person AttributeValues.
            foreach ( var personId in personAttributeCollection.Keys )
            {
                var personAttributes = personAttributeCollection[personId];
                foreach ( var attributeKey in personAttributes.Keys )
                {
                    ImportPackage.WriteToPackage( new PersonAttributeValue()
                    {
                        AttributeKey = attributeKey,
                        AttributeValue = personAttributes[attributeKey],
                        PersonId = personId
                    } );
                }
            }

            // Write all Schedules.
            foreach ( var importSchedule in schedules.Select( s => s.Value ) )
            {
                ImportPackage.WriteToPackage( importSchedule );
            }

            // Write all Locations.
            foreach ( var importLocation in locations.Select( s => s.Value ) )
            {
                ImportPackage.WriteToPackage( importLocation );
            }
        }

        private static List<CheckInDTO> GetCheckIns( DateTime modifiedSince, DateTime rangeStart, DateTime rangeEnd )
        {
            var checkIns = new List<CheckInDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "include", "event,locations,person" },
                { "per_page", "500" },
                { "where[created_at][gte]", rangeStart.ToString("yyyy-MM-dd") + "T00:01:00Z" },
                { "where[created_at][lte]", rangeEnd.ToString("yyyy-MM-dd") + "T00:01:00Z" }
            };

            var checkInQuery = GetAPIQuery( ApiEndpoint.API_CHECKINS, apiOptions, modifiedSince );

            if ( checkInQuery == null )
            {
                return checkIns;
            }

            foreach ( var item in checkInQuery.Items )
            {
                var checkIn = new CheckInDTO( item, checkInQuery.IncludedItems );
                checkIns.Add( checkIn );
            }

            return checkIns;
        }

        private static void WriteCheckInPersonAttributes()
        {
            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Medical Note",
                Key = CheckinAttributeKey.MedicalNotes,
                Category = "PCO Check-In",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Emergency Contact Name",
                Key = CheckinAttributeKey.EmergencyContactName,
                Category = "PCO Check-In",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Emergency Contact Phone Number",
                Key = CheckinAttributeKey.EmergencyContactPhone,
                Category = "PCO Check-In",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );
        }

        #endregion ExportAttendance() and Related Methods
    }
}
