using System.Collections.Generic;

namespace UEParser.Models;

public class TagConverters
{
    public Dictionary<string, string> HTMLTagConverters { get; set; }

    public TagConverters()
    {
        HTMLTagConverters = [];
    }
}