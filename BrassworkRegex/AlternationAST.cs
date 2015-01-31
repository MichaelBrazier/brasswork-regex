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
    /// <summary>Syntax tree for an alternation of two regular expressions.</summary>
    internal sealed class AlternationAST : RegexAST, IEquatable<AlternationAST>
    {
        /// <summary>The first alternative.</summary>
        internal RegexAST Left { get; private set; }

        /// <summary>The second alternative.</summary>
        internal RegexAST Right { get; private set; }

        private AlternationAST(RegexAST leftAST, RegexAST rightAST)
            : base()
        {
            Left = leftAST;
            Right = rightAST;
        }

        /// <summary>Builds an optimized AST for the alternation of its arguments.</summary>
        /// <param name="leftAST">First alternative.</param>
        /// <param name="rightAST">Second alternative.</param>
        /// <returns>A syntax tree that active either <paramref name="leftAST"/> or <paramref name="rightAST"/>.</returns>
        internal static RegexAST Make(RegexAST leftAST, RegexAST rightAST)
        {
            // a|<fail> == a
            if (AnyCharAST.Negate.Equals(leftAST)) return rightAST;
            if (AnyCharAST.Negate.Equals(rightAST)) return leftAST;

            // a|\p{Any}* == \p{Any}*|a == \p{Any}*
            if (AnyCharAST.Starred.Equals(leftAST) || AnyCharAST.Starred.Equals(rightAST))
                return AnyCharAST.Starred;

            // a|a == a
            if (leftAST.Equals(rightAST)) return leftAST;

            RegexAST factored = leftAST.FactorHeads(rightAST);
            if (factored != null)
                return factored;

            // (a|b)|c == a|(b|c)
            AlternationAST lAlt = leftAST as AlternationAST;
            if (lAlt != null)
                return AlternationAST.Make(lAlt.Left, AlternationAST.Make(lAlt.Right, rightAST));

            // (?~a)|(?~b) == (?~a&b)
            ComplementAST leftComp = leftAST as ComplementAST;
            ComplementAST rightComp = rightAST as ComplementAST;
            if (leftComp != null && rightComp != null)
                return ComplementAST.Make(IntersectionAST.Make(leftComp.Argument, rightComp.Argument));

            return new AlternationAST(leftAST, rightAST);
        }

        /// <summary>Performs left factoring of this syntax tree with another.</summary>
        /// <remarks>
        /// To left factor an alternation, the compiler checks <paramref name="other"/> against the alternatives.
        /// </remarks>
        /// <param name="other">The other syntax tree.</param>
        /// <returns>If this tree and <paramref name="other"/> share a common left factor,
        /// the factored alternation of the two trees; otherwise, null.</returns>
        public override RegexAST FactorHeads(RegexAST other)
        {
            RegexAST factored = Left.FactorHeads(other);
            if (factored != null)
                return AlternationAST.Make(factored, Right);
            else
            {
                factored = other.FactorHeads(Right);
                if (factored != null)
                    return AlternationAST.Make(Left, factored);
                else
                    return null;
            }
        }

        /// <summary>
        /// Annotates the AST with information on quantifiers and capturing groups.
        /// </summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            bool anchored = info.anchor;
            int atomic = info.nAtomics;
            Left.Annotate(info);
            int leftAtoms = info.nAtomics;
            info.nAtomics = atomic;
            Right.Annotate(info);
            if (info.nAtomics < leftAtoms) info.nAtomics = leftAtoms;
            info.anchor = anchored;
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            bool parens = !(parent == null || parent is AlternationAST ||
                parent is CaptureAST || parent is AtomicGroupAST);
            if (parens) sb.Append("(");
            Left.Write(sb, this);
            sb.Append("|");
            Right.Write(sb, this);
            if (parens) sb.Append(")");
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars
        {
            get { return OrTestAST.Make(Left.FirstChars, Right.FirstChars); }
        }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            if (Left == EmptyAST.Instance)
                derivs.Add(new RegexDerivative(EmptyAST.Instance, EmptyAST.Instance));
            else
                derivs.AddRange(Left.Derivatives);
            if (Right == EmptyAST.Instance)
                derivs.Add(new RegexDerivative(EmptyAST.Instance, EmptyAST.Instance));
            else
                derivs.AddRange(Right.Derivatives);
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return Left.IsFinal || Right.IsFinal; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return false; } }

        #region IEquatable<T> Members

        public bool Equals(AlternationAST other)
        {
            return other != null && Left.Equals(other.Left) && Right.Equals(other.Right);
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as AlternationAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as AlternationAST);
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() ^ Right.GetHashCode();
        }

        #endregion
    }
}
