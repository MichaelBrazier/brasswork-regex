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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Brasswork.Regex
{
    /// <summary>The NFA that implements matching with a regular expression.</summary>
    internal sealed class NFA
    {
        /// <summary>Number of atomic groups in the regular expression.</summary>
        internal readonly int AtomicCount;

        /// <summary>Names of capturing groups in the regular expression.</summary>
        internal readonly string[] GroupNames;

        /// <summary>Depth of quantifier nesting in the regular expression.</summary>
        internal readonly int QuantDepth;

        /// <summary>Whether the regular expression must start at the start of a string.</summary>
        internal readonly bool Anchored;

        /// <summary>Whether the regular expression contains any backreferences.</summary>
        internal readonly bool Backrefs;

        /// <summary>Types of instructions.</summary>
        internal enum TestCode : byte
        {
            Character,
            CharacterFC,
            Range,
            RangeFC,
            BoolProperty,
            Category,
            Script,
            ScriptExt,
            QuantMax,
            QuantMin,
            AtomOpen,
            AtomClose,
            CaptOpen,
            CaptClose,
            CaptCheck,
            BackrefCheck,
            BackrefCheckFC,
            BackrefDone,
            BackrefEmpty,
            Not,
            Or,
            And,
            Diff,
        }

        internal class Instruction
        {
            public TestCode test;
            public int arg1;
            public int arg2;
        }

        /// <summary>Instructions the NFA may execute while matching a string.</summary>
        internal readonly Instruction[] Instructions;

        /// <summary>Stack of indexes of instructions the match has started evaluating but not completed.</summary>
        private readonly int[] Pending;

        /// <summary>Current top of the Pending stack.</summary>
        private int PendingTop;

        private struct UndoStep
        {
            public enum Field : byte { State, Position, Backpos, QuantMax, QuantMin, OpenCapt, CloseCapt, OpenAtom, CloseAtom };
            public Field field;
            public int index;
            public int value;
        }

        /// <summary>Records the original values of fields changed while evaluating a match, so they can be reversed.</summary>
        private UndoStep[] Undos;

        /// <summary>Current top of the Undos stack.</summary>
        private int UndoTop;

        private void UndoPush(UndoStep.Field field, int value)
        {
            Undos[UndoTop].field = field;
            Undos[UndoTop].value = value;
            UndoTop++;
        }

        private void UndoPush(UndoStep.Field field, int value, int index)
        {
            Undos[UndoTop].field = field;
            Undos[UndoTop].index = index;
            Undos[UndoTop].value = value;
            UndoTop++;
        }

        internal struct Transition
        {
            public int instruction;
            public int target;
        }

        /// <summary>The states of the NFA. Each state is represented by the set of its transitions.</summary>
        internal readonly Transition[][] States;

        /// <summary>Count of the NFA's transitions.</summary>
        internal readonly int nTransitions;

        /// <summary>Index of the state corresponding to the empty regular expression.</summary>
        internal readonly int FinalState;

        /// <summary>Index of an instruction that checks whether a position can begin a match.</summary>
        internal readonly int FirstTest;

        internal readonly BoyerMooreScanner FixedPrefix;

        private readonly Match.SameFinal sameFinal;

        internal NFA(NFABuilder builder)
        {
            this.sameFinal = new Match.SameFinal();
            this.Pending = new int[builder.TestDepth];
            this.PendingTop = 0;
            this.Undos = new UndoStep[builder.MaxUndos + 2];
            this.UndoTop = 0;

            AtomicCount = builder.ParseInfo.nAtomics;
            GroupNames = builder.ParseInfo.captures.ToArray();
            QuantDepth = builder.ParseInfo.quantDepth;
            Anchored = builder.ParseInfo.anchor;
            Backrefs = builder.ParseInfo.backrefs;
            Instructions = builder.Instructions.ToArray();

            nTransitions = 0;
            List<Transition[]> stateList = new List<Transition[]>();
            foreach (List<Transition> transList in builder.States)
            {
                stateList.Add(transList.ToArray());
                nTransitions += transList.Count;
            }
            States = stateList.ToArray();
            FinalState = builder.FinalState;
            FirstTest = builder.FirstTest;
            FixedPrefix = builder.FixedPrefix;
        }

        /// <summary>Searches for a match, beginning from a specified position.</summary>
        /// <param name="text">The text to match against.</param>
        /// <param name="startIndex">The starting position.</param>
        /// <param name="anchored">Set to <c>true</c> to force matches to start from <paramref name="startIndex"/>.</param>
        /// <returns>Result of the search.</returns>
        internal Match Find(string text, int startIndex, bool anchored)
        {
            int length = text.Length;
            if (Anchored) anchored = true;
            Stack<Match> avail = new Stack<Match>();
            Queue<Match> active = new Queue<Match>(nTransitions);

            Match best = new Match(this, text, anchored);
            Match start = new Match(this, text, anchored);
            start.StateID = 0;
            start.Position = startIndex;
            if (!anchored) Scan(start);
            do
            {
                Match current;
                if (active.Count == 0 || active.Peek().Position > start.Position)
                    current = start;
                else
                    current = active.Dequeue();
                Transition[] trans = States[current.StateID];
                for (int i = 0; i < trans.Length; i++)
                {
                    if (Evaluate(current, trans[i].instruction, trans[i].target, true))
                    {
                        // don't duplicate work or run past the end of text
                        if (current.Position <= length && !active.Contains(current, sameFinal))
                        {
                            Match next;
                            if (avail.Count == 0)
                                next = new Match(this, text, anchored);
                            else
                                next = avail.Pop();
                            next.Copy(current);
                            active.Enqueue(next);
                        }
                    }
                    else if (Match.PosixPriority(current, best) > 0)
                        best.Copy(current);

                    Unevaluate(current);
                }
                if (current != start)
                    avail.Push(current);
                else if (anchored || best.IsSuccess)
                    start.Position = length;
                else
                {
                    start.Position++;
                    Scan(start);
                }
            } while (active.Count > 0 || start.Position < length);

            return best;
        }

        /// <summary>Moves the match's position to the next point in the string where the NFA could match.</summary>
        /// <param name="m">The match to be moved.</param>
        internal void Scan(Match m)
        {
            if (FixedPrefix != null)
            {
                FixedPrefix.Scan(m.Text, m.Position);
                m.Position = FixedPrefix.Position;
            }
            else if (FirstTest != -1)
            {
                int len = m.Text.Length;
                while (m.Position < len && !Evaluate(m, FirstTest, 0, false))
                {
                    if (m.Position + 1 < len && (m.Text[m.Position] & Unicode7.SURROGATE_MASK) == Unicode7.HI_SURR_MIN
                        && (m.Text[m.Position + 1] & Unicode7.SURROGATE_MASK) == Unicode7.LO_SURR_MIN) m.Position++;
                    m.Position++;
                }
            }
        }

        /// <summary>Evaluates an instruction at a match's current position.</summary>
        /// <param name="m">The match to be evaluated.</param>
        /// <param name="instructID">Identifies the instruction to evaluate.</param>
        /// <param name="targetID">Identifies the NFA state to enter if evaluation succeeds.</param>
        /// <param name="saveUndos">Whether the match can roll back side effects of evaluation.</param>
        /// <returns><c>true</c> if the instruction succeeds at <paramref name="m"/>'s position,
        /// otherwise <c>false</c>.</returns>
        internal bool Evaluate(Match m, int instructID, int targetID, bool saveUndos)
        {
            bool leafward = true;
            bool proceed = false;
            int currentChar = m.Text.ToCodepoint(m.Position);
            while (leafward || PendingTop > 0)
            {
                if (!leafward)
                {
                    PendingTop--;
                    instructID = Pending[PendingTop];
                }
                Instruction ins = Instructions[instructID];
                switch (ins.test)
                {
                    case TestCode.Character:
                        proceed = ins.arg1 == currentChar;
                        leafward = false; break;

                    case TestCode.CharacterFC:
                        proceed = ins.arg1 == currentChar.ToCaseFold();
                        leafward = false; break;

                    case TestCode.Range:
                        proceed = ins.arg1 <= currentChar && currentChar <= ins.arg2;
                        leafward = false; break;

                    case TestCode.RangeFC:
                        if (currentChar == int.MaxValue)
                            proceed = false;
                        else
                        {
                            bool forValue = false;
                            for (int ch = ins.arg1; !forValue && ch <= ins.arg2; ch++)
                                if (ch.ToCaseFold() == currentChar.ToCaseFold()) forValue = true;
                            proceed = forValue;
                        }
                        leafward = false; break;

                    case TestCode.BoolProperty:
                        proceed = ((Unicode7.Property)ins.arg1).Contains(m.Text, m.Position);
                        leafward = false; break;

                    case TestCode.Category:
                        proceed = currentChar.GetUnicodeCategory() == (UnicodeCategory)ins.arg1;
                        leafward = false; break;

                    case TestCode.Script:
                        proceed = currentChar.GetScript() == (Unicode7.Script)ins.arg1;
                        leafward = false; break;

                    case TestCode.ScriptExt:
                        proceed = currentChar.IsInExtendedScript((Unicode7.Script)ins.arg1);
                        leafward = false; break;

                    case TestCode.QuantMax:
                        proceed = ins.arg2 < 0 || m.data[m.quantStart + ins.arg1] < ins.arg2;
                        if (proceed)
                        {
                            if (saveUndos)
                                UndoPush(UndoStep.Field.QuantMax, m.data[m.quantStart + ins.arg1], ins.arg1);
                            m.data[m.quantStart + ins.arg1]++;
                        }
                        leafward = false; break;

                    case TestCode.QuantMin:
                        proceed = m.data[m.quantStart + ins.arg1] >= ins.arg2;
                        if (proceed)
                        {
                            if (saveUndos)
                                UndoPush(UndoStep.Field.QuantMin, m.data[m.quantStart + ins.arg1], ins.arg1);
                            m.data[m.quantStart + ins.arg1] = 0;
                        }
                        leafward = false; break;

                    case TestCode.AtomOpen:
                        proceed = true;
                        if (saveUndos)
                        {
                            UndoPush(UndoStep.Field.OpenAtom, m.data[(m.nCaptures + ins.arg1) << 1], ins.arg1);
                            UndoPush(UndoStep.Field.CloseAtom, m.data[((m.nCaptures + ins.arg1) << 1) + 1], ins.arg1);
                        }
                        m.data[(m.nCaptures + ins.arg1) << 1] = m.Position;
                        m.data[((m.nCaptures + ins.arg1) << 1) + 1] = -1;
                        leafward = false; break;

                    case TestCode.AtomClose:
                        proceed = true;
                        if (saveUndos)
                            UndoPush(UndoStep.Field.CloseAtom, m.data[((m.nCaptures + ins.arg1) << 1) + 1], ins.arg1);
                        m.data[((m.nCaptures + ins.arg1) << 1) + 1] = m.Position;
                        leafward = false; break;

                    case TestCode.CaptOpen:
                        proceed = true;
                        if (saveUndos)
                        {
                            UndoPush(UndoStep.Field.OpenCapt, m.data[ins.arg1 << 1], ins.arg1);
                            UndoPush(UndoStep.Field.CloseCapt, m.data[(ins.arg1 << 1) + 1], ins.arg1);
                        }
                        m.data[ins.arg1 << 1] = m.Position;
                        m.data[(ins.arg1 << 1) + 1] = -1;
                        leafward = false; break;

                    case TestCode.CaptClose:
                        proceed = true;
                        if (saveUndos)
                            UndoPush(UndoStep.Field.CloseCapt, m.data[(ins.arg1 << 1) + 1], ins.arg1);
                        if (m.data[(ins.arg1 << 1) + 1] < 0) m.data[(ins.arg1 << 1) + 1] = m.Position;
                        leafward = false; break;

                    case TestCode.CaptCheck:
                        proceed = m.data[(ins.arg1 << 1) + 1] >= 0;
                        leafward = false; break;

                    case TestCode.BackrefCheck:
                        if (currentChar == int.MaxValue || m.data[(ins.arg1 << 1) + 1] < 0)
                            proceed = false;
                        else
                        {
                            int back = (m.backPosition < 0) ? m.data[ins.arg1 << 1] : m.backPosition;
                            proceed = back < m.data[(ins.arg1 << 1) + 1] &&
                                currentChar == m.Text.ToCodepoint(back);
                            if (proceed)
                            {
                                if (saveUndos)
                                    UndoPush(UndoStep.Field.Backpos, m.backPosition);
                                m.backPosition = back;
                                m.Text.NextCodepoint(ref m.backPosition);
                            }
                        }
                        leafward = false; break;

                    case TestCode.BackrefCheckFC:
                        if (currentChar == int.MaxValue || m.data[(ins.arg1 << 1) + 1] < 0)
                            proceed = false;
                        else
                        {
                            int back = (m.backPosition < 0) ? m.data[ins.arg1 << 1] : m.backPosition;
                            proceed = back < m.data[(ins.arg1 << 1) + 1] &&
                                currentChar.ToCaseFold() == m.Text.ToCodepoint(back).ToCaseFold();
                            if (proceed)
                            {
                                if (saveUndos)
                                    UndoPush(UndoStep.Field.Backpos, m.backPosition);
                                m.backPosition = back;
                                m.Text.NextCodepoint(ref m.backPosition);
                            }
                        }
                        leafward = false; break;

                    case TestCode.BackrefDone:
                        if (m.backPosition < 0)
                            proceed = m.data[(ins.arg1 << 1) + 1] == m.data[ins.arg1 << 1];
                        else
                        {
                            proceed = m.backPosition >= m.data[(ins.arg1 << 1) + 1];
                            if (proceed)
                            {
                                if (saveUndos)
                                    UndoPush(UndoStep.Field.Backpos, m.backPosition);
                                m.backPosition = -1;
                            }
                        }
                        leafward = false; break;

                    case TestCode.BackrefEmpty:
                        proceed = m.data[(ins.arg1 << 1) + 1] == m.data[ins.arg1 << 1];
                        leafward = false; break;

                    case TestCode.Diff:
                        if (leafward)
                        {
                            Pending[PendingTop] = instructID;
                            PendingTop++;
                            instructID = ins.arg2;
                        }
                        else if (proceed)
                            proceed = false;
                        else
                        {
                            instructID = ins.arg1;
                            leafward = true;
                        }
                        break;

                    case TestCode.And:
                        if (leafward)
                        {
                            Pending[PendingTop] = instructID;
                            PendingTop++;
                            instructID = ins.arg1;
                        }
                        else if (proceed)
                        {
                            instructID = ins.arg2;
                            leafward = true;
                        }
                        break;

                    case TestCode.Or:
                        if (leafward)
                        {
                            Pending[PendingTop] = instructID;
                            PendingTop++;
                            instructID = ins.arg1;
                        }
                        else if (!proceed)
                        {
                            instructID = ins.arg2;
                            leafward = true;
                        }
                        break;

                    case TestCode.Not:
                        if (leafward)
                        {
                            Pending[PendingTop] = instructID;
                            PendingTop++;
                            instructID = ins.arg1;
                        }
                        else
                            proceed = !proceed;
                        break;

                    default:
                        PendingTop = 0;
                        throw new InvalidOperationException(String.Format("Opcode ({0:d}) is invalid", ins.test));
                }
            }
            if (proceed)
            {
                proceed = targetID != FinalState;
                if (saveUndos)
                {
                    UndoPush(UndoStep.Field.State, m.StateID);
                    m.StateID = targetID;
                    if (proceed)
                    {
                        UndoPush(UndoStep.Field.Position, m.Position);
                        m.Text.NextCodepoint(ref m.Position);
                    }
                }
            }
            return proceed;
        }

        internal void Unevaluate(Match m)
        {
            while (UndoTop > 0)
            {
                UndoTop--;
                switch (Undos[UndoTop].field)
                {
                    case UndoStep.Field.State:
                        m.StateID = Undos[UndoTop].value; break;

                    case UndoStep.Field.Position:
                        m.Position = Undos[UndoTop].value; break;

                    case UndoStep.Field.Backpos:
                        m.backPosition = Undos[UndoTop].value; break;

                    case UndoStep.Field.QuantMax:
                        m.data[m.quantStart + Undos[UndoTop].index]--; break;

                    case UndoStep.Field.QuantMin:
                        m.data[m.quantStart + Undos[UndoTop].index] = Undos[UndoTop].value; break;

                    case UndoStep.Field.OpenCapt:
                        m.data[Undos[UndoTop].index << 1] = Undos[UndoTop].value; break;

                    case UndoStep.Field.CloseCapt:
                        m.data[(Undos[UndoTop].index << 1) + 1] = Undos[UndoTop].value; break;

                    case UndoStep.Field.OpenAtom:
                        m.data[(m.nCaptures + Undos[UndoTop].index) << 1] = Undos[UndoTop].value; break;

                    case UndoStep.Field.CloseAtom:
                        m.data[((m.nCaptures + Undos[UndoTop].index) << 1) + 1] = Undos[UndoTop].value; break;
                }
            }
        }

#if DEBUG
        internal string InstructionString(int i)
        {
            switch (Instructions[i].test)
            {
                case NFA.TestCode.Character:
                    return Parser.EscapeChar(Instructions[i].arg1, true);

                case NFA.TestCode.CharacterFC:
                    return String.Format("{0}(i)", Parser.EscapeChar(Instructions[i].arg1, true));

                case NFA.TestCode.Range:
                    return String.Format("{0}-{1}", Parser.EscapeChar(Instructions[i].arg1, true),
                        Parser.EscapeChar(Instructions[i].arg2, true));

                case NFA.TestCode.RangeFC:
                    return String.Format("{0}-{1}(i)", Parser.EscapeChar(Instructions[i].arg1, true),
                        Parser.EscapeChar(Instructions[i].arg2, true));

                case NFA.TestCode.BoolProperty:
                    return Unicode7.Abbreviation((Unicode7.Property)Instructions[i].arg1, true);

                case NFA.TestCode.Category:
                    return @"\p{gc=" + Unicode7.Abbreviation((UnicodeCategory)Instructions[i].arg1) + "}";

                case NFA.TestCode.Script:
                    return @"\p{sc=" + Unicode7.Abbreviation((Unicode7.Script)Instructions[i].arg1) + "}";

                case NFA.TestCode.ScriptExt:
                    return @"\p{scx=" + Unicode7.Abbreviation((Unicode7.Script)Instructions[i].arg1) + "}";

                case NFA.TestCode.AtomOpen:
                    return String.Format("(?>{0:d}s)", Instructions[i].arg1);

                case NFA.TestCode.AtomClose:
                    return String.Format("(?>{0:d}e)", Instructions[i].arg1);

                case NFA.TestCode.CaptOpen:
                    return String.Format("(+{0:d}s)", Instructions[i].arg1);

                case NFA.TestCode.CaptClose:
                    return String.Format("(+{0:d}e)", Instructions[i].arg1);

                case NFA.TestCode.CaptCheck:
                    return String.Format("(+{0:d}c)", Instructions[i].arg1);

                case NFA.TestCode.BackrefCheck:
                    return String.Format("\\{0:d}c", Instructions[i].arg1);

                case NFA.TestCode.BackrefCheckFC:
                    return String.Format("\\{0:d}i", Instructions[i].arg1);

                case NFA.TestCode.BackrefDone:
                    return String.Format("\\{0:d}f", Instructions[i].arg1);

                case NFA.TestCode.BackrefEmpty:
                    return String.Format("\\{0:d}e", Instructions[i].arg1);

                case NFA.TestCode.QuantMax:
                    return "{" + Instructions[i].arg1.ToString() +
                        (Instructions[i].arg2 < 0 ? "++" : ("<" + Instructions[i].arg2.ToString())) + "}";

                case NFA.TestCode.QuantMin:
                    return "{" + Instructions[i].arg1.ToString() + ">=" + Instructions[i].arg2.ToString() + "}";

                case NFA.TestCode.Not:
                    return String.Format("[^{0}]", InstructionString(Instructions[i].arg1));

                case NFA.TestCode.Or:
                    return String.Format("[{0}||{1}]", InstructionString(Instructions[i].arg1), InstructionString(Instructions[i].arg2));

                case NFA.TestCode.And:
                    return String.Format("[{0}&&{1}]", InstructionString(Instructions[i].arg1), InstructionString(Instructions[i].arg2));

                case NFA.TestCode.Diff:
                    return String.Format("[{0}--{1}]", InstructionString(Instructions[i].arg1), InstructionString(Instructions[i].arg2));

                default:
                    throw new InvalidOperationException(String.Format("Opcode ({0:d}) is invalid", Instructions[i].test));
            }
        }
#endif
    }
}
