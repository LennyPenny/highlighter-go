﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
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
        public string Map { get; private set; }

        public HighlightFinder(string path)
        {
            Path = path;
            _parser = new DemoParser(File.OpenRead(path));

            _parser.ParseHeader();

            Map = _parser.Map;

            _highlights = new SortedDictionary<int, string>();

            _parser.MatchStarted += (e, s) => SetupHighlightHandlers(); //we dont want the stuff that's happening in warmup
        }

        private Player LocalPlayer()
        {
            return _parser.PlayingParticipants.First(ply => ply.SteamID == (long)Program.Settings.playerID);
        }

        private void SetupHighlightHandlers()
        {
            //multikill finder (finds kills in quick succession)
            {
                var counter = 0; //keeps track of kills in quick succession
                var timeLimit = (float) Program.Settings.multiKill.timeLimit;
                    //how many seconds until a multkill expires
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
                    if (e.Killer?.SteamID != LocalPlayer().SteamID)
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
                        if (counter > 1)
                            _highlights.Add(start - (int) _parser.TickRate*2, $"{counter} kills in quick succession");

                        counter = 0;
                        start = 0;
                        lastKill = .0f;
                    }

                };
            }

            //clutch finder (1vX that were won)
            {

                //this is ugly but the RoundEnded event is broken and fires too early
                int start = 0; //tick where the clutch started
                bool clutch = false;
                int clutchNum = 0; //1vx

                _parser.RoundStart += (s, e) =>
                {
                    clutch = false;
                };

                _parser.PlayerKilled += (s, e) =>
                {
                    if (clutch)
                        return;

                    if (!LocalPlayer().IsAlive)
                        return;

                    if (_parser.PlayingParticipants.Count(ply => ply.Team == LocalPlayer().Team && ply.IsAlive) > 1)
                        return;

                    clutch = true;
                    start = _parser.IngameTick;
                    clutchNum = _parser.PlayingParticipants.Count(ply => ply.Team != LocalPlayer().Team && ply.IsAlive);

                };

                _parser.RoundOfficiallyEnd += (s, e) =>
                {
                    if (!clutch || !LocalPlayer().IsAlive)
                        return;

                    _highlights.Add(start - (int)_parser.TickRate * 3, $"1v{clutchNum} clutch win");
                };
            }

            //one deags
            {
                _parser.PlayerKilled += (s, e) =>
                {
                    if (e.Killer?.SteamID != LocalPlayer().SteamID)
                        return;

                    if (!e.Headshot)
                        return;

                    if (e.Weapon.Weapon != EquipmentElement.Deagle)
                        return;

                    _highlights.Add(_parser.IngameTick - (int)_parser.TickRate * 4, "one-deag");
                };
            }

            //nade kills
            {
                _parser.PlayerKilled += (s, e) =>
                {
                    if (e.Killer?.SteamID != LocalPlayer().SteamID)
                        return;

                    if (e.Weapon.Weapon != EquipmentElement.HE)
                        return;

                    _highlights.Add(_parser.IngameTick - (int)_parser.TickRate * 8, "nade kill");
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