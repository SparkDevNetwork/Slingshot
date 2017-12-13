using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators
{
    public static class F1Group
    {
        public static Group Translate( XElement inputGroup, int parentGroupId, IEnumerable<XElement> inputMembers )
        {
            var group = new Group();

            group.Id = inputGroup.Attribute( "id" ).Value.AsInteger();
            group.Name = inputGroup.Element( "name" ).Value;

            group.ParentGroupId = parentGroupId;
            group.GroupTypeId = inputGroup.Element( "groupType" ).Attribute( "id" ).Value.AsInteger();

            foreach ( var inputMember in inputMembers.Elements() )
            {
                var groupMember = new GroupMember();
                groupMember.GroupId = group.Id;
                groupMember.PersonId = inputMember.Element( "person" ).Attribute( "id" ).Value.AsInteger();
                groupMember.Role = inputMember.Element( "memberType" ).Element( "name" ).Value;

                group.GroupMembers.Add( groupMember );
            }


            return group;
        }
    }
}
