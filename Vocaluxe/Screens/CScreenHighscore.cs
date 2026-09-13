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
            get { return 11; }
        }

        private const int _NumLeaderboard = 12;

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
        private EHighscoreChartMode _ChartMode = EHighscoreChartMode.SeasonalPlays;
        private bool _NeedsRefresh = true;
        private SChartData _CachedChartData;
        private SChartLayout _CachedChartLayout;

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

            // Init Chart arrays (up to NumChartRows)
            _TextChartName = new string[CHighscoreChart.NumChartRows];
            _TextChartValue = new string[CHighscoreChart.NumChartRows];
            for (int i = 0; i < CHighscoreChart.NumChartRows; i++)
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

            SRectF? chartCardRect = (_Statics != null && _Statics.ContainsKey("StaticCardHighlight"))
                ? (SRectF?)_Statics["StaticCardHighlight"].Rect
                : null;

            if (_CachedChartData != null && _CachedChartData.Rows != null && _CachedChartData.Rows.Count > 0)
            {
                CHighscoreChart.DrawChart(_CachedChartData, _CachedChartLayout, chartCardRect, _Texts, _TextChartName, _TextChartValue);
            }

            _DrawCardAccents();
        }

        private void _DrawCardAccents()
        {
            if (_Statics == null)
                return;

            // Accent lines dynamically anchored to the top of theme card containers
            if (_Statics.ContainsKey("StaticCardCurrent"))
            {
                var r = _Statics["StaticCardCurrent"].Rect;
                CDraw.DrawRect(new SColorF(0.18f, 0.85f, 0.55f, 0.95f), new SRectF(r.X, r.Y, r.W, 4, -1f));
            }

            if (_Statics.ContainsKey("StaticCardLore"))
            {
                var r = _Statics["StaticCardLore"].Rect;
                CDraw.DrawRect(new SColorF(0.85f, 0.45f, 0.95f, 0.95f), new SRectF(r.X, r.Y, r.W, 4, -1f));
            }

            if (_Statics.ContainsKey("StaticCardHighlight"))
            {
                var r = _Statics["StaticCardHighlight"].Rect;
                CDraw.DrawRect(new SColorF(0.95f, 0.65f, 0.20f, 0.95f), new SRectF(r.X, r.Y, r.W, 4, -1f));
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
            if (_Scores == null || _Round >= _Scores.Length || _Scores[_Round] == null)
                return true;

            if (!_NeedsRefresh)
                return true;

            _NeedsRefresh = false;

            _SetText(_TextLoreTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_SONG_LORE"));
            _SetText(_TextHighlightTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_CLUB_HIGHLIGHT"));

            _UpdateLeaderboard();
            _UpdateSongLore();
            _UpdateChart();

            return true;
        }

        private void _UpdateLeaderboard()
        {
            _SetText(_TextLeaderboardTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_LEADERBOARD"), true, new SColorF(1f, 1f, 1f, 1f));
            string seasonRange = "09/" + _SeasonYear + "-08/" + (_SeasonYear + 1);
            _SetText(_TextLeaderboardSubTitle, seasonRange + " " + CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON") + " (▲/▼)", true, new SColorF(0.6f, 0.85f, 0.95f, 0.85f));

            var scores = _Scores[_Round] ?? new List<SDBScoreEntry>();
            var priorScores = scores.Where(s => !_IsNewEntry(s.ID)).ToList();

            // 1. Identify active session runs / players
            var sessionRows = new List<SLeaderboardRow>();
            CPoints points = CGame.GetPoints();

            if (points != null && !_FromScreenSong && CScreenSong.GetAudioMode() != EAudioMode.TR_AUDIOMODE_KARAOKE)
            {
                SPlayer[] players = points.GetPlayer(_Round, CGame.NumPlayers);
                for (int p = 0; p < players.Length; p++)
                {
                    var player = players[p];
                    string playerName = CProfiles.GetPlayerName(player.ProfileID);
                    string displayName = playerName + (_IsDuet ? " (P" + (player.VoiceNr + 1) + ")" : "");
                    int score = (int)Math.Round(player.Points);

                    // Check if player's score is an all-time record for their mic/voice line
                    var voiceScores = priorScores.Where(s => !_IsDuet || s.VoiceNr == player.VoiceNr).ToList();
                    int prevVoiceRecord = voiceScores.Count > 0 ? voiceScores.Max(s => s.Score) : 0;
                    bool isAllTimeMicRecord = score > prevVoiceRecord && score > CSettings.MinScoreForDB;

                    string tag = isAllTimeMicRecord ? ("★ " + CLanguage.Translate("TR_SCREENHIGHSCORE_NEW_SONG_RECORD") + " ★") : CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SESSION");
                    SColorF color = _ColorSession;

                    // Match with database entry that was just inserted in _AddScoresToDB()
                    int entryId = -1;
                    string dateStr = "";
                    var dbEntry = scores.FirstOrDefault(s => _NewEntryIDs.Contains(s.ID) && s.VoiceNr == player.VoiceNr && s.Name == playerName);
                    if (dbEntry.ID > 0)
                    {
                        entryId = dbEntry.ID;
                        score = dbEntry.Score;
                        dateStr = CHighscoreStats.FormatScoreDateTime(dbEntry);
                    }
                    else if (player.DateTicks > 0)
                    {
                        dateStr = CHighscoreStats.FormatScoreDateTime(new SDBScoreEntry { DateTicks = player.DateTicks });
                    }
                    else
                    {
                        dateStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                    }

                    sessionRows.Add(new SLeaderboardRow
                    {
                        ID = entryId,
                        Name = displayName,
                        Score = score,
                        Tag = tag,
                        Date = dateStr,
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
                // From song selection: identify recent session entries (only if played within the last 12 hours)
                long latestTicks = scores.Max(s => s.DateTicks);
                if (latestTicks > 0 && (DateTime.Now - new DateTime(latestTicks)).TotalHours < 12)
                {
                    var latestSession = scores
                        .Where(s => Math.Abs(s.DateTicks - latestTicks) < TimeSpan.TicksPerSecond * 15)
                        .OrderByDescending(s => s.Score)
                        .ToList();

                    foreach (var entry in latestSession)
                    {
                        string displayName = entry.Name + (_IsDuet ? " (P" + (entry.VoiceNr + 1) + ")" : "");
                        sessionRows.Add(new SLeaderboardRow
                        {
                            ID = entry.ID,
                            Name = displayName,
                            Score = entry.Score,
                            Tag = CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SESSION"),
                            Date = CHighscoreStats.FormatScoreDateTime(entry),
                            Color = _ColorSession,
                            HasParticles = false,
                            IsSession = true,
                            VoiceNr = entry.VoiceNr
                        });
                    }
                }
            }

            var displayRows = CHighscoreStats.BuildLeaderboardRows(
                scores,
                sessionRows,
                _SeasonYear,
                _IsDuet,
                _NumLeaderboard,
                _ColorSession,
                _ColorSeason,
                _ColorNormal);

            // Render into UI texts and particles
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
            int totalDbScores = CDataBase.GetTotalScoreCount();
            var info = CHighscoreStats.GetSongLoreInfo(scores, _SeasonYear, totalDbScores, _SessionRecordsBroken, _SessionSongsSung.Count);

            // Column 1: PERFORMANCES HISTORY
            if (info.UniquePerformances == 0)
            {
                _SetText("TextLoreStat1_Num", "0");
                _SetText(_TextLoreStat1, CLanguage.Translate("TR_SCREENHIGHSCORE_NEVER_SUNG"));
                _SetText(_TextLoreStat4, null, false);
            }
            else
            {
                _SetText("TextLoreStat1_Num", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_TIMES_SUNG"), info.UniquePerformances));

                if (info.TotalScores > info.UniquePerformances)
                {
                    _SetText(_TextLoreStat1, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_SCORES_COUNT"), info.TotalScores));
                }
                else
                {
                    _SetText(_TextLoreStat1, null, false);
                }

                if (info.SeasonCount > 0)
                {
                    _SetText(_TextLoreStat4, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_SEASON_PERFORMANCES"), info.SeasonCount));
                }
                else
                {
                    _SetText(_TextLoreStat4, null, false);
                }
            }

            // Column 2: ALL-TIME RECORD
            if (info.HasAllTimeBest)
            {
                _SetText("TextLoreStat2_Num", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_RECORD_POINTS"), info.AllTimeBest.Score.ToString("N0")));
                string recordHolderName = info.AllTimeBest.Name + (_IsDuet ? " (P" + (info.AllTimeBest.VoiceNr + 1) + ")" : "");
                _SetText(_TextLoreStat2, recordHolderName);

                if (info.RecordAgeDays <= 0)
                {
                    _SetText(_TextLoreStat3, CLanguage.Translate("TR_SCREENHIGHSCORE_SET_TODAY"));
                }
                else
                {
                    _SetText(_TextLoreStat3, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_SET_DAYS_AGO"), info.RecordAgeDays, info.AllTimeBest.Date));
                }
            }
            else
            {
                _SetText("TextLoreStat2_Num", null, false);
                _SetText(_TextLoreStat2, null, false);
                _SetText(_TextLoreStat3, null, false);
            }

            _SetText("TextLoreFact", info.FactText);
        }

        private void _UpdateChart()
        {
            var currentScores = (_Scores != null && _Round < _Scores.Length) ? _Scores[_Round] : new List<SDBScoreEntry>();
            _CachedChartData = CHighscoreChart.GetChartData(currentScores, _ChartMode, _SeasonYear);
            _SetText(_TextHighlightTitle, CLanguage.Translate(_CachedChartData.TitleKey));
            _SetText(_TextHighlightBody, null, false);

            SRectF? chartCardRect = (_Statics != null && _Statics.ContainsKey("StaticCardHighlight"))
                ? (SRectF?)_Statics["StaticCardHighlight"].Rect
                : null;

            if (_CachedChartData != null && _CachedChartData.Rows != null && _CachedChartData.Rows.Count > 0)
            {
                _CachedChartLayout = CHighscoreChart.GetLayout(_CachedChartData.Rows.Count, chartCardRect);

                for (int i = 0; i < CHighscoreChart.NumChartRows; i++)
                {
                    if (i < _CachedChartData.Rows.Count)
                    {
                        if (_Texts != null)
                        {
                            float y = _CachedChartLayout.StartY + i * _CachedChartLayout.RowPitch + (_CachedChartLayout.BarHeight - _CachedChartLayout.FontHeight) / 2f;
                            SColorF rowColor = _CachedChartData.Rows[i].IsSelectedSeason ? _ColorSeason : _ColorNormal;

                            if (_Texts.ContainsKey(_TextChartName[i]))
                            {
                                _Texts[_TextChartName[i]].Text = _CachedChartData.Rows[i].Label;
                                _Texts[_TextChartName[i]].Y = y;
                                _Texts[_TextChartName[i]].Font = new CFont(_Texts[_TextChartName[i]].Font.Name, _Texts[_TextChartName[i]].Font.Style, _CachedChartLayout.FontHeight);
                                _Texts[_TextChartName[i]].Color = rowColor;
                                _Texts[_TextChartName[i]].Visible = false;
                            }
                            if (_Texts.ContainsKey(_TextChartValue[i]))
                            {
                                _Texts[_TextChartValue[i]].Text = _CachedChartData.Rows[i].ValueText;
                                _Texts[_TextChartValue[i]].Y = y;
                                _Texts[_TextChartValue[i]].Font = new CFont(_Texts[_TextChartValue[i]].Font.Name, _Texts[_TextChartValue[i]].Font.Style, _CachedChartLayout.FontHeight);
                                _Texts[_TextChartValue[i]].Color = rowColor;
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
            else
            {
                for (int i = 0; i < CHighscoreChart.NumChartRows; i++)
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
            _ChartMode = (EHighscoreChartMode)(((int)_ChartMode + 1) % 2);
            _NeedsRefresh = true;
            UpdateGame();
        }

        public override void OnShow()
        {
            base.OnShow();
            _HasPlayedHighscoreSound = false;
            _Round = 0;
            _SeasonYear = CHighscoreStats.GetCurrentSeasonYear();
            _FromScreenSong = (CGame.NumRounds == 0);
            
            _NewEntryIDs.Clear();
            _AddScoresToDB();
            _LoadScores();
            _CountBrokenRecords();
            _UpdateRound();

            _NeedsRefresh = true;
            UpdateGame();
        }

        private bool _IsNewEntry(int id)
        {
            return _NewEntryIDs.Any(t => t == id);
        }

        private void _CountBrokenRecords()
        {
            if (_FromScreenSong) return;

            CPoints points = CGame.GetPoints();
            if (points == null) return;

            for (int round = 0; round < points.NumRounds; round++)
            {
                if (_Scores == null || round >= _Scores.Length || _Scores[round] == null) continue;

                var roundScores = _Scores[round];
                var priorScores = roundScores.Where(s => !_IsNewEntry(s.ID)).ToList();
                SPlayer[] players = points.GetPlayer(round, CGame.NumPlayers);
                bool isDuet = (CGame.GetGameMode(round) == EGameMode.TR_GAMEMODE_DUET);

                for (int p = 0; p < players.Length; p++)
                {
                    var player = players[p];
                    if (player.Points > CSettings.MinScoreForDB && player.SongFinished && !CProfiles.IsGuestProfile(player.ProfileID))
                    {
                        int score = (int)Math.Round(player.Points);
                        var voiceScores = priorScores.Where(s => !isDuet || s.VoiceNr == player.VoiceNr).ToList();
                        int prevVoiceRecord = voiceScores.Count > 0 ? voiceScores.Max(s => s.Score) : 0;
                        if (score > prevVoiceRecord)
                        {
                            _SessionRecordsBroken++;
                        }
                    }
                }
            }
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
                for (int round = 0; round < rounds; round++)
                {
                    int songID = CGame.GetSong(round).ID;
                    EGameMode gameMode = CGame.GetGameMode(round);
                    _Scores[round] = CDataBase.LoadScore(songID, gameMode, EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL) ?? new List<SDBScoreEntry>();
                    _AvailableYears[round] = _Scores[round].Select(s => CHighscoreStats.GetSeasonYear(s)).Distinct().OrderByDescending(y => y).ToList();
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
            int currentYear = CHighscoreStats.GetCurrentSeasonYear();
            if (!years.Contains(currentYear)) years.Add(currentYear);
            years = years.Distinct().OrderByDescending(y => y).ToList();

            if (years.Count <= 1) return;

            int currentIndex = years.IndexOf(_SeasonYear);
            if (currentIndex == -1) currentIndex = 0;

            int newIndex = (currentIndex - dir).Clamp(0, years.Count - 1);
            if (newIndex != currentIndex)
            {
                _SeasonYear = years[newIndex];
                _NeedsRefresh = true;
                UpdateGame();
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
                CPoints points = CGame.GetPoints();
                if (points != null)
                {
                    _Round += num;
                    _Round = _Round.Clamp(0, points.NumRounds - 1);
                }
            }
            _UpdateRound();
            _NeedsRefresh = true;
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
