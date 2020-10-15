using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.F1.Utilities.SQL.DTO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Slingshot.F1.Utilities.Translators.SQL
{
    public static class F1Group
    {
        public static Group Translate( GroupDTO group, List<GroupMemberDTO> members, DataTable staffing )
        {
            var slingshotGroup = new Group();

            if ( group.GroupId.HasValue )
            {
                slingshotGroup.Id = group.GroupId.Value;
            }
            else
            {

                if ( !string.IsNullOrWhiteSpace( group.GroupName ) && group.ParentGroupId.HasValue )
                {
                    MD5 md5Hasher = MD5.Create();
                    string valueToHash = $"{ group.GroupName }{ group.ParentGroupId.Value }";
                    var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( valueToHash ) );
                    var groupId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                    if ( groupId > 0 )
                    {
                        slingshotGroup.Id = groupId;
                    }
                }
                else
                {
                    return null;
                }
            }

            // Limit the group name to 50 since the schedules that will be created in Rock use the group's name as the schedule name and
            // that field is limited in the database to 50 characters
            slingshotGroup.Name = group.GroupName.Left( 50 );
            slingshotGroup.GroupTypeId = group.GroupTypeId;
            slingshotGroup.IsActive = group.IsActive;
            if( group.IsPublic.HasValue )
            {
                slingshotGroup.IsPublic = group.IsPublic.Value;
            }
            slingshotGroup.MeetingDay = group.ScheduleDay;
            slingshotGroup.ParentGroupId = group.ParentGroupId ?? 90000000 + slingshotGroup.GroupTypeId;
            slingshotGroup.Description = group.Description;

            if( !string.IsNullOrWhiteSpace( group.StartHour ) )
            {
                slingshotGroup.MeetingTime = group.StartHour + ":00";
            }

            var importAddress = new GroupAddress();
            importAddress.GroupId = slingshotGroup.Id;
            importAddress.Street1 = group.Address1;
            importAddress.Street2 = group.Address2;
            importAddress.City = group.City;
            importAddress.State = group.StateProvince;
            importAddress.PostalCode = group.PostalCode;
            importAddress.Country = group.Country;
            importAddress.AddressType = AddressType.Other;


            // only add the address if we have a valid address
            if ( importAddress.Street1.IsNotNullOrWhitespace() &&
                    importAddress.City.IsNotNullOrWhitespace() &&
                    importAddress.PostalCode.IsNotNullOrWhitespace() )
            {
                slingshotGroup.Addresses.Add( importAddress );

            }

            var groupMembers = members.AsEnumerable().Where( m => m.GroupId == slingshotGroup.Id );

            foreach( var member in groupMembers )
            {
                var groupMember = new GroupMember();
                groupMember.GroupId = slingshotGroup.Id;
                groupMember.PersonId = member.IndividualId;
                groupMember.Role = member.GroupMemberType;

                slingshotGroup.GroupMembers.Add( groupMember );
            }

            if ( staffing != null )
            {
                foreach ( var staff in staffing.Select( $"RLC_ID = { slingshotGroup.Id } OR Activity_ID = { slingshotGroup.Id }" ) )
                {
                    var groupMember = new GroupMember();
                    groupMember.GroupId = slingshotGroup.Id;
                    groupMember.PersonId = staff.Field<int>( "INDIVIDUAL_ID" );
                    groupMember.Role = "Staff";

                    slingshotGroup.GroupMembers.Add( groupMember );
                }
            }

            return slingshotGroup;
        }
    }
}
