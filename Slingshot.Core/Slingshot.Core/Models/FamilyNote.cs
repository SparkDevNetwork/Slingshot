using Slingshot.Core.Data;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for FamilyNote
    /// </summary>
    public class FamilyNote : EntityNote, IImportModel
    {
        /// <summary>
        /// Gets or sets the family identifier.
        /// </summary>
        /// <value>
        /// The family identifier.
        /// </value>
        public int FamilyId { get; set; }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "family-note.csv";
        }
    }
}
