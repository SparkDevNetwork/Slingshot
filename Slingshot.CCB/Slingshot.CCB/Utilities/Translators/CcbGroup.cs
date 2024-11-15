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
        public static List<Group> Translate( XElement inputGroup )
        {
            List<Group> groups = new List<Group>();

            int? departmentId = null;
            int? directorId = null;

            var group = new Group();

            group.Id = inputGroup.Attribute( "id" ).Value.AsInteger();
            group.Name = inputGroup.Element( "name" )?.Value;
            group.Description = inputGroup.Element( "description" )?.Value.RemoveCrLf();
            group.GroupTypeId = inputGroup.Element( "group_type" ).Attribute( "id" ).Value.AsInteger();
            group.CampusId = inputGroup.Element( "campus" ).Attribute( "id" ).Value.AsIntegerOrNull();
            group.Capacity = inputGroup.Element( "group_capacity" ).Value.AsIntegerOrNull();
            group.IsActive = !inputGroup.Element( "inactive" ).Value.AsBoolean();
            group.IsPublic = inputGroup.Element( "public_search_listed" ).Value.AsBoolean();
            group.MeetingDay = inputGroup.Element( "meeting_day" ).Value;
            group.MeetingTime = inputGroup.Element( "meeting_time" ).Value;

            ProcessBooleanAttribute( group, inputGroup.Element( "childcare_provided" ), "HasChildcare" );

            if ( group.GroupTypeId == 0 )
            {
                group.GroupTypeId = CcbApi.GROUPTYPE_UNKNOWN_ID;
            }

            groups.Add( group );


            var importedDepartmentId = inputGroup.Element( "department" )?.Attribute( "id" )?.Value;
            var importedDirectorId = inputGroup.Element( "director" )?.Attribute( "id" )?.Value;

            var hasDepartment = importedDepartmentId.IsNotNullOrWhitespace();
            var hasDirector = importedDirectorId.IsNotNullOrWhitespace();
            
            // add the department as a group with an id of 9999 + its id to create a unique group id for it
            if ( hasDepartment )
            {
                departmentId = ( "9999" + importedDepartmentId ).AsInteger();

                var departmentName = inputGroup.Element( "department" ).Value;
                departmentName = departmentName.IsNullOrWhiteSpace() ? string.Empty : "No Department Name";

                var departmentGroup = new Group
                {
                    Id = departmentId.Value,
                    IsActive = true,
                    Name = departmentName,
                    GroupTypeId = 9999
                };

                groups.Add( departmentGroup );
            }

            // add the director as a group with an id of 9998 + its id to create a unique group id for it
            if ( hasDirector )
            {
                if ( hasDepartment )
                {
                    directorId = ( "9998" + importedDepartmentId + importedDirectorId ).AsInteger();
                }
                else
                {
                    directorId = ( "9998" + importedDirectorId ).AsInteger();
                }

                var directorName = inputGroup.Element( "director" ).Element( "full_name" ).Value;

                var directorGroup = new Group
                {
                    Id = directorId.Value,
                    IsActive = true,
                    Name = directorName,
                    GroupTypeId = 9998
                };

                // add parent group of the department if it exists
                if ( hasDepartment )
                {
                    directorGroup.ParentGroupId = departmentId.Value;
                }

                var directorMember = new GroupMember
                {
                    PersonId = importedDirectorId.AsInteger(),
                    Role = "Leader",
                    GroupId = directorGroup.Id
                };

                directorGroup.GroupMembers.Add( directorMember );
                groups.Add( directorGroup );
            }

            // add leader
            if ( inputGroup.Element( "main_leader" ).Attribute( "id" ) != null && inputGroup.Element( "main_leader" ).Attribute( "id" ).Value.AsInteger() != 0 )
            {
                group.GroupMembers.Add( new GroupMember { PersonId = inputGroup.Element( "main_leader" ).Attribute( "id" ).Value.AsInteger(), Role = "Leader", GroupId = group.Id } );
            }

            // add assistant leaders
            if ( inputGroup.Element( "leaders" ) != null )
            {
                foreach ( var leaderNode in inputGroup.Element( "leaders" ).Elements( "leader" ) )
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

            var addressList = inputGroup.Element( "addresses" ).Elements( "address" );
            foreach ( var address in addressList )
            {
                if ( address.Element( "street_address" ) != null && address.Element( "street_address" ).Value.IsNotNullOrWhitespace() )
                {
                    var importAddress = new GroupAddress();
                    importAddress.GroupId = group.Id;
                    importAddress.Street1 = address.Element( "street_address" ).Value.RemoveCrLf();
                    importAddress.City = address.Element( "city" ).Value;
                    importAddress.State = address.Element( "state" ).Value;
                    importAddress.PostalCode = address.Element( "zip" ).Value;
                    importAddress.Latitude = address.Element( "latitude" )?.Value;
                    importAddress.Longitude = address.Element( "longitude" )?.Value;
                    importAddress.Country = address.Element( "country" )?.Value;

                    var addressType = address.Attribute( "type" ).Value;

                    switch ( addressType )
                    {
                        case "mailing":
                        case "home":
                            {
                                importAddress.AddressType = AddressType.Home;
                                importAddress.IsMailing = addressType.Equals( "mailing" );
                                break;
                            }
                        case "work":
                            {
                                importAddress.AddressType = AddressType.Work;
                                break;
                            }
                        case "other":
                            {
                                importAddress.AddressType = AddressType.Other;
                                break;
                            }
                    }

                    // only add the address if we have a valid address
                    if ( importAddress.Street1.IsNotNullOrWhitespace() && importAddress.City.IsNotNullOrWhitespace() && importAddress.PostalCode.IsNotNullOrWhitespace() )
                    {
                        group.Addresses.Add( importAddress );
                    }
                }
            }

            // determine the parent group
            if ( directorId.HasValue )
            {
                group.ParentGroupId = directorId.Value;
            }
            else if ( departmentId.HasValue )
            {
                group.ParentGroupId = departmentId.Value;
            }

            return groups;
        }

        /// <summary>
        /// Processes the string attribute.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeKey">The attribute key.</param>
        private static void ProcessStringAttribute( Group group, XElement element, string attributeKey )
        {
            if ( element != null && element.Value.IsNotNullOrWhitespace() )
            {
                group.Attributes.Add( new GroupAttributeValue { GroupId = group.Id, AttributeKey = attributeKey, AttributeValue = element.Value } );
            }
        }

        /// <summary>
        /// Processes the boolean attribute.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeKey">The attribute key.</param>
        private static void ProcessBooleanAttribute( Group group, XElement element, string attributeKey )
        {
            if ( element != null && element.Value.IsNotNullOrWhitespace() )
            {
                group.Attributes.Add( new GroupAttributeValue { GroupId = group.Id, AttributeKey = attributeKey, AttributeValue = element.Value.AsBoolean().ToString() } );
            }
        }

        /// <summary>
        /// Processes the datetime attribute.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="element">The element.</param>
        /// <param name="attributeKey">The attribute key.</param>
        private static void ProcessDatetimeAttribute( Group group, XElement element, string attributeKey )
        {
            if ( element != null && element.Value.AsDateTime().HasValue )
            {
                group.Attributes.Add( new GroupAttributeValue { GroupId = group.Id, AttributeKey = attributeKey, AttributeValue = element.Value.AsDateTime().ToString() } );
            }
        }
    }
}