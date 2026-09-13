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
            get { return 6; }
        }

        private const int _NumLeaderboard = 13;

        private const string _TextSongName = "TextSongName";
        private const string _TextSongMode = "TextSongMode";
        
        // Left Pane - Unified Master Leaderboard (13 rows)
        private const string _TextLeaderboardTitle = "TextLeaderboardTitle";
        private const string _TextLeaderboardSubTitle = "TextLeaderboardSubTitle";
        private string[] _TextLeaderboardRank;
        private string[] _TextLeaderboardName;
        private string[] _TextLeaderboardScore;
        private string[] _TextLeaderboardTag;
        private string[] _TextLeaderboardDate;
        private string[] _ParticleEffectLeaderboard;

        // Top-Right: Song Lore
        private const string _TextLoreTitle = "TextLoreTitle";
        private const string _TextLoreStat1 = "TextLoreStat1";
        private const string _TextLoreStat2 = "TextLoreStat2";
        private const string _TextLoreStat3 = "TextLoreStat3";
        private const string _TextLoreStat4 = "TextLoreStat4";

        // Bottom-Right: Club Visualization Panel
        private const string _TextHighlightTitle = "TextHighlightTitle";
        private const string _TextHighlightBody = "TextHighlightBody";
        private string[] _TextChartName;
        private string[] _TextChartValue;
        private EHighscoreChartMode _ChartMode = EHighscoreChartMode.Popular5Months;

        // Color Palettes: Green for this/latest run, Blue for season, White default
        private static readonly SColorF _ColorSession = new SColorF(0.18f, 0.90f, 0.45f, 1f); // Vibrant Emerald/Mint (Green)
        private static readonly SColorF _ColorSeason = new SColorF(0.25f, 0.80f, 1.00f, 1f);  // Electric Cyan (Blue)
        private static readonly SColorF _ColorNormal = new SColorF(0.96f, 0.96f, 0.98f, 1f);  // Clean Crisp White (Default)

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
                _TextLeaderboardTitle, _TextLeaderboardSubTitle,
                _TextLoreTitle, _TextLoreStat1, _TextLoreStat2, _TextLoreStat3, _TextLoreStat4,
                "TextLoreStat1_Num", "TextLoreStat1_Label", "TextLoreStat2_Num", "TextLoreStat2_Label", "TextLoreFact",
                _TextHighlightTitle, _TextHighlightBody
            };

            // Init Unified Leaderboard arrays (13 rows)
            _TextLeaderboardRank = new string[_NumLeaderboard];
            _TextLeaderboardName = new string[_NumLeaderboard];
            _TextLeaderboardScore = new string[_NumLeaderboard];
            _TextLeaderboardTag = new string[_NumLeaderboard];
            _TextLeaderboardDate = new string[_NumLeaderboard];
            _ParticleEffectLeaderboard = new string[_NumLeaderboard];

            for (int i = 0; i < _NumLeaderboard; i++)
            {
                _TextLeaderboardRank[i] = "TextLeaderboardRank" + (i + 1);
                _TextLeaderboardName[i] = "TextLeaderboardName" + (i + 1);
                _TextLeaderboardScore[i] = "TextLeaderboardScore" + (i + 1);
                _TextLeaderboardTag[i] = "TextLeaderboardTag" + (i + 1);
                _TextLeaderboardDate[i] = "TextLeaderboardDate" + (i + 1);
                _ParticleEffectLeaderboard[i] = "ParticleEffectLeaderboard" + (i + 1);

                texts.Add(_TextLeaderboardRank[i]);
                texts.Add(_TextLeaderboardName[i]);
                texts.Add(_TextLeaderboardScore[i]);
                texts.Add(_TextLeaderboardTag[i]);
                texts.Add(_TextLeaderboardDate[i]);
            }

            // Init Chart arrays (up to 5)
            _TextChartName = new string[5];
            _TextChartValue = new string[5];
            for (int i = 0; i < 5; i++)
            {
                _TextChartName[i] = "TextChartName" + (i + 1);
                _TextChartValue[i] = "TextChartValue" + (i + 1);
                texts.Add(_TextChartName[i]);
                texts.Add(_TextChartValue[i]);
            }

            _ThemeTexts = texts.ToArray();
            _ThemeParticleEffects = _ParticleEffectLeaderboard;
            _ThemeStatics = new string[] { "StaticMenuBar", "StaticCardCurrent", "StaticCardLore", "StaticCardHighlight" };
            _NewEntryIDs = new List<int>();
        }

        public override void Draw()
        {
            base.Draw();

            SChartData chartData = CHighscoreChart.GetChartData(_ChartMode);
            CHighscoreChart.DrawChartBars(chartData, _ChartMode);

            // Draw chart texts ON TOP of the bars so they blend properly without depth buffer occlusion
            if (chartData != null && chartData.Rows != null)
            {
                for (int i = 0; i < chartData.Rows.Count && i < 5; i++)
                {
                    if (_Texts != null)
                    {
                        if (_Texts.ContainsKey(_TextChartName[i]))
                            _Texts[_TextChartName[i]].DrawRelative(0, 0);
                        if (_Texts.ContainsKey(_TextChartValue[i]))
                            _Texts[_TextChartValue[i]].DrawRelative(0, 0);
                    }
                }
            }

            // Draw sleek framing accent lines for panels
            if (_Texts != null)
            {
                // Top border for Left Pane (Emerald/Gold gradient accent)
                CDraw.DrawRect(new SColorF(0.18f, 0.85f, 0.55f, 0.95f), new SRectF(40, 140, 980, 4, -1f));

                // Top-Right Pane (Song Lore) top accent line
                CDraw.DrawRect(new SColorF(0.85f, 0.45f, 0.95f, 0.95f), new SRectF(1060, 140, 820, 4, -1f));

                // Bottom-Right Pane (Visualization Chart) top accent line
                CDraw.DrawRect(new SColorF(0.95f, 0.65f, 0.20f, 0.95f), new SRectF(1060, 620, 820, 4, -1f));
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
            if (mouseEvent.LB)
                _LeaveScreen();
            if (mouseEvent.RB)
                _LeaveScreen();
            if (mouseEvent.Wheel != 0)
                _ChangeSeasonYear(mouseEvent.Wheel);
            return true;
        }

        private void _SetText(string key, string text, bool visible = true, SColorF? color = null)
        {
            if (key != null && _Texts != null && _Texts.ContainsKey(key))
            {
                _Texts[key].Visible = visible;
                if (visible && text != null)
                {
                    _Texts[key].Text = text;
                    if (color.HasValue)
                        _Texts[key].Color = color.Value;
                }
            }
        }

        public override bool UpdateGame()
        {
            if (_Round >= _Scores.Length || _Scores[_Round] == null)
                return true;

            _SetText(_TextLoreTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_SONG_LORE"));
            _SetText(_TextHighlightTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_CLUB_HIGHLIGHT"));

            _UpdateLeaderboard();
            _UpdateSongLore();
            _UpdateChart();

            return true;
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

        private class SDisplayRow
        {
            public int Rank;
            public bool ShowRank;
            public string Name;
            public int Score;
            public string Tag;
            public string Date;
            public SColorF Color;
            public bool HasParticles;
            public bool IsSession;
            public int VoiceNr;
        }

        private void _UpdateLeaderboard()
        {
            _SetText(_TextLeaderboardTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_LEADERBOARD"), true, new SColorF(1f, 1f, 1f, 1f));
            string seasonRange = "09/" + _SeasonYear + "-08/" + (_SeasonYear + 1);
            _SetText(_TextLeaderboardSubTitle, seasonRange + " " + CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON") + " (▲/▼)", true, new SColorF(0.6f, 0.85f, 0.95f, 0.85f));

            var scores = _Scores[_Round] ?? new List<SDBScoreEntry>();
            var priorScores = scores.Where(s => !_IsNewEntry(s.ID)).ToList();

            // 1. Identify active session runs / players
            var sessionRows = new List<SDisplayRow>();
            CPoints points = CGame.GetPoints();

            if (points != null && !_FromScreenSong && CScreenSong.GetAudioMode() != EAudioMode.TR_AUDIOMODE_KARAOKE)
            {
                SPlayer[] players = points.GetPlayer(_Round, CGame.NumPlayers);
                for (int p = 0; p < players.Length; p++)
                {
                    var player = players[p];
                    string name = CProfiles.GetPlayerName(player.ProfileID);
                    if (_IsDuet || CGame.NumPlayers > 1) name += " (P" + (player.VoiceNr + 1) + ")";
                    int score = (int)player.Points;

                    // Check if player's score is an all-time record for their mic/voice line
                    var voiceScores = priorScores.Where(s => !_IsDuet || s.VoiceNr == player.VoiceNr).ToList();
                    int prevVoiceRecord = voiceScores.Count > 0 ? voiceScores.Max(s => s.Score) : 0;
                    bool isAllTimeMicRecord = score > prevVoiceRecord && score > CSettings.MinScoreForDB;

                    string tag = isAllTimeMicRecord ? ("★ " + CLanguage.Translate("TR_SCREENHIGHSCORE_NEW_SONG_RECORD") + " ★") : CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SESSION");
                    SColorF color = _ColorSession;

                    sessionRows.Add(new SDisplayRow
                    {
                        Name = name,
                        Score = score,
                        Tag = tag,
                        Date = DateTime.Now.ToString("dd/MM/yyyy"),
                        Color = color,
                        HasParticles = isAllTimeMicRecord,
                        IsSession = true,
                        VoiceNr = player.VoiceNr
                    });

                    if (isAllTimeMicRecord && !_HasPlayedHighscoreSound)
                    {
                        _HighscoreStream = PlaySound(ESounds.Highscore, CConfig.SoundEffectVolume);
                        _HasPlayedHighscoreSound = true;
                    }
                }
            }
            else if (_FromScreenSong && scores.Count > 0)
            {
                // From song selection: identify the latest session entries
                long latestTicks = scores.Max(s => s.DateTicks);
                var latestSession = scores
                    .Where(s => Math.Abs(s.DateTicks - latestTicks) < TimeSpan.TicksPerSecond * 15)
                    .OrderByDescending(s => s.Score)
                    .ToList();

                foreach (var entry in latestSession)
                {
                    string displayName = entry.Name + (_IsDuet ? " (P" + (entry.VoiceNr + 1) + ")" : "");
                    sessionRows.Add(new SDisplayRow
                    {
                        Name = displayName,
                        Score = entry.Score,
                        Tag = CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SESSION"),
                        Date = entry.Date,
                        Color = _ColorSession,
                        HasParticles = false,
                        IsSession = true,
                        VoiceNr = entry.VoiceNr
                    });
                }
            }

            int allTimeRecord = scores.Count > 0 ? scores.Max(s => s.Score) : 0;
            Func<SDBScoreEntry, string> getMicKey = s => s.Name + (_IsDuet ? " (P" + (s.VoiceNr + 1) + ")" : "");

            var candidateRows = new List<SDisplayRow>();

            // (A) Session rows first (Green)
            foreach (var sess in sessionRows)
            {
                candidateRows.Add(sess);
            }

            // (B) Seasonal bests for the selected season (Blue)
            var seasonScores = scores.Where(s => GetSeasonYear(s) == _SeasonYear).ToList();
            var seasonBests = seasonScores
                .GroupBy(getMicKey)
                .Select(g => g.OrderByDescending(s => s.Score).First())
                .ToList();

            foreach (var entry in seasonBests)
            {
                string displayName = getMicKey(entry);

                // Skip if already in candidateRows (e.g. from current session)
                if (candidateRows.Any(r => r.Name == displayName && r.Score == entry.Score && r.Date == entry.Date))
                    continue;

                candidateRows.Add(new SDisplayRow
                {
                    Name = displayName,
                    Score = entry.Score,
                    Tag = CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON"),
                    Date = entry.Date,
                    Color = _ColorSeason,
                    HasParticles = false,
                    IsSession = false,
                    VoiceNr = entry.VoiceNr
                });
            }

            // (C) All-time bests (Blue if this season, White default otherwise)
            var allTimeBests = scores
                .GroupBy(getMicKey)
                .Select(g => g.OrderByDescending(s => s.Score).First())
                .ToList();

            foreach (var entry in allTimeBests)
            {
                string displayName = getMicKey(entry);

                // Skip if already in candidateRows (e.g. from session or season)
                if (candidateRows.Any(r => r.Name == displayName && r.Score == entry.Score && r.Date == entry.Date))
                    continue;

                bool isSeason = GetSeasonYear(entry) == _SeasonYear;
                SColorF color = isSeason ? _ColorSeason : _ColorNormal;
                string tag = isSeason ? CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON") : (entry.Difficulty != EGameDifficulty.TR_CONFIG_NORMAL ? _GetShortDifficulty(entry.Difficulty) : "");

                candidateRows.Add(new SDisplayRow
                {
                    Name = displayName,
                    Score = entry.Score,
                    Tag = tag,
                    Date = entry.Date,
                    Color = color,
                    HasParticles = false,
                    IsSession = false,
                    VoiceNr = entry.VoiceNr
                });
            }

            // Sort all candidates by score descending
            candidateRows = candidateRows.OrderByDescending(r => r.Score).ToList();

            // Compute absolute rank in the song (per mic line) and filter to top 50%
            for (int i = 0; i < candidateRows.Count; i++)
            {
                var row = candidateRows[i];

                var voiceScores = scores
                    .Where(s => !_IsDuet || s.VoiceNr == row.VoiceNr)
                    .Select(s => s.Score)
                    .ToList();

                foreach (var sess in sessionRows.Where(s => !_IsDuet || s.VoiceNr == row.VoiceNr))
                {
                    if (!voiceScores.Contains(sess.Score))
                        voiceScores.Add(sess.Score);
                }

                int totalScores = voiceScores.Count;
                int absoluteRank = voiceScores.Count(sc => sc > row.Score) + 1;
                row.Rank = absoluteRank;
                row.ShowRank = totalScores == 0 || absoluteRank <= Math.Ceiling(totalScores * 0.5f);
            }

            // 3. Select rows to display in the 13 table rows (with Sticky Session Row support)
            var displayRows = new List<SDisplayRow>();

            if (candidateRows.Count <= _NumLeaderboard)
            {
                displayRows.AddRange(candidateRows);
            }
            else
            {
                // Check if any session rows ranked beyond the top 13
                var overflowSessionRows = candidateRows
                    .Skip(_NumLeaderboard)
                    .Where(r => r.IsSession)
                    .ToList();

                int normalSlots = _NumLeaderboard - overflowSessionRows.Count;
                displayRows.AddRange(candidateRows.Take(normalSlots));
                displayRows.AddRange(overflowSessionRows);
            }

            // 4. Render into UI texts and particles
            for (int i = 0; i < _NumLeaderboard; i++)
            {
                if (i < displayRows.Count)
                {
                    var row = displayRows[i];
                    string rankStr = row.ShowRank ? ("#" + row.Rank) : "";
                    _SetText(_TextLeaderboardRank[i], rankStr, row.ShowRank, row.Color);
                    _SetText(_TextLeaderboardName[i], row.Name, true, row.Color);
                    _SetText(_TextLeaderboardScore[i], row.Score.ToString("N0"), true, row.Color);
                    _SetText(_TextLeaderboardTag[i], row.Tag, true, row.Color);
                    _SetText(_TextLeaderboardDate[i], row.Date, true, new SColorF(row.Color.R * 0.9f, row.Color.G * 0.9f, row.Color.B * 0.9f, 0.85f));

                    if (_ParticleEffects.ContainsKey(_ParticleEffectLeaderboard[i]))
                        _ParticleEffects[_ParticleEffectLeaderboard[i]].Visible = row.HasParticles;
                }
                else if (i == 0 && displayRows.Count == 0)
                {
                    _SetText(_TextLeaderboardRank[0], null, false);
                    _SetText(_TextLeaderboardName[0], CLanguage.Translate("TR_SCREENHIGHSCORE_NO_SESSION"), true, new SColorF(0.6f, 0.7f, 0.6f, 0.8f));
                    _SetText(_TextLeaderboardScore[0], null, false);
                    _SetText(_TextLeaderboardTag[0], null, false);
                    _SetText(_TextLeaderboardDate[0], null, false);
                    if (_ParticleEffects.ContainsKey(_ParticleEffectLeaderboard[0]))
                        _ParticleEffects[_ParticleEffectLeaderboard[0]].Visible = false;
                }
                else
                {
                    _SetText(_TextLeaderboardRank[i], null, false);
                    _SetText(_TextLeaderboardName[i], null, false);
                    _SetText(_TextLeaderboardScore[i], null, false);
                    _SetText(_TextLeaderboardTag[i], null, false);
                    _SetText(_TextLeaderboardDate[i], null, false);
                    if (_ParticleEffects.ContainsKey(_ParticleEffectLeaderboard[i]))
                        _ParticleEffects[_ParticleEffectLeaderboard[i]].Visible = false;
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
            // Fun Fact Line at Bottom of Card 3 (Song Lore)
            _UpdateSongLoreFact(scores);
        }

        private void _UpdateSongLoreFact(List<SDBScoreEntry> scores)
        {
            var sortedScores = (scores ?? new List<SDBScoreEntry>()).OrderBy(s => s.DateTicks).ToList();
            int totalDbScores = CDataBase.GetTotalScoreCount();

            // Priority 1: Overall Club Milestones (Every 1,000 performances: 1000, 2000...) - Highest Priority!
            if (totalDbScores >= 1000 && (totalDbScores % 1000 <= 10))
            {
                int thousandMilestone = (totalDbScores / 1000) * 1000;
                _SetText("TextLoreFact", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_MILESTONE"), thousandMilestone));
                return;
            }

            // Priority 2: Long Drought Broken (>= 30 days since song was last performed)
            if (sortedScores.Count > 0)
            {
                var pastScores = sortedScores.Where(s => (DateTime.Now - new DateTime(s.DateTicks)).TotalHours > 12).OrderByDescending(s => s.DateTicks).ToList();
                if (pastScores.Count > 0)
                {
                    int daysAgo = (int)(DateTime.Now - new DateTime(pastScores.First().DateTicks)).TotalDays;
                    if (daysAgo >= 30)
                    {
                        _SetText("TextLoreFact", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_DROUGHT"), daysAgo));
                        return;
                    }
                }
            }

            // Priority 3: Session Records Broken
            if (_SessionRecordsBroken > 0)
            {
                _SetText("TextLoreFact", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_SESSION_RECORDS"), _SessionRecordsBroken));
                return;
            }

            // Priority 4: Session Songs Milestone (Only every 50 songs: 50, 100, 150...)
            if (_SessionSongsSung.Count >= 50 && (_SessionSongsSung.Count % 50 == 0))
            {
                _SetText("TextLoreFact", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_SESSION_SONGS"), _SessionSongsSung.Count));
                return;
            }

            // Priority 5: First Performance Date at Club (if > 7 days ago)
            if (sortedScores.Count > 0)
            {
                var earliest = sortedScores.First();
                int daysSinceFirst = (int)(DateTime.Now - new DateTime(earliest.DateTicks)).TotalDays;
                if (daysSinceFirst > 7)
                {
                    _SetText("TextLoreFact", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_FACT_FIRST_PERFORMED"), earliest.Date, daysSinceFirst));
                    return;
                }
            }

            // Priority 6: Total Club Performances Fallback
            _SetText("TextLoreFact", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_TOTAL_PERFORMANCES"), totalDbScores));
        }

        private void _UpdateChart()
        {
            SChartData data = CHighscoreChart.GetChartData(_ChartMode);
            _SetText(_TextHighlightTitle, CLanguage.Translate(data.TitleKey));
            _SetText(_TextHighlightBody, null, false);

            for (int i = 0; i < 5; i++)
            {
                if (i < data.Rows.Count)
                {
                    if (_Texts != null)
                    {
                        if (_Texts.ContainsKey(_TextChartName[i]))
                        {
                            _Texts[_TextChartName[i]].Text = data.Rows[i].Label;
                            _Texts[_TextChartName[i]].Visible = false;
                        }
                        if (_Texts.ContainsKey(_TextChartValue[i]))
                        {
                            _Texts[_TextChartValue[i]].Text = data.Rows[i].ValueText;
                            _Texts[_TextChartValue[i]].Visible = false;
                        }
                    }
                }
                else
                {
                    if (_Texts != null)
                    {
                        if (_Texts.ContainsKey(_TextChartName[i]))
                        {
                            _Texts[_TextChartName[i]].Text = "";
                            _Texts[_TextChartName[i]].Visible = false;
                        }
                        if (_Texts.ContainsKey(_TextChartValue[i]))
                        {
                            _Texts[_TextChartValue[i]].Text = "";
                            _Texts[_TextChartValue[i]].Visible = false;
                        }
                    }
                }
            }
        }

        private void _CycleChartMode()
        {
            _ChartMode = (EHighscoreChartMode)(((int)_ChartMode + 1) % 3);
            _UpdateChart();
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
            UpdateGame();
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
