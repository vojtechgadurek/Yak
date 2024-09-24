using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindJack.Utils
{
    internal static class ArgsParser
    {
        public static IEnumerable<(string, string)> ParseNamedArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("--"))
                {
                    string key = args[i].Substring(2);
                    string value = args[i + 1];
                    i++;
                    yield return (key, value);
                }
            }
        }
    }
}
