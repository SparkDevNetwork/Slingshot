using System.Collections.Generic;
using System.Data;

namespace Slingshot.F1.Utilities
{
    public partial class F1Sql : F1Translator
    {
        /// <summary>
        /// Gets a dictionary mapping of <see cref="HeadOfHousehold"/> objects.
        /// </summary>
        /// <param name="dtHoh">The <see cref="DataTable"/> with Head of Household data.</param>
        private static Dictionary<int, HeadOfHousehold> GetHeadOfHouseholdMap( DataTable dtIndividuals )
        {
            if ( _HeadOfHouseholdMapCache != null)
            {
                return _HeadOfHouseholdMapCache;
            }

            _HeadOfHouseholdMapCache = new Dictionary<int, HeadOfHousehold>();

            var householdPositionIndex = new Dictionary<int, int>();

            foreach ( DataRow drIndividual in dtIndividuals.Rows )
            {
                var householdId = drIndividual.Field<int>( "household_id" );
                if ( householdPositionIndex.ContainsKey( householdId ) && householdPositionIndex[householdId] == 10 )
                {
                    // We already found a Head of Household for this family, so we can skip this record.
                    continue;
                }

                var position = drIndividual.Field<string>( "Household_Position" ).ToLower();
                int positionIndex = 0;
                switch ( position )
                {
                    case "head":
                        positionIndex = 10;
                        break;
                    case "spouse":
                        positionIndex = 8;
                        break;
                    case "child":
                        positionIndex = 6;
                        break;

                    case "Other":
                        positionIndex = 4;
                        break;
                    case "Visitor":
                        positionIndex = 2;
                        break;
                }

                if ( householdPositionIndex.ContainsKey( householdId ) && householdPositionIndex[householdId] >= positionIndex )
                {
                    // This record is not in a higher position than the one we already found, so we can ignore it.
                    continue;
                }

                var individualId = drIndividual.Field<int>( "individual_id" );
                var subStatusName = drIndividual.Field<string>( "SubStatus_Name" );
                var individual = new HeadOfHousehold
                {
                    IndividualId = individualId,
                    SubStatusName = subStatusName
                };

                if ( householdPositionIndex.ContainsKey( householdId ) )
                {
                    // This record is a better match than the one already assigned, so replace the existing one.
                    householdPositionIndex[householdId] = positionIndex;
                    _HeadOfHouseholdMapCache[householdId] = individual;
                }
                else
                {
                    // This is the first time we've seen this family, so we'll assume (for now) that this is the
                    // best match.  We'll replace it later if we find a better one.
                    householdPositionIndex.Add( householdId, positionIndex );
                    _HeadOfHouseholdMapCache.Add( householdId, individual );
                }

            }

            return _HeadOfHouseholdMapCache;
        }
    }
}
