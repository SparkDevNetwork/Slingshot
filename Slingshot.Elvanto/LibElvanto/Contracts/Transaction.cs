using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LibElvanto.Attributes;

namespace LibElvanto.Contracts;

[ElvantoResource( "https://api.elvanto.com/v1/financial/transactions/getAll.json", "transactions", "transaction" )]

public class Transaction : ElvantoContract
{
    [JsonPropertyName( "id" )]
    public string? Id { get; set; }

    [JsonPropertyName( "person_id" )]
    public string? PersonId { get; set; }

    [JsonPropertyName( "transaction_date" )]
    public DateTime? TransactionDate { get; set; }

    [JsonPropertyName( "transaction_method" )]
    public string? TransactionMethod { get; set; }

    [JsonPropertyName( "check_number" )]
    public string? CheckNumber { get; set; }

    [JsonPropertyName( "batch" )]
    public Batch? Batch { get; set; }

    [JsonPropertyName( "transaction_total" )]
    public string? TransactionTotal { get; set; }

    [JsonPropertyName( "amounts" )]
    public Amounts? Amounts { get; set; }
}


public class Amount
{
    [JsonPropertyName( "id" )]
    public string? Id { get; set; }

    [JsonPropertyName( "category" )]
    public IdNameContract? Category { get; set; }

    [JsonPropertyName( "total" )]
    public string? Total { get; set; }

    [JsonPropertyName( "tax_deductible" )]
    public int? TaxDeductible { get; set; }

    [JsonPropertyName( "memo" )]
    public string? Memo { get; set; }

    [JsonPropertyName( "external_notes" )]
    public string? ExternalNotes { get; set; }
}

public class Amounts
{
    [JsonPropertyName( "amount" )]
    public List<Amount>? Amount { get; set; }
}

public class Batch
{
    [JsonPropertyName( "id" )]
    public string? Id { get; set; }

    [JsonPropertyName( "number" )]
    public string? Number { get; set; }

    [JsonPropertyName( "name" )]
    public string? Name { get; set; }
}
