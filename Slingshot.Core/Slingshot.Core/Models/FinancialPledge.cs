using System;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for FinancialPledge
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

        //// <summary>
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
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "financial-pledge.csv";
        }
    }

    public enum PledgeFrequency
    {
        // One Time
        OneTime,

        // Every Week
        Weekly,

        // Every Two Weeks
        BiWeekly,

        // Twice a Month
        TwiceAMonth,

        // Once a Month
        Monthly,

        // Every Quarter
        Quarterly,

        // Every Six Months
        TwiceAYear,

        // Every Year
        Yearly
    }
}