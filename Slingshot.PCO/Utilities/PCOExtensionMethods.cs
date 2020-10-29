using Slingshot.Core;
using System.Text.RegularExpressions;

namespace Slingshot.PCO.Utilities
{
    public static class PCOExtensionMethods
    {
        /// <summary>
        /// Strips HTML from the string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns></returns>
        public static string StripHtml( this string str )
        {
            return str.IsNullOrWhiteSpace()
                ? str
                : Regex.Replace( str, @"<.*?>|<!--(.|\r|\n)*?-->", string.Empty );
        }
    }
}
