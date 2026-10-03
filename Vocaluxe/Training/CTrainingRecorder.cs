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
using VocaluxeLib.Songs;

namespace Vocaluxe.Training
{
    public static class CTrainingRecorder
    {
        private static bool _IsActive;
        private static CSong _Song;
        private static int _ActivePlayerIndex;
        private static int _VoiceNr;
        private static string _ProfileName;
        private static EGameDifficulty _Difficulty;
        private static int _ToleranceSemitones;
        private static EGameMode _GameMode;
        private static EAudioMode _AudioMode;

        private static List<STrainingNote> _Notes = new List<STrainingNote>();
        private static STrainingNote[][] _NotesMap;
        private static STrainingNote _ActiveNote;
        private static STrainingChunk _ActiveChunk;
        private static int _CurrentLineIndex = -1;
        private static int _CurrentNoteIndex = -1;

        public static bool IsActive => _IsActive;

        public static void StartSession(CSong song, SPlayer[] players, int activePlayerIndex, EGameMode gameMode, EAudioMode audioMode)
        {
            if (song == null || players == null || activePlayerIndex < 0 || activePlayerIndex >= players.Length)
            {
                CancelSession();
                return;
            }

            var profileId = players[activePlayerIndex].ProfileId;
            var profileName = CProfiles.GetPlayerName(profileId, activePlayerIndex + 1);
            var difficulty = CProfiles.GetDifficulty(profileId);

            StartSession(song, players, activePlayerIndex, gameMode, audioMode, profileName, difficulty);
        }

        public static void StartSession(CSong song, SPlayer[] players, int activePlayerIndex, EGameMode gameMode, EAudioMode audioMode, string profileName, EGameDifficulty difficulty)
        {
            CancelSession();

            if (song == null || players == null || activePlayerIndex < 0 || activePlayerIndex >= players.Length)
            {
                return;
            }

            _Song = song;
            _ActivePlayerIndex = activePlayerIndex;
            _VoiceNr = players[activePlayerIndex].VoiceNr;
            _ProfileName = profileName ?? "Player";
            _Difficulty = difficulty;
            _ToleranceSemitones = 2 - (int)difficulty;
            _GameMode = gameMode;
            _AudioMode = audioMode;

            _Notes = new List<STrainingNote>();
            var voice = song.Notes.GetVoice(_VoiceNr);
            if (voice != null)
            {
                var noteCounter = 0;
                _NotesMap = new STrainingNote[voice.Lines.Length][];
                for (var l = 0; l < voice.Lines.Length; l++)
                {
                    var line = voice.Lines[l];
                    _NotesMap[l] = new STrainingNote[line.Notes.Length];
                    for (var n = 0; n < line.Notes.Length; n++)
                    {
                        var note = line.Notes[n];
                        var durationMs = song.Bpm > 0 ? note.Duration * (60000.0 / (song.Bpm * 4.0)) : 0.0;

                        var trainingNote = new STrainingNote
                        {
                            NoteIndex = noteCounter++,
                            LineIndex = l,
                            Text = note.Text,
                            Type = note.Type.ToString(),
                            TargetTone = note.Tone,
                            StartBeat = note.StartBeat,
                            DurationBeats = note.Duration,
                            DurationMs = durationMs,
                            Chunks = new List<STrainingChunk>()
                        };
                        _Notes.Add(trainingNote);
                        _NotesMap[l][n] = trainingNote;
                    }
                }
            }
            else
            {
                _NotesMap = new STrainingNote[0][];
            }

            _IsActive = true;
        }

        public static void RecordBeat(int playerIndex, int lineIndex, int noteIndex, int beat, int targetTone, int sungTone, bool isToneValid, bool hit)
        {
            if (!_IsActive || playerIndex != _ActivePlayerIndex)
            {
                return;
            }

            int? sungToneVal = null;
            int? pitchOffset = null;
            string status;

            if (!isToneValid)
            {
                status = "Silent";
                hit = false;
            }
            else
            {
                sungToneVal = sungTone;
                pitchOffset = sungTone - targetTone;
                if (pitchOffset == 0)
                {
                    status = "Target";
                }
                else if (pitchOffset < 0)
                {
                    status = "Flat";
                }
                else
                {
                    status = "Sharp";
                }
            }

            if (_CurrentLineIndex != lineIndex || _CurrentNoteIndex != noteIndex)
            {
                FlushActiveChunk();

                _CurrentLineIndex = lineIndex;
                _CurrentNoteIndex = noteIndex;

                if (_NotesMap != null &&
                    lineIndex >= 0 && lineIndex < _NotesMap.Length &&
                    noteIndex >= 0 && noteIndex < _NotesMap[lineIndex].Length)
                {
                    _ActiveNote = _NotesMap[lineIndex][noteIndex];
                }
                else
                {
                    _ActiveNote = null;
                }

                StartNewChunk(beat, sungToneVal, pitchOffset, status, hit);
                return;
            }

            if (_ActiveChunk != null &&
                _ActiveChunk.Status == status &&
                _ActiveChunk.SungTone == sungToneVal &&
                _ActiveChunk.PitchOffset == pitchOffset &&
                _ActiveChunk.Hit == hit &&
                (_ActiveChunk.StartBeat + _ActiveChunk.DurationBeats) == beat)
            {
                _ActiveChunk.DurationBeats++;
            }
            else
            {
                FlushActiveChunk();
                StartNewChunk(beat, sungToneVal, pitchOffset, status, hit);
            }
        }

        private static void StartNewChunk(int beat, int? sungTone, int? pitchOffset, string status, bool hit)
        {
            _ActiveChunk = new STrainingChunk
            {
                StartBeat = beat,
                DurationBeats = 1,
                SungTone = sungTone,
                PitchOffset = pitchOffset,
                Status = status,
                Hit = hit
            };
        }

        private static void FlushActiveChunk()
        {
            if (_ActiveChunk != null)
            {
                if (_ActiveNote != null)
                {
                    _ActiveNote.Chunks.Add(_ActiveChunk);
                }
                _ActiveChunk = null;
            }
        }

        public static void CancelSession()
        {
            _IsActive = false;
            _Song = null;
            _Notes.Clear();
            _NotesMap = null;
            _ActiveNote = null;
            _ActiveChunk = null;
            _CurrentLineIndex = -1;
            _CurrentNoteIndex = -1;
        }

        public static STrainingSessionData FinalizeSession()
        {
            if (!_IsActive)
            {
                return null;
            }

            FlushActiveChunk();

            var bpm = _Song?.Bpm ?? 200f;
            var msPerBeat = bpm > 0 ? (60000.0 / (bpm * 4.0)) : 0.0;

            var totalHitBeats = 0;
            var totalEvaluatedBeats = 0;

            foreach (var note in _Notes)
            {
                var noteHitBeats = 0;
                var noteTotalBeats = 0;
                var flatBeats = 0;
                var sharpBeats = 0;
                var silentBeats = 0;
                double weightedOffsetSum = 0;
                var voicedBeats = 0;

                foreach (var chunk in note.Chunks)
                {
                    chunk.DurationMs = chunk.DurationBeats * msPerBeat;
                    noteTotalBeats += chunk.DurationBeats;

                    if (chunk.Hit)
                    {
                        noteHitBeats += chunk.DurationBeats;
                    }
                    else
                    {
                        if (chunk.Status == "Flat") flatBeats += chunk.DurationBeats;
                        else if (chunk.Status == "Sharp") sharpBeats += chunk.DurationBeats;
                        else silentBeats += chunk.DurationBeats;
                    }

                    if (chunk.PitchOffset.HasValue)
                    {
                        weightedOffsetSum += chunk.PitchOffset.Value * chunk.DurationBeats;
                        voicedBeats += chunk.DurationBeats;
                    }
                }

                note.HitBeats = noteHitBeats;
                note.HitPercentage = note.DurationBeats > 0
                    ? Math.Round((double)noteHitBeats / note.DurationBeats * 100.0, 1)
                    : 0.0;

                if (note.HitPercentage >= 100.0)
                {
                    note.PrimaryMissDirection = "None";
                }
                else if (flatBeats >= sharpBeats && flatBeats >= silentBeats && flatBeats > 0)
                {
                    note.PrimaryMissDirection = "Flat";
                }
                else if (sharpBeats >= flatBeats && sharpBeats >= silentBeats && sharpBeats > 0)
                {
                    note.PrimaryMissDirection = "Sharp";
                }
                else if (silentBeats > 0)
                {
                    note.PrimaryMissDirection = "Silent";
                }
                else
                {
                    note.PrimaryMissDirection = "None";
                }

                note.AvgPitchOffset = voicedBeats > 0
                    ? Math.Round(weightedOffsetSum / voicedBeats, 2)
                    : (double?)null;

                totalHitBeats += noteHitBeats;
                totalEvaluatedBeats += noteTotalBeats;
            }

            var sessionData = new STrainingSessionData
            {
                Version = 1,
                Song = new STrainingSongInfo
                {
                    Artist = _Song?.Artist ?? "Unknown",
                    Title = _Song?.Title ?? "Unknown",
                    Bpm = bpm,
                    Gap = _Song?.Gap ?? 0f,
                    Voice = _VoiceNr,
                    GameMode = _GameMode.ToString(),
                    AudioMode = _AudioMode.ToString()
                },
                Session = new STrainingSessionMetadata
                {
                    Profile = _ProfileName,
                    Difficulty = _Difficulty.ToString(),
                    ToleranceSemitones = _ToleranceSemitones,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
                    TotalNotes = _Notes.Count,
                    NotesEvaluated = _Notes.Count(n => n.Chunks.Count > 0),
                    HitRatio = totalEvaluatedBeats > 0 ? Math.Round((double)totalHitBeats / totalEvaluatedBeats, 3) : 0.0,
                    Score = (int)Math.Round((_ActivePlayerIndex >= 0 && CGame.Players != null && _ActivePlayerIndex < CGame.Players.Length)
                        ? CGame.Players[_ActivePlayerIndex].Points : 0)
                },
                Notes = _Notes
            };

            CTrainingStorage.SaveRunAsync(sessionData);

            _Notes = new List<STrainingNote>();
            _NotesMap = null;
            _ActiveNote = null;
            _ActiveChunk = null;
            _CurrentLineIndex = -1;
            _CurrentNoteIndex = -1;
            _IsActive = false;
            return sessionData;
        }
    }
}
