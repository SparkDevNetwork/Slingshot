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
        public string MiddleName { get; set; }

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
        
        public int Age {
            get {
                DateTime today = DateTime.Today;
                int age = today.Year - BirthDate.Year;
                if (BirthDate > today.AddYears(-age))
                    age--;

                return age;
            }
        }

        [ColumnName("email1")]
        public string Email { get; set; }

        [ColumnName("c_phone")]
        public string CellPhone { get; set; }

        [ColumnName("c_unlisted")]
        public bool CellPhoneUnlisted { get; set; }

        [ColumnName("h_phone")]
        public string HomePhone { get; set; }

        [ColumnName("h_unlisted")]
        public bool HomePhoneUnlisted { get; set; }

        [ColumnName("marital_cd")]
        public long MaritalCode { get; set; }
        
        [ColumnName("wedding_dt")]
        public DateTime WeddingDate { get; set; }

        public RecordStatus RecordStatus { get; set; }
    }
}
