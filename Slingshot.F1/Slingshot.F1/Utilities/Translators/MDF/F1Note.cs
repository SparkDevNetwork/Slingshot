using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1Note
    {
        /// <summary>
        /// Translate an F1 note into a PersonNote for Rock
        /// </summary>
        /// <param name="row">A single row from the F1 Notes table</param>
        /// <param name="headOfHouseHolds">The subset of F1 individual_household records that are heads of the house</param>
        /// <param name="users">The F1 Users table</param>
        /// <returns></returns>
        public static PersonNote Translate( DataRow row, Dictionary<int, int> headOfHouseHolds, DataRow[] users )
        {
            try
            {
                var individualId = row.Field<int?>( "Individual_ID" );
                var householdId = row.Field<int?>( "Household_ID" );

                // Sometimes notes are made for households. Since rock notes go on people, attach that note to the head of household
                if ( !individualId.HasValue && householdId.HasValue && headOfHouseHolds.TryGetValue( householdId.Value, out var headOfHouseholdId ) )
                {
                    individualId = headOfHouseholdId;
                }

                if ( !individualId.HasValue )
                {
                    // The note didn't indicate an individual and no valid head of household was found, so not sure what to attach
                    // this note
                    return null;
                }

                // Determine who the author is by referencing the user table
                var authorUserId = row.Field<int>( "NoteCreatedByUserID" );
                var authorUser = users.FirstOrDefault( u => u.Field<int>( "UserID" ) == authorUserId );
                var authorPersonId = authorUser?.Field<int>( "LinkedIndividualID" );

                // This field is used twice, so read it outside the literal declaration
                var noteTypeName = row.Field<string>( "Note_Type_Name" );

                var note = new PersonNote
                {
                    Id = row.Field<int>( "Note_ID" ),
                    PersonId = individualId.Value,
                    CreatedByPersonId = authorPersonId,
                    //Caption
                    DateTime = row.Field<DateTime?>( "NoteCreated" ),
                    IsAlert = IsAlert( noteTypeName ),
                    //IsPrivateNote
                    NoteType = noteTypeName,
                    Text = row.Field<string>( "Note_Text" )
                };

                return note;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Convert an F1 noteTypeName field to a Rock IsAlert value
        /// </summary>
        /// <param name="noteTypeName"></param>
        /// <returns></returns>
        private static bool IsAlert( string noteTypeName )
        {
            if ( string.IsNullOrWhiteSpace( noteTypeName ) )
            {
                return false;
            }

            // This is the only value I am aware of that would be an alert, but more might need to be added here in the future
            switch ( noteTypeName.ToLower().Trim() )
            {
                case "red flag issues":
                    return true;
                default:
                    return false;
            }
        }
    }
}
