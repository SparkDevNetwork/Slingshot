using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportBatch
    {
        public static FinancialBatch Translate( BatchDTO inputBatch )
        {
            var financialBatch = new FinancialBatch
            {
                Id = inputBatch.Id,
                StartDate = inputBatch.CreatedAt,
                Name = inputBatch.Description,
                CreatedDateTime = inputBatch.CreatedAt,
                ModifiedDateTime = inputBatch.UpdatedAt,
                CreatedByPersonId = inputBatch.OwnerId,
                Status = ( inputBatch.CommittedAt.HasValue ) ? BatchStatus.Closed : BatchStatus.Open
            };

            return financialBatch;
        }
    }
}
