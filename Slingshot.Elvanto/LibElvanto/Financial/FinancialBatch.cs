using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibElvanto.Financial;

public class FinancialBatch
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? CampusId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; } = "Closed";
    public int? ModifiedByPersonId { get; set; }
    public string? ModifiedDateTime { get; set; }
    public int ControlAmount { get; set; } = 0;
}
