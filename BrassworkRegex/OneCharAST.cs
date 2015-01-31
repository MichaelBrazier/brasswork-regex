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
    /// <summary>A syntax leaf that matches exactly one codepoint.</summary>
    internal sealed class OneCharAST : CharClassAST
    {
        internal static readonly Func<int, bool, OneCharAST> Make;

        static OneCharAST()
        {
            Make = (character, foldCaseCompare) => new OneCharAST(character, foldCaseCompare);
            Make = Make.Memoize();
        }

        /// <summary>The codepoint the AST active and consumes.</summary>
        internal int Character { get; private set; }

        /// <summary>Whether the AST folds the case of a codepoint before checking it.</summary>
        internal bool FoldsCase { get; private set; }

        /// <summary>Builds an AST that active one codepoint.</summary>
        /// <param name="character">The codepoint the AST active and consumes.</param>
        /// <param name="foldCaseCompare">Whether the AST folds the case of a codepoint before checking it.</param>
        private OneCharAST(int character, bool foldCaseCompare)
            : base() 
        {
            FoldsCase = foldCaseCompare;
            Character = foldCaseCompare ? character.ToCaseFold() : character;
        }

        /// <summary>A prefix shared by all strings matching the AST.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns>The longest common prefix of strings matching the AST.</returns>
        public override string FixedPrefix(bool foldCase)
        {
            return (foldCase == this.FoldsCase) ? Character.ToUTF16String() : "";
        }

        /// <summary>Whether all strings matching the AST have a fixed prefix.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns><c>true</c> if there is a fixed prefix for the AST, otherwise <c>false</c>.</returns>
        public override bool IsFixed(bool foldCase) { return foldCase == this.FoldsCase; }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/>is in the class, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint)
        {
            if (FoldsCase)
                return codepoint.ToCaseFold() == Character;
            else
                return codepoint == Character;
        }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this class, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other)
        {
            OneCharAST oChar = other as OneCharAST;
            if (oChar != null)
                return (FoldsCase || !oChar.FoldsCase) && Contains(oChar.Character);

            return base.Contains(other);
        }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other)
        {
            if (!FoldsCase && !other.IsZeroWidth)
                return !other.Contains(Character);

            OneCharAST oChar = other as OneCharAST;
            if (oChar != null)
                return !(Contains(oChar.Character) || oChar.Contains(Character));

            return base.DisjointWith(other);
        }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = FoldsCase ? NFA.TestCode.CharacterFC : NFA.TestCode.Character,
                arg1 = Character,
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
            if (parent is NotTestAST)
                sb.AppendFormat("[^{0}]", Parser.EscapeChar(Character, true));
            else if (parent is OrTestAST)
                sb.Append(Parser.EscapeChar(Character, true));
            else
                sb.Append(Parser.EscapeChar(Character, false));
        }
#endif
    }
}
