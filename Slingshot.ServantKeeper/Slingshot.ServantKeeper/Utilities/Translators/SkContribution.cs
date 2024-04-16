using Slingshot.Core.Model;
using Slingshot.ServantKeeper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    class SKContribution
    {
        public static Dictionary<string, string> AmountTranslation = new Dictionary<string, string>()
        {
            { "0", "ˆ" },
            { "1", "‰" },
            { "2", "Š" },
            { "3", "‹" },
            { "4", "Œ" },
            { "5", "\u008d" },
            { "6", "Ž" },
            { "7", "\u008f" },
            { "8", "\u0090" },
            { "9", "‘" },
            { ".", "†" },
            { "-", "…" }
        };

        public static HashSet<int> UsedIds = new HashSet<int>();
        public static Dictionary<long, int> IdMap = new Dictionary<long, int>();

        public static FinancialTransaction Translate( Contribution contribution, List<ContributionDetail> details )
        {
            // The ID for a batch is a combination of the ID and the date
            long batchId = contribution.BatchId + contribution.BatchDate.Value.Ticks;

            FinancialTransaction financialTransaction = new FinancialTransaction();
            financialTransaction.Id = Math.Abs( unchecked(( int ) contribution.Id) );

            // Increment the id until we find one that is unqiue.
            while ( !UsedIds.Add( financialTransaction.Id ) )
            {
                financialTransaction.Id++;
            }
            IdMap.Add( contribution.Id, financialTransaction.Id );

            financialTransaction.BatchId = Math.Abs( unchecked(( int ) batchId) );
            financialTransaction.AuthorizedPersonId = Math.Abs( unchecked(( int ) contribution.IndividualId) );

            // TODO: Add all the payment types here
            switch ( contribution.PaymentType )
            {
                case "C":
                    financialTransaction.CurrencyType = CurrencyType.Check;
                    break;
                case "S":
                    financialTransaction.CurrencyType = CurrencyType.Cash;
                    break;
                case "O":
                default:
                    financialTransaction.CurrencyType = CurrencyType.Unknown;
                    break;
            }
            financialTransaction.TransactionDate = contribution.PostDate;
            financialTransaction.CreatedDateTime = contribution.CreatedDate;
            financialTransaction.ModifiedDateTime = contribution.UpdatedDate;
            financialTransaction.TransactionSource = TransactionSource.OnsiteCollection;
            financialTransaction.Summary = contribution.Note;
            financialTransaction.TransactionCode = contribution.CheckNumber;

            // If this is a split transaction or a special type of transaction, there will be details
            if ( details.Where( d => d.ContributionId == contribution.Id ).Any() )
            {
                foreach ( ContributionDetail detail in details.Where( d => d.ContributionId == contribution.Id ) )
                {
                    financialTransaction.FinancialTransactionDetails.Add( Translate( detail ) );
                }
            }
            else
            {
                ContributionDetail contributionDetail = new ContributionDetail();
                contributionDetail.Id = contribution.Id;
                contributionDetail.ContributionId = contribution.Id;
                contributionDetail.EncodedAmount = contribution.EncodedAmount;
                contributionDetail.Amount = contribution.Amount;
                contributionDetail.AccountId = contribution.AccountId;
                contributionDetail.CreatedDate = contribution.CreatedDate;
                contributionDetail.UpdatedDate = contribution.UpdatedDate;

                financialTransaction.FinancialTransactionDetails.Add( Translate( contributionDetail ) );
            }
            return financialTransaction;

        }


        public static HashSet<int> UsedDetailIds = new HashSet<int>();

        public static FinancialTransactionDetail Translate( ContributionDetail contributionDetail )
        {
            FinancialTransactionDetail detail = new FinancialTransactionDetail();
            detail.Id = Math.Abs( unchecked(( int ) contributionDetail.Id) );

            // Increment the id until we find one that is unqiue.
            while ( !UsedDetailIds.Add( detail.Id ) )
            {
                detail.Id++;
            }

            detail.TransactionId = IdMap[contributionDetail.ContributionId];
            detail.CreatedDateTime = contributionDetail.CreatedDate;
            detail.ModifiedDateTime = contributionDetail.UpdatedDate;
            detail.AccountId = Math.Abs( unchecked(( int ) contributionDetail.AccountId) );
            if ( contributionDetail.Amount == 0 )
            {
                string amount = contributionDetail.EncodedAmount;
                foreach ( var trans in AmountTranslation )
                {
                    amount = amount.Replace( trans.Value, trans.Key );
                }
                try
                {
                    detail.Amount = Decimal.Parse( amount );
                }
                catch ( Exception e )
                {
                    throw e;
                }
            }
            else
            {
                detail.Amount = contributionDetail.Amount;
            }

            return detail;

        }
    }
}
