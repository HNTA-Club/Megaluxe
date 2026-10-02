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

using System.Collections.Generic;
using NUnit.Framework;
using Vocaluxe.Training;

namespace Tests.Training
{
    [TestFixture]
    public class TrainingStorageTests
    {
        [TestCase("AC/DC", ExpectedResult = "AC_DC")]
        [TestCase("Song: Title? *Yes*", ExpectedResult = "Song_ Title_ _Yes_")]
        [TestCase("Artist <Name> | Special \"Quotes\"", ExpectedResult = "Artist _Name_ _ Special _Quotes_")]
        [TestCase("Normal Artist", ExpectedResult = "Normal Artist")]
        public string TestSanitizePath(string input)
        {
            return CTrainingStorage.SanitizePath(input);
        }

        [Test]
        public void TestGetSongDirectoryPath()
        {
            var dir = CTrainingStorage.GetSongDirectory("Queen", "Bohemian Rhapsody", "Singer1");
            Assert.That(dir, Does.Contain("Training"));
            Assert.That(dir, Does.Contain("Queen - Bohemian Rhapsody"));
            Assert.That(dir, Does.Contain("Singer1"));
        }

        [Test]
        public void TestJsonSerializationRoundTrip()
        {
            var session = new STrainingSessionData
            {
                Version = 1,
                Song = new STrainingSongInfo
                {
                    Artist = "Queen",
                    Title = "Bohemian Rhapsody",
                    Bpm = 288f,
                    Gap = 15000f,
                    Voice = 0,
                    GameMode = "TR_GAMEMODE_NORMAL",
                    AudioMode = "TR_AUDIOMODE_NORMAL"
                },
                Session = new STrainingSessionMetadata
                {
                    Profile = "Singer1",
                    Difficulty = "TR_CONFIG_NORMAL",
                    ToleranceSemitones = 1,
                    Timestamp = "2026-10-03T01:25:00Z",
                    TotalNotes = 1,
                    NotesEvaluated = 1,
                    HitRatio = 0.75,
                    Score = 750
                },
                Notes = new List<STrainingNote>
                {
                    new STrainingNote
                    {
                        NoteIndex = 0,
                        LineIndex = 0,
                        Text = "Ma-",
                        Type = "Normal",
                        TargetTone = 14,
                        StartBeat = 128,
                        DurationBeats = 8,
                        DurationMs = 416.7,
                        HitBeats = 6,
                        HitPercentage = 75.0,
                        PrimaryMissDirection = "Flat",
                        AvgPitchOffset = -0.35,
                        Chunks = new List<STrainingChunk>
                        {
                            new STrainingChunk
                            {
                                StartBeat = 128,
                                DurationBeats = 2,
                                DurationMs = 104.2,
                                SungTone = 13,
                                PitchOffset = -1,
                                Status = "Flat",
                                Hit = false
                            },
                            new STrainingChunk
                            {
                                StartBeat = 130,
                                DurationBeats = 6,
                                DurationMs = 312.5,
                                SungTone = 14,
                                PitchOffset = 0,
                                Status = "Target",
                                Hit = true
                            }
                        }
                    }
                }
            };

            var json = CTrainingStorage.Serialize(session);
            Assert.That(json, Does.Contain("Bohemian Rhapsody"));
            Assert.That(json, Does.Contain("Ma-"));
            Assert.That(json, Does.Contain("Flat"));

            var deserialized = CTrainingStorage.Deserialize(json);
            Assert.That(deserialized.Version, Is.EqualTo(1));
            Assert.That(deserialized.Song.Artist, Is.EqualTo("Queen"));
            Assert.That(deserialized.Song.Title, Is.EqualTo("Bohemian Rhapsody"));
            Assert.That(deserialized.Notes.Count, Is.EqualTo(1));
            Assert.That(deserialized.Notes[0].Chunks.Count, Is.EqualTo(2));
            Assert.That(deserialized.Notes[0].Chunks[0].PitchOffset, Is.EqualTo(-1));
            Assert.That(deserialized.Notes[0].Chunks[1].Hit, Is.True);
        }

        [Test]
        public void TestClearSongData_NonExistent_ReturnsFalse()
        {
            var res = CTrainingStorage.ClearSongData("NonExistentArtist_XYZ_123", "NonExistentTitle_XYZ_123");
            Assert.That(res, Is.False);
        }

        [Test]
        public void TestClearSongData_Existing_DeletesDirectoryAndReturnsTrue()
        {
            var artist = "TestArtistClear";
            var title = "TestTitleClear";
            var profile = "TestProfileClear";

            var dir = CTrainingStorage.GetSongDirectory(artist, title, profile);
            System.IO.Directory.CreateDirectory(dir);
            var testFilePath = System.IO.Path.Combine(dir, "run_test.json");
            System.IO.File.WriteAllText(testFilePath, "{}");

            Assert.That(System.IO.Directory.Exists(dir), Is.True);

            var cleared = CTrainingStorage.ClearSongData(artist, title);
            Assert.That(cleared, Is.True);

            var songBaseDir = System.IO.Path.GetDirectoryName(dir);
            Assert.That(System.IO.Directory.Exists(songBaseDir), Is.False);
        }
    }
}
