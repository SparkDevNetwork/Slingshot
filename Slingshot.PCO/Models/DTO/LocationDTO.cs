using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities.AddressParser;

namespace Slingshot.PCO.Models.DTO
{
    public class LocationDTO
    {
        public int Id { get; set; }

        public string DisplayPreference { get; set; }

        public string FullFormattedAddress { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        public string Name { get; set; }

        public string Radius { get; set; }

        public string Strategy { get; set; }

        public bool IsValid { get; set; }

        public string Street { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Zip { get; set; }


        public LocationDTO( DataItem data )
        {
            Id = data.Id;
            DisplayPreference = data.Item.display_preference;
            FullFormattedAddress = data.Item.full_formatted_address;
            Latitude = data.Item.latitude;
            Longitude = data.Item.longitude;
            Name = data.Item.name;
            Radius = data.Item.radius;
            Strategy = data.Item.strategy;
            IsValid = false;
            SetAddressFields();
        }

        private void SetAddressFields()
        {
            if ( FullFormattedAddress == "hidden" )
            {
                return;
            }

            var parser = new AddressParser();
            var parseResult = parser.ParseAddress( FullFormattedAddress.Replace( ", USA", "" ) );

            if ( parseResult == null )
            {
                return;
            }

            this.Street = parseResult.StreetLine;
            this.City = parseResult.City;
            this.State = parseResult.State;
            this.Zip = parseResult.Zip;

            if ( this.City.Contains( "\n" ) )
            {
                this.City = string.Empty;
            }

            IsValid =
                !string.IsNullOrWhiteSpace( this.Street )
                && !string.IsNullOrWhiteSpace( this.City )
                && !string.IsNullOrWhiteSpace( this.State )
                && !string.IsNullOrWhiteSpace( this.Zip );
        }
    }
}
