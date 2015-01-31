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
using NUnit.Framework;

namespace Brasswork.Regex.Test
{
    /// <summary>Unit tests for regex syntax identities: expressions that should compile to the same NFA.</summary>
    [TestFixture]
    class SyntaxIdentities
    {
        [TestCase(@"[aa]", @"a", RegexOptions.None,
            Description = @"Repeated character in set")]
        [TestCase(@"[a||a]", @"a", RegexOptions.None,
            Description = @"Repeated character in ||")]
        [TestCase(@"[a&&a]", @"a", RegexOptions.None,
            Description = @"Repeated character in &&")]
        [TestCase(@"[a--a]", @"\P{Any}", RegexOptions.None,
            Description = @"Repeated character in --")]
        [TestCase(@"[A-ZM][na-z]", @"[A-Z][a-z]", RegexOptions.None,
            Description = @"Characters in ranges")]
        [TestCase(@"[A-YZ][ab-z][ZA-Y][b-za]", @"[A-Z][a-z][A-Z][a-z]", RegexOptions.None,
            Description = @"Characters next to ranges")]
        [TestCase(@"[A-RH-Z][h-za-r][0-93-6][H-RA-Z][a-mn-z][5-90-4]",
            @"[A-Z][a-z][0-9][A-Z][a-z][0-9]", RegexOptions.None,
            Description = @"Overlapping ranges")]
        [TestCase(@"[A-ZM][na-z]", @"[A-Z][a-z]", RegexOptions.IgnoreCase,
            Description = @"Characters in ranges, case insensitive")]
        [TestCase(@"[A-YZ][ab-z][ZA-Y][b-za]", @"[A-Z][a-z][A-Z][a-z]", RegexOptions.IgnoreCase,
            Description = @"Characters next to ranges, case insensitive")]
        [TestCase(@"[A-RH-Z][h-za-r][0-93-6][H-RA-Z][a-mn-z][5-90-4]",
            @"[A-Z][a-z][0-9][A-Z][a-z][0-9]", RegexOptions.IgnoreCase,
            Description = @"Overlapping ranges, case insensitive")]
        [TestCase(@"[\p{Any}a][a\p{Any}]", @"\p{Any}\p{Any}", RegexOptions.None,
            Description = @"Union with \p{Any}")]
        [TestCase(@"[\p{Any}&&a][a&&\p{Any}]", @"aa", RegexOptions.None,
            Description = @"Intersection with \p{Any}")]
        [TestCase(@"[a--\p{Any}]", @"\P{Any}", RegexOptions.None,
            Description = @"Subtract \p{Any}")]
        [TestCase(@"[\p{Any}--a]", @"[^a]", RegexOptions.None,
            Description = @"Subtract from \p{Any}")]
        [TestCase(@"[^[^A-Z]]", @"[A-Z]", RegexOptions.None,
            Description = @"Not(not(A)) == A")]
        [TestCase(@"[[^A-R][^H-Z]][[^A-R][H-Z]][[A-R][^H-Z]]", @"[^H-R][^A-G][^S-Z]", RegexOptions.None,
            Description = @"Or(not(A)) identities")]
        [TestCase(@"[[^A-R]&&[^H-Z]][[^A-R]&&[H-Z]][[A-R]&&[^H-Z]]", @"[^A-Z][S-Z][A-G]", RegexOptions.None,
            Description = @"And(not(A)) identities")]
        [TestCase(@"[[^A-R]--[^H-Z]][[^A-R]--[H-Z]][[A-R]--[^H-Z]]", @"[S-Z][^A-Z][H-R]", RegexOptions.None,
            Description = @"Diff(not(A)) identities")]
        [TestCase(@"[\d||\p{Upper}--\p{Lu}&&\p{sc=Latin}]",
            @"[\p{Nd}[[\p{Upper}--\p{Lu}]&&\p{sc=Latn}]]", RegexOptions.None,
            Description = @"Char class operator precedence")]
        [TestCase(@"[[\p{Lu}&&\p{sc=Latin}]&&\p{Ascii}]",
            @"[\p{Lu}&&\p{sc=Latin}&&\p{Ascii}]", RegexOptions.None,
            Description = @"Set intersection is associative")]
        [TestCase(@"[\p{Lu}--\p{sc=Latin}--\p{sc=Greek}]",
            @"[\p{Lu}--[\p{sc=Latin}\p{sc=Greek}]]", RegexOptions.None,
            Description = @"Repeated set difference")]
        [TestCase(@"[\p{L}&&[\p{Ascii}||\p{Upper}]]", @"[\p{L}&&\p{Ascii}||\p{L}&&\p{Upper}]", RegexOptions.None,
            Description = @"[a&&[b||c]] == [a&&b||a&&c]")]
        [TestCase(@"[[\p{Ascii}||\p{Upper}]&&\p{L}]", @"[\p{Ascii}&&\p{L}||\p{Upper}&&\p{L}]", RegexOptions.None,
            Description = @"[[a||b]&&c] == [a&&c||b&&c]")]
        [TestCase(@"[[\p{Ascii}||\p{Upper}]--\p{L}]", @"[\p{Ascii}--\p{L}||\p{Upper}--\p{L}]", RegexOptions.None,
            Description = @"[[a||b]&&c] == [a&&c||b&&c]")]
        [TestCase(@"[\p{Alpha}||\p{Upper}]", @"\p{Alpha}", RegexOptions.None,
            Description = @"A||B = A if A contains B")]
        [TestCase(@"[\p{Upper}||\p{Alpha}]", @"\p{Alpha}", RegexOptions.None,
            Description = @"A||B = B if B contains A")]
        [TestCase(@"[\p{Alpha}&&\p{Upper}]", @"\p{Upper}", RegexOptions.None,
            Description = @"A&&B = B if A contains B")]
        [TestCase(@"[\p{Upper}&&\p{Alpha}]", @"\p{Upper}", RegexOptions.None,
            Description = @"A&&B = A if B contains A")]
        [TestCase(@"[\p{Upper}--\p{Alpha}]", @"\P{Any}", RegexOptions.None,
            Description = @"A--B matches nothing if B contains A")]
        [TestCase(@"\b", @"\b{W}", RegexOptions.None,
            Description = @"\b is a default word boundary")]
        [TestCase(@"\b", @"\b{w}", RegexOptions.SimpleWordBreak,
            Description = @"\b is a simple word boundary in mode /b")]
        [TestCase(@"^", @"\b{A}", RegexOptions.None,
            Description = @"^ is start of text")]
        [TestCase(@"^", @"\b{a}", RegexOptions.Multiline,
            Description = @"^ is start of line in mode /m")]
        [TestCase(@"$", @"\b{Z}", RegexOptions.None,
            Description = @"$ is end of text")]
        [TestCase(@"$", @"\b{z}", RegexOptions.Multiline,
            Description = @"$ is end of line in mode /m")]
        [TestCase(@"\B", @"\B{W}", RegexOptions.None,
            Description = @"\B is a default non-word boundary")]
        [TestCase(@"\B", @"\B{w}", RegexOptions.SimpleWordBreak,
            Description = @"\b is a simple non-word boundary in mode /b")]
        [TestCase(@"a|\P{Any}", @"a", RegexOptions.None,
            Description = @"a|\P{Any} simplifies to a")]
        [TestCase(@"\P{Any}|a", @"a", RegexOptions.None,
            Description = @"\P{Any}|a simplifies to a")]
        [TestCase(@"a|\p{Any}*", @"\p{Any}*", RegexOptions.None,
            Description = @"a|\p{Any}* == \p{Any}*")]
        [TestCase(@"\p{Any}*|a", @"\p{Any}*", RegexOptions.None,
            Description = @"\p{Any}*|a == \p{Any}*")]
        [TestCase(@"a|a", @"a", RegexOptions.None,
            Description = @"a|a simplifies to a")]
        [TestCase(@"(a|b)|(c|d)", @"a|b|c|d", RegexOptions.None,
            Description = @"Alternation is associative")]
        [TestCase(@"ab|ac", @"a(b|c)", RegexOptions.None,
            Description = @"Alternation extracts common factors")]
        [TestCase(@"(ab|bc)|ad", @"a(b|d)|bc", RegexOptions.None,
            Description = @"Alternation extracts common factors from left alternatives")]
        [TestCase(@"ab|bc|ad", @"bc|a(b|d)", RegexOptions.None,
            Description = @"Alternation extracts common factors from right alternatives")]
        [TestCase(@"\P{Any}&a", @"\P{Any}", RegexOptions.None,
            Description = @"\P{Any}&a simplifies to \P{Any}")]
        [TestCase(@"a&\P{Any}", @"\P{Any}", RegexOptions.None,
            Description = @"a&\P{Any} simplifies to \P{Any}")]
        [TestCase(@"a&a", @"a", RegexOptions.None,
            Description = @"a&a simplifies to a")]
        [TestCase(@"a&", @"\P{Any}", RegexOptions.None,
            Description = @"Intersecting non-empty with empty matches nothing")]
        [TestCase(@"a&b", @"\P{Any}", RegexOptions.None,
            Description = @"Intersection of different codepoints matches nothing")]
        [TestCase(@"\p{L}&(\p{Ascii}|\p{Upper})", @"\p{L}&\p{Ascii}|\p{L}&\p{Upper}", RegexOptions.None,
            Description = @"a&(b|c) == a&b|a&c")]
        [TestCase(@"(\p{Ascii}|\p{Upper})&\p{L}", @"\p{Ascii}&\p{L}|\p{Upper}&\p{L}", RegexOptions.None,
            Description = @"(a|b)&c == a&c|b&c")]
        [TestCase(@"~(~a)", @"a", RegexOptions.None,
            Description = @"~(~a) == a")]
        [TestCase(@"~\P{Any}", @"\p{Any}*", RegexOptions.None,
            Description = @"~\P{Any} == \p{Any}*")]
        [TestCase(@"~\p{Any}*", @"\P{Any}", RegexOptions.None,
            Description = @"~\p{Any}* == \P{Any}")]
        [TestCase(@"~", @"\p{Any}+", RegexOptions.None,
            Description = @"Complement of empty")]
        [TestCase(@"~a|~b", @"~(a&b)", RegexOptions.None,
            Description = @"~a|~b == ~(a&b)")]
        [TestCase(@"~a&~b", @"~(a|b)", RegexOptions.None,
            Description = @"~a&~b == ~(a|b)")]
        [TestCase(@"(ab)c", @"abc", RegexOptions.None,
            Description = @"Sequence is associative")]
        [TestCase(@"a?", @"a{0,1}", RegexOptions.None,
            Description = "a? == a{0,1}")]
        [TestCase(@"a*", @"a{0,}", RegexOptions.None,
            Description = "a* == a{0,}")]
        [TestCase(@"a+", @"a{1,}", RegexOptions.None,
            Description = "a+ == a{1,}")]
        [TestCase(@"\P{Any}*", @"", RegexOptions.None,
            Description = @"\P{Any}* == empty regex")]
        [TestCase(@"\P{Any}+", @"\P{Any}", RegexOptions.None,
            Description = @"\P{Any}+ == \P{Any}")]
        [TestCase(@"a{0}", @"", RegexOptions.None,
            Description = "Repeat no times == empty regex")]
        [TestCase(@"a{1}", @"a", RegexOptions.None,
            Description = "Repeat a once == just a")]
        [TestCase(@"a|", @"a{0,1}", RegexOptions.None,
            Description = "(a|) == a{0,1}")]
        [TestCase(@"|a", @"a{0,1}?", RegexOptions.None,
            Description = "(|a) == a{0,1}?")]
        [TestCase(@"(){3}", @"", RegexOptions.None,
            Description = "Quantified empty regex is still empty")]
        [TestCase(@"\b*", @"", RegexOptions.None,
            Description = "* over zero-width")]
        [TestCase(@"\b+", @"\b", RegexOptions.None,
            Description = "+ over zero-width")]
        [TestCase(@"\b{2,3}", @"\b", RegexOptions.None,
            Description = "Quantifier over zero-width")]
        [TestCase(@"a{2}{3}", @"a{6}", RegexOptions.None,
            Description = "Repeat of repeat")]
        [TestCase(@"a**", @"a*", RegexOptions.None,
            Description = "a** == a*")]
        [TestCase(@"a*{3}", @"a*", RegexOptions.None,
            Description = "a* under any other quantifier == a* alone")]
        [TestCase(@"()", @"", RegexOptions.None,
            Description = "Empty group is empty regex")]
        [TestCase(@"(a)", @"a", RegexOptions.None,
            Description = "Redundant groups are removed")]
        [TestCase(@"()a", @"a", RegexOptions.None,
            Description = "Empty regex is left identity for sequence")]
        [TestCase(@"a()", @"a", RegexOptions.None,
            Description = "Empty regex is right identity for sequence")]
        [TestCase(@"(+\P{Any})", @"\P{Any}", RegexOptions.None,
            Description = @"(+\P{Any}) == \P{Any}")]
        [TestCase(@"(?>)", @"", RegexOptions.None,
            Description = "Atomic around empty vanishes")]
        [TestCase(@"(?>\P{Any})", @"\P{Any}", RegexOptions.None,
            Description = @"(?>\P{Any}) == \P{Any}")]
        [TestCase(@"(?>(?>a))", @"(?>a)", RegexOptions.None,
            Description = "Nested atomic groups")]
        [TestCase(@"(?>(+a))", @"(+(?>a))", RegexOptions.None,
            Description = "Atomic around capture == capture around atomic")]
        public void EqualExpressions(string left, string right, RegexOptions opts)
        {
            RegexAST leftAST = Parser.Parse(left, opts);
            RegexAST rightAST = Parser.Parse(right, opts);
            Assert.True(leftAST.Equals(rightAST));
        }
    }
}
