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

namespace Vocaluxe.Screens
{
    public enum EHighscoreChartMode
    {
        SeasonalPlays = 0,
        SeasonalRecords = 1
    }

    public class SChartRow
    {
        public string Label;
        public string ValueText;
        public float Ratio;
        public int SeasonYear;
        public bool IsSelectedSeason;
    }

    public class SChartData
    {
        public string TitleKey;
        public List<SChartRow> Rows = new List<SChartRow>();
    }

    public struct SChartLayout
    {
        public float StartY;
        public float BarHeight;
        public float Gap;
        public float RowPitch;
        public float FontHeight;
    }

    public static class CHighscoreChart
    {
        public const int NumChartRows = 8;

        public static SChartLayout GetLayout(int count)
        {
            count = Math.Max(1, Math.Min(NumChartRows, count));
            const float cardTop = 688f;
            const float cardAvailableH = 324f;

            float barH;
            float gap;
            float fontH;

            switch (count)
            {
                case 1:
                    barH = 90f;
                    gap = 0f;
                    fontH = 42f;
                    break;
                case 2:
                    barH = 110f;
                    gap = 26f;
                    fontH = 40f;
                    break;
                case 3:
                    barH = 75f;
                    gap = 20f;
                    fontH = 34f;
                    break;
                case 4:
                    barH = 58f;
                    gap = 16f;
                    fontH = 30f;
                    break;
                case 5:
                    barH = 47f;
                    gap = 14f;
                    fontH = 28f;
                    break;
                case 6:
                    barH = 40f;
                    gap = 12f;
                    fontH = 26f;
                    break;
                case 7:
                    barH = 35f;
                    gap = 10f;
                    fontH = 25f;
                    break;
                case 8:
                default:
                    barH = 32f;
                    gap = 8f;
                    fontH = 24f;
                    break;
            }

            float totalHeight = count * barH + (count - 1) * gap;
            float startY = cardTop + (cardAvailableH - totalHeight) / 2f;

            return new SChartLayout
            {
                StartY = startY,
                BarHeight = barH,
                Gap = gap,
                RowPitch = barH + gap,
                FontHeight = fontH
            };
        }

        public static SChartData GetChartData(List<SDBScoreEntry> scores, EHighscoreChartMode mode, int selectedSeasonYear)
        {
            var data = new SChartData();
            data.TitleKey = (mode == EHighscoreChartMode.SeasonalPlays)
                ? "TR_SCREENHIGHSCORE_CHART_SEASONAL_PLAYS"
                : "TR_SCREENHIGHSCORE_CHART_SEASONAL_RECORDS";

            if (scores == null || scores.Count == 0)
                return data;

            int currentSeason = CHighscoreStats.GetCurrentSeasonYear();

            int minSeason = scores.Select(s => CHighscoreStats.GetSeasonYear(s)).Min();
            if (minSeason > currentSeason) minSeason = currentSeason;
            if (currentSeason - minSeason + 1 > NumChartRows)
                minSeason = currentSeason - NumChartRows + 1;

            var seasonList = new List<SeasonStat>();
            for (int sy = currentSeason; sy >= minSeason; sy--)
            {
                var seasonScores = scores.Where(s => CHighscoreStats.GetSeasonYear(s) == sy).ToList();
                int plays = seasonScores.Count;
                int peak = plays > 0 ? seasonScores.Max(s => s.Score) : 0;
                seasonList.Add(new SeasonStat
                {
                    SeasonYear = sy,
                    Plays = plays,
                    PeakScore = peak,
                    IsSelectedSeason = (sy == selectedSeasonYear)
                });
            }

            int maxPlays = seasonList.Count > 0 ? seasonList.Max(s => s.Plays) : 0;
            int maxPeak = seasonList.Count > 0 ? seasonList.Max(s => s.PeakScore) : 0;

            foreach (var s in seasonList)
            {
                string label = s.SeasonYear + "-" + (s.SeasonYear + 1);
                string valText;
                float ratio;

                if (mode == EHighscoreChartMode.SeasonalPlays)
                {
                    valText = String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_CHART_PERFORMANCES_UNIT"), s.Plays);
                    ratio = maxPlays > 0 ? (float)s.Plays / maxPlays : 0f;
                }
                else
                {
                    valText = s.PeakScore > 0 ? s.PeakScore.ToString("N0") : "-";
                    ratio = maxPeak > 0 ? (float)s.PeakScore / maxPeak : 0f;
                }

                data.Rows.Add(new SChartRow
                {
                    Label = label,
                    ValueText = valText,
                    Ratio = ratio,
                    SeasonYear = s.SeasonYear,
                    IsSelectedSeason = s.IsSelectedSeason
                });
            }

            return data;
        }

        private class SeasonStat
        {
            public int SeasonYear;
            public int Plays;
            public int PeakScore;
            public bool IsSelectedSeason;
        }

        public static void DrawChartBars(SChartData data, SChartLayout layout)
        {
            if (data == null || data.Rows == null || data.Rows.Count == 0)
                return;

            const float barStartX = 1085f;
            const float barMaxWidth = 770f;

            for (int i = 0; i < data.Rows.Count && i < NumChartRows; i++)
            {
                float y = layout.StartY + i * layout.RowPitch;
                float barWidth = Math.Max(25f, barMaxWidth * Math.Min(1.0f, Math.Max(0.05f, data.Rows[i].Ratio)));

                // Color hierarchy: Cyan for currently selected season, default white for other seasons
                SColorF fillColor;
                SColorF edgeColor;

                if (data.Rows[i].IsSelectedSeason)
                {
                    fillColor = new SColorF(0.25f, 0.80f, 1.00f, 0.45f); // Vibrant Electric Cyan
                    edgeColor = new SColorF(0.40f, 0.90f, 1.00f, 0.95f);
                }
                else
                {
                    fillColor = new SColorF(0.96f, 0.96f, 0.98f, 0.28f); // Soft Clean Default White
                    edgeColor = new SColorF(1.00f, 1.00f, 1.00f, 0.85f);
                }

                // 1. Draw dark translucent background tray at Z = -0.4f (behind fill bar!)
                SRectF bgRect = new SRectF(barStartX, y, barMaxWidth, layout.BarHeight, -0.4f);
                SColorF bgColor = new SColorF(0.06f, 0.08f, 0.14f, 0.65f);
                CDraw.DrawRect(bgColor, bgRect);

                // 2. Draw filled progress bar rect with color at Z = -0.5f (in front of tray!)
                SRectF fillRect = new SRectF(barStartX, y, barWidth, layout.BarHeight, -0.5f);
                CDraw.DrawRect(fillColor, fillRect);

                // 3. Draw bright edge highlight on the right end of the fill bar at Z = -0.6f (frontmost!)
                SRectF edgeRect = new SRectF(barStartX + barWidth - 3f, y, 3f, layout.BarHeight, -0.6f);
                CDraw.DrawRect(edgeColor, edgeRect);
            }
        }
    }
}
