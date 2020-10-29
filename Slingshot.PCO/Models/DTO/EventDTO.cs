using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class EventDTO
    {
        public int Id { get; set; }

        public bool? AttendanceRequestsEnabled { get; set; }

        public bool? AutomatedReminderEnabled { get; set; }

        public bool? Canceled { get; set; }

        public DateTime? CanceledAt { get; set; }

        public string Description { get; set; }

        public DateTime? EndsAt { get; set; }

        public string LocationTypePreference { get; set; }

        public bool? MultiDay { get; set; }

        public string Name { get; set; }

        public bool? RemindersSent { get; set; }

        public DateTime? RemindersSentAt { get; set; }

        public bool? Repeating { get; set; }

        public DateTime? StartsAt { get; set; }

        public string VirtualLocationUrl { get; set; }

        public EventDTO( DataItem data )
        {
            Id = data.Id;
            AttendanceRequestsEnabled = data.Item.attendance_requests_enabled;
            AutomatedReminderEnabled = data.Item.automated_reminder_enabled;
            Canceled = data.Item.canceled;
            CanceledAt = data.Item.canceled_at;
            Description = ( ( string ) data.Item.description ).StripHtml();
            EndsAt = data.Item.ends_at;
            LocationTypePreference = data.Item.location_type_preference;
            MultiDay = data.Item.multi_day;
            Name = data.Item.name;
            RemindersSent = data.Item.reminders_sent;
            RemindersSentAt = data.Item.reminders_sent_at;
            Repeating = data.Item.repeating;
            StartsAt = data.Item.starts_at;
            VirtualLocationUrl = data.Item.virtual_location_url;
        }
    }
}
