using Slingshot.Core.Model;
using Slingshot.ServantKeeper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    class SKPerson
    {
        public static Person Translate(Individual individual, List<Value> values, List<Family> families, List<Field> tableFields, List<Label> labels)
        {
            Person person = new Person();
            person.Id = Math.Abs(unchecked((int)individual.Id));

            // Import the family information
            Family family = families.Where(v => v.Id == individual.FamilyId).FirstOrDefault();
            person.FamilyId = Math.Abs(unchecked((int)individual.FamilyId));
            person.FamilyName = family.FamilyName;
            person.FamilyRole = individual.Age < 18 ? FamilyRole.Child : FamilyRole.Adult;

            // Import the basic stuff
            person.Gender = (Core.Model.Gender)individual.Gender;
            person.RecordStatus = individual.RecordStatus;
            Value memberStatus = values.Where(v => v.Id == individual.MemberStatus).FirstOrDefault();
            if (memberStatus != null)
            {
                person.ConnectionStatus = memberStatus.Description;
                if (memberStatus.Description == "Deceased")
                {
                    person.IsDeceased = true;
                    person.RecordStatus = RecordStatus.Inactive;
                    person.InactiveReason = memberStatus.Description;
                }
            }
            else
            {
                person.ConnectionStatus = "Unknown";
            }

            person.CreatedDateTime = individual.CreateDate > family.CreateDate ? individual.CreateDate : family.CreateDate;
            person.ModifiedDateTime = individual.UpdateDate > family.UpdateDate ? individual.UpdateDate : family.UpdateDate;

            person.FirstName = individual.FirstName;
            person.MiddleName = individual.MiddleName;
            person.LastName = individual.LastName;
            person.NickName = individual.NickName;
            person.Salutation = individual.Salutation;
            person.Suffix = individual.Suffix;

            person.Birthdate = individual.BirthDate;
            person.AnniversaryDate = individual.WeddingDate;

            if (individual.MaritalCode > 0)
            {
                switch(values.Where(v => v.Id == individual.MaritalCode).Select(v => v.Description).FirstOrDefault())
                {
                    case "Married":
                        person.MaritalStatus = MaritalStatus.Married;
                        break;
                    case "Single Dad":
                    case "Single Mom":
                    case "Single":
                        person.MaritalStatus = MaritalStatus.Single;
                        break;
                    case "Divorced":
                        person.MaritalStatus = MaritalStatus.Divorced;
                        break;
                    default:
                        person.MaritalStatus = MaritalStatus.Unknown;
                        break;
                }
            } else
            {
                if (individual.Age < 18)
                {
                    person.MaritalStatus = MaritalStatus.Single;
                }
                else
                {
                    person.MaritalStatus = MaritalStatus.Unknown;

                }
            }

            person.Email = individual.Email;
            person.EmailPreference = individual.EmailIndicator ? EmailPreference.NoMassEmails : EmailPreference.EmailAllowed;

            Regex digitsOnly = new Regex(@"[^\d]");
            if (!String.IsNullOrEmpty(individual.CellPhone))
            {
                PersonPhone phone = new PersonPhone();
                phone.PhoneNumber = digitsOnly.Replace(individual.CellPhone, "");
                phone.PersonId = Math.Abs(unchecked((int)individual.Id));
                phone.IsUnlisted = individual.CellPhoneUnlisted;
                phone.PhoneType = "Cell";
                person.PhoneNumbers.Add(phone);
            }
            if (!String.IsNullOrEmpty(individual.HomePhone))
            {
                PersonPhone phone = new PersonPhone();
                phone.PhoneNumber = digitsOnly.Replace(individual.HomePhone, "");
                phone.PersonId = Math.Abs(unchecked((int)individual.Id));
                phone.IsUnlisted = individual.HomePhoneUnlisted;
                phone.PhoneType = "Home";
                person.PhoneNumbers.Add(phone);
            }
            if (!String.IsNullOrEmpty(individual.WorkPhone))
            {
                PersonPhone phone = new PersonPhone();
                phone.PhoneNumber = digitsOnly.Replace(individual.WorkPhone, "");
                phone.PersonId = Math.Abs(unchecked((int)individual.Id));
                phone.IsUnlisted = individual.WorkPhoneUnlisted;
                phone.PhoneType = "Work";
                person.PhoneNumbers.Add(phone);
            }

            // Now export their address
            PersonAddress address = new PersonAddress();
            address.PersonId = Math.Abs(unchecked((int)individual.Id));
            address.AddressType = AddressType.Home;
            address.Street1 = family.Address1;
            address.Street2 = family.Address2;
            address.City = family.City;
            address.State = family.State;
            address.PostalCode = family.Zip;
            person.Addresses.Add(address);

            // Handle the User Defined Fields
            Value udf11 = values.Where(v => v.Id == individual.UserDefinedField11).FirstOrDefault();
            if (udf11 != null)
            {
                PersonAttributeValue pav = new PersonAttributeValue();
                pav.AttributeValue = udf11.Description;
                pav.PersonId = Math.Abs(unchecked((int)individual.Id));
                // Lookup the key from the table fields
                int labelId = tableFields.Where(tf => tf.Key == udf11.Name).Select(tf => tf.LabelId).FirstOrDefault();
                pav.AttributeKey = labels.Where(l => l.LabelId == labelId).Select(l => l.Description).FirstOrDefault();
                person.Attributes.Add(pav);
            }


            return person;
            
        }
    }
}
