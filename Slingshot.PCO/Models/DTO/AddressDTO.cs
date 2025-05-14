using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class AddressDTO
    {
        public int Id { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Street { get; set; }

        public string Street2 { get; set; }

        public string Zip { get; set; }

        public string Country { get; set; }

        public string Location { get; set; }

        public AddressDTO( DataItem data )
        {
            Id = data.Id;
            City = data.Item.city;
            State = data.Item.state;
            Zip = data.Item.zip;
            Location = data.Item.location;
            Country = data.Item.country_code;
            SetStreet( data );
        }

        private void SetStreet( DataItem data )
        {
            // Check for "street" returned from the API.
            // If it is not found, then we will check for Street Line 1 and Street Line 2.

            if ( data.Item.street != null )
            {
                var street1 = ( string ) data.Item.street;
                Street = street1;
            }
            else if ( data.Item.street_line_1 != null )
            {
                var street1 = ( string ) data.Item.street_line_1;
                Street = street1;
            }

            if ( data.Item.street_line_2 != null )
            {
                var street2 = ( string ) data.Item.street_line_2;
                Street2 = street2;
            }
        }
    }
}
