using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1FinancialTransaction
    {
        public static FinancialTransaction Translate( DataRow row, DataRow[] headOfHouseHolds )
        {
            var transaction = new FinancialTransaction();

            if( row.Field<int?>( "Individual_ID" ).HasValue )
            {
                transaction.AuthorizedPersonId = row.Field<int?>( "Individual_ID" ).Value;
            }
            else
            {
                var headOfHousehold = headOfHouseHolds.Where( x => x.Field<int?>( "household_id" ) == row.Field<int?>( "household_id" ) ).FirstOrDefault();

                if ( headOfHousehold != null )
                {
                    transaction.AuthorizedPersonId = headOfHousehold.Field<int>( "individual_id" );
                }
                else
                {
                    //If there is no head of household, and no indivual tied to the transaction, it should be assumed to be a business transaction.
                    transaction.AuthorizedPersonId = row.Field<int>( "household_id" ) + 900000000;
                }
            }

            if( row.Field<int?>( "BatchID" ).HasValue )
            {
                transaction.BatchId = row.Field<int?>( "BatchID" ).Value;
            }
            else
            {
                transaction.BatchId = 90000000 +  int.Parse( row.Field<DateTime?>( "Received_Date" ).Value.ToString( "yyyyMMdd" ) );
            }

            transaction.TransactionDate = row.Field<DateTime?>( "Received_Date" );
            transaction.TransactionCode = row.Field<string>( "Check_Number" );
            transaction.Summary = row.Field<string>( "Memo" );
            transaction.Id = row.Field<int>( "ContributionID" );

            switch ( row.Field<string>( "Contribution_Type_Name" ) )
            {
                case "Cash":
                    transaction.CurrencyType = CurrencyType.Cash;
                    break;
                case "Check":
                    transaction.CurrencyType = CurrencyType.Check;
                    break;
                case "Credit Card":
                    transaction.CurrencyType = CurrencyType.CreditCard;
                    break;
                case "ACH":
                    transaction.CurrencyType = CurrencyType.ACH;
                    break;
                case "Non-Cash":
                    transaction.CurrencyType = CurrencyType.NonCash;
                    break;
                default:
                    transaction.CurrencyType = CurrencyType.Unknown;
                    break;
            }

            var accountId = 0;
            MD5 md5Hasher = MD5.Create();
            byte[] hashed;
            //Set Account Id
            if ( string.IsNullOrWhiteSpace( row.Field<string>( "sub_fund_name" ) ) )
            {
                //Use Hash to create Account ID
                hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( row.Field<string>( "fund_name" ) ) );
               
            }
            else
            {
                hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( row.Field<string>( "fund_name" ) + row.Field<string>( "sub_fund_name" ) ) );
            }

            accountId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number

            transaction.FinancialTransactionDetails.Add( new FinancialTransactionDetail
            {
                Id = transaction.Id,
                TransactionId = transaction.Id,
                Amount = row.Field<decimal>( "Amount" ),
                AccountId = accountId
            } );

            return transaction;
        }
    }
}
