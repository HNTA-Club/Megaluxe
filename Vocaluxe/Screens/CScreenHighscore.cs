#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public partial class CScreenHighscore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 13; }
        }

        private const string _TextSongName = "TextSongName";
        private const string _TextSongMode = "TextSongMode";

        private List<SDBScoreEntry>[] _Scores;
        private List<int> _NewEntryIds;
        private int _Round;
        private bool _IsDuet;
        private bool _FromScreenSong = false;
        private int _HighscoreStream = -1;
        private bool _HasPlayedHighscoreSound = false;

        private static int PlaySound(ESounds sound, int volume)
        {
            var streamId = CSound.PlaySound(sound, false);
            CSound.SetStreamVolume(streamId, volume);
            return streamId;
        }

        public override EMusicType CurrentMusicType
        {
            get { return EMusicType.BackgroundPreview; }
        }

        public override void Init()
        {
            base.Init();

            var texts = new List<string>
            {
                _TextSongName,
                _TextSongMode
            };

            _InitDashboard(texts);

            _ThemeTexts = texts.ToArray();

            _NewEntryIds = new List<int>();
        }

        public override void Draw()
        {
            base.Draw();
            _DrawDashboard();
        }

        public override bool UpdateGame()
        {
            if (_Scores == null || _Round >= _Scores.Length || _Scores[_Round] == null)
                return true;

            if (!_NeedsRefresh)
                return true;

            _NeedsRefresh = false;

            _UpdateDashboard();

            return true;
        }

        public override void OnShow()
        {
            base.OnShow();
            _HasPlayedHighscoreSound = false;
            _Round = 0;
            _SeasonYear = CHighscoreStats.GetCurrentSeasonYear();
            _FromScreenSong = (CGame.NumRounds == 0);
            _ResetDifficultyHover();

            _NewEntryIds.Clear();
            _AddScoresToDB();
            _LoadScores();
            _CountBrokenRecords();
            _UpdateRound();

            _NeedsRefresh = true;
            UpdateGame();
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) { }
            else
            {
                switch (keyEvent.Key)
                {
                    case Keys.Escape:
                    case Keys.Back:
                    case Keys.Enter:
                        _LeaveScreen();
                        break;

                    case Keys.Left:
                        _ChangeRound(-1);
                        break;
                    case Keys.Right:
                        _ChangeRound(1);
                        break;
                    case Keys.Up:
                        _ChangeSeasonYear(1);
                        break;
                    case Keys.Down:
                        _ChangeSeasonYear(-1);
                        break;
                    case Keys.Tab:
                    case Keys.M:
                        _CycleChartMode();
                        break;
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (_HandleDifficultyHover(mouseEvent))
                return true;

            if (mouseEvent.LB)
            {
                _LeaveScreen();
            }
            if (mouseEvent.RB)
            {
                _LeaveScreen();
            }
            if (mouseEvent.Wheel != 0)
            {
                _ChangeSeasonYear(-Math.Sign(mouseEvent.Wheel));
            }
            return true;
        }

        private bool _IsNewEntry(int id)
        {
            return _NewEntryIds.Any(t => t == id);
        }

        private void _AddScoresToDB()
        {
            if (_FromScreenSong) return;

            var points = CGame.GetPoints();
            if (points == null) return;
            if (CScreenSong.GetAudioMode() == EAudioMode.TR_AUDIOMODE_KARAOKE) return;

            for (var round = 0; round < points.NumRounds; round++)
            {
                var players = points.GetPlayer(round, CGame.NumPlayers);
                for (var p = 0; p < players.Length; p++)
                {
                    if (players[p].Points > CSettings.MinScoreForDB && players[p].SongFinished && !CProfiles.IsGuestProfile(players[p].ProfileId))
                    {
                        int id = CDataBase.AddScore(players[p]);
                        if (id > 0)
                            _NewEntryIds.Add(id);
                    }
                }
            }

            for (var round = 0; round < points.NumRounds; round++)
            {
                var song = CGame.GetSong(round);
                if (song != null && song.Id >= 0)
                    _SessionSongsSung.Add(song.Id);
            }
        }

        private void _LoadScores()
        {
            var rounds = CGame.NumRounds;

            if (rounds == 0)
            {
                _FromScreenSong = true;
                _Round = (int)EGameMode.TR_GAMEMODE_NORMAL;
                _Scores = new List<SDBScoreEntry>[4];
                _AvailableYears = new List<int>[4];
                var songId = CScreenSong.getSelectedSongId();
                bool foundHighscoreEntries = false;

                for (var gameModeNum = 0; gameModeNum < 4; gameModeNum++)
                {
                    _Scores[gameModeNum] = CDataBase.LoadScore(songId, (EGameMode)gameModeNum, EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL) ?? new List<SDBScoreEntry>();
                    _AvailableYears[gameModeNum] = _Scores[gameModeNum].Select(s => CHighscoreStats.GetSeasonYear(s)).Distinct().OrderByDescending(y => y).ToList();

                    if (!foundHighscoreEntries && _Scores[gameModeNum].Count > 0)
                    {
                        _Round = gameModeNum;
                        foundHighscoreEntries = true;
                    }
                }
            }
            else
            {
                _FromScreenSong = false;
                _Scores = new List<SDBScoreEntry>[rounds];
                _AvailableYears = new List<int>[rounds];
                for (var round = 0; round < rounds; round++)
                {
                    var songId = CGame.GetSong(round).Id;
                    var gameMode = CGame.GetGameMode(round);
                    _Scores[round] = CDataBase.LoadScore(songId, gameMode, EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL) ?? new List<SDBScoreEntry>();
                    _AvailableYears[round] = _Scores[round].Select(s => CHighscoreStats.GetSeasonYear(s)).Distinct().OrderByDescending(y => y).ToList();
                }
            }
        }

        private void _UpdateRound()
        {
            _IsDuet = false;
            var points = CGame.GetPoints();

            CSong song;
            if (_FromScreenSong)
            {
                song = CSongs.GetSong(CScreenSong.getSelectedSongId());
            }
            else
            {
                song = CGame.GetSong(_Round);
            }

            if (song == null)
            {
                return;
            }

            _Texts[_TextSongName].Text = song.Artist + " - " + song.Title;
            if (points != null && !_FromScreenSong && points.NumRounds > 1)
            {
                _Texts[_TextSongName].Text += " (" + (_Round + 1) + "/" + points.NumRounds + ")";
            }

            switch (_FromScreenSong ? (EGameMode)_Round : CGame.GetGameMode(_Round))
            {
                case EGameMode.TR_GAMEMODE_NORMAL:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_NORMAL";
                    break;
                case EGameMode.TR_GAMEMODE_MEDLEY:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_MEDLEY";
                    break;
                case EGameMode.TR_GAMEMODE_DUET:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_DUET";
                    _IsDuet = true;
                    break;
                case EGameMode.TR_GAMEMODE_SHORTSONG:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_SHORTSONG";
                    break;
                default:
                    _Texts[_TextSongMode].Text = "TR_GAMEMODE_NORMAL";
                    break;
            }
        }

        private void _ChangeRound(int num)
        {
            if (_FromScreenSong)
            {
                if (num < 0)
                {
                    if (_Round == (int)EGameMode.TR_GAMEMODE_NORMAL) _Round = (int)EGameMode.TR_GAMEMODE_SHORTSONG;
                    else --_Round;
                }
                else
                {
                    if (_Round == (int)EGameMode.TR_GAMEMODE_SHORTSONG) _Round = (int)EGameMode.TR_GAMEMODE_NORMAL;
                    else ++_Round;
                }
            }
            else
            {
                var points = CGame.GetPoints();
                if (points != null)
                {
                    _Round += num;
                    _Round = _Round.Clamp(0, points.NumRounds - 1);
                }
            }
            _UpdateRound();
            _SeasonYear = CHighscoreStats.GetCurrentSeasonYear();
            _NeedsRefresh = true;
            UpdateGame();
        }

        public override void OnClose()
        {
            base.OnClose();
        }

        private void _LeaveScreen()
        {
            if (_HighscoreStream != -1)
            {
                CSound.Close(_HighscoreStream);
                _HighscoreStream = -1;
            }
            CParty.LeavingHighscore();
        }
    }
}