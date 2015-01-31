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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Brasswork.Regex
{
    /// <summary>Represents the method that is called each time a regular expression match
    /// is found during a Replace method operation.</summary>
    /// <param name="match">The match to evaluate.</param>
    /// <returns>The result of evaluating <paramref name="match"/>.</returns>
    public delegate string MatchEvaluator(Match match);

    internal delegate List<string> ScanMatch(Match match);

    /// <summary>Represents the result of a regular expression match.</summary>
    public sealed class Match
    {
        /// <summary>Represents the result of a capturing group.</summary>
        public sealed class Group
        {
            private Match parent;
            private int index;
            private string value = null;

            internal Group(Match parent, int index) { this.parent = parent; this.index = index; }

            /// <summary>The group's name.</summary>
            public string Name { get { return parent.nfa.GroupNames[index]; } }

            /// <summary>The first character matched by the group.</summary>
            public int Start { get { return parent.data[index << 1]; } }

            internal int End { get { return parent.data[(index << 1) + 1]; } }

            /// <summary>The number of characters matched by the group.</summary>
            public int Length { get { return End - Start; } }

            /// <summary>The substring captured by the group.</summary>
            public string Value
            {
                get
                {
                    if (value == null && this.Length >= 0)
                        value = parent.Text.Substring(this.Start, this.Length);

                    return value;
                }
            }

            /// <summary>Extracts the substring captured by the group.</summary>
            /// <returns>The captured substring.</returns>
            public override string ToString() { return Value; }
        }

        /// <summary>A read-only view of the capturing groups in a <see cref="Match"/>.</summary>
        public class GroupCollection : IEnumerable<Match.Group>
        {
            Match source;

            internal GroupCollection(Match m) { source = m; }

            /// <summary>Returns the group with the specified index.</summary>
            /// <param name="index">The group's index.</param>
            /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/>
            /// is negative, or greater than the number of groups.</exception>
            public Match.Group this[int index]
            {
                get { return source.groups[index]; }
            }

            /// <summary>Returns the group with the specified name.</summary>
            /// <param name="name">The group's name.</param>
            /// <exception cref="ArgumentOutOfRangeException">No group is named <paramref name="name"/>.</exception>
            public Match.Group this[string name]
            {
                get
                {
                    int index = Array.IndexOf(source.nfa.GroupNames, name);
                    if (index < 0)
                        throw new ArgumentOutOfRangeException("name", name, "Not a group name");
                    return source.groups[index];
                }
            }

            /// <summary>Returns the number of capturing groups in the regex.</summary>
            public int Count { get { return source.groups.Length; } }

            #region IEnumerable<Match.Group> Members

            /// <summary>An enumerator for the capturing groups of a match.</summary>
            public IEnumerator<Match.Group> GetEnumerator()
            {
                return new Enumerator(source);
            }

            #endregion

            #region IEnumerable Members

            /// <summary>An enumerator for the capturing groups of a match.</summary>
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new Enumerator(source);
            }

            #endregion

            /// <summary>An enumerator for the capturing groups of a match.</summary>
            public sealed class Enumerator : IEnumerator<Match.Group>
            {
                Match source;
                int index;

                internal Enumerator(Match m) { source = m; index = -1; }

                #region IEnumerator<Match.Group> Members

                /// <summary>The current group.</summary>
                public Match.Group Current
                {
                    get { return (index >= 0 && index < source.groups.Length) ? source.groups[index] : null; }
                }

                #endregion

                #region IDisposable Members

                /// <summary>Releases resources held by the enumerator.</summary>
                public void Dispose() { }

                #endregion

                #region IEnumerator Members

                /// <summary>The current group.</summary>
                object IEnumerator.Current
                {
                    get { return (index >= 0 && index < source.groups.Length) ? source.groups[index] : null; }
                }

                /// <summary>Moves to the next group.</summary>
                public bool MoveNext()
                {
                    if (index < 0)
                        index = 0;
                    else if (index < source.groups.Length)
                        index++;
                    return (index >= 0 && index < source.groups.Length);
                }

                /// <summary>Returns to before the first group.</summary>
                public void Reset() { index = -1; }

                #endregion
            }
        }

        private readonly NFA nfa;
        private Group[] groups;

        /// <summary>The current NFA state.</summary>
        internal int StateID;

        /// <summary>The match's position in the text.</summary>
        internal int Position;

        /// <summary>While matching a backreference, the position in the text in the capture being referred to.</summary>
        internal int backPosition;

        /// <summary>Positions of capturing and atomic groups that have been processed.</summary>
        internal readonly int[] data;

        /// <summary>Number of capturing groups.</summary>
        internal readonly int nCaptures;

        /// <summary></summary>
        internal readonly int quantStart;

        /// <summary>Returns the input string.</summary>
        public readonly string Text;

        /// <summary>A read-only view of the capturing groups in the match./></summary>
        public readonly GroupCollection Groups;

        /// <summary>Indicates whether the match is anchored to its starting point.</summary>
        public bool Anchored { get; private set; }

        /// <summary>Indicates whether the match is valid.</summary>
        public bool IsSuccess { get { return this.StateID == nfa.FinalState; } }

        /// <summary>The first character matched.</summary>
        public int Start { get { return groups[0].Start; } }

        /// <summary>The number of characters matched.</summary>
        public int Length { get { return groups[0].Length; } }

        /// <summary>The matched string.</summary>
        public string Value { get { return groups[0].Value; } }

        /// <summary>Returns the matched string.</summary>
        public override string ToString() { return Value; }

        /// <summary>Constructs an initial match.</summary>
        /// <param name="nfa">The engine performing the match.</param>
        /// <param name="text">The text to match against.</param>
        /// <param name="anchored">Whether the match is anchored to its starting point.</param>
        internal Match(NFA nfa, string text, bool anchored)
        {
            this.nfa = nfa;
            this.Text = text;
            this.Anchored = anchored;
            this.StateID = -1;
            this.Position = -1;
            this.backPosition = -1;

            this.nCaptures = nfa.GroupNames.Length;
            int nPairs = this.nCaptures + nfa.AtomicCount;
            this.quantStart = nPairs << 1;
            this.data = new int[(nPairs << 1) + nfa.QuantDepth];
            for (int i = 0; i < this.quantStart; i += 2)
            {
                this.data[i] = text.Length + 1;
                this.data[i + 1] = -1;
            }
            this.groups = new Group[this.nCaptures];
            for (int i = 0; i < this.nCaptures; i++)
                this.groups[i] = new Group(this, i);
            this.Groups = new GroupCollection(this);
        }

        /// <summary>Replaces this match's state with another's.</summary>
        /// <param name="other">The match to copy from.</param>
        /// <exception cref="InvalidOperationException"><paramref name="other"/>
        /// is from a different regex, or applies to a different string.</exception>
        internal void Copy(Match other)
        {
            if (other == this) return;

            if (other.nfa != this.nfa)
                throw new ArgumentException("Matches from different regexes are not compatible");

            if (other.Text != this.Text)
                throw new ArgumentException("Matches on different strings are not compatible");

            Buffer.BlockCopy(other.data, 0, this.data, 0, Buffer.ByteLength(this.data));

            this.Anchored = other.Anchored;
            this.StateID = other.StateID;
            this.Position = other.Position;
            this.backPosition = other.backPosition;
        }

        /// <summary>Searches the input string for another occurrence of the
        /// regular expression, starting at the next character after this match.</summary>
        /// <returns>A regular expression <see cref="Match"/> object.</returns>
        /// <exception cref="InvalidOperationException">The match is not valid.</exception>
        public Match FindNext()
        {
            if (!IsSuccess) throw new InvalidOperationException("Failed match");

            int startIndex = data[1];
            if (data[1] == data[0])
            {
                if (Anchored || startIndex < 0 || startIndex >= Text.Length)
                    return new Match(nfa, Text, true);
                else
                    Text.NextCodepoint(ref startIndex);
            }
            return nfa.Find(Text, startIndex, Anchored);
        }

        /// <summary>Called by <see cref="Regex"/>.Split.</summary>
        /// <returns>The captured substrings from this match.</returns>
        /// <exception cref="InvalidOperationException">The match is not valid.</exception>
        internal List<string> SplitResult()
        {
            if (!IsSuccess) throw new InvalidOperationException("Failed match");

            List<string> capvalues = new List<string>(this.groups.Length);
            for (int i = 1; i < this.groups.Length; i++)
                capvalues.Add(this.Groups[i].ToString());
            return capvalues;
        }

        /// <summary>Formats a replacement pattern with data from the match. Called by <see cref="Regex"/>.Replace.</summary>
        /// <param name="pattern">The pattern to be formatted.</param>
        /// <returns>Result of the format.</returns>
        /// <exception cref="InvalidOperationException">The match is not valid.</exception>
        internal string ReplaceResult(string pattern)
        {
            if (!IsSuccess) throw new InvalidOperationException("Failed match");
            StringBuilder sb = new StringBuilder();
            int pos = 0;
            while (pos < pattern.Length)
            {
                if (pattern[pos] != '$' || pos + 1 >= pattern.Length)
                {
                    sb.Append(pattern[pos]);
                    pos++;
                }
                else switch(pattern[pos + 1])
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        {
                            int index = 0;
                            for (pos++; pos < pattern.Length && pattern[pos] >= '0' && pattern[pos] <= '9'; pos++)
                            {
                                index *= 10;
                                index += pattern[pos] - '0';
                            }
                            sb.Append(this.Groups[index].ToString());
                            break;
                        }
                    case '{':
                        {
                            pos++;
                            string groupName = Parser.ReadBracketedName(pattern, ref pos);
                            sb.Append(this.Groups[groupName].ToString());
                            break;
                        }
                    case '_': sb.Append(Text); pos += 2; break;
                    case '&': sb.Append(this.ToString()); pos += 2; break;
                    case '`': sb.Append(Text.Substring(0, data[0])); pos += 2; break;
                    case '\'': sb.Append(Text.Substring(data[1])); pos += 2; break;
                    default: sb.Append(pattern[pos + 1]); pos += 2; break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Determines the relative priorities of two matches.</summary>
        /// <remarks>
        /// <para>Atomic groups are checked in the order their right parentheses appear in the regex.
        /// When both matches have completed a group, and started it from the same position, the
        /// match that ends that group at a later position has higher priority. When a failing
        /// match has not completed a group, any other match that has completed that group has
        /// higher priority.</para>
        /// <para>If every atomic group in the regex has been checked and the priority is still
        /// unknown, the starting and ending positions of the matches determine the priority. A
        /// match that starts at an earlier position has higher priority, and when two matches
        /// start at the same position the match that ends later has higher priority.</para>
        /// </remarks>
        /// <param name="x">The first match.</param>
        /// <param name="y">The second match.</param>
        /// <returns>An int &lt; 0 if <paramref name="x"/> has a lower priority than <paramref name="y"/>;
        /// an int &gt; 0 if <paramref name="x"/> has a higher priority than <paramref name="y"/>;
        /// 0 if <paramref name="x"/> and <paramref name="y"/> have the same priority.</returns>
        internal static int PosixPriority(Match x, Match y)
        {
            if (x.nfa != y.nfa)
                throw new ArgumentException("Matches from different regexes are not comparable");

            if (x.Text != y.Text)
                throw new ArgumentException("Matches on different strings are not comparable");

            // Compare atomic groups
            int diff;
            for (int i = x.nCaptures; i < x.nCaptures + x.nfa.AtomicCount; i++)
            {
                // When a match has not completed a group, any other match that has completed
                // that group has higher priority.
                if (x.data[(i << 1) + 1] < 0 && y.data[(i << 1) + 1] >= 0)
                    return -1;

                if (y.data[(i << 1) + 1] < 0 && x.data[(i << 1) + 1] >= 0)
                    return 1;

                // When matches both complete a group from the same starting position,
                // the match that ends the group last has higher priority
                if (x.data[(i << 1) + 1] >= 0 && y.data[(i << 1) + 1] >= 0 && x.data[i << 1] == y.data[i << 1])
                {
                    diff = x.data[(i << 1) + 1] - y.data[(i << 1) + 1];
                    if (diff != 0) return diff;
                }
            }
            // When matches are complete, the match that starts earlier has higher priority; when 
            // complete matches start from the same position, the match that ends later has higher priority
            if (x.IsSuccess && y.IsSuccess)
            {
                diff = y.data[0] - x.data[0];
                if (diff != 0)
                    return diff;
                else
                    return x.data[1] - y.data[1];
            }
            // Failing matches have lower priority than successful matches
            return x.IsSuccess ? 1 : (y.IsSuccess ? -1 : 0);
        }

        /// <summary>Compares two <see cref="Match"/> objects to detect whether they will continue
        /// to the same final position in a string.</summary>
        internal class SameFinal : EqualityComparer<Match>
        {
            /// <summary>Indicates whether two <see cref="Match"/> objects represent partial matches
            /// that will continue to the same final position.</summary>
            /// <param name="x">The first match.</param>
            /// <param name="y">The second match</param>
            /// <returns><c>true</c> if <paramref name="x"/> and <paramref name="y"/> continue to
            /// the same final position; otherwise <c>false</c>.</returns>
            public override bool Equals(Match x, Match y)
            {
                if (Object.ReferenceEquals(x, y)) return true;

                if (x.nfa == y.nfa && x.Text == y.Text
                    && x.Position == y.Position && x.StateID == y.StateID)
                {
                    for (int i = x.quantStart; i < x.data.Length; i++)
                        if (x.data[i] != y.data[i]) return false;
                    if (x.nfa.Backrefs)
                        for (int i = 2; i < 2 * x.nCaptures; i++)
                            if (x.data[i] != y.data[i]) return false;
                    return true;
                }
                else
                    return false;
            }

            public override int GetHashCode(Match obj)
            {
                return obj.nfa.GetHashCode() ^ obj.Text.GetHashCode() ^ obj.Position.GetHashCode()
                    ^ obj.StateID.GetHashCode() ^ obj.data.GetHashCode();
            }
        }
    }
}
