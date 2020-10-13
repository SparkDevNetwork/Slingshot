using Slingshot.PCO.Models.ApiModels;
using System.Collections.Generic;

namespace Slingshot.PCO.Utilities
{
    /// <summary>
    /// Represents the result of a query from the PCO API.
    /// </summary>
    public class PCOApiQueryResult
    {
        /// <summary>
        /// The Items included in this query.
        /// </summary>
        public List<DataItem> Items { get; set; }

        /// <summary>
        /// Related items specified in the "include" option of the query.
        /// </summary>
        public Dictionary<string, DataItem> IncludedItems { get; }

        /// <summary>
        /// Constructor for new query.
        /// </summary>
        /// <param name="newItems"></param>
        public PCOApiQueryResult(List<DataItem> newItems)
        {
            Items = new List<DataItem>();
            IncludedItems = new Dictionary<string, DataItem>();
            AddIncludedItems(newItems);
        }

        /// <summary>
        /// Sorts the included items by key and adds them to the dictionary.
        /// </summary>
        /// <param name="newItems">The list of included items.</param>
        private void AddIncludedItems( List<DataItem> newItems )
        {
            foreach ( var includedItem in newItems )
            {
                string key = $"{includedItem.Type}:{includedItem.Id}";
                IncludedItems.Add(key, includedItem);
            }
        }

        /// <summary>
        /// Constructor for a new page to an existing paged query.
        /// </summary>
        /// <param name="newItems"></param>
        public PCOApiQueryResult( PCOApiQueryResult existingResults, List<DataItem> includedItems )
        {
            Items = existingResults.Items;
            IncludedItems = existingResults.IncludedItems;
            MergeIncludedItems( includedItems );
        }

        /// <summary>
        /// Sorts the included items by key and adds them to the dictionary if they are not already there.
        /// </summary>
        /// <param name="newItems">The list of included items.</param>
        private void MergeIncludedItems( List<DataItem> newItems )
        {
            foreach ( var includedItem in newItems )
            {
                string key = $"{includedItem.Type}:{includedItem.Id}";

                if ( IncludedItems.ContainsKey( key ) )
                {
                    continue;
                }

                IncludedItems.Add( key, includedItem );
            }
        }
    }

    /// <summary>
    /// PCO API Query Result-related Extension Methods.
    /// </summary>
    internal static class PCOApiQueryResultExtensions
    {
        /// <summary>
        /// Matches a DataItem from the relationship by the key (Type:Id).
        /// </summary>
        /// <param name="included">The dictionary of <see cref="DataItem"/>s included in the API query.</param>
        /// <param name="relationship">The relationship <see cref="DataItem"/>.</param>
        /// <returns></returns>
        public static DataItem LocateItem( this Dictionary<string, DataItem> included, DataItem relationship )
        {
            if ( relationship == null )
            {
                // Nothing to match.
                return null;
            }

            return included.LocateItem( relationship.Type, relationship.Id );
        }

        /// <summary>
        /// Matches a DataItem directly by the key (Type:Id).
        /// </summary>
        /// <param name="included">The dictionary of <see cref="DataItem"/>s included in the API query.</param>
        /// <param name="relationship">The relationship <see cref="DataItem"/>.</param>
        /// <returns></returns>
        public static DataItem LocateItem( this Dictionary<string, DataItem> included, string type, int id )
        {
            string itemKey = $"{type}:{id}";
            if ( !included.ContainsKey( itemKey ) )
            {
                // Item isn't in the collection.
                return null;
            }

            return included[itemKey];
        }
    }
}
