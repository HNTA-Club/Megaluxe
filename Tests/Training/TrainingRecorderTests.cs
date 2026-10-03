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

        [Test]
        public void TestMultiLineAndMultiNoteChunkTracking()
        {
            var song = (CSong)Activator.CreateInstance(typeof(CSong), true);
            song.Artist = "TestArtist";
            song.Title = "MultiNoteSong";
            song.Bpm = 240f;
            song.Gap = 0f;

            var voice = new CVoice();

            // Line 0: Note 0 (beat 0..3, tone 10), Note 1 (beat 4..7, tone 12)
            var line0 = new CSongLine();
            line0.AddNote(new CSongNote(0, 4, 10, "A", ENoteType.Normal));
            line0.AddNote(new CSongNote(4, 4, 12, "B", ENoteType.Normal));
            voice.AddLine(line0);

            // Line 1: Note 0 (beat 10..13, tone 14), Note 1 (beat 14..17, tone 16)
            var line1 = new CSongLine();
            line1.AddNote(new CSongNote(10, 4, 14, "C", ENoteType.Normal));
            line1.AddNote(new CSongNote(14, 4, 16, "D", ENoteType.Normal));
            voice.AddLine(line1);

            song.Notes.AddVoice(voice);

            var players = CreateMockPlayers();
            CTrainingRecorder.StartSession(song, players, 0, EGameMode.TR_GAMEMODE_NORMAL, EAudioMode.TR_AUDIOMODE_NORMAL, "TestPlayer", EGameDifficulty.TR_CONFIG_NORMAL);

            // Record Line 0, Note 0: 4 beats on target (tone 10)
            for (var b = 0; b < 4; b++)
            {
                CTrainingRecorder.RecordBeat(0, 0, 0, b, 10, 10, true, true);
            }

            // Record Line 0, Note 1: 2 beats flat (tone 11), 2 beats target (tone 12)
            CTrainingRecorder.RecordBeat(0, 0, 1, 4, 12, 11, true, false);
            CTrainingRecorder.RecordBeat(0, 0, 1, 5, 12, 11, true, false);
            CTrainingRecorder.RecordBeat(0, 0, 1, 6, 12, 12, true, true);
            CTrainingRecorder.RecordBeat(0, 0, 1, 7, 12, 12, true, true);

            // Record Line 1, Note 0: 4 beats unvoiced / silent
            for (var b = 10; b < 14; b++)
            {
                CTrainingRecorder.RecordBeat(0, 1, 0, b, 14, 0, false, false);
            }

            // Record Line 1, Note 1: 4 beats on target (tone 16)
            for (var b = 14; b < 18; b++)
            {
                CTrainingRecorder.RecordBeat(0, 1, 1, b, 16, 16, true, true);
            }

            var session = CTrainingRecorder.FinalizeSession();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Notes.Count, Is.EqualTo(4));

            // Verify Line 0, Note 0 (index 0)
            var n0 = session.Notes[0];
            Assert.That(n0.LineIndex, Is.EqualTo(0));
            Assert.That(n0.NoteIndex, Is.EqualTo(0));
            Assert.That(n0.HitBeats, Is.EqualTo(4));
            Assert.That(n0.HitPercentage, Is.EqualTo(100.0));
            Assert.That(n0.Chunks.Count, Is.EqualTo(1));
            Assert.That(n0.Chunks[0].Status, Is.EqualTo("Target"));

            // Verify Line 0, Note 1 (index 1)
            var n1 = session.Notes[1];
            Assert.That(n1.LineIndex, Is.EqualTo(0));
            Assert.That(n1.NoteIndex, Is.EqualTo(1));
            Assert.That(n1.HitBeats, Is.EqualTo(2));
            Assert.That(n1.HitPercentage, Is.EqualTo(50.0));
            Assert.That(n1.PrimaryMissDirection, Is.EqualTo("Flat"));
            Assert.That(n1.Chunks.Count, Is.EqualTo(2));
            Assert.That(n1.Chunks[0].Status, Is.EqualTo("Flat"));
            Assert.That(n1.Chunks[0].DurationBeats, Is.EqualTo(2));
            Assert.That(n1.Chunks[1].Status, Is.EqualTo("Target"));
            Assert.That(n1.Chunks[1].DurationBeats, Is.EqualTo(2));

            // Verify Line 1, Note 0 (index 2)
            var n2 = session.Notes[2];
            Assert.That(n2.LineIndex, Is.EqualTo(1));
            Assert.That(n2.NoteIndex, Is.EqualTo(2));
            Assert.That(n2.HitBeats, Is.EqualTo(0));
            Assert.That(n2.HitPercentage, Is.EqualTo(0.0));
            Assert.That(n2.PrimaryMissDirection, Is.EqualTo("Silent"));
            Assert.That(n2.Chunks.Count, Is.EqualTo(1));
            Assert.That(n2.Chunks[0].Status, Is.EqualTo("Silent"));

            // Verify Line 1, Note 1 (index 3)
            var n3 = session.Notes[3];
            Assert.That(n3.LineIndex, Is.EqualTo(1));
            Assert.That(n3.NoteIndex, Is.EqualTo(3));
            Assert.That(n3.HitBeats, Is.EqualTo(4));
            Assert.That(n3.HitPercentage, Is.EqualTo(100.0));
            Assert.That(n3.Chunks.Count, Is.EqualTo(1));
            Assert.That(n3.Chunks[0].Status, Is.EqualTo("Target"));
        }

        [Test]
        public void TestStartSession_SinglePlayerWithPlayerIndexZero_ActivatesSuccessfully()
        {
            var song = CreateMockSong(1, 4, 10);
            var players = new SPlayer[1];
            players[0] = new SPlayer { VoiceNr = 0, ProfileId = Guid.NewGuid() };

            CTrainingRecorder.StartSession(song, players, 0, EGameMode.TR_GAMEMODE_NORMAL, EAudioMode.TR_AUDIOMODE_NORMAL, "SoloPlayer", EGameDifficulty.TR_CONFIG_NORMAL);

            Assert.That(CTrainingRecorder.IsActive, Is.True);
            var session = CTrainingRecorder.FinalizeSession();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Session.Profile, Is.EqualTo("SoloPlayer"));
            Assert.That(session.Session.Difficulty, Is.EqualTo("TR_CONFIG_NORMAL"));
            Assert.That(session.Session.ToleranceSemitones, Is.EqualTo(1));
        }

        [TestCase(EGameDifficulty.TR_CONFIG_EASY, ExpectedResult = 2)]
        [TestCase(EGameDifficulty.TR_CONFIG_NORMAL, ExpectedResult = 1)]
        [TestCase(EGameDifficulty.TR_CONFIG_HARD, ExpectedResult = 0)]
        public int TestDifficultyToleranceMetadataCalculation(EGameDifficulty difficulty)
        {
            var song = CreateMockSong(1, 4, 10);
            var players = CreateMockPlayers(difficulty);

            CTrainingRecorder.StartSession(song, players, 0, EGameMode.TR_GAMEMODE_NORMAL, EAudioMode.TR_AUDIOMODE_NORMAL, "DifficultyTester", difficulty);
            var session = CTrainingRecorder.FinalizeSession();
            return session.Session.ToleranceSemitones;
        }

        [Test]
        public void TestFinalizeSession_DecouplesNotesFromSubsequentCancelSession()
        {
            var song = CreateMockSong(1, 4, 10);
            var players = CreateMockPlayers();

            CTrainingRecorder.StartSession(song, players, 0, EGameMode.TR_GAMEMODE_NORMAL, EAudioMode.TR_AUDIOMODE_NORMAL, "DecoupleTester", EGameDifficulty.TR_CONFIG_NORMAL);
            CTrainingRecorder.RecordBeat(0, 0, 0, 0, 10, 10, true, true);

            var session = CTrainingRecorder.FinalizeSession();
            Assert.That(session.Notes.Count, Is.EqualTo(1));
            Assert.That(session.Notes[0].Chunks.Count, Is.EqualTo(1));

            // Calling CancelSession should NOT mutate session.Notes
            CTrainingRecorder.CancelSession();
            Assert.That(session.Notes.Count, Is.EqualTo(1));
            Assert.That(session.Notes[0].Chunks.Count, Is.EqualTo(1));
        }
    }
}
