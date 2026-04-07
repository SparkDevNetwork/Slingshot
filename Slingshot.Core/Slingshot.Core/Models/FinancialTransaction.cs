using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// ImportModel for FinancialTransaction
    /// </summary>
    public class FinancialTransaction : IImportModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the batch identifier.
        /// </summary>
        /// <value>
        /// The batch identifier.
        /// </value>
        public int BatchId { get; set; }

        /// <summary>
        /// Gets or sets the authorized person identifier.
        /// </summary>
        /// <value>
        /// The authorized person identifier.
        /// </value>
        public int? AuthorizedPersonId { get; set; }

        /// <summary>
        /// Gets or sets the transaction date.
        /// </summary>
        /// <value>
        /// The transaction date.
        /// </value>
        public DateTime? TransactionDate { get; set; }

        /// <summary>
        /// Gets or sets the type of the transaction.
        /// </summary>
        /// <value>
        /// The type of the transaction.
        /// </value>
        public TransactionType TransactionType { get; set; }

        /// <summary>
        /// Gets or sets the transaction source.
        /// </summary>
        /// <value>
        /// The transaction source.
        /// </value>
        public TransactionSource TransactionSource { get; set; }

        /// <summary>
        /// Gets or sets the type of the currency.
        /// </summary>
        /// <value>
        /// The type of the currency.
        /// </value>
        public CurrencyType CurrencyType { get; set; }

        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        /// <value>
        /// The summary.
        /// </value>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the transaction code.
        /// </summary>
        /// <value>
        /// The transaction code.
        /// </value>
        public string TransactionCode { get; set; }

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
        /// Gets or sets the financial transaction details.
        /// </summary>
        /// <value>
        /// The financial transaction details.
        /// </value>
        public List<FinancialTransactionDetail> FinancialTransactionDetails { get; set; } = new List<FinancialTransactionDetail>();

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "financial-transaction.csv";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Transaction type of Contribution 
        /// </summary>
        Contribution,

        /// <summary>
        /// Transaction type of Event Registration
        /// </summary>
        EventRegistration,

        /// <summary>
        /// Transaction type of Receipt
        /// </summary>
        Receipt
    }

    /// <summary>
    /// 
    /// </summary>
    public enum TransactionSource
    {
        /// <summary>
        /// website
        /// </summary>
        Website,

        /// <summary>
        /// kiosk
        /// </summary>
        Kiosk,

        /// <summary>
        /// mobile application
        /// </summary>
        MobileApplication,

        /// <summary>
        /// onsite collection
        /// </summary>
        OnsiteCollection,

        /// <summary>
        /// bank checks
        /// </summary>
        BankChecks
    }

    /// <summary>
    /// 
    /// </summary>
    public enum CurrencyType
    {
        /// <summary>
        /// unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// check
        /// </summary>
        Check,

        /// <summary>
        /// cash
        /// </summary>
        Cash,

        /// <summary>
        /// credit card
        /// </summary>
        CreditCard,

        /// <summary>
        /// ach
        /// </summary>
        ACH,

        /// <summary>
        /// other
        /// </summary>
        Other,

        /// <summary>
        /// non cash
        /// </summary>
        NonCash
    }
}
