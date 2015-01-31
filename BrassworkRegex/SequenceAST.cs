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
    /// <summary>Syntax tree for a sequence of two regular expressions.</summary>
    internal sealed class SequenceAST : RegexAST, IEquatable<SequenceAST>
    {
        /// <summary>The first regex to match.</summary>
        public RegexAST Left { get; private set; }

        /// <summary>The second regex to match.</summary>
        public RegexAST Right { get; private set; }

        private SequenceAST(RegexAST leftAST, RegexAST rightAST)
            : base()
        {
            Left = leftAST;
            Right = rightAST;
        }

        /// <summary>Builds an optimized AST for the sequence of its arguments.</summary>
        /// <param name="leftAST">The first regex to match.</param>
        /// <param name="rightAST">The second regex to match.</param>
        /// <returns>A syntax tree that active <paramref name="leftAST"/> followed by <paramref name="rightAST"/>.</returns>
        public static RegexAST Make(RegexAST leftAST, RegexAST rightAST)
        {
            // <fail>a == a<fail> == <fail>
            if (AnyCharAST.Negate.Equals(leftAST) || AnyCharAST.Negate.Equals(rightAST))
                return AnyCharAST.Negate;

            // empty regex is identity for sequence
            if (leftAST == EmptyAST.Instance) return rightAST;
            if (rightAST == EmptyAST.Instance) return leftAST;

            // sequence of zero-width regexes == intersection of the regexes
            if (leftAST.IsZeroWidth && rightAST.IsZeroWidth)
                return AndTestAST.Make(leftAST as InstructAST, rightAST as InstructAST);

            // (ab)c == a(bc)
            SequenceAST lSeq = leftAST as SequenceAST;
            if (lSeq != null)
                return SequenceAST.Make(lSeq.Left, SequenceAST.Make(lSeq.Right, rightAST));

            return new SequenceAST(leftAST, rightAST);
        }

        /// <summary>The first unit in the AST.</summary>
        public override RegexAST Head { get { return Left; } }

        /// <summary>What remains after the <see cref="Head"/> is removed from the AST.</summary>
        public override RegexAST Tail { get { return Right; } }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            Left.Annotate(info);
            Right.Annotate(info);
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent)
        {
            bool parens = parent is QuantifierAST || parent is StarAST;
            if (parens) sb.Append("(");
            Left.Write(sb, this);
            Right.Write(sb, this);
            if (parens) sb.Append(")");
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars
        {
            get {
                if (Left.IsFinal)
                    return OrTestAST.Make(Left.FirstChars, Right.FirstChars);
                else if (Left.IsZeroWidth)
                    return AndTestAST.Make(Left.FirstChars, Right.FirstChars);
                else if (Left is BackRefAST)
                    return Right.FirstChars;
                else
                    return Left.FirstChars;
            }
        }

        /// <summary>A prefix shared by all strings matching the AST.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns>The longest common prefix of strings matching the AST.</returns>
        public override string FixedPrefix(bool foldCase)
        {
            return Left.IsFixed(foldCase) ?
                Left.FixedPrefix(foldCase) + Right.FixedPrefix(foldCase) : Left.FixedPrefix(foldCase);
        }

        /// <summary>Whether all strings matching the AST have a fixed prefix.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns><c>true</c> if there is a fixed prefix for the AST, otherwise <c>false</c>.</returns>
        public override bool IsFixed(bool foldCase)
        {
            return Left.IsFixed(foldCase) && Right.IsFixed(foldCase);
        }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            foreach (RegexDerivative ld in Left.Derivatives)
                derivs.AddRange(ld.SequenceWith(Right));
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return Left.IsFinal && Right.IsFinal; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return Left.IsZeroWidth && Right.IsZeroWidth; } }

        #region IEquatable<T> Members

        public bool Equals(SequenceAST other)
        {
            return other != null && Left.Equals(other.Left) && Right.Equals(other.Right);
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as SequenceAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as SequenceAST);
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() ^ Right.GetHashCode();
        }

        #endregion
    }
}
