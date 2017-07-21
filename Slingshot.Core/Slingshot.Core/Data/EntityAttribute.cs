namespace Slingshot.Core.Data
{
    public abstract class EntityAttribute
    {
        /// <summary>
        /// Gets or sets the attribute key.
        /// </summary>
        /// <value>
        /// The attribute key.
        /// </value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the name of the attribute.
        /// </summary>
        /// <value>
        /// The name of the attribute.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the attribute field.
        /// </summary>
        /// <value>
        /// The type of the attribute field.
        /// </value>
        public string FieldType { get; set; }

        /// <summary>
        /// Gets or sets the attribute category.
        /// </summary>
        /// <value>
        /// The attribute category.
        /// </value>
        public string Category { get; set; }
    }
}
