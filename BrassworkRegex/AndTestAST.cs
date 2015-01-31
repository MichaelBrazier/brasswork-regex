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
using System.Text;

namespace Brasswork.Regex
{
    /// <summary>The intersection of two <see cref="InstructAST"/>s.</summary>
    internal class AndTestAST : InstructAST
    {
        internal static readonly Func<InstructAST, InstructAST, InstructAST> Make;

        static AndTestAST()
        {
            Make = (left, right) => Build(left, right);
            Make = Make.Memoize();
        }

        /// <summary>The first requirement.</summary>
        internal InstructAST Left { get; private set; }

        /// <summary>The second requirement.</summary>
        internal InstructAST Right { get; private set; }

        internal AndTestAST(InstructAST left, InstructAST right) : base() { Left = left; Right = right; }

        internal static InstructAST Build(InstructAST left, InstructAST right)
        {
            // a && a == a
            if (left.Equals(right)) return left;

            // <empty> && a == a && <empty> == a if zero-width, <fail> otherwise
            if (left == EmptyAST.Instance)
                return right.IsZeroWidth ? right : AnyCharAST.Negate;
            if (right == EmptyAST.Instance)
                return left.IsZeroWidth ? left : AnyCharAST.Negate;

            // When a contains b, a && b == b
            if (left.Contains(right)) return right;

            // When b contains a, a && b == a
            if (right.Contains(left)) return left;

            // When a and b are disjoint, a && b matches nowhere
            if (left.DisjointWith(right)) return AnyCharAST.Negate;

            // !a && !b == !(a || b), !a && b == b -- a, a && !b == a -- b
            NotTestAST negLeft = left as NotTestAST;
            NotTestAST negRight = right as NotTestAST;
            if (negLeft != null)
            {
                if (negRight != null)
                    return NotTestAST.Make(OrTestAST.Make(negLeft.Argument, negRight.Argument));
                else
                    return DiffTestAST.Make(right, negLeft.Argument);
            }
            else if (negRight != null)
                return DiffTestAST.Make(left, negRight.Argument);

            // (a && b) && c == a && (b && c)
            AndTestAST andRight = right as AndTestAST;
            if (andRight != null)
                return AndTestAST.Make(AndTestAST.Make(left, andRight.Left), andRight.Right);

            // (a || b) && c == (a && c) || (b && c)
            OrTestAST orLeft = left as OrTestAST;
            if (orLeft != null)
                return OrTestAST.Make(AndTestAST.Make(orLeft.Left, right), AndTestAST.Make(orLeft.Right, right));

            // a && (b || c) == (a && b) || (a && c)
            if (!left.IsZeroWidth)
            {
                OrTestAST orRight = right as OrTestAST;
                if (orRight != null)
                    return OrTestAST.Make(AndTestAST.Make(left, orRight.Left), AndTestAST.Make(left, orRight.Right));
            }

            // Intersection of overlapping ranges is the overlap
            CharRangeAST leftRange = left as CharRangeAST;
            CharRangeAST rightRange = right as CharRangeAST;
            if (leftRange != null && rightRange != null && (leftRange.FoldsCase == rightRange.FoldsCase))
            {
                int low = Math.Max(leftRange.Min, rightRange.Min);
                int high = Math.Min(leftRange.Max, rightRange.Max);
                return CharRangeAST.Make(low, high, leftRange.FoldsCase);
            }

            return new AndTestAST(left, right);
        }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/> belongs to <see cref="Left"/>
        /// and <see cref="Right"/>, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint)
        {
            return (Left.IsZeroWidth || Left.Contains(codepoint)) &&
                (Right.IsZeroWidth || Right.Contains(codepoint));
        }

        /// <summary>Indicates whether all positions matched by another instruction
        /// are also matched by this instruction.</summary>
        /// <param name="other">The other instruction.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this instruction,
        /// otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other)
        {
            return Left.Contains(other) && Right.Contains(other);
        }

        /// <summary>Indicates whether this and another instruction ever match at the same positions.</summary>
        /// <param name="other">The other instruction.</param>
        /// <returns><c>true</c> if this instruction and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other)
        {
            return Left.DisjointWith(other) || Right.DisjointWith(other);
        }

        /// <summary>Indicates whether the start of the instruction was generated from a regex as part of a derivative.</summary>
        /// <param name="ast">A regex from which <see cref="Left"/> may have been generated.</param>
        /// <returns><c>true</c> if <paramref name="ast"/> is the origin of <see cref="Left"/>, otherwise <c>false</c>.</returns>
        public override bool ComesFrom(RegexAST ast) { return Left.ComesFrom(ast); }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth
        {
            get { return Left.IsZeroWidth && Right.IsZeroWidth; }
        }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return Left.SideEffects + Right.SideEffects; } }

        /// <summary>Maximum number of saved partial results while evaluating this instruction.</summary>
        public override int TestDepth { get { return Math.Max(Left.TestDepth + 1, Right.TestDepth); } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = NFA.TestCode.And,
                arg1 = builder.AddInstructions(Left),
                arg2 = builder.AddInstructions(Right),
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent)
        {
            bool parens = !(parent is OrTestAST || parent is AndTestAST);
            if (parens)
            {
                sb.Append("[");
                if (parent is NotTestAST) sb.Append("^");
            }
            Left.Write(sb, this);
            sb.Append("&&");
            Right.Write(sb, this);
            if (parens) sb.Append("]");
        }
#endif

    }
}
