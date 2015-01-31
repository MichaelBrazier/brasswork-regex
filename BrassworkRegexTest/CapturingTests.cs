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
    /// <summary>Unit tests involving capture groups and backreferences.</summary>
    [TestFixture]
    class CapturingTests
    {
        [TestCase(@"(?(", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?(/ at position 3: unterminated conditional group assertion",
            Description = @"Missing conditional group assertion")]
        [TestCase(@"(?(1a|b", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?(1a|b/ at position 3: unterminated conditional group assertion",
            Description = @"Open conditional group assertion")]
        [TestCase(@"(?()a|b)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?()a|b)/ at position 4: conditional group must begin with an assertion or capture ID",
            Description = @"If-less conditional")]
        [TestCase(@"(?{1}a)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?{1}a)/ at position 6: conditional group must contain exactly two branches",
            Description = @"Branch-less conditional")]
        [TestCase(@"(?{1}a|b", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?{1}a|b/ at position 8: unterminated conditional group",
            Description = @"Open conditional group assertion")]
        [TestCase(@"(?{1}a|b|c)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?{1}a|b|c)/ at position 8: conditional group must contain exactly two branches",
            Description = @"Three-branched conditional")]
        [TestCase(@"(+abc)(?{2}a|b)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(+abc)(?{2}a|b)/ at position 8: No capturing group is named ""2""",
            Description = @"Check on nonexistent capture")]
        [TestCase(@"({hi}abc)(?{lo}a|b)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /({hi}abc)(?{lo}a|b)/ at position 11: No capturing group is named ""lo""",
            Description = @"Named check on nonexistent capture")]
        [TestCase(@"({a", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /({a/ at position 3: unterminated bracketed name",
            Description = @"Open capturing group name at regex's end")]
        [TestCase(@"({abc)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /({abc)/ at position 5: bracketed name must contain only word characters",
            Description = @"Open capturing group name in closed group")]
        [TestCase(@"({}a)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /({}a)/ at position 2: bracketed name may not be empty",
            Description = @"Empty capturing group name")]
        [TestCase(@"({A}abc", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /({A}abc/ at position 7: unterminated group",
            Description = @"Open capturing group")]
        [TestCase(@"({r&b})", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /({r&b})/ at position 3: bracketed name must contain only word characters",
            Description = @"Capturing group name with punctuation")]
        [TestCase(@"\g", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\g/ at position 2: backreference requires a capture ID",
            Description = @"Missing backreference name")]
        [TestCase(@"\g{a", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\g{a/ at position 4: unterminated bracketed name",
            Description = @"Open backreference name")]
        [TestCase(@"(+abc)\2", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(+abc)\2/ at position 7: regex has 1 captures, capture 2 does not exist",
            Description = @"Backreference to nonexistent capture")]
        [TestCase(@"({hi}abc)\g{lo}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /({hi}abc)\g{lo}/ at position 11: No capturing group is named ""lo""", 
            Description = @"Named backreference to nonexistent capture")]
        public void CapturingError(string pattern, RegexOptions opts)
        {
            Regex re = new Regex(pattern, opts);
        }

        [Test]
        public void ConditionalGroup()
        {
            Regex re = new Regex(@"((+a)|b)(?{1}A|B)");
            Assert.That(re.Match("aA").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aA", "a" }));
            Assert.That(re.Match("bB").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bB", null }));
            Assert.False(re.Match("aB").IsSuccess);
            Assert.False(re.Match("bA").IsSuccess);
        }

        [Test]
        public void NamedCaptureCheck()
        {
            Regex re = new Regex(@"(({choice}a)|b)(?{choice}A|B)");
            Match m = re.Match("aA");
            Assert.That(m.ToString(), Is.EqualTo("aA"));
            Assert.That(m.Groups["choice"].ToString(), Is.EqualTo("a"));

            m = re.Match("bB");
            Assert.That(m.ToString(), Is.EqualTo("bB"));
            Assert.That(m.Groups["choice"].ToString(), Is.Null);
            
            Assert.False(re.Match("aB").IsSuccess);
            Assert.False(re.Match("bA").IsSuccess);
        }

        [Test]
        public void OptionalParentheses()
        {
            Regex re = new Regex(@"(+ \( )? [^()]+ (?{1} \) |) ", RegexOptions.FreeSpacing);
            Assert.That(re.Match("abcd").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcd", null }));
            Assert.That(re.Match("(abcd)").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "(abcd)", "(" }));
            Assert.That(re.Match("the quick (abcd) fox").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "the quick ", null }));
            Assert.That(re.Match("(abcd").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcd", null }));
        }

        [Test]
        public void NestedCaptures()
        {
            Regex re = new Regex(@"^(+a(+b(+c)))(+d(+e(+f)))(+h(+i(+j)))(+k(+l(+m)))$");
            Assert.That(re.Match("abcdefhijklm").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcdefhijklm", "abc", "bc", "c", "def", "ef", "f",
                    "hij", "ij", "j", "klm", "lm", "m" }));
        }

        [Test]
        public void CapturesInNormalGroups()
        {
            Regex re = new Regex(@"^(a(+b(+c)))(d(+e(+f)))(h(+i(+j)))(k(+l(+m)))$");
            Assert.That(re.Match("abcdefhijklm").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcdefhijklm", "bc", "c", "ef", "f", "ij", "j", "lm", "m" }));
        }

        [Test]
        public void CaptureTrailingContext()
        {
            Regex re = new Regex(@"^(+ab\k(+cd))");
            Assert.That(re.Match("abcd").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "ab", "abcd", "cd" }));
        }

        [Test]
        public void CaptureLeadingContext()
        {
            Regex re = new Regex(@"(+foo)a\Kbar");
            Assert.That(re.Match("fooabar").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bar", "foo" }));
            Assert.False(re.Match("bar").IsSuccess);
            Assert.False(re.Match("foobbar").IsSuccess);
        }

        [Test]
        public void OptionalLeadingContext()
        {
            Regex re = new Regex(@"(+abc\K)?xyz");
            Assert.That(re.Match("abcxyz").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "xyz", "abc" }));
            Assert.That(re.Match("pqrxyz").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "xyz", null }));
        }

        [Test]
        public void GreedyDoubleStar()
        {
            Regex re = new Regex("(+[ab]*)*");
            Assert.That(re.Match("a").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "a", "a" }));
            Assert.That(re.Match("b").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "b", "b" }));
            Assert.That(re.Match("ababab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "ababab", "ababab" }));
            Assert.That(re.Match("aaaabcde").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "aaaab", "aaaab" }));
            Assert.That(re.Match("bbbb").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "bbbb", "bbbb" }));
            Assert.That(re.Match("cccc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "", null }));
        }

        [Test]
        public void LazyDoubleStar()
        {
            Regex re = new Regex("(+[ab]*?)*");
            Assert.That(re.Match("abab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "abab", "b" }));
            Assert.That(re.Match("baba").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "baba", "a" }));
        }

        [Test]
        public void CaptureInAtomicGroup()
        {
            Regex re = new Regex(@"(?>(+\.\d\d[1-9]?))\d+");
            Assert.That(re.Match("1.230003938").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { ".230003938", ".23" }));
            Assert.That(re.Match("1.875000282").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { ".875000282", ".875" }));
            Assert.False(re.Match("1.235").IsSuccess);
        }

        [Test]
        public void AlternatingAtomicsInCapture()
        {
            Regex re = new Regex(@"^(+(?>\w+)|(?>\s+))*$");
            Assert.That(re.Match("Now is the time for all good men to come to the aid of the party"
                ).Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "Now is the time for all good men to come to the aid of the party", "party" }));
            Assert.False(re.Match("this is not a line with only words and spaces!").IsSuccess);
        }

        [Test]
        public void OverlappingAtomicGroup()
        {
            Regex re = new Regex(@"(+(?>\d+))(+\w)");
            Assert.That(re.Match("12345a").Groups.Select(g => g.ToString()),
                Is.EqualTo(new String[] { "12345a", "12345", "a" }));
            Assert.False(re.Match("12345+").IsSuccess);
        }

        [Test]
        public void LimitQuantOverCapture()
        {
            Regex re = new Regex("^(+abc){1,2}zz");
            Assert.That(re.Match("abczz").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abczz", "abc" }));
            Assert.That(re.Match("abcabczz").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcabczz", "abc" }));
            Assert.False(re.Match("zz").IsSuccess);
            Assert.False(re.Match("abcabcabczz").IsSuccess);
            Assert.False(re.Match(">>abczz").IsSuccess);
        }

        [Test]
        public void CaptureOverGreedyQuant()
        {
            Regex re = new Regex("^(+b+|a){1,2}c");
            Assert.That(re.Match("bc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bc", "b" }));
            Assert.That(re.Match("bbc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bbc", "bb" }));
            Assert.That(re.Match("bbbc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bbbc", "bbb" }));
            Assert.That(re.Match("bac").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bac", "a" }));
            Assert.That(re.Match("bbac").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bbac", "a" }));
            Assert.That(re.Match("aac").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aac", "a" }));
            Assert.That(re.Match("abbbbbbbbbbbc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abbbbbbbbbbbc", "bbbbbbbbbbb" }));
            Assert.That(re.Match("bbbbbbbbbbbac").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bbbbbbbbbbbac", "a" }));
            Assert.False(re.Match("aaac").IsSuccess);
            Assert.False(re.Match("abbbbbbbbbbbac").IsSuccess);
        }

        [Test]
        public void CaptureOverLazyQuant()
        {
            Regex re = new Regex("^(+b+?|a){1,2}?c");
            Assert.That(re.Match("bc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bc", "b" }));
            Assert.That(re.Match("bbc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bbc", "b" }));
            Assert.That(re.Match("bbbc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bbbc", "bb" }));
            Assert.That(re.Match("bac").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bac", "a" }));
            Assert.That(re.Match("bbac").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bbac", "a" }));
            Assert.That(re.Match("aac").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aac", "a" }));
            Assert.That(re.Match("abbbbbbbbbbbc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abbbbbbbbbbbc", "bbbbbbbbbbb" }));
            Assert.That(re.Match("bbbbbbbbbbbac").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bbbbbbbbbbbac", "a" }));
            Assert.False(re.Match("aaac").IsSuccess);
            Assert.False(re.Match("abbbbbbbbbbbac").IsSuccess);
        }

        [Test]
        public void BasicBackref()
        {
            Regex re = new Regex(@"\b{A}(+abc|def)=(+\1){2,3}\b{Z}");
            Assert.That(re.Match("abc=abcabc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abc=abcabc", "abc", "abc" }));
            Assert.That(re.Match("def=defdefdef").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "def=defdefdef", "def", "def" }));
            Assert.False(re.Match("abc=defdef").IsSuccess);
        }

        [Test]
        public void NumberBackref()
        {
            Regex re = new Regex(@"\b{A}(+abc|def)=(+\g1){2,3}\b{Z}");
            Assert.That(re.Match("abc=abcabc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abc=abcabc", "abc", "abc" }));
            Assert.That(re.Match("def=defdefdef").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "def=defdefdef", "def", "def" }));
            Assert.False(re.Match("abc=defdef").IsSuccess);
        }

        [Test]
        public void NamedBackref()
        {
            Regex re = new Regex(@"\b{A}({base}abc|def)=(+\g{base}){2,3}\b{Z}");
            Match m = re.Match("abc=abcabc");
            Assert.That(m.ToString(), Is.EqualTo("abc=abcabc"));
            Assert.That(m.Groups["base"].ToString(), Is.EqualTo("abc"));

            m = re.Match("def=defdefdef");
            Assert.That(m.ToString(), Is.EqualTo("def=defdefdef"));
            Assert.That(m.Groups["base"].ToString(), Is.EqualTo("def"));

            Assert.False(re.Match("abc=defdef").IsSuccess);
        }

        [Test]
        public void DoublyQuantifiedBackref()
        {
            Regex re = new Regex(@"\b{A}(+\w)(\1){3,4}+\b{Z}");
            Assert.That(re.Match("aaaa").ToString(), Is.EqualTo("aaaa"));
            Assert.That(re.Match("bbbbb").ToString(), Is.EqualTo("bbbbb"));
            Assert.That(re.Match("ccccccc").ToString(), Is.EqualTo("ccccccc"));
            Assert.That(re.Match("dddddddd").ToString(), Is.EqualTo("dddddddd"));
            Assert.False(re.Match("ffffff").IsSuccess);
        }

        [Test]
        public void ElevenBackrefs()
        {
            Regex re = new Regex(@"^(+a)(+b)(+c)(+d)(+e)(+f)(+g)(+h)(+i)(+j)(+k)\11*(+\3\4)\1(?#)2$");
            Assert.That(re.Match("abcdefghijkcda2").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcdefghijkcda2", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "cd" }));
            Assert.That(re.Match("abcdefghijkkkkcda2").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcdefghijkkkkcda2", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "cd" }));
        }

        [Test]
        public void BackrefMatchedEmpty()
        {
            Regex re = new Regex(@"^(+cow|)\1(+bell)");
            Assert.That(re.Match("cowcowbell").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "cowcowbell", "cow", "bell" }));
            Assert.That(re.Match("bell").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "bell", "", "bell" }));
            Assert.False(re.Match("cowbell").IsSuccess);
        }

        [Test]
        public void StarEmptyBackref()
        {
            Regex re = new Regex(@"^(+a|)\1*b");
            Assert.That(re.Match("ab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "ab", "a" }));
            Assert.That(re.Match("aaaab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aaaab", "a" }));
            Assert.That(re.Match("b").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "b", "" }));
            Assert.False(re.Match("acb").IsSuccess);
        }

        [Test]
        public void PlusEmptyBackref()
        {
            Regex re = new Regex(@"^(+a|)\1+b");
            Assert.That(re.Match("aab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aab", "a" }));
            Assert.That(re.Match("aaaab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aaaab", "a" }));
            Assert.That(re.Match("b").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "b", "" }));
            Assert.False(re.Match("ab").IsSuccess);
        }

        [Test]
        public void OptEmptyBackref()
        {
            Regex re = new Regex(@"^(+a|)\1?b");
            Assert.That(re.Match("ab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "ab", "a" }));
            Assert.That(re.Match("aab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aab", "a" }));
            Assert.That(re.Match("b").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "b", "" }));
            Assert.False(re.Match("acb").IsSuccess);
        }

        [Test]
        public void RepeatEmptyBackref()
        {
            Regex re = new Regex(@"^(+a|)\1{2}b");
            Assert.That(re.Match("aaab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aaab", "a" }));
            Assert.That(re.Match("b").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "b", "" }));
            Assert.False(re.Match("ab").IsSuccess);
            Assert.False(re.Match("aab").IsSuccess);
            Assert.False(re.Match("aaaab").IsSuccess);
        }

        [Test]
        public void LimitQuantEmptyBackref()
        {
            Regex re = new Regex(@"^(+a|)\1{2,3}b");
            Assert.That(re.Match("aaab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aaab", "a" }));
            Assert.That(re.Match("aaaab").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "aaaab", "a" }));
            Assert.That(re.Match("b").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "b", "" }));
            Assert.False(re.Match("ab").IsSuccess);
            Assert.False(re.Match("aab").IsSuccess);
            Assert.False(re.Match("aaaaab").IsSuccess);
        }

        [Test]
        public void BackrefLeadingContext()
        {
            Regex re = new Regex(@"(+foo)\Kbar\1");
            Assert.That(re.Match("foobarfoo").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "barfoo", "foo" }));
            Assert.That(re.Match("foobarfootling").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "barfoo", "foo" }));
            Assert.False(re.Match("foobar").IsSuccess);
            Assert.False(re.Match("barfoo").IsSuccess);
        }

        [Test]
        public void CaseFoldBackref()
        {
            Regex re = new Regex(@"(+abc)\1", RegexOptions.IgnoreCase);
            Assert.That(re.Match("abcabc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcabc", "abc" }));
            Assert.That(re.Match("ABCabc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "ABCabc", "ABC" }));
            Assert.That(re.Match("abcABC").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcABC", "abc" }));
        }

        [Test]
        public void NamedCaseFoldBackref()
        {
            Regex re = new Regex(@"({base}abc)\g{base}", RegexOptions.IgnoreCase);
            Assert.That(re.Match("abcabc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcabc", "abc" }));
            Assert.That(re.Match("ABCabc").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "ABCabc", "ABC" }));
            Assert.That(re.Match("abcABC").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcABC", "abc" }));
        }

        [Test]
        public void CaseFoldCapture()
        {
            Regex re = new Regex(@"(+(?i)blah)\s+\1");
            Assert.That(re.Match("blah blah").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "blah blah", "blah" }));
            Assert.That(re.Match("BLAH BLAH").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "BLAH BLAH", "BLAH" }));
            Assert.That(re.Match("Blah Blah").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "Blah Blah", "Blah" }));
            Assert.That(re.Match("blaH blaH").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "blaH blaH", "blaH" }));
            Assert.False(re.Match("blah BLAH").IsSuccess);
            Assert.False(re.Match("Blah blah").IsSuccess);
            Assert.False(re.Match("blaH blah").IsSuccess);
        }

        [Test]
        public void IntersectionCapture()
        {
            Regex re = new Regex("^((ab(+de))&(+abd)e)");
            Assert.That(re.Match("abde").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abde", "de", "abd" }));
        }

        [Test]
        public void IntersectionLookahead()
        {
            Regex re = new Regex("ab(+cd)&(+ab).*", RegexOptions.Anchored);
            Assert.That(re.Match("abcd").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcd", "cd", "ab" }));
        }
    }
}
