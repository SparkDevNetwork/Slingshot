using System;
using System.Globalization;
using System.Data;
using System.Text.RegularExpressions;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    public static class SkPerson
    {
        public static Person Translate( DataRow row )
        {
            string field;
            DateTime dtvalue;
            Person person = new Person();

            // Note History
            //var notes = new List<string>();
            //if (notes.Count > 0)
            //{
            //    person.Note = string.Join(",", notes);
            //}

            // Servant Keeper Individual ID is too big to fit into a Rock Person ID.
            // Use the Person Record ID instead. Store the old value in the Dictionary AND in an Attribute
            field = row.Field<string>("IND_ID");
            person.Id = row.Field<int>("PERSON_REC_ID");
            person.Attributes.Add(new PersonAttributeValue
            {
                AttributeKey = "SKPersonID",  // Manually define this attribute in Rock beforehand
                AttributeValue = field,
                PersonId = person.Id
            });

            // Servant Keeper Family ID is too big to fit into a Rock Family ID.
            // Use the Family Record ID instead. Store the old value in an attribute.
            field = row.Field<string>("FAMILY_ID");
            if (field.IsNotNullOrWhitespace())
            {
                person.FamilyId = row.Field<int>("FAMILY_REC_ID");
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "SKFamilyID",  // Manually define this attribute in Rock beforehand
                    AttributeValue = field,
                    PersonId = person.Id
                });
            }


            // Names of the Person
            string fname = row.Field<string>("FIRST_NAME");
            if (fname.IsNotNullOrWhitespace()) person.FirstName = fname;

            field = row.Field<string>("PREFERNAME");
            if (field.IsNotNullOrWhitespace() && field != fname) person.NickName = field;

            field = row.Field<string>("MID_NAME");
            if (field.IsNotNullOrWhitespace()) person.MiddleName = field;

            field = row.Field<string>("LAST_NAME");
            if (field.IsNotNullOrWhitespace()) person.LastName = field;

            field = row.Field<string>("FAM_NAME");
            if (field.IsNotNullOrWhitespace()) person.FamilyName = field;

            // Suffix - This is a defined type in Rock; not a text field.
            field = row.Field<string>("SUFFIX");
            switch (field)
            {
                // Standard Values
                case "Jr.":
                case "Sr.":
                case "Ph.D.":
                case "II":
                case "III":
                case "IV":
                case "V":
                case "VI":
                    person.Suffix = field;
                    break;

                // Corrections
                case "Jr":
                    person.Suffix = "Jr.";
                    break;
                case "Jr. M.D.":
                    person.Suffix = "M.D.";
                    break;
            }

            // Salutation / Title - This is a defined type in Rock; not a text field.
            field = row.Field<string>("TITLE");
            switch (field)
            {
                // Standard Values
                case "Mr.":
                case "Mrs.":
                case "Ms.":
                case "Miss":
                case "Dr.":
                case "Rev.":
                    person.Salutation = field;
                    break;
                
                // Corrections
                case "mr":
                    person.Salutation = "Mr.";
                    break;
            }

            // Record/Connection Status Fields
            switch (row.Field<string>("MEM_STATUS"))
            {
                case "Active Member":
                    person.RecordStatus = RecordStatus.Active;
                    person.ConnectionStatus = "Member";
                    break;
                case "Bible Study":
                    person.RecordStatus = RecordStatus.Active;
                    person.ConnectionStatus = "Participant";
                    break;
                case "Club182":
                case "Club182 parent":
                    person.RecordStatus = RecordStatus.Active;
                    person.ConnectionStatus = "Club182";    // Manually define this status in Rock beforehand
                    break;
                case "Deceased":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.IsDeceased = true;
                    person.ConnectionStatus = "Attendee";
                    person.InactiveReason = "Deceased";
                    break;
                case "Inactive Attender":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.ConnectionStatus = "Attendee";
                    person.InactiveReason = "No Longer Attending";
                    break;
                case "Inactive Kids Hope":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.ConnectionStatus = "Kids Hope";   // Manually define this status in Rock beforehand
                    person.InactiveReason = "No Longer Attending";
                    break;
                case "Inactive Member":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.ConnectionStatus = "Member";
                    person.InactiveReason = "No Longer Attending";
                    break;
                case "inactive missionary":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.ConnectionStatus = "Missionary";  // Manually define this status in Rock beforehand
                    person.InactiveReason = "No Activity";
                    break;
                case "Inactive Visitor":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.ConnectionStatus = "Visitor";
                    person.InactiveReason = "No Longer Attending";
                    break;
                case "Kids Hope":
                case "Kids Hope parent":
                    person.RecordStatus = RecordStatus.Active;
                    person.ConnectionStatus = "Kids Hope";   // Manually define this status in Rock beforehand
                    break;
                case "missionary":
                    person.RecordStatus = RecordStatus.Active;
                    person.ConnectionStatus = "Missionary";   // Manually define this status in Rock beforehand
                    break;
                case "non attending youth":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.ConnectionStatus = "Non Attender";   // Manually define this status in Rock beforehand
                    person.InactiveReason = "Does not attend with family";
                    break;
                case "Regular Attender":
                case "PrimeTimers":
                    person.RecordStatus = RecordStatus.Active;
                    person.ConnectionStatus = "Attendee";
                    break;
                case "Transferred":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.ConnectionStatus = row.Field<string>("HOW_JOIN").IsNotNullOrWhitespace() ? "Member" : "Attendee";
                    person.InactiveReason = "Moved";
                    break;
                case "VBS":
                    person.RecordStatus = RecordStatus.Active;
                    person.ConnectionStatus = "VBS";     // Manually define this status in Rock beforehand
                    break;
                case "Visitor":
                    person.RecordStatus = RecordStatus.Active;
                    person.ConnectionStatus = "Visitor";
                    break;
                case "youth parent":
                case "fusion parent":
                    person.RecordStatus = RecordStatus.Inactive;
                    person.ConnectionStatus = "Non Attender";    // Manually define this status in Rock beforehand
                    person.InactiveReason = "Does not attend with family";
                    person.Note = "Youth Parent";
                    break;
                default:
                    person.RecordStatus = row.Field<string>("ACTIVE_IND") == "1" ? RecordStatus.Inactive : RecordStatus.Active;
                    person.ConnectionStatus = "Participant";
                    break;
            }


            // Dates
            person.CreatedDateTime = row.Field<DateTime?>("CREATE_TS");
            person.ModifiedDateTime = row.Field<DateTime?>("UPDATE_TS");

            field = row.Field<string>("BIRTH_DT");
            if (DateTime.TryParseExact(field, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtvalue)) person.Birthdate = dtvalue;
            field = row.Field<string>("WEDDING_DT");
            if (DateTime.TryParseExact(field, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtvalue)) person.AnniversaryDate = dtvalue;
            field = row.Field<string>("JOIN_DT");
            if (DateTime.TryParseExact(field, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtvalue))
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "JoinedDate",    // Manually define this attribute in Rock beforehand
                    AttributeValue = dtvalue.ToString("MM/dd/yyyy"),
                    PersonId = person.Id
                });

            field = row.Field<string>("BAPTIZE_DT");
            if (DateTime.TryParseExact(field, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtvalue))
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "BaptismDate",
                    AttributeValue = dtvalue.ToString("MM/dd/yyyy"),
                    PersonId = person.Id
                });

            field = row.Field<string>("MEM_DT");
            if (DateTime.TryParseExact(field, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtvalue))
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "MembershipDate",
                    AttributeValue = dtvalue.ToString("MM/dd/yyyy"),
                    PersonId = person.Id
                });

            // Convert Servant Keeper relationship and/or Age to Rock role
            string rel = row.Field<string>("RELATIONSHIP");
            switch (rel)
            {
                case "Head of Household":
                case "Spouse":
                case "Father":
                case "Mother":
                case "Father In-Law":
                case "Son In-Law":
                case "Grandfather":
                case "Grandmother":
                    person.FamilyRole = FamilyRole.Adult;
                    break;
                default:
                    person.FamilyRole = person.Birthdate.HasValue && person.Birthdate.Age() <= 18 ? FamilyRole.Child : FamilyRole.Adult;
                    break;
            }

            // Gives Individually?  1 Means person is NOT included on family statement.
            person.GiveIndividually = row.Field<string>("STATUS") == "1" ? true : false;

            // Gender
            switch (row.Field<string>("SEX"))
            {
                case "M":
                    person.Gender = Gender.Male; break;
                case "F":
                    person.Gender = Gender.Female; break;
                default:
                    person.Gender = Gender.Unknown; break;
            }

            // School Grade. Rock Bulk Import is ignoring the Grade property on Person.
            // Convert to Graduation Year, store in custom attribute, then update real field on Person table using SQL after import.
            string GraduateYear = null;
            switch (row.Field<string>("GRADE"))
            {
                case "9A":  // Preschool 1
                    GraduateYear = "2033"; break;   // Manually add to Rock as a Grade option. Value 14.
                case "A0":  // Preschool 2
                    GraduateYear = "2032"; break;   // Manually add to Rock as a Grade option. Value 13.
                case "B0":  // Kindergarten
                    GraduateYear = "2031"; break;
                case "C0":  // 1st Grade
                    GraduateYear = "2030"; break;
                case "D0":  // 2nd Grade
                    GraduateYear = "2029"; break;
                case "E0":  // 3rd Grade
                    GraduateYear = "2028"; break;
                case "F0":  // 4th Grade
                    GraduateYear = "2027"; break;
                case "G0":  // 5th Grade
                    GraduateYear = "2026"; break;
                case "H0":   // 6th Grade
                    GraduateYear = "2025"; break;
                case "I0":  // 7th Grade
                    GraduateYear = "2024"; break;
                case "J0":  // 8th Grade
                    GraduateYear = "2023"; break;
                case "K0":  // Freshman
                    GraduateYear = "2022"; break;
                case "L0":  // Sophomore
                    GraduateYear = "2021"; break;
                case "M0":   // Junior
                    GraduateYear = "2020"; break;
                case "N0":  // Senior
                    GraduateYear = "2019"; break;
                default:
                    field = null; break;
            }
            if (GraduateYear.IsNotNullOrWhitespace())
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "GraduateYear",        // Manually define this attribute in Rock beforehand
                    AttributeValue = GraduateYear,
                    PersonId = person.Id
                });

            // Marital Status.  Rock won't allow import of any custom value. Store those in custom attribute for now and update to
            // real marital status field using SQL after import.
            switch (row.Field<string>("MARITAL"))
            {
                case "Divorced":
                    person.MaritalStatus = MaritalStatus.Divorced; break;
                case "Married":
                    person.MaritalStatus = MaritalStatus.Married; break;
                case "Single":
                    person.MaritalStatus = MaritalStatus.Single; break;
                case "Separated":  // Manually add 'Separated' as Marital Status option to Rock beforehand
                    person.MaritalStatus = MaritalStatus.Unknown;
                    person.Attributes.Add(new PersonAttributeValue
                    {
                        AttributeKey = "MaritalStatus",        // Manually define this attribute in Rock beforehand
                        AttributeValue = "Separated",
                        PersonId = person.Id
                    });
                    break;
                case "W":
                case "Widow":
                case "Widowed":
                case "Widower":  // Manually add 'Widowed' as Marital Status option to Rock beforehand
                    person.MaritalStatus = MaritalStatus.Unknown;
                    person.Attributes.Add(new PersonAttributeValue
                    {
                        AttributeKey = "MaritalStatus",        // Manually define this attribute in Rock beforehand
                        AttributeValue = "Widowed",
                        PersonId = person.Id
                    });
                    break;

                // Manually add option to Rock
                default:
                    if (GraduateYear.IsNotNullOrWhitespace() || (person.Birthdate.HasValue && person.Birthdate.Age() <= 25))
                        person.MaritalStatus = MaritalStatus.Single;
                    else
                        person.MaritalStatus = MaritalStatus.Unknown;
                    break;
            }

            // Email Fields
            field = row.Field<string>("EMAIL1");
            if (field.IsNotNullOrWhitespace())
            {
                person.Email = field;
                person.EmailPreference = row.Field<string>("EMAIL1_IND") == "1" ? EmailPreference.NoMassEmails : EmailPreference.EmailAllowed;
            }

            // Phone Numbers
            field = Regex.Replace(row.Field<string>("H_PHONE") ?? "", @"[^\d]", ""); // Strip out non-digits. Replace doesn't like null.
            if (field.Length >= 7)
                person.PhoneNumbers.Add(new PersonPhone()
                {
                    PhoneNumber = field,
                    PersonId = person.Id,
                    IsUnlisted = row.Field<string>("H_UNLIST") == "1" ? true : false,
                    PhoneType = "Home"
                });

            field = Regex.Replace(row.Field<string>("W_PHONE") ?? "", @"[^\d]", ""); // Strip out non-digits. Replace doesn't like null.
            if (field.Length >= 7)
                person.PhoneNumbers.Add(new PersonPhone()
                {
                    PhoneNumber = field,
                    PersonId = person.Id,
                    IsUnlisted = row.Field<string>("W_UNLIST") == "1" ? true : false,
                    PhoneType = "Work"
                });

            field = Regex.Replace(row.Field<string>("C_PHONE") ?? "", @"[^\d]", ""); // Strip out non-digits. Replace doesn't like null.
            if (field.Length >= 7)
                person.PhoneNumbers.Add(new PersonPhone()
                {
                    PhoneNumber = field,
                    PersonId = person.Id,
                    IsUnlisted = row.Field<string>("C_UNLIST") == "1" ? true : false,
                    PhoneType = "Mobile"
                });


            // Home Address
            field = row.Field<string>("ADDR1");
            if (field.IsNotNullOrWhitespace())
            {
                PersonAddress address = new PersonAddress()
                {
                    PersonId = person.Id,
                    AddressType = AddressType.Home,
                    Street1 = field,
                    Street2 = row.Field<string>("ADDR2"),
                    City = row.Field<string>("CITY"),
                    State = row.Field<string>("STATE"),
                    PostalCode = row.Field<string>("ZIP")
                };

                field = row.Field<string>("COUNTRY");
                if (field.IsNotNullOrWhitespace()) address.Country = field;

                person.Addresses.Add(address);
            }


            // Person Attributes
            field = row.Field<string>("ENV_NO");
            if (field.IsNotNullOrWhitespace())
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "core_GivingEnvelopeNumber",
                    AttributeValue = field,
                    PersonId = person.Id
                });

            field = row.Field<string>("EMPLOYER");
            if (field.IsNotNullOrWhitespace())
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "Employer",
                    AttributeValue = field,
                    PersonId = person.Id
                });

            field = row.Field<string>("JOB");
            if (field.IsNotNullOrWhitespace())
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "Position",
                    AttributeValue = field,
                    PersonId = person.Id
                });

            field = row.Field<string>("LAYMAN");
            if (field.IsNotNullOrWhitespace())
                person.Attributes.Add(new PersonAttributeValue
                {
                    AttributeKey = "KidsHopeMentor",
                    AttributeValue = field,
                    PersonId = person.Id
                });

            // Person User Defined Fields. These will be different for every church since they are user defined.
            string UDFcol;
            if (ServanrKeeperApi._PersonUDFColumns.TryGetValue(new Tuple<string, string>("First Visited", "date"), out UDFcol))
                if (DateTime.TryParseExact(row.Field<string>(UDFcol), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtvalue))
                    person.Attributes.Add(new PersonAttributeValue
                    {
                        AttributeKey = "FirstVisit",
                        AttributeValue = dtvalue.ToString("MM/dd/yyyy"),
                        PersonId = person.Id
                    });

            if (ServanrKeeperApi._PersonUDFColumns.TryGetValue(new Tuple<string, string>("Deceased Date", "date"), out UDFcol))
                if (DateTime.TryParseExact(row.Field<string>(UDFcol), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dtvalue))
                    person.Attributes.Add(new PersonAttributeValue
                    {
                        AttributeKey = "DateofDeath",
                        AttributeValue = dtvalue.ToString("MM/dd/yyyy"),
                        PersonId = person.Id
                    });


            return person;
        }
    }
}
