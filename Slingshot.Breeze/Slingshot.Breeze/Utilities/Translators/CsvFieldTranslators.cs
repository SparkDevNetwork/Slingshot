using System;
using System.Globalization;
using Slingshot.Core.Model;
using CsvRecord = System.Collections.Generic.IDictionary<string, object>;

namespace Slingshot.Breeze.Utilities
{
    public class CsvFieldTranslators
    {
        public static object GetValue( Type type, string csvFieldName, CsvRecord csvRecord )
        {
            var nullableUnderlyingType = Nullable.GetUnderlyingType( type );
            var isNullable = nullableUnderlyingType != null;
            type = isNullable ? nullableUnderlyingType : type;
            var typeCode = Type.GetTypeCode( type );

            if ( typeCode == TypeCode.Boolean)
            {
                return isNullable ? GetNullableBool( csvFieldName, csvRecord ) : GetBool( csvFieldName, csvRecord );
            }

            if ( typeCode == TypeCode.Int32 )
            {
                return isNullable ? GetNullableInt( csvFieldName, csvRecord ) : GetInt( csvFieldName, csvRecord );
            }

            if (typeCode == TypeCode.Decimal )
            {
                return isNullable ? GetNullableDecimal( csvFieldName, csvRecord ) : GetDecimal( csvFieldName, csvRecord );
            }

            if ( isNullable && type == typeof( DateTime ) )
            {
                return GetNullableDateTime( csvFieldName, csvRecord );
            }

            if ( type == typeof( TransactionSource ) )
            {
                return GetTransactionSource( csvFieldName, csvRecord );
            }

            if ( type == typeof( CurrencyType ) )
            {
                return GetCurrencyType( csvFieldName, csvRecord );
            }

            if ( type == typeof( Gender ) )
            {
                return GetGender( csvFieldName, csvRecord );
            }

            if ( type == typeof( FamilyRole ) )
            {
                return GetFamilyRole( csvFieldName, csvRecord );
            }

            if ( type == typeof( EmailPreference ) )
            {
                return GetEmailPreference( csvFieldName, csvRecord );
            }

            if ( type == typeof( MaritalStatus ) )
            {
                return GetMaritalStatus( csvFieldName, csvRecord );
            }

            return GetString( csvFieldName, csvRecord );
        }

        public static string GetString( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( string );

            if ( !csvRecord.ContainsKey( key ) )
            {
                return defaultValue;
            }

            var value = csvRecord[key];
            return value == null ? defaultValue : value.ToString().Trim();            
        }
                
        public static TransactionSource GetTransactionSource( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( TransactionSource );
            var stringValue = GetString( key, csvRecord );

            if ( string.IsNullOrEmpty( stringValue ) )
            {
                return defaultValue;
            }

            switch ( stringValue.ToLower() )
            {
                case "check":
                    return TransactionSource.BankChecks;
                case "cash":
                    return TransactionSource.OnsiteCollection;
                case "credit/debit online":
                    return TransactionSource.Website;
                default:
                    return defaultValue;
            }
        }

        public static CurrencyType GetCurrencyType( string key, CsvRecord csvRecord )
        {
            var defaultValue = CurrencyType.Unknown;
            var stringValue = GetString( key, csvRecord );

            if ( string.IsNullOrEmpty( stringValue ) )
            {
                return defaultValue;
            }

            switch ( stringValue.ToLower() )
            {
                case "check":
                    return CurrencyType.Check;
                case "cash":
                    return CurrencyType.Cash;
                case "credit/debit online":
                    return CurrencyType.CreditCard;
                default:
                    return defaultValue;
            }
        }

        public static FamilyRole GetFamilyRole( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( FamilyRole );
            var stringValue = GetString( key, csvRecord );

            if ( string.IsNullOrEmpty( stringValue ) )
            {
                return defaultValue;
            }

            switch ( stringValue.ToLower() )
            {
                case "adult":
                case "spouse":
                case "head of household":
                    return FamilyRole.Adult;
                case "child":
                    return FamilyRole.Child;
                default:
                    return defaultValue;
            }
        }

        public static Gender GetGender( string key, CsvRecord csvRecord )
        {
            var defaultValue = Gender.Unknown;
            var stringValue = GetString( key, csvRecord );

            if ( string.IsNullOrEmpty( stringValue ) )
            {
                return defaultValue;
            }

            switch ( stringValue.ToLower() )
            {
                case "female":
                    return Gender.Female;
                case "male":
                    return Gender.Male;
                default:
                    return defaultValue;
            }
        }

        public static EmailPreference GetEmailPreference( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( EmailPreference );
            var stringValue = GetString( key, csvRecord );

            if ( string.IsNullOrEmpty( stringValue ) )
            {
                return defaultValue;
            }

            // Values look like this:
            // No email (must check box in child's account to opt out parents)
            if ( stringValue.ToLower().StartsWith( "no email" ) )
            {
                return EmailPreference.DoNotEmail;
            }

            return defaultValue;
        }

        public static MaritalStatus GetMaritalStatus( string key, CsvRecord csvRecord )
        {
            var defaultValue = MaritalStatus.Unknown;
            var stringValue = GetString( key, csvRecord );

            if ( string.IsNullOrEmpty( stringValue ) )
            {
                return defaultValue;
            }

            switch ( stringValue.ToLower() )
            {
                case "single":
                case "engaged":
                    return MaritalStatus.Single;
                case "married":
                    return MaritalStatus.Married;
                default:
                    return defaultValue;
            }
        }

        public static bool? GetNullableBool( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( bool? );
            var stringValue = GetString( key, csvRecord );
            
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return defaultValue;
            }

            switch ( stringValue.ToLower() )
            {
                case "true":
                case "yes":
                case "on":
                case "1":
                    return true;
                case "false":
                case "no":
                case "off":
                case "0":
                    return false;
                default:
                    return defaultValue;
            }
        }

        public static bool GetBool( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( bool );
            return GetNullableBool( key, csvRecord ) ?? defaultValue;
        }

        public static int GetInt( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( int );
            return GetNullableInt( key, csvRecord ) ?? defaultValue;
        }

        public static int? GetNullableInt( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( int? );
            var stringValue = GetString( key, csvRecord );
            var successfulParse = int.TryParse( stringValue, out var parsedValue );
            return successfulParse ? parsedValue : defaultValue;
        }

        public static decimal? GetNullableDecimal( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( decimal? );
            var stringValue = GetString( key, csvRecord );
            var successfulParse = decimal.TryParse( stringValue, out var parsedValue );

            if (!successfulParse)
            {
                successfulParse = decimal.TryParse( stringValue, NumberStyles.Currency, CultureInfo.CurrentCulture, out parsedValue );
            }

            return successfulParse ? parsedValue : defaultValue;
        }

        public static decimal GetDecimal( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( decimal );
            return GetNullableDecimal( key, csvRecord ) ?? defaultValue;
        }

        public static DateTime? GetNullableDateTime( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( DateTime? );
            var stringValue = GetString( key, csvRecord );
            var successfulParse = DateTime.TryParse( stringValue, out var parsedValue );
            return successfulParse ? parsedValue : defaultValue;
        }

        public static string GetGrade( string key, CsvRecord csvRecord )
        {
            var defaultValue = default( string );
            var grade = GetString( key, csvRecord );

            if ( string.IsNullOrWhiteSpace( grade ) )
            {
                return defaultValue;
            }

            // Translate to the value of the grade defined values in Rock
            switch ( grade )
            {
                case "12th":
                    return "0";
                case "11th":
                    return "1";
                case "10th":
                    return "2";
                case "9th":
                    return "3";
                case "8th":
                    return "4";
                case "7th":
                    return "5";
                case "6th":
                    return "6";
                case "5th":
                    return "7";
                case "4th":
                    return "8";
                case "3rd":
                    return "9";
                case "2nd":
                    return "10";
                case "1st":
                    return "11";
                case "Kindergarten":
                    return "12";
                default:
                    return defaultValue;
            }
        }
    }
}
