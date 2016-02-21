using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace bindgen
{
    class Program
    {
        class InputData
        {
            public string path;
            public string map
            public SortedDictionary<int, string> events;
        }

        static void Main(string[] args)
        {
            var inputpath = "";

            if (args.Length == 1)
                inputpath = args[1];
            else
                inputpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)+@"\output.json";



            var input = JsonConvert.DeserializeObject<List<InputData>>(File.ReadAllText(inputpath));

            using (var fileStream = File.OpenWrite("highlights.cfg"))
            {
                using (var writer = new StreamWriter(fileStream))
                {
                    var lastdemo = 0;
                    var demonum = input.Count;
                    foreach (var demo in input)
                    {
                        if (lastdemo == 0)
                        {
                            writer.WriteLine($"alias highlighter_advance highlighter_demo{lastdemo}");
                        }

                        var lastEventTick = 0;
                        foreach (var @event in demo.events)
                        {
                            if (lastEventTick == 0)
                            {
                                lastEventTick = @event.Key;
                                writer.WriteLine($"alias highlighter_demo{lastdemo} \"playdemo {demo.path}; alias highligher_advance highlighter_eventick{@event.Key}\"");
                                continue;
                            }

                            writer.WriteLine($"alias highlighter_eventick{lastEventTick} \"demo_gototick {lastEventTick}; alias highligher_advance highlighter_eventick{@event.Key}\"");
                            lastEventTick = @event.Key;
                        }
                        writer.WriteLine($"alias highlighter_eventick{lastEventTick} \"demo_gototick {lastEventTick}; alias highligher_advance highlighter_demo{lastdemo+1}\"");
                        writer.WriteLine("");

                        lastdemo++;
                    }
                }
            }
        }
    }
}
