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
using NUnit.Framework;
using Vocaluxe.Screens;
using VocaluxeLib;

namespace Tests.Screens
{
    [TestFixture]
    public class CHighscoreStatsTest
    {
        [Test]
        public void TestSongLoreInfo_PostSong_WithPriorScores_ShowsPriorDate()
        {
            var priorScore = new SDBScoreEntry
            {
                Id = 1,
                Name = "SingerA",
                Score = 8000,
                DateTicks = DateTime.Now.AddDays(-40).Ticks,
                Date = DateTime.Now.AddDays(-40).ToString("dd/MM/yyyy")
            };

            var currentScore = new SDBScoreEntry
            {
                Id = 2,
                Name = "SingerB",
                Score = 8500,
                DateTicks = DateTime.Now.Ticks,
                Date = DateTime.Now.ToString("dd/MM/yyyy")
            };

            var allScores = new List<SDBScoreEntry> { priorScore, currentScore };
            var priorScores = new List<SDBScoreEntry> { priorScore };

            var info = CHighscoreStats.GetSongLoreInfo(allScores, 2026, 50, 0, 1, priorScores);

            Assert.That(info.UniquePerformances, Is.EqualTo(2));
            Assert.That(info.HasLastSung, Is.True);
            Assert.That(info.LastSungAgeDays, Is.EqualTo(40));
            Assert.That(info.LastSungDate, Is.EqualTo(priorScore.Date));
        }

        [Test]
        public void TestSongLoreInfo_PostSong_SungEarlierToday_ShowsToday()
        {
            var earlierTodayScore = new SDBScoreEntry
            {
                Id = 1,
                Name = "SingerA",
                Score = 8000,
                DateTicks = DateTime.Now.AddHours(-2).Ticks,
                Date = DateTime.Now.ToString("dd/MM/yyyy")
            };

            var currentScore = new SDBScoreEntry
            {
                Id = 2,
                Name = "SingerA",
                Score = 9000,
                DateTicks = DateTime.Now.Ticks,
                Date = DateTime.Now.ToString("dd/MM/yyyy")
            };

            var allScores = new List<SDBScoreEntry> { earlierTodayScore, currentScore };
            var priorScores = new List<SDBScoreEntry> { earlierTodayScore };

            var info = CHighscoreStats.GetSongLoreInfo(allScores, 2026, 50, 0, 1, priorScores);

            Assert.That(info.UniquePerformances, Is.EqualTo(2));
            Assert.That(info.HasLastSung, Is.True);
            Assert.That(info.LastSungAgeDays, Is.EqualTo(0));
        }

        [Test]
        public void TestSongLoreInfo_PostSong_DebutPerformance_ShowsFirstSungToday()
        {
            var currentScore = new SDBScoreEntry
            {
                Id = 1,
                Name = "SingerA",
                Score = 7500,
                DateTicks = DateTime.Now.Ticks,
                Date = DateTime.Now.ToString("dd/MM/yyyy")
            };

            var allScores = new List<SDBScoreEntry> { currentScore };
            var priorScores = new List<SDBScoreEntry>();

            var info = CHighscoreStats.GetSongLoreInfo(allScores, 2026, 50, 0, 1, priorScores);

            Assert.That(info.UniquePerformances, Is.EqualTo(1));
            Assert.That(info.HasLastSung, Is.True);
            Assert.That(info.LastSungAgeDays, Is.EqualTo(0));
        }

        [Test]
        public void TestSongLoreInfo_SongList_ShowsLatestScore()
        {
            var oldScore = new SDBScoreEntry
            {
                Id = 1,
                Name = "SingerA",
                Score = 7000,
                DateTicks = DateTime.Now.AddDays(-20).Ticks,
                Date = DateTime.Now.AddDays(-20).ToString("dd/MM/yyyy")
            };

            var allScores = new List<SDBScoreEntry> { oldScore };

            // When opened from song list, priorScores is null
            var info = CHighscoreStats.GetSongLoreInfo(allScores, 2026, 50, 0, 1, null);

            Assert.That(info.UniquePerformances, Is.EqualTo(1));
            Assert.That(info.HasLastSung, Is.True);
            Assert.That(info.LastSungAgeDays, Is.EqualTo(20));
            Assert.That(info.LastSungDate, Is.EqualTo(oldScore.Date));
        }

        [Test]
        public void TestSongLoreInfo_SongList_EmptyScores_ShowsNeverSung()
        {
            var allScores = new List<SDBScoreEntry>();

            var info = CHighscoreStats.GetSongLoreInfo(allScores, 2026, 50, 0, 0, null);

            Assert.That(info.UniquePerformances, Is.EqualTo(0));
            Assert.That(info.HasLastSung, Is.False);
        }
    }
}
