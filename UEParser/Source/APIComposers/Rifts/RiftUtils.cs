using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UEParser.APIComposers;

public class RiftUtils
{
    // BHVR uses codenames such as "TOME19", to make it consistent in all cases turn it into "Tome"
    //public static string TomeToTitleCase(string input)
    //{
    //    if (string.IsNullOrEmpty(input) || !input.StartsWith("tome", StringComparison.OrdinalIgnoreCase))
    //    {
    //        return input;
    //    }

    //    string firstChar = input[..1].ToUpper();
    //    string restOfChars = input[1..].ToLower();
    //    return firstChar + restOfChars;
    //}
}
