using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class AddressDTO
    {
        public int Id { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Street { get; set; }

        public string Zip { get; set; }

        public string Location { get; set; }

        public AddressDTO( DataItem data )
        {
            Id = data.Id;
            City = data.Item.city;
            State = data.Item.state;
            Street = data.Item.street;
            Zip = data.Item.zip;
            Location = data.Item.location;
        }
    }
}
