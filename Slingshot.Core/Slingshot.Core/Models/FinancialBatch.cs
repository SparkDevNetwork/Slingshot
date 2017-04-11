using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for FinancialBatch
    /// </summary>
    public class FinancialBatch : IImportModel
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
        /// Gets or sets the campus identifier.
        /// </summary>
        /// <value>
        /// The campus identifier.
        /// </value>
        public int? CampusId { get; set; }

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        /// <value>
        /// The start date.
        /// </value>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>
        /// The end date.
        /// </value>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public BatchStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the created by person identifier.
        /// </summary>
        /// <value>
        /// The created by person identifier.
        /// </value>
        public int? CreatedByPersonId { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        /// <value>
        /// The created date time.
        /// </value>
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the modified by person identifier.
        /// </summary>
        /// <value>
        /// The modified by person identifier.
        /// </value>
        public int? ModifiedByPersonId { get; set; }

        /// <summary>
        /// Gets or sets the modified date time.
        /// </summary>
        /// <value>
        /// The modified date time.
        /// </value>
        public DateTime? ModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the control amount.
        /// </summary>
        /// <value>
        /// The control amount.
        /// </value>
        public decimal ControlAmount {
            get
            {
                return FinancialTransactions.SelectMany( t => t.FinancialTransactionDetails ).Sum( d => d.Amount );
            }
        }

        /// <summary>
        /// Gets or sets the financial transactions.
        /// </summary>
        /// <value>
        /// The financial transactions.
        /// </value>
        public List<FinancialTransaction> FinancialTransactions { get; set; } = new List<FinancialTransaction>();

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "financial-batch.csv";
        }
    }

    public enum BatchStatus
    {
        /// <summary>
        /// Pending
        /// In the process of scanning the checks to it
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Open
        /// Transactions are all entered and are ready to be matched
        /// </summary>
        Open = 1,

        /// <summary>
        /// Closed
        /// All is well and good
        /// </summary>
        Closed = 2
    }
}
