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

        [ColumnName("family_id")]
        public long FamilyId { get; set; }

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
        public DateTime? BirthDate { get; set; }
        
        public int Age {
            get {
                int age = 999;
                if (BirthDate.HasValue) { 
                    DateTime today = DateTime.Today;
                    age = today.Year - BirthDate.Value.Year;
                    if (BirthDate > today.AddYears(-age))
                        age--;
                }
                return age;
            }
        }

        [ColumnName("email1")]
        public string Email { get; set; }

        [ColumnName("email1_ind")]
        public bool EmailIndicator { get; set; }

        [ColumnName("c_phone")]
        public string CellPhone { get; set; }

        [ColumnName("c_unlisted")]
        public bool CellPhoneUnlisted { get; set; }

        [ColumnName("h_phone")]
        public string HomePhone { get; set; }

        [ColumnName("h_unlisted")]
        public bool HomePhoneUnlisted { get; set; }

        [ColumnName("w_phone")]
        public string WorkPhone { get; set; }

        [ColumnName("w_unlisted")]
        public bool WorkPhoneUnlisted { get; set; }

        [ColumnName("marital_cd")]
        public long MaritalCode { get; set; }
        
        [ColumnName("wedding_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? WeddingDate { get; set; }

        [ColumnName("mem_status")]
        public long MemberStatus { get; set; }

        [ColumnName("note")]
        public String Note { get; set; }

        [ColumnName("join_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? JoinDate { get; set; }

        [ColumnName("how_join")]
        public long HowJoined { get; set; }

        [ColumnName("sunschool")]
        public string SundaySchool { get; set; }

        [ColumnName("baptized")]
        public bool Baptized { get; set; }

        [ColumnName("baptize_dt")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? BaptizedDate { get; set; }
        
        [ColumnName("job_cd")]
        public long JobCode { get; set; }

        [ColumnName("employer")]
        public string Employer { get; set; }

        [ColumnName("udf1")]
        public string Udf1 { get; set; }

        [ColumnName("udf2")]
        public string Udf2 { get; set; }

        [ColumnName("udf3")]
        public string Udf3 { get; set; }

        [ColumnName("udf4")]
        public string Udf4 { get; set; }

        [ColumnName("udf5")]
        public string Udf5 { get; set; }

        [ColumnName("udf6")]
        public string Udf6 { get; set; }

        [ColumnName("udf7")]
        public string Udf7 { get; set; }

        [ColumnName("udf8")]
        public string Udf8 { get; set; }

        [ColumnName("udf9")]
        public string Udf9 { get; set; }

        [ColumnName("udf10")]
        public string Udf10 { get; set; }

        [ColumnName("udf11")]
        public long Udf11 { get; set; }

        [ColumnName("udf12")]
        public string Udf12 { get; set; }

        [ColumnName("udf13")]
        public string Udf13 { get; set; }

        [ColumnName("udf14")]
        public string Udf14 { get; set; }

        [ColumnName("udf15")]
        public string Udf15 { get; set; }

        [ColumnName("udf16")]
        public string Udf16 { get; set; }

        [ColumnName("udf_dt1")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate1 { get; set; }

        [ColumnName("udf_dt2")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate2 { get; set; }
   
        [ColumnName("udf_dt3")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate3 { get; set; }

        [ColumnName("udf_dt4")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate4 { get; set; }

        [ColumnName("udf_dt5")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate5 { get; set; }

        [ColumnName("udf_dt6")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate6 { get; set; }

        [ColumnName("udf_dt7")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate7 { get; set; }

        [ColumnName("udf_dt8")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate8 { get; set; }

        [ColumnName("udf_dt9")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate9 { get; set; }

        [ColumnName("udf_dt10")]
        [DateTimeParseString("yyyyMMdd")]
        public DateTime? UdfDate10 { get; set; }

        [ColumnName("udf_check1")]
        public bool UdfCheck1 { get; set; }

        [ColumnName("udf_check2")]
        public bool UdfCheck2 { get; set; }

        [ColumnName("udf_check3")]
        public bool UdfCheck3 { get; set; }

        [ColumnName("udf_check4")]
        public bool UdfCheck4 { get; set; }

        [ColumnName("udf_check5")]
        public bool UdfCheck5 { get; set; }

        [ColumnName("udf_check6")]
        public bool UdfCheck6 { get; set; }

        [ColumnName("udf_check7")]
        public bool UdfCheck7 { get; set; }

        [ColumnName("udf_check8")]
        public bool UdfCheck8 { get; set; }

        [ColumnName("udf_check9")]
        public bool UdfCheck9 { get; set; }

        [ColumnName("udf_chk10")]
        public bool UdfCheck10 { get; set; }

        public RecordStatus RecordStatus { get; set; }

        [ColumnName("create_ts")]
        [DateTimeParseString("yyyyMMddhhmmss")]
        public DateTime? CreateDate { get; set; }

        [ColumnName("update_ts")]
        [DateTimeParseString("yyyyMMddhhmmss")]
        public DateTime? UpdateDate { get; set; }
    }
}
