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
            public string map;
            public SortedDictionary<int, string> events;
        }

        static void Main(string[] args)
        {
            var inputpath = "";

            if (args.Length == 1)
                inputpath = args[1];
            else
                inputpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)+@"\output.json";



            if (File.Exists("highlights.cfg"))
                File.Delete("highlights.cfg");

            using (var fileStream = File.OpenWrite("highlights.cfg"))
            {
                using (var writer = new StreamWriter(fileStream))
                {
                    var input = JsonConvert.DeserializeObject<List<InputData>>(File.ReadAllText(inputpath));

                    var lastdemo = 0;
                    var demonum = input.Count;
                    foreach (var demo in input)
                    {
                        if (lastdemo == 0)
                        {
                            writer.WriteLine($"alias hl_advance \"hl_demo{lastdemo};\"");
                        }

                        var lastEventTick = 0;
                        foreach (var @event in demo.events)
                        {
                            if (lastEventTick == 0)
                            {
                                lastEventTick = @event.Key;
                                writer.WriteLine($"alias hl_demo{lastdemo} \"playdemo {demo.path}; alias hl_advance hl_tick{@event.Key};\"");
                                continue;
                            }

                            writer.WriteLine($"alias hl_tick{lastEventTick} \"demo_gototick {lastEventTick}; demo_pause; alias hl_advance hl_tick{@event.Key}; alias hl_prev hl_tick{lastEventTick};\"");
                            lastEventTick = @event.Key;
                        }

                        writer.WriteLine($"alias hl_tick{lastEventTick} \"demo_gototick {lastEventTick}; demo_pause; alias hl_advance hl_demo{lastdemo+1};\"");
                        writer.WriteLine("");

                        lastdemo++;
                    }
                }
            }
        }
    }
}
