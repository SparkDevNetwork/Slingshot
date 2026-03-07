using System.IO.Compression;
using System.Text;
using LibElvanto;
using LibElvanto.Contracts;
using LibElvanto.Financial;
using LibElvanto.Utilities;
using Microsoft.Maui.Storage;

namespace Slingshot.Elvanto;

public partial class MainPage : ContentPage
{
    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void tbApiKey_TextChanged( object sender, TextChangedEventArgs e )
    {
        btnImport.IsEnabled = !string.IsNullOrEmpty( tbApiKey.Text );
    }

    private async void btnImport_Clicked( object sender, EventArgs e )
    {

        await Task.Run( async () =>
        {
            try
            {
                await DoExport();
            }
            catch ( Exception ex )
            {
                await MainThread.InvokeOnMainThreadAsync( () =>
                {
                    lOutput.Text = ex.Message;
                    btnRetry.IsVisible = true;
                } );
            }
        } );
    }

    private async Task DoExport()
    {
        await MainThread.InvokeOnMainThreadAsync( () =>
        {
            vslForm.IsVisible = false;
            vslOutput.IsVisible = true;
            btnSave.IsVisible = false;
            tbSaveLocation.IsVisible = false;
            lSaveLocation.IsVisible = false;
            btnRetry.IsVisible = false;
        } );

        string mainDir = FileSystem.Current.AppDataDirectory;
        var tempDir = mainDir + "\\Temp";
        var zipFile = mainDir + "\\Elvanto.slingshot";

        if ( Directory.Exists( tempDir ) )
        {
            Directory.Delete( tempDir, true );
        }
        Directory.CreateDirectory( tempDir );

        if ( File.Exists( zipFile ) )
        {
            File.Delete( zipFile );
        }

        using ( var zip = ZipFile.Open( zipFile, ZipArchiveMode.Create ) )
        {

            await MainThread.InvokeOnMainThreadAsync( () =>
            {
                lOutput.Text = "Initializing Elvanto Client";
            } );
            var client = new Client( tbApiKey.Text );

            await MainThread.InvokeOnMainThreadAsync( () =>
            {
                lOutput.Text += "\nLoading Person Categories";
            } );

            var categories = client.Get<Category>();
            var categoryLookup = new Dictionary<string, string>();
            await foreach ( var category in categories )
            {
                if ( category.Id == null || category.Name == null )
                {
                    continue;
                }
                categoryLookup[category.Id] = category.Name;
            }

            await MainThread.InvokeOnMainThreadAsync( () =>
            {
                lOutput.Text += "\nLoading Person Attributes";
            } );


            var customFields = client.Get<CustomFields>();
            var fields = new List<string>();

            var personAttributes = new StringBuilder();
            personAttributes.AppendLine( "Key,Name,FieldType,Category" );

            var keys = new List<string>();

            await foreach ( var customField in customFields )
            {
                if ( customField?.Name != null && !keys.Contains( customField.Id ) )
                {
                    keys.Add( customField.Id );
                    personAttributes.AppendLine( $"{customField.Id},{customField.Name},{( customField.Type == "datetime" ? "Rock.Field.Types.DateFieldType" : "Rock.Field.Types.TextFieldType" )},Elvanto" );
                    fields.Add( $"custom_{customField.Id}" );
                }
            }

            File.WriteAllText( $"{tempDir}\\person-attribute.csv", personAttributes.ToString() );
            zip.CreateEntryFromFile( $"{tempDir}\\person-attribute.csv", "person-attribute.csv" );

            await MainThread.InvokeOnMainThreadAsync( () =>
            {
                lOutput.Text += "\nLoading Person Data";
            } );

            var personIdManager = new IdLookupManager( 1000 );
            var familyIdLookupManager = new IdLookupManager( 1000 );
            var campusIdManager = new IdLookupManager( 1 );

            var personSb = new StringBuilder();
            personSb.AppendLine( "Id,FamilyId,FamilyName,FamilyImageUrl,FamilyRole,FirstName,NickName,LastName,MiddleName,Salutation,Suffix,Email,Gender,MaritalStatus,Birthdate,AnniversaryDate,RecordStatus,InactiveReason,ConnectionStatus,EmailPreference,CreatedDateTime,ModifiedDateTime,PersonPhotoUrl,CampusId,CampusName,Note,Grade,GiveIndividually,IsDeceased" );

            var personAttributeValue = new StringBuilder();
            personAttributeValue.AppendLine( "PersonId,AttributeKey,AttributeValue" );

            var phonenumberSb = new StringBuilder();
            phonenumberSb.AppendLine( "PersonId,PhoneType,PhoneNumber,IsMessagingEnabled,IsUnlisted" );

            var personAddress = new StringBuilder();
            personAddress.AppendLine( "PersonId,Street1,Street2,City,State,PostalCode,Country,Latitude,Longitude,IsMailing,AddressType" );

            var data = client.Get<Person>( fields );

            var count = 0;

            var familiesWithSaveAddressses = new List<int>();

            await foreach ( var person in data )
            {
                var personalCategory = "Attendee";
                if ( categoryLookup.ContainsKey( person.CategoryId ?? "" ) )
                {
                    personalCategory = categoryLookup[person.CategoryId ?? ""];
                }


                var personId = personIdManager.GetId( person.Id );
                if ( personId % 1000 == 0 )
                {
                    count += 1000;
                    await MainThread.InvokeOnMainThreadAsync( () =>
                    {
                        lOutput.Text += $"\nLoaded {count} people";
                    } );
                }

                var familyId = familyIdLookupManager.GetId( person.FamilyId );
                if ( familyId == 0 )
                {
                    familyId = familyIdLookupManager.GetId( Guid.NewGuid().ToString() );
                }

                //PERSON
                var personRow = $"{personId}," +
                 $"{familyId}," +
                 $"{person.Lastname}," +
                 $"," + //Family Image
                 $"{( person.FamilyRelationship == "Child" ? "Child" : "Adult" )}," +
                 $"{person.Firstname.ForCSV()}," +
                 $"{person.PreferredName.ForCSV()}," +
                 $"{person.Lastname}," +
                 $"{person.MiddleName}," +
                 $"," + //Salutation
                 $"," + //Suffix
                 $"{person.Email}," +
                 $"{( string.IsNullOrEmpty( person.Gender ) ? "Unknown" : person.Gender )}," +
                 $"{person.MaritalStatus.AsMaritalStatus()}," +
                 $"{person.BirthdayFormatted}," +
                 $"," + //Anniversary Date
                 $"{person.GetRecordStatus()}," +
                 $"," + //Inactive Reason
                 $"{personalCategory}," +
                 $"{( person.GetRecordStatus() == "Active" ? "EmailAllowed" : "DoNotEmail" )}," +
                 $"{person.DateAddedFormatted}," +
                 $"{person.DateModifiedFormatted}," +
                 $"{person.Picture}," +
                 $"{( person.Campus != null ? campusIdManager.GetId( person.Campus.Id ) : "1" )}," +
                 $"{( person.Campus != null ? person.Campus.Name : "" )}," +
                 $"{personalCategory}," + //Person Note
                 $"{person.Grade}," +
                 $"," + //Give Individually
                 $"{( person.Deceased == 1 ? "TRUE" : "FALSE" )}";
                personSb.AppendLine( personRow );

                //PhoneNumbers
                if ( !string.IsNullOrWhiteSpace( person.Phone ) )
                {
                    phonenumberSb.AppendLine( $"{personId}," +
                        $"Home," +
                        $"{person.Phone}," +
                        $"False," +
                        $"False" );
                }

                if ( !string.IsNullOrWhiteSpace( person.Mobile ) )
                {
                    phonenumberSb.AppendLine( $"{personId}," +
                        $"Mobile," +
                        $"{person.Mobile}," +
                        $"True," +
                        $"False" );
                }

                //ADDRESSES

                if ( !familiesWithSaveAddressses.Contains( familyId )
                    && !string.IsNullOrEmpty( person.Address )
                    && !string.IsNullOrEmpty( person.City )
                    && !string.IsNullOrEmpty( person.Country ) )
                {
                    familiesWithSaveAddressses.Add( familyId );
                    
                    var address = $"{personIdManager.GetId( person.Id )}," +
                        $"{person.Address.ForCSV()}," +
                        $"{person.Address2.ForCSV()}," +
                        $"{person.City.ForCSV()}," +
                        $"{person.State}," +
                        $"{person.PostCode}," +
                        $"{CountryAbbreviation.GetCode( person.Country )}," +
                        $",," + //Lat Lng
                        $"TRUE," +
                        $"Home";
                    personAddress.AppendLine( address );
                }

                //PERSON ATTRIBUTE VALUES
                foreach ( var attribute in person.AttributeValues )
                {
                    if ( !string.IsNullOrWhiteSpace( attribute.Value ) )
                    {
                        var attributeValues = $"{personIdManager.GetId( person.Id )},{attribute.Key},{attribute.Value.ForCSV()}";
                        personAttributeValue.AppendLine( attributeValues );
                    }
                }
            }
            await MainThread.InvokeOnMainThreadAsync( () =>
            {
                lOutput.Text += $"\nFinished Loading people";
            } );

            File.WriteAllText( $"{tempDir}\\person.csv", personSb.ToString() );
            zip.CreateEntryFromFile( $"{tempDir}\\person.csv", "person.csv" );
            File.WriteAllText( $"{tempDir}\\person-attributevalue.csv", personAttributeValue.ToString() );
            zip.CreateEntryFromFile( $"{tempDir}\\person-attributevalue.csv", "person-attributevalue.csv" );
            File.WriteAllText( $"{tempDir}\\person-address.csv", personAddress.ToString() );
            zip.CreateEntryFromFile( $"{tempDir}\\person-address.csv", "person-address.csv" );
            File.WriteAllText( $"{tempDir}\\person-phone.csv", phonenumberSb.ToString() );
            zip.CreateEntryFromFile( $"{tempDir}\\person-phone.csv", "person-phone.csv" );

            if ( cbGroups.IsChecked )
            {
                //GROUPS!
                await MainThread.InvokeOnMainThreadAsync( () =>
                {
                    lOutput.Text += "\nLoading Groups";
                } );
                var groups = client.Get<Group>();

                var grouptypeSb = new StringBuilder();
                var groupTypes = new Dictionary<int, string>();
                grouptypeSb.AppendLine( "Id,Name" );
                var grouptypesIdManager = new IdLookupManager();

                var groupsSb = new StringBuilder();
                groupsSb.AppendLine( "Id,Name,Description,Order,ParentGroupId,GroupTypeId,CampusId,Capacity,MeetingDay,MeetingTime,IsActive,IsPublic" );
                var groupIdManager = new IdLookupManager();

                var groupMembersSb = new StringBuilder();
                groupMembersSb.AppendLine( "PersonId,GroupId,Role" );

                var groupAddressSb = new StringBuilder();
                groupAddressSb.AppendLine( "GroupId,Street1,Street2,City,State,PostalCode,Country,Latitude,Longitude,IsMailing,AddressType" );

                await foreach ( var group in groups )
                {
                    if ( group == null )
                    {
                        continue;
                    }

                    var groupTypeId = 100;
                    var groupType = group.GroupType;
                    if ( groupType != null )
                    {
                        groupTypeId = grouptypesIdManager.GetId( groupType.Id );
                        if ( !groupTypes.ContainsKey( groupTypeId ) )
                        {
                            groupTypes.Add( groupTypeId, groupType.Name ?? "Imported Group Type" );
                            grouptypeSb.AppendLine( $"{groupTypeId}, {groupTypes[groupTypeId]}" );
                        }
                    }

                    var groupId = groupIdManager.GetId( group.Id );
                    int? campusId = group.Campus != null ? campusIdManager.GetId( group.Campus?.Id ) : null;

                    groupsSb.AppendLine( $"{groupId}," +
                        $"{group.Name.Truncate( 49 ).ForCSV()}," +
                        $"{group.Description.StripHTML().Truncate( 255 ).ForCSV()}," +
                        $"0," + // Order
                        $"0," + //Parent Group Id
                        $"{groupTypeId}," +
                        $"{campusId}," +
                        $"," + //Capacity
                        $"{group.MeetingDay}," +
                        $"{group.MeetingTime.FormatTime()}," +
                        $"{group.Status == "Active"}," +
                        $"FALSE" );

                    if ( !string.IsNullOrEmpty( group.MeetingAddress )
                        && !string.IsNullOrEmpty( group.MeetingCountry ) )
                    {
                        groupAddressSb.AppendLine( $"{groupId}," +
                            $"{group.MeetingAddress.ForCSV()}," +
                            $"," + //Street 2
                            $"{group.MeetingCity.ForCSV()}," +
                            $"{group.MeetingState}," +
                            $"{CountryAbbreviation.GetCode( group.MeetingCountry )}," +
                            $"{group.MeetingPostcode}," +
                            $"," + //Lat
                            $"," + //Lon
                            $"FALSE," +
                            $"Other" );
                    }

                    foreach ( var person in group.GroupMembers )
                    {
                        groupMembersSb.AppendLine( $"{personIdManager.GetId( person.Id )}," +
                            $"{groupId}," +
                            $"{( !string.IsNullOrEmpty( person.Position ) ? person.Position : "Member" )}" );
                    }
                }
                File.WriteAllText( $"{tempDir}\\grouptype.csv", grouptypeSb.ToString().Trim() );
                zip.CreateEntryFromFile( $"{tempDir}\\grouptype.csv", "grouptype.csv" );
                File.WriteAllText( $"{tempDir}\\group.csv", groupsSb.ToString().Trim() );
                zip.CreateEntryFromFile( $"{tempDir}\\group.csv", "group.csv" );
                File.WriteAllText( $"{tempDir}\\groupmember.csv", groupMembersSb.ToString().Trim() );
                zip.CreateEntryFromFile( $"{tempDir}\\groupmember.csv", "groupmember.csv" );
                File.WriteAllText( $"{tempDir}\\group-address.csv", groupAddressSb.ToString().Trim() );
                zip.CreateEntryFromFile( $"{tempDir}\\group-address.csv", "group-address.csv" );
            }


            if ( cbFinancial.IsChecked )
            {
                await MainThread.InvokeOnMainThreadAsync( () =>
                {
                    lOutput.Text += "\nLoading Accounts";
                } );

                var accounts = client.Get<FinancialCategory>();
                var accountSb = new StringBuilder();
                accountSb.AppendLine( "Id,Name,IsTaxDeductible,CampusId,ParentAccountId" );
                var accountsIdManager = new IdLookupManager();

                await foreach ( var account in accounts )
                {
                    var accountId = accountsIdManager.GetId( account.Id );

                    accountSb.AppendLine( $"{accountId}," +
                        $"{account.Name}," +
                        $"True," +
                        $"," + //CampusId
                        $"" );//ParentAccountId
                }

                await MainThread.InvokeOnMainThreadAsync( () =>
                {
                    lOutput.Text += "\nLoading Transactions";
                } );

                var transactions = client.Get<Transaction>();

                var batches = new List<FinancialBatch>();
                var batchesIdManager = new IdLookupManager();
                var batchesSb = new StringBuilder();
                batchesSb.AppendLine( "Id,Name,CampusId,StartDate,EndDate,Status,CreatedByPersonId,CreatedDateTime,ModifiedByPersonId,ModifiedDateTime,ControlAmount" );

                var financialTransactions = new List<FinancialTransaction>();
                var transactionsIdManager = new IdLookupManager();
                var transactionsSb = new StringBuilder();
                transactionsSb.AppendLine( "Id,BatchId,AuthorizedPersonId,TransactionDate,TransactionType,TransactionSource,CurrencyType,Summary,TransactionCode,CreatedByPersonId,CreatedDateTime,ModifiedByPersonId,ModifiedDateTime" );

                var financialTransactionDetails = new List<FinancialTransactionDetail>();
                var transactionDetailsIdManager = new IdLookupManager();
                var transactionDetailsSb = new StringBuilder();
                transactionDetailsSb.AppendLine( "Id,TransactionId,AccountId,Amount,Summary,CreatedByPersonId,CreatedDateTime,ModifiedByPersonId,ModifiedDateTime" );

                await foreach ( var transaction in transactions )
                {
                    var batch = transaction.Batch;
                    if ( batch != null )
                    {
                        var batchId = batchesIdManager.GetId( batch.Id );

                        if ( !batches.Where( b => b.Id == batchId ).Any() )
                        {
                            batches.Add( new FinancialBatch
                            {
                                Id = batchId,
                                Name = batch.Name,
                                StartDate = transaction.TransactionDate,
                                EndDate = transaction.TransactionDate
                            } );
                        }
                    }
                    var transactionId = transactionDetailsIdManager.GetId( transaction.Id );

                    financialTransactions.Add( new FinancialTransaction
                    {
                        Id = transactionId,
                        AuthorizedPersonId = personIdManager.GetId( transaction.PersonId ),
                        BatchId = batchesIdManager.GetId( transaction.Batch?.Id ),
                        TransactionDate = transaction.TransactionDate,
                        CurrencyType = transaction.TransactionMethod,
                        Summary = transaction.CheckNumber,
                        TransactionCode = transaction.Id,
                        Total = transaction.TransactionTotal.AsDecimal()
                    } );


                    foreach ( var amount in transaction.Amounts?.Amount ?? new List<Amount>() )
                    {
                        financialTransactionDetails.Add( new FinancialTransactionDetail
                        {
                            Id = transactionDetailsIdManager.GetId( amount.Id ),
                            TransactionId = transactionId,
                            Amount = amount.Total.AsDecimal(),
                            AccountId = accountsIdManager.GetId( amount.Category?.Id )
                        } );
                    }
                }

                foreach ( var batch in batches )
                {
                    var controlAmount = financialTransactions.Select( t => t.Total ).DefaultIfEmpty( 0 ).Sum();
                    batchesSb.AppendLine( $"{batch.Id}," +
                        $"{batch.Name}," +
                        $"," + //CampusId
                        $"{batch.StartDate:MM/dd/yyyy}," +
                        $"{batch.EndDate:MM/dd/yyyy}," +
                        $"Closed," + //Status
                        $"," +//Createdby
                        $"," + //CreatedDate
                        $"," + //ModifiedBy
                        $"," +//ModifiedDate
                        $"{batch.ControlAmount}" );
                }

                foreach ( var transaction in financialTransactions )
                {
                    transactionsSb.AppendLine( $"{transaction.Id}," +
                        $"{transaction.BatchId}," +
                        $"{transaction.AuthorizedPersonId}," +
                        $"{transaction.TransactionDate:MM/dd/yyyy}," +
                        $"{transaction.TransactionType}," +
                        $"{transaction.TransactionSource}," +
                        $"{transaction.CurrencyType.Replace( " ", "" )}," +
                        $"{transaction.Summary}," +//Summary
                        $"{transaction.TransactionCode}," +
                        $"," + //Created By
                        $"," + //Created Date
                        $"," + //Modified By
                        $"," ); //Modified Date
                }

                foreach ( var detail in financialTransactionDetails )
                {
                    transactionDetailsSb.AppendLine( $"{detail.Id}," +
                        $"{detail.TransactionId}," +
                        $"{detail.AccountId}," +
                        $"{detail.Amount}," +
                        $"," + //Created By
                        $"," + //Created Date
                        $"," + //Modified By
                        $"," + //Modified Date
                        $"" ); //Summary
                }

                File.WriteAllText( $"{tempDir}\\financial-account.csv", accountSb.ToString() );
                zip.CreateEntryFromFile( $"{tempDir}\\financial-account.csv", "financial-account.csv" );
                File.WriteAllText( $"{tempDir}\\financial-batch.csv", batchesSb.ToString() );
                zip.CreateEntryFromFile( $"{tempDir}\\financial-batch.csv", "financial-batch.csv" );
                File.WriteAllText( $"{tempDir}\\financial-transaction.csv", transactionsSb.ToString() );
                zip.CreateEntryFromFile( $"{tempDir}\\financial-transaction.csv", "financial-transaction.csv" );
                File.WriteAllText( $"{tempDir}\\financial-transactiondetail.csv", transactionDetailsSb.ToString() );
                zip.CreateEntryFromFile( $"{tempDir}\\financial-transactiondetail.csv", "financial-transactiondetail.csv" );
            }
        }

        await MainThread.InvokeOnMainThreadAsync( () =>
        {
            lOutput.Text += "\nSuccessfully Downloaded Data";
            btnSave.IsVisible = true;
            tbSaveLocation.IsVisible = true;
            lSaveLocation.IsVisible = true;
            tbSaveLocation.Text = Environment.GetFolderPath( Environment.SpecialFolder.Desktop );
        } );
    }

    private void btnRetry_Clicked( object sender, EventArgs e )
    {
        vslForm.IsVisible = true;
        vslOutput.IsVisible = false;
    }

    private void btnSave_Clicked( object sender, EventArgs e )
    {
        try
        {
            var newPath = $"{tbSaveLocation.Text}\\Elvanto_{DateTime.Now:yyyyMMddHHmm}.slingshot";
            File.Copy( $"{FileSystem.Current.AppDataDirectory}\\Elvanto.slingshot", newPath );
            lOutput.Text = $"Slingshot file has been saved to: {newPath}";
        }
        catch ( Exception ex )
        {
            lOutput.Text = ex.Message;
        }
    }
}

