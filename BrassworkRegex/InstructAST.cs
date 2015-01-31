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
    /// <summary>Base class for a syntax tree of a regex that is a transition step.</summary>
    internal abstract class InstructAST : RegexAST
    {
        /// <summary>Indicates whether this instruction matches a codepoint.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if this matches <paramref name="codepoint"/>, otherwise <c>false</c>.</returns>
        public abstract bool Contains(int codepoint);

        /// <summary>Indicates whether all positions matched by another instruction
        /// are also matched by this instruction.</summary>
        /// <param name="other">The other instruction.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this instruction,
        /// otherwise <c>false.</c></returns>
        public virtual bool Contains(InstructAST other)
        {
            OneCharAST oChar = other as OneCharAST;
            if (oChar != null && !oChar.FoldsCase)
                return Contains(oChar.Character);

            AndTestAST oAnd = other as AndTestAST;
            if (oAnd != null)
                return Contains(oAnd.Left) || Contains(oAnd.Right);

            DiffTestAST oDiff = other as DiffTestAST;
            if (oDiff != null)
                return Contains(oDiff.Left);

            return this.Equals(other);
        }

        /// <summary>Indicates whether this and another instruction ever match at the same positions.</summary>
        /// <param name="other">The other instruction.</param>
        /// <returns><c>true</c> if this instruction and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public virtual bool DisjointWith(InstructAST other)
        {
            OneCharAST oChar = other as OneCharAST;
            if (oChar != null && !oChar.FoldsCase)
                return !Contains(oChar.Character);

            NotTestAST oNot = other as NotTestAST;
            if (oNot != null)
                return oNot.Argument.Contains(this);

            AndTestAST oAnd = other as AndTestAST;
            if (oAnd != null)
                return DisjointWith(oAnd.Left) || DisjointWith(oAnd.Right);

            DiffTestAST oDiff = other as DiffTestAST;
            if (oDiff != null)
                return DisjointWith(oDiff.Left);

            return false;
        }

        /// <summary>Indicates whether the instruction is generated from a regex as part of a derivative.</summary>
        /// <param name="ast">A regex from which the instruction may have been generated.</param>
        /// <returns><c>true</c> if <paramref name="ast"/> is the origin of this instruction, otherwise <c>false</c>.</returns>
        public virtual bool ComesFrom(RegexAST ast) { return false; }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return false; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return false; } }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public virtual int SideEffects { get { return 0; } }

        /// <summary>Maximum number of saved partial results while evaluating this instruction.</summary>
        public virtual int TestDepth { get { return 0; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public abstract NFA.Instruction ToInstruction(NFABuilder builder);

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info) { }

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars { get { return this; } }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>A <see cref="RegexDerivative"/> that accepts this instruction and leads to <see cref="EmptyAST"/>.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            derivs.Add(new RegexDerivative(this, EmptyAST.Instance));
            return derivs;
        }
    }

    /// <summary>The negation of an <see cref="InstructAST"/>.</summary>
    internal class NotTestAST : InstructAST
    {
        internal static readonly Func<InstructAST, InstructAST> Make;

        static NotTestAST()
        {
            Make = argument => Build(argument);
            Make = Make.Memoize();
        }

        internal InstructAST Argument { get; private set; }

        private NotTestAST(InstructAST argument) : base() { Argument = argument; }

        internal static InstructAST Build(InstructAST argument)
        {
            // !!a == a
            NotTestAST negArg = argument as NotTestAST;
            if (negArg != null) return negArg.Argument;

            // !<empty> == <fail>
            //if (argument == EmptyAST.Instance) return AnyCharAST.Negate;

            return new NotTestAST(argument);
        }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>false</c> if <paramref name="codepoint"/> belongs to <see cref="Argument"/>,
        /// otherwise <c>true</c>.</returns>
        public override bool Contains(int codepoint) { return !Argument.Contains(codepoint); }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this class, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other) { return Argument.DisjointWith(other); }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other) { return Argument.Contains(other); }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return Argument.IsZeroWidth; } }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return Argument.SideEffects; } }

        /// <summary>Maximum number of saved partial results while evaluating this instruction.</summary>
        public override int TestDepth { get { return Argument.TestDepth + 1; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = NFA.TestCode.Not,
                arg1 = builder.AddInstructions(Argument),
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent)
        {
            Argument.Write(sb, this);
        }
#endif
    }
}
