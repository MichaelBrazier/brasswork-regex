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
using System.Text;

namespace Brasswork.Regex
{
    /// <summary>Syntax tree for a capturing group.</summary>
    internal sealed class CaptureAST : RegexAST, IEquatable<CaptureAST>
    {
        /// <summary>The expression whose matching string is captured.</summary>
        internal RegexAST Argument { get; private set; }

        /// <summary>The name of this capturing group.</summary>
        internal string Name { get; private set; }

        /// <summary>The number for this capturing group.</summary>
        /// <remarks>Captures are numbered in the order their opening parentheses appear in the regex.</remarks>
        internal int Index { get; private set; }

        /// <summary>Where the opening parenthesis of this group appeared in the regex. Logged for error reporting.</summary>
        internal int Position { get; private set; }

        private CaptureAST(RegexAST argument, string captureName, int position)
            : base() 
        {
            Argument = argument; Name = captureName; Index = -1; Position = position;
        }

        /// <summary>Builds a syntax tree for an unnamed capturing group.</summary>
        /// <param name="argument">The subexpression to match.</param>
        /// <param name="position">Where the opening parenthesis of this group appeared in the regex.
        /// Logged for error reporting.</param>
        /// <returns>An AST for a capturing group without an explicit name.</returns>
        internal static RegexAST Make(RegexAST argument, int position)
        {
            if (AnyCharAST.Negate.Equals(argument)) return AnyCharAST.Negate;

            return new CaptureAST(argument, null, position);
        }

        /// <summary>Builds a syntax tree for a named capturing group.</summary>
        /// <param name="argument">The subexpression to match.</param>
        /// <param name="captureName">The name of the capturing group.</param>
        /// <param name="position">Where the opening parenthesis of this group appeared in the regex.
        /// Logged for error reporting.</param>
        /// <returns>An AST for a capturing group with an explicit name.</returns>
        internal static RegexAST Make(RegexAST argument, string captureName, int position)
        {
            if (AnyCharAST.Negate.Equals(argument)) return AnyCharAST.Negate;

            return new CaptureAST(argument, captureName, position);
        }

        /// <summary>The first unit in the AST.</summary>
        public override RegexAST Head
        {
            get { return SequenceAST.Make(new CaptureOpenAST(this), Argument.Head); }
        }

        /// <summary>What remains after the <see cref="Head"/> is removed from the AST.</summary>
        public override RegexAST Tail
        {
            get { return SequenceAST.Make(Argument.Tail, new CaptureCloseAST(this)); }
        }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            if (Index < 0) // don't assign a capture ID twice
            {
                if (Name == null)
                {
                    Index = info.captures.Count;
                    Name = Index.ToString();
                    info.captures.Add(Name);
                }
                else if ((Index = info.captures.FindIndex(c => c == Name)) < 0)
                {
                    Index = info.captures.Count;
                    info.captures.Add(Name);
                }
                Argument.Annotate(info);
            }
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent)
        {
            sb.Append("(");
            if (Index < 0)
                sb.Append("+");
            else
            {
                sb.Append("{");
                sb.Append(Name);
                sb.Append("}");
            }
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
            RegexAST enterGroup = SequenceAST.Make(new CaptureOpenAST(this),
                SequenceAST.Make(Argument, new CaptureCloseAST(this)));
            derivs.AddRange(enterGroup.Derivatives);
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return Argument.IsFinal; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return Argument.IsZeroWidth; } }

        #region IEquatable<CaptureAST> Members

        public bool Equals(CaptureAST other)
        {
            return other != null && Argument.Equals(other.Argument);
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as CaptureAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as CaptureAST);
        }

        public override int GetHashCode()
        {
            return Argument.GetHashCode();
        }

        #endregion
    }

    /// <summary>An AST that, when processed, registers the start of a subexpression capture.</summary>
    internal sealed class CaptureOpenAST : ZeroWidthInstructAST
    {
        /// <summary>The capturing group to open.</summary>
        internal CaptureAST Source { get; private set; }

        /// <summary>Builds a syntax tree that notes the start of a capturing group.</summary>
        /// <param name="source">The capturing group to open.</param>
        internal CaptureOpenAST(CaptureAST source) : base() { Source = source; }

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

            return (Source != null) && Source.Equals(ast);
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
            return new NFA.Instruction
            {
                test = NFA.TestCode.CaptOpen,
                arg1 = (Source == null) ? 0 : Source.Index,
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent)
        {
            if (Source == null)
                sb.Append("\\K");
            else
            {
                sb.AppendFormat("\\g{0:d}", Source.Index);
                sb.Append("{s}");
            }
        }
#endif
    }

    /// <summary>An AST that, when processed, registers the end of a subexpression capture.</summary>
    internal sealed class CaptureCloseAST : ZeroWidthInstructAST
    {
        /// <summary>The capturing group to close.</summary>
        internal CaptureAST Source { get; private set; }

        /// <summary>Builds a syntax tree that closes a capturing group.</summary>
        /// <param name="source">The capturing group to close.</param>
        internal CaptureCloseAST(CaptureAST source) : base() { Source = source; }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return true; } }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return 1; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = NFA.TestCode.CaptClose,
                arg1 = (Source == null) ? 0 : Source.Index,
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent)
        {
            if (Source == null)
                sb.Append("\\k");
            else
            {
                sb.AppendFormat("\\g{0:d}", (Source == null) ? 0 : Source.Index);
                sb.Append("{e}");
            }
        }
#endif
    }
}
