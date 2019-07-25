using System;
using Slingshot.Core.Data;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// ImportModel for Person Search Key.
    /// Search Keys can be used to help find a person record
    /// </summary>
    public class PersonSearchKey: IImportModel
    {
        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public int PersonId { get; set; }

        /// <summary>
        /// Gets or sets the name of the search type. Default is 'Alternate Id'
        /// </summary>
        /// <value>
        /// The name of the search type.
        /// </value>
        [Obsolete("Open Question if we want to support this")]
        public string SearchTypeName { get; set; }

        /// <summary>
        /// Gets or sets the search value (max length of 255)
        /// </summary>
        /// <value>
        /// The search value.
        /// </value>
        public string SearchValue { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "person-search-key.csv";
        }
    }
}
