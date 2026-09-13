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

namespace Vocaluxe.Screens
{
    public class SLeaderboardRow
    {
        public int ID;
        public int Rank;
        public bool ShowRank;
        public string Name;
        public int Score;
        public string Tag;
        public string Date;
        public SColorF Color;
        public bool HasParticles;
        public bool IsSession;
        public int VoiceNr;
    }

    public class SSongLoreInfo
    {
        public int UniquePerformances;
        public int TotalScores;
        public int SeasonCount;
        public bool HasAllTimeBest;
        public SDBScoreEntry AllTimeBest;
        public int RecordAgeDays;
        public string FactText;
    }

    public static class CHighscoreStats
    {
        public const int SeasonStartMonth = 9;

        public static int GetSeasonYear(DateTime date)
        {
            return (date.Month >= SeasonStartMonth) ? date.Year : date.Year - 1;
        }

        public static int GetSeasonYear(long ticks)
        {
            return GetSeasonYear(new DateTime(ticks));
        }

        public static int GetSeasonYear(SDBScoreEntry entry)
        {
            return GetSeasonYear(entry.DateTicks);
        }

        public static int GetCurrentSeasonYear()
        {
            return GetSeasonYear(DateTime.Now);
        }

        public static string FormatScoreDateTime(SDBScoreEntry entry)
        {
            if (entry.DateTicks > 0)
            {
                DateTime dt = new DateTime(entry.DateTicks);
                return dt.ToString("dd/MM/yyyy HH:mm");
            }
            if (!string.IsNullOrEmpty(entry.Date))
            {
                if (DateTime.TryParse(entry.Date, out DateTime dt))
                {
                    return dt.ToString("dd/MM/yyyy HH:mm");
                }
                return entry.Date;
            }
            return "";
        }

        public static string GetShortDifficulty(EGameDifficulty diff)
        {
            switch (diff)
            {
                case EGameDifficulty.TR_CONFIG_EASY:
                    return "[Easy]";
                case EGameDifficulty.TR_CONFIG_NORMAL:
                    return "[Norm.]";
                case EGameDifficulty.TR_CONFIG_HARD:
                    return "[Hard]";
                default:
                    return "[Norm.]";
            }
        }

        public static int CountUniquePerformances(List<SDBScoreEntry> scores, int timeWindowSeconds = 10)
        {
            if (scores == null || scores.Count == 0)
                return 0;

            long lastTicks = -1;
            int count = 0;
            var sorted = scores.OrderBy(s => s.DateTicks);
            long windowTicks = TimeSpan.TicksPerSecond * timeWindowSeconds;

            foreach (var s in sorted)
            {
                if (lastTicks == -1 || Math.Abs(s.DateTicks - lastTicks) > windowTicks)
                {
                    count++;
                }
                lastTicks = s.DateTicks;
            }
            return count;
        }

        public static List<SLeaderboardRow> BuildLeaderboardRows(
            List<SDBScoreEntry> scores,
            List<SLeaderboardRow> sessionRows,
            int seasonYear,
            bool isDuet,
            int maxRows,
            SColorF colorSession,
            SColorF colorSeason,
            SColorF colorNormal)
        {
            scores = scores ?? new List<SDBScoreEntry>();
            sessionRows = sessionRows ?? new List<SLeaderboardRow>();

            Func<SDBScoreEntry, string> getMicKey = s => s.Name + (isDuet ? " (P" + (s.VoiceNr + 1) + ")" : "");
            var candidateRows = new List<SLeaderboardRow>();

            // (A) Session rows first
            foreach (var sess in sessionRows)
            {
                candidateRows.Add(sess);
            }

            // (B) Seasonal bests for the selected season
            var seasonScores = scores.Where(s => GetSeasonYear(s) == seasonYear).ToList();
            var seasonBests = seasonScores
                .GroupBy(getMicKey)
                .Select(g => g.OrderByDescending(s => s.Score).First())
                .ToList();

            foreach (var entry in seasonBests)
            {
                string displayName = getMicKey(entry);
                string entryDate = FormatScoreDateTime(entry);

                if (candidateRows.Any(r => (r.ID > 0 && r.ID == entry.ID) || (r.Name == displayName && r.Score == entry.Score && r.Date == entryDate)))
                    continue;

                candidateRows.Add(new SLeaderboardRow
                {
                    ID = entry.ID,
                    Name = displayName,
                    Score = entry.Score,
                    Tag = CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON"),
                    Date = entryDate,
                    Color = colorSeason,
                    HasParticles = false,
                    IsSession = false,
                    VoiceNr = entry.VoiceNr
                });
            }

            // (C) All-time bests (per player/mic)
            var allTimeBests = scores
                .GroupBy(getMicKey)
                .Select(g => g.OrderByDescending(s => s.Score).First())
                .ToList();

            foreach (var entry in allTimeBests)
            {
                string displayName = getMicKey(entry);
                string entryDate = FormatScoreDateTime(entry);

                if (candidateRows.Any(r => (r.ID > 0 && r.ID == entry.ID) || (r.Name == displayName && r.Score == entry.Score && r.Date == entryDate)))
                    continue;

                bool isSeason = GetSeasonYear(entry) == seasonYear;
                SColorF color = isSeason ? colorSeason : colorNormal;
                string tag = isSeason ? CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON") : (entry.Difficulty != EGameDifficulty.TR_CONFIG_NORMAL ? GetShortDifficulty(entry.Difficulty) : "");

                candidateRows.Add(new SLeaderboardRow
                {
                    ID = entry.ID,
                    Name = displayName,
                    Score = entry.Score,
                    Tag = tag,
                    Date = entryDate,
                    Color = color,
                    HasParticles = false,
                    IsSession = false,
                    VoiceNr = entry.VoiceNr
                });
            }

            // (D) Backfill if remaining slots exist
            if (candidateRows.Count < maxRows)
            {
                var remainingScores = scores.OrderByDescending(s => s.Score).ToList();
                foreach (var entry in remainingScores)
                {
                    if (candidateRows.Count >= maxRows)
                        break;

                    string displayName = getMicKey(entry);
                    string entryDate = FormatScoreDateTime(entry);

                    if (candidateRows.Any(r => (r.ID > 0 && r.ID == entry.ID) || (r.Name == displayName && r.Score == entry.Score && r.Date == entryDate)))
                        continue;

                    bool isSeason = GetSeasonYear(entry) == seasonYear;
                    SColorF color = isSeason ? colorSeason : colorNormal;
                    string tag = isSeason ? CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON") : (entry.Difficulty != EGameDifficulty.TR_CONFIG_NORMAL ? GetShortDifficulty(entry.Difficulty) : "");

                    candidateRows.Add(new SLeaderboardRow
                    {
                        ID = entry.ID,
                        Name = displayName,
                        Score = entry.Score,
                        Tag = tag,
                        Date = entryDate,
                        Color = color,
                        HasParticles = false,
                        IsSession = false,
                        VoiceNr = entry.VoiceNr
                    });
                }
            }

            // Sort all candidates by score descending
            candidateRows = candidateRows.OrderByDescending(r => r.Score).ToList();

            // Pre-build score pools per voice for O(N log N) rank calculation
            var voicePools = new Dictionary<int, List<int>>();
            var voicesToProcess = candidateRows.Select(r => isDuet ? r.VoiceNr : 0).Distinct();

            foreach (var v in voicesToProcess)
            {
                var pool = scores
                    .Where(s => !isDuet || s.VoiceNr == v)
                    .Select(s => s.Score)
                    .ToList();

                foreach (var sess in sessionRows.Where(s => !isDuet || s.VoiceNr == v))
                {
                    if (!pool.Contains(sess.Score))
                        pool.Add(sess.Score);
                }

                voicePools[v] = pool;
            }

            for (int i = 0; i < candidateRows.Count; i++)
            {
                var row = candidateRows[i];
                int voiceKey = isDuet ? row.VoiceNr : 0;
                var pool = voicePools.ContainsKey(voiceKey) ? voicePools[voiceKey] : new List<int>();

                int totalScores = pool.Count;
                int absoluteRank = pool.Count(sc => sc > row.Score) + 1;
                row.Rank = absoluteRank;
                row.ShowRank = totalScores == 0 || absoluteRank <= Math.Ceiling(totalScores * 0.5f);
            }

            // Selection with Sticky Session Row support
            var displayRows = new List<SLeaderboardRow>();
            if (candidateRows.Count <= maxRows)
            {
                displayRows.AddRange(candidateRows);
            }
            else
            {
                var overflowSessionRows = candidateRows
                    .Skip(maxRows)
                    .Where(r => r.IsSession)
                    .ToList();

                int normalSlots = maxRows - overflowSessionRows.Count;
                displayRows.AddRange(candidateRows.Take(normalSlots));
                displayRows.AddRange(overflowSessionRows);
            }

            return displayRows;
        }

        public static SSongLoreInfo GetSongLoreInfo(
            List<SDBScoreEntry> scores,
            int seasonYear,
            int totalDbScores,
            int sessionRecordsBroken,
            int sessionSongsCount)
        {
            var info = new SSongLoreInfo();
            scores = scores ?? new List<SDBScoreEntry>();

            info.TotalScores = scores.Count;
            info.UniquePerformances = CountUniquePerformances(scores);
            info.SeasonCount = scores.Count(s => GetSeasonYear(s) == seasonYear);

            var best = scores.OrderByDescending(s => s.Score).FirstOrDefault();
            if (best.Name != null)
            {
                info.HasAllTimeBest = true;
                info.AllTimeBest = best;
                DateTime recordDate = new DateTime(best.DateTicks);
                info.RecordAgeDays = Math.Max(0, (int)(DateTime.Now - recordDate).TotalDays);
            }

            info.FactText = EvaluateSongLoreFact(scores, totalDbScores, sessionRecordsBroken, sessionSongsCount);
            return info;
        }

        public static string EvaluateSongLoreFact(
            List<SDBScoreEntry> scores,
            int totalDbScores,
            int sessionRecordsBroken,
            int sessionSongsCount)
        {
            var sortedScores = (scores ?? new List<SDBScoreEntry>()).OrderBy(s => s.DateTicks).ToList();

            // Priority 1: Overall Club Milestones (Every 1,000 performances: 1000, 2000...)
            if (totalDbScores >= 1000 && (totalDbScores % 1000 <= 10))
            {
                int thousandMilestone = (totalDbScores / 1000) * 1000;
                return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_MILESTONE"), thousandMilestone);
            }

            // Priority 2: Long Drought Broken (>= 30 days since song was last performed)
            if (sortedScores.Count > 0)
            {
                var pastScores = sortedScores.Where(s => (DateTime.Now - new DateTime(s.DateTicks)).TotalHours > 12).OrderByDescending(s => s.DateTicks).ToList();
                if (pastScores.Count > 0)
                {
                    int daysAgo = (int)(DateTime.Now - new DateTime(pastScores.First().DateTicks)).TotalDays;
                    if (daysAgo >= 30)
                    {
                        return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_DROUGHT"), daysAgo);
                    }
                }
            }

            // Priority 3: Session Records Broken
            if (sessionRecordsBroken > 0)
            {
                return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_SESSION_RECORDS"), sessionRecordsBroken);
            }

            // Priority 4: Session Songs Milestone (Only every 50 songs: 50, 100, 150...)
            if (sessionSongsCount >= 50 && (sessionSongsCount % 50 == 0))
            {
                return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_SESSION_SONGS"), sessionSongsCount);
            }

            // Priority 5: First Performance Date at Club (if > 7 days ago)
            if (sortedScores.Count > 0)
            {
                var earliest = sortedScores.First();
                int daysSinceFirst = (int)(DateTime.Now - new DateTime(earliest.DateTicks)).TotalDays;
                if (daysSinceFirst > 7)
                {
                    return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_FACT_FIRST_PERFORMED"), earliest.Date, daysSinceFirst);
                }
            }

            // Priority 6: Total Club Performances Fallback
            return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_TOTAL_PERFORMANCES"), totalDbScores);
        }
    }
}
