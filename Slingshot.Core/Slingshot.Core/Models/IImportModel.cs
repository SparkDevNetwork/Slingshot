namespace Slingshot.Core.Model
{
    /// <summary>
    /// Interface for import models. These will turned into CSV files and stored in the *.slingshot file, which will then be handed off to Rock to get imported
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