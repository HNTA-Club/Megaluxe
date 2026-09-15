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

    /// <summary>
    ///     Calculates deterministic song difficulty metrics in two allocation-free in-memory passes.
    /// </summary>
    public static class CDifficultyCalculator
    {
        private const int HistogramSize = 128;
        private const int ToneOffset = 64; // Supports tones from -64 to +63 (covers Vocaluxe ToneMin = -36 to +48)

        private static float Sigmoid(float x, float x0, float k)
        {
            float val = 1.0f + 4.0f / (1.0f + (float)Math.Exp(-k * (x - x0)));
            return Math.Max(1.0f, Math.Min(5.0f, val));
        }

        /// <summary>
        ///     Calculates difficulty metrics for a given voice.
        /// </summary>
        public static SDifficultyMetrics Calculate(CVoice voice, float bpm, bool isRap)
        {
            if (voice == null || voice.NumLines == 0 || float.IsNaN(bpm) || float.IsInfinity(bpm) || bpm <= 0f)
                return SDifficultyMetrics.Default;

            CSongLine[] lines = voice.Lines;
            if (lines == null || lines.Length == 0)
                return SDifficultyMetrics.Default;

            // ==========================================
            // PASS 1: Boundary & Histogram Accumulation
            // ==========================================
            int[] toneHistogram = new int[HistogramSize];
            int totalNonFreestyleNotes = 0;
            int totalPitchedNotes = 0;
            int activePhonationBeats = 0;
            int firstBeat = int.MaxValue;
            int lastBeat = int.MinValue;

            for (int l = 0; l < lines.Length; l++)
            {
                CSongLine line = lines[l];
                if (line == null)
                    continue;

                CSongNote[] notes = line.Notes;
                if (notes == null)
                    continue;

                for (int n = 0; n < notes.Length; n++)
                {
                    CSongNote note = notes[n];
                    if (note == null || note.Type == ENoteType.Freestyle)
                        continue;

                    totalNonFreestyleNotes++;
                    activePhonationBeats += note.Duration;

                    if (note.StartBeat < firstBeat)
                        firstBeat = note.StartBeat;
                    if (note.EndBeat > lastBeat)
                        lastBeat = note.EndBeat;

                    // Exclude rap notes from pitch distribution
                    if (!note.IsRapNote)
                    {
                        int idx = Math.Max(0, Math.Min(HistogramSize - 1, note.Tone + ToneOffset));
                        toneHistogram[idx]++;
                        totalPitchedNotes++;
                    }
                }
            }

            if (totalNonFreestyleNotes == 0 || lastBeat <= firstBeat)
                return SDifficultyMetrics.Default;

            float activeSingingDuration = (activePhonationBeats / bpm) * 60f;
            if (activeSingingDuration <= 0f)
                activeSingingDuration = 0.1f;

            // Percentile Extraction
            int p5Tone = 0;
            int medianTone = 0;
            int p95Tone = 0;
            int toneSpan = 0;

            if (totalPitchedNotes > 0)
            {
                int p5Target = (int)Math.Ceiling(totalPitchedNotes * 0.05f);
                int medTarget = (int)Math.Ceiling(totalPitchedNotes * 0.50f);
                int p95Target = (int)Math.Ceiling(totalPitchedNotes * 0.95f);

                bool p5Found = false;
                bool medFound = false;
                bool p95Found = false;
                int cumCount = 0;

                for (int i = 0; i < HistogramSize; i++)
                {
                    cumCount += toneHistogram[i];

                    if (!p5Found && cumCount >= p5Target)
                    {
                        p5Tone = i - ToneOffset;
                        p5Found = true;
                    }
                    if (!medFound && cumCount >= medTarget)
                    {
                        medianTone = i - ToneOffset;
                        medFound = true;
                    }
                    if (!p95Found && cumCount >= p95Target)
                    {
                        p95Tone = i - ToneOffset;
                        p95Found = true;
                        break;
                    }
                }
                toneSpan = Math.Max(0, p95Tone - p5Tone);
            }

            // ==========================================
            // PASS 2: Line NPS & Melodic Transitions
            // ==========================================
            float peakLineNps = 0f;
            float weightedAgilitySum = 0f;
            int agilityTransitionCount = 0;
            CSongNote prevPitchedNote = null;

            for (int l = 0; l < lines.Length; l++)
            {
                CSongLine line = lines[l];
                if (line == null)
                    continue;

                CSongNote[] notes = line.Notes;
                if (notes == null || notes.Length == 0)
                    continue;

                // Line NPS
                int lineNonFreestyle = 0;
                for (int n = 0; n < notes.Length; n++)
                {
                    if (notes[n].Type != ENoteType.Freestyle)
                        lineNonFreestyle++;
                }

                float lineDuration = ((line.LastNoteBeat - line.FirstNoteBeat + 1) / bpm) * 60f;
                if (lineDuration > 0.3f && lineNonFreestyle > 0)
                {
                    float lineNps = lineNonFreestyle / lineDuration;
                    if (lineNps > peakLineNps)
                        peakLineNps = lineNps;
                }

                // Note-level melodic transitions (pitched notes only)
                for (int n = 0; n < notes.Length; n++)
                {
                    CSongNote note = notes[n];
                    if (note.Type == ENoteType.Freestyle || note.IsRapNote)
                        continue;

                    if (prevPitchedNote != null)
                    {
                        float transGapSec = Math.Max(0.05f, ((note.StartBeat - prevPitchedNote.EndBeat) / bpm) * 60f);
                        if (transGapSec < 2.5f) // Disregard transitions across long rests
                        {
                            int deltaP = note.Tone - prevPitchedNote.Tone;
                            int absDeltaP = Math.Abs(deltaP);

                            float dirMult = deltaP > 0 ? 1.25f : 1.0f; // Ascending leap penalty
                            if (absDeltaP >= 7 && transGapSec < 0.25f)
                                dirMult *= 1.5f; // Rapid octave/fifth penalty

                            float leapVelocity = (absDeltaP / transGapSec) * dirMult;
                            weightedAgilitySum += leapVelocity;
                            agilityTransitionCount++;
                        }
                    }
                    prevPitchedNote = note;
                }
            }

            // ==========================================
            // COMPUTE NORMALIZED 3-AXIS METRICS (1.0 to 5.0)
            // ==========================================
            float avgNps = totalNonFreestyleNotes / activeSingingDuration;
            float peakNps = Math.Max(avgNps, peakLineNps);
            float combinedNps = 0.4f * avgNps + 0.6f * peakNps;
            float paceScore = Sigmoid(combinedNps, 5.0f, 0.55f);

            float rangeScore;
            if (totalPitchedNotes == 0)
            {
                rangeScore = 1.0f;
            }
            else
            {
                rangeScore = Sigmoid(toneSpan, 12.5f, 0.35f);
            }

            float agilityScore;
            if (totalPitchedNotes == 0 || agilityTransitionCount == 0)
            {
                agilityScore = 1.0f;
            }
            else
            {
                float avgVelocity = weightedAgilitySum / agilityTransitionCount;
                agilityScore = Sigmoid(avgVelocity, 14.5f, 0.14f);
            }

            // ==========================================
            // GAMEPLAY-AWARE RAP & SINGING DIFFICULTY BLEND
            // ==========================================
            float pitchedRatio = totalNonFreestyleNotes > 0 ? (float)totalPitchedNotes / totalNonFreestyleNotes : 1.0f;
            float singingScore = 0.35f * rangeScore + 0.35f * agilityScore + 0.30f * paceScore;
            float rapScore = 1.0f + (paceScore - 1.0f) * 0.45f;

            float overall;
            if (totalPitchedNotes == 0)
            {
                overall = rapScore;
            }
            else if (pitchedRatio < 0.20f || totalPitchedNotes < 30)
            {
                overall = 0.75f * rapScore + 0.25f * singingScore;
            }
            else if (pitchedRatio < 0.50f)
            {
                overall = 0.65f * singingScore + 0.35f * rapScore;
            }
            else
            {
                overall = 0.85f * singingScore + 0.15f * rapScore;
            }
            overall = Math.Max(1.0f, Math.Min(5.0f, overall));

            return new SDifficultyMetrics(
                overall, paceScore, rangeScore, agilityScore,
                peakNps, avgNps, p5Tone, p95Tone, toneSpan, medianTone, pitchedRatio);
        }

        /// <summary>
        ///     Combines two voice difficulty metrics for a duet song using a lead-biased formula (70% max + 30% min).
        /// </summary>
        public static SDifficultyMetrics CombineDuet(SDifficultyMetrics p1, SDifficultyMetrics p2)
        {
            float overall = Math.Max(1.0f, Math.Min(5.0f, 0.70f * Math.Max(p1.Overall, p2.Overall) + 0.30f * Math.Min(p1.Overall, p2.Overall)));
            float pace = Math.Max(1.0f, Math.Min(5.0f, 0.70f * Math.Max(p1.Pace, p2.Pace) + 0.30f * Math.Min(p1.Pace, p2.Pace)));
            float range = Math.Max(1.0f, Math.Min(5.0f, 0.70f * Math.Max(p1.Range, p2.Range) + 0.30f * Math.Min(p1.Range, p2.Range)));
            float agility = Math.Max(1.0f, Math.Min(5.0f, 0.70f * Math.Max(p1.Agility, p2.Agility) + 0.30f * Math.Min(p1.Agility, p2.Agility)));

            float peakNps = Math.Max(p1.PeakNps, p2.PeakNps);
            float avgNps = (p1.AvgNps + p2.AvgNps) * 0.5f;
            int p5Tone = Math.Min(p1.P5Tone, p2.P5Tone);
            int p95Tone = Math.Max(p1.P95Tone, p2.P95Tone);
            int toneSpan = Math.Max(p1.ToneSpan, p2.ToneSpan);
            int medianTone = (p1.MedianTone + p2.MedianTone) / 2;
            float pitchedRatio = (p1.PitchedRatio + p2.PitchedRatio) * 0.5f;

            return new SDifficultyMetrics(
                overall, pace, range, agility,
                peakNps, avgNps, p5Tone, p95Tone, toneSpan, medianTone, pitchedRatio);
        }
    }
}
