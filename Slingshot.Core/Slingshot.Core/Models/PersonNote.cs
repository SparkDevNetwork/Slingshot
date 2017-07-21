using Slingshot.Core.Data;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for PersonNote
    /// </summary>
    public class PersonNote : EntityNote, IImportModel
    {
        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public int PersonId { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "person-note.csv";
        }
    }
}
