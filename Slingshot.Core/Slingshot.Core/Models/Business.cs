using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Slingshot.Core.Model
{
    public class Business : IImportModel
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the Business.
        /// </summary>
        /// <value>
        /// The name of the Business.
        /// </value>
        public string Name { get; set; }


        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the record status.
        /// </summary>
        /// <value>
        /// The record status.
        /// </value>
        public RecordStatus RecordStatus { get; set; }

        /// <summary>
        /// Gets or sets the inactive reason.
        /// </summary>
        /// <value>
        /// The inactive reason.
        /// </value>
        public string InactiveReason { get; set; }

  
        /// <summary>
        /// Gets or sets the email preference.
        /// </summary>
        /// <value>
        /// The email preference.
        /// </value>
        public EmailPreference EmailPreference { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        /// <value>
        /// The created date time.
        /// </value>
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the modified date time.
        /// </summary>
        /// <value>
        /// The modified date time.
        /// </value>
        public DateTime? ModifiedDateTime { get; set; }


        /// <summary>
        /// Gets or sets the campus.
        /// </summary>
        /// <value>
        /// The campus.
        /// </value>
        public Campus Campus { get; set; } = new Campus();

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        /// <value>
        /// The note.
        /// </value>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public List<BusinessAttribute> Attributes { get; set; } = new List<BusinessAttribute>();

        /// <summary>
        /// Gets or sets the phone numbers.
        /// </summary>
        /// <value>
        /// The phone numbers.
        /// </value>
        public List<BusinessPhone> PhoneNumbers { get; set; } = new List<BusinessPhone>();

        /// <summary>
        /// Gets or sets the addresses.
        /// </summary>
        /// <value>
        /// The addresses.
        /// </value>
        public List<BusinessAddress> Addresses { get; set; } = new List<BusinessAddress>();

        /// <summary>
        /// Gets or sets the business Contacts.
        /// </summary>
        /// <value>
        /// The business contacts.
        /// </value>
        public List<BusinessContact> Contacts { get; set; } = new List<BusinessContact>();

        public string GetFileName()
        {
            return "business.csv";
        }
    }
}
