using System.Collections.Generic;

using System.Data;

namespace Slingshot.F1.Utilities
{
    public partial class F1Mdb : F1Translator
    {
        /// <summary>
        /// Gets a dictionary mapping of <see cref="HeadOfHousehold"/> objects.
        /// </summary>
        /// <param name="dtHoh">The <see cref="DataTable"/> with Head of Household data.</param>
        private static Dictionary<int, HeadOfHousehold> GetHeadOfHouseholdMap( DataTable dtHoh )
        {
            if ( _HeadOfHouseholdMapCache != null)
            {
                return _HeadOfHouseholdMapCache;
            }

            _HeadOfHouseholdMapCache = new Dictionary<int, HeadOfHousehold>();

            foreach ( DataRow headOfHousehold in dtHoh.Rows )
            {
                var individualId = headOfHousehold.Field<int>( "individual_id" );
                var householdId = headOfHousehold.Field<int>( "household_id" );
                var subStatusName = headOfHousehold.Field<string>( "SubStatus_Name" );

                _HeadOfHouseholdMapCache[householdId] = new HeadOfHousehold
                {
                    IndividualId = individualId,
                    SubStatusName = subStatusName
                };
            }

            return _HeadOfHouseholdMapCache;
        }
    }

}
