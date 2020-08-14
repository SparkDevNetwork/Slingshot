using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Slingshot.F1.Utilities.SQL.DTO
{

    /*
     * 8/12/20 - Shaun
     * 
     * These DTOs are used within F1Sql.ExportMethods.cs to accomplish translation tasks
     * that were previously handled within Access SQL queries of the F1Mdb exporter.
     * 
     * */

    /// <summary>
    /// The Group Member DTO.  Implements IEquatable for <see cref="Enumerable.Distinct{TSource}(IEnumerable{TSource})"/>.
    /// </summary>
    public class GroupMemberDTO : IEquatable<GroupMemberDTO>
    {

        #region Public Properties

        /// <summary>
        /// The Individual Id.
        /// </summary>
        public int IndividualId;

        /// <summary>
        /// The Group Id.
        /// </summary>
        public int? GroupId;

        /// <summary>
        /// The Group Member Type.
        /// </summary>
        public string GroupMemberType;

        /// <summary>
        /// The Breakout Group Name.
        /// </summary>
        public string BreakoutGroupName;

        /// <summary>
        /// The Parent Group Id.
        /// </summary>
        public int? ParentGroupId;

        /// <summary>
        /// <see cref="MD5"/> object used to create a new GroupId value if one is not supplied by F1.
        /// </summary>
        public MD5 GroupIdHasher
        {
            set
            {
                if ( GroupId == null )
                {
                    var hashed = value.ComputeHash( Encoding.UTF8.GetBytes( this.BreakoutGroupName + this.ParentGroupId ) );
                    GroupId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                }
            }
        }

        #endregion Public Properties

        #region IEquatable Implementation

        /// <summary>
        /// Tests equality.
        /// </summary>
        /// <param name="other">The other <see cref="GroupMemberDTO"/>.</param>
        /// <returns></returns>
        public bool Equals( GroupMemberDTO other )
        {

            //Check whether the compared object is null.
            if ( Object.ReferenceEquals( other, null ) )
			{
                return false;
			}

            //Check whether the compared object references the same data.
            if ( Object.ReferenceEquals( this, other ) )
            {
                return true;
            }

            //Check whether the objects' properties are equal.
            return
				IndividualId.Equals( other.IndividualId ) &&
				GroupId.Equals( other.GroupId ) &&
				GroupMemberType.Equals( other.GroupMemberType );
        }

        /// <summary>
        /// Generates the hash code (which is used by LINQ to test uniqueness of the object).
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            //Get hash code for the IndividualId field.
            int hashIndividualId = IndividualId.GetHashCode();

            //Get hash code for the GroupId field if it is not null.
            int hashGroupId = GroupId == null ? 0 : GroupId.GetHashCode();

            //Get hash code for the GroupMemberType field.
            int hashGroupMemberType = GroupMemberType.GetHashCode();

			//Calculate the hash code for the object.
            return hashIndividualId ^ hashGroupId ^ hashGroupMemberType;
        }

        #endregion IEquatable Implementation

    }

    /// <summary>
    /// The Attendance DTO.  Implements IEquatable for <see cref="Enumerable.Distinct{TSource}(IEnumerable{TSource})"/>.
    /// </summary>
    public class AttendanceDTO : IEquatable<AttendanceDTO>
    {
        /// <summary>
        /// The Individual Id.
        /// </summary>
        public int IndividualId;

        /// <summary>
        /// The Attendance Id.
        /// </summary>
        public int? AttendanceId;

        /// <summary>
        /// The Group Id.
        /// </summary>
        public int? GroupId;

        /// <summary>
        /// The Start Date Time.
        /// </summary>
        public DateTime StartDateTime;

        /// <summary>
        /// The End Date Time.
        /// </summary>
        public DateTime? EndDateTime;

        /// <summary>
        /// Checked In As (for individual attendance records).
        /// </summary>
        public string CheckedInAs;

        /// <summary>
        /// Job Title (for individual attendance records).
        /// </summary>
        public string JobTitle;

        /// <summary>
        /// Comments (for group attendance records).
        /// </summary>
        public string Comments;

        /// <summary>
        /// Calculates the Note value based on other inputs (CheckedInAs and JobTitle, or Comments).
        /// </summary>
        public string Note
        {
            get
            {
                if ( !string.IsNullOrEmpty( Comments ) )
                {
                    return Comments;
                }
                else if ( !string.IsNullOrEmpty( CheckedInAs ) )
                {
                    if ( !string.IsNullOrEmpty( JobTitle ) )
                    {
                        return $"Checked in as {CheckedInAs} ({JobTitle})";
                    }
                    return $"Checked in as {CheckedInAs}";
                }

                return "";
            }
        }

        #region IEquatable Implementation

        /// <summary>
        /// Tests equality.
        /// </summary>
        /// <param name="other">The other <see cref="AttendanceDTO"/>.</param>
        /// <returns></returns>
        public bool Equals( AttendanceDTO other )
        {
            //Check whether the compared object is null.
            if ( Object.ReferenceEquals( other, null ) )
			{
                return false;
			}

            //Check whether the compared object references the same data.
            if ( Object.ReferenceEquals( this, other ) )
            {
                return true;
        }

            //Check whether the objects' properties are equal.
            return
				IndividualId.Equals( other.IndividualId ) &&
                AttendanceId.Equals( other.AttendanceId ) && 
				GroupId.Equals( other.GroupId ) &&
                StartDateTime.Equals( other.StartDateTime ) &&
                EndDateTime.Equals( other.EndDateTime ) &&
                Note.Equals( other.Note );
        }

        /// <summary>
        /// Generates the hash code (which is used by LINQ to test uniqueness of the object).
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            //Get hash code for the IndividualId field.
            int hashIndividualId = IndividualId.GetHashCode();

            //Get hash code for the AttendanceId field.
            int hashAttendanceId = AttendanceId == null ? 0 : AttendanceId.GetHashCode();

            //Get hash code for the GroupId field if it is not null.
            int hashGroupId = GroupId == null ? 0 : GroupId.GetHashCode();

            //Get hash code for the StartDateTime field.
            int hashStartDateTime = StartDateTime.GetHashCode();

            //Get hash code for the StartDateTime field.
            int hashEndDateTime = EndDateTime == null ? 0 : EndDateTime.GetHashCode();

            //Get hash code for the Note field.
            int hashNote = Note.GetHashCode();

            //Calculate the hash code for the object.
            return hashIndividualId ^ hashAttendanceId ^ hashGroupId ^ hashStartDateTime ^ hashEndDateTime & hashNote;
        }

        #endregion IEquatable Implementation

    }
    
    /// <summary>
    /// The Batch DTO.
    /// </summary>
    public class BatchDTO
    {
        /// <summary>
        /// The Batch Id.
        /// </summary>
        public int BatchId;

        /// <summary>
        /// The Batch Name.
        /// </summary>
        public string BatchName;

        /// <summary>
        /// The Batch Date.
        /// </summary>
        public DateTime BatchDate;

        /// <summary>
        /// The Batch Amount.
        /// </summary>
        public decimal BatchAmount;
    }

    /// <summary>
    /// The Group DTO.
    /// </summary>
    public class GroupDTO
    {
        /// <summary>
        /// The Group Name.
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// The Group Id.
        /// </summary>
        public int? GroupId { get; set; }

        /// <summary>
        /// The Group Type Id.
        /// </summary>
        public int GroupTypeId { get; set; }

        /// <summary>
        /// The Description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Is Active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// The Start Date.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Is Public.
        /// </summary>
        public bool? IsPublic { get; set; }

        /// <summary>
        /// The Location Name.
        /// </summary>
        public string LocationName { get; set; }

        /// <summary>
        /// The Schedule Day.
        /// </summary>
        public string ScheduleDay { get; set; }

        /// <summary>
        /// The Start Hour.
        /// </summary>
        public string StartHour { get; set; }

        /// <summary>
        /// Address Line 1.
        /// </summary>
        public string Address1 { get; set; }

        /// <summary>
        /// Address Line 2.
        /// </summary>
        public string Address2 { get; set; }

        /// <summary>
        /// City.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// State/Province.
        /// </summary>
        public string StateProvince { get; set; }

        /// <summary>
        /// Postal/Zip Code.
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Country.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// The Parent Group Id.
        /// </summary>
        public int? ParentGroupId { get; set; }
    }

}
