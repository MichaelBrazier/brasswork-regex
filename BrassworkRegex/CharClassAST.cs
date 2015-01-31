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
using System.Text;

namespace Brasswork.Regex
{
    internal abstract class CharClassAST : InstructAST { }

    /// <summary>An instruction that matches any codepoint.</summary>
    internal class AnyCharAST : CharClassAST
    {
        private AnyCharAST() : base() { }

        internal static readonly AnyCharAST Instance = new AnyCharAST();
        internal static readonly InstructAST Negate = NotTestAST.Make(Instance);
        internal static readonly RegexAST Starred = StarAST.Make(Instance, true);

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/>is in the class, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint) { return true; }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this class, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other) { return true; }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other) { return false; }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction {
                test = NFA.TestCode.BoolProperty,
                arg1 = (int)Unicode7.Property.AnyChar,
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent)
        {
            sb.Append((parent is NotTestAST) ? "\\P{Any}" : "\\p{Any}");
        }
#endif
    }
}
