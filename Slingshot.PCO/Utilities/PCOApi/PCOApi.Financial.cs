using Slingshot.Core.Utilities;
using Slingshot.PCO.Models.DTO;
using Slingshot.PCO.Utilities.Translators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// PCO API Class - Financial data export methods.
    /// </summary>
    public static partial class PCOApi
    {
        /// <summary>
        /// Api Endpoint Declarations.
        /// </summary>
        internal static partial class ApiEndpoint
        {
            internal const string API_FUNDS = "/giving/v2/funds";
            internal const string API_BATCHES = "/giving/v2/batches";
            internal const string API_DONATIONS = "/giving/v2/donations";
        }

        #region ExportFinancialAccounts() and Related Methods

        /// <summary>
        /// Exports the accounts.
        /// </summary>
        public static void ExportFinancialAccounts()
        {
            try
            {
                var funds = GetFunds();

                foreach ( var fund in funds )
                {
                    var importFund = PCOImportFund.Translate( fund );
                    if ( importFund != null )
                    {
                        ImportPackage.WriteToPackage( importFund );
                    }
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Get the Funds from PCO.
        /// </summary>
        /// <returns></returns>
        private static List<FundDTO> GetFunds()
        {
            var funds = new List<FundDTO>();

            var fundQuery = GetAPIQuery( ApiEndpoint.API_FUNDS );

            if ( fundQuery == null )
            {
                return funds;
            }

            foreach ( var item in fundQuery.Items )
            {
                var fund = new FundDTO( item );
                if ( fund != null )
                {
                    funds.Add( fund );
                }
            }

            return funds;
        }

        #endregion ExportFinancialAccounts() and Related Methods

        #region ExportFinancialBatches() and Related Methods

        /// <summary>
        /// Exports the batches.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public static void ExportFinancialBatches( DateTime modifiedSince )
        {
            try
            {
                var batches = GetBatches( modifiedSince );

                foreach ( var batch in batches )
                {
                    var importBatch = PCOImportBatch.Translate( batch );

                    if ( importBatch != null )
                    {
                        ImportPackage.WriteToPackage( importBatch );
                    }
                }

            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Get the Batches from PCO.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        public static List<BatchDTO> GetBatches( DateTime? modifiedSince )
        {
            var batches = new List<BatchDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "include", "owner" },
                { "per_page", "100" }
            };

            var batchesQuery = GetAPIQuery( ApiEndpoint.API_BATCHES, apiOptions, modifiedSince );

            if ( batchesQuery == null )
            {
                return batches;
            }

            foreach ( var item in batchesQuery.Items )
            {
                var batch = new BatchDTO( item );
                if ( batch != null )
                {
                    batches.Add( batch );
                }
            }

            return batches;
        }

        #endregion ExportFinancialBatches() and Related Methods

        #region ExportContributions() and Related Methods

        /// <summary>
        /// Exports the contributions.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        public static void ExportContributions( DateTime modifiedSince )
        {
            try
            {
                var donations = GetDonations( modifiedSince )
                    .Where( d => d.Refunded == false && d.PaymentStatus == "succeeded" )
                    .ToList();

                foreach ( var donation in donations )
                {
                    var importTransaction = PCOImportDonation.Translate( donation );
                    var importTransactionDetails = PCOImportDesignation.Translate( donation );

                    if ( importTransaction != null )
                    {
                        ImportPackage.WriteToPackage( importTransaction );

                        foreach( var detail in importTransactionDetails )
                        {
                            if ( detail != null )
                            {
                                ImportPackage.WriteToPackage( detail );
                            }
                        }
                    }                                
                }            
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }
        }

        /// <summary>
        /// Gets Donations from PCO.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        private static List<DonationDTO> GetDonations( DateTime? modifiedSince )
        {
            var donations = new List<DonationDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "include", "designations" },
                { "per_page", "100" }
            };

            var donationsQuery = GetAPIQuery( ApiEndpoint.API_DONATIONS, apiOptions, modifiedSince );

            if ( donationsQuery == null )
            {
                return donations;
            }

            foreach ( var item in donationsQuery.Items )
            {
                var donation = new DonationDTO( item, donationsQuery.IncludedItems );
                if ( donation != null )
                {
                    donations.Add( donation );
                }
            }

            return donations;
        }

        #endregion ExportContributions() and Related Methods
    }
}
