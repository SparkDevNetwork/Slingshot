using Slingshot.Core.Model;
using Slingshot.PCO.Models.DTO;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportPersonNote
    {
        public static PersonNote Translate( NoteDTO inputNote )
        {
            var note = new PersonNote
            {
                PersonId = inputNote.PersonId.Value,
                DateTime = inputNote.CreatedAt,
                Id = inputNote.Id,
                NoteType = inputNote.NoteCategory.Name,
                CreatedByPersonId = inputNote.CreatedById,
                Text = inputNote.Note
            };

            return note;
        }
    }
}
