using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace LibElvanto.Financial;

public class FinancialTransactionDetail
{
    public int? Id { get; set; }
    public int? TransactionId { get; set; }
    public int? AccountId { get; set; }
    public decimal? Amount { get; set; }
    public string? Summary { get; set; }
    public int? CreatedByPersonId { get; set; }
    public string? CreatedDateTime { get; set; }
    public int? ModifiedByPersonId { get; set; }
    public string? ModifiedDateTime { get; set; }
}