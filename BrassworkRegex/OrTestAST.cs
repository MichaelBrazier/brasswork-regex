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
    /// <summary>The union of two <see cref="InstructAST"/>s.</summary>
    internal class OrTestAST : InstructAST
    {
        internal static readonly Func<InstructAST, InstructAST, InstructAST> Make;

        static OrTestAST()
        {
            Make = (left, right) => Build(left, right);
            Make = Make.Memoize();
        }

        /// <summary>The first alternative.</summary>
        internal InstructAST Left { get; private set; }

        /// <summary>The second alternative.</summary>
        internal InstructAST Right { get; private set; }

        internal OrTestAST(InstructAST left, InstructAST right) : base() { Left = left; Right = right; }

        internal static InstructAST Build(InstructAST left, InstructAST right)
        {
            // a || <empty> == <empty> || a == a
            if (EmptyAST.Instance == left) return right;
            if (EmptyAST.Instance == right) return left;

            // a || a == a
            if (left.Equals(right)) return left;

            // When a contains b, a || b == a
            if (left.Contains(right)) return left;

            // When b contains a, a || b == b
            if (right.Contains(left)) return right;

            // !a || !b == !(a && b), !a || b == !(a -- b), a || !b == !(b -- a)
            NotTestAST negLeft = left as NotTestAST;
            NotTestAST negRight = right as NotTestAST;
            if (negLeft != null)
            {
                if (negRight != null)
                    return NotTestAST.Make(AndTestAST.Make(negLeft.Argument, negRight.Argument));
                else
                    return NotTestAST.Make(DiffTestAST.Make(negLeft.Argument, right));
            }
            else if (negRight != null)
                return NotTestAST.Make(DiffTestAST.Make(negRight.Argument, left));

            // (a || b) || c == a || (b || c)
            OrTestAST orRight = right as OrTestAST;
            if (orRight != null)
                return OrTestAST.Make(OrTestAST.Make(left, orRight.Left), orRight.Right);

            // Merge overlapping and adjacent ranges
            OneCharAST oChar = null;
            CharRangeAST range = null;

            CharRangeAST leftRange = left as CharRangeAST;
            CharRangeAST rightRange = right as CharRangeAST;
            if (leftRange != null)
            {
                if (rightRange != null && (leftRange.FoldsCase == rightRange.FoldsCase) &&
                    rightRange.Max + 1 >= leftRange.Min && leftRange.Max + 1 >= rightRange.Min)
                {
                    int low = Math.Min(leftRange.Min, rightRange.Min);
                    int high = Math.Max(leftRange.Max, rightRange.Max);
                    return CharRangeAST.Make(low, high, leftRange.FoldsCase);
                }

                oChar = right as OneCharAST;
                range = leftRange;
            }
            else if (rightRange != null)
            {
                oChar = left as OneCharAST;
                range = rightRange;
            }

            // Merge a character adjacent to a range with the range
            if (oChar != null)
            {
                if (range.FoldsCase && oChar.FoldsCase)
                {
                    if (oChar.Character.ToCaseFold() == range.Min.ToCaseFold() - 1)
                        return CharRangeAST.Make(range.Min - 1, range.Max, true);
                    else if (oChar.Character.ToCaseFold() == range.Max.ToCaseFold() + 1)
                        return CharRangeAST.Make(range.Min, range.Max + 1, true);
                }
                if (!range.FoldsCase && !oChar.FoldsCase)
                {
                    if (oChar.Character == range.Min - 1)
                        return CharRangeAST.Make(range.Min - 1, range.Max, false);
                    else if (oChar.Character == range.Max + 1)
                        return CharRangeAST.Make(range.Min, range.Max + 1, false);
                }
            }

            return new OrTestAST(left, right);
        }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/> belongs to <see cref="Left"/>
        /// or <see cref="Right"/>, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint)
        {
            return (!Left.IsZeroWidth && Left.Contains(codepoint)) ||
                (!Right.IsZeroWidth && Right.Contains(codepoint));
        }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of <see cref="Left"/>
        /// or <see cref="Right"/>, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other)
        {
            return Left.Contains(other) || Right.Contains(other);
        }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other)
        {
            return Left.DisjointWith(other) && Right.DisjointWith(other);
        }

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
                test = NFA.TestCode.Or,
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
            bool parens = !(parent is OrTestAST);
            if (parens)
            {
                sb.Append("[");
                if (parent is NotTestAST) sb.Append("^");
            }
            Left.Write(sb, this);
            if (Left is AndTestAST || Left is DiffTestAST || Right is AndTestAST || Right is DiffTestAST)
                sb.Append("||");
            Right.Write(sb, this);
            if (parens) sb.Append("]");
        }
#endif
    }
}
