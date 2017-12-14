using Slingshot.Core.Model;
using Slingshot.ServantKeeper.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slingshot.ServantKeeper.Utilities.Translators
{
    class SKPerson
    {
        public static Person Translate(Individual individual, List<Value> values)
        {
            Person person = new Person();
            person.Id = Math.Abs(unchecked((int)individual.Id));
            person.RecordStatus = individual.RecordStatus;
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
            if (!String.IsNullOrEmpty(individual.CellPhone))
            {
                PersonPhone phone = new PersonPhone();
                phone.PhoneNumber = individual.CellPhone;
                phone.PersonId = Math.Abs(unchecked((int)individual.Id));
                phone.IsUnlisted = individual.CellPhoneUnlisted;
                phone.PhoneType = "Cell";
                person.PhoneNumbers.Add(phone);
            }
            if (!String.IsNullOrEmpty(individual.HomePhone))
            {
                PersonPhone phone = new PersonPhone();
                phone.PhoneNumber = individual.HomePhone;
                phone.PersonId = Math.Abs(unchecked((int)individual.Id));
                phone.IsUnlisted = individual.HomePhoneUnlisted;
                phone.PhoneType = "Home";
                person.PhoneNumbers.Add(phone);
            }
            return person;
            
        }
    }
}
