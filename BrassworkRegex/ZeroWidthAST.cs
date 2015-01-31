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
    /// <summary>A syntax tree that cannot consume characters during a match.</summary> 
    internal abstract class ZeroWidthInstructAST : InstructAST
    {
        /// <summary>Indicates whether this instruction matches a codepoint.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if this matches <paramref name="codepoint"/>, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint) { return false; }

        /// <summary>Indicates whether this and another instruction ever match at the same positions.</summary>
        /// <param name="other">The other instruction.</param>
        /// <returns><c>true</c> if this instruction and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other) { return false; }

        /// <summary>Whether all strings matching the AST have a fixed prefix.</summary>
        /// <param name="foldCase">Whether the prefix, if there is one, should be case-insensitive.</param>
        /// <returns><c>true</c> if there is a fixed prefix for the AST, otherwise <c>false</c>.</returns>
        public override bool IsFixed(bool foldCase) { return true; }

        /// <summary>Indicates whether this instruction matches a position without consuming a codepoint.</summary>
        public override bool IsZeroWidth { get { return true; } }
    }

    /// <summary>Syntax tree for the empty regex.</summary>
    internal sealed class EmptyAST : ZeroWidthInstructAST
    {
        /// <summary>The AST of the empty regex //.</summary>
        internal static readonly EmptyAST Instance = new EmptyAST();

        private EmptyAST() : base() { }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info) { }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(StringBuilder sb, RegexAST parent) { } // The empty string represents the empty regex.
#endif

        /// <summary>The test that must succeed at the beginning of a string matching this AST.</summary>
        public override InstructAST FirstChars { get { return AnyCharAST.Instance; } }

        /// <summary>Calculates <see cref="RegexAST.Derivatives"/> for this AST.</summary>
        /// <returns>An empty list of <see cref="RegexDerivative"/>s.</returns>
        internal override List<RegexDerivative> FindDerivatives()
        {
            return new List<RegexDerivative>();
        }

        /// <summary>Whether the AST unconditionally matches an empty substring.</summary>
        public override bool IsFinal { get { return true; } }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <exception cref="NotImplementedException">The method should never be called.</exception>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>A syntax tree that checks if the current position is a type of boundary.</summary> 
    internal sealed class BoundaryAST : ZeroWidthInstructAST
    {
        internal static readonly Func<Unicode7.Property, BoundaryAST> Make;

        static BoundaryAST()
        {
            Make = prop => new BoundaryAST(prop);
            Make = Functional.Memoize(Make);
        }

        private BoundaryAST(Unicode7.Property property) { Property = property; }

        internal Unicode7.Property Property { get; private set; }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction { test = NFA.TestCode.BoolProperty, arg1 = (int)Property, };
        }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            if (Property == Unicode7.Property.TextBegin) info.anchor = true;
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.Append(Property.Abbreviation(!(parent is NotTestAST)));
        }
#endif
    }

    /// <summary>A syntax tree that checks whether a capturing group has matched anything.</summary> 
    internal sealed class CaptureCheckAST : ZeroWidthInstructAST
    {
        private int position;

        /// <summary>Index of the capture to check.</summary> 
        internal int Index;

        /// <summary>Name of the capture to check.</summary> 
        internal string Name;

        private CaptureCheckAST(int position, int captureIndex, string captureName)
        {
            this.position = position;
            Index = captureIndex;
            Name = captureName;
        }

        /// <summary>Builds a syntax tree for a capture check.</summary>
        /// <param name="position">The capture check's position in the parsed string.</param>
        /// <param name="captureIndex">Index of the capture to check.</param>
        /// <returns>The syntax tree.</returns>
        internal static CaptureCheckAST Make(int position, int captureIndex)
        {
            return new CaptureCheckAST(position, captureIndex, null);
        }

        /// <summary>Builds a syntax tree for a capture check.</summary>
        /// <param name="position">The capture check's position in the parsed string.</param>
        /// <param name="captureName">Name of the capture to check.</param>
        /// <returns>The syntax tree.</returns>
        internal static CaptureCheckAST Make(int position, string captureName)
        {
            return new CaptureCheckAST(position, -1, captureName);
        }

        /// <summary>Annotates the AST with information on quantifiers and capturing groups.</summary>
        /// <param name="info">Collects information from the AST in which this is embedded.</param>
        public override void Annotate(AnnotationVisitor info)
        {
            base.Annotate(info);
            info.backrefs = true;
            if (Index < 0 || Name == null)
                info.SetCaptureInfo(ref Index, ref Name, position);
        }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction { test = NFA.TestCode.CaptCheck, arg1 = Index, };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            sb.Append((parent is NotTestAST) ? "\\C{" : "\\c{");
            sb.Append(Index);
            sb.Append("}");
        }
#endif
    }
}