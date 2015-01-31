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
using System.Linq;
using System.Text;

namespace Brasswork.Regex
{
    /// <summary>A syntax leaf that active any codepoint in a range.</summary>
    internal sealed class CharRangeAST : CharClassAST
    {
        internal static readonly Func<int, int, bool, InstructAST> Make;

        static CharRangeAST()
        {
            Make = (minCharacter, maxCharacter, foldCaseCompare) =>
            {
                if (minCharacter > maxCharacter)
                    return NotTestAST.Make(AnyCharAST.Instance);
                else if (minCharacter == maxCharacter)
                    return OneCharAST.Make(minCharacter, foldCaseCompare);
                else
                    return new CharRangeAST(minCharacter, maxCharacter, foldCaseCompare);
            };
            Make = Make.Memoize();
        }

        /// <summary>The smallest codepoint the AST active and consumes.</summary>
        internal int Min { get; private set; }

        /// <summary>The largest codepoint the AST active and consumes.</summary>
        internal int Max { get; private set; }

        /// <summary>Whether the AST folds the case of a codepoint before checking it.</summary>
        internal bool FoldsCase { get; private set; }

        /// <summary>Builds an AST that matches one codepoint from a range.</summary>
        /// <param name="minCharacter">The smallest codepoint the AST matches and consumes.</param>
        /// <param name="maxCharacter">The largest codepoint the AST matches and consumes.</param>
        /// <param name="foldCaseCompare">Whether the AST folds the case of a codepoint before checking it.</param>
        private CharRangeAST(int minCharacter, int maxCharacter, bool foldCaseCompare)
            : base()
        {
            FoldsCase = foldCaseCompare;
            Min = minCharacter;
            Max = maxCharacter;
        }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/>is in the class, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint)
        {
            if (FoldsCase)
            {
                for (int ch = Min; ch <= Max; ch++)
                    if (ch.ToCaseFold() == codepoint.ToCaseFold()) return true;
                return false;
            }
            else
                return codepoint >= Min && codepoint <= Max;
        }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this class, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other)
        {
            OneCharAST oChar = other as OneCharAST;
            if (oChar != null)
                return (this.FoldsCase || !oChar.FoldsCase) && Contains(oChar.Character);

            CharRangeAST oRange = other as CharRangeAST;
            if (oRange != null) return (this.FoldsCase || !oRange.FoldsCase) &&
                this.Min <= oRange.Min && oRange.Max <= this.Max;

            return base.Contains(other);
        }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other)
        {
            if (!FoldsCase)
            {
                CharRangeAST oRange = other as CharRangeAST;
                if (oRange != null && !oRange.FoldsCase)
                    return this.Max < oRange.Min || this.Min > oRange.Max;

                for (int i = Min; i <= Max; i++)
                    if (other.Contains(i)) return false;
                return true;
            }

            return base.DisjointWith(other);
        }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = FoldsCase ? NFA.TestCode.RangeFC : NFA.TestCode.Range,
                arg1 = Min,
                arg2 = Max,
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            if (FoldsCase)
                sb.Append("(?i)");
            string rangeName = String.Format("{0}-{1}", Parser.EscapeChar(Min, true), Parser.EscapeChar(Max, true));
            if (parent is NotTestAST)
                sb.AppendFormat("[^{0}]", rangeName);
            else if (parent is OrTestAST)
                sb.AppendFormat(rangeName);
            else
                sb.AppendFormat("[{0}]", rangeName);
        }
#endif
    }
}
