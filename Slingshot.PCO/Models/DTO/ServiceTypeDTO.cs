using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class ServiceTypeDTO
    {
        public int Id { get; set; }

        public DateTime? ArchivedAt { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? DeletedAt { get; set; }

        public string Name { get; set; }

        public int? Sequence { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool? AttachmentTypesEnabled { get; set; }

        public string BackgroundCheckPermissions { get; set; }

        public string CommentPermissions { get; set; }

        public string Frequency { get; set; }

        public string LastPlanFrom { get; set; }

        public string Permissions { get; set; }

        public ServiceTypeDTO( DataItem data )
        {
            Id = data.Id;
            ArchivedAt = data.Item.archived_at;
            CreatedAt = data.Item.created_at;
            DeletedAt = data.Item.deleted_at;
            Name = data.Item.name;
            Sequence = data.Item.sequence;
            UpdatedAt = data.Item.updated_at;
            AttachmentTypesEnabled = data.Item.attachment_types_enabled;
            BackgroundCheckPermissions = data.Item.background_check_permissions;
            CommentPermissions = data.Item.comment_permissions;
            Frequency = data.Item.frequency;
            LastPlanFrom = data.Item.last_plan_from;
            Permissions = data.Item.permissions;
        }
    }
}
