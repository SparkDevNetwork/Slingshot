using Slingshot.Core.Data;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for PersonAttribute
    /// </summary>
    public class BusinessAttribute: EntityAttribute, IImportModel
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "business-attribute.csv";
        }
    }
}
