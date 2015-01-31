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
    /// <summary>Syntax tree for an intersection of two regular expressions.</summary>
    internal sealed class IntersectionAST : RegexAST, IEquatable<IntersectionAST>
    {
        /// <summary>The first alternative.</summary>
        internal RegexAST Left { get; private set; }

        /// <summary>The second alternative.</summary>
        internal RegexAST Right { get; private set; }

        private IntersectionAST(RegexAST leftAST, RegexAST rightAST)
            : base()
        {
            Left = leftAST;
            Right = rightAST;
        }

        internal static RegexAST Make(RegexAST leftAST, RegexAST rightAST)
        {
            // a&<fail> == <fail>
            if (AnyCharAST.Negate.Equals(leftAST) || AnyCharAST.Negate.Equals(rightAST))
                return AnyCharAST.Negate;

            // a&\p{Any}* == \p{Any}*&a == a
            if (AnyCharAST.Starred.Equals(leftAST)) return rightAST;
            if (AnyCharAST.Starred.Equals(rightAST)) return leftAST;

            // a&a == a
            if (leftAST.Equals(rightAST)) return leftAST;

            if (leftAST == EmptyAST.Instance)
            {
                if (rightAST.IsZeroWidth)
                    return rightAST;
                else if (rightAST.IsFinal)
                    return EmptyAST.Instance;
                else
                    return AnyCharAST.Negate;
            }

            if (rightAST == EmptyAST.Instance)
            {
                if (leftAST.IsZeroWidth)
                    return leftAST;
                else if (leftAST.IsFinal)
                    return EmptyAST.Instance;
                else
                    return AnyCharAST.Negate;
            }

            // (a|b)&c) == a&c|b&c
            AlternationAST leftAlt = leftAST as AlternationAST;
            if (leftAlt != null)
                return AlternationAST.Make(IntersectionAST.Make(leftAlt.Left, rightAST),
                    IntersectionAST.Make(leftAlt.Right, rightAST));

            // a&(b|c) == a&b|a&c
            AlternationAST rightAlt = rightAST as AlternationAST;
            if (rightAlt != null)
                return AlternationAST.Make(IntersectionAST.Make(leftAST, rightAlt.Left),
                    IntersectionAST.Make(leftAST, rightAlt.Right));

            // (?~a)&(?~b) == (?~a|b)
            ComplementAST leftComp = leftAST as ComplementAST;
            ComplementAST rightComp = rightAST as ComplementAST;
            if (leftComp != null && rightComp != null)
                return ComplementAST.Make(AlternationAST.Make(leftComp.Argument, rightComp.Argument));

            // When x and y are character classes, (xa)&(yb) == (x&&y)(a&b)
            CharClassAST leftHead = leftAST.Head as CharClassAST;
            CharClassAST rightHead = rightAST.Head as CharClassAST;
            if (leftHead != null && rightHead != null)
                return SequenceAST.Make(AndTestAST.Make(leftHead, rightHead),
                    IntersectionAST.Make(leftAST.Tail, rightAST.Tail));

            return new IntersectionAST(leftAST, rightAST);
        }

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
            bool parens = !(parent == null || parent is IntersectionAST ||
                parent is AlternationAST || parent is CaptureAST || parent is AtomicGroupAST);
            if (parens) sb.Append("(");
            if (Left is ComplementAST)
            {
                Right.Write(sb, this);
                sb.Append("~");
                Left.Write(sb, this);
            }
            else if (Right is ComplementAST)
            {
                Left.Write(sb, this);
                sb.Append("~");
                Right.Write(sb, this);
            }
            else
            {
                Left.Write(sb, this);
                sb.Append("&");
                Right.Write(sb, this);
            }
            if (parens) sb.Append(")");
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars
        {
            get { return AndTestAST.Make(Left.FirstChars, Right.FirstChars); }
        }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            foreach (RegexDerivative ld in Left.Derivatives)
            {
                foreach (RegexDerivative rd in Right.Derivatives)
                {
                    InstructAST accept = AndTestAST.Build(ld.Accept, rd.Accept);
                    RegexAST target = IntersectionAST.Make(ld.Target, rd.Target);
                    if (!(AnyCharAST.Negate.Equals(accept) || AnyCharAST.Negate.Equals(target)))
                        derivs.Add(new RegexDerivative(accept, target));
                }
            }
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return Left.IsFinal && Right.IsFinal; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return Left.IsZeroWidth && Right.IsZeroWidth; } }

        #region IEquatable<IntersectionAST> Members

        public bool Equals(IntersectionAST other)
        {
            return other != null && Left.Equals(other.Left) && Right.Equals(other.Right);
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as IntersectionAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as IntersectionAST);
        }

        public override int GetHashCode()
        {
            return Left.GetHashCode() ^ Right.GetHashCode();
        }

        #endregion
    }
}
