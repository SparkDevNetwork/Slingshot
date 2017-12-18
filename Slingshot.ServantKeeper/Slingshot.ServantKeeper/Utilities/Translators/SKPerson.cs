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
            person.Campus = new Campus() {  CampusId = 0, CampusName = "Main Campus"};

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
                phone.PersonId = person.Id;
                phone.IsUnlisted = individual.CellPhoneUnlisted;
                phone.PhoneType = "Cell";
                person.PhoneNumbers.Add(phone);
            }
            if (!String.IsNullOrEmpty(individual.HomePhone))
            {
                PersonPhone phone = new PersonPhone();
                phone.PhoneNumber = digitsOnly.Replace(individual.HomePhone, "");
                phone.PersonId = person.Id;
                phone.IsUnlisted = individual.HomePhoneUnlisted;
                phone.PhoneType = "Home";
                person.PhoneNumbers.Add(phone);
            }
            if (!String.IsNullOrEmpty(individual.WorkPhone))
            {
                PersonPhone phone = new PersonPhone();
                phone.PhoneNumber = digitsOnly.Replace(individual.WorkPhone, "");
                phone.PersonId = person.Id;
                phone.IsUnlisted = individual.WorkPhoneUnlisted;
                phone.PhoneType = "Work";
                person.PhoneNumbers.Add(phone);
            }

            // Now export their address
            PersonAddress address = new PersonAddress();
            address.PersonId = person.Id;
            address.AddressType = AddressType.Home;
            address.Street1 = family.Address1;
            address.Street2 = family.Address2;
            address.City = family.City;
            address.State = family.State;
            address.PostalCode = family.Zip;
            person.Addresses.Add(address);

            // Handle the User Defined Fields
            var properties = individual.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.Name.ToLower().Contains("udf"))
                {
                    var attribute = property.CustomAttributes.Where(ca => ca.AttributeType.Name == "ColumnName").FirstOrDefault();
                    
                    if (attribute != null)
                    {
                        var fieldKey = ((string)attribute.ConstructorArguments.FirstOrDefault().Value).ToLower();
                        var tableField = tableFields.Where(tf => tf.Name.ToLower().Contains(fieldKey)).FirstOrDefault();

                        if (tableField != null)
                        {
                            PersonAttributeValue pav = new PersonAttributeValue();
                            // If this is a string
                            if (property.PropertyType == typeof(string) && !string.IsNullOrWhiteSpace((string)property.GetValue(individual)))
                            {
                                pav.AttributeValue = (string)property.GetValue(individual);
                            }
                            // If this is a long (lookup value)
                            if (property.PropertyType == typeof(long))
                            {
                                pav.AttributeValue = values.Where(v => v.Id == (long)property.GetValue(individual)).Select(i => i.Description).FirstOrDefault();
                            }
                            // If this is a date
                            if (property.PropertyType == typeof(DateTime) && ((DateTime)property.GetValue(individual)).Year > 1)
                            {
                                pav.AttributeValue = (string)property.GetValue(individual);
                            }
                            // If this is a boolean
                            if (property.PropertyType == typeof(bool))
                            {
                                pav.AttributeValue = (bool)property.GetValue(individual) ? "True" : "False";
                            }
                            pav.PersonId = person.Id;
                            // Lookup the key from the table fields
                            pav.AttributeKey = labels.Where(l => l.LabelId == tableField.LabelId).Select(l => l.Description).DefaultIfEmpty(tableField.Description).FirstOrDefault().Replace(" ", string.Empty);
                            person.Attributes.Add(pav);
                        }
                    }
                }
            }

            person.Note = individual.Note;

            if (individual.JoinDate.DayOfYear > 1)
            {
                PersonAttributeValue pav = new PersonAttributeValue();
                pav.AttributeValue = individual.JoinDate.ToString();
                pav.PersonId = person.Id;
                pav.AttributeKey = "JoinDate";
                person.Attributes.Add(pav);
            }

            if (individual.HowJoined > 1)
            {
                PersonAttributeValue pav = new PersonAttributeValue();
                pav.AttributeValue = values.Where(v => v.Id == individual.HowJoined).Select(v => v.Description).FirstOrDefault();
                pav.PersonId = person.Id;
                pav.AttributeKey = "HowJoined";
                person.Attributes.Add(pav);
            }

            if (individual.BaptizedDate.Year > 1)
            {
                PersonAttributeValue pav = new PersonAttributeValue();
                pav.AttributeValue = individual.BaptizedDate.ToString();
                pav.PersonId = person.Id;
                pav.AttributeKey = "BaptizedDate";
                person.Attributes.Add(pav);
            }

            { 
                PersonAttributeValue pav = new PersonAttributeValue();
                pav.AttributeValue = individual.Baptized?"True":"False";
                pav.PersonId = person.Id;
                pav.AttributeKey = "Baptized";
                person.Attributes.Add(pav);
            }

            if (individual.JobCode > 1)
            {
                PersonAttributeValue pav = new PersonAttributeValue();
                pav.AttributeValue = values.Where(v => v.Id == individual.JobCode).Select(v => v.Description).FirstOrDefault();
                pav.PersonId = person.Id;
                pav.AttributeKey = "Occupation";
                person.Attributes.Add(pav);
            }

            if (!string.IsNullOrWhiteSpace(individual.Employer))
            {
                PersonAttributeValue pav = new PersonAttributeValue();
                pav.AttributeValue = individual.Employer;
                pav.PersonId = person.Id;
                pav.AttributeKey = "Employer";
                person.Attributes.Add(pav);
            }

            if (!string.IsNullOrWhiteSpace(individual.SundaySchool))
            {
                PersonAttributeValue pav = new PersonAttributeValue();
                pav.AttributeValue = individual.SundaySchool;
                pav.PersonId = person.Id;
                pav.AttributeKey = "SundaySchool";
                person.Attributes.Add(pav);
            }

            return person;
            
        }
    }
}
