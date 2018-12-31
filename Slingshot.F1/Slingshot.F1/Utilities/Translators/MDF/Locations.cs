using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Slingshot.F1.Utilities.Translators
{
    public partial class F1Location
    {
        /// <summary>
        /// Translates the family address.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateFamilyAddress( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var locationService = new LocationService( lookupContext );

            var familyGroupMemberList = new GroupMemberService( lookupContext ).Queryable().AsNoTracking()
                .Where( gm => gm.Group.GroupType.Guid.Equals( new Guid( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ) ) ).ToList();

            var customLocationTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.GROUP_LOCATION_TYPE ), lookupContext ).DefinedValues;

            const string otherGroupLocationName = "Other (Imported)";
            var otherGroupLocationTypeId = customLocationTypes.Where( dv => dv.TypeName == otherGroupLocationName )
                .Select( v => (int?)v.Id ).FirstOrDefault();
            if ( !otherGroupLocationTypeId.HasValue )
            {
                var otherGroupLocationType = AddDefinedValue( lookupContext, Rock.SystemGuid.DefinedType.GROUP_LOCATION_TYPE, otherGroupLocationName );
                customLocationTypes.Add( otherGroupLocationType );
                otherGroupLocationTypeId = otherGroupLocationType.Id;
            }

            var newGroupLocations = new List<GroupLocation>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completed = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying address import ({totalRows:N0} found)." );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var individualId = row["Individual_ID"] as int?;
                var householdId = row["Household_ID"] as int?;
                var personKeys = GetPersonKeys( individualId, householdId, includeVisitors: false );
                if ( personKeys != null )
                {
                    var familyGroup = familyGroupMemberList.Where( gm => gm.PersonId == personKeys.PersonId )
                        .Select( gm => gm.Group ).FirstOrDefault();

                    if ( familyGroup != null )
                    {
                        var groupLocation = new GroupLocation();

                        var street1 = row["Address_1"] as string;
                        var street2 = row["Address_2"] as string;
                        var city = row["City"] as string;
                        var state = row["State"] as string;
                        var country = row["country"] as string; // NOT A TYPO: F1 has property in lower-case
                        var zip = row["Postal_Code"] as string ?? string.Empty;

                        // restrict zip to 5 places to prevent duplicates
                        var familyAddress = locationService.Get( street1, street2, city, state, zip.Left( 5 ), country, verifyLocation: false );

                        if ( familyAddress != null && !familyGroup.GroupLocations.Any( gl => gl.LocationId == familyAddress.Id ) )
                        {
                            familyAddress.CreatedByPersonAliasId = ImportPersonAliasId;
                            familyAddress.Name = familyGroup.Name;
                            familyAddress.IsActive = true;

                            groupLocation.GroupId = familyGroup.Id;
                            groupLocation.LocationId = familyAddress.Id;
                            groupLocation.IsMailingLocation = false;
                            groupLocation.IsMappedLocation = false;

                            var addressType = row["Address_Type"].ToString();
                            if ( addressType.Equals( "Primary", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                groupLocation.GroupLocationTypeValueId = HomeLocationTypeId;
                                groupLocation.IsMailingLocation = true;
                                groupLocation.IsMappedLocation = true;
                            }
                            else if ( addressType.Equals( "Business", StringComparison.CurrentCultureIgnoreCase ) || addressType.StartsWith( "Org", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                groupLocation.GroupLocationTypeValueId = WorkLocationTypeId;
                            }
                            else if ( addressType.Equals( "Previous", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                groupLocation.GroupLocationTypeValueId = PreviousLocationTypeId;
                            }
                            else if ( !string.IsNullOrWhiteSpace( addressType ) )
                            {
                                // look for existing group location types, otherwise mark as imported
                                var customTypeId = customLocationTypes.Where( dv => dv.Value.Equals( addressType, StringComparison.CurrentCultureIgnoreCase ) )
                                    .Select( dv => (int?)dv.Id ).FirstOrDefault();
                                groupLocation.GroupLocationTypeValueId = customTypeId ?? otherGroupLocationTypeId;
                            }

                            familyGroup.GroupLocations.Add( groupLocation );
                            newGroupLocations.Add( groupLocation );
                            completed++;

                            if ( completed % percentage < 1 )
                            {
                                var percentComplete = completed / percentage;
                                ReportProgress( percentComplete, $"{completed:N0} addresses imported ({percentComplete}% complete)." );
                            }
                            else if ( completed % ReportingNumber < 1 )
                            {
                                SaveFamilyAddress( newGroupLocations );

                                // Reset context
                                newGroupLocations.Clear();
                                lookupContext = new RockContext();
                                locationService = new LocationService( lookupContext );

                                ReportPartialProgress();
                            }
                        }
                    }
                }
            }

            if ( newGroupLocations.Any() )
            {
                SaveFamilyAddress( newGroupLocations );
            }

            ReportProgress( 100, $"Finished address import: {completed:N0} addresses imported." );
        }

        /// <summary>
        /// Saves the family address.
        /// </summary>
        /// <param name="newGroupLocations">The new group locations.</param>
        private static void SaveFamilyAddress( List<GroupLocation> newGroupLocations )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.BulkInsert( newGroupLocations );
            }
        }
    }
}