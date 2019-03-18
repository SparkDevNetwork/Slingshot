using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Slingshot.Core;
using Slingshot.Core.Model;
using Slingshot.PCO.Models;

namespace Slingshot.PCO.Utilities.Translators
{
    public static class PCOImportPersonNote
    {
        public static PersonNote Translate( PCONote inputNote )
        {
            var note = new PersonNote();

            note.PersonId = inputNote.person_id.Value;
            note.DateTime = inputNote.created_at;
            note.Id = inputNote.id;
            note.NoteType = inputNote.note_category.name;
            note.CreatedByPersonId = inputNote.created_by_id;
            note.Text = inputNote.note;

            return note;
        }
    }
}
