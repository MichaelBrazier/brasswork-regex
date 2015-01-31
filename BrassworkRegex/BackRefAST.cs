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
using System.Collections.Generic;

namespace Brasswork.Regex
{
    /// <summary>A syntax tree for a backreference.</summary>
    internal sealed class BackRefAST : RegexAST
    {
        private int position;

        /// <summary>ID of the capture to check.</summary> 
        internal int CaptureID;

        /// <summary>Name of the capture to check.</summary> 
        internal string Name;

        /// <summary>Whether the AST folds the case of a codepoint before checking it.</summary>
        internal bool FoldsCase { get; private set; }

        private BackRefAST(int position, int captureIndex, string captureName, bool foldCaseCompare)
            : base() 
        {
            this.position = position;
            CaptureID = captureIndex;
            Name = captureName;
            FoldsCase = foldCaseCompare;
        }

        /// <summary>Builds a syntax tree for a backreference.</summary>
        /// <param name="position">The backreference's position in the parsed string.</param>
        /// <param name="captureIndex">Index of the capture to check.</param>
        /// <param name="foldCaseCompare">Whether the AST folds the case of a codepoint before checking it.</param>
        /// <returns>The syntax tree.</returns>
        internal static RegexAST Make(int position, int captureIndex, bool foldCaseCompare)
        {
            return new BackRefAST(position, captureIndex, null, foldCaseCompare);
        }

        /// <summary>Builds a syntax tree for a backreference.</summary>
        /// <param name="position">The backreference's position in the parsed string.</param>
        /// <param name="captureName">Name of the capture to check.</param>
        /// <param name="foldCaseCompare">Whether the AST folds the case of a codepoint before checking it.</param>
        /// <returns>The syntax tree.</returns>
        internal static RegexAST Make(int position, string captureName, bool foldCaseCompare)
        {
            return new BackRefAST(position, -1, captureName, foldCaseCompare);
        }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            info.backrefs = true;
            if (CaptureID < 0 || Name == null)
                info.SetCaptureInfo(ref CaptureID, ref Name, position);
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            if (FoldsCase)
                sb.Append("(?i)");
            sb.Append("\\");
            if (Name == null || CaptureID.ToString() == Name)
            {
                sb.Append(CaptureID.ToString());
            }
            else
            {
                sb.Append("m{");
                sb.Append(Name);
                sb.Append("}");
            }
        }
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars
        {
            get { return EmptyAST.Instance; }
        }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>The non-failing partial derivatives of this AST, together with the tests leading to them.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            List<RegexDerivative> derivs = new List<RegexDerivative>();
            derivs.Add(new RegexDerivative(new BackDoneAST(this), EmptyAST.Instance));
            derivs.Add(new RegexDerivative(new BackCheckAST(this), this));
            return derivs;
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return false; } }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return false; } }
    }

    /// <summary>A syntax tree that scans one character from a previously matched capture group.</summary>
    internal sealed class BackCheckAST : CharClassAST
    {
        /// <summary>The original backreference.</summary> 
        internal BackRefAST Source { get; private set; }

        /// <summary>Builds an AST that scans one character for a backreference.</summary>
        /// <param name="source">The original backreference.</param>
        internal BackCheckAST(BackRefAST source) : base() { Source = source; }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>false</c>; the character a backreference matches varies with the NFA's state.</returns>
        public override bool Contains(int codepoint) { return false; }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return 1; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = Source.FoldsCase ? NFA.TestCode.BackrefCheckFC : NFA.TestCode.BackrefCheck,
                arg1 = Source.CaptureID,
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            if (Source.FoldsCase)
                sb.Append("(?i)");
            sb.AppendFormat("\\g{0:d}", Source.CaptureID);
        }
#endif
    }

    /// <summary>A syntax tree that, when processed, checks if a backreference has been completely matched.</summary>
    internal sealed class BackDoneAST : ZeroWidthInstructAST
    {
        /// <summary>The original backreference.</summary> 
        internal BackRefAST Source { get; private set; }

        /// <summary>Builds an AST that checks if a backreference has been matched.</summary>
        /// <param name="source">The original backreference.</param>
        internal BackDoneAST(BackRefAST source) : base() { Source = source; }

        public override bool ComesFrom(RegexAST ast)
        {
            StarAST starAST = ast as StarAST;
            if (starAST != null)
                return ComesFrom(starAST.Argument);

            QuantifierAST quantAST = ast as QuantifierAST;
            if (quantAST != null)
                return ComesFrom(quantAST.Argument);

            return Source.Equals(ast);
        }

        /// <summary>Count of possible side effects from evaluating this instruction.</summary>
        public override int SideEffects { get { return 1; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction { test = NFA.TestCode.BackrefDone, arg1 = Source.CaptureID, };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.AppendFormat("\\g{0:d}", Source.CaptureID);
            sb.Append("{f}");
        }
#endif
    }

    /// <summary>A syntax tree that, when processed, checks if a capture matched an empty substring.</summary>
    internal sealed class BackEmptyAST : ZeroWidthInstructAST
    {
        /// <summary>The original backreference.</summary> 
        internal BackRefAST Source { get; private set; }

        /// <summary>Builds an AST that checks if a backreference has been matched.</summary>
        /// <param name="source">The original backreference.</param>
        internal BackEmptyAST(BackRefAST source) : base() { Source = source; }

        public override bool ComesFrom(RegexAST ast)
        {
            StarAST starAST = ast as StarAST;
            if (starAST != null)
                return ComesFrom(starAST.Argument);

            QuantifierAST quantAST = ast as QuantifierAST;
            if (quantAST != null)
                return ComesFrom(quantAST.Argument);

            return Source.Equals(ast);
        }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction { test = NFA.TestCode.BackrefEmpty, arg1 = Source.CaptureID, };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.AppendFormat("\\g{0:d}", Source.CaptureID);
            sb.Append("{0}");
        }
#endif
    }
}
