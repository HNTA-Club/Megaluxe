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
        public bool HasLastSung;
        public int LastSungAgeDays;
        public string LastSungDate;
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
            if (ticks <= 0 || ticks > DateTime.MaxValue.Ticks) return GetCurrentSeasonYear();
            try { return GetSeasonYear(new DateTime(ticks)); } catch { return GetCurrentSeasonYear(); }
        }

        public static int GetSeasonYear(SDBScoreEntry entry)
        {
            if (entry.DateTicks > 0)
                return GetSeasonYear(entry.DateTicks);
            if (!string.IsNullOrEmpty(entry.Date) && DateTime.TryParse(entry.Date, out DateTime dt))
                return GetSeasonYear(dt);
            return GetCurrentSeasonYear();
        }

        public static int GetCurrentSeasonYear()
        {
            return GetSeasonYear(DateTime.Now);
        }

        public static string FormatScoreDateTime(SDBScoreEntry entry)
        {
            if (entry.DateTicks > 0 && entry.DateTicks <= DateTime.MaxValue.Ticks)
            {
                try
                {
                    DateTime dt = new DateTime(entry.DateTicks);
                    return dt.ToString("dd/MM/yyyy HH:mm");
                }
                catch {}
            }
            if (!string.IsNullOrEmpty(entry.Date))
            {
                return entry.Date;
            }
            return "";
        }

        public static string GetShortDifficulty(EGameDifficulty diff)
        {
            switch (diff)
            {
                case EGameDifficulty.TR_CONFIG_EASY:
                case EGameDifficulty.TR_CONFIG_NORMAL:
                case EGameDifficulty.TR_CONFIG_HARD:
                    return "[" + CLanguage.Translate(diff.ToString()) + "]";
                default:
                    return "[" + CLanguage.Translate("TR_CONFIG_NORMAL") + "]";
            }
        }

        public static int CountUniquePerformances(List<SDBScoreEntry> scores, int timeWindowSeconds = 10)
        {
            if (scores == null || scores.Count == 0)
                return 0;

            int count = 0;
            var withTicks = scores.Where(s => s.DateTicks > 0).OrderBy(s => s.DateTicks).ToList();
            var withoutTicks = scores.Where(s => s.DateTicks <= 0).ToList();

            if (withTicks.Count > 0)
            {
                long lastTicks = -1;
                long windowTicks = TimeSpan.TicksPerSecond * timeWindowSeconds;
                foreach (var s in withTicks)
                {
                    if (lastTicks == -1 || Math.Abs(s.DateTicks - lastTicks) > windowTicks)
                    {
                        count++;
                    }
                    lastTicks = s.DateTicks;
                }
            }

            if (withoutTicks.Count > 0)
            {
                var dateGroups = withoutTicks.GroupBy(s => !string.IsNullOrEmpty(s.Date) ? s.Date : s.Id.ToString());
                count += dateGroups.Count();
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

                if (candidateRows.Any(r => (r.ID > 0 && r.ID == entry.Id) || (r.ID <= 0 && r.Name == displayName && Math.Abs(r.Score - entry.Score) <= 1 && r.Date == entryDate)))
                    continue;

                candidateRows.Add(new SLeaderboardRow
                {
                    ID = entry.Id,
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

                if (candidateRows.Any(r => (r.ID > 0 && r.ID == entry.Id) || (r.ID <= 0 && r.Name == displayName && Math.Abs(r.Score - entry.Score) <= 1 && r.Date == entryDate)))
                    continue;

                bool isSeason = GetSeasonYear(entry) == seasonYear;
                SColorF color = isSeason ? colorSeason : colorNormal;
                string tag = isSeason ? CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON") : (entry.Difficulty != EGameDifficulty.TR_CONFIG_NORMAL ? GetShortDifficulty(entry.Difficulty) : "");

                candidateRows.Add(new SLeaderboardRow
                {
                    ID = entry.Id,
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

                    if (candidateRows.Any(r => (r.ID > 0 && r.ID == entry.Id) || (r.ID <= 0 && r.Name == displayName && Math.Abs(r.Score - entry.Score) <= 1 && r.Date == entryDate)))
                        continue;

                    bool isSeason = GetSeasonYear(entry) == seasonYear;
                    SColorF color = isSeason ? colorSeason : colorNormal;
                    string tag = isSeason ? CLanguage.Translate("TR_SCREENHIGHSCORE_TAG_SEASON") : (entry.Difficulty != EGameDifficulty.TR_CONFIG_NORMAL ? GetShortDifficulty(entry.Difficulty) : "");

                    candidateRows.Add(new SLeaderboardRow
                    {
                        ID = entry.Id,
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
                    // Avoid duplicating an entry if it was already loaded from DB scores
                    if (sess.ID <= 0 || !scores.Any(s => s.Id == sess.ID))
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

                int normalSlots = Math.Max(0, maxRows - overflowSessionRows.Count);
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
            int sessionSongsCount,
            List<SDBScoreEntry> priorScores = null)
        {
            var info = new SSongLoreInfo();
            scores = scores ?? new List<SDBScoreEntry>();

            info.TotalScores = scores.Count;
            info.UniquePerformances = CountUniquePerformances(scores);
            var seasonScores = scores.Where(s => GetSeasonYear(s) == seasonYear).ToList();
            info.SeasonCount = CountUniquePerformances(seasonScores);

            var best = scores.OrderByDescending(s => s.Score).FirstOrDefault();
            if (best.Name != null)
            {
                info.HasAllTimeBest = true;
                if (string.IsNullOrEmpty(best.Date) && best.DateTicks > 0 && best.DateTicks <= DateTime.MaxValue.Ticks)
                {
                    try { best.Date = new DateTime(best.DateTicks).ToString("dd/MM/yyyy"); } catch {}
                }
                info.AllTimeBest = best;
                if (best.DateTicks > 0 && best.DateTicks <= DateTime.MaxValue.Ticks)
                {
                    try
                    {
                        DateTime recordDate = new DateTime(best.DateTicks);
                        info.RecordAgeDays = Math.Max(0, (int)(DateTime.Now - recordDate).TotalDays);
                    }
                    catch {}
                }
                else if (!string.IsNullOrEmpty(best.Date) && DateTime.TryParse(best.Date, out DateTime parsedDate))
                {
                    info.RecordAgeDays = Math.Max(0, (int)(DateTime.Now - parsedDate).TotalDays);
                }
            }

            // Target scores for calculating "last sung":
            // When priorScores is provided (e.g. post-song screen), we evaluate when the song was sung
            // prior to the performance that just finished.
            // If priorScores is empty, it means this performance was the song's debut at the club.
            // When priorScores is null (e.g. song list screen), we evaluate the most recent performance overall.
            List<SDBScoreEntry> targetScores = priorScores ?? scores;
            var validScores = targetScores
                .Where(s => s.DateTicks > 0 && s.DateTicks <= DateTime.MaxValue.Ticks)
                .OrderBy(s => s.DateTicks)
                .ToList();

            if (validScores.Count > 0)
            {
                var latest = validScores.Last();
                info.HasLastSung = true;
                info.LastSungDate = !string.IsNullOrEmpty(latest.Date) ? latest.Date : new DateTime(latest.DateTicks).ToString("dd/MM/yyyy");
                DateTime lastDate = new DateTime(latest.DateTicks);
                info.LastSungAgeDays = Math.Max(0, (int)(DateTime.Now - lastDate).TotalDays);
            }
            else if (targetScores.Count > 0)
            {
                var scoresWithDate = targetScores
                    .Where(s => !string.IsNullOrEmpty(s.Date) && DateTime.TryParse(s.Date, out _))
                    .ToList();
                if (scoresWithDate.Count > 0)
                {
                    var latest = scoresWithDate.OrderByDescending(s => DateTime.Parse(s.Date)).First();
                    DateTime lastDate = DateTime.Parse(latest.Date);
                    info.HasLastSung = true;
                    info.LastSungDate = lastDate.ToString("dd/MM/yyyy");
                    info.LastSungAgeDays = Math.Max(0, (int)(DateTime.Now - lastDate).TotalDays);
                }
            }
            else if (priorScores != null && scores.Count > 0)
            {
                // Debut performance on post-song screen (no prior scores, but played now)
                info.HasLastSung = true;
                var latestCurrent = scores.Where(s => s.DateTicks > 0 && s.DateTicks <= DateTime.MaxValue.Ticks).OrderBy(s => s.DateTicks).LastOrDefault();
                if (latestCurrent.DateTicks > 0)
                {
                    info.LastSungDate = !string.IsNullOrEmpty(latestCurrent.Date) ? latestCurrent.Date : new DateTime(latestCurrent.DateTicks).ToString("dd/MM/yyyy");
                    info.LastSungAgeDays = Math.Max(0, (int)(DateTime.Now - new DateTime(latestCurrent.DateTicks)).TotalDays);
                }
                else
                {
                    info.LastSungDate = DateTime.Now.ToString("dd/MM/yyyy");
                    info.LastSungAgeDays = 0;
                }
            }

            info.FactText = EvaluateSongLoreFact(scores, totalDbScores, sessionRecordsBroken, sessionSongsCount, priorScores);
            return info;
        }

        public static string EvaluateSongLoreFact(
            List<SDBScoreEntry> scores,
            int totalDbScores,
            int sessionRecordsBroken,
            int sessionSongsCount,
            List<SDBScoreEntry> priorScores = null)
        {
            var validScores = (scores ?? new List<SDBScoreEntry>())
                .Where(s => s.DateTicks > 0 && s.DateTicks <= DateTime.MaxValue.Ticks)
                .OrderBy(s => s.DateTicks)
                .ToList();

            // Priority 1: Overall Club Milestones (Every 1,000 performances: 1000, 2000...)
            if (totalDbScores >= 1000 && (totalDbScores % 1000 <= 10))
            {
                int thousandMilestone = (totalDbScores / 1000) * 1000;
                return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_MILESTONE"), thousandMilestone.ToString("N0"));
            }

            // Priority 2: Long Drought Broken (>= 30 days since song was last performed, and sung recently within last 12h)
            if (priorScores != null)
            {
                var validPrior = priorScores
                    .Where(s => s.DateTicks > 0 && s.DateTicks <= DateTime.MaxValue.Ticks)
                    .OrderBy(s => s.DateTicks)
                    .ToList();
                if (validPrior.Count > 0)
                {
                    int daysAgo = (int)(DateTime.Now - new DateTime(validPrior.Last().DateTicks)).TotalDays;
                    if (daysAgo >= 30)
                    {
                        return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_DROUGHT"), daysAgo);
                    }
                }
            }
            else if (validScores.Count > 1)
            {
                double lastHoursAgo = (DateTime.Now - new DateTime(validScores.Last().DateTicks)).TotalHours;
                bool sungRecently = lastHoursAgo >= -0.5 && lastHoursAgo <= 12;
                if (sungRecently)
                {
                    var pastScores = validScores.Where(s => (DateTime.Now - new DateTime(s.DateTicks)).TotalHours > 12).OrderByDescending(s => s.DateTicks).ToList();
                    if (pastScores.Count > 0)
                    {
                        int daysAgo = (int)(DateTime.Now - new DateTime(pastScores.First().DateTicks)).TotalDays;
                        if (daysAgo >= 30)
                        {
                            return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_DROUGHT"), daysAgo);
                        }
                    }
                }
            }

            // Priority 3: First Performance Date at Club (if > 7 days ago) - song-specific history
            if (validScores.Count > 0)
            {
                var earliest = validScores.First();
                int daysSinceFirst = (int)(DateTime.Now - new DateTime(earliest.DateTicks)).TotalDays;
                if (daysSinceFirst > 7)
                {
                    string dateStr = !string.IsNullOrEmpty(earliest.Date) ? earliest.Date : new DateTime(earliest.DateTicks).ToString("dd/MM/yyyy");
                    return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_FACT_FIRST_PERFORMED"), dateStr, daysSinceFirst);
                }
            }

            // Priority 4: Session Records Broken
            if (sessionRecordsBroken > 0)
            {
                return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_SESSION_RECORDS"), sessionRecordsBroken);
            }

            // Priority 5: Session Songs Milestone (Only every 50 songs: 50, 100, 150...)
            if (sessionSongsCount >= 50 && (sessionSongsCount % 50 == 0))
            {
                return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_SESSION_SONGS"), sessionSongsCount);
            }

            // Priority 6: Total Club Performances Fallback
            return String.Format(CLanguage.Translate("TR_SCREENHIGHSCORE_HIGHLIGHT_TOTAL_PERFORMANCES"), totalDbScores.ToString("N0"));
        }
    }
}
