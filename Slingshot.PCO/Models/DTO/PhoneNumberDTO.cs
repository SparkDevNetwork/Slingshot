using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class PhoneNumberDTO
    {
        public int Id { get; set; }

        public string Number { get; set; }

        public string Location { get; set; }

        public PhoneNumberDTO( DataItem data )
        {
            Id = data.Id;
            Number = data.Item.number;
            Location = data.Item.location;
        }
    }
}
