namespace Slingshot.Core.Model
{
    /// <summary>
    /// Interface for import models
    /// </summary>
    public interface IImportModel
    {
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns></returns>
        string GetFileName();
    }
}