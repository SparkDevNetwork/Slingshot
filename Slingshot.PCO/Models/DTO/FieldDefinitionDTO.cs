using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Models.DTO
{
    public class FieldDefinitionDTO
    {
        public int Id { get; set; }

        public string DataType { get; set; }

        public string Name { get; set; }

        public int Sequence { get; set; }

        public string Slug { get; set; }

        public string Config { get; set; }

        public int TabId { get; set; }

        public DateTime? DeletedAt { get; set; }

        public TabDTO Tab { get; set; }

        public FieldDefinitionDTO( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            Id = data.Id;
            DataType = data.Item.data_type;
            Name = data.Item.name;
            Sequence = data.Item.sequence;
            Slug = data.Item.slug;
            Config = data.Item.config;
            DeletedAt = data.Item.deteled_at;
            TabId = data.Item.tab_id;

            SetTab( data, includedItems );
        }

        private void SetTab( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            int tabId = data.Item.tab_id;
            var tab = includedItems.LocateItem( "Tab", tabId );

            if ( tab == null )
            {
                return;
            }

            Tab = new TabDTO( tab );
        }
    }
}
