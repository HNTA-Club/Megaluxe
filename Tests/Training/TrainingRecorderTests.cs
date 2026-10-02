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
using Vocaluxe.Base;
using Vocaluxe.Training;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Tests.Training
{
    [TestFixture]
    public class TrainingRecorderTests
    {
        private static CSong CreateMockSong(int noteCount = 1, int noteDuration = 8, int targetTone = 14)
        {
            var song = (CSong)Activator.CreateInstance(typeof(CSong), true);
            song.Artist = "TestArtist";
            song.Title = "TestTitle";
            song.Bpm = 240f;
            song.Gap = 0f;

            var voice = new CVoice();
            var line = new CSongLine();
            var currentBeat = 0;

            for (var i = 0; i < noteCount; i++)
            {
                var note = new CSongNote(currentBeat, noteDuration, targetTone, "La", ENoteType.Normal);
                line.AddNote(note);
                currentBeat += noteDuration;
            }

            voice.AddLine(line);
            song.Notes.AddVoice(voice);
            return song;
        }

        private static SPlayer[] CreateMockPlayers(EGameDifficulty difficulty = EGameDifficulty.TR_CONFIG_NORMAL)
        {
            var players = new SPlayer[1];
            players[0] = new SPlayer
            {
                VoiceNr = 0,
                ProfileId = System.Guid.NewGuid()
            };
            return players;
        }

        [TearDown]
        public void Cleanup()
        {
            CTrainingRecorder.CancelSession();
        }

        [Test]
        public void TestSustainedToneOnTargetProducesSingleTargetChunk()
        {
            var song = CreateMockSong(1, 8, 14);
            var players = CreateMockPlayers();

            CTrainingRecorder.StartSession(song, players, 0, EGameMode.TR_GAMEMODE_NORMAL, EAudioMode.TR_AUDIOMODE_NORMAL, "TestPlayer", EGameDifficulty.TR_CONFIG_NORMAL);
            Assert.That(CTrainingRecorder.IsActive, Is.True);

            for (var beat = 0; beat < 8; beat++)
            {
                CTrainingRecorder.RecordBeat(0, 0, 0, beat, 14, 14, true, true);
            }

            var session = CTrainingRecorder.FinalizeSession();
            Assert.That(CTrainingRecorder.IsActive, Is.False);
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Notes.Count, Is.EqualTo(1));

            var note = session.Notes[0];
            Assert.That(note.HitBeats, Is.EqualTo(8));
            Assert.That(note.HitPercentage, Is.EqualTo(100.0));
            Assert.That(note.PrimaryMissDirection, Is.EqualTo("None"));
            Assert.That(note.AvgPitchOffset, Is.EqualTo(0.0));
            Assert.That(note.Chunks.Count, Is.EqualTo(1));

            var chunk = note.Chunks[0];
            Assert.That(chunk.Status, Is.EqualTo("Target"));
            Assert.That(chunk.StartBeat, Is.EqualTo(0));
            Assert.That(chunk.DurationBeats, Is.EqualTo(8));
            Assert.That(chunk.SungTone, Is.EqualTo(14));
            Assert.That(chunk.PitchOffset, Is.EqualTo(0));
            Assert.That(chunk.Hit, Is.True);
        }

        [Test]
        public void TestFlatOnsetThenTargetProducesTwoChunks()
        {
            var song = CreateMockSong(1, 8, 14);
            var players = CreateMockPlayers();

            CTrainingRecorder.StartSession(song, players, 0, EGameMode.TR_GAMEMODE_NORMAL, EAudioMode.TR_AUDIOMODE_NORMAL, "TestPlayer", EGameDifficulty.TR_CONFIG_NORMAL);

            // 2 beats flat (offset -1, miss)
            CTrainingRecorder.RecordBeat(0, 0, 0, 0, 14, 13, true, false);
            CTrainingRecorder.RecordBeat(0, 0, 0, 1, 14, 13, true, false);

            // 6 beats target (offset 0, hit)
            for (var beat = 2; beat < 8; beat++)
            {
                CTrainingRecorder.RecordBeat(0, 0, 0, beat, 14, 14, true, true);
            }

            var session = CTrainingRecorder.FinalizeSession();
            var note = session.Notes[0];

            Assert.That(note.HitBeats, Is.EqualTo(6));
            Assert.That(note.HitPercentage, Is.EqualTo(75.0));
            Assert.That(note.PrimaryMissDirection, Is.EqualTo("Flat"));
            Assert.That(note.AvgPitchOffset, Is.EqualTo(-0.25).Within(0.001));
            Assert.That(note.Chunks.Count, Is.EqualTo(2));

            // Chunk 1: Flat
            Assert.That(note.Chunks[0].Status, Is.EqualTo("Flat"));
            Assert.That(note.Chunks[0].DurationBeats, Is.EqualTo(2));
            Assert.That(note.Chunks[0].SungTone, Is.EqualTo(13));
            Assert.That(note.Chunks[0].PitchOffset, Is.EqualTo(-1));
            Assert.That(note.Chunks[0].Hit, Is.False);

            // Chunk 2: Target
            Assert.That(note.Chunks[1].Status, Is.EqualTo("Target"));
            Assert.That(note.Chunks[1].DurationBeats, Is.EqualTo(6));
            Assert.That(note.Chunks[1].SungTone, Is.EqualTo(14));
            Assert.That(note.Chunks[1].PitchOffset, Is.EqualTo(0));
            Assert.That(note.Chunks[1].Hit, Is.True);
        }

        [Test]
        public void TestUnvoicedBeatsProduceSilentChunk()
        {
            var song = CreateMockSong(1, 4, 10);
            var players = CreateMockPlayers();

            CTrainingRecorder.StartSession(song, players, 0, EGameMode.TR_GAMEMODE_NORMAL, EAudioMode.TR_AUDIOMODE_NORMAL, "TestPlayer", EGameDifficulty.TR_CONFIG_NORMAL);

            // 2 beats silent / unvoiced
            CTrainingRecorder.RecordBeat(0, 0, 0, 0, 10, 0, false, false);
            CTrainingRecorder.RecordBeat(0, 0, 0, 1, 10, 0, false, false);

            // 2 beats sharp (offset +1)
            CTrainingRecorder.RecordBeat(0, 0, 0, 2, 10, 11, true, false);
            CTrainingRecorder.RecordBeat(0, 0, 0, 3, 10, 11, true, false);

            var session = CTrainingRecorder.FinalizeSession();
            var note = session.Notes[0];

            Assert.That(note.Chunks.Count, Is.EqualTo(2));
            Assert.That(note.Chunks[0].Status, Is.EqualTo("Silent"));
            Assert.That(note.Chunks[0].DurationBeats, Is.EqualTo(2));
            Assert.That(note.Chunks[0].SungTone, Is.Null);
            Assert.That(note.Chunks[0].PitchOffset, Is.Null);
            Assert.That(note.Chunks[0].Hit, Is.False);

            Assert.That(note.Chunks[1].Status, Is.EqualTo("Sharp"));
            Assert.That(note.Chunks[1].DurationBeats, Is.EqualTo(2));
            Assert.That(note.Chunks[1].SungTone, Is.EqualTo(11));
            Assert.That(note.Chunks[1].PitchOffset, Is.EqualTo(1));
        }

        [Test]
        public void TestCancelSessionResetsActiveState()
        {
            var song = CreateMockSong(1, 4, 10);
            var players = CreateMockPlayers();

            CTrainingRecorder.StartSession(song, players, 0, EGameMode.TR_GAMEMODE_NORMAL, EAudioMode.TR_AUDIOMODE_NORMAL, "TestPlayer", EGameDifficulty.TR_CONFIG_NORMAL);
            Assert.That(CTrainingRecorder.IsActive, Is.True);

            CTrainingRecorder.CancelSession();
            Assert.That(CTrainingRecorder.IsActive, Is.False);
        }
    }
}
