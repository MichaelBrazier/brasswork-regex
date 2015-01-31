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
    /// <summary>Syntax tree for the complement of a regular expression.</summary>
    internal sealed class ComplementAST : RegexAST, IEquatable<ComplementAST>
    {
        /// <summary>The expression to be complemented.</summary>
        internal RegexAST Argument { get; private set; }

        private ComplementAST(RegexAST argument) : base() { Argument = argument; }

        internal static RegexAST Make(RegexAST argument)
        {
            // ~<fail> == \p{Any}*
            if (AnyCharAST.Negate.Equals(argument))
                return AnyCharAST.Starred;
            if (AnyCharAST.Starred.Equals(argument))
                return AnyCharAST.Negate;

            // ~<empty> == \p{Any}+
            if (argument == EmptyAST.Instance)
                return QuantifierAST.Make(AnyCharAST.Instance, 1, -1, true);

            // ~~a == a
            ComplementAST compArg = argument as ComplementAST;
            if (compArg != null)
                return compArg.Argument;

            return new ComplementAST(argument);
        }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            Argument.Annotate(info);
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent)
        {
            if (parent is IntersectionAST)
                Argument.Write(sb, this);
            else
            {
                sb.Append("(?~");
                Argument.Write(sb, this);
                sb.Append(")");
            }
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars { get { return AnyCharAST.Instance; } }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            derivs.Add(new RegexDerivative(AnyCharAST.Instance, AnyCharAST.Starred));
            foreach (RegexDerivative argDeriv in Argument.Derivatives)
            {
                if (argDeriv.Accept != EmptyAST.Instance)
                {
                    List<RegexDerivative> intersects = new List<RegexDerivative>();
                    foreach (RegexDerivative d in derivs)
                    {
                        InstructAST accept = AndTestAST.Build(d.Accept, argDeriv.Accept);
                        RegexAST target = IntersectionAST.Make(ComplementAST.Make(argDeriv.Target), d.Target);
                        if (!(AnyCharAST.Negate.Equals(accept) || AnyCharAST.Negate.Equals(target)))
                            intersects.Add(new RegexDerivative(accept, target));

                        accept = AndTestAST.Build(d.Accept, NotTestAST.Make(argDeriv.Accept));
                        if (!(AnyCharAST.Negate.Equals(accept)))
                            intersects.Add(new RegexDerivative(accept, d.Target));
                    }
                    derivs = intersects;
                }
            }

            if (IsFinal) derivs.Add(new RegexDerivative(EmptyAST.Instance, EmptyAST.Instance));
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return !Argument.IsFinal; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return false; } }

        #region IEquatable<ComplementAST> Members

        public bool Equals(ComplementAST other)
        {
            return other != null && Argument.Equals(other.Argument);
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as ComplementAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as ComplementAST);
        }

        public override int GetHashCode()
        {
            return ~Argument.GetHashCode();
        }

        #endregion
    }
}
