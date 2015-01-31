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
using System.Linq;
using NUnit.Framework;

namespace Brasswork.Regex.Test
{
    /// <summary>Unit tests for character classes.</summary>
    [TestFixture]
    class CharacterClassTests
    {
        [TestCase(@"\", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\/ at position 1: incomplete escape sequence",
            Description = @"Unescaped backslash")]
        [TestCase(@"\m", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\m/ at position 1: unrecognized metacharacter '\m'",
            Description = @"Unrecognized metacharacter")]
        [TestCase(@"\x", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\x/ at position 2: bracketed hexadecimal expected",
            Description = @"Missing hex number in \x")]
        [TestCase(@"\x{4f", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\x{4f/ at position 5: unterminated bracketed hexadecimal",
            Description = @"Open hex number in \x")]
        [TestCase(@"\x{}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\x{}/ at position 3: hexadecimal must not be empty",
            Description = @"Empty hex number in \x")]
        [TestCase(@"\c", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\c/ at position 2: incomplete control character sequence",
            Description = @"Incomplete control character")]
        [TestCase(@"\c ", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\c / at position 2: unrecognized control character sequence",
            Description = @"Incomplete control character")]
        [TestCase(@"ab)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /ab)/ at position 2: ')' must be escaped",
            Description = @"Unmatched )")]
        [TestCase(@"ab]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /ab]/ at position 2: ']' must be escaped",
            Description = @"Unmatched ]")]
        [TestCase(@"ab}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /ab}/ at position 2: '}' must be escaped",
            Description = @"Unmatched }")]
        [TestCase(@"*a", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /*a/ at position 0: '*' must be escaped",
            Description = @"* with nothing to modify")]
        [TestCase(@"+a", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /+a/ at position 0: '+' must be escaped",
            Description = @"+ with nothing to modify")]
        [TestCase(@"?a", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /?a/ at position 0: '?' must be escaped",
            Description = @"? with nothing to modify")]
        public void OneCharacterError(string pattern, RegexOptions opts)
        {
            Regex re = new Regex(pattern, opts);
        }

        [TestCase(@"a", RegexOptions.None, "a", Result = true,
            Description = "Basic character")]
        [TestCase(@"^ÿ", RegexOptions.None, "\xff", Result = true,
            Description = "Eccentric character")]
        [TestCase(@"\x{1D6B7}", RegexOptions.None, "\U0001D6B7", Result = true,
            Description = "Surrogate pair")]
        [TestCase(@"\cc", RegexOptions.None, "\x2", Result = true,
            Description = @"Control-C")]
        [TestCase(@"\cV", RegexOptions.None, "\x15", Result = true,
            Description = @"Control-V")]
        [TestCase(@"\a", RegexOptions.None, "\x7", Result = true,
            Description = @"Metacharacter \a")]
        [TestCase(@"\e", RegexOptions.None, "\x1b", Result = true,
            Description = @"Metacharacter \e")]
        [TestCase(@"\f", RegexOptions.None, "\xc", Result = true,
            Description = @"Metacharacter \f")]
        [TestCase(@"\n", RegexOptions.None, "\xa", Result = true,
            Description = @"Metacharacter \n")]
        [TestCase(@"\r", RegexOptions.None, "\xd", Result = true,
            Description = @"Metacharacter \n")]
        [TestCase(@"\t", RegexOptions.None, "\x9", Result = true,
            Description = @"Metacharacter \t")]
        [TestCase(@"\v", RegexOptions.None, "\xb", Result = true,
            Description = @"Metacharacter \v")]
        [TestCase(@"\\", RegexOptions.None, "\\", Result = true,
            Description = @"Escaped \")]
        [TestCase(@"\.", RegexOptions.None, ".", Result = true,
            Description = @"Escaped .")]
        [TestCase(@"\^", RegexOptions.None, "^", Result = true,
            Description = @"Escaped ^")]
        [TestCase(@"\$", RegexOptions.None, "$", Result = true,
            Description = @"Escaped $")]
        [TestCase(@"\|", RegexOptions.None, "|", Result = true,
            Description = @"Escaped |")]
        [TestCase(@"\&", RegexOptions.None, "&", Result = true,
            Description = @"Escaped &")]
        [TestCase(@"\~", RegexOptions.None, "~", Result = true,
            Description = @"Escaped &")]
        [TestCase(@"\?", RegexOptions.None, "?", Result = true,
            Description = @"Escaped ?")]
        [TestCase(@"\*", RegexOptions.None, "*", Result = true,
            Description = @"Escaped *")]
        [TestCase(@"\+", RegexOptions.None, "+", Result = true,
            Description = @"Escaped +")]
        [TestCase(@"\(", RegexOptions.None, "(", Result = true,
            Description = @"Escaped (")]
        [TestCase(@"\)", RegexOptions.None, ")", Result = true,
            Description = @"Escaped )")]
        [TestCase(@"\[", RegexOptions.None, "[", Result = true,
            Description = @"Escaped [")]
        [TestCase(@"\]", RegexOptions.None, "]", Result = true,
            Description = @"Escaped ]")]
        [TestCase(@"\{", RegexOptions.None, "{", Result = true,
            Description = @"Escaped {")]
        [TestCase(@"\}", RegexOptions.None, "}", Result = true,
            Description = @"Escaped }")]
        public bool OneCharacter(string pattern, RegexOptions opts, string text)
        {
            Regex re = new Regex(pattern, opts);
            return re.Match(text).IsSuccess;
        }

        [Test]
        public void CharacterSet()
        {
            Regex re = new Regex(@"^[ab\]cde]");
            Assert.That(re.Match("athing").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("bthing").ToString(), Is.EqualTo("b"));
            Assert.That(re.Match("cthing").ToString(), Is.EqualTo("c"));
            Assert.That(re.Match("dthing").ToString(), Is.EqualTo("d"));
            Assert.That(re.Match("ething").ToString(), Is.EqualTo("e"));
            Assert.That(re.Match("]thing").ToString(), Is.EqualTo("]"));
            Assert.False(re.Match("fthing").IsSuccess);
            Assert.False(re.Match("[thing").IsSuccess);
            Assert.False(re.Match("\\thing").IsSuccess);
        }

        [Test]
        public void NegatedCharacterSet()
        {
            Regex re = new Regex(@"^[^ab\]cde]");
            Assert.False(re.Match("athing").IsSuccess);
            Assert.False(re.Match("bthing").IsSuccess);
            Assert.False(re.Match("cthing").IsSuccess);
            Assert.False(re.Match("dthing").IsSuccess);
            Assert.False(re.Match("ething").IsSuccess);
            Assert.False(re.Match("]thing").IsSuccess);
            Assert.That(re.Match("fthing").ToString(), Is.EqualTo("f"));
            Assert.That(re.Match("[thing").ToString(), Is.EqualTo("["));
            Assert.That(re.Match("\\thing").ToString(), Is.EqualTo("\\"));
        }

        [Test]
        public void FindCharacter()
        {
            Regex re = new Regex(":");
            Assert.That(re.Match("Well, we need a colon: somewhere").ToString(), Is.EqualTo(":"));
            Assert.False(re.Match("Fail if we don't").IsSuccess);
        }

        [TestCase(@"[a", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[a/ at position 2: unterminated character class",
            Description = @"Open character class")]
        [TestCase(@"[]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[]/ at position 1: character class may not be empty",
            Description = @"Empty character class")]
        [TestCase(@"[\", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[\/ at position 2: incomplete escape sequence",
            Description = @"Open character class")]
        [TestCase(@"[a-", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[a-/ at position 3: unterminated character range",
            Description = @"Open character range")]
        [TestCase(@"[a-]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[a-]/ at position 3: unterminated character range",
            Description = @"Open character range in closed class")]
        [TestCase(@"[a-&&b]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[a-&&b]/ at position 3: unterminated character range",
            Description = @"Open character range as operand")]
        [TestCase(@"[z-a]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[z-a]/ at position 4: 'z' sorts after 'a'",
            Description = @"Reversed character range")]
        [TestCase(@"[||\p{Lu}]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[||\p{Lu}]/ at position 1: character class part expected before ""||""",
            Description = @"Missing left operand of ||")]
        [TestCase(@"[\p{Lu}||]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[\p{Lu}||]/ at position 9: character class part expected after ""||""",
            Description = @"Missing right operand of ||")]
        [TestCase(@"[&&\p{Lu}]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[&&\p{Lu}]/ at position 1: character class part expected before ""&&""",
            Description = @"Missing left operand of &&")]
        [TestCase(@"[\p{Lu}&&]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[\p{Lu}&&]/ at position 9: character class part expected after ""&&""",
            Description = @"Missing right operand of &&")]
        [TestCase(@"[--\p{Lu}]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[--\p{Lu}]/ at position 1: character class part expected before ""--""",
            Description = @"Missing left operand of --")]
        [TestCase(@"[\p{Lu}--]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[\p{Lu}--]/ at position 9: character class part expected after ""--""",
            Description = @"Missing right operand of --")]
        [TestCase(@"[[\p{Upper}--\p{Lu}][\p{Lower}--\p{Ll}]", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /[[\p{Upper}--\p{Lu}][\p{Lower}--\p{Ll}]/ at position 39: " +
                "unterminated character class",
            Description = @"Open character class with nested classes")]
        public void CharacterClassError(string pattern, RegexOptions opts)
        {
            Regex re = new Regex(pattern, opts);
        }

        [TestCase(@"[a]", RegexOptions.None, "a", Result = true,
            Description = @"One-character class")]
        [TestCase(@"[^a]", RegexOptions.None, "b", Result = true,
            Description = @"Negated character class")]
        [TestCase(@"[\b]", RegexOptions.None, "\x8", Result = true,
            Description = @"Metacharacter \b in character class")]
        [TestCase(@"[\d]", RegexOptions.None, "0", Result = true,
            Description = @"Metacharacter \d in character class")]
        [TestCase(@"[A-Z]", RegexOptions.None, "M", Result = true,
            Description = @"Character range")]
        [TestCase(@"[-|&]", RegexOptions.None, "-", Result = true,
            Description = @"Undoubled operators as single characters")]
        [TestCase(@"[a-|][!-&]", RegexOptions.None, "a!", Result = true,
            Description = @"Undoubled operators as range endpoints")]
        [TestCase(@"^[\w][\W][\s][\S][\d][\D][\b][\n][\x{1d}]", RegexOptions.None, "a+ Z0+\x08\n\x1d", Result = true,
            Description = @"Many metacharacters")]
        [TestCase(@"^[.^$|()*+?{,}~]+", RegexOptions.None, ".^$(*+)|{?,?}~", Result = true,
            Description = @"Match regex operators")]
        public bool CharacterClass(string pattern, RegexOptions opts, string text)
        {
            Regex re = new Regex(pattern, opts);
            return re.Match(text).IsSuccess;
        }

        [Test]
        public void Numbers()
        {
            Regex re = new Regex("^[0-9]+$");
            Assert.That(re.Match("0").ToString(), Is.EqualTo("0"));
            Assert.That(re.Match("1").ToString(), Is.EqualTo("1"));
            Assert.That(re.Match("2").ToString(), Is.EqualTo("2"));
            Assert.That(re.Match("3").ToString(), Is.EqualTo("3"));
            Assert.That(re.Match("4").ToString(), Is.EqualTo("4"));
            Assert.That(re.Match("5").ToString(), Is.EqualTo("5"));
            Assert.That(re.Match("6").ToString(), Is.EqualTo("6"));
            Assert.That(re.Match("7").ToString(), Is.EqualTo("7"));
            Assert.That(re.Match("8").ToString(), Is.EqualTo("8"));
            Assert.That(re.Match("9").ToString(), Is.EqualTo("9"));
            Assert.That(re.Match("10").ToString(), Is.EqualTo("10"));
            Assert.That(re.Match("100").ToString(), Is.EqualTo("100"));
            Assert.False(re.Match("abc").IsSuccess);
        }

        [Test]
        public void Dot()
        {
            Regex re = new Regex("^.*nter");
            Assert.That(re.Match("enter").ToString(), Is.EqualTo("enter"));
            Assert.That(re.Match("inter").ToString(), Is.EqualTo("inter"));
            Assert.That(re.Match("uponter").ToString(), Is.EqualTo("uponter"));
        }

        [Test]
        public void Whitespace()
        {
            Regex re = new Regex("^(+\\d+)\\s+IN\\s+SOA\\s+(+\\S+)\\s+(+\\S+)\\s*\\(\\s*$");
            Assert.That(re.Match("1 IN SOA non-sp1 non-sp2(").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "1 IN SOA non-sp1 non-sp2(", "1", "non-sp1", "non-sp2" }));
            Assert.That(re.Match("1    IN    SOA    non-sp1    non-sp2   (").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "1    IN    SOA    non-sp1    non-sp2   (", "1", "non-sp1", "non-sp2" }));
            Assert.False(re.Match("1IN SOA non-sp1 non-sp2(").IsSuccess);
        }

        [Test]
        public void MixedSet()
        {
            Regex re = new Regex(@"(+[\da-f:]+)$", RegexOptions.IgnoreCase);
            Assert.That(re.Match("0abc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "0abc", "0abc" }));
            Assert.That(re.Match("abc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abc", "abc" }));
            Assert.That(re.Match("fed").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "fed", "fed" }));
            Assert.That(re.Match("E").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "E", "E" }));
            Assert.That(re.Match("::").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "::", "::" }));
            Assert.That(re.Match("5f03:12C0::932e").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "5f03:12C0::932e", "5f03:12C0::932e" }));
            Assert.That(re.Match("fed def").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "def", "def" }));
            Assert.That(re.Match("Any old stuff").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "ff", "ff" }));
            Assert.False(re.Match("0zzz").IsSuccess);
            Assert.False(re.Match("gzzz").IsSuccess);
            Assert.False(re.Match("fed\x20").IsSuccess);
            Assert.False(re.Match("Any old rubbish").IsSuccess);
        }

        [Test]
        public void TwelveDigits([Values(
            @"^[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]", @"^\d\d\d\d\d\d\d\d\d\d\d\d",
            @"^[\d][\d][\d][\d][\d][\d][\d][\d][\d][\d][\d][\d]")]string pattern)
        {
            Regex re = new Regex(pattern);
            Assert.That(re.Match("123456654321").ToString(), Is.EqualTo("123456654321"));
        }

        [Test]
        public void TwelveABCs([Values(@"^[abc]{12}", @"^[a-c]{12}", @"^(a|b|c){12}")]string pattern)
        {
            Regex re = new Regex(pattern);
            Assert.That(re.Match("abcabcabcabc").ToString(), Is.EqualTo("abcabcabcabc"));
        }

        [Test]
        public void UnionCharClass()
        {
            Regex re = new Regex(@"[\p{Lu}||\p{sc=Latin}]");
            Assert.True(re.Match("A").IsSuccess, "Did not match 'A'");
            Assert.True(re.Match("a").IsSuccess, "Did not match 'a'");
            Assert.True(re.Match("\x391").IsSuccess, "Did not match '\x391'");
            Assert.False(re.Match("\x3b1").IsSuccess, "Wrongly matched '\x3b1'");
        }

        [Test]
        public void IntersectCharClass()
        {
            Regex re = new Regex(@"[\p{Lu}&&\p{sc=Latin}]");
            Assert.True(re.Match("A").IsSuccess, "Did not match 'A'");
            Assert.False(re.Match("a").IsSuccess, "Wrongly matched 'a'");
            Assert.False(re.Match("\x391").IsSuccess, "Wrongly matched '\x391'");
            Assert.False(re.Match("\x3b1").IsSuccess, "Wrongly matched '\x3b1'");
        }

        [Test]
        public void SubtractCharClass()
        {
            Regex re = new Regex(@"[\p{Lu}--\p{sc=Latin}]");
            Assert.False(re.Match("A").IsSuccess, "Wrongly matched 'A'");
            Assert.False(re.Match("a").IsSuccess, "Wrongly matched 'a'");
            Assert.True(re.Match("\x391").IsSuccess, "Did not match '\x391'");
            Assert.False(re.Match("\x3b1").IsSuccess, "Wrongly matched '\x3b1'");
        }

        [Test]
        public void NestedCharClasses()
        {
            Regex re = new Regex(@"[[\p{Upper}--\p{Lu}][\p{Lower}--\p{Ll}]]");
            Assert.True(re.Match("\xaa").IsSuccess, "Did not match '\xaa'");
            Assert.True(re.Match("\x2160").IsSuccess, "Did not match '\x2160'");
            Assert.False(re.Match("A").IsSuccess, "Wrongly matched 'A'");
            Assert.False(re.Match("a").IsSuccess, "Wrongly matched 'a'");
        }
    }
}
