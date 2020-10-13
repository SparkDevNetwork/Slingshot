using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class EmailAddressDTO
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string Location { get; set; }

        public bool Primary { get; set; }

        public EmailAddressDTO( DataItem data )
        {
            Id = data.Id;
            Address = data.Item.address;
            Location = data.Item.location;
            Primary = data.Item.primary;
        }
    }
}
