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
using System.Globalization;

namespace Brasswork.Regex
{
    /// <summary>A syntax leaf that active any codepoint satisfying a binary property.</summary>
    internal class PropertyAST : CharClassAST
    {
        internal static readonly Func<Unicode7.Property, PropertyAST> Make;

        static PropertyAST()
        {
            Make = prop => new PropertyAST(prop);
            Make = Functional.Memoize(Make);
        }

        private PropertyAST(Unicode7.Property property) : base() { Property = property; }

        /// <summary>The code for the binary property.</summary>
        public Unicode7.Property Property { get; private set; }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction { test = NFA.TestCode.BoolProperty, arg1 = (int)Property };
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

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/>is in the class, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint) { return Property.Contains(codepoint); }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this class, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other)
        {
            PropertyAST oProp = other as PropertyAST;
            if (oProp != null)
                return this.Property.Contains(oProp.Property);

            CategoryAST oCat = other as CategoryAST;
            if (oCat != null)
                return this.Property.Contains(oCat.Category);
            
            return base.Contains(other);
        }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other)
        {
            PropertyAST oProp = other as PropertyAST;
            if (oProp != null)
                return this.Property.DisjointWith(oProp.Property);

            CategoryAST oCat = other as CategoryAST;
            if (oCat != null)
                return this.Property.DisjointWith(oCat.Category);
            
            return base.DisjointWith(other);
        }
    }

    /// <summary>A syntax leaf that matches any codepoint in a <see cref="UnicodeCategory"/>.</summary>
    internal sealed class CategoryAST : CharClassAST
    {
        internal static readonly Func<UnicodeCategory, CategoryAST> Make;

        static CategoryAST()
        {
            Make = cat => new CategoryAST(cat);
            Make = Functional.Memoize(Make);
        }

        /// <summary>The general category.</summary>
        internal UnicodeCategory Category { get; private set; }

        /// <summary>Builds an AST that active any codepoint in a <see cref="UnicodeCategory"/>.</summary>
        /// <param name="cat">The general category.</param>
        private CategoryAST(UnicodeCategory cat) : base() { Category = cat; }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/>is in the class, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint)
        {
            return codepoint.GetUnicodeCategory() == Category;
        }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this class, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other)
        {
            CategoryAST oCat = other as CategoryAST;
            if (oCat != null)
                return this.Category == oCat.Category;
            
            return base.Contains(other);
        }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other)
        {
            PropertyAST oProp = other as PropertyAST;
            if (oProp != null)
                return oProp.Property.DisjointWith(this.Category);

            CategoryAST oCat = other as CategoryAST;
            if (oCat != null)
                return this.Category != oCat.Category;

            return base.DisjointWith(other);
        }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction { test = NFA.TestCode.Category, arg1 = (int)Category, };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            if (parent is NotTestAST)
                sb.Append("\\P{");
            else
                sb.Append("\\p{");
            sb.Append(Category.Abbreviation());
            sb.Append("}");
        }
#endif
    }

    /// <summary>A syntax leaf that matches any codepoint in a <see cref="Unicode7.Script"/>.</summary>
    internal sealed class ScriptAST : CharClassAST
    {
        internal static readonly Func<Unicode7.Script, bool, ScriptAST> Make;

        static ScriptAST()
        {
            Make = (script, checkExtended) => new ScriptAST(script, checkExtended);
            Make = Functional.Memoize(Make);
        }

        /// <summary>The code for the script.</summary>
        internal Unicode7.Script Script { get; private set; }

        /// <summary>Whether to check the Script Extensions property.</summary>
        internal bool IsExtended { get; private set; }

        /// <summary>Builds an AST that active any codepoint in a <see cref="Unicode7.Script"/>.</summary>
        /// <param name="script">The code for the script.</param>
        /// <param name="checkExtended">Whether to check the Script Extensions property.</param>
        private ScriptAST(Unicode7.Script script, bool checkExtended)
            : base()
        {
            Script = script; IsExtended = checkExtended;
        }

        /// <summary>Indicates whether a codepoint belongs to this character class.</summary>
        /// <param name="codepoint">The codepoint to be examined.</param>
        /// <returns><c>true</c> if <paramref name="codepoint"/>is in the class, otherwise <c>false</c>.</returns>
        public override bool Contains(int codepoint)
        {
            if (IsExtended)
                return codepoint.IsInExtendedScript(Script);
            else
                return codepoint.GetScript() == Script;
        }

        /// <summary>Indicates whether all characters in another character class are also in this class.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if <paramref name="other"/> is a subset of this class, otherwise <c>false.</c></returns>
        public override bool Contains(InstructAST other)
        {
            ScriptAST oScr = other as ScriptAST;
            if (oScr != null)
                return (this.IsExtended || !oScr.IsExtended) && this.Script == oScr.Script;

            return base.Contains(other);
        }

        /// <summary>Indicates whether this and another character class have no codepoints in common.</summary>
        /// <param name="other">The other character class.</param>
        /// <returns><c>true</c> if this character class and <paramref name="other"/> are disjoint,
        /// otherwise <c>false</c>.</returns>
        public override bool DisjointWith(InstructAST other)
        {
            ScriptAST oScr = other as ScriptAST;
            if (oScr != null)
                return !this.IsExtended && !oScr.IsExtended && this.Script != oScr.Script;

            return base.DisjointWith(other);
        }

        /// <summary>The translation of this syntax tree into NFA instruction code.</summary>
        /// <param name="builder">Data needed to construct an NFA.</param>
        /// <returns>The <see cref="NFA.Instruction"/> implementing this AST.</returns>
        public override NFA.Instruction ToInstruction(NFABuilder builder)
        {
            return new NFA.Instruction
            {
                test = IsExtended ? NFA.TestCode.ScriptExt : NFA.TestCode.Script,
                arg1 = (int)Script,
            };
        }

#if DEBUG
        /// <summary>Flattens the syntax tree into a representation of the source regex.</summary>
        /// <param name="sb">Buffer for the output string.</param>
        /// <param name="parent">The tree, if any, containing this syntax tree.</param>
        public override void Write(System.Text.StringBuilder sb, RegexAST parent)
        {
            if (parent is NotTestAST)
            {
                if (IsExtended)
                    sb.AppendFormat("\\P{{scx={0}}}", Script.Abbreviation());
                else
                    sb.AppendFormat("\\P{{sc={0}}}", Script.Abbreviation());
            }
            else
            {
                if (IsExtended)
                    sb.AppendFormat("\\p{{scx={0}}}", Script.Abbreviation());
                else
                    sb.AppendFormat("\\p{{sc={0}}}", Script.Abbreviation());
            }
        }
#endif
    }
}
