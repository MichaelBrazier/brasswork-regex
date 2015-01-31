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
    /// <summary>The difference of two <see cref="InstructAST"/>s.</summary>
    internal class DiffTestAST : InstructAST
    {
        internal static readonly Func<InstructAST, InstructAST, InstructAST> Make;

        static DiffTestAST()
        {
            Make = (left, right) => Build(left, right);
            Make = Make.Memoize();
        }

        /// <summary>The included characters.</summary>
        internal InstructAST Left { get; private set; }

        /// <summary>The excluded characters.</summary>
        internal InstructAST Right { get; private set; }

        internal DiffTestAST(InstructAST left, InstructAST right) : base() { Left = left; Right = right; }

        internal static InstructAST Build(InstructAST left, InstructAST right)
        {
            // a -- a == <fail>
            if (left.Equals(right)) return AnyCharAST.Negate;

            // When b contains a, a -- b matches no codepoints
            if (right.Contains(left)) return AnyCharAST.Negate;

            // When a and b are disjoint, a -- b == a
            if (left.DisjointWith(right)) return left;

            // \p{Any} -- a == !a
            if (AnyCharAST.Instance.Equals(left)) return NotTestAST.Make(right);

            // !a -- !b == b -- a; !a -- b == !(a || b); a -- !b == a && b
            NotTestAST negLeft = left as NotTestAST;
            NotTestAST negRight = right as NotTestAST;
            if (negLeft != null)
            {
                if (negRight != null)
                    return DiffTestAST.Make(negRight.Argument, negLeft.Argument);
                else
                    return NotTestAST.Make(OrTestAST.Make(negLeft.Argument, right));
            }
            else if (negRight != null)
                return AndTestAST.Make(left, negRight.Argument);

            // (a -- b) -- c == a -- (b || c)
            DiffTestAST diffLeft = left as DiffTestAST;
            if (diffLeft != null)
                return DiffTestAST.Make(diffLeft.Left, OrTestAST.Make(diffLeft.Right, right));

            // (a || b) -- c == (a -- c) || (b -- c)
            OrTestAST orLeft = left as OrTestAST;
            if (orLeft != null)
                return OrTestAST.Make(DiffTestAST.Make(orLeft.Left, right), DiffTestAST.Make(orLeft.Right, right));

            // When a and b are overlapping ranges, a -- b == a with a gap
            CharRangeAST leftRange = left as CharRangeAST;
            if (leftRange != null)
            {
                int excludeMin = -1;
                int excludeMax = -1;

                OneCharAST rightChar = right as OneCharAST;
                if (rightChar != null && (leftRange.FoldsCase == rightChar.FoldsCase))
                {
                    excludeMin = excludeMax = rightChar.Character;
                }

                CharRangeAST rightRange = right as CharRangeAST;
                if (rightRange != null && (leftRange.FoldsCase == rightRange.FoldsCase))
                {
                    excludeMin = rightRange.Min;
                    excludeMax = rightRange.Max;
                }

                if (leftRange.Min <= excludeMax && excludeMin <= leftRange.Max)
                    return OrTestAST.Make(CharRangeAST.Make(leftRange.Min, excludeMin - 1, leftRange.FoldsCase),
                        CharRangeAST.Make(excludeMax + 1, leftRange.Max, leftRange.FoldsCase));
            }

            return new DiffTestAST(left, right);
        }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/> belongs to <see cref="Left"/>
        /// but not <see cref="Right"/>, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint)
        {
            return (Left.IsZeroWidth || Left.Contains(codepoint)) &&
                (Right.IsZeroWidth || !Right.Contains(codepoint));
        }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of <see cref="Left"/>
        /// but not <see cref="Right"/>, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other)
        {
            return Left.Contains(other) && Right.DisjointWith(other);
        }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other)
        {
            return Left.DisjointWith(other) || Right.Contains(other);
        }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth
        {
            get { return Left.IsZeroWidth && Right.IsZeroWidth; }
        }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return Left.SideEffects + Right.SideEffects; } }

        /// <summary>Maximum number of saved partial results while evaluating this instruction.</summary>
        public override int TestDepth { get { return Math.Max(Left.TestDepth, Right.TestDepth + 1); } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = NFA.TestCode.Diff,
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
            bool parens = !(parent is OrTestAST || parent is AndTestAST || parent is DiffTestAST);
            if (parens)
            {
                sb.Append("[");
                if (parent is NotTestAST) sb.Append("^");
            }
            Left.Write(sb, this);
            sb.Append("--");
            Right.Write(sb, this);
            if (parens) sb.Append("]");
        }
#endif
    }
}
