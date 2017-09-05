using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for FamilyAttribute
    /// </summary>
    public class FamilyAttribute : IImportModel
    {

        /// <summary>
        /// Gets or sets the attribute key.
        /// </summary>
        /// <value>
        /// The attribute key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        /// <value>
        /// The name of the attribute.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the attribute field.
        /// </summary>
        /// <value>
        /// The type of the attribute field.
        /// </value>
        public string FieldType { get; set; }

        /// <summary>
        /// Gets or sets the attribute category.
        /// </summary>
        /// <value>
        /// The attribute category.
        /// </value>
        public string Category { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "family-attribute.csv";
        }
    }
}
