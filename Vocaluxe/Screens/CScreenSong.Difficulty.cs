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
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.Screens
{
    public partial class CScreenSong
    {
        private bool _DifficultyHoverActive;
        private SColorF _DefaultHelpBarColor = new SColorF(0.96f, 0.96f, 0.98f, 1f);

        private string _FormatDifficultyBreakdown(CSong song)
        {
            if (song == null || song.Difficulty.Overall < 1.0f)
                return String.Empty;

            var d = song.Difficulty;
            string agilityStr;
            string rangeStr;
            string paceStr = CLanguage.Translate("TR_DIFFICULTY_PACE") + ": " + d.Pace.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ★";

            if (d.PitchedRatio < 0.20f)
            {
                rangeStr = CLanguage.Translate("TR_DIFFICULTY_RANGE") + ": —";
                agilityStr = CLanguage.Translate("TR_DIFFICULTY_AGILITY") + ": — (" + CLanguage.Translate("TR_DIFFICULTY_RAP_FOOTER") + ")";
            }
            else
            {
                string minNote = SDifficultyMetrics.FormatNoteName(d.P5Tone);
                string maxNote = SDifficultyMetrics.FormatNoteName(d.P95Tone);
                rangeStr = CLanguage.Translate("TR_DIFFICULTY_RANGE") + ": " + d.Range.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ★ (" + minNote + "–" + maxNote + ", " + d.ToneSpan + " st)";
                agilityStr = CLanguage.Translate("TR_DIFFICULTY_AGILITY") + ": " + d.Agility.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ★";
            }

            string tierKey = SDifficultyMetrics.GetTierNameKey(d.Overall);
            string tierStr = CLanguage.Translate(tierKey);
            string overallStr = CLanguage.Translate("TR_CONFIG_DIFFICULTY") + ": " + tierStr + " (" + d.Overall.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ★)";

            string breakdown = agilityStr + "  •  " + rangeStr + "  •  " + paceStr + "  •  " + overallStr;

            if ((_Sso.Sorting.DuetOptions == EDuetOptions.Duets || song.IsDuet) && song.Notes != null && song.Notes.VoiceCount >= 2)
            {
                CVoice v0 = song.Notes.GetVoice(0);
                CVoice v1 = song.Notes.GetVoice(1);
                if (v0 != null && v1 != null && v0.Difficulty.Overall >= 1.0f && v1.Difficulty.Overall >= 1.0f)
                {
                    try
                    {
                        breakdown += "  •  " + String.Format(System.Globalization.CultureInfo.InvariantCulture, CLanguage.Translate("TR_DIFFICULTY_DUET_VOICES"), v0.Difficulty.Overall, v1.Difficulty.Overall);
                    }
                    catch (FormatException)
                    {
                        breakdown += "  •  P1: " + v0.Difficulty.Overall.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ★ • P2: " + v1.Difficulty.Overall.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " ★";
                    }
                }
            }

            return breakdown;
        }

        private void _UpdateDifficultyHover(bool isOverDifficulty)
        {
            if (_Texts == null || !_Texts.ContainsKey(_TextHelpBar) || _Texts[_TextHelpBar] == null)
                return;

            if (isOverDifficulty && !_Sso.Selection.PartyMode && !_SearchActive)
            {
                int previewNr = _SongMenu.GetPreviewSongNr();
                if (previewNr >= 0 && previewNr < CSongs.VisibleSongs.Count)
                {
                    CSong song = CSongs.VisibleSongs[previewNr];
                    if (song != null && song.Difficulty.Overall >= 1.0f)
                    {
                        if (_SongMenu != null)
                            _SongMenu.SetDifficultySelected(true);
                        _Texts[_TextHelpBar].Text = _FormatDifficultyBreakdown(song);
                        _Texts[_TextHelpBar].Color = SDifficultyMetrics.GetTierColor(song.Difficulty.Overall);
                        _DifficultyHoverActive = true;
                        return;
                    }
                }
            }

            if (_DifficultyHoverActive)
            {
                if (_SongMenu != null)
                    _SongMenu.SetDifficultySelected(false);
                _Texts[_TextHelpBar].Text = CLanguage.Translate("TR_SCREENSONG_HELPMESSAGE");
                _Texts[_TextHelpBar].Color = _DefaultHelpBarColor;
                _DifficultyHoverActive = false;
            }
        }

        /// <summary>
        ///     If sorting by difficulty ascending, snaps the cursor to the first ranked song (> 1.0f).
        ///     Unranked placeholder songs (1.0f) remain before it in the list and can still be accessed by scrolling up.
        /// </summary>
        private void _SelectFirstRankedSongIfDifficulty()
        {
            if (!CSongs.IsInCategory || CSongs.VisibleSongs == null || CSongs.VisibleSongs.Count == 0 || _SongMenu == null)
                return;

            ESongSorting sorting = CSongs.Sorter.SongSorting;
            bool isDifficultySort = sorting == ESongSorting.TR_CONFIG_DIFFICULTY ||
                                    sorting == ESongSorting.TR_CONFIG_DIFFICULTY_AGILITY ||
                                    sorting == ESongSorting.TR_CONFIG_DIFFICULTY_RANGE ||
                                    sorting == ESongSorting.TR_CONFIG_DIFFICULTY_PACE;

            if (!isDifficultySort || CSongs.Sorter.SortDescending)
                return;

            for (int i = 0; i < CSongs.VisibleSongs.Count; i++)
            {
                CSong song = CSongs.VisibleSongs[i];
                if (song != null && song.Difficulty.IsRankedForSort(sorting))
                {
                    _SongMenu.SetSelectedSong(i);
                    break;
                }
            }
        }
    }
}
