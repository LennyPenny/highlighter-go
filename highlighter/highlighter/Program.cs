using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace highlighter
{
    public class Program
    {
        public static dynamic Settings;

        class OutputData
        {
            public string path;
            public string map;
            public SortedDictionary<int, string> events;
        }

        static void Main(string[] args)
        {
            Settings = JsonConvert.DeserializeObject(File.ReadAllText("settings.json"));

            var demos = new List<HighlightFinder>();

            if (args.Length > 0)
            {
                foreach (var path in args)
                {
                    demos.Add(new HighlightFinder(path));
                }
            }
            else
            {
                foreach (var path in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "*.dem"))
                {
                    demos.Add(new HighlightFinder(path));
                }
            }

            OutputData[] results = new OutputData[demos.Count];

            var highlightTasks = new List<Task>();
            for (int i = 0; i < demos.Count; i++)
            {
                var currentdemo = i;
                highlightTasks.Add(Task.Run(async () =>
                {
                    var result = new OutputData();
                    result.path = demos[currentdemo].Path;
                    result.map = demos[currentdemo].Map;
                    result.events = await demos[currentdemo].GetHighlights();
                    results[currentdemo] = result;
                }));
            }

            Task.WaitAll(highlightTasks.ToArray());

            var output = JsonConvert.SerializeObject(results, Formatting.Indented);

            File.WriteAllText("output.json", output);   
        }
    }
}
