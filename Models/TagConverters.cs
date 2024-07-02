using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Models;

public class TagConverters
{
    public Dictionary<string, string> HTMLTagConverters { get; set; }

    public TagConverters()
    {
        HTMLTagConverters = [];
    }
}