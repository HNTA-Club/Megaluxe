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
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Game;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public partial class CScreenHighscore
    {
        private const int _NumLeaderboard = 12;

        // Left Pane - Unified Master Leaderboard (12 rows)
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
        private const string _TextLoreStat3 = "TextLoreStat3";
        private const string _TextLoreStat4 = "TextLoreStat4";

        // Top-Right: Difficulty Breakdown
        private const string _TextLoreDiffTitle = "TextLoreDiffTitle";
        private const string _TextLoreDiffOverall = "TextLoreDiffOverall";
        private const string _TextLoreDiffFooter = "TextLoreDiffFooter";
        private const string _TextLoreFact = "TextLoreFact";
        private string[] _TextLoreDiffName;
        private string[] _TextLoreDiffValue;
        private int _HoveredDifficultyAxis = -1;
        private string _DefaultLoreFact = String.Empty;
        private SColorF _DefaultLoreFactColor = new SColorF(1f, 1f, 1f, 1f);

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

        private List<int>[] _AvailableYears;
        private int _SeasonYear = CHighscoreStats.GetCurrentSeasonYear();

        private static HashSet<int> _SessionSongsSung = new HashSet<int>();
        private static int _SessionRecordsBroken = 0;

        private void _InitDashboard(List<string> texts)
        {
            texts.Add(_TextLeaderboardTitle);
            texts.Add(_TextLeaderboardSubTitle);
            texts.Add(_TextLoreTitle);
            texts.Add(_TextLoreStat1);
            texts.Add(_TextLoreStat3);
            texts.Add(_TextLoreStat4);
            texts.Add("TextLoreStat1_Num");
            texts.Add("TextLoreFact");
            texts.Add(_TextLoreDiffTitle);
            texts.Add(_TextLoreDiffOverall);
            texts.Add(_TextLoreDiffFooter);
            texts.Add(_TextHighlightTitle);
            texts.Add(_TextHighlightBody);

            // Init Unified Leaderboard arrays (12 rows)
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

            // Init Difficulty breakdown arrays (3 axes)
            _TextLoreDiffName = new string[CDifficultyChart.NumAxes];
            _TextLoreDiffValue = new string[CDifficultyChart.NumAxes];
            for (int i = 0; i < CDifficultyChart.NumAxes; i++)
            {
                _TextLoreDiffName[i] = "TextLoreDiffName" + (i + 1);
                _TextLoreDiffValue[i] = "TextLoreDiffValue" + (i + 1);
                texts.Add(_TextLoreDiffName[i]);
                texts.Add(_TextLoreDiffValue[i]);
            }

            // Init Chart arrays (up to NumChartRows)
            _TextChartName = new string[CHighscoreChart.NumChartRows];
            _TextChartValue = new string[CHighscoreChart.NumChartRows];
            for (var i = 0; i < CHighscoreChart.NumChartRows; i++)
            {
                _TextChartName[i] = "TextChartName" + (i + 1);
                _TextChartValue[i] = "TextChartValue" + (i + 1);
                texts.Add(_TextChartName[i]);
                texts.Add(_TextChartValue[i]);
            }

            _ThemeParticleEffects = _ParticleEffectLeaderboard;
            _ThemeStatics = new string[] { "StaticMenuBar", "StaticCardCurrent", "StaticCardLore", "StaticCardHighlight" };
        }

        private void _DrawDashboard()
        {
            SRectF? chartCardRect = (_Statics != null && _Statics.ContainsKey("StaticCardHighlight"))
                ? (SRectF?)_Statics["StaticCardHighlight"].Rect
                : null;

            if (_CachedChartData != null && _CachedChartData.Rows != null && _CachedChartData.Rows.Count > 0)
            {
                CHighscoreChart.DrawChart(_CachedChartData, _CachedChartLayout, chartCardRect, _Texts, _TextChartName, _TextChartValue);
            }

            CSong currentSong = _FromScreenSong ? CSongs.GetSong(CScreenSong.getSelectedSongId()) : CGame.GetSong(_Round);
            if (currentSong != null && currentSong.Difficulty.Overall >= 1.0f)
            {
                CDifficultyChart.DrawDifficultyBars(currentSong.Difficulty, _Texts, _TextLoreDiffName, _TextLoreDiffValue, hoveredAxis: _HoveredDifficultyAxis);
            }

            _DrawCardAccents(currentSong);
        }

        private void _UpdateDashboard()
        {
            _SetText(_TextLoreTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_SONG_LORE"));
            _SetText(_TextHighlightTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_CLUB_HIGHLIGHT"));

            _UpdateLeaderboard();
            _UpdateSongLore();
            _UpdateChart();
        }

        private void _DrawCardAccents(CSong curSong = null)
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
                SColorF accentCol = (curSong != null && curSong.Difficulty.Overall >= 1.0f)
                    ? CDifficultyChart.GetTierColor(curSong.Difficulty.Overall)
                    : new SColorF(0.85f, 0.45f, 0.95f, 0.95f);
                CDraw.DrawRect(accentCol, new SRectF(r.X, r.Y, r.W, 4, -1f));
            }

            if (_Statics.ContainsKey("StaticCardHighlight"))
            {
                var r = _Statics["StaticCardHighlight"].Rect;
                CDraw.DrawRect(new SColorF(0.95f, 0.65f, 0.20f, 0.95f), new SRectF(r.X, r.Y, r.W, 4, -1f));
            }
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
                    {
                        _Texts[key].Color = color.Value;
                    }
                }
            }
        }

        private void _UpdateLeaderboard()
        {
            _SetText(_TextLeaderboardTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_LEADERBOARD"), true, new SColorF(1f, 1f, 1f, 1f));
            string seasonRange = "09/" + _SeasonYear + "-08/" + (_SeasonYear + 1);
            _SetText(_TextLeaderboardSubTitle, seasonRange + " " + CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON") + " (▲/▼)", true, new SColorF(0.6f, 0.85f, 0.95f, 0.85f));

            var scores = _Scores[_Round] ?? new List<SDBScoreEntry>();
            var priorScores = scores.Where(s => !_IsNewEntry(s.Id)).ToList();

            // 1. Identify active session runs / players
            var sessionRows = new List<SLeaderboardRow>();
            CPoints points = CGame.GetPoints();

            if (points != null && !_FromScreenSong && CScreenSong.GetAudioMode() != EAudioMode.TR_AUDIOMODE_KARAOKE)
            {
                SPlayer[] players = points.GetPlayer(_Round, CGame.NumPlayers);
                for (int p = 0; p < players.Length; p++)
                {
                    var player = players[p];
                    string playerName = CProfiles.GetPlayerName(player.ProfileId);
                    string displayName = playerName + (_IsDuet ? " (P" + (player.VoiceNr + 1) + ")" : "");
                    int score = (int)Math.Round(player.Points);

                    // Check if player achieved a profile-specific personal best (earns sparkles!)
                    var playerPriorScores = priorScores.Where(s => s.Name == playerName && (!_IsDuet || s.VoiceNr == player.VoiceNr)).ToList();
                    int prevPlayerBest = playerPriorScores.Count > 0 ? playerPriorScores.Max(s => s.Score) : 0;
                    bool isPersonalHighscore = score > prevPlayerBest && score > CSettings.MinScoreForDB;

                    // Check if player's score is an all-time record for their mic/voice line (earns NEW RECORD tag!)
                    var voiceScores = priorScores.Where(s => !_IsDuet || s.VoiceNr == player.VoiceNr).ToList();
                    int prevVoiceRecord = voiceScores.Count > 0 ? voiceScores.Max(s => s.Score) : 0;
                    bool isAllTimeMicRecord = score > prevVoiceRecord && score > CSettings.MinScoreForDB;

                    string tag = isAllTimeMicRecord ? ("★ " + CLanguage.Translate("TR_SCREENHIGHSCORE_NEW_SONG_RECORD") + " ★") : CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SESSION");
                    SColorF color = _ColorSession;

                    // Match with database entry that was just inserted in _AddScoresToDB()
                    int entryId = -1;
                    string dateStr = "";
                    var dbEntry = scores.FirstOrDefault(s => _NewEntryIds.Contains(s.Id) && s.VoiceNr == player.VoiceNr && s.Name == playerName);
                    if (dbEntry.Id > 0)
                    {
                        entryId = dbEntry.Id;
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
                        HasParticles = isPersonalHighscore,
                        IsSession = true,
                        VoiceNr = player.VoiceNr
                    });

                    if (isPersonalHighscore && !_HasPlayedHighscoreSound)
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
                if (latestTicks > 0 && latestTicks <= DateTime.MaxValue.Ticks)
                {
                    double hoursAgo = (DateTime.Now - new DateTime(latestTicks)).TotalHours;
                    if (hoursAgo >= -0.5 && hoursAgo < 12)
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
                                ID = entry.Id,
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

            CSong song = _FromScreenSong ? CSongs.GetSong(CScreenSong.getSelectedSongId()) : CGame.GetSong(_Round);
            bool hasDifficulty = (song != null && song.Difficulty.Overall >= 1.0f);

            // Left Pane: Performance History & Record Info
            if (info.UniquePerformances == 0)
            {
                _SetText("TextLoreStat1_Num", "0");
                _SetText(_TextLoreStat1, CLanguage.Translate("TR_SCREENHIGHSCORE_NEVER_SUNG"));
                _SetText(_TextLoreStat4, null, false);
                _SetText(_TextLoreStat3, null, false);
            }
            else
            {
                // Line 1: A times
                _SetText("TextLoreStat1_Num", String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_TIMES_SUNG"), info.UniquePerformances));

                // Line 2: B individual scores (if multi-mic runs exist)
                if (info.TotalScores > info.UniquePerformances)
                {
                    _SetText(_TextLoreStat1, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_SCORES_COUNT"), info.TotalScores));
                }
                else
                {
                    _SetText(_TextLoreStat1, null, false);
                }

                // Line 3: Last sung C days ago (C-date)
                if (info.HasLastSung)
                {
                    if (info.LastSungAgeDays <= 0)
                    {
                        string todayText = info.UniquePerformances == 1
                            ? CLanguage.Translate("TR_SCREENHIGHSCORE_FIRST_SUNG_TODAY")
                            : CLanguage.Translate("TR_SCREENHIGHSCORE_LAST_SUNG_TODAY");
                        _SetText(_TextLoreStat4, todayText);
                    }
                    else
                    {
                        _SetText(_TextLoreStat4, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_LAST_SUNG_DAYS_AGO"), info.LastSungAgeDays, info.LastSungDate));
                    }
                }
                else
                {
                    _SetText(_TextLoreStat4, null, false);
                }

                // Line 4: Record set D days ago (D-date)
                if (info.HasAllTimeBest)
                {
                    if (info.RecordAgeDays <= 0)
                    {
                        _SetText(_TextLoreStat3, CLanguage.Translate("TR_SCREENHIGHSCORE_RECORD_SET_TODAY"));
                    }
                    else
                    {
                        _SetText(_TextLoreStat3, String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_RECORD_SET_DAYS_AGO"), info.RecordAgeDays, info.AllTimeBest.Date));
                    }
                }
                else
                {
                    _SetText(_TextLoreStat3, null, false);
                }
            }

            // Right Pane: Difficulty Breakdown
            if (hasDifficulty)
            {
                _SetText(_TextLoreDiffTitle, CLanguage.Translate("TR_SCREENHIGHSCORE_DIFFICULTY"));
                string tierKey = CDifficultyChart.GetTierNameKey(song.Difficulty.Overall);
                string tierName = CLanguage.Translate(tierKey);
                SColorF tierColor = CDifficultyChart.GetTierColor(song.Difficulty.Overall);
                _SetText(_TextLoreDiffOverall, "★ " + song.Difficulty.Overall.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "  [" + tierName.ToUpper() + "]", true, tierColor);

                string[] metricKeys = new string[]
                {
                    "TR_DIFFICULTY_AGILITY",
                    "TR_DIFFICULTY_RANGE",
                    "TR_DIFFICULTY_PACE"
                };

                float[] metricValues = new float[]
                {
                    song.Difficulty.Agility,
                    song.Difficulty.Range,
                    song.Difficulty.Pace
                };

                const float barStartY = CDifficultyChart.BarStartY;
                const float barPitch = CDifficultyChart.BarPitch;
                const float barH = CDifficultyChart.BarHeight;
                const float fontH = CDifficultyChart.FontHeight;

                for (int i = 0; i < CDifficultyChart.NumAxes; i++)
                {
                    float y = barStartY + i * barPitch + (barH - fontH) / 2f;
                    if (_Texts != null)
                    {
                        if (i < _TextLoreDiffName.Length && _Texts.ContainsKey(_TextLoreDiffName[i]))
                        {
                            _Texts[_TextLoreDiffName[i]].Text = CLanguage.Translate(metricKeys[i]).ToUpper();
                            _Texts[_TextLoreDiffName[i]].Y = y;
                            _Texts[_TextLoreDiffName[i]].Font = new CFont(_Texts[_TextLoreDiffName[i]].Font.Name, _Texts[_TextLoreDiffName[i]].Font.Style, fontH);
                            _Texts[_TextLoreDiffName[i]].Color = _ColorNormal;
                            _Texts[_TextLoreDiffName[i]].Visible = false;
                        }

                        if (i < _TextLoreDiffValue.Length && _Texts.ContainsKey(_TextLoreDiffValue[i]))
                        {
                            string valText = (metricKeys[i] != "TR_DIFFICULTY_PACE" && song.Difficulty.PitchedRatio == 0f) ? "—" : "★ " + metricValues[i].ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
                            _Texts[_TextLoreDiffValue[i]].Text = valText;
                            _Texts[_TextLoreDiffValue[i]].Y = y;
                            _Texts[_TextLoreDiffValue[i]].Font = new CFont(_Texts[_TextLoreDiffValue[i]].Font.Name, _Texts[_TextLoreDiffValue[i]].Font.Style, fontH);
                            _Texts[_TextLoreDiffValue[i]].Color = _ColorNormal;
                            _Texts[_TextLoreDiffValue[i]].Visible = false;
                        }
                    }
                }

                string footerText = "";
                if (song.Difficulty.PitchedRatio < 0.20f)
                {
                    footerText = CLanguage.Translate("TR_DIFFICULTY_RAP_FOOTER");
                }
                else
                {
                    string minNote = CDifficultyChart.FormatNoteName(song.Difficulty.P5Tone);
                    string maxNote = CDifficultyChart.FormatNoteName(song.Difficulty.P95Tone);
                    try
                    {
                        footerText = String.Format(CLanguage.Translate("TR_DIFFICULTY_RANGE_FOOTER"), minNote, maxNote, song.Difficulty.ToneSpan);
                    }
                    catch (FormatException)
                    {
                        footerText = minNote + " – " + maxNote + " (" + song.Difficulty.ToneSpan + " st)";
                    }

                    if ((_IsDuet || song.IsDuet) && song.Notes != null && song.Notes.VoiceCount >= 2)
                    {
                        CVoice voice0 = song.Notes.GetVoice(0);
                        CVoice voice1 = song.Notes.GetVoice(1);
                        if (voice0 != null && voice1 != null && voice0.Difficulty.Overall >= 1.0f && voice1.Difficulty.Overall >= 1.0f)
                        {
                            try
                            {
                                footerText += "  (" + String.Format(System.Globalization.CultureInfo.InvariantCulture, CLanguage.Translate("TR_DIFFICULTY_DUET_VOICES"), voice0.Difficulty.Overall, voice1.Difficulty.Overall) + ")";
                            }
                            catch (FormatException)
                            {
                                footerText += "  (P1: " + voice0.Difficulty.Overall.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ★ • P2: " + voice1.Difficulty.Overall.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ★)";
                            }
                        }
                    }
                }
                _SetText(_TextLoreDiffFooter, footerText);
            }
            else
            {
                _SetText(_TextLoreDiffTitle, null, false);
                _SetText(_TextLoreDiffOverall, null, false);
                _SetText(_TextLoreDiffFooter, null, false);
                _HoveredDifficultyAxis = -1;
                if (_TextLoreDiffName != null)
                {
                    for (int i = 0; i < _TextLoreDiffName.Length; i++)
                    {
                        if (_Texts != null)
                        {
                            if (i < _TextLoreDiffName.Length && _Texts.ContainsKey(_TextLoreDiffName[i]))
                            {
                                _Texts[_TextLoreDiffName[i]].Text = "";
                                _Texts[_TextLoreDiffName[i]].Visible = false;
                            }
                            if (i < _TextLoreDiffValue.Length && _Texts.ContainsKey(_TextLoreDiffValue[i]))
                            {
                                _Texts[_TextLoreDiffValue[i]].Text = "";
                                _Texts[_TextLoreDiffValue[i]].Visible = false;
                            }
                        }
                    }
                }
            }

            // Bottom Fact Text
            if (_Texts != null && _Texts.ContainsKey("TextLoreFact"))
            {
                _Texts["TextLoreFact"].X = 1100f;
                _Texts["TextLoreFact"].Y = 490f;
            }
            _DefaultLoreFact = info.FactText;
            if (_HoveredDifficultyAxis < 0)
            {
                _SetText("TextLoreFact", info.FactText, !String.IsNullOrEmpty(info.FactText), _DefaultLoreFactColor);
            }
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

        private void _CountBrokenRecords()
        {
            if (_FromScreenSong) return;

            var points = CGame.GetPoints();
            if (points == null) return;

            for (var round = 0; round < points.NumRounds; round++)
            {
                if (_Scores == null || round >= _Scores.Length || _Scores[round] == null) continue;

                var roundScores = _Scores[round];
                var priorScores = roundScores.Where(s => !_IsNewEntry(s.Id)).ToList();
                var players = points.GetPlayer(round, CGame.NumPlayers);
                var isDuet = (CGame.GetGameMode(round) == EGameMode.TR_GAMEMODE_DUET);

                // Group valid players by voice line so multi-player single-mic rounds only count at most 1 broken record
                var playerVoices = players
                    .Where(p => p.Points > CSettings.MinScoreForDB && p.SongFinished && !CProfiles.IsGuestProfile(p.ProfileId))
                    .GroupBy(p => isDuet ? p.VoiceNr : 0);

                foreach (var voiceGroup in playerVoices)
                {
                    var voiceNr = voiceGroup.Key;
                    var voiceScores = priorScores.Where(s => !isDuet || s.VoiceNr == voiceNr).ToList();
                    // Only count as broken if an existing prior record existed in the database
                    if (voiceScores.Count > 0)
                    {
                        var prevVoiceRecord = voiceScores.Max(s => s.Score);
                        var maxRoundScore = voiceGroup.Max(p => (int)Math.Round(p.Points));
                        if (maxRoundScore > prevVoiceRecord)
                        {
                            _SessionRecordsBroken++;
                        }
                    }
                }
            }
        }

        private void _ResetDifficultyHover()
        {
            _HoveredDifficultyAxis = -1;
        }

        private bool _HandleDifficultyHover(SMouseEvent mouseEvent)
        {
            CSong currentSong = _FromScreenSong ? CSongs.GetSong(CScreenSong.getSelectedSongId()) : CGame.GetSong(_Round);
            if (currentSong != null && currentSong.Difficulty.Overall >= 1.0f)
            {
                int axis = CDifficultyChart.GetHoveredAxis(mouseEvent);
                if (axis != _HoveredDifficultyAxis)
                {
                    _HoveredDifficultyAxis = axis;
                    if (_HoveredDifficultyAxis >= 0)
                    {
                        string[] explainKeys = new string[]
                        {
                            "TR_DIFFICULTY_EXPLAIN_AGILITY",
                            "TR_DIFFICULTY_EXPLAIN_RANGE",
                            "TR_DIFFICULTY_EXPLAIN_PACE"
                        };
                        if (_HoveredDifficultyAxis < explainKeys.Length)
                        {
                            _SetText(_TextLoreFact, CLanguage.Translate(explainKeys[_HoveredDifficultyAxis]), true, CDifficultyChart.GetAxisColor(_HoveredDifficultyAxis));
                        }
                    }
                    else
                    {
                        _SetText(_TextLoreFact, _DefaultLoreFact, !String.IsNullOrEmpty(_DefaultLoreFact), _DefaultLoreFactColor);
                    }
                }

                if (axis >= 0 && (mouseEvent.LB || mouseEvent.RB))
                    return true;
            }
            return false;
        }
    }
}
