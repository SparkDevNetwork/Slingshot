using Slingshot.PCO.Models.ApiModels;

namespace Slingshot.PCO.Models.DTO
{
    public class EmailTemplateDTO
    {
        public int Id { get; set; }

        public string Kind { get; set; }

        public string Subject { get; set; }

        public EmailTemplateDTO( DataItem data )
        {
            Id = data.Id;
            Kind = data.Item.kind;
            Subject = data.Item.subject;
        }
    }
}
