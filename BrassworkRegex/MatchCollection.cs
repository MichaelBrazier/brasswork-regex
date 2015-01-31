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
using System.Collections.Generic;

namespace Brasswork.Regex
{
    /// <summary>Collection of all occurences of a regex in a string.</summary>
    public sealed class MatchCollection : IEnumerable<Match>
    {
        private NFA searcher;
        private string text;
        private int begin;
        private bool anchored;

        internal MatchCollection(NFA searcher, string text, int startIndex, bool anchored)
        {
            this.searcher = searcher;
            this.text = text;
            this.begin = startIndex;
            this.anchored = anchored;
        }

        /// <summary>Enumerates a <see cref="MatchCollection"/>.</summary>
        public sealed class Enumerator : IEnumerator<Match>
        {
            private MatchCollection coll;
            private Match current;

            /// <summary>Constructor.</summary>
            /// <param name="matches">The <see cref="MatchCollection"/> to enumerate.</param>
            public Enumerator(MatchCollection matches) {
                coll = matches;
                current = null;
            }

            #region IEnumerator<Match> Members

            /// <summary>The current match.</summary>
            Match IEnumerator<Match>.Current { get { return current; } }

            #endregion

            #region IEnumerator Members

            /// <summary>The current match.</summary>
            object System.Collections.IEnumerator.Current { get { return current; } }

            /// <summary>Finds the next match.</summary>
            bool System.Collections.IEnumerator.MoveNext()
            {
                if (current == null)
                    current = coll.searcher.Find(coll.text, coll.begin, coll.anchored);
                else if (current.IsSuccess)
                    current = current.FindNext();

                return current.IsSuccess;
            }

            /// <summary>Resets matching to the start of the string.</summary>
            void System.Collections.IEnumerator.Reset() { current = null; }

            #endregion

            #region IDisposable Members

            /// <summary>Releases resources held by the enumerator.</summary>
            public void Dispose() { }

            #endregion
        }

        #region IEnumerable<Match> Members

        IEnumerator<Match> IEnumerable<Match>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        #endregion
    }
}
