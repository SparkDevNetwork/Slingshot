using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using OrcaMDF.Core.MetaData;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace Slingshot.F1.Utilities.Translators
{
    public static class F1Financial
    {
        /// <summary>
        /// Translates the account data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateBankAccount( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var importedBankAccounts = new FinancialPersonBankAccountService( lookupContext ).Queryable().AsNoTracking().ToList();
            var newBankAccounts = new List<FinancialPersonBankAccount>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying check number import ({totalRows:N0} found, {importedBankAccounts.Count:N0} already exist)." );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var individualId = row["Individual_ID"] as int?;
                var householdId = row["Household_ID"] as int?;
                var personKeys = GetPersonKeys( individualId, householdId );
                if ( personKeys != null && personKeys.PersonAliasId > 0 )
                {
                    var routingNumber = row["Routing_Number"] as int?;
                    var accountNumber = row["Account"] as string;
                    if ( routingNumber.HasValue && !string.IsNullOrWhiteSpace( accountNumber ) )
                    {
                        accountNumber = accountNumber.Replace( " ", string.Empty );
                        var encodedNumber = FinancialPersonBankAccount.EncodeAccountNumber( routingNumber.ToString(), accountNumber );
                        if ( !importedBankAccounts.Any( a => a.PersonAliasId == personKeys.PersonAliasId && a.AccountNumberSecured == encodedNumber ) )
                        {
                            var bankAccount = new FinancialPersonBankAccount
                            {
                                CreatedByPersonAliasId = ImportPersonAliasId,
                                AccountNumberSecured = encodedNumber,
                                AccountNumberMasked = accountNumber.ToString().Masked(),
                                PersonAliasId = (int)personKeys.PersonAliasId
                            };

                            newBankAccounts.Add( bankAccount );
                            completedItems++;
                            if ( completedItems % percentage < 1 )
                            {
                                var percentComplete = completedItems / percentage;
                                ReportProgress( percentComplete, $"{completedItems:N0} numbers imported ({percentComplete}% complete)." );
                            }
                            else if ( completedItems % ReportingNumber < 1 )
                            {
                                SaveBankAccounts( newBankAccounts );
                                newBankAccounts.Clear();
                                ReportPartialProgress();
                            }
                        }
                    }
                }
            }

            if ( newBankAccounts.Any() )
            {
                SaveBankAccounts( newBankAccounts );
            }

            ReportProgress( 100, $"Finished check number import: {completedItems:N0} numbers imported." );
        }

        /// <summary>
        /// Saves the bank accounts.
        /// </summary>
        /// <param name="newBankAccounts">The new bank accounts.</param>
        private static void SaveBankAccounts( List<FinancialPersonBankAccount> newBankAccounts )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.BulkInsert( newBankAccounts );
            }
        }

        /// <summary>
        /// Translates the batch data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void TranslateBatch( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var newBatches = new List<FinancialBatch>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var earliestBatchDate = ImportDateTime;
            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying batch import ({totalRows:N0} found, {ImportedBatches.Count:N0} already exist)." );
            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var batchId = row["BatchID"] as int?;
                if ( batchId.HasValue && !ImportedBatches.ContainsKey( (int)batchId ) )
                {
                    var batch = new FinancialBatch
                    {
                        CreatedByPersonAliasId = ImportPersonAliasId,
                        ForeignKey = batchId.ToString(),
                        ForeignId = batchId,
                        Note = string.Empty,
                        Status = BatchStatus.Closed,
                        AccountingSystemCode = string.Empty
                    };

                    var name = row["BatchName"] as string;
                    if ( !string.IsNullOrWhiteSpace( name ) )
                    {
                        name = name.Trim();
                        batch.Name = name.Truncate( 50 );
                        batch.CampusId = GetCampusId( name );
                    }

                    var batchDate = row["BatchDate"] as DateTime?;
                    if ( batchDate.HasValue )
                    {
                        batch.BatchStartDateTime = batchDate;
                        batch.BatchEndDateTime = batchDate;

                        if ( batchDate < earliestBatchDate )
                        {
                            earliestBatchDate = (DateTime)batchDate;
                        }
                    }

                    var amount = row["BatchAmount"] as decimal?;
                    if ( amount.HasValue )
                    {
                        batch.ControlAmount = amount.Value;
                    }

                    newBatches.Add( batch );
                    completedItems++;
                    if ( completedItems % percentage < 1 )
                    {
                        var percentComplete = completedItems / percentage;
                        ReportProgress( percentComplete, $"{completedItems:N0} batches imported ({percentComplete}% complete)." );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SaveFinancialBatches( newBatches );
                        newBatches.ForEach( b => ImportedBatches.Add( (int)b.ForeignId, (int?)b.Id ) );
                        newBatches.Clear();
                        ReportPartialProgress();
                    }
                }
            }

            // add a default batch to use with contributions
            if ( !ImportedBatches.ContainsKey( 0 ) )
            {
                var defaultBatch = new FinancialBatch
                {
                    CreatedDateTime = ImportDateTime,
                    CreatedByPersonAliasId = ImportPersonAliasId,
                    BatchStartDateTime = earliestBatchDate,
                    Status = BatchStatus.Closed,
                    Name = $"Default Batch (Imported {ImportDateTime})",
                    ControlAmount = 0.0m,
                    ForeignKey = "0",
                    ForeignId = 0
                };

                newBatches.Add( defaultBatch );
            }

            if ( newBatches.Any() )
            {
                SaveFinancialBatches( newBatches );
                newBatches.ForEach( b => ImportedBatches.Add( (int)b.ForeignId, (int?)b.Id ) );
            }

            ReportProgress( 100, $"Finished batch import: {completedItems:N0} batches imported." );
        }

        /// <summary>
        /// Saves the financial batches.
        /// </summary>
        /// <param name="newBatches">The new batches.</param>
        private static void SaveFinancialBatches( List<FinancialBatch> newBatches )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.BulkInsert( newBatches );
            }
        }

        /// <summary>
        /// Translates the contribution.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateContribution( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();

            var transactionTypeContributionId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid(), lookupContext ).Id;

            var currencyTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE ) );
            var currencyTypeACH = currencyTypes.DefinedValues.FirstOrDefault( dv => dv.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH ) ) ).Id;
            var currencyTypeCash = currencyTypes.DefinedValues.FirstOrDefault( dv => dv.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CASH ) ) ).Id;
            var currencyTypeCheck = currencyTypes.DefinedValues.FirstOrDefault( dv => dv.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CHECK ) ) ).Id;
            var currencyTypeCreditCard = currencyTypes.DefinedValues.FirstOrDefault( dv => dv.Guid.Equals( new Guid( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD ) ) ).Id;
            var currencyTypeNonCash = currencyTypes.DefinedValues.Where( dv => dv.Value.Equals( "Non-Cash" ) ).Select( dv => (int?)dv.Id ).FirstOrDefault();
            if ( currencyTypeNonCash == null )
            {
                var newTenderNonCash = new DefinedValue
                {
                    Value = "Non-Cash",
                    Description = "Non-Cash",
                    DefinedTypeId = currencyTypes.Id
                };

                lookupContext.DefinedValues.Add( newTenderNonCash );
                lookupContext.SaveChanges();

                currencyTypeNonCash = newTenderNonCash.Id;
            }

            var creditCardTypes = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.FINANCIAL_CREDIT_CARD_TYPE ) ).DefinedValues;

            var refundReasons = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_REFUND_REASON ), lookupContext ).DefinedValues;

            var accountList = new FinancialAccountService( lookupContext ).Queryable().AsNoTracking().ToList();

            int? defaultBatchId = null;
            if ( ImportedBatches.ContainsKey( 0 ) )
            {
                defaultBatchId = ImportedBatches[0];
            }

            // Get all imported contributions
            var importedContributions = new FinancialTransactionService( lookupContext ).Queryable().AsNoTracking()
               .Where( c => c.ForeignId != null )
               .ToDictionary( t => (int)t.ForeignId, t => (int?)t.Id );

            // List for batching new contributions
            var newTransactions = new List<FinancialTransaction>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying contribution import ({totalRows:N0} found, {importedContributions.Count:N0} already exist)." );
            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var individualId = row["Individual_ID"] as int?;
                var householdId = row["Household_ID"] as int?;
                var contributionId = row["ContributionID"] as int?;

                if ( contributionId.HasValue && !importedContributions.ContainsKey( (int)contributionId ) )
                {
                    var transaction = new FinancialTransaction
                    {
                        CreatedByPersonAliasId = ImportPersonAliasId,
                        ModifiedByPersonAliasId = ImportPersonAliasId,
                        TransactionTypeValueId = transactionTypeContributionId,
                        ForeignKey = contributionId.ToString(),
                        ForeignId = contributionId
                    };

                    int? giverAliasId = null;
                    var personKeys = GetPersonKeys( individualId, householdId );
                    if ( personKeys != null && personKeys.PersonAliasId > 0 )
                    {
                        giverAliasId = personKeys.PersonAliasId;
                        transaction.CreatedByPersonAliasId = giverAliasId;
                        transaction.AuthorizedPersonAliasId = giverAliasId;
                        transaction.ProcessedByPersonAliasId = giverAliasId;
                    }

                    var summary = row["Memo"] as string;
                    if ( !string.IsNullOrWhiteSpace( summary ) )
                    {
                        transaction.Summary = summary;
                    }

                    var batchId = row["BatchID"] as int?;
                    if ( batchId.HasValue && ImportedBatches.Any( b => b.Key.Equals( batchId ) ) )
                    {
                        transaction.BatchId = ImportedBatches.FirstOrDefault( b => b.Key.Equals( batchId ) ).Value;
                    }
                    else
                    {
                        // use the default batch for any non-matching transactions
                        transaction.BatchId = defaultBatchId;
                    }

                    var receivedDate = row["Received_Date"] as DateTime?;
                    if ( receivedDate.HasValue )
                    {
                        transaction.TransactionDateTime = receivedDate;
                        transaction.CreatedDateTime = receivedDate;
                        transaction.ModifiedDateTime = ImportDateTime;
                    }

                    var contributionFields = row.Columns.Select( c => c.Name ).ToList();
                    var cardType = contributionFields.Contains( "Card_Type" ) ? row["Card_Type"] as string : string.Empty;
                    var cardLastFour = contributionFields.Contains( "Last_Four" ) ? row["Last_Four"] as string : string.Empty;
                    var contributionType = contributionFields.Contains( "Contribution_Type_Name" ) ? row["Contribution_Type_Name"] as string : string.Empty;

                    if ( !string.IsNullOrWhiteSpace( contributionType ) )
                    {
                        // set default source to onsite, exceptions listed below
                        transaction.SourceTypeValueId = TransactionSourceTypeOnsiteId;

                        int? paymentCurrencyTypeId = null, creditCardTypeId = null;
                        switch ( contributionType.ToLower() )
                        {
                            case "cash":
                                paymentCurrencyTypeId = currencyTypeCash;
                                break;

                            case "check":
                                paymentCurrencyTypeId = currencyTypeCheck;
                                break;

                            case "ach":
                                paymentCurrencyTypeId = currencyTypeACH;
                                transaction.SourceTypeValueId = TransactionSourceTypeWebsiteId;
                                break;

                            case "credit card":
                                paymentCurrencyTypeId = currencyTypeCreditCard;
                                transaction.SourceTypeValueId = TransactionSourceTypeWebsiteId;

                                if ( !string.IsNullOrWhiteSpace( cardType ) )
                                {
                                    creditCardTypeId = creditCardTypes.Where( t => t.Value.Equals( cardType, StringComparison.CurrentCultureIgnoreCase ) )
                                        .Select( t => (int?)t.Id ).FirstOrDefault();
                                }
                                break;

                            default:
                                paymentCurrencyTypeId = currencyTypeNonCash;
                                break;
                        }

                        var paymentDetail = new FinancialPaymentDetail
                        {
                            CreatedDateTime = receivedDate,
                            CreatedByPersonAliasId = giverAliasId,
                            ModifiedDateTime = ImportDateTime,
                            ModifiedByPersonAliasId = giverAliasId,
                            CurrencyTypeValueId = paymentCurrencyTypeId,
                            CreditCardTypeValueId = creditCardTypeId,
                            AccountNumberMasked = cardLastFour,
                            ForeignKey = contributionId.ToString(),
                            ForeignId = contributionId
                        };

                        transaction.FinancialPaymentDetail = paymentDetail;
                    }

                    var checkNumber = row["Check_Number"] as string;
                    // if the check number is valid, put it in the transaction code
                    if ( checkNumber.AsType<int?>().HasValue )
                    {
                        transaction.TransactionCode = checkNumber;
                    }
                    // check for SecureGive kiosk transactions
                    else if ( !string.IsNullOrWhiteSpace( checkNumber ) && checkNumber.StartsWith( "SG" ) )
                    {
                        transaction.SourceTypeValueId = TransactionSourceTypeKioskId;
                    }

                    var fundName = contributionFields.Contains( "Fund_Name" ) ? row["Fund_Name"] as string : string.Empty;
                    var subFund = contributionFields.Contains( "Sub_Fund_Name" ) ? row["Sub_Fund_Name"] as string : string.Empty;
                    var fundGLAccount = contributionFields.Contains( "Fund_GL_Account" ) ? row["Fund_GL_Account"] as string : string.Empty;
                    var subFundGLAccount = contributionFields.Contains( "Sub_Fund_GL_Account" ) ? row["Sub_Fund_GL_Account"] as string : string.Empty;
                    var isFundActive = contributionFields.Contains( "Fund_Is_active" ) ? row["Fund_Is_active"] as string : null;
                    var statedValue = row["Stated_Value"] as decimal?;
                    var amount = row["Amount"] as decimal?;
                    if ( !string.IsNullOrWhiteSpace( fundName ) && amount.HasValue )
                    {
                        int transactionAccountId;
                        var parentAccount = accountList.FirstOrDefault( a => !a.CampusId.HasValue && a.Name.Equals( fundName.Truncate( 50 ), StringComparison.CurrentCultureIgnoreCase ) );
                        if ( parentAccount == null )
                        {
                            parentAccount = AddFinancialAccount( lookupContext, fundName, $"{fundName} imported {ImportDateTime}", fundGLAccount, null, null, isFundActive.AsBooleanOrNull(), receivedDate, fundName.RemoveSpecialCharacters() );
                            accountList.Add( parentAccount );
                        }

                        if ( !string.IsNullOrWhiteSpace( subFund ) )
                        {
                            int? campusFundId = null;
                            // assign a campus if the subfund is a campus fund
                            var campusFund = CampusList.FirstOrDefault( c => subFund.StartsWith( c.Name, StringComparison.CurrentCultureIgnoreCase ) || subFund.StartsWith( c.ShortCode, StringComparison.CurrentCultureIgnoreCase ) );
                            if ( campusFund != null )
                            {
                                // use full campus name as the subfund
                                subFund = campusFund.Name;
                                campusFundId = campusFund.Id;
                            }

                            // add info to easily find/assign this fund in the view
                            subFund = $"{subFund} {fundName}";

                            var childAccount = accountList.FirstOrDefault( c => c.ParentAccountId == parentAccount.Id && c.Name.Equals( subFund.Truncate( 50 ), StringComparison.CurrentCultureIgnoreCase ) );
                            if ( childAccount == null )
                            {
                                // create a child account with a campusId if it was set
                                childAccount = AddFinancialAccount( lookupContext, subFund, $"{subFund} imported {ImportDateTime}", subFundGLAccount, campusFundId, parentAccount.Id, isFundActive.AsBooleanOrNull(), receivedDate, subFund.RemoveSpecialCharacters() );
                                accountList.Add( childAccount );
                            }

                            transactionAccountId = childAccount.Id;
                        }
                        else
                        {
                            transactionAccountId = parentAccount.Id;
                        }

                        if ( amount == 0 && statedValue.HasValue && statedValue != 0 )
                        {
                            amount = statedValue;
                        }

                        var transactionDetail = new FinancialTransactionDetail
                        {
                            Amount = (decimal)amount,
                            CreatedDateTime = receivedDate,
                            AccountId = transactionAccountId
                        };

                        transaction.TransactionDetails.Add( transactionDetail );

                        if ( amount < 0 )
                        {
                            transaction.RefundDetails = new FinancialTransactionRefund();
                            transaction.RefundDetails.CreatedDateTime = receivedDate;
                            transaction.RefundDetails.RefundReasonValueId = refundReasons.Where( dv => summary != null && dv.Value.Contains( summary ) )
                                .Select( dv => (int?)dv.Id ).FirstOrDefault();
                            transaction.RefundDetails.RefundReasonSummary = summary;
                        }
                    }

                    newTransactions.Add( transaction );
                    completedItems++;
                    if ( completedItems % percentage < 1 )
                    {
                        var percentComplete = completedItems / percentage;
                        ReportProgress( percentComplete, $"{completedItems:N0} contributions imported ({percentComplete}% complete)." );
                    }
                    else if ( completedItems % ReportingNumber < 1 )
                    {
                        SaveContributions( newTransactions );
                        newTransactions.Clear();
                        ReportPartialProgress();
                    }
                }
            }

            if ( newTransactions.Any() )
            {
                SaveContributions( newTransactions );
            }

            ReportProgress( 100, $"Finished contribution import: {completedItems:N0} contributions imported." );
        }

        /// <summary>
        /// Saves the contributions.
        /// </summary>
        /// <param name="newTransactions">The new transactions.</param>
        private static void SaveContributions( List<FinancialTransaction> newTransactions )
        {
            using ( var rockContext = new RockContext() )
            {
                // can't use bulk insert, transaction can contain PaymentDetail, TransactionDetail, Refund
                rockContext.Configuration.AutoDetectChangesEnabled = false;
                rockContext.FinancialTransactions.AddRange( newTransactions );
                rockContext.SaveChanges( DisableAuditing );
            }
        }

        /// <summary>
        /// Translates the pledge.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void TranslatePledge( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var accountList = new FinancialAccountService( lookupContext ).Queryable().AsNoTracking().ToList();
            var importedPledges = new FinancialPledgeService( lookupContext ).Queryable().AsNoTracking().ToList();

            var pledgeFrequencies = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.FINANCIAL_FREQUENCY ), lookupContext ).DefinedValues;
            var oneTimePledgeFrequencyId = pledgeFrequencies.FirstOrDefault( f => f.Guid == new Guid( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME ) ).Id;

            var newPledges = new List<FinancialPledge>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completedItems = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying pledge import ({totalRows:N0} found)." );

            foreach ( var row in tableData.Where( r => r != null ) )
            {
                var amount = row["Total_Pledge"] as decimal?;
                var startDate = row["Start_Date"] as DateTime?;
                var endDate = row["End_Date"] as DateTime?;
                if ( amount.HasValue && startDate.HasValue && endDate.HasValue )
                {
                    var individualId = row["Individual_ID"] as int?;
                    var householdId = row["Household_ID"] as int?;

                    var personKeys = GetPersonKeys( individualId, householdId, includeVisitors: false );
                    if ( personKeys != null && personKeys.PersonAliasId > 0 )
                    {
                        var pledge = new FinancialPledge
                        {
                            PersonAliasId = personKeys.PersonAliasId,
                            CreatedByPersonAliasId = ImportPersonAliasId,
                            ModifiedDateTime = ImportDateTime,
                            StartDate = (DateTime)startDate,
                            EndDate = (DateTime)endDate,
                            TotalAmount = (decimal)amount
                        };

                        var frequency = row["Pledge_Frequency_Name"].ToString();
                        if ( !string.IsNullOrWhiteSpace( frequency ) )
                        {
                            if ( frequency.Equals( "one time", StringComparison.CurrentCultureIgnoreCase ) || frequency.Equals( "as can", StringComparison.CurrentCultureIgnoreCase ) )
                            {
                                pledge.PledgeFrequencyValueId = oneTimePledgeFrequencyId;
                            }
                            else
                            {
                                pledge.PledgeFrequencyValueId = pledgeFrequencies
                                    .Where( f => f.Value.StartsWith( frequency, StringComparison.CurrentCultureIgnoreCase ) || f.Description.StartsWith( frequency, StringComparison.CurrentCultureIgnoreCase ) )
                                    .Select( f => f.Id ).FirstOrDefault();
                            }
                        }

                        var fundName = row["Fund_Name"] as string;
                        var subFund = row["Sub_Fund_Name"] as string;
                        if ( !string.IsNullOrWhiteSpace( fundName ) )
                        {
                            var parentAccount = accountList.FirstOrDefault( a => !a.CampusId.HasValue && a.Name.Equals( fundName.Truncate( 50 ), StringComparison.CurrentCultureIgnoreCase ) );
                            if ( parentAccount == null )
                            {
                                parentAccount = AddFinancialAccount( lookupContext, fundName, $"{fundName} imported {ImportDateTime}", string.Empty, null, null, null, startDate, fundName.RemoveSpecialCharacters() );
                                accountList.Add( parentAccount );
                            }

                            if ( !string.IsNullOrWhiteSpace( subFund ) )
                            {
                                int? campusFundId = null;
                                // assign a campus if the subfund is a campus fund
                                var campusFund = CampusList.FirstOrDefault( c => subFund.StartsWith( c.Name ) || subFund.StartsWith( c.ShortCode ) );
                                if ( campusFund != null )
                                {
                                    // use full campus name as the subfund
                                    subFund = campusFund.Name;
                                    campusFundId = campusFund.Id;
                                }

                                // add info to easily find/assign this fund in the view
                                subFund = $"{subFund} {fundName}";

                                var childAccount = accountList.FirstOrDefault( c => c.ParentAccountId == parentAccount.Id && c.Name.Equals( subFund.Truncate( 50 ), StringComparison.CurrentCultureIgnoreCase ) );
                                if ( childAccount == null )
                                {
                                    // create a child account with a campusId if it was set
                                    childAccount = AddFinancialAccount( lookupContext, subFund, $"{subFund} imported {ImportDateTime}", string.Empty, campusFundId, parentAccount.Id, null, startDate, subFund.RemoveSpecialCharacters() );
                                    accountList.Add( childAccount );
                                }

                                pledge.AccountId = childAccount.Id;
                            }
                            else
                            {
                                pledge.AccountId = parentAccount.Id;
                            }
                        }

                        newPledges.Add( pledge );
                        completedItems++;
                        if ( completedItems % percentage < 1 )
                        {
                            var percentComplete = completedItems / percentage;
                            ReportProgress( percentComplete, $"{completedItems:N0} pledges imported ({percentComplete}% complete)." );
                        }
                        else if ( completedItems % ReportingNumber < 1 )
                        {
                            SavePledges( newPledges );
                            ReportPartialProgress();
                            newPledges.Clear();
                        }
                    }
                }
            }

            if ( newPledges.Any() )
            {
                SavePledges( newPledges );
            }

            ReportProgress( 100, $"Finished pledge import: {completedItems:N0} pledges imported." );
        }

        /// <summary>
        /// Saves the pledges.
        /// </summary>
        /// <param name="newPledges">The new pledges.</param>
        private static void SavePledges( List<FinancialPledge> newPledges )
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.BulkInsert( newPledges );
            }
        }
    }
}