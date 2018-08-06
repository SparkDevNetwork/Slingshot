using Slingshot.Core.Data;

namespace Slingshot.Core.Model
{
    /// <summary>
    /// Model for GroupAttribute
    /// </summary>
    public class GroupAttribute : EntityAttribute, IImportModel
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            return "group-attribute.csv";
        }
    }
}