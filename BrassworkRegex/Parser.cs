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
using System.Diagnostics;
using System.Globalization;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BrassworkRegexTest")]
namespace Brasswork.Regex
{
    /// <summary>Parses a regular expression, using recursive descent.</summary>
    internal static class Parser
    {
        internal static RegexAST Parse(string text, RegexOptions opts)
        {
            int position = 0;
            RegexAST ast = Expression(text, opts, false, ref position, 1);
            if (position < text.Length)
                throw new RegexParseException(text, position, "')' must be escaped");
            return CaptureAST.Make(ast, 0);
        }

        private static RegexAST Expression(string text, RegexOptions opts, bool inComp, ref int pos, byte minPrec)
        {
            RegexAST left = Sequence(text, opts, inComp, ref pos);
            char nextOp = (pos < text.Length) ? text[pos] : ')';
            byte prec;
            switch (nextOp)
            {
                case '~': prec = 3; break;
                case '&': prec = 2; break;
                case '|': prec = 1; break;
                default: prec = 0; break;
            }
            while (prec >= minPrec)
            {
                pos++;
                if (nextOp == '~') prec++;
                RegexAST right = Expression(text, opts, (nextOp == '~') || inComp, ref pos, prec);
                switch (nextOp)
                {
                    case '~':
                        if (left == EmptyAST.Instance)
                            left = ComplementAST.Make(right);
                        else
                            left = IntersectionAST.Make(left, ComplementAST.Make(right));
                        break;
                    case '&': left = IntersectionAST.Make(left, right); break;
                    case '|': left = AlternationAST.Make(left, right); break;
                    default: // this should never be reached
                        break;
                }
                nextOp = (pos < text.Length) ? text[pos] : ')';
                switch (nextOp)
                {
                    case '~': prec = 3; break;
                    case '&': prec = 2; break;
                    case '|': prec = 1; break;
                    default: prec = 0; break;
                }
            }
            return left;
        }

        private static RegexAST Sequence(string text, RegexOptions opts, bool inComp, ref int pos)
        {
            RegexAST ast;
            if (pos >= text.Length)
                ast = null;
            else switch (text[pos])
                {
                    case '~':
                    case '&':
                    case '|':
                    case ')': ast = null; break; // to signal end of sequence

                    case ']':
                    case '}':
                    case '?':
                    case '*':
                    case '+':
                    case '{': throw new RegexParseException(text, pos,
                        String.Format("'{0}' must be escaped", text[pos]));

                    default:
                        ast = Assertion(text, opts, ref pos);
                        if (ast == null)
                            ast = CharMatch(text, ref opts, inComp, ref pos);
                        ast = Quantifier(text, opts, ref pos, ast);
                        break;
                }

            if (ast != null)
            {
                RegexAST tail = Sequence(text, opts, inComp, ref pos);
                ast = SequenceAST.Make(ast, tail);
                return ast;
            }
            else
                return EmptyAST.Instance;
        }

        private static RegexAST Quantifier(string text, RegexOptions opts, ref int pos, RegexAST baseAst)
        {
            bool greedy = true;
            int min = -1, max = -1;
            if (pos >= text.Length)
                return baseAst;
            else switch (text[pos])
                {
                    case '?': pos++; min = 0; max = 1; break;
                    case '*': pos++; min = 0; break;
                    case '+': pos++; min = 1; break;

                    case '{':
                        pos++;
                        if (pos >= text.Length)
                            throw new RegexParseException(text, pos, "unterminated quantifier");

                        min = ReadNumber(text, ref pos);
                        if (pos >= text.Length)
                            throw new RegexParseException(text, pos, "unterminated quantifier");
                        else if (text[pos] == '}')
                        {
                            pos++;
                            max = min;
                        }
                        else if (text[pos] == ',')
                        {
                            pos++;
                            if (pos >= text.Length)
                                throw new RegexParseException(text, pos, "unterminated quantifier");
                            else if (text[pos] != '}')
                            {
                                max = ReadNumber(text, ref pos);
                                if (pos >= text.Length || text[pos] != '}')
                                    throw new RegexParseException(text, pos, "unterminated quantifier");
                            }
                            pos++;
                        }
                        else
                            throw new RegexParseException(text, pos, "unterminated quantifier");
                        break;
                }
            if (min != max)
            {
                greedy = !(pos < text.Length && text[pos] == '?');
                if (!greedy) pos++;
            }

            if (min < 0)
                return baseAst;
            else if (max >= 0 && max < min)
                throw new RegexParseException(text, pos, "max repeats less than min repeats");
            else
                return Quantifier(text, opts, ref pos, QuantifierAST.Make(baseAst, min, max, greedy));
        }

        private static InstructAST Assertion(string text, RegexOptions opts, ref int pos)
        {
            Debug.Assert(pos < text.Length, "Expected assertion, found end of text");
            bool multiline = (opts & RegexOptions.Multiline) != 0;
            bool simpleBreaks = (opts & RegexOptions.SimpleWordBreak) != 0;
            bool negate = false;
            InstructAST result = null;

            switch (text[pos])
            {
                case '\\':
                    if (pos + 1 >= text.Length)
                        result = null;
                    else switch (text[pos + 1])
                    {
                        case 'B':
                            negate = true; goto case 'b';

                        case 'b':
                            {
                                Unicode7.Property bound = simpleBreaks ?
                                        Unicode7.Property.SimpleWordBreak : Unicode7.Property.DefaultWordBreak;
                                pos += 2;
                                if (pos + 2 < text.Length && text[pos] == '{' && text[pos + 2] == '}')
                                {
                                    switch (text[pos + 1])
                                    {
                                        case 'a': pos += 3; bound = Unicode7.Property.LineBegin; break;
                                        case 'A': pos += 3; bound = Unicode7.Property.TextBegin; break;
                                        case 'z': pos += 3; bound = Unicode7.Property.LineEnd; break;
                                        case 'Z': pos += 3; bound = Unicode7.Property.TextEnd; break;
                                        case 'w': pos += 3; bound = Unicode7.Property.SimpleWordBreak; break;
                                        case 'W': pos += 3; bound = Unicode7.Property.DefaultWordBreak; break;
                                        default: break;
                                    }
                                }
                                result = BoundaryAST.Make(bound);
                                if (negate)
                                    result = NotTestAST.Make(result);
                                break;
                            }

                        default: result = null; break;
                    }
                    break;

                case '^':
                    pos++;
                    result = BoundaryAST.Make(multiline ? Unicode7.Property.LineBegin : Unicode7.Property.TextBegin);
                    break;

                case '$':
                    pos++;
                    result = BoundaryAST.Make(multiline ? Unicode7.Property.LineEnd : Unicode7.Property.TextEnd);
                    break;

                default: result = null; break;
            }
            return result;
        }

        private static RegexAST CharMatch(string text, ref RegexOptions opts, bool inComp, ref int pos)
        {
            bool foldCase = (opts & RegexOptions.IgnoreCase) != 0;
            bool multiline = (opts & RegexOptions.Multiline) != 0;
            bool dotAll = (opts & RegexOptions.DotAll) != 0;
            bool freeSpace = (opts & RegexOptions.FreeSpacing) != 0;

            if (freeSpace && text.IsWhitespace(pos))
            {
                do
                    text.NextCodepoint(ref pos);
                while (text.IsWhitespace(pos));
                return EmptyAST.Instance;
            }

            switch (text[pos])
            {
                case '\\':
                    if (pos + 1 >= text.Length)
                        return OneCharAST.Make(ReadCharacter(text, opts, ref pos), foldCase);
                    else if (Char.IsDigit(text, pos + 1))
                    {
                        pos++;
                        int end = pos;
                        RegexAST capture = BackRefAST.Make(pos, ReadNumber(text, ref end), foldCase);
                        pos = end;
                        return capture;
                    }
                    else if (text[pos + 1] == '^' || text[pos + 1] == '$')
                    {
                        pos++;
                        return OneCharAST.Make(ReadCharacter(text, opts, ref pos), foldCase);
                    }
                    else
                    {
                        InstructAST ast = Property(text, opts, ref pos);
                        if (ast != null)
                            return ast;
                        else if (text[pos + 1] == 'g')
                        {
                            RegexAST capture;
                            pos += 2;
                            int end = pos;
                            if (end < text.Length && Char.IsDigit(text, end))
                                capture = BackRefAST.Make(pos, ReadNumber(text, ref end), foldCase);
                            else if (end < text.Length && text[end] == '{')
                                capture = BackRefAST.Make(pos, ReadBracketedName(text, ref end), foldCase);
                            else
                                throw new RegexParseException(text, end, "backreference requires a capture ID");
                            pos = end;
                            return capture;
                        }
                        else if (text[pos + 1] == 'k')
                        {
                            pos += 2;
                            return new CaptureCloseAST(null);
                        }
                        else if (text[pos + 1] == 'K')
                        {
                            pos += 2;
                            return new CaptureOpenAST(null);
                        }
                        else
                            return OneCharAST.Make(ReadCharacter(text, opts, ref pos), foldCase);
                    }

                case '#':
                    if (freeSpace)
                    {
                        do
                            text.NextCodepoint(ref pos);
                        while (pos < text.Length && !text.IsNewline(pos));
                        return EmptyAST.Instance;
                    }
                    else goto default;

                case '[': return CharClass(text, opts, ref pos);

                case '(': return Group(text, ref opts, inComp, ref pos);

                case '.':
                    pos++;
                    if (dotAll)
                        return AnyCharAST.Instance;
                    else
                        return NotTestAST.Make(PropertyAST.Make(Unicode7.Property.Newline));

                default: return OneCharAST.Make(ReadCharacter(text, opts, ref pos), foldCase);
            }
        }

        private static InstructAST CharClass(string text, RegexOptions opts, ref int pos)
        {
            Debug.Assert(pos < text.Length && text[pos] == '[', "Expected character class");
            pos++;
            bool negated = pos < text.Length && text[pos] == '^';
            if (negated) pos++;

            InstructAST range = CharExpr(text, opts, ref pos, 1);
            pos++;
            return negated ? NotTestAST.Make(range) : range;
        }

        private static InstructAST CharExpr(string text, RegexOptions opts, ref int pos, byte minPrec)
        {
            InstructAST range = Range(text, opts, ref pos);
            if (range != null)
            {
                InstructAST nextRange = Range(text, opts, ref pos);
                while (nextRange != null)
                {
                    range = OrTestAST.Make(range, nextRange);
                    nextRange = Range(text, opts, ref pos);
                }
                char nextOp = (pos < text.Length) ? text[pos] : ']';
                byte prec;
                switch (nextOp)
                {
                    case '-': prec = 3; break;
                    case '&': prec = 2; break;
                    case '|': prec = 1; break;
                    default: prec = 0; break;
                }
                while (prec >= minPrec)
                {
                    pos += 2;
                    prec++;
                    nextRange = CharExpr(text, opts, ref pos, prec);
                    switch (nextOp)
                    {
                        case '-': range = DiffTestAST.Make(range, nextRange); break;
                        case '&': range = AndTestAST.Make(range, nextRange); break;
                        case '|': range = OrTestAST.Make(range, nextRange); break;
                        default: // this should never be reached
                            break;
                    }
                    nextOp = (pos < text.Length) ? text[pos] : ']';
                    switch (nextOp)
                    {
                        case '-': prec = 3; break;
                        case '&': prec = 2; break;
                        case '|': prec = 1; break;
                        default: prec = 0; break;
                    }
                }
            }

            if (pos >= text.Length)
                throw new RegexParseException(text, pos, "unterminated character class");
            else if (range == null)
            {
                if (text[pos] != ']')
                    throw new RegexParseException(text, pos, String.Format("character class part expected before \"{0}{0}\"", text[pos]));
                else if (text[pos - 1] == '[')
                    throw new RegexParseException(text, pos, "character class may not be empty");
                else
                    throw new RegexParseException(text, pos, String.Format("character class part expected after \"{0}{0}\"", text[pos - 1]));
            }
            return range;
        }

        private static InstructAST Range(string text, RegexOptions opts, ref int pos)
        {
            bool foldCase = (opts & RegexOptions.IgnoreCase) != 0;
            int min = 0, max = 0;
            InstructAST range = null;
            if (pos >= text.Length || text[pos] == ']' || (text[pos] == '-' || text[pos] == '&' || text[pos] == '|')
                && pos + 1 < text.Length && text[pos + 1] == text[pos])
                return null;
            else if (text[pos] == '\\')
            {
                if (pos + 1 < text.Length && text[pos + 1] == 'b')
                {
                    min = 0x8; // \b is backspace in character class
                    pos += 2;
                }
                else
                {
                    range = Property(text, opts, ref pos);
                    if (range == null)
                        min = ReadCharacter(text, opts, ref pos);
                }
            }
            else if (text[pos] == '[')
                range = CharClass(text, opts, ref pos);
            else
                min = ReadCharacter(text, opts, ref pos);

            if (range == null)
            {
                if (!(pos < text.Length && text[pos] == '-'))
                    range = OneCharAST.Make(min, foldCase);
                else if (pos + 1 < text.Length && text[pos + 1] == '-')
                    range = OneCharAST.Make(min, foldCase);
                else
                {
                    pos++;
                    if (pos >= text.Length || text[pos] == '[' || text[pos] == ']' ||
                        (text[pos] == '&' || text[pos] == '|') && pos + 1 < text.Length && text[pos + 1] == text[pos])
                        throw new RegexParseException(text, pos, "unterminated character range");
                    else
                        max = ReadCharacter(text, opts, ref pos);

                    if (max < min) throw new RegexParseException(text, pos,
                        String.Format("'{0}' sorts after '{1}'", min.ToUTF16String(), max.ToUTF16String()));
                    range = CharRangeAST.Make(min, max, foldCase);
                }
            }

            return range;
        }

        enum GroupType : short { Normal, Capture, Atomic };

        private static RegexAST Group(string text, ref RegexOptions opts, bool inComp, ref int pos)
        {
            Debug.Assert(pos < text.Length || text[pos] == '(', "Expected group");
            pos++;
            GroupType grType;
            string capName = null;
            switch (text[pos])
            {
                case '+':
                    if (inComp)
                        throw new RegexParseException(text, pos, "capturing group cannot be complemented");
                    else
                    {
                        pos++; grType = GroupType.Capture;
                    }
                    break;

                case '{':
                    if (inComp)
                        throw new RegexParseException(text, pos, "capturing group cannot be complemented");
                    else
                    {
                        grType = GroupType.Capture;
                        capName = ReadBracketedName(text, ref pos);
                    }
                    break;

                case '?':
                    if (pos + 1 >= text.Length)
                        throw new RegexParseException(text, pos + 1, "unterminated group");
                    else switch (text[pos + 1])
                        {
                            case '-':
                            case 'b':
                            case 'i':
                            case 'm':
                            case 's':
                            case 'x': pos++; Mode(text, ref opts, ref pos); return EmptyAST.Instance;

                            case '{':
                            case '(': pos++; return Conditional(text, opts, inComp, ref pos);

                            case '#':
                                pos += 2;
                                while (pos < text.Length && text[pos] != ')') pos++;
                                if (pos >= text.Length)
                                    throw new RegexParseException(text, pos, "unterminated comment");
                                pos++;
                                return EmptyAST.Instance;

                            case '>':
                                if (inComp)
                                    throw new RegexParseException(text, pos, "atomic group cannot be complemented");
                                else
                                {
                                    pos += 2; grType = GroupType.Atomic;
                                }
                                break;

                            default:
                                throw new RegexParseException(text, pos,
                                    String.Format("unrecognized grouping \"{0}\"", text.Substring(pos - 1, 3)));
                        }
                        break;

                default: grType = GroupType.Normal; break;
            }

            RegexAST ast = Expression(text, opts, inComp, ref pos, 1);
            if (pos >= text.Length || text[pos] != ')')
                throw new RegexParseException(text, pos, "unterminated group");
            pos++;

            switch (grType)
            {
                case GroupType.Capture: return CaptureAST.Make(ast, capName, pos);
                case GroupType.Atomic: return AtomicGroupAST.Make(ast);
                default: return ast;
            }
        }

        private static void Mode(string text, ref RegexOptions opts, ref int pos)
        {
            bool done = false;
            RegexOptions on = RegexOptions.None;
            RegexOptions off = RegexOptions.None;
            while (pos < text.Length && !done)
            {
                switch (text[pos])
                {
                    case 'i': pos++; on |= RegexOptions.IgnoreCase; break;
                    case 'm': pos++; on |= RegexOptions.Multiline; break;
                    case 's': pos++; on |= RegexOptions.DotAll; break;
                    case 'x': pos++; on |= RegexOptions.FreeSpacing; break;
                    case 'b': pos++; on |= RegexOptions.SimpleWordBreak; break;
                    default: done = true; break;
                }
            }
            if (pos < text.Length && text[pos] == '-')
            {
                pos++;
                done = false;
                while (pos < text.Length && !done)
                {
                    switch (text[pos])
                    {
                        case 'i': pos++; off |= RegexOptions.IgnoreCase; break;
                        case 'm': pos++; off |= RegexOptions.Multiline; break;
                        case 's': pos++; off |= RegexOptions.DotAll; break;
                        case 'x': pos++; off |= RegexOptions.FreeSpacing; break;
                        case 'b': pos++; off |= RegexOptions.SimpleWordBreak; break;
                        default: done = true; break;
                    }
                }
            }
            if (pos >= text.Length || text[pos] != ')')
                throw new RegexParseException(text, pos, "unterminated mode modifier");

            if (on != RegexOptions.None || off != RegexOptions.None)
                opts = (opts | on) & ~off;
            pos++;
        }

        private static RegexAST Conditional(string text, RegexOptions opts, bool inComp, ref int pos)
        {
            InstructAST ifAst = null;
            if (text[pos] == '{')
                ifAst = CaptureCheckAST.Make(pos, ReadBracketedName(text, ref pos));
            else if (text[pos] == '(')
            {
                pos++;
                if (pos >= text.Length)
                    throw new RegexParseException(text, pos, "unterminated conditional group assertion");
                ifAst = Assertion(text, opts, ref pos);
                if (pos >= text.Length || text[pos] != ')')
                    throw new RegexParseException(text, pos, "unterminated conditional group assertion");
                pos++;
            }
            if (ifAst == null || !ifAst.IsZeroWidth)
                throw new RegexParseException(text, pos, "conditional group must begin with an assertion or capture ID");

            RegexAST thenAst = Sequence(text, opts, inComp, ref pos);
            if (pos >= text.Length || text[pos] != '|')
                throw new RegexParseException(text, pos, "conditional group must contain exactly two branches");
            pos++;
            RegexAST elseAst = Sequence(text, opts, inComp, ref pos);
            if (pos >= text.Length)
                throw new RegexParseException(text, pos, "unterminated conditional group");
            if (text[pos] == '|')
                throw new RegexParseException(text, pos, "conditional group must contain exactly two branches");

            pos++;
            return CondAST.Make(ifAst, thenAst, elseAst);
        }

        private static InstructAST Property(string text, RegexOptions opts, ref int pos)
        {
            Debug.Assert(pos < text.Length && text[pos] == '\\', "expected property");

            bool negated;
            if (pos + 1 >= text.Length)
                return null;
            else switch (text[pos + 1])
                {
                    case 'N': pos += 2; return PropertyAST.Make(Unicode7.Property.Newline);
                    case 'd': pos += 2; return CategoryAST.Make(UnicodeCategory.DecimalDigitNumber);
                    case 's': pos += 2; return PropertyAST.Make(Unicode7.Property.Space);
                    case 'w': pos += 2; return PropertyAST.Make(Unicode7.Property.Word);

                    case 'D':
                        pos += 2;
                        return NotTestAST.Make(CategoryAST.Make(UnicodeCategory.DecimalDigitNumber));

                    case 'S':
                        pos += 2;
                        return NotTestAST.Make(PropertyAST.Make(Unicode7.Property.Space));

                    case 'W':
                        pos += 2;
                        return NotTestAST.Make(PropertyAST.Make(Unicode7.Property.Word));

                    case 'p': pos += 2; negated = false; break;
                    case 'P': pos += 2; negated = true; break;

                    default: return null;
                }

            InstructAST property;
            if (pos >= text.Length || text[pos] != '{')
                throw new RegexParseException(text, pos, "bracketed Unicode property name expected");

            pos++;
            int end = pos;
            while (text.IsWordCharacter(end))
                text.NextCodepoint(ref end);
            if (end >= text.Length || !(text[end] == '}' || text[end] == '='))
                throw new RegexParseException(text, end, "unterminated Unicode property");
            else if (end == pos)
                throw new RegexParseException(text, end, "Unicode property name must not be empty");

            string propName = text.Substring(pos, end - pos);
            string propValue = "";
            if (text[end] == '=')
            {
                pos = end + 1;
                end = pos;
                while (text.IsWordCharacter(end))
                    text.NextCodepoint(ref end);
                if (end >= text.Length || text[end] != '}')
                    throw new RegexParseException(text, end, "unterminated Unicode property");
                else if (end == pos)
                    throw new RegexParseException(text, end, "Unicode property value must not be empty");
                propValue = text.Substring(pos, end - pos);
            }

            if (propName.ToCanonPropertyValue() == "ascii")
            {
                property = CharRangeAST.Make(0, 0x7F, (opts & RegexOptions.IgnoreCase) != 0);
            }
            else if (propName.ToCanonPropertyValue() == "blk" || propName.ToCanonPropertyValue() == "block")
            {
                Unicode7.Range? block = Unicode7.FindBlock(propValue);
                if (block == null)
                    throw new RegexParseException(text, end,
                        String.Format("unrecognized block name \"{0}\"", propValue));

                property = CharRangeAST.Make(block.Value.Min, block.Value.Max, (opts & RegexOptions.IgnoreCase) != 0);
            }
            else
            {
                UnicodeCategory? cat = Unicode7.CategoryName(propName);
                Unicode7.Script? script = Unicode7.ScriptName(propName);
                Unicode7.Property? propCode = Unicode7.PropertyName(propName);

                if (propValue == "" && cat != null)
                    property = CategoryAST.Make(cat.Value);
                else if (propValue == "" && script != null)
                    property = ScriptAST.Make(script.Value, false);
                else if (propCode == null)
                    throw new RegexParseException(text, end,
                        String.Format("unrecognized Unicode property name \"{0}\"", propName));
                else switch (propCode.Value)
                    {
                        case Unicode7.Property.AnyChar: property = AnyCharAST.Instance; break;

                        case Unicode7.Property.Newline:
                        case Unicode7.Property.Space:
                        case Unicode7.Property.Graph:
                        case Unicode7.Property.Word:
                        case Unicode7.Property.Alpha:
                        case Unicode7.Property.Upper:
                        case Unicode7.Property.Lower:
                        case Unicode7.Property.Ignorable:
                        case Unicode7.Property.NonChar:
                        case Unicode7.Property.Letter:
                        case Unicode7.Property.Cased:
                        case Unicode7.Property.Mark:
                        case Unicode7.Property.Separator:
                        case Unicode7.Property.Symbol:
                        case Unicode7.Property.Number:
                        case Unicode7.Property.Punctuation:
                        case Unicode7.Property.Other: property = PropertyAST.Make(propCode.Value); break;

                        case Unicode7.Property.GenCategory:
                            cat = Unicode7.CategoryName(propValue);
                            if (cat == null)
                                throw new RegexParseException(text, end,
                                    String.Format("unrecognized value \"{0}\" for Gc property", propValue));
                            else
                                property = CategoryAST.Make(cat.Value);
                            break;

                        case Unicode7.Property.Script:
                            script = Unicode7.ScriptName(propValue);
                            if (script == null)
                                throw new RegexParseException(text, end,
                                    String.Format("unrecognized value \"{0}\" for Sc property", propValue));
                            else
                                property = ScriptAST.Make(script.Value, false);
                            break;

                        case Unicode7.Property.ScriptExtension:
                            script = Unicode7.ScriptName(propValue);
                            if (script == null)
                                throw new RegexParseException(text, end,
                                    String.Format("unrecognized value \"{0}\" for Scx property", propValue));
                            else
                                property = ScriptAST.Make(script.Value, true);
                            break;

                        default: return null;
                    }
            }
            pos = end + 1;
            return negated ? NotTestAST.Make(property) : property;
        }

        private static int ReadCharacter(string text, RegexOptions opts, ref int pos)
        {
            int cp;
            if (text[pos] == '\\')
            {
                if (pos + 1 >= text.Length)
                    throw new RegexParseException(text, pos + 1, "incomplete escape sequence");
                else if ((opts & RegexOptions.FreeSpacing) != 0 && text.IsWhitespace(pos + 1))
                {
                    cp = text.ToCodepoint(pos + 1);
                    pos += 2;
                }
                else switch (text[pos + 1])
                {
                    case 'a': pos += 2; cp = 0x7; break;
                    case 't': pos += 2; cp = 0x9; break;
                    case 'n': pos += 2; cp = 0xA; break;
                    case 'v': pos += 2; cp = 0xB; break;
                    case 'f': pos += 2; cp = 0xC; break;
                    case 'r': pos += 2; cp = 0xD; break;
                    case 'e': pos += 2; cp = 0x1B; break;

                    case 'x': pos += 2; cp = ReadHex(text, ref pos); break;

                    case 'c': if (pos + 2 < text.Length)
                        {
                            if (text[pos + 2] >= 'A' && text[pos + 2] <= 'Z')
                            {
                                cp = text[pos + 2] - 'A'; pos += 3; 
                            }
                            else if (text[pos + 2] >= 'a' && text[pos + 2] <= 'z')
                            {
                                cp = text[pos + 2] - 'a'; pos += 3; 
                            }
                            else
                                throw new RegexParseException(text, pos + 2, "unrecognized control character sequence");
                        }
                        else
                            throw new RegexParseException(text, pos + 2, "incomplete control character sequence");
                        break;

                    case '\\':
                    case '.':
                    case '?':
                    case '*':
                    case '+':
                    case '|':
                    case '&':
                    case '~':
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                    case '{':
                    case '}': cp = text.ToCodepoint(pos + 1); pos += 2; break;

                    default:
                        throw new RegexParseException(text, pos + 1,
                            String.Format("unrecognized metacharacter '\\{0}'", text[pos + 1]));
                }
            }
            else
            {
                cp = text.ToCodepoint(pos);
                text.NextCodepoint(ref pos);
            }
            return cp;
        }

        internal static string ReadBracketedName(string text, ref int pos)
        {
            Debug.Assert(pos < text.Length || text[pos] == '{', "bracketed name expected");

            pos++;
            int end = pos;
            while (text.IsWordCharacter(end))
                text.NextCodepoint(ref end);
            if (end == pos)
                throw new RegexParseException(text, end, "bracketed name may not be empty");

            if (end >= text.Length)
                throw new RegexParseException(text, end, "unterminated bracketed name");
            else if (text[end] != '}')
                throw new RegexParseException(text, end, "bracketed name must contain only word characters");

            string name = text.Substring(pos, end - pos);
            pos = end + 1;
            return name;
        }

        private static int ReadNumber(string text, ref int pos)
        {
            int end = pos;
            while (end < text.Length &&
                Char.ConvertToUtf32(text, end).GetUnicodeCategory() == UnicodeCategory.DecimalDigitNumber)
                text.NextCodepoint(ref end);
            if (end == pos)
                throw new RegexParseException(text, end, "decimal number expected");

            string numeral = text.Substring(pos, end - pos);
            pos = end;
            return int.Parse(numeral);
        }

        private static int ReadHex(string text, ref int pos)
        {
            if (pos >= text.Length || text[pos] != '{')
                throw new RegexParseException(text, pos, "bracketed hexadecimal expected");

            pos++;
            int cp = 0, len = 0;
            while (pos < text.Length && len < 6)
            {
                int hex = HexValue(text[pos]);
                if (hex < 0) break;
                cp *= 16;
                cp += hex;
                len++;
                pos++;
            }

            if (len == 0)
                throw new RegexParseException(text, pos, "hexadecimal must not be empty");
            if (pos >= text.Length || text[pos] != '}')
                throw new RegexParseException(text, pos, "unterminated bracketed hexadecimal");
            pos++;

            return cp;
        }

        private static int HexValue(char ch)
        {
            if (ch >= 'a' && ch <= 'f')
                return ch - 'a' + 10;
            else if (ch >= 'A' && ch <= 'F')
                return ch - 'A' + 10;
            else if (ch >= '0' && ch <= '9')
                return ch - '0';
            else
                return -1;
        }

#if DEBUG
        internal static string EscapeChar(int codepoint, bool inCharClass)
        {
            if (codepoint > Char.MaxValue)
                return "\\x{" + String.Format("{0:x}", codepoint) + "}";
            else switch (codepoint.GetUnicodeCategory())
                {
                    case UnicodeCategory.LineSeparator:
                    case UnicodeCategory.ParagraphSeparator:
                    case UnicodeCategory.Control:
                    case UnicodeCategory.Format:
                    case UnicodeCategory.Surrogate:
                    case UnicodeCategory.PrivateUse:
                    case UnicodeCategory.OtherNotAssigned:
                        return "\\x{" + String.Format("{0:x}", codepoint) + "}";

                    default: switch (codepoint)
                        {
                            case '.':
                            case '|':
                            case '^':
                            case '$':
                            case '?':
                            case '*':
                            case '+':
                            case '(':
                            case ')':
                                if (inCharClass)
                                    return codepoint.ToUTF16String();
                                else
                                    return "\\" + codepoint.ToUTF16String();

                            case '\\':
                            case '[':
                            case ']':
                            case '{':
                            case '}': return "\\" + codepoint.ToUTF16String();

                            default: return codepoint.ToUTF16String();
                        }
                }
        }
#endif
    }

    /// <summary>The exception thrown when a string is an ill-formed regular expression.</summary>
    [Serializable]
    public class RegexParseException : ArgumentException
    {
        /// <summary>Constructor.</summary>
        /// <param name="expr">The regular expression being parsed.</param>
        /// <param name="pos">The pos of the error in <paramref name="expr"/>.</param>
        /// <param name="msg">A description of the parser error.</param>
        public RegexParseException(string expr, int pos, string msg)
            : base(String.Format("Error in /{0}/ at position {1:d}: {2}", expr, pos, msg)) { }
    }
}
