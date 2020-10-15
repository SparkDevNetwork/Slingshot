using Slingshot.PCO.Models.ApiModels;
using Slingshot.PCO.Utilities;
using System;
using System.Collections.Generic;

namespace Slingshot.PCO.Models.DTO
{
    public class NoteDTO
    {
        public int Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public int? CreatedById { get; set; }

        public string Note { get; set; }

        public int? NoteCategoryId { get; set; }

        public int? PersonId { get; set; }

        public NoteCategoryDTO NoteCategory { get; set; }

        public NoteDTO( DataItem data, Dictionary<string, DataItem> includedItems )
        {
            Id = data.Id;
            CreatedAt = data.Item.created_at;
            CreatedById = data.Item.created_by_id;
            Note = data.Item.note;
            NoteCategoryId = data.Item.note_category_id;
            PersonId = data.Item.person_id;

            SetCategory( data, includedItems );
        }

        private void SetCategory( DataItem data, Dictionary<string, DataItem> included )
        {
            if ( data.Relationships == null || data.Relationships.NoteCategory == null )
            {
                return;
            }
            foreach ( var relationship in data.Relationships.NoteCategory.Data )
            {
                var item = included.LocateItem( relationship );

                if ( item == null )
                {
                    continue;
                }

                NoteCategory = new NoteCategoryDTO( item );
            }
        }
    }
}
