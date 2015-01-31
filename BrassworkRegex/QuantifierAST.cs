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
    /// <summary>Syntax tree for a quantified regular expression.</summary>
    internal sealed class QuantifierAST : RegexAST, IEquatable<QuantifierAST>
    {
        /// <summary>The expression being repeated.</summary>
        internal RegexAST Argument { get; private set; }

        /// <summary>Whether the quantifier is greedy.</summary>
        internal bool IsGreedy { get; private set; }

        /// <summary>Minimum number of times <see cref="Argument"/> matches.</summary>
        internal int MinReps { get; private set; }

        /// <summary>Maximum number of times <see cref="Argument"/> matches.</summary>
        internal int MaxReps { get; private set; }
        
        /// <summary>The quantifier's nesting level.</summary>
        internal int Level { get; private set; }

        private QuantifierAST(RegexAST argument, int minReps, int maxReps, bool isGreedy)
            : base() 
        {
            Argument = argument;
            MinReps = minReps;
            MaxReps = maxReps;
            IsGreedy = isGreedy;
            Level = -1;
        }

        /// <summary>Builds an optimized AST for a general quantified regex.</summary>
        /// <param name="argument">The expression being repeated.</param>
        /// <param name="minReps">Minimum number of times <paramref name="argument"/> repeats. Must be &gt;= 0.</param>
        /// <param name="maxReps">Maximum number of times <paramref name="argument"/> repeats.
        /// If &lt; 0, repetition is unbounded.</param>
        /// <param name="isGreedy">Whether the expression should match as many repetitions
        /// as possible (<c>true</c>) or only as few as needed (<c>false</c>).</param>
        /// <returns>The tree for the quantified expression.</returns>
        /// <exception cref="ArgumentException"><paramref name="maxReps"/> is not negative, and
        /// less than <paramref name="minReps"/>; or <paramref name="minReps"/> is less than 0.</exception>
        internal static RegexAST Make(RegexAST argument, int minReps, int maxReps, bool isGreedy)
        {
            System.Diagnostics.Debug.Assert(minReps >= 0, "cannot repeat fewer than 0 times");
            System.Diagnostics.Debug.Assert(maxReps == -1 || maxReps >= minReps, "max repeats cannot be less than min repeats");

            // <fail>{0,n} == <empty>; <fail>{m > 0,n} == <fail>
            if (AnyCharAST.Negate.Equals(argument))
            {
                if (minReps > 0)
                    return AnyCharAST.Negate;
                else
                    return EmptyAST.Instance;
            }

            if (maxReps == -1)
                return SequenceAST.Make(QuantifierAST.Make(argument, minReps, minReps, isGreedy),
                    StarAST.Make(argument, isGreedy));

            // a{0} == <empty>
            if (maxReps == 0) return EmptyAST.Instance;

            // a{1} == a; <empty>{n,} == <empty>{m,n} == <empty>
            if ((minReps == 1 && maxReps == 1) || argument == EmptyAST.Instance) return argument;

            // a{0,1} == (a|); a{0,1}? == (|a)
            if (minReps == 0 && maxReps == 1)
                return isGreedy ? AlternationAST.Make(argument, EmptyAST.Instance) :
                    AlternationAST.Make(EmptyAST.Instance, argument);

            // when a is zero-width, a{0,n} == <empty>, a{m > 0,n} == a
            if (argument.IsZeroWidth) return (minReps == 0) ? EmptyAST.Instance : argument;

            // a{i}{j} == a{i*j}
            QuantifierAST loopArg = argument as QuantifierAST;
            if (loopArg != null && loopArg.MinReps == loopArg.MaxReps && minReps == maxReps)
            {
                int fixedReps = minReps * loopArg.MinReps;
                return new QuantifierAST(loopArg.Argument, fixedReps, fixedReps, true);
            }

            // when b is a* or a*?, b{n,} == b{n,}? == b{m,n} == b{m,n}? == b
            StarAST starArg = argument as StarAST;
            if (starArg != null) return starArg;

            return new QuantifierAST(argument, minReps, maxReps, isGreedy);
        }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            if (Level < 0) // don't assign a level twice
            {
                bool anchored = info.anchor;
                Level = info.quantLevel;
                info.quantLevel++;
                if (info.quantLevel > info.quantDepth) info.quantDepth++;
                Argument.Annotate(info);
                info.quantLevel--;
                if (MinReps == 0) info.anchor = anchored;
            }
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            Argument.Write(sb, this);
            sb.Append("{");
            if (MaxReps == MinReps)
                sb.AppendFormat("{0:d}", MinReps);
            else
                sb.AppendFormat("{0:d},{1:d}", MinReps, MaxReps);
            sb.Append("}");
            if (!IsGreedy) sb.Append("?");
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars
        {
            get
            {
                if (MinReps == 0)
                    return AnyCharAST.Instance;
                else
                    return Argument.FirstChars;
            }
        }

        /// <summary>A prefix shared by all strings matching the AST.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns>The longest common prefix of strings matching the AST.</returns>
        public override string FixedPrefix(bool foldCase)
        {
            return MinReps == 0 ? "" : Argument.FixedPrefix(foldCase);
        }

        /// <summary>Whether all strings matching the AST have a fixed prefix.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns><c>true</c> if there is a fixed prefix for the AST, otherwise <c>false</c>.</returns>
        public override bool IsFixed(bool foldCase)
        {
            return MinReps != 0 && Argument.IsFixed(foldCase);
        }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            RegexAST loopEnter = SequenceAST.Make(new QuantMaxAST(this), SequenceAST.Make(Argument, this));
            RegexDerivative loopExit = new RegexDerivative(new QuantMinAST(this), EmptyAST.Instance);

            derivs.AddRange(loopEnter.Derivatives);
            if (this.IsGreedy)
                derivs.Add(loopExit);
            else
                derivs.Insert(0, loopExit);

            BackRefAST backArg = Argument as BackRefAST;
            if (backArg != null)
                derivs.Add(new RegexDerivative(new BackEmptyAST(backArg), EmptyAST.Instance));
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return MinReps == 0; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return false; } }

        #region IEquatable<QuantifierAST> Members

        public bool Equals(QuantifierAST other)
        {
            return other != null && Argument.Equals(other.Argument) && (IsGreedy == other.IsGreedy) &&
                MinReps == other.MinReps && MaxReps == other.MaxReps;
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as QuantifierAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as QuantifierAST);
        }

        public override int GetHashCode()
        {
            return Argument.GetHashCode() ^ IsGreedy.GetHashCode() ^
                MinReps.GetHashCode() ^ MaxReps.GetHashCode();
        }

        #endregion
    }

    /// <summary>Syntax tree for a Kleene star.</summary>
    internal sealed class StarAST : RegexAST, IEquatable<StarAST>
    {
        /// <summary>The expression being repeated.</summary>
        internal RegexAST Argument { get; private set; }

        /// <summary>Whether the star is greedy.</summary>
        internal bool IsGreedy { get; private set; }

        private StarAST(RegexAST argument, bool isGreedy) : base() { Argument = argument; IsGreedy = isGreedy; }

        internal static RegexAST Make(RegexAST argument, bool isGreedy)
        {
            // <fail>* == <empty>* == <empty>
            // when a is zero-width, a* == <empty>
            if (argument == EmptyAST.Instance || AnyCharAST.Negate.Equals(argument) || argument.IsZeroWidth)
                return EmptyAST.Instance;

            // a** == a**? == a*; a*?* == a*?*? == a*?
            if (argument is StarAST) return argument;

            return new StarAST(argument, isGreedy);
        }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            bool anchored = info.anchor;
            Argument.Annotate(info);
            info.anchor = anchored;
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            Argument.Write(sb, this);
            sb.Append("*");
            if (!IsGreedy) sb.Append("?");
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars { get { return AnyCharAST.Instance; } }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            RegexAST loopEnter = SequenceAST.Make(Argument, this);
            derivs.AddRange(loopEnter.Derivatives);

            RegexDerivative loopExit = new RegexDerivative(EmptyAST.Instance, EmptyAST.Instance);
            if (this.IsGreedy)
                derivs.Add(loopExit);
            else
                derivs.Insert(0, loopExit);
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return true; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return false; } }

        #region IEquatable<StarAST> Members

        public bool Equals(StarAST other)
        {
            return other != null && Argument.Equals(other.Argument) && (IsGreedy == other.IsGreedy);
        }

        public override bool Equals(RegexAST other)
        {
            return object.ReferenceEquals(this, other) || this.Equals(other as StarAST);
        }

        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj) || this.Equals(obj as StarAST);
        }

        public override int GetHashCode()
        {
            return Argument.GetHashCode() ^ IsGreedy.GetHashCode();
        }

        #endregion
    }

    /// <summary>A syntax leaf that matches a position until a quantifier's argument has been entered
    /// a specified number of times.</summary>
    internal sealed class QuantMaxAST : ZeroWidthInstructAST
    {
        /// <summary>The original quantifier.</summary>
        internal QuantifierAST Source { get; private set; }

        /// <summary>Builds an AST that matches a position until a quantifier's argument has been entered
        /// a specified number of times.</summary>
        /// <param name="source">The original quantifier.</param>
        internal QuantMaxAST(QuantifierAST source) : base() { Source = source; }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return 1; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = NFA.TestCode.QuantMax,
                arg1 = Source.Level,
                arg2 = Source.MaxReps,
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.Append("\\q{");
            if (Source.MaxReps < 0)
                sb.AppendFormat("{0:d}+", Source.Level);
            else
                sb.AppendFormat("{0:d}<{1:d}", Source.Level, Source.MaxReps);
            sb.Append("}");
        }
#endif
    }

    /// <summary>A syntax leaf that matches a position only if a quantifier's argument has been entered
    /// a specified number of times.</summary>
    internal sealed class QuantMinAST : ZeroWidthInstructAST
    {
        /// <summary>The original quantifier.</summary>
        internal QuantifierAST Source { get; private set; }

        /// <summary>Builds a syntax tree that notes the entry of a quantified expression.</summary>
        /// <param name="source">The original quantifier.</param>
        internal QuantMinAST(QuantifierAST source) : base() { Source = source; }

        public override bool ComesFrom(RegexAST ast)
        {
            StarAST starAST = ast as StarAST;
            if (starAST != null)
                return ComesFrom(starAST.Argument);

            QuantifierAST quantAST = ast as QuantifierAST;
            if (quantAST != null)
                return Source.Equals(ast) || ComesFrom(quantAST.Argument);

            return false;
        }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return 1; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = NFA.TestCode.QuantMin,
                arg1 = Source.Level,
                arg2 = Source.MinReps,
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.Append("\\q{");
            sb.AppendFormat("{0:d}>={1:d}", Source.Level, Source.MinReps);
            sb.Append("}");
        }
#endif
    }
}
