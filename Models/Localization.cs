using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.Models;

public class LocalizationEntry
{
    public required string Key { get; set; }
    public required string SourceString { get; set; }
}