using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

using Slingshot.Core;
using Slingshot.Core.Model;

namespace Slingshot.F1.Utilities.Translators.MDB
{
    public static class F1Business
    {
        public static Business Translate( DataRow row )
        {
            var business = new Business();
            var notes = new List<string>();
            try
            {

            }
            catch(Exception ex)
            {
                notes.Add( "ERROR in Export: " + ex.Message + ": " + ex.StackTrace );
            }

            // write out import notes
            if ( notes.Count > 0 )
            {
                business.Note = string.Join( ",", notes );
            }

            return business;
        }
    }
}
