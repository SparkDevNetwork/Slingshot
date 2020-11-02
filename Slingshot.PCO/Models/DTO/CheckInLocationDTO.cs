using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities.AddressParser;
using System;

namespace Slingshot.PCO.Models.DTO
{
    public class CheckInLocationDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Kind { get; set; }

        public bool? Opened { get; set; }

        public int? MinAgeInMonths { get; set; }

        public int? MaxAgeInMonths { get; set; }

        public string AgeRangeBy { get; set; }

        public DateTime? AgeOn { get; set; }

        public string ChildOrAdult { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public string Gender { get; set; }

        public int? MinGrade { get; set; }

        public int? MaxGrade { get; set; }

        public int? MaxOccupancy { get; set; }

        public int? Position { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CreatedAt { get; set; }


        public CheckInLocationDTO( DataItem data )
        {
            Id = data.Id;
            Name = data.Item.name;
            Kind = data.Item.kind;
            Opened = data.Item.opened;
            MinAgeInMonths = data.Item.age_min_in_months;
            MaxAgeInMonths = data.Item.age_max_in_months;
            AgeRangeBy = data.Item.age_range_by;
            AgeOn = data.Item.age_on;
            ChildOrAdult = data.Item.child_or_adult;
            EffectiveDate = data.Item.effective_date;
            Gender = data.Item.gender;
            MinGrade = data.Item.grade_min;
            MaxGrade = data.Item.grade_max;
            MaxOccupancy = data.Item.max_occupancy;
            Position = data.Item.position;
            UpdatedAt = data.Item.updated_at;
            CreatedAt = data.Item.created_at;
        }
    }
}
