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
using Newtonsoft.Json;

namespace Vocaluxe.Training
{
    public class STrainingChunk
    {
        [JsonProperty("startBeat")]
        public int StartBeat { get; set; }

        [JsonProperty("durationBeats")]
        public int DurationBeats { get; set; }

        [JsonProperty("durationMs")]
        public double DurationMs { get; set; }

        [JsonProperty("sungTone")]
        public int? SungTone { get; set; }

        [JsonProperty("pitchOffset")]
        public int? PitchOffset { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("hit")]
        public bool Hit { get; set; }
    }

    public class STrainingNote
    {
        [JsonProperty("noteIndex")]
        public int NoteIndex { get; set; }

        [JsonProperty("lineIndex")]
        public int LineIndex { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("targetTone")]
        public int TargetTone { get; set; }

        [JsonProperty("startBeat")]
        public int StartBeat { get; set; }

        [JsonProperty("durationBeats")]
        public int DurationBeats { get; set; }

        [JsonProperty("durationMs")]
        public double DurationMs { get; set; }

        [JsonProperty("hitBeats")]
        public int HitBeats { get; set; }

        [JsonProperty("hitPercentage")]
        public double HitPercentage { get; set; }

        [JsonProperty("primaryMissDirection")]
        public string PrimaryMissDirection { get; set; }

        [JsonProperty("avgPitchOffset")]
        public double? AvgPitchOffset { get; set; }

        [JsonProperty("chunks")]
        public List<STrainingChunk> Chunks { get; set; } = new List<STrainingChunk>();
    }

    public class STrainingSongInfo
    {
        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("bpm")]
        public float Bpm { get; set; }

        [JsonProperty("gap")]
        public float Gap { get; set; }

        [JsonProperty("voice")]
        public int Voice { get; set; }

        [JsonProperty("gameMode")]
        public string GameMode { get; set; }

        [JsonProperty("audioMode")]
        public string AudioMode { get; set; }
    }

    public class STrainingSessionMetadata
    {
        [JsonProperty("profile")]
        public string Profile { get; set; }

        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("toleranceSemitones")]
        public int ToleranceSemitones { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("totalNotes")]
        public int TotalNotes { get; set; }

        [JsonProperty("notesEvaluated")]
        public int NotesEvaluated { get; set; }

        [JsonProperty("hitRatio")]
        public double HitRatio { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }
    }

    public class STrainingSessionData
    {
        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("song")]
        public STrainingSongInfo Song { get; set; }

        [JsonProperty("session")]
        public STrainingSessionMetadata Session { get; set; }

        [JsonProperty("notes")]
        public List<STrainingNote> Notes { get; set; } = new List<STrainingNote>();
    }
}
