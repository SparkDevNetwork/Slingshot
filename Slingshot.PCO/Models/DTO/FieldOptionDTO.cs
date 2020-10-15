using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class FieldOptionDTO
    {
        public int Id { get; set; }

        public string Value { get; set; }

        public int Sequence { get; set; }

        public FieldOptionDTO( DataItem data )
        {
            Id = data.Id;
            Value = data.Item.value;
            Sequence = data.Item.sequence;
        }
    }
}
