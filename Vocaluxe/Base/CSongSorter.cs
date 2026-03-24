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
using System.Diagnostics;
using System.Linq;
using VocaluxeLib;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    class CSongSorter : CObservable
    {
        private CSongPointer[] _SortedSongs = new CSongPointer[0];
        private EOffOn _IgnoreArticles = CConfig.Config.Game.IgnoreArticles;
        private ESongSorting _SongSorting = CConfig.Config.Game.SongSorting;
        private bool _ReverseSorting = CConfig.Config.Game.SongSortingReverse;

        public CSongSorter()
        {
            CSongs.Filter.ObjectChanged += _HandleFilteredSongsChanged;
        }

        public CSongPointer[] SortedSongs
        {
            get
            {
                _SortSongs();
                return _SortedSongs;
            }
        }

        public EOffOn IgnoreArticles
        {
            get { return _IgnoreArticles; }
            set
            {
                if (value == _IgnoreArticles)
                    return;
                _IgnoreArticles = value;
                _SetChanged();
            }
        }

        public ESongSorting SongSorting
        {
            get { return _SongSorting; }
            set
            {
                if (value == _SongSorting)
                    return;
                _SongSorting = value;
                _SetChanged();
            }
        }

        public bool ReverseSorting
        {
            get { return _ReverseSorting; }
            set
            {
                if (value == _ReverseSorting)
                    return;
                _ReverseSorting = value;
                _SetChanged();
            }
        }

        public bool SortDescending
        {
            get { return _IsSortDescending(_SongSorting, _ReverseSorting); }
        }

        public void SetOptions(ESongSorting songSorting, EOffOn ignoreArticles)
        {
            if (songSorting != _SongSorting || ignoreArticles != _IgnoreArticles)
            {
                _SongSorting = songSorting;
                _IgnoreArticles = ignoreArticles;
                _SetChanged();
            }
        }

        private void _HandleFilteredSongsChanged(object sender, EventArgs args)
        {
            _SetChanged();
        }

        private int _SortByFieldArtistTitle(CSongPointer s1, CSongPointer s2)
        {
            int res = String.Compare(s1.SortString, s2.SortString, StringComparison.CurrentCultureIgnoreCase);
            if (res == 0)
            {
                if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                {
                    res = String.Compare(CSongs.Songs[s1.SongID].ArtistSorting, CSongs.Songs[s2.SongID].ArtistSorting, StringComparison.CurrentCultureIgnoreCase);
                    return res != 0 ? res : String.Compare(CSongs.Songs[s1.SongID].TitleSorting, CSongs.Songs[s2.SongID].TitleSorting, StringComparison.CurrentCultureIgnoreCase);
                }
                res = String.Compare(CSongs.Songs[s1.SongID].Artist, CSongs.Songs[s2.SongID].Artist, StringComparison.CurrentCultureIgnoreCase);
                return res != 0 ? res : String.Compare(CSongs.Songs[s1.SongID].Title, CSongs.Songs[s2.SongID].Title, StringComparison.CurrentCultureIgnoreCase);
            }
            return res;
        }

        private int _SortByFieldTitle(CSongPointer s1, CSongPointer s2)
        {
            int res = String.Compare(s1.SortString, s2.SortString, StringComparison.CurrentCultureIgnoreCase);
            if (res == 0)
            {
                return _IgnoreArticles == EOffOn.TR_CONFIG_ON
                           ? String.Compare(CSongs.Songs[s1.SongID].TitleSorting, CSongs.Songs[s2.SongID].TitleSorting, StringComparison.CurrentCultureIgnoreCase) :
                           String.Compare(CSongs.Songs[s1.SongID].Title, CSongs.Songs[s2.SongID].Title, StringComparison.CurrentCultureIgnoreCase);
            }
            return res;
        }

        /// <summary>
        /// Compares two songs by means of: first letter of sorting field, sorting field, title.
        /// </summary>
        private int _SortByLetterFieldTitle(CSongPointer s1, CSongPointer s2)
        {
            int res = String.Compare(s1.SortString[0].ToString(), s2.SortString[0].ToString(), StringComparison.CurrentCultureIgnoreCase);
            return res != 0 ? res : _SortByFieldTitle(s1, s2);
        }

        /// <summary>
        /// Compares two songs by means of: first letter of sorting field, sorting field, artist, title.
        /// </summary>
        private int _SortByLetterFieldArtistTitle(CSongPointer s1, CSongPointer s2)
        {
            int res = String.Compare(s1.SortString[0].ToString(), s2.SortString[0].ToString(), StringComparison.CurrentCultureIgnoreCase);
            return res != 0 ? res : _SortByFieldArtistTitle(s1, s2);
        }

        private int _SortByNumPlayed(CSongPointer s1, CSongPointer s2)
        {

            int res = Convert.ToInt32(s1.SortString).CompareTo(Convert.ToInt32(s2.SortString));
            if (res == 0)
            {
                if (_IgnoreArticles == EOffOn.TR_CONFIG_ON)
                {
                    res = String.Compare(CSongs.Songs[s2.SongID].ArtistSorting, CSongs.Songs[s1.SongID].ArtistSorting, StringComparison.CurrentCultureIgnoreCase);
                    return res != 0 ? res : String.Compare(CSongs.Songs[s2.SongID].TitleSorting, CSongs.Songs[s1.SongID].TitleSorting, StringComparison.CurrentCultureIgnoreCase);
                }
                res = String.Compare(CSongs.Songs[s2.SongID].Artist, CSongs.Songs[s1.SongID].Artist, StringComparison.CurrentCultureIgnoreCase);
                return res != 0 ? res : String.Compare(CSongs.Songs[s2.SongID].Title, CSongs.Songs[s1.SongID].Title, StringComparison.CurrentCultureIgnoreCase);
            }
            return res;
        }

        private static bool _HasDefaultDescendingSort(ESongSorting sorting)
        {
            return sorting == ESongSorting.TR_CONFIG_DATEADDED ||
                   sorting == ESongSorting.TR_CONFIG_NUMPLAYED ||
                   sorting == ESongSorting.TR_CONFIG_LASTPLAYED ||
                   sorting == ESongSorting.TR_CONFIG_HIGHSCORE;
        }

        private static bool _IsSortDescending(ESongSorting sorting, bool reverseSorting)
        {
            return _HasDefaultDescendingSort(sorting) ^ reverseSorting;
        }

        private void _AddSongToList(CSong song, List<CSongPointer> list)
        {
            string value = null;
            List<string> values = null;
            switch (_SongSorting)
            {
                case ESongSorting.TR_CONFIG_NONE:
                    value = "";
                    break;
                case ESongSorting.TR_CONFIG_FOLDER:
                    value = song.FolderName;
                    break;
                case ESongSorting.TR_CONFIG_ARTIST:
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                    value = _IgnoreArticles == EOffOn.TR_CONFIG_ON ? song.ArtistSorting : song.Artist;
                    break;
                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                    value = _IgnoreArticles == EOffOn.TR_CONFIG_ON ? song.TitleSorting : song.Title;
                    break;
                case ESongSorting.TR_CONFIG_EDITION:
                    values = song.Editions;
                    break;
                case ESongSorting.TR_CONFIG_GENRE:
                    values = song.Genres;
                    break;
                case ESongSorting.TR_CONFIG_TAGS:
                    values = song.Tags;
                    break;
                case ESongSorting.TR_CONFIG_LANGUAGE:
                    values = song.Languages;
                    break;
                case ESongSorting.TR_CONFIG_DECADE:
                case ESongSorting.TR_CONFIG_YEAR:
                    value = song.Year;
                    break;
                case ESongSorting.TR_CONFIG_DATEADDED:
                    value = song.DateAdded.ToString("yyyyMMdd");
                    break;
                case ESongSorting.TR_CONFIG_NUMPLAYED:
                    value = song.NumPlayed.ToString();
                    break;
                case ESongSorting.TR_CONFIG_CREATOR:
                    value = song.Creator;
                    break;
                case ESongSorting.TR_CONFIG_ENCODING:
                    value = song.Encoding.ToString();
                    break;
                case ESongSorting.TR_CONFIG_LASTPLAYED:
                    value = song.LastPlayed.ToString("yyyyMMddHHmmssfff");
                    break;
                case ESongSorting.TR_CONFIG_HIGHSCORE:
                    value = song.HighScore.ToString();
                    break;
                default:
                    Debug.Assert(false, "Forgot sorting option");
                    break;
            }
            Debug.Assert(value != null || values != null, "Sorting implementation faulty");
            if (value != null)
                list.Add(new CSongPointer(song.ID, value));
            else
            {
                if (values.Count == 0)
                    list.Add(new CSongPointer(song.ID, ""));
                else
                    list.AddRange(values.Select(val => new CSongPointer(song.ID, val)));
            }
        }

        private void _SortSongs()
        {
            if (!_Changed)
                return;

            List<CSongPointer> sortList = new List<CSongPointer>();
            foreach (CSong song in CSongs.Filter.FilteredSongs)
                _AddSongToList(song, sortList);
            switch (_SongSorting)
            {
                case ESongSorting.TR_CONFIG_ARTIST:
                case ESongSorting.TR_CONFIG_NONE:
                    sortList.Sort(_SortByFieldTitle);
                    break;
                case ESongSorting.TR_CONFIG_ARTIST_LETTER:
                    sortList.Sort(_SortByLetterFieldTitle);
                    break;
                case ESongSorting.TR_CONFIG_TITLE_LETTER:
                    sortList.Sort(_SortByLetterFieldArtistTitle);
                    break;
                case ESongSorting.TR_CONFIG_NUMPLAYED:
                    sortList.Sort(_SortByNumPlayed);
                    break;
                default:
                    sortList.Sort(_SortByFieldArtistTitle);
                    break;
            }

            if (_IsSortDescending(_SongSorting, _ReverseSorting))
                sortList.Reverse();

            _SortedSongs = sortList.ToArray();
            _Changed = false;
        }
    }
}
