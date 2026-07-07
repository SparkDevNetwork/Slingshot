using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace LibElvanto.Utilities;

public static class StringExtensions
{
    public static decimal AsDecimal( this string? s )
    {
        if ( s != null && decimal.TryParse( s, out var value ) )
        {
            return value;
        }
        return 0;

    }

    public static string AsMaritalStatus( this string? s )
    {
        if ( string.IsNullOrEmpty( s ) )
        {
            return "Unknown";
        }

        var allowed = new List<string>
        {
            "Married",
            "Divorced",
            "Single",
            "Unknown"
        };

        if ( allowed.Contains( s ) )
        {
            return s;
        }

        return "Unknown";
    }

    public static string ForCSV( this string? s )
    {
        return $"\"{s.Replace( "\"", "\"\"" )}\"";
    }

    public static string Truncate( this string value, int maxChars )
    {
        return value.Length <= maxChars ? value : value.Substring( 0, maxChars );
    }

    public static string StripHTML( this string input )
    {
        return Regex.Replace( input, "<.*?>", String.Empty );
    }

    public static string FormatTime( this string input )
    {
        try
        {
            DateTime dateTime = DateTime.ParseExact( input, "h:mm tt", CultureInfo.InvariantCulture );
            return dateTime.ToString("HH:mm");
        }
        catch
        {
            return string.Empty;
        }
    }
}
