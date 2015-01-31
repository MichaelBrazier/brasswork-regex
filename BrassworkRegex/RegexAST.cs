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
    /// <summary>Base class of regular expression syntax trees.</summary>
    internal abstract class RegexAST : IEquatable<RegexAST>
    {
        private List<RegexDerivative> derivatives;

        internal protected RegexAST() { derivatives = null; }

        /// <summary>Performs left factoring of this syntax tree with another.</summary>
        /// <remarks>
        /// If two regular expressions <i>a</i> and <i>b</i> have the same <see cref="Head"/>,
        /// <c><i>a</i>|<i>b</i></c> and <c><i>a.Head</i>(<i>a.Tail</i>|<i>b.Tail</i>)</c>
        /// are equivalent; and the second expression matches more efficiently than the
        /// first.  The compiler therefore transforms the first expression into the
        /// second whenever it can, by calling <see cref="FactorHeads"/>.
        /// </remarks>
        /// <param name="other">The other syntax tree.</param>
        /// <returns>If this tree and <paramref name="other"/> share a common left factor,
        /// the factored alternation of the two trees; otherwise, null.</returns>
        public virtual RegexAST FactorHeads(RegexAST other)
        {
            if (other is AlternationAST)
                return other.FactorHeads(this);
            else if (Head.Equals(other.Head))
                return SequenceAST.Make(Head, AlternationAST.Make(Tail, other.Tail));
            else
                return null;
        }

        /// <summary>The first unit in the AST.</summary>
        public virtual RegexAST Head { get { return this; } }

        /// <summary>What remains after the <see cref="Head"/> is removed from the AST.</summary>
        public virtual RegexAST Tail { get { return EmptyAST.Instance; } }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public abstract void Annotate(AnnotationVisitor info);

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public abstract void Write(StringBuilder sb, RegexAST parent);

        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            Write(sb, null);
            return sb.ToString();
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public abstract InstructAST FirstChars { get; }

        /// <summary>A prefix shared by all strings matching the AST.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns>The longest common prefix of strings matching the AST.</returns>
        public virtual string FixedPrefix(bool foldCase) { return ""; }

        /// <summary>Whether all strings matching the AST have a fixed prefix.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns><c>true</c> if there is a fixed prefix for the AST, otherwise <c>false</c>.</returns>
        public virtual bool IsFixed(bool foldCase) { return false; }

        /// <summary>The non-failing partial derivatives of this AST, together with the tests leading to them.</summary>
        public List<RegexDerivative> Derivatives
        {
            get
            {
                if (derivatives == null)
                    derivatives = FindDerivatives();
                return derivatives;
            }
        }

        /// <summary>Calculates <see cref="Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal abstract List<RegexDerivative> FindDerivatives();

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public abstract bool IsFinal { get; }

        /// <summary>Indicates whether this AST matches a position without consuming a codepoint.</summary>
        public abstract bool IsZeroWidth { get; }

        public virtual bool Equals(RegexAST other)
        {
            return Object.ReferenceEquals(this, other);
        }
    }

    /// <summary>Data for an NFA transition.</summary>
    internal class RegexDerivative
    {
        /// <summary>An expression matching at most one character.</summary>
        public InstructAST Accept;

        /// <summary>The part of the regex left to be matched after matching <see cref="Accept"/>.</summary>
        public RegexAST Target;

        /// <summary>Constructor.</summary>
        /// <param name="accept">An expression matching at most one character.</param>
        /// <param name="target">The part of the regex left to be matched after matching <paramref name="accept"/>.</param>
        public RegexDerivative(InstructAST accept, RegexAST target)
        {
            this.Accept = accept;
            this.Target = target;
        }

        /// <summary>Appends another syntax tree to the target of this derivative; then, if <see cref="Accept"/> 
        /// is zero-width, extracts the derivatives of the resulting sequence and combines their <see cref="Accept"/>
        /// with this derivative's <see cref="Accept"/>.</summary>
        /// <param name="next">The AST to append to <see cref="Target"/>.</param>
        /// <returns>If <see cref="Accept"/> is zero-width and <paramref name="next"/> is not empty, the derivatives 
        /// of <paramref name="next"/> with <see cref="Accept"/> prepended to their <see cref="Accept"/>s; otherwise,
        /// the single derivative {<see cref="Accept"/>, <see cref="Target"/> . <paramref name="next"/>}.</returns>
        public List<RegexDerivative> SequenceWith(RegexAST next)
        {
            List<RegexDerivative> targDerivs = new List<RegexDerivative>();
            if (this.Accept.IsZeroWidth && this.Target == EmptyAST.Instance && !(next == EmptyAST.Instance))
            {
                // If any derivative of next has this.Accept as its Accept, sequencing next with
                // this would start an infinite loop. Reject such derivatives.
                if (!this.Accept.ComesFrom(next))
                    foreach (RegexDerivative d in next.Derivatives)
                    {
                        if (this.Accept == EmptyAST.Instance)
                            targDerivs.Add(d);
                        else
                        {
                            RegexDerivative newDeriv;
                            newDeriv = new RegexDerivative(d.Accept == EmptyAST.Instance ? this.Accept :
                                AndTestAST.Build(this.Accept, d.Accept), d.Target);
                            targDerivs.Add(newDeriv);
                        }
                    }
            }
            else
                targDerivs.Add(new RegexDerivative(this.Accept, SequenceAST.Make(this.Target, next)));

            return targDerivs;
        }
    }
}
