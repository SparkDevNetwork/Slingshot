using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for FinancialAccount
    /// </summary>
    public class FinancialAccount : IImportModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is tax deductible.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is tax deductible; otherwise, <c>false</c>.
        /// </value>
        public bool IsTaxDeductible { get; set; }

        /// <summary>
        /// Gets or sets the campus identifier.
        /// </summary>
        /// <value>
        /// The campus identifier.
        /// </value>
        public int? CampusId { get; set; }

        /// <summary>
        /// Gets or sets the parent account identifier.
        /// </summary>
        /// <value>
        /// The parent account identifier.
        /// </value>
        public int? ParentAccountId { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "financial-account.csv";
        }
    }
}
