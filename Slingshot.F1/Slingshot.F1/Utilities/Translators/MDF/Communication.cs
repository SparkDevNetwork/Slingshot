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
    public static class F1Communication
    {
        /// <summary>
        /// Translates the communication data.
        /// </summary>
        /// <param name="tableData">The table data.</param>
        /// <param name="totalRows">The total rows.</param>
        private void TranslateCommunication( IQueryable<Row> tableData, long totalRows = 0 )
        {
            var lookupContext = new RockContext();
            var personService = new PersonService( lookupContext );

            var definedTypePhoneType = DefinedTypeCache.Get( new Guid( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE ), lookupContext );
            var otherNumberTypeId = definedTypePhoneType.DefinedValues.Where( dv => dv.Value.StartsWith( "Other" ) ).Select( v => (int?)v.Id ).FirstOrDefault();
            if ( !otherNumberTypeId.HasValue )
            {
                var otherNumberType = AddDefinedValue( lookupContext, Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Other" );
                if ( otherNumberType != null )
                {
                    definedTypePhoneType.DefinedValues.Add( otherNumberType );
                    otherNumberTypeId = otherNumberType.Id;
                }
            }

            // Look up existing Person attributes
            var personAttributes = new AttributeService( lookupContext ).GetByEntityTypeId( PersonEntityTypeId ).AsNoTracking().ToList();

            // Cached Rock attributes: Facebook, Twitter, Instagram
            var twitterAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "Twitter", StringComparison.InvariantCultureIgnoreCase ) );
            var facebookAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "Facebook", StringComparison.InvariantCultureIgnoreCase ) );
            var instagramAttribute = personAttributes.FirstOrDefault( a => a.Key.Equals( "Instagram", StringComparison.InvariantCultureIgnoreCase ) );

            var newNumbers = new List<PhoneNumber>();
            var existingNumbers = new PhoneNumberService( lookupContext ).Queryable().AsNoTracking().ToList();
            var newPeopleAttributes = new Dictionary<int, Person>();

            if ( totalRows == 0 )
            {
                totalRows = tableData.Count();
            }

            var completed = 0;
            var percentage = ( totalRows - 1 ) / 100 + 1;
            ReportProgress( 0, $"Verifying communication import ({totalRows:N0} found, {existingNumbers.Count:N0} already exist)." );

            foreach ( var groupedRows in tableData.OrderByDescending( r => r["LastUpdatedDate"] ).GroupBy<Row, int?>( r => r["Household_ID"] as int? ) )
            {
                foreach ( var row in groupedRows.Where( r => r != null ) )
                {
                    var value = row["Communication_Value"] as string;
                    var individualId = row["Individual_ID"] as int?;
                    var householdId = row["Household_ID"] as int?;
                    var peopleToUpdate = new List<PersonKeys>();

                    if ( individualId != null )
                    {
                        var matchingPerson = GetPersonKeys( individualId, householdId, includeVisitors: false );
                        if ( matchingPerson != null )
                        {
                            peopleToUpdate.Add( matchingPerson );
                        }
                    }
                    else
                    {
                        peopleToUpdate = GetFamilyByHouseholdId( householdId, includeVisitors: false );
                    }

                    if ( peopleToUpdate.Any() && !string.IsNullOrWhiteSpace( value ) )
                    {
                        var lastUpdated = row["LastUpdatedDate"] as DateTime?;
                        var communicationComment = row["Communication_Comment"] as string;
                        var type = row["Communication_Type"] as string;
                        var isListed = (bool)row["Listed"];
                        value = value.RemoveWhitespace();

                        // Communication value is a number
                        if ( type.Contains( "Phone" ) || type.Contains( "Mobile" ) )
                        {
                            var extension = string.Empty;
                            var countryCode = PhoneNumber.DefaultCountryCode();
                            var normalizedNumber = string.Empty;
                            var countryIndex = value.IndexOf( '+' );
                            var extensionIndex = value.LastIndexOf( 'x' ) > 0 ? value.LastIndexOf( 'x' ) : value.Length;
                            if ( countryIndex >= 0 )
                            {
                                countryCode = value.Substring( countryIndex, countryIndex + 3 ).AsNumeric();
                                normalizedNumber = value.Substring( countryIndex + 3, extensionIndex - 3 ).AsNumeric();
                                extension = value.Substring( extensionIndex );
                            }
                            else if ( extensionIndex > 0 )
                            {
                                normalizedNumber = value.Substring( 0, extensionIndex ).AsNumeric();
                                extension = value.Substring( extensionIndex ).AsNumeric();
                            }
                            else
                            {
                                normalizedNumber = value.AsNumeric();
                            }

                            if ( !string.IsNullOrWhiteSpace( normalizedNumber ) )
                            {
                                foreach ( var personKeys in peopleToUpdate )
                                {
                                    var matchingNumberTypeId = definedTypePhoneType.DefinedValues.Where( v => type.StartsWith( v.Value, StringComparison.CurrentCultureIgnoreCase ) )
                                        .Select( v => (int?)v.Id ).FirstOrDefault() ?? otherNumberTypeId;

                                    var numberExists = existingNumbers.Any( n => n.PersonId == personKeys.PersonId && n.Number.Equals( normalizedNumber ) && n.NumberTypeValueId == matchingNumberTypeId );
                                    if ( !numberExists )
                                    {
                                        var newNumber = new PhoneNumber();
                                        newNumber.CreatedByPersonAliasId = ImportPersonAliasId;
                                        newNumber.ModifiedDateTime = lastUpdated;
                                        newNumber.PersonId = (int)personKeys.PersonId;
                                        newNumber.IsMessagingEnabled = type.StartsWith( "Mobile", StringComparison.CurrentCultureIgnoreCase );
                                        newNumber.CountryCode = countryCode;
                                        newNumber.IsUnlisted = !isListed;
                                        newNumber.Extension = extension.Left( 20 ) ?? string.Empty;
                                        newNumber.Number = normalizedNumber.Left( 20 );
                                        newNumber.Description = communicationComment;
                                        newNumber.NumberFormatted = PhoneNumber.FormattedNumber( countryCode, newNumber.Number, true );
                                        newNumber.NumberTypeValueId = matchingNumberTypeId;

                                        newNumbers.Add( newNumber );
                                        existingNumbers.Add( newNumber );
                                    }
                                }

                                completed++;
                            }
                        }
                        else
                        {
                            var personKeys = peopleToUpdate.FirstOrDefault();
                            var person = !newPeopleAttributes.ContainsKey( personKeys.PersonId )
                                ? personService.Queryable( includeDeceased: true ).FirstOrDefault( p => p.Id == personKeys.PersonId )
                                : newPeopleAttributes[personKeys.PersonId];

                            if ( person != null )
                            {
                                if ( person.Attributes == null || person.AttributeValues == null )
                                {
                                    // make sure we have valid objects to assign to
                                    person.Attributes = new Dictionary<string, AttributeCache>();
                                    person.AttributeValues = new Dictionary<string, AttributeValueCache>();
                                }

                                // Check for an InFellowship ID/email before checking other types of email
                                var isLoginValue = type.IndexOf( "InFellowship", StringComparison.OrdinalIgnoreCase ) >= 0;
                                var personAlreadyHasLogin = person.Attributes.ContainsKey( InFellowshipLoginAttribute.Key );
                                if ( isLoginValue && !personAlreadyHasLogin )
                                {
                                    // add F1 authentication capability
                                    AddEntityAttributeValue( lookupContext, InFellowshipLoginAttribute, person, value );
                                    //AddUserLogin( f1AuthProviderId, person, value );
                                }

                                // also add the Infellowship Email to anyone who doesn't have one
                                if ( value.IsEmail() )
                                {
                                    // person email is empty
                                    if ( string.IsNullOrWhiteSpace( person.Email ) )
                                    {
                                        person.Email = value.Left( 75 );
                                        person.IsEmailActive = isListed;
                                        person.EmailPreference = isListed ? EmailPreference.EmailAllowed : EmailPreference.DoNotEmail;
                                        person.ModifiedDateTime = lastUpdated;
                                        person.EmailNote = communicationComment;
                                        lookupContext.SaveChanges( DisableAuditing );
                                    }
                                    // this is a different email, assign it to SecondaryEmail
                                    else if ( !person.Email.Equals( value ) && !person.Attributes.ContainsKey( SecondaryEmailAttribute.Key ) )
                                    {
                                        AddEntityAttributeValue( lookupContext, SecondaryEmailAttribute, person, value );
                                    }
                                }
                                else if ( type.Contains( "Twitter" ) && !person.Attributes.ContainsKey( twitterAttribute.Key ) )
                                {
                                    AddEntityAttributeValue( lookupContext, twitterAttribute, person, value );
                                }
                                else if ( type.Contains( "Facebook" ) && !person.Attributes.ContainsKey( facebookAttribute.Key ) )
                                {
                                    AddEntityAttributeValue( lookupContext, facebookAttribute, person, value );
                                }
                                else if ( type.Contains( "Instagram" ) && !person.Attributes.ContainsKey( instagramAttribute.Key ) )
                                {
                                    AddEntityAttributeValue( lookupContext, instagramAttribute, person, value );
                                }

                                if ( !newPeopleAttributes.ContainsKey( personKeys.PersonId ) )
                                {
                                    newPeopleAttributes.Add( personKeys.PersonId, person );
                                }
                                else
                                {
                                    newPeopleAttributes[personKeys.PersonId] = person;
                                }
                            }

                            completed++;
                        }

                        if ( completed % percentage < 1 )
                        {
                            var percentComplete = completed / percentage;
                            ReportProgress( percentComplete, $"{completed:N0} communication items imported ({percentComplete}% complete)." );
                        }
                        else if ( completed % ReportingNumber < 1 )
                        {
                            if ( newNumbers.Any() || newPeopleAttributes.Any() )
                            {
                                SaveCommunication( newNumbers, newPeopleAttributes );
                            }

                            // reset so context doesn't bloat
                            lookupContext = new RockContext();
                            personService = new PersonService( lookupContext );
                            newPeopleAttributes.Clear();
                            newNumbers.Clear();
                            ReportPartialProgress();
                        }
                    }
                }
            }

            if ( newNumbers.Any() || newPeopleAttributes.Any() )
            {
                SaveCommunication( newNumbers, newPeopleAttributes );
            }

            ReportProgress( 100, $"Finished communications import: {completed:N0} items imported." );
        }

        /// <summary>
        /// Saves the communication.
        /// </summary>
        /// <param name="newNumberList">The new number list.</param>
        /// <param name="updatedPersonList">The updated person list.</param>
        private static void SaveCommunication( List<PhoneNumber> newNumberList, Dictionary<int, Person> updatedPersonList )
        {
            var rockContext = new RockContext();
            rockContext.WrapTransaction( () =>
            {
                rockContext.Configuration.AutoDetectChangesEnabled = false;

                if ( newNumberList.Any() )
                {
                    rockContext.PhoneNumbers.AddRange( newNumberList );
                }

                if ( updatedPersonList.Any() )
                {
                    foreach ( var person in updatedPersonList.Values.Where( p => p.Attributes.Any() ) )
                    {
                        // don't call LoadAttributes, it only rewrites existing cache objects
                        // person.LoadAttributes( rockContext );

                        foreach ( var attributeCache in person.Attributes.Select( a => a.Value ) )
                        {
                            var existingValue = rockContext.AttributeValues.FirstOrDefault( v => v.Attribute.Key == attributeCache.Key && v.EntityId == person.Id );
                            var newAttributeValue = person.AttributeValues[attributeCache.Key];

                            // set the new value and add it to the database
                            if ( existingValue == null )
                            {
                                existingValue = new AttributeValue
                                {
                                    AttributeId = newAttributeValue.AttributeId,
                                    EntityId = person.Id,
                                    Value = newAttributeValue.Value
                                };

                                rockContext.AttributeValues.Add( existingValue );
                            }
                            else
                            {
                                existingValue.Value = newAttributeValue.Value;
                                rockContext.Entry( existingValue ).State = EntityState.Modified;
                            }
                        }
                    }
                }

                rockContext.ChangeTracker.DetectChanges();
                rockContext.SaveChanges( DisableAuditing );
            } );
        }
    }
}