using Slingshot.Core.Data;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// ImportModel for FamilyAttribute
    /// </summary>
    public class FamilyAttribute: EntityAttribute, IImportModel
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "family-attribute.csv";
        }
    }
}
