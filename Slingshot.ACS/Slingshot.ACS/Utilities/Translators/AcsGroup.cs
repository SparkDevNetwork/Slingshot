using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;

using Slingshot.Core.Model;

namespace Slingshot.ACS.Utilities.Translators
{
    public static class AcsGroup
    {
        public static Group Translate( DataRow row )
        {
            var group = new Group();

            string groupName = row.Field<string>( "GroupName" );
            group.Name = groupName;

            // generate a unique group id
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( groupName ) );
            var groupId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
            if ( groupId > 0 )
            {
                group.Id = groupId;
            }

            group.Order = row.Field<int>( "GroupSort" );

            // using the "Imported Group" group type
            group.GroupTypeId = 9999;

            return group;
        }
    }
}
