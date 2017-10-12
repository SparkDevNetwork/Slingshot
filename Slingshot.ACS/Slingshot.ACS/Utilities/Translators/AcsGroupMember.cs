using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;

using Slingshot.Core.Model;

namespace Slingshot.ACS.Utilities.Translators
{
    public static class AcsGroupMember
    {
        public static GroupMember Translate( DataRow row )
        {
            var groupMember = new GroupMember();

            groupMember.PersonId = row.Field<int>( "IndividualId" );

            var groupName = row.Field<string>( "GroupName" );

            // generate a unique group id
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( groupName ) );
            var groupId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
            if ( groupId > 0 )
            {
                groupMember.GroupId = groupId;
            }

            groupMember.Role = row.Field<string>( "Position" );

            return groupMember;
        }
    }
}
