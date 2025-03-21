using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibElvanto.Utilities
{
    public class IdLookupManager
    {
        private int currentInt = 0;
        private Dictionary<string, int> lookup = new Dictionary<string, int>();
        public IdLookupManager( int startInt = 100 )
        {
            currentInt = startInt;
        }

        public int GetId( string? key )
        {
            if ( string.IsNullOrWhiteSpace( key ) )
            {
                return 0;
            }

            if ( lookup.ContainsKey( key ) )
            {
                return lookup[key];
            }

            lookup.Add( key, currentInt );
            currentInt++;
            return lookup[key];
        }
    }
}
