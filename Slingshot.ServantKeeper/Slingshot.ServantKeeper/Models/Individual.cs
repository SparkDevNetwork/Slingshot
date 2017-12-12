using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slingshot.ServantKeeper.Attributes;
using Slingshot.Core.Model;

namespace Slingshot.ServantKeeper.Models
{
    public class Individual
    {
        [ColumnName("IND_ID")]
        public long Id { get; set; }

        [ColumnName("FAMILY_ID")]
        public string FamilyId { get; set; }

        [ColumnName("TITLE")]
        public string Title { get; set; }

        [ColumnName("SUFFIX")]
        public string Suffix { get; set; }

        [ColumnName("FIRST_NAME")]
        public string FirstName { get; set; }

        [ColumnName("MID_NAME")]
        public string MID_NAME { get; set; }

        [ColumnName("LAST_NAME")]
        public string LastName { get; set; }

        [ColumnName("PREFERNAME")]
        public string NickName { get; set; }

        [ColumnName("ACTIVE_IND")]
        public string ActiveInd { get; set; }

        [ColumnName("SALUTATION")]
        public string Salutation { get; set; }

        [ColumnName("SEX")]
        [MapEnum("U,M,F")]
        public Gender Gender { get; set; }

        [ColumnName("BIRTH_DT")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime BirthDate { get; set; }

        [ColumnName("AGE")]
        public int Age { get; set; }

        [ColumnName("email")]
        public string Email { get; set; }

        [ColumnName("c_phone")]
        public string CellPhone { get; set; }

        [ColumnName("c_unlisted")]
        public bool CellPhoneUnlisted { get; set; }

        [ColumnName("h_phone")]
        public string HomePhone { get; set; }

        [ColumnName("h_unlisted")]
        public bool HomePhoneUnlisted { get; set; }

        public Person Person {
            get{
                Person person = new Person();
                person.Id = Math.Abs(unchecked((int)Id));
                person.FirstName = FirstName;
                person.NickName = NickName;
                person.LastName = LastName;
                person.Birthdate = BirthDate;
                person.Email = Email;
                if (!String.IsNullOrEmpty(CellPhone))
                {
                    PersonPhone phone = new PersonPhone();
                    phone.PhoneNumber = CellPhone;
                    phone.PersonId = Math.Abs(unchecked((int)Id));
                    phone.IsUnlisted = CellPhoneUnlisted;
                    phone.PhoneType = "Cell";
                    person.PhoneNumbers.Add(phone);
                }
                if (!String.IsNullOrEmpty(HomePhone))
                {
                    PersonPhone phone = new PersonPhone();
                    phone.PhoneNumber = HomePhone;
                    phone.PersonId = Math.Abs(unchecked((int)Id));
                    phone.IsUnlisted = HomePhoneUnlisted;
                    phone.PhoneType = "Home";
                    person.PhoneNumbers.Add(phone);
                }
                return person;

            }
        }
    }
}
