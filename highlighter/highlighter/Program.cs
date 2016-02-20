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

        static void Main(string[] args)
        {
            Settings = JsonConvert.DeserializeObject(File.ReadAllText("settings.json"));

            var results = new Dictionary<string, SortedDictionary<int, string>>();

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

            var highlightTasks = new List<Task>();
            foreach (var demo in demos)
            {
                highlightTasks.Add(Task.Run(async () =>
                {
                    results.Add(demo.Path, await demo.GetHighlights());
                }));
            }

            Task.WaitAll(highlightTasks.ToArray());
        }
    }
}
