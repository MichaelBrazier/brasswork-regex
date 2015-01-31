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
    /// <summary>Unit tests for the basic regex operators.</summary>
    [TestFixture]
    class StructuralTests
    {
        [TestCase(@"a{", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{/ at position 2: unterminated quantifier",
            Description = @"Open quantifier without arguments")]
        [TestCase(@"a{b", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{b/ at position 2: decimal number expected",
            Description = @"Letter as quantifier minimum")]
        [TestCase(@"a{2", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{2/ at position 3: unterminated quantifier", 
            Description = @"Open repetition at regex's end")]
        [TestCase(@"a{2b", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{2b/ at position 3: unterminated quantifier",
            Description = @"Open repetition running into text")]
        [TestCase(@"a{2,", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{2,/ at position 4: unterminated quantifier",
            Description = @"Open unlimited quantifier at regex's end")]
        [TestCase(@"a{2,b", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{2,b/ at position 4: decimal number expected",
            Description = @"Open unlimited quantifier running into text")]
        [TestCase(@"a{2,3", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{2,3/ at position 5: unterminated quantifier",
            Description = @"Open limited quantifier at regex's end")]
        [TestCase(@"a{2,3b", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{2,3b/ at position 5: unterminated quantifier",
            Description = @"Open limited quantifier running into text")]
        [TestCase(@"a{1,0}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /a{1,0}/ at position 6: max repeats less than min repeats", 
            Description = @"Reversed quantifier")]
        [TestCase(@"(abc", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(abc/ at position 4: unterminated group", 
            Description = @"Open normal group")]
        [TestCase(@"(?", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?/ at position 2: unterminated group", 
            Description = @"Incomplete special group")]
        [TestCase(@"(?a)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?a)/ at position 1: unrecognized grouping ""(?a""", 
            Description = @"Unrecognized special group")]
        [TestCase(@"(?>abc", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?>abc/ at position 6: unterminated group",
            Description = @"Open atomic group")]
        [TestCase(@"~(+abc)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /~(+abc)/ at position 2: capturing group cannot be complemented",
            Description = @"Capture in complement")]
        [TestCase(@"~({hi}abc)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /~({hi}abc)/ at position 2: capturing group cannot be complemented",
            Description = @"Capture in complement")]
        [TestCase(@"~(?>abc)", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /~(?>abc)/ at position 2: atomic group cannot be complemented",
            Description = @"Capture in complement")]
        public void StructuralError(string pattern, RegexOptions opts)
        {
            Regex re = new Regex(pattern, opts);
        }

        [Test]
        public void Sequence()
        {
            Regex re = new Regex("the quick brown fox");
            Assert.AreEqual("the quick brown fox", re.Match("the quick brown fox").ToString());
            Assert.AreEqual("the quick brown fox",
                re.Match("What do you know about the quick brown fox?").ToString());
            Assert.False(re.Match("the quick brown FOX").IsSuccess);
            Assert.False(re.Match("What do you know about THE QUICK BROWN FOX?").IsSuccess);
        }

        [Test]
        public void EmptyAlternative()
        {
            Regex re = new Regex("|abc");
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("").ToString(), Is.EqualTo(""));
        }

        [Test]
        public void Quant0to1()
        {
            Regex re = new Regex("^(a){0,1}");
            Assert.That(re.Match("bcd").ToString(), Is.EqualTo(""));
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("a"));
        }

        [Test]
        public void Quant0to2()
        {
            Regex re = new Regex("^(a){0,2}");
            Assert.That(re.Match("bcd").ToString(), Is.EqualTo(""));
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("aa"));
        }

        [Test]
        public void Quant0to3()
        {
            Regex re = new Regex("^(a){0,3}");
            Assert.That(re.Match("bcd").ToString(), Is.EqualTo(""));
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("aa"));
            Assert.That(re.Match("aaa").ToString(), Is.EqualTo("aaa"));
        }

        [Test]
        public void Quant0andUp()
        {
            Regex re = new Regex("^(a){0,}");
            Assert.That(re.Match("bcd").ToString(), Is.EqualTo(""));
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("aa"));
            Assert.That(re.Match("aaaaaaaa").ToString(), Is.EqualTo("aaaaaaaa"));
        }

        [Test]
        public void Quant1to1()
        {
            Regex re = new Regex("^(a){1,1}");
            Assert.False(re.Match("bcd").IsSuccess);
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("a"));
        }

        [Test]
        public void Quant1to2()
        {
            Regex re = new Regex("^(a){1,2}");
            Assert.False(re.Match("bcd").IsSuccess);
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("aa"));
        }

        [Test]
        public void Quant1to3()
        {
            Regex re = new Regex("^(a){1,3}");
            Assert.False(re.Match("bcd").IsSuccess);
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("aa"));
            Assert.That(re.Match("aaa").ToString(), Is.EqualTo("aaa"));
        }

        [Test]
        public void Quant1andUp()
        {
            Regex re = new Regex("^(a){1,}");
            Assert.False(re.Match("bcd").IsSuccess);
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("a"));
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("aa"));
            Assert.That(re.Match("aaaaaaaa").ToString(), Is.EqualTo("aaaaaaaa"));
        }

        [Test]
        public void Repetition()
        {
            Regex re = new Regex("^(a){2}");
            Assert.False(re.Match("bcd").IsSuccess);
            Assert.False(re.Match("abc").IsSuccess);
            Assert.That(re.Match("aab").ToString(), Is.EqualTo("aa"));
        }

        [Test]
        public void QuantResets()
        {
            Regex re = new Regex(@"([a-f](.[d-m].){0,2}[h-n]){2}", RegexOptions.Anchored);
            Assert.That(re.Match("a hundred fair pages").ToString(), Is.EqualTo("a hundred fai"));
        }

        [Test]
        public void AtomicGroup()
        {
            Regex re = new Regex("(?>.*/)foo");
            Assert.That(re.Match("/this/is/a/very/long/line/in/deed/with/very/many/slashes/in/and/foo").ToString(),
                Is.EqualTo("/this/is/a/very/long/line/in/deed/with/very/many/slashes/in/and/foo"));
            Assert.False(re.Match("/this/is/a/very/long/line/in/deed/with/very/many/slashes/in/it/you/see/").IsSuccess);
        }

        [Test]
        public void QuantifiedAtomicGroup()
        {
            Regex re = new Regex("(?>[ab]{1,2}){1,2}a");
            Assert.That(re.Match("baa").ToString(), Is.EqualTo("baa"));
            Assert.That(re.Match("babaa").ToString(), Is.EqualTo("babaa"));
            Assert.False(re.Match("baba").IsSuccess);
        }

        [Test]
        public void TrailingContext()
        {
            Regex re = new Regex(@"\w+\k\t");
            Assert.That(re.Match("the quick brown\t fox").ToString(), Is.EqualTo("brown"));
            Assert.False(re.Match("the quick brown fox").IsSuccess);
        }

        [Test]
        public void TrailingContextAlternative()
        {
            Regex re = new Regex(@"\.\d\d(\k0|[1-9]\k\d)");
            Assert.That(re.Match("1.230003938").ToString(), Is.EqualTo(".23"));
            Assert.That(re.Match("1.875000282").ToString(), Is.EqualTo(".875"));
            Assert.False(re.Match("1.235").IsSuccess);
        }

        [Test]
        public void SplitBefore()
        {
            Regex re = new Regex(@"\kC");
            Assert.That(re.Split("ABCDECBA"), Is.EqualTo(new string[] { "AB", "CDE", "CBA" }));
        }

        [Test]
        public void LeadingContext()
        {
            Regex re = new Regex(@"foo\n\K^bar", RegexOptions.Multiline);
            Assert.That(re.Match("foo\nbar").ToString(), Is.EqualTo("bar"));
            Assert.False(re.Match("bar").IsSuccess);
            Assert.False(re.Match("baz\nbar").IsSuccess);
        }

        [Test]
        public void SplitAfter()
        {
            Regex re = new Regex(@"C\K");
            Assert.That(re.Split("ABCDECBA"), Is.EqualTo(new string[] { "ABC", "DEC", "BA" }));
        }

        [Test]
        public void BasicIntersection()
        {
            Regex re = new Regex("^(.*a.*&.*b.*)$");
            Assert.That(re.Match("abcd").ToString(), Is.EqualTo("abcd"));
            Assert.That(re.Match("cbda").ToString(), Is.EqualTo("cbda"));
            Assert.False(re.Match("aeiou").IsSuccess);
            Assert.False(re.Match("robber").IsSuccess);
        }

        [Test]
        public void IntersectCasefold()
        {
            Regex re = new Regex("abc&(?i)abc", RegexOptions.Anchored);
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("ABC").IsSuccess);
        }

        [Test]
        public void Permutation()
        {
            Regex re = new Regex("[abc]{3}&.*a.*&.*b.*&.*c.*");
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("acb").ToString(), Is.EqualTo("acb"));
            Assert.That(re.Match("bac").ToString(), Is.EqualTo("bac"));
            Assert.That(re.Match("bca").ToString(), Is.EqualTo("bca"));
            Assert.That(re.Match("cab").ToString(), Is.EqualTo("cab"));
            Assert.That(re.Match("cba").ToString(), Is.EqualTo("cba"));
            Assert.False(re.Match("aaa").IsSuccess);
            Assert.False(re.Match("bbc").IsSuccess);
        }

        [Test]
        public void ComplexIntersection()
        {
            Regex re = new Regex("[abcd].*[abcd] & [^ab]*a[^ab]*b[^ab]* & [^cd]*c[^cd]*d[^cd]*", RegexOptions.FreeSpacing);
            Assert.That(re.Match("aebcfd").ToString(), Is.EqualTo("aebcfd"));
            Assert.That(re.Match("efcaghdbij").ToString(), Is.EqualTo("caghdb"));
            Assert.False(re.Match("abccd").IsSuccess);
            Assert.False(re.Match("bacd").IsSuccess);
            Assert.False(re.Match("dabc").IsSuccess);
        }

        [Test]
        public void BasicComplement()
        {
            Regex re = new Regex("^(~.*ab.*)$", RegexOptions.DotAll);
            Assert.That(re.Match("bbaaa").ToString(), Is.EqualTo("bbaaa"));
            Assert.That(re.Match("aaa").ToString(), Is.EqualTo("aaa"));
            Assert.False(re.Match("aaabb").IsSuccess);
        }

        [Test]
        public void ComplementSuffix()
        {
            Regex re = new Regex("foo(~bar.*)$");
            Assert.That(re.Match("foobar is foolish see?").ToString(), Is.EqualTo("foolish see?"));
        }

        [Test]
        public void CStyleComment()
        {
            Regex re = new Regex(@"/\*(~.*\*/.*)\*/", RegexOptions.DotAll | RegexOptions.Anchored);
            Assert.That(re.Match("/* comment */").ToString(), Is.EqualTo("/* comment */"));
            Assert.That(re.Match("/* multiline\n * comment\n */").ToString(), Is.EqualTo("/* multiline\n * comment\n */"));
            Assert.That(re.Match("/*foo*/bar*/").ToString(), Is.EqualTo("/*foo*/"));
            Assert.False(re.Match("this isn't a comment").IsSuccess);
            Assert.False(re.Match("/* this isn't terminated").IsSuccess);
        }

        [Test]
        public void WordDifference()
        {
            Regex re = new Regex("...~abc");
            Assert.That(re.Match("foo").ToString(), Is.EqualTo("foo"));
            Assert.False(re.Match("abc").IsSuccess);
        }

        [Test]
        public void RepeatDifference()
        {
            Regex re = new Regex("...~foo~bar");
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("foo").IsSuccess);
            Assert.False(re.Match("bar").IsSuccess);
        }

        [Test]
        public void ComplementPrefix()
        {
            Regex re = new Regex("(...~foo|^.{0,2})bar.*");
            Assert.That(re.Match("foobar crowbar etc").ToString(), Is.EqualTo("rowbar etc"));
            Assert.That(re.Match("barrel").ToString(), Is.EqualTo("barrel"));
            Assert.That(re.Match("2barrel").ToString(), Is.EqualTo("2barrel"));
            Assert.That(re.Match("A barrel").ToString(), Is.EqualTo("A barrel"));
        }
    }
}
