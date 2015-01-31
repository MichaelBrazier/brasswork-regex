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
    /// <summary>Syntax tree for a conditional regular expression.</summary>
    internal sealed class CondAST : RegexAST, IEquatable<CondAST>
    {
        /// <summary>Assertion to be tested.</summary>
        internal InstructAST IfPart { get; private set; }

        /// <summary>Tree to match if <see cref="IfPart"/> is true.</summary>
        internal RegexAST ThenPart { get; private set; }

        /// <summary>Tree to match if <see cref="IfPart"/> is false.</summary>
        internal RegexAST ElsePart { get; private set; }

        private CondAST(InstructAST ifPart, RegexAST thenPart, RegexAST elsePart)
            : base() 
        {
            IfPart = ifPart;
            ThenPart = thenPart;
            ElsePart = elsePart;
        }

        /// <summary>Builds a conditional syntax tree.</summary>
        /// <param name="ifPart">Assertion to be tested.</param>
        /// <param name="thenPart">Tree to match if <paramref name="ifPart"/> is true.</param>
        /// <param name="elsePart">Tree to match if <paramref name="ifPart"/> is false.</param>
        /// <returns>The tree for the conditional expression.</returns>
        internal static RegexAST Make(InstructAST ifPart, RegexAST thenPart, RegexAST elsePart)
        {
            if (AnyCharAST.Negate.Equals(thenPart)) return SequenceAST.Make(NotTestAST.Make(ifPart), elsePart);
            if (AnyCharAST.Negate.Equals(elsePart)) return SequenceAST.Make(ifPart, thenPart);

            return new CondAST(ifPart, thenPart, elsePart);
        }

        /// <summary>
        /// Annotates the AST with information on quantifiers and capturing groups.
        /// </summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            IfPart.Annotate(info);
            ThenPart.Annotate(info);
            ElsePart.Annotate(info);
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            CaptureCheckAST ifCapture = IfPart as CaptureCheckAST;
            sb.Append("(?(");
            if (ifCapture != null)
                sb.Append(ifCapture.Name);
            else
                IfPart.Write(sb, this);
            sb.Append(")");
            ThenPart.Write(sb, this);
            sb.Append("|");
            ElsePart.Write(sb, this);
            sb.Append(")");
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars
        {
            get
            {
                return OrTestAST.Make(AndTestAST.Make(IfPart, ThenPart.FirstChars),
                    DiffTestAST.Make(ElsePart.FirstChars, IfPart));
            }
        }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            RegexAST thenPath = SequenceAST.Make(IfPart, ThenPart);
            derivs.AddRange(thenPath.Derivatives);

            RegexAST elsePath = SequenceAST.Make(NotTestAST.Make(IfPart), ElsePart);
            derivs.AddRange(elsePath.Derivatives);
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return ThenPart.IsFinal && ElsePart.IsFinal; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return ThenPart.IsZeroWidth && ElsePart.IsZeroWidth; } }

        #region IEquatable<CondAST> Members

        public bool Equals(CondAST other)
        {
            return other != null && IfPart.Equals(other.IfPart) &&
                ThenPart.Equals(other.ThenPart) && ElsePart.Equals(other.ElsePart);
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as CondAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as CondAST);
        }

        public override int GetHashCode()
        {
            return IfPart.GetHashCode() ^ ThenPart.GetHashCode() ^ ElsePart.GetHashCode();
        }

        #endregion
    }
}
