using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock;
using Rock.Data;
using Rock.Model;

namespace Slingshot.F1.Utilities.Translators
{
    public static class F1Attendance
    {
        /// <summary>
        /// Translates the attendance data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateAttendance( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var newAttendances = new List<Attendance>();
            var importedAttendancesCount = lookupContext.Attendances.AsNoTracking()
                .Count( a => a.ForeignKey != null );

            var importedCodes = lookupContext.AttendanceCodes.AsNoTracking()
                .Where( c => c.ForeignKey != null ).ToList();

            var importedDevices = lookupContext.Devices.AsNoTracking()
                .Where( d => d.DeviceTypeValueId == DeviceTypeCheckinKioskId ).ToList();

            var archivedScheduleName = "Archived Attendance";
            var archivedSchedule = new ScheduleService( lookupContext ).Queryable()
                .FirstOrDefault( s => s.Name.Equals( archivedScheduleName ) );
            if ( archivedSchedule == null )
            {
                archivedSchedule = AddNamedSchedule( lookupContext, archivedScheduleName, null, null, null,
                    ImportDateTime, archivedScheduleName.RemoveSpecialCharacters(), true, ImportPersonAliasId );
            }

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying attendance import ({importedAttendancesCount:N0} already exist)." );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var rlcId = row["RLC_ID"] as int?;
                var individualId = row["Individual_ID"] as int?;
                var startDate = row["Start_Date_Time"] as DateTime?;
                var attendanceCode = row["Tag_Code"] as string;
                var attendanceNote = row["BreakoutGroup_Name"] as string;
                var checkinDate = row["Check_In_Time"] as DateTime?;
                var checkoutDate = row["Check_Out_Time"] as DateTime?;
                var machineName = row["Checkin_Machine_Name"] as string;

                // at minimum, attendance needs a person and a date
                var personKeys = GetPersonKeys( individualId, null );
                if ( personKeys != null && personKeys.PersonAliasId > 0 && startDate.HasValue )
                {
                    // create the initial attendance
                    var attendance = new Attendance
                    {
                        PersonAliasId = personKeys.PersonAliasId,
                        DidAttend = true,
                        Note = attendanceNote,
                        StartDateTime = (DateTime)startDate,
                        EndDateTime = checkoutDate,
                        CreatedDateTime = checkinDate,
                        ForeignKey = $"Attendance imported {ImportDateTime}"
                    };

                    // add the RLC info
                    var rlcGroup = ImportedGroups.FirstOrDefault( g => g.ForeignId.Equals( rlcId ) );
                    if ( rlcGroup != null && rlcGroup.Id > 0 )
                    {
                        attendance.CampusId = rlcGroup.CampusId;

                        attendance.Occurrence = new AttendanceOccurrence
                        {
                            OccurrenceDate = (DateTime)startDate,
                            GroupId = rlcGroup.Id,
                            ScheduleId = archivedSchedule.Id,
                            LocationId = rlcGroup.GroupLocations.Select( gl => (int?)gl.LocationId ).FirstOrDefault(),
                        };
                    }

                    // add the tag code
                    //if ( !string.IsNullOrWhiteSpace( attendanceCode ) )
                    //{
                    //var issueDatetime = checkinDate ?? (DateTime)startDate;
                    //var code = importedCodes.FirstOrDefault( c => c.Code.Equals( attendanceCode ) && c.IssueDateTime.Equals( issueDatetime ) );
                    //if ( code == null )
                    //{
                    //    code = new AttendanceCode
                    //    {
                    //        Code = attendanceCode,
                    //        IssueDateTime = issueDatetime,
                    //        ForeignKey = string.Format( "Attendance imported {0}", ImportDateTime )
                    //    };

                    //    lookupContext.AttendanceCodes.Add( code );
                    //    lookupContext.SaveChanges();
                    //    importedCodes.Add( code );
                    //}

                    //attendance.AttendanceCodeId = code.Id;
                    //}

                    // add the device
                    if ( !string.IsNullOrWhiteSpace( machineName ) )
                    {
                        var device = importedDevices.FirstOrDefault( d => d.Name.Equals( machineName, StringComparison.CurrentCultureIgnoreCase ) );
                        if ( device == null )
                        {
                            device = AddDevice( lookupContext, machineName, null, DeviceTypeCheckinKioskId, null, null, ImportDateTime,
                                $"{machineName} imported {ImportDateTime}", true, ImportPersonAliasId );
                            importedDevices.Add( device );
                        }

                        attendance.DeviceId = device.Id;
                    }

                    newAttendances.Add( attendance );

                    completedItems++;
                    if ( completedItems % percentage < 1 )
                    {
                        var percentComplete = completedItems / percentage;
                        ReportProgress( percentComplete, $"{completedItems:N0} attendances imported ({percentComplete}% complete)." );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SaveAttendances( newAttendances, false );
                        ReportPartialProgress();

                        // Reset lists and context
                        lookupContext.Dispose();
                        lookupContext = new RockContext();
                        newAttendances.Clear();
                    }
                }
            }

            if ( newAttendances.Any() )
            {
                SaveAttendances( newAttendances, false );
            }

            lookupContext.Dispose();
            ReportProgress( 100, $"Finished attendance import: {completedItems:N0} attendances imported." );
        }

        /// <summary>
        /// Translates the groups attendance data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateGroupsAttendance( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var newAttendances = new List<Attendance>();
            var newOccurrences = new List<AttendanceOccurrence>();
            var importedAttendancesCount = lookupContext.Attendances.AsNoTracking()
                .Count( a => a.ForeignKey != null && a.Occurrence.GroupId.HasValue && a.Occurrence.Group.GroupTypeId == GeneralGroupTypeId );

            var archivedScheduleName = "Archived Attendance";
            var archivedSchedule = new ScheduleService( lookupContext ).Queryable()
                .FirstOrDefault( s => s.Name.Equals( archivedScheduleName ) );
            if ( archivedSchedule == null )
            {
                archivedSchedule = AddNamedSchedule( lookupContext, archivedScheduleName, null, null, null,
                    ImportDateTime, archivedScheduleName.RemoveSpecialCharacters(), true, ImportPersonAliasId );
            }

            // Get list of existing attendance occurrences
            var existingOccurrences = new HashSet<ImportOccurrence>( new AttendanceOccurrenceService( lookupContext ).Queryable()
                .Select( o => new ImportOccurrence
                {
                    Id = o.Id,
                    GroupId = o.GroupId,
                    LocationId = o.LocationId,
                    ScheduleId = o.ScheduleId,
                    OccurrenceDate = o.OccurrenceDate
                } ) );

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying group attendance import, ({totalRows:N0} found, {importedAttendancesCount:N0} already exist)." );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var groupId = row["GroupID"] as int?;
                var startDate = row["StartDateTime"] as DateTime?;
                var endDate = row["EndDateTime"] as DateTime?;
                var attendanceNote = row["Comments"] as string;
                var wasPresent = row["Individual_Present"] as int?;
                var individualId = row["IndividualID"] as int?;
                var checkinDate = row["CheckinDateTime"] as DateTime?;
                var checkoutDate = row["CheckoutDateTime"] as DateTime?;
                var createdDate = row["AttendanceCreatedDate"] as DateTime?;

                var personKeys = GetPersonKeys( individualId, null );
                var peopleGroup = groupId.HasValue ? ImportedGroups.FirstOrDefault( g => g.ForeignId.Equals( groupId ) ) : null;
                if ( personKeys != null && personKeys.PersonAliasId > 0 && startDate.HasValue )
                {
                    // create the initial attendance
                    var attendance = new Attendance
                    {
                        PersonAliasId = personKeys.PersonAliasId,
                        DidAttend = wasPresent != 0,
                        Note = attendanceNote,
                        StartDateTime = (DateTime)startDate,
                        EndDateTime = checkoutDate,
                        CreatedDateTime = checkinDate,
                        ForeignKey = $"Group Attendance imported {ImportDateTime}"
                    };

                    // add the group info
                    if ( peopleGroup != null && peopleGroup.Id > 0 )
                    {
                        attendance.CampusId = peopleGroup.CampusId;

                        var groupLocationId = peopleGroup.GroupLocations.Select( gl => (int?)gl.LocationId ).FirstOrDefault();

                        var existingOccurrence = existingOccurrences
                            .FirstOrDefault( o =>
                                o.GroupId == peopleGroup.Id &&
                                o.OccurrenceDate == (DateTime)startDate &&
                                o.ScheduleId == archivedSchedule.Id &&
                                GroupTypeMeetingLocationId == groupLocationId
                            );

                        if ( existingOccurrence == null )
                        {
                            attendance.Occurrence = new AttendanceOccurrence
                            {
                                OccurrenceDate = (DateTime)startDate,
                                GroupId = peopleGroup.Id,
                                ScheduleId = archivedSchedule.Id,
                                LocationId = peopleGroup.GroupLocations.Select( gl => (int?)gl.LocationId ).FirstOrDefault(),
                            };
                            newOccurrences.Add( attendance.Occurrence );
                        }
                        else
                        {
                            attendance.OccurrenceId = existingOccurrence.Id;
                        }
                    }

                    newAttendances.Add( attendance );

                    completedItems++;
                    if ( completedItems % percentage < 1 )
                    {
                        var percentComplete = completedItems / percentage;
                        ReportProgress( percentComplete, $"{completedItems:N0} group attendances imported ({percentComplete}% complete)." );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SaveAttendances( newAttendances );
                        ReportPartialProgress();
                        foreach ( var o in newOccurrences )
                        {
                            existingOccurrences.Add( new ImportOccurrence
                            {
                                Id = o.Id,
                                GroupId = o.GroupId,
                                LocationId = o.LocationId,
                                ScheduleId = o.ScheduleId,
                                OccurrenceDate = o.OccurrenceDate
                            } );
                        }

                        // Reset lists and context
                        lookupContext.Dispose();
                        lookupContext = new RockContext();
                        newAttendances.Clear();
                    }
                }
            }

            if ( newAttendances.Any() )
            {
                SaveAttendances( newAttendances );
            }

            lookupContext.Dispose();
            ReportProgress( 100, $"Finished group attendance import: {completedItems:N0} attendances imported." );
        }

        /// <summary>
        /// Saves the attendances.
        /// </summary>
        /// <param name="newAttendances">The new attendances.</param>
        /// <param name="createWeeklySchedules">if set to <c>true</c> [create weekly schedules].</param>
        private static void SaveAttendances( List<Attendance> newAttendances, bool createWeeklySchedules = true )
        {
            if ( newAttendances.Count > 0 )
            {
                using ( var rockContext = new RockContext() )
                {
                    rockContext.Attendances.AddRange( newAttendances );
                    rockContext.SaveChanges();

                    if ( createWeeklySchedules )
                    {
                        var groupSchedules = newAttendances
                            .Where( a => a.Occurrence.GroupId.HasValue )
                            .DistinctBy( a => a.Occurrence.GroupId )
                            .ToDictionary( a => a.Occurrence.GroupId, a => a.StartDateTime );
                        foreach ( var group in rockContext.Groups.Where( g => groupSchedules.Keys.Contains( g.Id ) ) )
                        {
                            var attendanceDate = groupSchedules[group.Id];
                            group.Schedule = new Schedule
                            {
                                // Note: this depends on an iCal dependency at save
                                WeeklyDayOfWeek = attendanceDate.DayOfWeek,
                                WeeklyTimeOfDay = attendanceDate.TimeOfDay,
                                CreatedByPersonAliasId = ImportPersonAliasId,
                                CreatedDateTime = group.CreatedDateTime,
                                ForeignKey = $"Attendance imported {ImportDateTime}"
                            };
                        }

                        rockContext.SaveChanges();
                    }
                }
            }
        }
    }
}