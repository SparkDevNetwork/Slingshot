using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// ImportModel for Business Phone
    /// </summary>
    public class BusinessPhone : IImportModel
    {
        /// <summary>
        /// Gets or sets the business identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public int BusinessId { get; set; }

        /// <summary>
        /// Gets or sets the type of the phone.
        /// </summary>
        /// <value>
        /// The type of the phone.
        /// </value>
        public string PhoneType { get; set; }

        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        /// <value>
        /// The phone number.
        /// </value>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the is messaging enabled.
        /// </summary>
        /// <value>
        /// The is messaging enabled.
        /// </value>
        public bool? IsMessagingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the is unlisted.
        /// </summary>
        /// <value>
        /// The is unlisted.
        /// </value>
        public bool? IsUnlisted { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "business-phone.csv";
        }
    }
}
