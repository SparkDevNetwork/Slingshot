using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LibElvanto.Contracts
{
    internal class PaginatedResponse<T>
    {
        public int OnThisPage { get; set; } = 0;
        public int Page { get; set; } = 0;
        public int PerPage { get; set; } = 0;
        public int Total { get; set; } = 0;

        public List<T> Data { get; set; } = new List<T>();
    }
}
