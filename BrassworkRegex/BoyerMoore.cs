/*
	Copyright 2010, 2015 Michael D. Brazier

	This file is part of Brasswork Regex.

	Brasswork Regex is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	Brasswork Regex is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with Brasswork Regex.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;

namespace Brasswork.Regex
{
    /// <summary>An assignable three-level table holding a map of Unicode characters to other values.</summary>
    /// <typeparam name="TValue">The type of values for the property encoded in this table.</typeparam>
    internal class CodepointMap<TValue>
    {
        public const byte PAGE_BITS = 7;
        const int PAGE_SIZE = 1 << PAGE_BITS;

        byte[] top;
        byte[][] middle;
        TValue[][] bottom;
        byte midCount;
        byte botCount;
        TValue initial;
        TValue[] ascii;

        public CodepointMap(TValue init)
        {
            initial = init;
            top = new byte[PAGE_SIZE];
            for (int i = 0; i < PAGE_SIZE; i++)
                top[i] = byte.MaxValue;
            middle = new byte[4][];
            midCount = 0;
            bottom = new TValue[4][];
            botCount = 0;
            ascii = new TValue[PAGE_SIZE];
            for (int i = 0; i < PAGE_SIZE; i++)
                ascii[i] = init;
        }

        public TValue this[int codepoint]
        {
            get
            {
                if (codepoint < PAGE_SIZE) return ascii[codepoint];
                int page = codepoint >> PAGE_BITS;

                int i = page >> PAGE_BITS;
                byte midPage = top[i];
                if (midPage > midCount) return initial;

                int j = page & (PAGE_SIZE - 1);
                byte botPage = middle[midPage][j];
                if (botPage > botCount) return initial;

                int k = codepoint & (PAGE_SIZE - 1);
                return bottom[botPage][k];
            }
            set
            {
                if (codepoint < PAGE_SIZE)
                    ascii[codepoint] = value;
                else
                {
                    int page = codepoint >> PAGE_BITS;
                    int i = page >> PAGE_BITS;
                    if (top[i] > midCount)
                    {
                        if (i >= middle.Length)
                            Array.Resize(ref middle, middle.Length << 1);
                        middle[midCount] = new byte[PAGE_SIZE];
                        for (int n = 0; n < PAGE_SIZE; n++)
                            middle[midCount][n] = byte.MaxValue;
                        top[i] = midCount;
                        midCount++;
                    }

                    int j = page & (PAGE_SIZE - 1);
                    if (middle[top[i]][j] > botCount)
                    {
                        if (j >= bottom.Length)
                            Array.Resize(ref bottom, bottom.Length << 1);
                        bottom[botCount] = new TValue[PAGE_SIZE];
                        for (int n = 0; n < PAGE_SIZE; n++)
                            bottom[botCount][n] = initial;
                        middle[top[i]][j] = botCount;
                        botCount++;
                    }

                    int k = codepoint & (PAGE_SIZE - 1);
                    bottom[middle[top[i]][j]][k] = value;
                }
            }
        }
    }

    /// <summary>Implements the Boyer-Moore string search algorithm.</summary>
    /// <remarks>The algorithm for computing the shift tables is adapted from
    /// "On the shift-table in Boyer-Moore’s String Matching Algorithm" by Yang Wang
    /// (International Journal of Digital Content Technology and its Applications,
    /// Volume 3, Number 4, December 2009)</remarks>
    public class BoyerMooreScanner
    {
        int[] pattern;
        int[] suffixShifts;
        CodepointMap<int> charShifts;
        int patLast;

        /// <summary>Builds a scanner for a search string.</summary>
        /// <param name="needle">The string to search for.</param>
        public BoyerMooreScanner(string needle) : this(needle, false) { }

        /// <summary>Builds a scanner for a search string.</summary>
        /// <param name="needle">The string to search for.</param>
        /// <param name="foldCase"><c>true</c> for a case-insensitive search, <c>false</c>
        /// (the default) for a case-sensitive search.</param>
        public BoyerMooreScanner(string needle, bool foldCase)
        {
            Needle = needle;
            FoldsCase = foldCase;
            Position = -1;
            int patLen = 0;
            for (int i = 0; i < needle.Length; needle.NextCodepoint(ref i)) patLen++;
            patLast = needle.Length;
            needle.PrevCodepoint(ref patLast);

            suffixShifts = new int[patLen];
            charShifts = new CodepointMap<int>(patLen);
            pattern = new int[patLen];
            for (int i = 0, p = 0; i < needle.Length; needle.NextCodepoint(ref i), p++)
            {
                pattern[p] = needle.ToCodepoint(i);
                if (FoldsCase)
                    pattern[p] = pattern[p].ToCaseFold();
            }
            CalculateShifts();
        }

        /// <summary>The start of the last successful scan. Equals -1 before a scan,
        /// and the length of the scanned text after a failure.</summary>
        public int Position { get; private set; }

        /// <summary>The string the scanner searches for.</summary>
        public readonly string Needle;

        /// <summary><c>true</c> if the scanner performs a case-insensitive search.</summary>
        readonly bool FoldsCase;

        /// <summary>Builds the shift tables for the scanner.</summary>
        void CalculateShifts()
        {
            int patLen = pattern.Length;

            // Bad character shifts
            for (int index = 0; index < patLen; index++)
                charShifts[pattern[index]] = patLen - 1 - index;

            // Auxiliary table of suffix lengths
            int[] suffixLengths = new int[patLen];
            suffixLengths[patLen - 1] = patLen;
            int i = patLen - 2;
            while (i >= 0)
            {
                while (i >= 0 && pattern[i] != pattern[patLen - 1])
                {
                    suffixLengths[i] = 0;
                    i--;
                }
                if (i < 0) break;

                int delta = patLen - 1 - i;
                int j = i - 1;
                while (j >= 0 && pattern[j] == pattern[j + delta]) j--;
                while (i > j)
                {
                    suffixLengths[i] = suffixLengths[i + delta] == 0 ? 0 : i - j;
                    i--;
                }
            }

            // Good suffix shifts
            for (i = 0; i < patLen && suffixLengths[patLen - 1 - i] != 0; i++) { }
            suffixShifts[patLen - 1] = i;

            for (int index = patLen - 1; index > 0; index--)
                suffixShifts[index - 1] = 2 * patLen - index;
            int prefix = patLen - 1;
            for (int index = patLen - 1; index > 0; index--)
            {
                if (suffixLengths[index-1] == index)
                {
                    for (int j = patLen - prefix; j < patLen - index; j++)
                        suffixShifts[j] -= index;
                    prefix = index;
                }
            }

            for (int index = 1; index < patLen; index++)
            {
                int delta = suffixLengths[index - 1];
                if (delta != 0 && delta != index)
                    suffixShifts[patLen - 1 - delta] = patLen - index + delta;
            }
        }

        /// <summary>Searches a string for the first occurrence of the pattern between two positions.</summary>
        /// <param name="haystack">The string to be searched.</param>
        /// <param name="start">The position to start searching from.</param>
        /// <param name="end">The position to stop searching, if the pattern hasn't been found.</param>
        /// <returns>The position of the pattern's first occurrence between <paramref name="start"/> and
        /// <paramref name="end"/> in <paramref name="haystack"/>, or the end of <paramref name="haystack"/>
        /// if the pattern isn't there.</returns>
        public bool Scan(string haystack, int start, int end)
        {
            int patPos = pattern.Length - 1;
            int readPos = start + patLast;
            int prevPos = start - 1;
            while (readPos < end)
            {
                int patChar = pattern[patPos];
                int readChar = haystack[readPos];
                if ((readChar & Unicode7.SURROGATE_MASK) == Unicode7.HI_SURR_MIN
                    && (haystack[readPos + 1] & Unicode7.SURROGATE_MASK) == Unicode7.LO_SURR_MIN)
                    readChar = ((readChar - Unicode7.HI_SURR_OFFSET) << 10)
                        + haystack[readPos + 1] - Unicode7.LO_SURR_MIN;
                if (FoldsCase)
                    readChar = readChar.ToCaseFold();

                if (patChar == readChar)
                {
                    patPos--;
                    readPos--;
                    if (readPos > start
                        && (haystack[readPos] & Unicode7.SURROGATE_MASK) == Unicode7.LO_SURR_MIN
                        && (haystack[readPos - 1] & Unicode7.SURROGATE_MASK) == Unicode7.HI_SURR_MIN)
                        readPos--;
                    if (patPos < 0 || readPos == prevPos)
                        break;
                }
                else
                {
                    int shift = charShifts[readChar];
                    if (shift < suffixShifts[patPos]) shift = suffixShifts[patPos];
                    if (shift > pattern.Length) prevPos = readPos;
                    for (; shift > 0; shift--)
                    {
                        if (readPos + 1 < end
                            && (haystack[readPos] & Unicode7.SURROGATE_MASK) == Unicode7.HI_SURR_MIN
                            && (haystack[readPos + 1] & Unicode7.SURROGATE_MASK) == Unicode7.LO_SURR_MIN)
                            readPos++;
                        readPos++;
                    }
                    patPos = pattern.Length - 1;
                }
            }
            if (readPos >= 0 && readPos + 1 < end
                && (haystack[readPos] & Unicode7.SURROGATE_MASK) == Unicode7.HI_SURR_MIN
                && (haystack[readPos + 1] & Unicode7.SURROGATE_MASK) == Unicode7.LO_SURR_MIN)
                readPos++;
            if (readPos < end)
                readPos++;
            else
                readPos = end;
            Position = readPos;
            return readPos < end;
        }

        /// <summary>Searches a string for the first occurrence of the pattern after a position.</summary>
        /// <param name="haystack">The string to be searched.</param>
        /// <param name="start">The position to start searching from.</param>
        /// <returns>The position of the pattern's first occurrence after <paramref name="start"/> in
        /// <paramref name="haystack"/>, or the end of <paramref name="haystack"/> if the pattern isn't there.</returns>
        public bool Scan(string haystack, int start) { return Scan(haystack, start, haystack.Length); }

        /// <summary>Searches a string for the first occurrence of the pattern.</summary>
        /// <param name="haystack">The string to be searched.</param>
        /// <returns>The position of the pattern's first occurrence in <paramref name="haystack"/>,
        /// or the end of <paramref name="haystack"/> if the pattern isn't there.</returns>
        public bool Scan(string haystack) { return Scan(haystack, 0, haystack.Length); }
    }
}
