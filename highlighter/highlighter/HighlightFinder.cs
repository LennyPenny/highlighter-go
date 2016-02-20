using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DemoInfo;

namespace highlighter
{
    class HighlightFinder
    {
        private SortedDictionary<int, string> _highlights;

        private DemoParser _parser;

        public string Path { get; private set; }

        public HighlightFinder(string path)
        {
            Path = path;
            _parser = new DemoParser(File.OpenRead(path));

            _parser.ParseHeader();

            _highlights = new SortedDictionary<int, string>();

            SetupHighlightHandlers();
        }

        private void SetupHighlightHandlers()
        {
            //multikill finder (finds kills in quick succession)
            {
                var counter = 0; //keeps track of kills in quick succession
                var timeLimit = (float)Program.Settings.multiKill.timeLimit; //how many seconds until a multkill expires
                var start = 0; //tick where the mutlkill started
                var lastKill = .0f; //time the last kill of the multikill happened

                _parser.RoundStart += (s, e) =>
                {
                    counter = 0;
                    start = 0;
                    lastKill = .0f;
                };

                _parser.PlayerKilled += (s, e) =>
                {
                    if (e.Killer?.SteamID != (long) Program.Settings.playerID)
                        return;

                    if (_parser.CurrentTime - lastKill <= timeLimit || counter == 0)
                    {
                        counter++;
                        lastKill = _parser.CurrentTime;

                        if (counter == 1)
                            start = _parser.IngameTick;
                    }
                };

                _parser.TickDone += (s, e) =>
                {
                    if (lastKill == .0f)
                        return;

                    if (_parser.CurrentTime - lastKill > timeLimit)
                    {
                        _highlights.Add(_parser.IngameTick, $"{counter} kills in quick succession");

                        counter = 0;
                        start = 0;
                        lastKill = .0f;
                    }

                };
            }
        }

        public async Task<SortedDictionary<int, string>> GetHighlights()
        {
            _parser.ParseToEnd();
            _parser.Dispose();

            return _highlights;
        }
    }
}