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

using VocaluxeLib;

namespace VocaluxeLib.Songs
{
    /// <summary>
    ///     Represents deterministic difficulty metrics calculated from song note data.
    ///     Immutable struct to ensure thread safety and prevent CS1612 property mutation errors.
    /// </summary>
    public struct SDifficultyMetrics
    {
        public static readonly SDifficultyMetrics Default = new SDifficultyMetrics(
            1.0f, 1.0f, 1.0f, 1.0f,
            0.0f, 0.0f, 0, 0, 0, 0, 1.0f);

        // Normalized difficulty scores [1.0, 5.0]
        public float Overall { get; }
        public float Pace { get; }
        public float Range { get; }
        public float Agility { get; }

        /// <summary>
        ///     Returns true if the metrics represent a ranked song (i.e. not the default 1.0 placeholder).
        /// </summary>
        public bool IsRanked => Overall > 1.0f;

        /// <summary>
        ///     Checks whether the song is ranked for a specific difficulty sorting criteria.
        /// </summary>
        public bool IsRankedForSort(ESongSorting sorting)
        {
            switch (sorting)
            {
                case ESongSorting.TR_CONFIG_DIFFICULTY_AGILITY:
                    return Agility > 1.0f;
                case ESongSorting.TR_CONFIG_DIFFICULTY_RANGE:
                    return Range > 1.0f;
                case ESongSorting.TR_CONFIG_DIFFICULTY_PACE:
                    return Pace > 1.0f;
                case ESongSorting.TR_CONFIG_DIFFICULTY:
                    return Overall > 1.0f;
                default:
                    return false;
            }
        }

        // Backward compatibility alias for Pace
        public float Speed => Pace;

        // Raw musical statistics
        public float PeakNps { get; }
        public float AvgNps { get; }
        public int P5Tone { get; }
        public int P95Tone { get; }
        public int ToneSpan { get; }
        public int MedianTone { get; }
        public float PitchedRatio { get; }

        public SDifficultyMetrics(
            float overall, float pace, float range, float agility,
            float peakNps, float avgNps, int p5Tone, int p95Tone, int toneSpan, int medianTone, float pitchedRatio)
        {
            Overall = overall;
            Pace = pace;
            Range = range;
            Agility = agility;
            PeakNps = peakNps;
            AvgNps = avgNps;
            P5Tone = p5Tone;
            P95Tone = p95Tone;
            ToneSpan = toneSpan;
            MedianTone = medianTone;
            PitchedRatio = pitchedRatio;
        }

        private static readonly string[] _NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public static string FormatNoteName(int tone)
        {
            // UltraStar tone 0 = C4 (MIDI note 60)
            int midi = tone + 60;
            if (midi < 0 || midi > 127)
                return "?";

            int octave = (midi / 12) - 1;
            int noteIndex = ((midi % 12) + 12) % 12;
            return _NoteNames[noteIndex] + octave;
        }

        public static string GetTierStars(float overall)
        {
            if (overall >= 4.1f) return "★★★★★";
            if (overall >= 3.5f) return "★★★★☆";
            if (overall >= 2.8f) return "★★★☆☆";
            if (overall >= 2.2f) return "★★☆☆☆";
            return "★☆☆☆☆";
        }

        public static string GetTierNameKey(float overall)
        {
            if (overall >= 4.1f) return "TR_DIFFICULTY_TIER_EXPERT";
            if (overall >= 3.5f) return "TR_DIFFICULTY_TIER_ADVANCED";
            if (overall >= 2.8f) return "TR_DIFFICULTY_TIER_INTERMEDIATE";
            if (overall >= 2.2f) return "TR_DIFFICULTY_TIER_EASY";
            return "TR_DIFFICULTY_TIER_BEGINNER";
        }

        public static SColorF GetTierColor(float overall)
        {
            if (overall >= 4.1f) return new SColorF(1.00f, 0.75f, 0.15f, 1f); // Gold / Amber
            if (overall >= 3.5f) return new SColorF(0.85f, 0.35f, 1.00f, 1f); // Vibrant Purple
            if (overall >= 2.8f) return new SColorF(0.30f, 0.70f, 1.00f, 1f); // Sky Blue
            if (overall >= 2.2f) return new SColorF(0.15f, 0.85f, 0.95f, 1f); // Cyan
            return new SColorF(0.20f, 0.90f, 0.50f, 1f);                      // Mint Green
        }
    }
}
