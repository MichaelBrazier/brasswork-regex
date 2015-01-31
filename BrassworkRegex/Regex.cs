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
using System.Collections.Generic;
using System.Diagnostics;

namespace Brasswork.Regex
{
    /// <summary>Options to control the behavior of regular expressions.</summary>
    [FlagsAttribute]
    public enum RegexOptions : byte
    {
        /// <summary>The default.</summary>
        None = 0,
        /// <summary>Activates case-insensitive matching. Mode = i</summary>
        IgnoreCase = 1,
        /// <summary>^ and $ match respectively at the start and end of a line. Mode = m</summary>
        Multiline = 2,
        /// <summary>The dot(.) matches newline characters. Mode = s</summary>
        DotAll = 4,
        /// <summary>The regex parser ignores whitespace characters, unless they are escaped. Mode = x</summary>
        FreeSpacing = 8,
        /// <summary>\b matches between a character in \w and one in \W. Mode = b</summary>
        SimpleWordBreak = 16,
        /// <summary>The match of a regex must begin at the initial position. Mode = A</summary>
        Anchored = 32,
    }

    /// <summary>Represents a regular expression.</summary>
    public sealed class Regex
    {
        /// <summary>Decodes character codes for regex options.</summary>
        /// <param name="optionCodes">Character codes for the options desired.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="optionCodes"/>
        /// contains an invalid character code.</exception>
        private static RegexOptions ParseOptions(string optionCodes)
        {
            RegexOptions opts = RegexOptions.None;
            foreach (char c in optionCodes)
            {
                switch (c)
                {
                    case 'b': opts |= RegexOptions.SimpleWordBreak; break;
                    case 'i': opts |= RegexOptions.IgnoreCase; break;
                    case 'm': opts |= RegexOptions.Multiline; break;
                    case 's': opts |= RegexOptions.DotAll; break;
                    case 'x': opts |= RegexOptions.FreeSpacing; break;
                    case 'A': opts |= RegexOptions.Anchored; break;
                    default: throw new ArgumentOutOfRangeException(
                        "optionCodes", optionCodes, "Valid characters are [bimsxA]");
                }
            }
            return opts;
        }

        private string rep;
        private RegexOptions opts;
        private NFA nfa;

        /// <summary>Initializes and compiles a regular expression.</summary>
        /// <param name="rep">The pattern to be compiled.</param>
        /// <exception cref="ArgumentException"><paramref name="rep"/> is an ill-formed pattern.</exception>
        public Regex(string rep) : this(rep, RegexOptions.None) { }

        /// <summary>Initializes and compiles a regular expression, with options that modify the pattern.</summary>
        /// <param name="rep">The pattern to be compiled.</param>
        /// <param name="optionCodes">Character codes for the options desired.</param>
        /// <exception cref="ArgumentException"><paramref name="rep"/> is an ill-formed pattern.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="optionCodes"/>
        /// contains an invalid character code.</exception>
        public Regex(string rep, string optionCodes) : this(rep, ParseOptions(optionCodes)) { }

        /// <summary>Initializes and compiles a regular expression, with options that modify the pattern.</summary>
        /// <param name="rep">The pattern to be compiled.</param>
        /// <param name="opts">The options desired.</param>
        /// <exception cref="ArgumentException"><paramref name="rep"/> is an ill-formed pattern.</exception>
        public Regex(string rep, RegexOptions opts)
        {
            this.rep = rep;
            this.opts = opts;
            NFABuilder builder = new NFABuilder(rep, opts);
            Debug.WriteLine(String.Format("Parsed /{0}/{1}", rep, this.Options));

            builder.Traverse();
            Debug.WriteLine(String.Format("Canonical form = {0}, NFA:", builder.CanonicalForm));

            nfa = new NFA(builder);
#if DEBUG
            Debug.Indent();
            Debug.WriteLine(String.Format("{0:d} non-* quantifiers, {1:d} atomic groups, {2}captures = ({3})",
                nfa.QuantDepth, nfa.AtomicCount, nfa.Anchored ? "anchored, " : "", String.Join(", ", nfa.GroupNames)));
            Debug.WriteLine(String.Format("Fixed prefix of match = \"{0}\", first character in {1}",
                nfa.FixedPrefix == null ? "" : nfa.FixedPrefix.Needle, nfa.InstructionString(nfa.FirstTest)));
            for (int s = 0; s < nfa.States.Length; s++)
            {
                Debug.Write(s);
                if (s == nfa.FinalState)
                    Debug.Write(" accepts");
                foreach (NFA.Transition tr in nfa.States[s])
                    Debug.Write(String.Format(", {0} -> {1:d}", nfa.InstructionString(tr.instruction), tr.target));
                Debug.WriteLine("");
            }
            Debug.Unindent();
#endif
        }

        /// <summary>Returns the pattern from which the regular expression was compiled.</summary>
        /// <returns>The original pattern.</returns>
        public override string ToString()
        {
            return String.Format("/{0}/{1}", rep, this.Options);
        }

        /// <summary>Indicates whether the regular expression was compiled with a given option.</summary>
        /// <param name="opt">The option to check.</param>
        /// <returns><b>true</b> if the regular expression was compiled with <paramref name="opt"/>;
        /// otherwise <b>false</b>.</returns>
        public bool IsOption(RegexOptions opt) { return (this.opts & opt) != 0; }

        /// <summary>
        /// The character codes for the <see cref="RegexOptions"/> the regular expression was compiled with.
        /// </summary>
        public string Options
        {
            get
            {
                string result = "";
                if (IsOption(RegexOptions.SimpleWordBreak)) result += "b";
                if (IsOption(RegexOptions.IgnoreCase)) result += "i";
                if (IsOption(RegexOptions.Multiline)) result += "m";
                if (IsOption(RegexOptions.DotAll)) result += "s";
                if (IsOption(RegexOptions.FreeSpacing)) result += "x";
                if (IsOption(RegexOptions.Anchored)) result += "A";
                return result;
            }
        }

        /// <summary>Names of capturing groups in the regular expression.</summary>
        internal string[] GroupNames { get { return nfa.GroupNames; } }

        /// <summary>Searches a string for an occurrence of the regular expression.</summary>
        /// <param name="text">The string to be searched.</param>
        /// <returns>A regular expression Match object.</returns>
        public Match Match(string text) { return this.Match(text, 0); }

        /// <summary>Searches a string for an occurrence of the regular expression,
        /// starting at a given pos.</summary>
        /// <param name="text">The string to be searched.</param>
        /// <param name="startIndex">The pos in the string to start the search.</param>
        /// <returns>A regular expression Match object.</returns>
        public Match Match(string text, int startIndex)
        {
            Debug.WriteLine(String.Format("Matching /{0}/{1} against \"{2}\" starting at {3:d}",
                rep, this.Options, text, startIndex));
            if (text == null)
                throw new ArgumentNullException("text");
            else if (startIndex < 0 || startIndex > text.Length)
                throw new ArgumentOutOfRangeException("startIndex", startIndex, "");
            else
                return nfa.Find(text, startIndex, IsOption(RegexOptions.Anchored));
        }

        /// <summary>Searches a string for every occurrence of the regular expression.</summary>
        /// <param name="text">The string to be searched.</param>
        /// <returns>An enumeration of Match objects.</returns>
        public MatchCollection Matches(string text) { return this.Matches(text, 0); }

        /// <summary>Searches a string for every occurrence of the regular expression,
        /// starting at a given pos.</summary>
        /// <param name="text">The string to be searched.</param>
        /// <param name="startIndex">The pos in the string to start the search.</param>
        /// <returns>An enumeration of Match objects.</returns>
        public MatchCollection Matches(string text, int startIndex)
        {
            Debug.WriteLine(String.Format("Finding all matches of /{0}/{1} against \"{2}\" starting at {3:d}",
                rep, this.Options, text, startIndex));
            return new MatchCollection(nfa, text, startIndex, IsOption(RegexOptions.Anchored));
        }

        /// <summary>Splits a string into an array of substrings at the occurrences
        /// of the regular expression.</summary>
        /// <param name="text">The string to be split.</param>
        /// <returns>An array of substrings.</returns>
        public string[] Split(string text) { return Split(text, -1, 0); }

        /// <summary>Splits a string into an array of substrings at a given maximum
        /// number of occurrences of the regular expression.</summary>
        /// <param name="text">The string to be split.</param>
        /// <param name="limit">The maximum number of occurrences to split on.</param>
        /// <returns>An array of substrings.</returns>
        public string[] Split(string text, int limit) { return Split(text, limit, 0); }

        /// <summary>Splits a string into an array of substrings at a given maximum
        /// number of occurrences of the regular expression.  The search for
        /// occurrences starts at a given pos.</summary>
        /// <param name="text">The string to be split.</param>
        /// <param name="limit">The maximum number of occurrences to split on.</param>
        /// <param name="startIndex">The pos in the string to start the search.</param>
        /// <returns>An array of substrings.</returns>
        public string[] Split(string text, int limit, int startIndex)
        {
            return Scan(m => m.SplitResult(), text, startIndex, limit - 1).ToArray();
        }

        /// <summary>Copies an input string, replacing occurrences of the regular
        /// expression with a replacement pattern.</summary>
        /// <param name="text">The string to be copied.</param>
        /// <param name="pattern">The replacement pattern.</param>
        /// <returns>A string identical to <paramref name="text"/>, except that each
        /// substring matched has been replaced by <paramref name="pattern"/>.</returns>
        public string Replace(string text, string pattern) { return Replace(text, pattern, -1, 0); }

        /// <summary>Copies an input string, replacing up to a given maximum number of
        /// occurrences of the regular expression with a replacement pattern.</summary>
        /// <param name="text">The string to be copied.</param>
        /// <param name="pattern">The replacement pattern.</param>
        /// <param name="times">The maximum number of occurrences to replace.</param>
        /// <returns>A string identical to <paramref name="text"/>, except that each
        /// substring matched has been replaced by <paramref name="pattern"/>.</returns>
        public string Replace(string text, string pattern, int times) { return Replace(text, pattern, times, 0); }

        /// <summary>Copies an input string, replacing up to a given maximum number of
        /// occurrences of the regular expression with a replacement pattern.  The
        /// search for occurrences starts at a given pos.</summary>
        /// <param name="text">The string to be copied.</param>
        /// <param name="pattern">The replacement pattern.</param>
        /// <param name="times">The maximum number of occurrences to replace.</param>
        /// <param name="startIndex">The pos in the string to start the search.</param>
        /// <returns>A string identical to <paramref name="text"/>, except that each
        /// substring matched has been replaced by <paramref name="pattern"/>.</returns>
        public string Replace(string text, string pattern, int times, int startIndex)
        {
            return Replace(text, m => m.ReplaceResult(pattern), times, startIndex);
        }

        /// <summary>Copies an input string, replacing occurrences of the regular
        /// expression with a string returned by a <see cref="MatchEvaluator"/> delegate.</summary>
        /// <param name="text">The string to be copied.</param>
        /// <param name="eval">A custom method that examines a match and returns
        /// a replacement string.</param>
        /// <returns>A string identical to <paramref name="text"/>, except that each
        /// substring matched has been replaced by the result of <paramref name="eval"/>.</returns>
        public string Replace(string text, MatchEvaluator eval) { return Replace(text, eval, -1, 0); }

        /// <summary>Copies an input string, replacing up to a given maximum number of
        /// occurrences of the regular expression with a string returned by a
        /// <see cref="MatchEvaluator"/> delegate.</summary>
        /// <param name="text">The string to be copied.</param>
        /// <param name="eval">A custom method that examines a match and returns
        /// a replacement string.</param>
        /// <param name="times">The maximum number of occurrences to replace.</param>
        /// <returns>A string identical to <paramref name="text"/>, except that each
        /// substring matched has been replaced by the result of <paramref name="eval"/>.</returns>
        public string Replace(string text, MatchEvaluator eval, int times)
        {
            return Replace(text, eval, times, 0);
        }

        /// <summary>Copies an input string, replacing up to a given maximum number of
        /// occurrences of the regular expression with a string returned by a
        /// <see cref="MatchEvaluator"/> delegate.  The search for occurrences starts
        /// at a given pos.</summary>
        /// <param name="text">The string to be copied.</param>
        /// <param name="eval">A custom method that examines a match and returns
        /// a replacement string.</param>
        /// <param name="times">The maximum number of occurrences to replace.</param>
        /// <param name="startIndex">The position in <paramref name="text"/> to start the search.</param>
        /// <returns>A string identical to <paramref name="text"/>, except that each
        /// substring matched has been replaced by the result of <paramref name="eval"/>.</returns>
        public string Replace(string text, MatchEvaluator eval, int times, int startIndex)
        {
            return String.Join("", Scan(m => EvalToScan(eval, m), text, startIndex, times).ToArray());
        }

        internal static List<string> EvalToScan(MatchEvaluator eval, Match m)
        {
            List<string> result = new List<string>();
            result.Add(eval(m));
            return result;
        }

        /// <summary>Factors out the common part of Split and Replace.</summary>
        /// <param name="eval">A custom method that examines a match and returns a list of strings.</param>
        /// <param name="text">The codepoints to be scanned.</param>
        /// <param name="limit">The maximum number of match occurrences to replace.</param>
        /// <param name="startIndex">The position in <paramref name="text"/> to start the search.</param>
        /// <returns>A list of substrings of <paramref name="text"/>, in which the substrings lying between
        /// matches of this regex are separated by the results of <paramref name="eval"/> on each match.</returns>
        private List<string> Scan(ScanMatch eval, string text, int startIndex, int limit)
        {
            List<string> parts = new List<string>();
            int index = 0;
            foreach (Match m in this.Matches(text, startIndex))
            {
                parts.Add(text.Substring(index, m.Groups[0].Start - index));
                parts.AddRange(eval(m));
                index = m.Groups[0].End;
                if (limit > 0) limit--;
                if (limit == 0) break;
            }
            parts.Add(text.Substring(index));
            return parts;
        }
    }
}
