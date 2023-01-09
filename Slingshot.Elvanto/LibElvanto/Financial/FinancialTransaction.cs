using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace LibElvanto.Financial;

public class FinancialTransaction
{
    public int? Id { get; set; }
    public int? BatchId { get; set; }
    public int? AuthorizedPersonId { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? TransactionType { get; set; } = "Contribution";
    public string? TransactionSource { get; set; } = "Website";
    public string? CurrencyType { get; set; }
    public string? Summary { get; set; }
    public string? TransactionCode { get; set; }
    public int? CreatedByPersonId { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public int? ModifiedByPersonId { get; set; }
    public string? ModifiedDateTime { get; set; }

    public decimal Total { get; set; } = 0;
}