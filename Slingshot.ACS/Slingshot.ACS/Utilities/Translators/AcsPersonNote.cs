using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Slingshot.Core.Model;

namespace Slingshot.ACS.Utilities.Translators
{
    public static class AcsPersonNote
    {
        public static PersonNote Translate ( DataRow row )
        {
            var note = new PersonNote();

            note.PersonId = row.Field<int>( "IndividualId" );
            note.Text = row.Field<string>( "Comment" );
            note.NoteType = row.Field<string>( "ComtType" );

            var date = row.Field<DateTime?>( "ComtDate" );
            if ( date != null )
            {
                note.DateTime = date.Value;
            }

            // generate a unique note id
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( note.PersonId.ToString() + note.Text + note.NoteType ) );
            var noteId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
            if ( noteId > 0 )
            {
                note.Id = noteId;
            }

            return note;
        }
    }
}
