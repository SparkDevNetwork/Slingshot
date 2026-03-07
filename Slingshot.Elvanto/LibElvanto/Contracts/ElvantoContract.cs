using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LibElvanto.Contracts;

public abstract class ElvantoContract
{
    public Dictionary<string, string> AttributeValues { get; set; } = new Dictionary<string, string>();

    public virtual void Process( JsonElement dataElement, List<string>? fields )
    {

    }

}
