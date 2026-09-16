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

using System.IO;
using System.Text;

namespace VocaluxeLib
{
    public static class CEncoding
    {
        public static Encoding GetEncoding(this string encodingName)
        {
            switch (encodingName)
            {
                case "AUTO":
                    return Encoding.Default;
                case "CP1250":
                    return Encoding.GetEncoding(1250);
                case "CP1252":
                    return Encoding.GetEncoding(1252);
                case "LOCALE":
                    return Encoding.Default;
                case "UTF8":
                    return Encoding.UTF8;
                default:
                    return Encoding.Default;
            }
        }

        public static string GetEncodingName(this Encoding enc)
        {
            var result = "UTF8";

            if (enc.CodePage == 1250)
            {
                result = "CP1250";
            }

            if (enc.CodePage == 1252)
            {
                result = "CP1252";
            }

            return result;
        }

        /// <summary>
        /// Analyzes a byte buffer to check if it contains valid UTF-8 sequences,
        /// including 1-byte ASCII, 2-byte, 3-byte, and 4-byte supplementary plane characters (emojis, special symbols).
        /// </summary>
        public static bool IsValidUtf8(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return true;
            }

            int i = 0;
            while (i < bytes.Length)
            {
                byte b = bytes[i];
                if (b <= 0x7F)
                {
                    i++;
                    continue;
                }
                else if (b >= 0xC2 && b <= 0xDF)
                {
                    if (i + 1 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF)
                    {
                        return false;
                    }
                    i += 2;
                }
                else if (b >= 0xE0 && b <= 0xEF)
                {
                    if (i + 2 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF || bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF)
                    {
                        return false;
                    }
                    i += 3;
                }
                else if (b >= 0xF0 && b <= 0xF4)
                {
                    // 4-byte sequence (Unicode supplementary planes: emojis, special symbols)
                    if (i + 3 >= bytes.Length || bytes[i + 1] < 0x80 || bytes[i + 1] > 0xBF || bytes[i + 2] < 0x80 || bytes[i + 2] > 0xBF || bytes[i + 3] < 0x80 || bytes[i + 3] > 0xBF)
                    {
                        return false;
                    }
                    i += 4;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Detects the encoding of a text file. If the file bytes form valid UTF-8 (including emojis),
        /// UTF-8 is returned; otherwise, fallback (Encoding.Default) is returned.
        /// </summary>
        public static Encoding DetectFileEncoding(string filePath, Encoding fallback = null)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return fallback ?? Encoding.Default;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                return IsValidUtf8(bytes) ? Encoding.UTF8 : (fallback ?? Encoding.Default);
            }
            catch
            {
                return fallback ?? Encoding.Default;
            }
        }
    }
}