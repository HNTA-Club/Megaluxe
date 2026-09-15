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
using NUnit.Framework;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Tests.VocaluxeLib.Songs
{
    [TestFixture]
    public class CDifficultyCalculatorTest
    {
        private const float DefaultBpm = 400f; // UltraStar BPM (4x) ~ 100 musical BPM

        private static CVoice CreateVoiceWithNotes(params CSongNote[] notes)
        {
            var voice = new CVoice();
            var line = new CSongLine();
            foreach (var note in notes)
            {
                line.AddNote(note);
            }
            voice.AddLine(line);
            return voice;
        }

        [Test]
        public void TestEmptyVoiceReturnsDefault()
        {
            var voice = new CVoice();
            SDifficultyMetrics metrics = CDifficultyCalculator.Calculate(voice, DefaultBpm, false);

            Assert.AreEqual(1.0f, metrics.Overall);
            Assert.AreEqual(1.0f, metrics.Pace);
            Assert.AreEqual(1.0f, metrics.Range);
            Assert.AreEqual(1.0f, metrics.Agility);
            Assert.IsFalse(float.IsNaN(metrics.Overall));
        }

        [Test]
        public void TestPureRapSongDoesNotCrashAndDrivesBySpeedWithDiscount()
        {
            // 20 rapid rap notes, 0 pitched notes
            var notes = new CSongNote[20];
            for (int i = 0; i < 20; i++)
            {
                notes[i] = new CSongNote(i * 4, 2, 0, "rap", ENoteType.Rap);
            }

            CVoice voice = CreateVoiceWithNotes(notes);
            SDifficultyMetrics metrics = CDifficultyCalculator.Calculate(voice, DefaultBpm, true);

            // Range and agility must baseline to 1.0 on pure rap
            Assert.AreEqual(1.0f, metrics.Range);
            Assert.AreEqual(1.0f, metrics.Agility);

            // Pace is active
            Assert.Greater(metrics.Pace, 1.0f);
            Assert.Greater(metrics.Overall, 1.0f);

            // Pure rap overall is capped by gameplay-aware discount (cannot exceed ~3.0)
            Assert.Less(metrics.Overall, 3.0f);
            Assert.IsFalse(float.IsNaN(metrics.Overall));
        }

        [Test]
        public void TestNegativePitchOffsets()
        {
            // Tones spanning -36 to -12 (covers Vocaluxe ToneMin = -36)
            var notes = new CSongNote[]
            {
                new CSongNote(0, 4, -36, "low", ENoteType.Normal),
                new CSongNote(8, 4, -30, "mid-low", ENoteType.Normal),
                new CSongNote(16, 4, -24, "mid", ENoteType.Normal),
                new CSongNote(24, 4, -18, "mid-high", ENoteType.Normal),
                new CSongNote(32, 4, -12, "high", ENoteType.Normal)
            };

            CVoice voice = CreateVoiceWithNotes(notes);
            SDifficultyMetrics metrics = CDifficultyCalculator.Calculate(voice, DefaultBpm, false);

            Assert.Less(metrics.P5Tone, 0);
            Assert.Less(metrics.P95Tone, 0);
            Assert.Greater(metrics.ToneSpan, 0);
            Assert.Greater(metrics.Range, 2.0f);
            Assert.IsFalse(float.IsNaN(metrics.Range));
        }

        [Test]
        public void TestDirectionalAgilityAscendingIsHarder()
        {
            // Ascending octave leap: 0 -> 12
            var ascendingNotes = new CSongNote[]
            {
                new CSongNote(0, 4, 0, "a", ENoteType.Normal),
                new CSongNote(5, 4, 12, "b", ENoteType.Normal)
            };
            CVoice ascendingVoice = CreateVoiceWithNotes(ascendingNotes);
            SDifficultyMetrics ascendingMetrics = CDifficultyCalculator.Calculate(ascendingVoice, DefaultBpm, false);

            // Descending octave leap: 12 -> 0
            var descendingNotes = new CSongNote[]
            {
                new CSongNote(0, 4, 12, "a", ENoteType.Normal),
                new CSongNote(5, 4, 0, "b", ENoteType.Normal)
            };
            CVoice descendingVoice = CreateVoiceWithNotes(descendingNotes);
            SDifficultyMetrics descendingMetrics = CDifficultyCalculator.Calculate(descendingVoice, DefaultBpm, false);

            Assert.Greater(ascendingMetrics.Agility, descendingMetrics.Agility,
                "Ascending leap agility should exceed descending leap agility due to vocal break mechanics.");
        }

        [Test]
        public void TestRapDiscountVsSingingWithWideRange()
        {
            // 20 rapid rap notes
            var rapNotes = new CSongNote[20];
            for (int i = 0; i < 20; i++)
            {
                rapNotes[i] = new CSongNote(i * 4, 2, 0, "rap", ENoteType.Rap);
            }
            CVoice rapVoice = CreateVoiceWithNotes(rapNotes);
            SDifficultyMetrics rapMetrics = CDifficultyCalculator.Calculate(rapVoice, DefaultBpm, true);

            // 20 equally rapid singing notes with a wide 24-semitone vocal range
            var singNotes = new CSongNote[20];
            for (int i = 0; i < 20; i++)
            {
                int tone = (i % 2 == 0) ? -6 : 18; // 24-semitone leaps
                singNotes[i] = new CSongNote(i * 4, 2, tone, "sing", ENoteType.Normal);
            }
            CVoice singVoice = CreateVoiceWithNotes(singNotes);
            SDifficultyMetrics singMetrics = CDifficultyCalculator.Calculate(singVoice, DefaultBpm, false);

            Assert.Greater(singMetrics.Overall, rapMetrics.Overall,
                "Singing requiring accurate pitch reach and agility must be significantly harder than unpitched rap noise.");
            Assert.Greater(singMetrics.Range, rapMetrics.Range);
        }

        [Test]
        public void TestDuetCombiningIsLeadBiased()
        {
            // P1 is an expert part (5.0), P2 is an easy backing part (1.0)
            var p1 = new SDifficultyMetrics(5.0f, 5.0f, 5.0f, 5.0f, 10f, 8f, 0, 24, 24, 12, 1.0f);
            var p2 = new SDifficultyMetrics(1.0f, 1.0f, 1.0f, 1.0f, 2f, 1f, 0, 4, 4, 2, 1.0f);

            SDifficultyMetrics combined = CDifficultyCalculator.CombineDuet(p1, p2);

            // 0.70 * 5.0 + 0.30 * 1.0 = 3.8
            Assert.AreEqual(3.8f, combined.Overall, 0.01f);
            Assert.Greater(combined.Overall, 3.0f, "Duet difficulty should bias toward the harder lead part.");
        }

        [Test]
        public void TestBoundsInvariantUnderExtremeInputs()
        {
            // Extreme rapid leaps, huge range, dense fast notes
            var extremeNotes = new CSongNote[50];
            for (int i = 0; i < 50; i++)
            {
                int tone = (i % 2 == 0) ? -36 : 48; // Massive 84-semitone leaps
                extremeNotes[i] = new CSongNote(i * 2, 1, tone, "x", ENoteType.Normal);
            }
            CVoice extremeVoice = CreateVoiceWithNotes(extremeNotes);
            SDifficultyMetrics metrics = CDifficultyCalculator.Calculate(extremeVoice, 1200f, false);

            // Invariant: all scores must remain strictly bounded in [1.0, 5.0]
            Assert.GreaterOrEqual(metrics.Overall, 1.0f);
            Assert.LessOrEqual(metrics.Overall, 5.0f);
            Assert.GreaterOrEqual(metrics.Pace, 1.0f);
            Assert.LessOrEqual(metrics.Pace, 5.0f);
            Assert.GreaterOrEqual(metrics.Range, 1.0f);
            Assert.LessOrEqual(metrics.Range, 5.0f);
            Assert.GreaterOrEqual(metrics.Agility, 1.0f);
            Assert.LessOrEqual(metrics.Agility, 5.0f);
        }

        [Test]
        public void TestFormatNoteNameStandardScale()
        {
            // UltraStar tone 0 = C4 (MIDI 60)
            Assert.AreEqual("C4", SDifficultyMetrics.FormatNoteName(0));
            Assert.AreEqual("C5", SDifficultyMetrics.FormatNoteName(12));
            Assert.AreEqual("C3", SDifficultyMetrics.FormatNoteName(-12));
            Assert.AreEqual("A4", SDifficultyMetrics.FormatNoteName(9));
            Assert.AreEqual("G#3", SDifficultyMetrics.FormatNoteName(-4));
            Assert.AreEqual("E2", SDifficultyMetrics.FormatNoteName(-20));
            Assert.AreEqual("F5", SDifficultyMetrics.FormatNoteName(17));
        }

        [Test]
        public void TestTierNamingAndColorHierarchy()
        {
            Assert.AreEqual("TR_DIFFICULTY_TIER_EXPERT", SDifficultyMetrics.GetTierNameKey(4.5f));
            Assert.AreEqual("TR_DIFFICULTY_TIER_ADVANCED", SDifficultyMetrics.GetTierNameKey(3.8f));
            Assert.AreEqual("TR_DIFFICULTY_TIER_INTERMEDIATE", SDifficultyMetrics.GetTierNameKey(3.0f));
            Assert.AreEqual("TR_DIFFICULTY_TIER_EASY", SDifficultyMetrics.GetTierNameKey(2.4f));
            Assert.AreEqual("TR_DIFFICULTY_TIER_BEGINNER", SDifficultyMetrics.GetTierNameKey(1.8f));

            // Ensure distinct colors are returned across tiers
            SColorF expertCol = SDifficultyMetrics.GetTierColor(4.8f);
            SColorF advCol = SDifficultyMetrics.GetTierColor(3.6f);
            SColorF begCol = SDifficultyMetrics.GetTierColor(1.2f);

            Assert.AreNotEqual(expertCol.R, begCol.R);
            Assert.AreNotEqual(advCol.B, expertCol.B);
        }

        [Test]
        public void TestTierStarsHierarchy()
        {
            Assert.AreEqual("★★★★★", SDifficultyMetrics.GetTierStars(4.5f));
            Assert.AreEqual("★★★★☆", SDifficultyMetrics.GetTierStars(3.8f));
            Assert.AreEqual("★★★☆☆", SDifficultyMetrics.GetTierStars(3.0f));
            Assert.AreEqual("★★☆☆☆", SDifficultyMetrics.GetTierStars(2.4f));
            Assert.AreEqual("★☆☆☆☆", SDifficultyMetrics.GetTierStars(1.8f));
            Assert.AreEqual("★☆☆☆☆", SDifficultyMetrics.GetTierStars(1.0f));
        }

        [Test]
        public void TestNaNAndInfinityBpmReturnsDefault()
        {
            var notes = new CSongNote[]
            {
                new CSongNote(0, 4, 0, "test", ENoteType.Normal)
            };
            CVoice voice = CreateVoiceWithNotes(notes);

            SDifficultyMetrics nanMetrics = CDifficultyCalculator.Calculate(voice, float.NaN, false);
            Assert.AreEqual(SDifficultyMetrics.Default.Overall, nanMetrics.Overall);
            Assert.IsFalse(float.IsNaN(nanMetrics.Overall));

            SDifficultyMetrics infMetrics = CDifficultyCalculator.Calculate(voice, float.PositiveInfinity, false);
            Assert.AreEqual(SDifficultyMetrics.Default.Overall, infMetrics.Overall);
            Assert.IsFalse(float.IsNaN(infMetrics.Overall));
        }
    }
}
