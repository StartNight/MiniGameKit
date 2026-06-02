using System.Collections.Generic;
using System.Text;

namespace MGKit.Editor
{
    internal static class CsvLineParser
    {
        public static string[] Parse(string line)
        {
            var result = new List<string>();
            var cur = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cur.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(cur.ToString());
                    cur.Clear();
                }
                else
                {
                    cur.Append(c);
                }
            }

            result.Add(cur.ToString());
            return result.ToArray();
        }
    }
}
