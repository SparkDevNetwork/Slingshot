using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1Group
    {
        public static Group Translate( DataRow row, DataTable members )
        {
            var group = new Group();

            if ( row.Field<int?>( "Group_Id" ).HasValue )
            {
                group.Id = row.Field<int?>( "Group_Id" ).Value;
            }
            else
            {

                if ( !string.IsNullOrWhiteSpace( row.Field<string>( "Group_Name" ) ) && row.Field<int?>( "ParentGroupId" ).HasValue )
                {
                    MD5 md5Hasher = MD5.Create();
                    var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( row.Field<string>( "Group_Name" ) + row.Field<int?>( "ParentGroupId" ).Value ) );
                    var groupId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                    if ( groupId > 0 )
                    {
                        group.Id = groupId;
                    }
                }
                else
                {
                    return null;
                }
            }

            group.Name = row.Field<string>( "Group_Name" );
            group.GroupTypeId = row.Field<int>( "Group_Type_ID" );
            group.IsActive = row.Field<int>( "is_active" ) != 0;
            group.IsPublic = row.Field<int>( "is_public" ) != 0;
            group.MeetingDay = row.Field<string>( "ScheduleDay" );
            group.ParentGroupId = row.Field<int?>( "ParentGroupId" ) ?? 90000000 + group.GroupTypeId;
            group.Description = row.Field<string>( "Description" );

            if( !string.IsNullOrWhiteSpace( row.Field<string>( "StartHour" ) ) )
            {
                group.MeetingTime = row.Field<string>( "StartHour" ) + ":00";
            }

            var importAddress = new GroupAddress();
            importAddress.GroupId = group.Id;
            importAddress.Street1 = row.Field<string>( "Address1" );
            importAddress.Street2 = row.Field<string>( "Address2" );
            importAddress.City = row.Field<string>( "City" );
            importAddress.State = row.Field<string>( "StProvince" );
            importAddress.PostalCode = row.Field<string>( "PostalCode" );
            importAddress.Country = row.Field<string>( "country" );
            importAddress.AddressType = AddressType.Other;


            // only add the address if we have a valid address
            if ( importAddress.Street1.IsNotNullOrWhitespace() &&
                    importAddress.City.IsNotNullOrWhitespace() &&
                    importAddress.PostalCode.IsNotNullOrWhitespace() )
            {
                group.Addresses.Add( importAddress );

            }

            foreach( var member in members.Select( "Group_Id =" + group.Id ) )
            {
                var groupMember = new GroupMember();
                groupMember.GroupId = group.Id;
                groupMember.PersonId = member.Field<int>( "Individual_ID" );
                groupMember.Role = member.Field<string>( "Group_Member_Type" );

                group.GroupMembers.Add( groupMember );
            }

            return group;
        }
    }
}
