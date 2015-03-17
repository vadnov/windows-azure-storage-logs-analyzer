using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AnalyzeStorageLogs
{
    class Program
    {

        private static readonly Dictionary<string, SummaryItem> Values = new Dictionary<string, SummaryItem>();
        static void Main(string[] args)
        {
            var prefix = Directory.GetCurrentDirectory();
            var dirs = Directory.EnumerateDirectories(prefix);
            foreach (var dir in dirs)
            {
                var hourDirs = Directory.EnumerateDirectories(dir);
                foreach (var hourDir in hourDirs)
                {
                    var files = Directory.EnumerateFiles(hourDir);
                    foreach (var file in files)
                    {
                        var lines = File.ReadAllLines(file);
                        foreach (var line in lines)
                        {
                            var fields = new List<string>();
                            var builder = new StringBuilder();
                            var state = 0;
                            foreach (var substr in line)
                            {
                                switch (state)
                                {
                                    case 0:
                                        if (';' == substr)
                                        {
                                            state = 1;
                                            fields.Add(builder.ToString());
                                            builder.Clear();
                                        }
                                        else
                                        {
                                            builder.Append(substr);
                                        }
                                        break;
                                    case 1:
                                        if (';' == substr)
                                        {
                                            fields.Add(builder.ToString());
                                            builder.Clear();
                                            state = 1;
                                        }
                                        else if ('"' == substr)
                                        {
                                            builder.Append(substr);
                                            state = 2;
                                        }
                                        else
                                        {
                                            builder.Append(substr);
                                            state = 0;
                                        }
                                        break;
                                    case 2:
                                        builder.Append(substr);
                                        if ('"' == substr)
                                        {
                                            state = 3;
                                        }
                                        break;
                                    case 3:
                                        if (';' == substr)
                                        {
                                            fields.Add(builder.ToString());
                                            builder.Clear();
                                            state = 1;
                                        }
                                        break;
                                }
                            }
                            fields.Add(builder.ToString());
                            builder.Clear();
                            var key = fields[12].Trim('"');
                            var bytes = long.Parse(fields[20]);
                            if (Values.ContainsKey(key))
                            {
                                Values[key].Bytes += bytes;
                                Values[key].Times += 1;
                            }
                            else
                            {
                                Values.Add(key, new SummaryItem { Bytes = bytes, Times = 1 });
                            }
                        }
                    }
                }
            }
            var stream = File.OpenWrite("summary");
            using (var writer = new StreamWriter(stream))
            {
                foreach (KeyValuePair<string, SummaryItem> pair in Values)
                {
                    writer.WriteLine(pair.Key + "\t" + pair.Value.Times + "\t" + pair.Value.Bytes);
                }
                writer.Close();
                Values.Clear();
            }
        }

        class SummaryItem
        {
            public long Bytes { get; set; }
            public int Times { get; set; }

        }
    }
}
