using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// ImportModel for Location
    /// </summary>
    public class Location : IImportModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the parent account identifier.
        /// </summary>
        /// <value>
        /// The parent account identifier.
        /// </value>
        public int? ParentLocationId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        /// <value>
        /// The type of the location.
        /// </value>
        public LocationType LocationType { get; set; }

        /// <summary>
        /// Gets or sets the street1.
        /// </summary>
        /// <value>
        /// The street1.
        /// </value>
        public string Street1 { get; set; }

        /// <summary>
        /// Gets or sets the street2.
        /// </summary>
        /// <value>
        /// The street2.
        /// </value>
        public string Street2 { get; set; }

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        /// <value>
        /// The city.
        /// </value>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        /// <value>
        /// The country.
        /// </value>
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets the postal code.
        /// </summary>
        /// <value>
        /// The postal code.
        /// </value>
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the county.
        /// </summary>
        /// <value>
        /// The county.
        /// </value>
        public string County { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "locations.csv";
        }
    }

    /// <summary>
    /// Location Types
    /// </summary>
    public enum LocationType
    {
        /// <summary>
        /// home
        /// </summary>
        Home = 0,

        /// <summary>
        /// work
        /// </summary>
        Work = 1,

        /// <summary>
        /// previous
        /// </summary>
        Previous = 2,

        /// <summary>
        /// meeting location
        /// </summary>
        MeetingLocation = 3,

        /// <summary>
        /// geographic area
        /// </summary>
        GeographicArea = 4
    }
}
