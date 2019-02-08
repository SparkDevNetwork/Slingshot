using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;

using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    public static class SkGroupMember
    {
        public static GroupMember Translate( DataRow row, int GroupID )
        {
            GroupMember member = new GroupMember();

            member.PersonId = row.Field<int>("PERSON_ID");
            member.GroupId = GroupID;
            member.Role = "Member";

            return member;
        }
    }
}
