using Slingshot.PCO.Models.ApiModels;
using System.Collections.Generic;

namespace Slingshot.PCO.Models.DTO
{
    public class TagGroupDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string FolderName { get; set; }

        public List<TagDTO> Tags { get; set; }

        public string Key
        {
            get { return string.Format( "PCOTagGroup:{0}", Id ); }
        }

        public TagGroupDTO( DataItem data )
        {
            Id = data.Id;
            Name = data.Item.name;
            FolderName = data.Item.service_type_folder_name;
            Tags = new List<TagDTO>();
            // ToDo:  Tags collection is not implemented.
        }
    }
}
