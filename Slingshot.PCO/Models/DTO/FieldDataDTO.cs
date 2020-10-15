using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class FieldDataDTO
    {
        public int Id { get; set; }

        public string FileUrl { get; set; }

        public string FileContentType { get; set; }

        public string FileName { get; set; }

        public int? FileSize { get; set; }

        public string Value { get; set; }

        public int FieldDefinitionId { get; set; }

        public FieldDataDTO( DataItem data )
        {
            Id = data.Id;
            FileUrl = data.Item.file.url;
            FileContentType = data.Item.file_content_type;
            FileName = data.Item.file_name;
            FileSize = data.Item.file_size;
            Value = data.Item.value;

            SetFieldDefinitionId( data );
        }

        private void SetFieldDefinitionId( DataItem data )
        {
            if ( data.Relationships == null || data.Relationships.FieldDefinition == null )
            {
                return;
            }

            foreach ( var relationship in data.Relationships.FieldDefinition.Data )
            {
                FieldDefinitionId = relationship.Id;
            }
        }
    }
}
