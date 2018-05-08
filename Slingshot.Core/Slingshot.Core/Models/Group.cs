using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for Group
    /// </summary>
    public class Group : IImportModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the parent group identifier.
        /// </summary>
        /// <value>
        /// The parent group identifier.
        /// </value>
        public int ParentGroupId { get; set; }

        /// <summary>
        /// Gets or sets the group type identifier.
        /// </summary>
        /// <value>
        /// The group type identifier.
        /// </value>
        public int GroupTypeId { get; set; }

        /// <summary>
        /// Gets or sets the campus identifier.
        /// </summary>
        /// <value>
        /// The campus identifier.
        /// </value>
        public int? CampusId { get; set; }

        /// <summary>
        /// Gets or sets the group capacity.
        /// </summary>
        /// <value>
        /// The group capacity.
        /// </value>
        public int? Capacity { get; set; }

        /// <summary>
        /// Gets or sets the meeting day.
        /// </summary>
        /// <value>
        /// The meeting day.
        /// </value>
        public string MeetingDay { get; set; }

        /// <summary>
        /// Gets or sets the meeting time.
        /// </summary>
        /// <value>
        /// The meeting time.
        /// </value>
        ///
        public string MeetingTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this group is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this group is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this group is public.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this group is public; otherwise, <c>false</c>.
        /// </value>
        public bool IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the group members.
        /// </summary>
        /// <value>
        /// The group members.
        /// </value>
        public List<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public List<GroupAttributeValue> Attributes { get; set; } = new List<GroupAttributeValue>();

        /// <summary>
        /// Gets or sets the addresses.
        /// </summary>
        /// <value>
        /// The addresses.
        /// </value>
        public List<GroupAddress> Addresses { get; set; } = new List<GroupAddress>();

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "group.csv";
        }
    }
}