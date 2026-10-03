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
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vocaluxe.Base;
using VocaluxeLib.Log;

namespace Vocaluxe.Training
{
    public static class CTrainingStorage
    {
        private const string ExtraInvalidChars = "<>:\"/\\|?*";
        private static readonly Regex InvalidCharsRegex = new Regex(
            $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()) + ExtraInvalidChars)}]",
            RegexOptions.Compiled);

        public static string SanitizePath(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Unknown";
            }

            var sanitized = InvalidCharsRegex.Replace(input, "_");
            return sanitized.Trim();
        }

        public static string GetSongDirectory(string artist, string title, string profile)
        {
            var songDirName = SanitizePath($"{artist} - {title}");
            var profileDirName = SanitizePath(profile);

            return Path.Combine(CSettings.DataFolder, "Training", songDirName, profileDirName);
        }

        public static string Serialize(STrainingSessionData data)
        {
            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public static STrainingSessionData Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<STrainingSessionData>(json);
        }

        public static Task<string> SaveRunAsync(STrainingSessionData data)
        {
            return Task.Run(() =>
            {
                try
                {
                    var dir = GetSongDirectory(data.Song?.Artist ?? "Unknown", data.Song?.Title ?? "Unknown", data.Session?.Profile ?? "Player");
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    var filePath = Path.Combine(dir, $"run_{timestamp}.json");
                    var json = Serialize(data);

                    File.WriteAllText(filePath, json);
                    CLog.Information($"Training run saved successfully: {filePath}");
                    return filePath;
                }
                catch (Exception ex)
                {
                    CLog.Error($"Failed to save training run: {ex.Message}");
                    return null;
                }
            });
        }

        public static bool ClearSongData(string artist, string title)
        {
            try
            {
                var songDirName = SanitizePath($"{artist} - {title}");
                var songPath = Path.Combine(CSettings.DataFolder, "Training", songDirName);

                if (Directory.Exists(songPath))
                {
                    Directory.Delete(songPath, true);
                    CLog.Information($"Training logs cleared for song: {songDirName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                CLog.Error($"Failed to clear training logs for {artist} - {title}: {ex.Message}");
                return false;
            }
        }
    }
}
