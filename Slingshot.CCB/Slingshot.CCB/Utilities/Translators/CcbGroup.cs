using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.CCB.Utilities.Translators
{
    public static class CcbGroup
    {
        public static List<Group> Translate(XElement inputGroup )
        {
            List<Group> groups = new List<Group>();

            int? departmentId = null;
            int? directorId = null;

            var group = new Group();
            
            group.Id = inputGroup.Attribute("id").Value.AsInteger();
            group.Name = inputGroup.Element( "name" )?.Value;
            group.GroupTypeId = inputGroup.Element( "group_type" ).Attribute( "id" ).Value.AsInteger();
            group.CampusId = inputGroup.Element( "campus" ).Attribute( "id" ).Value.AsIntegerOrNull();

            if ( group.GroupTypeId != 0 )
            {
                groups.Add( group );
            }

            // add the department as a group with an id of 9999 + its id to create a unique group id for it
            if (inputGroup.Element("department") != null && inputGroup.Element( "department" ).Attribute("id") != null && inputGroup.Element("department").Attribute("id").Value.IsNotNullOrWhitespace() )
            {
                departmentId = ( "9999" + inputGroup.Element( "department" ).Attribute( "id" ).Value ).AsInteger();
                var departmentName = inputGroup.Element( "department" ).Value;
                if ( departmentName.IsNullOrWhiteSpace() )
                {
                    departmentName = "No Department Name";
                }
                groups.Add( new Group { Id = departmentId.Value, Name = inputGroup.Element( "department" ).Value, GroupTypeId = 9999 } );
            }

            // add the director as a group with an id of 9998 + its id to create a unique group id for it
            if ( inputGroup.Element( "director" ) != null && inputGroup.Element( "director" ).Attribute( "id" ) != null && inputGroup.Element( "director" ).Attribute( "id" ).Value.IsNotNullOrWhitespace() )
            {
                directorId = ( "9998" + inputGroup.Element( "director" ).Attribute( "id" ).Value ).AsInteger();

                var directorGroup = new Group();
                directorGroup.Id = directorId.Value;
                directorGroup.Name = inputGroup.Element( "director" ).Element( "full_name" ).Value;
                directorGroup.GroupTypeId = 9998;

                // add parent group of the department if it exists
                if ( departmentId.HasValue )
                {
                    directorGroup.ParentGroupId = departmentId.Value;
                }

                directorGroup.GroupMembers.Add( new GroupMember { PersonId = inputGroup.Element( "director" ).Attribute( "id" ).Value.AsInteger(), Role = "Leader", GroupId = directorGroup.Id } );

                groups.Add( directorGroup );
            }

            // add leader
            if ( inputGroup.Element( "main_leader" ).Attribute("id") != null && inputGroup.Element( "main_leader" ).Attribute( "id" ).Value.AsInteger() != 0 )
            {
                group.GroupMembers.Add( new GroupMember { PersonId = inputGroup.Element( "main_leader" ).Attribute( "id" ).Value.AsInteger(), Role = "Leader", GroupId = group.Id } );
            }

            // add assistant leaders
            if ( inputGroup.Element( "leaders" ) != null )
            {
                foreach(var leaderNode in inputGroup.Element( "leaders" ).Elements("leader" ) )
                {
                    group.GroupMembers.Add( new GroupMember { PersonId = leaderNode.Attribute( "id" ).Value.AsInteger(), Role = "Assistant Leader", GroupId = group.Id } );
                }
            }

            // add participants
            if ( inputGroup.Element( "participants" ) != null )
            {
                foreach ( var participantNode in inputGroup.Element( "participants" ).Elements( "participant" ) )
                {
                    group.GroupMembers.Add( new GroupMember { PersonId = participantNode.Attribute( "id" ).Value.AsInteger(), Role = "Member", GroupId = group.Id } );
                }
            }

            // determine the parent group
            if ( directorId.HasValue )
            {
                group.ParentGroupId = directorId.Value;
            }
            else if ( departmentId.HasValue ){
                group.ParentGroupId = departmentId.Value;
            }

            return groups;
        }
    }
}
