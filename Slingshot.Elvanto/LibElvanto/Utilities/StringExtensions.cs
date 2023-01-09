using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
