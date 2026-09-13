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
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public enum EHighscoreChartMode
    {
        MostSungAllTime = 0,
        Popular5Months = 0,
        LongestUnbrokenRecords = 1,
        ForgottenBangers = 2
    }

    public class SChartRow
    {
        public string Label;
        public string ValueText;
        public float Ratio;
    }

    public class SChartData
    {
        public string TitleKey;
        public List<SChartRow> Rows = new List<SChartRow>();
    }

    public static class CHighscoreChart
    {
        public const int NumChartRows = 8;
        private static Dictionary<EHighscoreChartMode, SChartData> _Cache = new Dictionary<EHighscoreChartMode, SChartData>();
        private static DateTime _LastCacheTime = DateTime.MinValue;

        public static void ClearCache()
        {
            _Cache.Clear();
            _LastCacheTime = DateTime.MinValue;
        }

        public static SChartData GetChartData(EHighscoreChartMode mode)
        {
            if (_Cache.ContainsKey(mode) && (DateTime.Now - _LastCacheTime).TotalSeconds < 30)
            {
                return _Cache[mode];
            }

            _BuildAllChartData();

            if (_Cache.ContainsKey(mode))
                return _Cache[mode];

            return new SChartData { TitleKey = "TR_SCREENHIGHSCORE_CHART_MOST_SUNG" };
        }

        private static void _BuildAllChartData()
        {
            _Cache.Clear();
            _LastCacheTime = DateTime.Now;

            List<CSong> songs = CSongs.Songs;
            if (songs == null || songs.Count == 0)
                return;

            long sixtyDaysAgoTicks = DateTime.Now.AddDays(-60).Ticks;

            var songStats = new List<SongStatSummary>();

            foreach (var song in songs)
            {
                List<SDBScoreEntry> scores = CDataBase.LoadScore(song.ID, EGameMode.TR_GAMEMODE_NORMAL, EHighscoreStyle.TR_CONFIG_HIGHSCORE_LIST_ALL);
                if (scores == null || scores.Count == 0)
                    continue;

                SDBScoreEntry allTimeBest = scores.OrderByDescending(s => s.Score).FirstOrDefault();
                SDBScoreEntry latestScore = scores.OrderByDescending(s => s.DateTicks).FirstOrDefault();

                songStats.Add(new SongStatSummary
                {
                    Song = song,
                    TotalScores = scores.Count,
                    AllTimeRecordDateTicks = allTimeBest.DateTicks,
                    LatestDateTicks = latestScore.DateTicks,
                    RecordHolder = allTimeBest.Name
                });
            }

            // 1. All-Time Most Sung / Club Classics
            var popData = new SChartData { TitleKey = "TR_SCREENHIGHSCORE_CHART_MOST_SUNG" };
            var topPop = songStats
                .OrderByDescending(s => s.TotalScores)
                .Take(NumChartRows)
                .ToList();

            if (topPop.Count > 0)
            {
                int maxVal = topPop.Max(s => s.TotalScores);
                foreach (var item in topPop)
                {
                    popData.Rows.Add(new SChartRow
                    {
                        Label = item.Song.Title,
                        ValueText = String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_CHART_PERFORMANCES_UNIT"), item.TotalScores),
                        Ratio = maxVal > 0 ? (float)item.TotalScores / maxVal : 0f
                    });
                }
            }
            _Cache[EHighscoreChartMode.MostSungAllTime] = popData;

            // 2. Longest Unbroken Records (filtered to genuine club songs with >= 5 performances)
            var recData = new SChartData { TitleKey = "TR_SCREENHIGHSCORE_CHART_LONGEST_RECORDS" };
            var topRec = songStats
                .Where(s => s.TotalScores >= 5)
                .Select(s => new { s.Song, DaysAge = (int)(DateTime.Now - new DateTime(s.AllTimeRecordDateTicks)).TotalDays })
                .Where(x => x.DaysAge > 0)
                .OrderByDescending(x => x.DaysAge)
                .Take(NumChartRows)
                .ToList();

            if (topRec.Count > 0)
            {
                int maxDays = topRec.Max(x => x.DaysAge);
                foreach (var item in topRec)
                {
                    recData.Rows.Add(new SChartRow
                    {
                        Label = item.Song.Title,
                        ValueText = String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_CHART_DAYS_UNIT"), item.DaysAge),
                        Ratio = maxDays > 0 ? (float)item.DaysAge / maxDays : 0f
                    });
                }
            }
            _Cache[EHighscoreChartMode.LongestUnbrokenRecords] = recData;

            // 3. Forgotten Bangers (unplayed for >= 60 days, with >= 5 performances)
            var bangData = new SChartData { TitleKey = "TR_SCREENHIGHSCORE_CHART_FORGOTTEN_BANGERS" };
            var topBang = songStats
                .Where(s => s.TotalScores >= 5 && s.LatestDateTicks < sixtyDaysAgoTicks)
                .Select(s => new { s.Song, DroughtDays = (int)(DateTime.Now - new DateTime(s.LatestDateTicks)).TotalDays })
                .OrderByDescending(x => x.DroughtDays)
                .Take(NumChartRows)
                .ToList();

            if (topBang.Count > 0)
            {
                int maxDrought = topBang.Max(x => x.DroughtDays);
                foreach (var item in topBang)
                {
                    bangData.Rows.Add(new SChartRow
                    {
                        Label = item.Song.Title,
                        ValueText = String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_CHART_DROUGHT_UNIT"), item.DroughtDays),
                        Ratio = maxDrought > 0 ? (float)item.DroughtDays / maxDrought : 0f
                    });
                }
            }
            _Cache[EHighscoreChartMode.ForgottenBangers] = bangData;
        }

        private class SongStatSummary
        {
            public CSong Song;
            public int TotalScores;
            public long AllTimeRecordDateTicks;
            public long LatestDateTicks;
            public string RecordHolder;
        }

        public static void DrawChartBars(SChartData data, EHighscoreChartMode mode)
        {
            if (data == null || data.Rows == null || data.Rows.Count == 0)
                return;

            const float barStartX = 1085f;
            const float barMaxWidth = 770f;
            const float barHeight = 32f;
            const float rowPitch = 40f;
            const float startY = 690f;

            // Soft translucent vibrant mode color
            SColorF fillColor;
            switch (mode)
            {
                case EHighscoreChartMode.MostSungAllTime:
                    fillColor = new SColorF(0.18f, 0.58f, 0.90f, 0.45f); // Soft Cyan/Blue
                    break;
                case EHighscoreChartMode.LongestUnbrokenRecords:
                    fillColor = new SColorF(0.92f, 0.68f, 0.18f, 0.45f); // Soft Gold
                    break;
                case EHighscoreChartMode.ForgottenBangers:
                    fillColor = new SColorF(0.92f, 0.38f, 0.22f, 0.45f); // Soft Amber/Coral
                    break;
                default:
                    fillColor = new SColorF(0.20f, 0.60f, 0.90f, 0.45f);
                    break;
            }

            for (int i = 0; i < data.Rows.Count && i < NumChartRows; i++)
            {
                float y = startY + i * rowPitch;
                float barWidth = Math.Max(25f, barMaxWidth * Math.Min(1.0f, Math.Max(0.05f, data.Rows[i].Ratio)));

                // 1. Draw dark translucent background tray at Z = -0.4f (behind fill bar!)
                SRectF bgRect = new SRectF(barStartX, y, barMaxWidth, barHeight, -0.4f);
                SColorF bgColor = new SColorF(0.06f, 0.08f, 0.14f, 0.65f);
                CDraw.DrawRect(bgColor, bgRect);

                // 2. Draw filled progress bar rect with soft vibrant color at Z = -0.5f (in front of tray!)
                SRectF fillRect = new SRectF(barStartX, y, barWidth, barHeight, -0.5f);
                CDraw.DrawRect(fillColor, fillRect);

                // 3. Draw bright edge highlight on the right end of the fill bar at Z = -0.6f (frontmost!)
                SRectF edgeRect = new SRectF(barStartX + barWidth - 3f, y, 3f, barHeight, -0.6f);
                SColorF edgeColor = new SColorF(Math.Min(1.0f, fillColor.R * 1.25f), Math.Min(1.0f, fillColor.G * 1.25f), Math.Min(1.0f, fillColor.B * 1.25f), 0.95f);
                CDraw.DrawRect(edgeColor, edgeRect);
            }
        }
    }
}
