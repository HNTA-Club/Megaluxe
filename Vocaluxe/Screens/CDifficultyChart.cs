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
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public static class CDifficultyChart
    {
        public const int NumAxes = 3;

        // Horizontal Bars default layout (Song Lore right-half mode)
        public const float BarStartX = 1470f;
        public const float BarMaxWidth = 385f;
        public const float BarStartY = 215f;
        public const float BarPitch = 52f;
        public const float BarHeight = 34f;
        public const float FontHeight = 24f;

        // 3-Axis Neon Palette
        private static readonly SColorF[] _FillColors = new SColorF[]
        {
            new SColorF(0.00f, 0.90f, 1.00f, 0.45f), // Pace: Electric Cyan
            new SColorF(0.18f, 0.90f, 0.50f, 0.45f), // Range: Emerald Mint
            new SColorF(0.85f, 0.35f, 1.00f, 0.45f)  // Agility: Vivid Violet
        };

        private static readonly SColorF[] _EdgeColors = new SColorF[]
        {
            new SColorF(0.20f, 1.00f, 1.00f, 0.95f), // Pace edge
            new SColorF(0.35f, 1.00f, 0.65f, 0.95f), // Range edge
            new SColorF(0.95f, 0.55f, 1.00f, 0.95f)  // Agility edge
        };

        public static string FormatNoteName(int tone)
        {
            return SDifficultyMetrics.FormatNoteName(tone);
        }

        public static string GetTierNameKey(float overall)
        {
            return SDifficultyMetrics.GetTierNameKey(overall);
        }

        public static SColorF GetTierColor(float overall)
        {
            return SDifficultyMetrics.GetTierColor(overall);
        }

        public static void DrawDifficultyBars(
            SDifficultyMetrics diff,
            COrderedDictionaryLite<CText> texts,
            string[] names,
            string[] values,
            float startX = BarStartX,
            float startY = BarStartY,
            float maxWidth = BarMaxWidth,
            float pitch = BarPitch,
            float height = BarHeight)
        {
            if (diff.Overall < 1.0f)
                return;

            float[] axisValues = new float[]
            {
                diff.Pace,
                diff.Range,
                diff.Agility
            };

            for (int i = 0; i < NumAxes; i++)
            {
                float y = startY + i * pitch;

                // 1. Dark translucent tray behind the bar
                SRectF bgRect = new SRectF(startX, y, maxWidth, height, -0.4f);
                SColorF bgColor = new SColorF(0.06f, 0.08f, 0.14f, 0.65f);
                CDraw.DrawRect(bgColor, bgRect);

                // 2. Filled progress bar
                float ratio = Math.Max(0.05f, Math.Min(1.0f, (axisValues[i] - 1.0f) / 4.0f));
                float barWidth = Math.Max(25f, maxWidth * ratio);

                SRectF fillRect = new SRectF(startX, y, barWidth, height, -0.5f);
                CDraw.DrawRect(_FillColors[i], fillRect);

                // 3. Glowing edge highlight at the end of the bar
                SRectF edgeRect = new SRectF(startX + barWidth - 3f, y, 3f, height, -0.6f);
                CDraw.DrawRect(_EdgeColors[i], edgeRect);
            }

            // Render labels and values on top of the bars to prevent occlusion
            if (texts != null && names != null && values != null)
            {
                for (int i = 0; i < NumAxes; i++)
                {
                    if (i < names.Length && texts.ContainsKey(names[i]) && texts[names[i]] != null)
                        texts[names[i]].DrawRelative(0, 0);

                    if (i < values.Length && texts.ContainsKey(values[i]) && texts[values[i]] != null)
                        texts[values[i]].DrawRelative(0, 0);
                }
            }
        }
    }
}
