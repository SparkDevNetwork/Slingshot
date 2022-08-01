using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.Core.Utilities;
using Slingshot.PCO.Models.DTO;
using Slingshot.PCO.Utilities.Translators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// PCO API Class - Individual data export methods.
    /// </summary>
    public static partial class PCOApi
    {
        /// <summary>
        /// Api Endpoint Declarations.
        /// </summary>
        internal static partial class ApiEndpoint
        {
            internal const string API_PEOPLEORGANIZATION = "/people/v2";
            internal const string API_PEOPLE = "/people/v2/people";
            internal const string API_SERVICE_PEOPLE = "/services/v2/people";
            internal const string API_NOTES = "/people/v2/notes";
            internal const string API_FIELD_DEFINITIONS = "/people/v2/field_definitions";
        }

        /// <summary>
        /// Test access to the people API.
        /// </summary>
        /// <returns></returns>
        public static bool TestIndividualAccess()
        {
            var initalErrorValue = PCOApi.ErrorMessage;

            var response = ApiGet( ApiEndpoint.API_PEOPLEORGANIZATION );

            PCOApi.ErrorMessage = initalErrorValue;

            return ( response != string.Empty );
        }

        #region ExportIndividuals() and Related Methods

        /// <summary>
        /// Exports the individuals.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        public static void ExportIndividuals( DateTime modifiedSince, int peoplePerPage = 100 )
        {
            var apiOptions = new Dictionary<string, string>
            {
                { "include", "emails,addresses,phone_numbers,field_data,households,inactive_reason,marital_status,name_prefix,name_suffix,primary_campus,school,social_profiles" },
                { "per_page", peoplePerPage.ToString() }
            };

            var PCOPeople = GetPeople( ApiEndpoint.API_PEOPLE, apiOptions, modifiedSince );
            var PCOServicePeople = GetServicePeople( modifiedSince );
            var PCONotes = GetNotes( modifiedSince );
            var headOfHouseholdMap = GetHeadOfHouseholdMap( PCOPeople );
            var personAttributes = WritePersonAttributes();

            foreach ( var person in PCOPeople )
            {
                PersonDTO headOfHouse = person; // Default headOfHouse to person, in case they are not assigned to a household in PCO.
                if( person.Household != null && headOfHouseholdMap.ContainsKey( person.Household.Id ) )
                {
                    headOfHouse = headOfHouseholdMap[person.Household.Id];
                }

                // The backgroundCheckPerson is pulled from a different API endpoint.
                PersonDTO backgroundCheckPerson = null;
                if ( PCOServicePeople != null )
                {
                    backgroundCheckPerson = PCOServicePeople.Where( x => x.Id == person.Id ).FirstOrDefault();
                }

                var importPerson = PCOImportPerson.Translate( person, personAttributes, headOfHouse, backgroundCheckPerson );
                if ( importPerson != null )
                {
                    ImportPackage.WriteToPackage( importPerson );
                }

                // save person image
                if ( person.Avatar.IsNotNullOrWhitespace() )
                {
                    WebClient client = new WebClient();

                    var path = Path.Combine( ImportPackage.ImageDirectory, "Person_" + person.Id + ".png" );
                    try
                    {
                        client.DownloadFile( new Uri( person.Avatar ), path );
                        ApiCounter++;
                    }
                    catch ( Exception ex )
                    {
                        Console.WriteLine( ex.Message );
                    }
                }
            }
            // save notes.
            if ( PCONotes != null )
            {
                foreach ( NoteDTO note in PCONotes )
                {
                    PersonNote importNote = PCOImportPersonNote.Translate( note );
                    if ( importNote != null )
                    {
                        ImportPackage.WriteToPackage( importNote );
                    }
                }
            }
        }

        /// <summary>
        /// Gets people from the services endpoint.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <param name="peoplePerPage">The people per page.</param>
        /// <returns></returns>
        private static List<PersonDTO> GetServicePeople( DateTime modifiedSince, int peoplePerPage = 100 )
        {
            var apiOptions = new Dictionary<string, string>
            {
                { "per_page", peoplePerPage.ToString() }
            };

            return GetPeople( ApiEndpoint.API_SERVICE_PEOPLE, apiOptions, modifiedSince );
        }

        /// <summary>
        /// Gets people from the specified endpoint.
        /// </summary>
        /// <param name="apiEndPoint">The API end point.</param>
        /// <param name="apiRequestOptions">A collection of request options.</param>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        public static List<PersonDTO> GetPeople( string apiEndPoint, Dictionary<string, string> apiRequestOptions, DateTime? modifiedSince )
        {
            var people = new List<PersonDTO>();

            var personQuery = GetAPIQuery( apiEndPoint, apiRequestOptions, modifiedSince );

            if ( personQuery == null )
            {
                return people;
            }

            foreach ( var item in personQuery.Items )
            {
                var person = new PersonDTO( item, personQuery.IncludedItems );
                people.Add( person );
            }

            return people;
        }

        /// <summary>
        /// Maps household Ids to the PCOPerson object designated as the primary contact for that household.  This map method is used to avoid repetitive searches for the head of household for each household member.
        /// </summary>
        /// <param name="people">The list of <see cref="PersonDTO"/> records.</param>
        /// <returns></returns>
        private static Dictionary<int, PersonDTO> GetHeadOfHouseholdMap( List<PersonDTO> people )
        {
            var map = new Dictionary<int, PersonDTO>();

            foreach ( var person in people )
            {
                if ( person.Household == null || map.ContainsKey( person.Household.Id ) )
                {
                    continue;
                }

                if ( person.Household.PrimaryContactId == person.Id )
                {
                    map.Add( person.Household.Id, person );
                }
            }

            return map;
        }

        /// <summary>
        /// Gets notes from PCO.
        /// </summary>
        /// <param name="modifiedSince">The modified since.</param>
        /// <returns></returns>
        public static List<NoteDTO> GetNotes( DateTime? modifiedSince )
        {
            var notes = new List<NoteDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "include", "category" }
            };

            var notesQuery = GetAPIQuery( ApiEndpoint.API_NOTES, apiOptions, modifiedSince );

            if ( notesQuery == null )
            {
                return notes;
            }


            foreach ( var item in notesQuery.Items )
            {
                var note = new NoteDTO( item, notesQuery.IncludedItems );
                notes.Add( note );
            }

            return notes;
        }

        /// <summary>
        /// Exports the person attributes.
        /// </summary>
        private static List<FieldDefinitionDTO> WritePersonAttributes()
        {
            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Facebook",
                Key = "Facebook",
                Category = "Social Media",
                FieldType = "Rock.Field.Types.SocialMediaAccountFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Twitter",
                Key = "Twitter",
                Category = "Social Media",
                FieldType = "Rock.Field.Types.SocialMediaAccountFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Instagram",
                Key = "Instagram",
                Category = "Social Media",
                FieldType = "Rock.Field.Types.SocialMediaAccountFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "LinkedIn",
                Key = "LinkedIn",
                Category = "Social Media",
                FieldType = "Rock.Field.Types.SocialMediaAccountFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "School",
                Key = "School",
                Category = "Education",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "PCO Remote Id",
                Key = "RemoteId",
                Category = "Education",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            ImportPackage.WriteToPackage( new PersonAttribute()
            {
                Name = "Background Check Result",
                Key = "BackgroundCheckResult",
                Category = "Safety & Security",
                FieldType = "Rock.Field.Types.TextFieldType"
            } );

            var attributes = new List<FieldDefinitionDTO>();

            // export person attributes
            try
            {
                var fieldDefinitions = GetFieldDefinitions();

                foreach ( var fieldDefinition in fieldDefinitions )
                {
                    // get field type
                    var fieldtype = "Rock.Field.Types.TextFieldType";
                    if ( fieldDefinition.DataType == "text" )
                    {
                        fieldtype = "Rock.Field.Types.MemoFieldType";
                    }
                    else if ( fieldDefinition.DataType == "date" )
                    {
                        fieldtype = "Rock.Field.Types.DateFieldType";
                    }
                    else if ( fieldDefinition.DataType == "boolean" )
                    {
                        fieldtype = "Rock.Field.Types.BooleanFieldType";
                    }
                    else if ( fieldDefinition.DataType == "file" )
                    {
                        continue;
                        //fieldtype = "Rock.Field.Types.FileFieldType";
                    }
                    else if ( fieldDefinition.DataType == "number" )
                    {
                        fieldtype = "Rock.Field.Types.IntegerFieldType";
                    }

                    var newAttribute = new PersonAttribute()
                    {
                        Name = fieldDefinition.Name,
                        Key = fieldDefinition.Id + "_" + fieldDefinition.Slug,
                        Category = ( fieldDefinition.Tab == null ) ? "PCO Attributes" : fieldDefinition.Tab.Name,
                        FieldType = fieldtype,
                    };

                    ImportPackage.WriteToPackage( newAttribute );

                    attributes.Add( fieldDefinition );
                }
            }
            catch ( Exception ex )
            {
                ErrorMessage = ex.Message;
            }

            return attributes;
        }

        /// <summary>
        /// Get the field definitions from PCO.
        /// </summary>
        /// <returns></returns>
        private static List<FieldDefinitionDTO> GetFieldDefinitions()
        {
            var fields = new List<FieldDefinitionDTO>();

            var apiOptions = new Dictionary<string, string>
            {
                { "include", "field_options,tab" }
            };

            var fieldQuery = GetAPIQuery( ApiEndpoint.API_FIELD_DEFINITIONS, apiOptions );

            if ( fieldQuery == null )
            {
                return fields;
            }

            foreach ( var item in fieldQuery.Items )
            {
                var field = new FieldDefinitionDTO( item, fieldQuery.IncludedItems );
                if ( field != null && field.DataType != "header" )
                {
                    fields.Add( field );
                }
            }
            return fields;
        }

        #endregion ExportIndividuals() and Related Methods
    }
}
