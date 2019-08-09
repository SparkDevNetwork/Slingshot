using System;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// ImportModel for Financial Pledge
    /// </summary>
    public class FinancialPledge : IImportModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public int PersonId { get; set; }

        /// <summary>
        /// Gets or sets the account identifier.
        /// </summary>
        /// <value>
        /// The account identifier.
        /// </value>
        public int? AccountId { get; set; }

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
        /// Gets or sets the pledge frequency.
        /// </summary>
        /// <value>
        /// The pledge frequency.
        /// </value>
        public PledgeFrequency? PledgeFrequency { get; set; }

        /// <summary>
        /// Gets or sets the pledge amount that is promised to be given at the specified <see cref="PledgeFrequency"/>.
        /// </summary>
        /// <value>
        /// A <see cref="System.Decimal"/> representing the amount to be pledged at the specified frequency.
        /// </value>
        /// <remarks>
        /// An example is that a person pledges $100.00 to be given monthly for the next year. This value will be $100.00 and the grand total of the pledge would be $1,200.00
        /// </remarks>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        /// <value>
        /// The created date time.
        /// </value>
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the modified date time.
        /// </summary>
        /// <value>
        /// The modified date time.
        /// </value>
        public DateTime? ModifiedDateTime { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "financial-pledge.csv";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PledgeFrequency
    {
        /// <summary>
        /// One time
        /// </summary>
        OneTime,

        /// <summary>
        /// Every Week
        /// </summary>
        Weekly,

        /// <summary>
        /// Every Two Weeks
        /// </summary>
        BiWeekly,

        /// <summary>
        /// Twice a Month
        /// </summary>
        TwiceAMonth,

        /// <summary>
        /// Once a Month
        /// </summary>
        Monthly,

        /// <summary>
        /// Every Quarter
        /// </summary>
        Quarterly,

        /// <summary>
        /// Every Six Months
        /// </summary>
        TwiceAYear,

        /// <summary>
        /// Every Year
        /// </summary>
        Yearly
    }
}