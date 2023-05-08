using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// ImportModel for Business Contact
    /// </summary>
    public class BusinessContact : IImportModel
    {
        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public int PersonId { get; set; }

        /// <summary>
        /// Gets or sets the business identifier.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public int BusinessId { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "business-contact.csv";
        }
    }
}
