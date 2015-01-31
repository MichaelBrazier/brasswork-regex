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

namespace Brasswork.Regex
{
    /// <summary>Syntax tree for an atomic group.</summary>
    internal sealed class AtomicGroupAST : RegexAST, IEquatable<AtomicGroupAST>
    {
        /// <summary>The expression to match atomically.</summary>
        internal RegexAST Argument { get; private set; }

        /// <summary>The ID of this atomic group.</summary>
        /// <remarks>Groups are numbered in the order their opening parentheses appear in the regex.</remarks>
        internal int Index { get; private set; }

        private AtomicGroupAST(RegexAST argument)
            : base()
        {
            Argument = argument;
            Index = -1;
        }

        /// <summary>Builds an optimized AST for an atomic group.</summary>
        /// <param name="argument">The expression to match atomically.</param>
        /// <returns>The syntax tree.</returns>
        public static RegexAST Make(RegexAST argument)
        {
            // /(?><fail>)/ == <fail>, /(?><empty>)/ == <empty>, /(?>(?>a))/ == /(?>a)/
            if (argument == EmptyAST.Instance || AnyCharAST.Negate.Equals(argument) || argument is AtomicGroupAST)
                return argument;

            // /(?>(+a))/ == /(+(?>a))/
            if (argument is CaptureAST)
            {
                CaptureAST capArg = argument as CaptureAST;
                return CaptureAST.Make(new AtomicGroupAST(capArg.Argument), capArg.Name, capArg.Position);
            }

            return new AtomicGroupAST(argument);
        }

        /// <summary>The first unit in the AST.</summary>
        public override RegexAST Head
        {
            get { return SequenceAST.Make(new AtomicOpenAST(this), Argument.Head); }
        }

        /// <summary>What remains after the <see cref="Head"/> is removed from the AST.</summary>
        public override RegexAST Tail
        {
            get { return SequenceAST.Make(Argument.Tail, new AtomicCloseAST(this)); }
        }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            if (this.Index < 0)
            {
                this.Index = info.nAtomics;
                Argument.Annotate(info);
                info.nAtomics++;
            }
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.Append("(?>");
            Argument.Write(sb, this);
            sb.Append(")");
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars { get { return Argument.FirstChars; } }

        /// <summary>A prefix shared by all strings matching the AST.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns>The longest common prefix of strings matching the AST.</returns>
        public override string FixedPrefix(bool foldCase) { return Argument.FixedPrefix(foldCase); }

        /// <summary>Whether all strings matching the AST have a fixed prefix.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns><c>true</c> if there is a fixed prefix for the AST, otherwise <c>false</c>.</returns>
        public override bool IsFixed(bool foldCase) { return Argument.IsFixed(foldCase); }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            RegexAST enterGroup = SequenceAST.Make(new AtomicOpenAST(this),
                SequenceAST.Make(Argument, new AtomicCloseAST(this)));
            derivs.AddRange(enterGroup.Derivatives);
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return Argument.IsFinal; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return Argument.IsZeroWidth; } }

        #region IEquatable<AtomicGroupAST> Members

        public bool Equals(AtomicGroupAST other)
        {
            return other != null && Argument.Equals(other.Argument);
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as AtomicGroupAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as AtomicGroupAST);
        }

        public override int GetHashCode()
        {
            return Argument.GetHashCode();
        }

        #endregion
    }

    /// <summary>An AST that, when processed, registers the start of an atomic group.</summary>
    internal sealed class AtomicOpenAST : ZeroWidthInstructAST
    {
        /// <summary>The atomic group to open.</summary>
        internal AtomicGroupAST Source { get; private set; }

        /// <summary>Builds a syntax tree that notes the start of an atomic group.</summary>
        /// <param name="source">The atomic group to open.</param>
        internal AtomicOpenAST(AtomicGroupAST source) : base() { Source = source; }

        /// <summary>Indicates whether the instruction is generated from a regex as part of a derivative.</summary>
        /// <param name="ast">A regex from which the instruction may have been generated.</param>
        /// <returns><c>true</c> if <paramref name="ast"/> is the origin of this instruction, otherwise <c>false</c>.</returns>
        public override bool ComesFrom(RegexAST ast)
        {
            StarAST starAST = ast as StarAST;
            if (starAST != null)
                return ComesFrom(starAST.Argument);

            QuantifierAST quantAST = ast as QuantifierAST;
            if (quantAST != null)
                return ComesFrom(quantAST.Argument);

            return Source.Equals(ast);
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return true; } }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return 2; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction { test = NFA.TestCode.AtomOpen, arg1 = Source.Index, };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.AppendFormat("\\a{0:d}", Source.Index);
            sb.Append("{s}");
        }
#endif
    }

    /// <summary>An AST that, when processed, registers the end of an atomic group.</summary>
    internal sealed class AtomicCloseAST : ZeroWidthInstructAST
    {
        /// <summary>The atomic group to close.</summary>
        internal AtomicGroupAST Source { get; private set; }

        /// <summary>Builds a syntax tree that notes the end of an atomic group.</summary>
        /// <param name="source">The atomic group to close.</param>
        internal AtomicCloseAST(AtomicGroupAST source) : base() { Source = source; }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return true; } }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return 1; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction { test = NFA.TestCode.AtomClose, arg1 = Source.Index, };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.AppendFormat("\\a{0:d}", Source.Index);
            sb.Append("{s}");
        }
#endif
    }
}
