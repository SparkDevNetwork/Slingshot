using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;

using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    public static class SkGroup
    {
        public static Group Translate( DataRow row, Group parent )
        {
            Group group = new Group();

            group.Id = row.Field<int>("GROUP_ID");
            group.Name = row.Field<string>( "GROUP_NAME" );
            group.IsActive = true;
            group.IsPublic = true;
            group.GroupTypeId = parent.GroupTypeId;
            group.ParentGroupId = parent.Id;

            // group.Order = row.Field<int>( "GroupSort" );
            return group;
        }
    }
}
