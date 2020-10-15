using Slingshot.Core.Model;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace Slingshot.F1.Utilities.Translators.SQL
{
    public static class F1FinancialAccount
    {
        public static FinancialAccount Translate( DataRow row )
        {
            var account = new FinancialAccount();

            if( string.IsNullOrWhiteSpace( row.Field<string>( "sub_fund_name" ) ) )
            {
                account.Name = row.Field<string>( "fund_name" );

                //Use Hash to create Account ID
                MD5 md5Hasher = MD5.Create();
                var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( row.Field<string>( "fund_name" ) ) );
                var accountId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                if ( accountId > 0 )
                {
                    account.Id = accountId;
                }
            }
            else
            {
                account.Name = row.Field<string>( "sub_fund_name" );
               
                //Use Hash to get parent Account ID
                MD5 md5Hasher = MD5.Create();
                string valueToHash = row.Field<string>( "fund_name" );
                var hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( valueToHash ) );
                var ParentAccountId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                if ( ParentAccountId > 0 )
                {
                    account.ParentAccountId = ParentAccountId;
                }

                //Use Hash to create Account ID
                md5Hasher = MD5.Create();
                valueToHash = row.Field<string>( "fund_name" ) + row.Field<string>( "sub_fund_name" );
                hashed = md5Hasher.ComputeHash( Encoding.UTF8.GetBytes( valueToHash ) );
                var accountId = Math.Abs( BitConverter.ToInt32( hashed, 0 ) ); // used abs to ensure positive number
                if ( accountId > 0 )
                {
                    account.Id = accountId;
                }
            }

            //ToDo:  This field should be in the database, but it isn't.  If it gets added, we need to respect it.
            //account.IsTaxDeductible = row.Field<int>( "taxDeductible" ) != 0;
            account.IsTaxDeductible = false;

            return account;
        }
    }
}
