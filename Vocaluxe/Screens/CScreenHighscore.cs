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
using Vocaluxe.Lib.Sound;

namespace Vocaluxe.Screens
{
    public class CScreenHighscore : CMenu
    {
        // Version number for theme files. Increment it, if you've changed something on the theme files!
        protected override int _ScreenVersion
        {
            get { return 4; }
        }

        private const int _NumCurrent = 6;
        private const int _NumRecord = 4;
        private const int _NumSeason = 5;

        private const string _TextSongName = "TextSongName";
        private const string _TextSongMode = "TextSongMode";
        
        // Quadrant 1: Current Performance & Song Record
        private const string _TextCurrentTitle = "TextCurrentTitle";
        private string[] _TextCurrentName;
        private string[] _TextCurrentScore;
        private string[] _TextCurrentRecord;
        private string[] _ParticleEffectNew;

        // Quadrant 2: Seasonal Leaderboard
        private const string _TextSeasonTitle = "TextSeasonTitle";
        private const string _TextSeasonSubTitle = "TextSeasonSubTitle";
        private string[] _TextSeasonRank;
        private string[] _TextSeasonScore;
        private string[] _TextSeasonName;
        private string[] _TextSeasonDiff;
        private string[] _TextSeasonDate;

        // Quadrant 3: Song Lore
        private const string _TextLoreTitle = "TextLoreTitle";
        private const string _TextLoreStat1 = "TextLoreStat1";
        private const string _TextLoreStat2 = "TextLoreStat2";
        private const string _TextLoreStat3 = "TextLoreStat3";
        private const string _TextLoreStat4 = "TextLoreStat4";

        // Quadrant 4: Club Highlight
        private const string _TextHighlightTitle = "TextHighlightTitle";
        private const string _TextHighlightBody = "TextHighlightBody";

        private List<SDBScoreEntry>[] _Scores;
        private List<int>[] _AvailableYears;
        private int _SeasonYear = DateTime.Now.Year;
        private List<int> _NewEntryIDs;
        private int _Round;
        private bool _IsDuet;
        private bool _FromScreenSong = false;
        private int _HighscoreStream = -1;
        private bool _HasPlayedHighscoreSound = false;

        private static HashSet<int> _SessionSongsSung = new HashSet<int>();
        private static int _SessionRecordsBroken = 0;
        
        private static int PlaySound(ESounds sound, int volume)
        {
            int streamId = CSound.PlaySound(sound, false);
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
                _TextSongName, _TextSongMode,
                _TextCurrentTitle,
                _TextSeasonTitle,
                _TextLoreTitle, _TextLoreStat1, _TextLoreStat2, _TextLoreStat3, _TextLoreStat4,
                "TextLoreStat1_Num", "TextLoreStat1_Label", "TextLoreStat2_Num", "TextLoreStat2_Label",
                _TextHighlightTitle, _TextHighlightBody
            };

            // Init Current Performance arrays
            _TextCurrentName = new string[_NumCurrent];
            _TextCurrentScore = new string[_NumCurrent];
            _TextCurrentRecord = new string[_NumCurrent];
            _ParticleEffectNew = new string[_NumCurrent];
            for (int i = 0; i < _NumCurrent; i++)
            {
                _TextCurrentName[i] = "TextCurrentName" + (i + 1);
                _TextCurrentScore[i] = "TextCurrentScore" + (i + 1);
                _TextCurrentRecord[i] = "TextCurrentRecord" + (i + 1);
                _ParticleEffectNew[i] = "ParticleEffectNew" + (i + 1);
                texts.Add(_TextCurrentName[i]);
                texts.Add(_TextCurrentScore[i]);
                texts.Add(_TextCurrentRecord[i]);
            }

            // Init Season arrays
            _TextSeasonRank = new string[_NumSeason];
            _TextSeasonScore = new string[_NumSeason];
            _TextSeasonName = new string[_NumSeason];
            _TextSeasonDiff = new string[_NumSeason];
            _TextSeasonDate = new string[_NumSeason];
            for (int i = 0; i < _NumSeason; i++)
            {
                _TextSeasonRank[i] = "TextSeasonRank" + (i + 1);
                _TextSeasonScore[i] = "TextSeasonScore" + (i + 1);
                _TextSeasonName[i] = "TextSeasonName" + (i + 1);
                _TextSeasonDiff[i] = "TextSeasonDiff" + (i + 1);
                _TextSeasonDate[i] = "TextSeasonDate" + (i + 1);
                texts.Add(_TextSeasonRank[i]);
                texts.Add(_TextSeasonScore[i]);
                texts.Add(_TextSeasonName[i]);
                texts.Add(_TextSeasonDiff[i]);
                texts.Add(_TextSeasonDate[i]);
            }
            texts.Add(_TextSeasonSubTitle);

            _ThemeTexts = texts.ToArray();
            _ThemeParticleEffects = _ParticleEffectNew;
            _ThemeStatics = new string[] { "StaticMenuBar", "StaticCardCurrent", "StaticCardSeason", "StaticCardLore", "StaticCardHighlight" };
            _NewEntryIDs = new List<int>();
        }

        public override void Draw()
        {
            base.Draw();

            if (_Texts != null && _Texts.ContainsKey(_TextCurrentTitle))
            {
                SColorF accent = _Texts[_TextCurrentTitle].Color;
                CDraw.DrawRect(accent, new SRectF(60, 140, 870, 4, -1f));
                CDraw.DrawRect(accent, new SRectF(990, 140, 870, 4, -1f));
                CDraw.DrawRect(accent, new SRectF(60, 620, 870, 4, -1f));
                CDraw.DrawRect(accent, new SRectF(990, 620, 870, 4, -1f));
            }
        }

        public override bool HandleInput(SKeyEvent keyEvent)
        {
            if (keyEvent.KeyPressed && !Char.IsControl(keyEvent.Unicode)) {}
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
                }
            }
            return true;
        }

        public override bool HandleMouse(SMouseEvent mouseEvent)
        {
            if (mouseEvent.LB)
                _LeaveScreen();
            if (mouseEvent.RB)
                _LeaveScreen();
            if (mouseEvent.Wheel != 0)
                _ChangeSeasonYear(mouseEvent.Wheel);
            return true;
        }

        private void _SetText(string key, string text, bool visible = true)
        {
            if (key != null && _Texts.ContainsKey(key))
            {
                _Texts[key].Visible = visible;
                if (visible && text != null)
                    _Texts[key].Text = text;
            }
        }

        public override bool UpdateGame()
        {
            if (_Round >= _Scores.Length || _Scores[_Round] == null)
                return true;

            _SetText(_TextCurrentTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_CURRENT_PERFORMANCE"));
            
            // For now hardcode season format, later use configuration
            _SetText(_TextSeasonTitle, _SeasonYear + "-" + (_SeasonYear + 1) + " " + CLanguage.Translate("TR_SCREENHIGHSCORE_SEASON_LEADERBOARD"));
            _SetText(_TextSeasonSubTitle, "(09/" + _SeasonYear + " - 08/" + (_SeasonYear + 1) + ")");
            _SetText(_TextLoreTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_SONG_LORE"));
            _SetText(_TextHighlightTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_CLUB_HIGHLIGHT"));

            _UpdateCurrentPerformance();
            _UpdateSeasonLeaderboard();
            _UpdateSongLore();
            _UpdateClubHighlight();

            return true;
        }

        private void _UpdateCurrentPerformance()
        {
            CPoints points = CGame.GetPoints();
            if (points != null && !_FromScreenSong && CScreenSong.GetAudioMode() != EAudioMode.TR_AUDIOMODE_KARAOKE)
            {
                _SetText(_TextCurrentTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_CURRENT_PERFORMANCE"));
                SPlayer[] players = points.GetPlayer(_Round, CGame.NumPlayers);
                for (int p = 0; p < _NumCurrent; p++)
                {
                    if (p < players.Length)
                    {
                        var player = players[p];
                        string name = CProfiles.GetPlayerName(player.ProfileID);
                        if (_IsDuet) name += " (P" + (player.VoiceNr + 1) + ")";
                        
                        _SetText(_TextCurrentName[p], name);
                        _SetText(_TextCurrentScore[p], player.Points.ToString("0"));

                        bool isNewRecord = false;
                        _SetText(_TextCurrentRecord[p], isNewRecord ? CLanguage.Translate("TR_SCREENHIGHSCORE_NEW_SONG_RECORD") : "");

                        if (_ParticleEffects.ContainsKey(_ParticleEffectNew[p]))
                        {
                            _ParticleEffects[_ParticleEffectNew[p]].Visible = isNewRecord;
                            if (isNewRecord && !_HasPlayedHighscoreSound)
                            {
                                _HighscoreStream = PlaySound(ESounds.Highscore, CConfig.SoundEffectVolume);
                                _HasPlayedHighscoreSound = true;
                            }
                        }
                    }
                    else
                    {
                        _SetText(_TextCurrentName[p], null, false);
                        _SetText(_TextCurrentScore[p], null, false);
                        _SetText(_TextCurrentRecord[p], null, false);
                        if (_ParticleEffects.ContainsKey(_ParticleEffectNew[p]))
                            _ParticleEffects[_ParticleEffectNew[p]].Visible = false;
                    }
                }
            }
            else
            {
                // From ScreenSong (Song Selection) or Karaoke Mode -> Show All-Time Top Hall of Fame!
                _SetText(_TextCurrentTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_ALLTIME_TOP"));
                var topScores = _Scores[_Round]
                    .GroupBy(s => s.Name + (_IsDuet ? s.VoiceNr.ToString() : ""))
                    .Select(g => g.First())
                    .OrderByDescending(s => s.Score)
                    .Take(_NumCurrent)
                    .ToList();

                for (int p = 0; p < _NumCurrent; p++)
                {
                    if (p < topScores.Count)
                    {
                        var entry = topScores[p];
                        string displayName = entry.Name + (_IsDuet ? " (P" + (entry.VoiceNr + 1) + ")" : "");
                        _SetText(_TextCurrentName[p], (p + 1) + ". " + displayName);
                        _SetText(_TextCurrentScore[p], entry.Score.ToString("D"));
                        _SetText(_TextCurrentRecord[p], null, false);
                    }
                    else
                    {
                        _SetText(_TextCurrentName[p], null, false);
                        _SetText(_TextCurrentScore[p], null, false);
                        _SetText(_TextCurrentRecord[p], null, false);
                    }
                    if (_ParticleEffects.ContainsKey(_ParticleEffectNew[p]))
                        _ParticleEffects[_ParticleEffectNew[p]].Visible = false;
                }
            }
        }

        private string _GetShortDifficulty(EGameDifficulty diff)
        {
            switch (diff)
            {
                case EGameDifficulty.TR_CONFIG_EASY:
                    return "[Easy]";
                case EGameDifficulty.TR_CONFIG_NORMAL:
                    return "[Norm.]";
                case EGameDifficulty.TR_CONFIG_HARD:
                    return "[Hard]";
                default:
                    return "[Norm.]";
            }
        }

        private static int GetSeasonYear(SDBScoreEntry entry)
        {
            DateTime date = new DateTime(entry.DateTicks);
            return (date.Month >= 9) ? date.Year : date.Year - 1;
        }

        private void _UpdateSeasonLeaderboard()
        {
            var seasonScores = _Scores[_Round]
                .Where(s => GetSeasonYear(s) == _SeasonYear)
                .GroupBy(s => s.Name)
                .Select(g => g.First())
                .OrderByDescending(s => s.Score)
                .ToList();

            for (int p = 0; p < _NumSeason; p++)
            {
                if (p < seasonScores.Count)
                {
                    var entry = seasonScores[p];
                    _SetText(_TextSeasonRank[p], "#" + (p + 1));
                    _SetText(_TextSeasonScore[p], entry.Score.ToString("D"));
                    _SetText(_TextSeasonName[p], entry.Name + (_IsDuet ? " (P" + (entry.VoiceNr + 1) + ")" : ""));
                    _SetText(_TextSeasonDiff[p], _GetShortDifficulty(entry.Difficulty));
                    _SetText(_TextSeasonDate[p], entry.Date);
                }
                else
                {
                    _SetText(_TextSeasonRank[p], null, false);
                    _SetText(_TextSeasonScore[p], null, false);
                    _SetText(_TextSeasonName[p], null, false);
                    _SetText(_TextSeasonDiff[p], null, false);
                    _SetText(_TextSeasonDate[p], null, false);
                }
            }
        }

        private void _UpdateSongLore()
        {
            var scores = _Scores[_Round];
            int totalScores = scores.Count;

            long lastTicks = -1;
            int uniquePerformances = 0;
            var sortedScores = scores.OrderBy(s => s.DateTicks).ToList();
            foreach (var s in sortedScores)
            {
                if (lastTicks == -1 || Math.Abs(s.DateTicks - lastTicks) > TimeSpan.TicksPerSecond * 10)
                {
                    uniquePerformances++;
                }
                lastTicks = s.DateTicks;
            }

            // Column 1: PERFORMANCES HISTORY
            if (uniquePerformances == 0)
            {
                _SetText("TextLoreStat1_Num", "0");
                _SetText(_TextLoreStat1, CLanguage.Translate("TR_SCREENHIGHSCORE_NEVER_SUNG"));
                _SetText(_TextLoreStat4, null, false);
            }
            else
            {
                _SetText("TextLoreStat1_Num", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_TIMES_SUNG"), uniquePerformances));

                if (totalScores > uniquePerformances)
                {
                    _SetText(_TextLoreStat1, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_SCORES_COUNT"), totalScores));
                }
                else
                {
                    _SetText(_TextLoreStat1, null, false);
                }

                int seasonCount = scores.Count(s => GetSeasonYear(s) == _SeasonYear);
                if (seasonCount > 0)
                {
                    _SetText(_TextLoreStat4, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_SEASON_PERFORMANCES"), seasonCount));
                }
                else
                {
                    _SetText(_TextLoreStat4, null, false);
                }
            }

            // Column 2: ALL-TIME RECORD
            var allTimeBest = scores.OrderByDescending(s => s.Score).FirstOrDefault();
            if (allTimeBest.Name != null)
            {
                _SetText("TextLoreStat2_Num", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_RECORD_POINTS"), allTimeBest.Score.ToString("N0")));
                string recordHolderName = allTimeBest.Name + (_IsDuet ? " (P" + (allTimeBest.VoiceNr + 1) + ")" : "");
                _SetText(_TextLoreStat2, recordHolderName);

                DateTime recordDate = new DateTime(allTimeBest.DateTicks);
                int ageDays = (int)(DateTime.Now - recordDate).TotalDays;
                if (ageDays <= 0)
                {
                    _SetText(_TextLoreStat3, CLanguage.Translate("TR_SCREENHIGHSCORE_SET_TODAY"));
                }
                else
                {
                    _SetText(_TextLoreStat3, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_SET_DAYS_AGO"), ageDays, allTimeBest.Date));
                }
            }
            else
            {
                _SetText("TextLoreStat2_Num", null, false);
                _SetText(_TextLoreStat2, null, false);
                _SetText(_TextLoreStat3, null, false);
            }
        }

        private void _UpdateClubHighlight()
        {
            var scores = _Scores[_Round];
            var sortedScores = scores.OrderBy(s => s.DateTicks).ToList();

            // Candidate 1: Long Drought Broken (>= 30 days)
            var pastScores = sortedScores.Where(s => (DateTime.Now - new DateTime(s.DateTicks)).TotalHours > 12).OrderByDescending(s => s.DateTicks).ToList();
            if (pastScores.Count > 0)
            {
                int daysAgo = (int)(DateTime.Now - new DateTime(pastScores.First().DateTicks)).TotalDays;
                if (daysAgo >= 30)
                {
                    _SetText(_TextHighlightBody, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_DROUGHT"), daysAgo));
                    return;
                }
            }

            // Candidate 2: Session Records Broken
            if (_SessionRecordsBroken > 0)
            {
                _SetText(_TextHighlightBody, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_SESSION_RECORDS"), _SessionRecordsBroken));
                return;
            }

            // Candidate 3: 1000th Performance Milestone
            int totalDbScores = CDataBase.GetTotalScoreCount();
            if (totalDbScores >= 1000 && (totalDbScores % 1000 <= 10))
            {
                int thousandMilestone = (totalDbScores / 1000) * 1000;
                _SetText(_TextHighlightBody, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_MILESTONE"), thousandMilestone));
                return;
            }

            // Candidate 4: Session Songs Sung
            if (_SessionSongsSung.Count > 1)
            {
                _SetText(_TextHighlightBody, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_SESSION_SONGS"), _SessionSongsSung.Count));
                return;
            }

            // Candidate 5: Total Club Performances Fallback
            _SetText(_TextHighlightBody, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_TOTAL_PERFORMANCES"), totalDbScores));
        }

        public override void OnShow()
        {
            base.OnShow();
            _HasPlayedHighscoreSound = false;
            _Round = 0;
            
            int currentMonth = DateTime.Now.Month;
            _SeasonYear = (currentMonth >= 9) ? DateTime.Now.Year : DateTime.Now.Year - 1;
            
            _NewEntryIDs.Clear();
            _AddScoresToDB();
            _LoadScores();
            _UpdateRound();

            UpdateGame();
        }

        private bool _IsNewEntry(int id)
        {
            return _NewEntryIDs.Any(t => t == id);
        }

        private void _AddScoresToDB()
        {
            CPoints points = CGame.GetPoints();
            if (points == null) return;
            if (CScreenSong.GetAudioMode() == EAudioMode.TR_AUDIOMODE_KARAOKE) return;

            for (int round = 0; round < points.NumRounds; round++)
            {
                SPlayer[] players = points.GetPlayer(round, CGame.NumPlayers);
                for (int p = 0; p < players.Length; p++)
                {
                    if (players[p].Points > CSettings.MinScoreForDB && players[p].SongFinished && !CProfiles.IsGuestProfile(players[p].ProfileID))
                    {
                        int id = CDataBase.AddScore(players[p]);
                        _NewEntryIDs.Add(id);
                    }
                }
            }

            if (!_FromScreenSong)
            {
                int currentSongID = CScreenSong.getSelectedSongID();
                if (currentSongID >= 0)
                    _SessionSongsSung.Add(currentSongID);
                _SessionRecordsBroken += _NewEntryIDs.Count;
            }
        }

        private void _LoadScores()
        {
            int rounds = CGame.NumRounds;

            if (rounds == 0)
            {
                _FromScreenSong = true;
                _Round = (int)EGameMode.TR_GAMEMODE_NORMAL;
                _Scores = new List<SDBScoreEntry>[4];
                _AvailableYears = new List<int>[4];
                int songID = CScreenSong.getSelectedSongID();
                bool foundHighscoreEntries = false;

                for (int gameModeNum = 0; gameModeNum < 4; gameModeNum++)
                {
                    _Scores[gameModeNum] = CDataBase.LoadScore(songID, (EGameMode)gameModeNum, EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL) ?? new List<SDBScoreEntry>();
                    _AvailableYears[gameModeNum] = _Scores[gameModeNum].Select(s => GetSeasonYear(s)).Distinct().OrderByDescending(y => y).ToList();

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
                for (int round = 0; round < rounds; round++)
                {
                    int songID = CGame.GetSong(round).ID;
                    EGameMode gameMode = CGame.GetGameMode(round);
                    _Scores[round] = CDataBase.LoadScore(songID, gameMode, EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL) ?? new List<SDBScoreEntry>();
                    _AvailableYears[round] = _Scores[round].Select(s => GetSeasonYear(s)).Distinct().OrderByDescending(y => y).ToList();
                }
            }
        }

        private void _UpdateRound()
        {
            _IsDuet = false;
            CPoints points = CGame.GetPoints();
            
            CSong song;
            if (_FromScreenSong) song = CSongs.GetSong(CScreenSong.getSelectedSongID());
            else song = CGame.GetSong(_Round);

            if (song == null) return;

            _Texts[_TextSongName].Text = song.Artist + " - " + song.Title;
            if (points != null && !_FromScreenSong && points.NumRounds > 1)
                _Texts[_TextSongName].Text += " (" + (_Round + 1) + "/" + points.NumRounds + ")";

            switch ((_FromScreenSong ? (EGameMode)_Round : CGame.GetGameMode(_Round)))
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

        private void _ChangeSeasonYear(int dir)
        {
            if (_AvailableYears == null || _Round >= _AvailableYears.Length || _AvailableYears[_Round] == null) return;

            var years = new List<int>(_AvailableYears[_Round]);
            int currentYear = (DateTime.Now.Month >= 9) ? DateTime.Now.Year : DateTime.Now.Year - 1;
            if (!years.Contains(currentYear)) years.Add(currentYear);
            years = years.Distinct().OrderByDescending(y => y).ToList();

            if (years.Count <= 1) return;

            int currentIndex = years.IndexOf(_SeasonYear);
            if (currentIndex == -1) currentIndex = 0;

            int newIndex = (currentIndex - dir).Clamp(0, years.Count - 1);
            if (newIndex != currentIndex)
            {
                _SeasonYear = years[newIndex];
                UpdateGame();
            }
        }

        private void _ChangeRound(int num)
        {
            if (_FromScreenSong)
            {
                if (_Round == (int)EGameMode.TR_GAMEMODE_SHORTSONG) _Round = (int)EGameMode.TR_GAMEMODE_NORMAL;
                else ++_Round;
            }
            else
            {
                CPoints points = CGame.GetPoints();
                if (points != null)
                {
                    _Round += num;
                    _Round = _Round.Clamp(0, points.NumRounds - 1);
                }
            }
            _UpdateRound();
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
