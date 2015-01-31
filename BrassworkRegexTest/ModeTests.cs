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
    /// <summary>Unit tests for switchable regex options.</summary>
    [TestFixture]
    class ModeTests
    {
        [TestCase(@"^1234(?# test newlines\n  inside", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /^1234(?# test newlines\n  inside/ at position 32: unterminated comment",
            Description = @"Open inline comment")]
        [TestCase(@"(?ihello", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /(?ihello/ at position 3: unterminated mode modifier",
            Description = @"Open mode switch")]
        [TestCase(@"\b{g}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\b{g}/ at position 3: decimal number expected",
            Description = @"Open capture check ID")]
        [TestCase(@"\B{g}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\B{g}/ at position 3: decimal number expected",
            Description = @"Open negated capture check ID")]
        public void ModeError(string pattern, RegexOptions opts)
        {
            Regex re = new Regex(pattern, opts);
        }

        [TestCase(@"^1234(?# test newlines\n  inside)", RegexOptions.None, "1234", Result = true,
            Description = @"Inline comment")]
        [TestCase(@"   ^    a   (?# begins with a)  b\sc (?# then b c) $ (?# then end)",
            RegexOptions.FreeSpacing, "ab c", Result = true,
            Description = @"Free spacing option")]
        [TestCase(@"(?x)   ^    a   (?# begins with a)  b\sc (?# then b c) $ (?# then end)",
            RegexOptions.FreeSpacing, "ab c", Result = true,
            Description = @"Free spacing mode switch")]
        [TestCase(@"a (?x) b # wild and free", RegexOptions.None, "a b", Result = true,
            Description = @"Turn free spacing on")]
        [TestCase(@"a (?-x) b", RegexOptions.FreeSpacing, "a b", Result = true,
            Description = @"Turn free spacing off")]
        [TestCase(@"1234 #comment in extended re\n  ", RegexOptions.FreeSpacing, "1234", Result = true,
            Description = @"Free spacing comment")]
        [TestCase(@"#rhubarb\n  abcd", RegexOptions.FreeSpacing, "abcd", Result = true,
            Description = @"Newline ends free spacing comment")]
        public bool Modes(string pattern, RegexOptions opts, string text)
        {
            Regex re = new Regex(pattern, opts);
            return re.Match(text).IsSuccess;
        }

        [Test]
        public void CaseFoldSequence()
        {
            Regex re = new Regex("the quick brown fox", RegexOptions.IgnoreCase);
            Assert.AreEqual("the quick brown fox", re.Match("the quick brown fox").ToString());
            Assert.AreEqual("the quick brown fox",
                re.Match("What do you know about the quick brown fox?").ToString());
            Assert.AreEqual("the quick brown FOX", re.Match("the quick brown FOX").ToString());
            Assert.AreEqual("THE QUICK BROWN FOX",
                re.Match("What do you know about THE QUICK BROWN FOX?").ToString());
        }

        [Test]
        public void CaseFoldModeSwitch()
        {
            Regex re = new Regex("the (?i)quick brown(?-i) fox", RegexOptions.IgnoreCase);
            Assert.AreEqual("the quick brown fox", re.Match("the quick brown fox").ToString());
            Assert.AreEqual("the QUICK BROWN fox", re.Match("the QUICK BROWN fox").ToString());
            Assert.AreEqual("the QUICK brown fox", re.Match("the QUICK brown fox").ToString());
            Assert.False(re.Match("the quick brown FOX").IsSuccess);
        }

        [Test]
        public void NegateCaseFold()
        {
            Regex re = new Regex("[^a]", RegexOptions.IgnoreCase);
            Assert.AreEqual("b", re.Match("Abc").ToString());
            Assert.AreEqual("b", re.Match("aaaabcd").ToString());
            Assert.AreEqual("b", re.Match("aaAabcd").ToString());
        }

        [Test]
        public void MixedCaseRange()
        {
            Regex re = new Regex(@"^[W-c]+$", RegexOptions.IgnoreCase);
            Assert.That(re.Match("WXY_^abc").ToString(), Is.EqualTo("WXY_^abc"));
            Assert.That(re.Match("wxy_^ABC").ToString(), Is.EqualTo("wxy_^ABC"));
        }

        [Test]
        public void MixedCaseRangeByCodepoints()
        {
            Regex re = new Regex(@"^[\x{3f}-\x{5F}]+$", RegexOptions.IgnoreCase);
            Assert.That(re.Match("WXY_^abc").ToString(), Is.EqualTo("WXY_^abc"));
            Assert.That(re.Match("wxy_^ABC").ToString(), Is.EqualTo("wxy_^ABC"));
        }

        [Test]
        public void FreeSpacingMatchSpace()
        {
            Regex re = new Regex(@"^   a\ b[c ]d       $", RegexOptions.FreeSpacing);
            Assert.True(re.Match("a bcd").IsSuccess);
            Assert.True(re.Match("a b d").IsSuccess);
            Assert.False(re.Match("abcd").IsSuccess);
            Assert.False(re.Match("ab d").IsSuccess);
        }

        [Test]
        public void TextAnchors()
        {
            Regex re = new Regex(@"\b{A}abc\b{Z}");
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("qqq\nabc").IsSuccess);
            Assert.False(re.Match("abc\nzzz").IsSuccess);
            Assert.False(re.Match("qqq\nabc\nzzz").IsSuccess);
        }

        [Test]
        public void NegatedTextAnchors()
        {
            Regex re = new Regex(@"\B{A}abc\B{Z}");
            Assert.That(re.Match("qqq\nabc\nzzz").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("qqq\nabc").IsSuccess);
            Assert.False(re.Match("abc\nzzz").IsSuccess);
            Assert.False(re.Match("abc").IsSuccess);
        }

        [Test]
        public void LineAnchors()
        {
            Regex re = new Regex(@"\b{a}abc\b{z}");
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("qqq\nabc").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("abc\nzzz").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("qqq\nabc\nzzz").ToString(), Is.EqualTo("abc"));
        }

        [Test]
        public void NegatedLineAnchors()
        {
            Regex re = new Regex(@"\B{a}abc\B{z}");
            Assert.That(re.Match("qqq abc zzz").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("qqq\nabc zzz").IsSuccess);
            Assert.False(re.Match("qqq abc\nzzz").IsSuccess);
            Assert.False(re.Match("qqq\nabc\nzzz").IsSuccess);
        }

        [Test]
        public void ShortAnchors()
        {
            Regex re = new Regex(@"^abc$");
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("qqq\nabc").IsSuccess);
            Assert.False(re.Match("abc\nzzz").IsSuccess);
            Assert.False(re.Match("qqq\nabc\nzzz").IsSuccess);
        }

        [Test]
        public void MultilineOption()
        {
            Regex re = new Regex(@"^abc$", RegexOptions.Multiline);
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("qqq\nabc").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("abc\nzzz").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("qqq\nabc\nzzz").ToString(), Is.EqualTo("abc"));
        }

        [Test]
        public void MultilineModeOn()
        {
            Regex re = new Regex(@"^ab(?m)c$");
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("qqq\nabc").IsSuccess);
            Assert.That(re.Match("abc\nzzz").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("qqq\nabc\nzzz").IsSuccess);
        }

        [Test]
        public void MultilineModeOff()
        {
            Regex re = new Regex(@"^ab(?-m)c$", RegexOptions.Multiline);
            Assert.That(re.Match("abc").ToString(), Is.EqualTo("abc"));
            Assert.That(re.Match("qqq\nabc").ToString(), Is.EqualTo("abc"));
            Assert.False(re.Match("abc\nzzz").IsSuccess);
            Assert.False(re.Match("qqq\nabc\nzzz").IsSuccess);
        }

        [Test]
        public void DotAllOption()
        {
            Regex re = new Regex("^12.34", RegexOptions.DotAll);
            Assert.That(re.Match("12\n34").ToString(), Is.EqualTo("12\n34"));
            Assert.That(re.Match("12\r34").ToString(), Is.EqualTo("12\r34"));
        }

        [Test]
        public void DotAllModeOn()
        {
            Regex re = new Regex("^12(?s).34");
            Assert.That(re.Match("12\n34").ToString(), Is.EqualTo("12\n34"));
            Assert.That(re.Match("12\r34").ToString(), Is.EqualTo("12\r34"));
        }

        [Test]
        public void DotAllModeOff()
        {
            Regex re = new Regex("^12(?-s).34", RegexOptions.DotAll);
            Assert.That(re.Match("12.34").ToString(), Is.EqualTo("12.34"));
            Assert.False(re.Match("12\n34").IsSuccess);
            Assert.False(re.Match("12\r34").IsSuccess);
        }

        [Test]
        public void DotWithDotAll()
        {
            Regex re = new Regex(@"\b{A}(+.)*\b{Z}", RegexOptions.DotAll);
            Assert.That(re.Match("abc\ndef").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abc\ndef", "f" }));
        }

        [Test]
        public void DotWithMultiline()
        {
            Regex re = new Regex(@"\b{A}(+.)*\b{Z}", RegexOptions.Multiline);
            Assert.That(re.Match("abcdef").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "abcdef", "f" }));
            Assert.False(re.Match("abc\ndef").IsSuccess);
        }

        [TestCase(@".*\.gif", RegexOptions.None, "borfle\nbib.gif\nno", Result = "bib.gif",
            Description = @".gif suffix, no options")]
        [TestCase(@".*\.gif", RegexOptions.Multiline, "borfle\nbib.gif\nno", Result = "bib.gif",
            Description = @".gif suffix, Multiline")]
        [TestCase(@".*\.gif", RegexOptions.DotAll, "borfle\nbib.gif\nno", Result = "borfle\nbib.gif",
            Description = @".gif suffix, DotAll")]
        [TestCase(@".*\.gif", RegexOptions.DotAll | RegexOptions.Multiline, "borfle\nbib.gif\nno",
            Result = "borfle\nbib.gif",
            Description = @".gif suffix, DotAll and Multiline")]
        [TestCase(@".*$", RegexOptions.None, "borfle\nbib.gif\nno", Result = "no",
            Description = @"$ assertion, no options")]
        [TestCase(@".*$", RegexOptions.Multiline, "borfle\nbib.gif\nno", Result = "borfle",
            Description = @"$ assertion, Multiline")]
        [TestCase(@".*$", RegexOptions.DotAll, "borfle\nbib.gif\nno", Result = "borfle\nbib.gif\nno",
            Description = @"$ assertion, DotAll")]
        [TestCase(@".*$", RegexOptions.DotAll | RegexOptions.Multiline, "borfle\nbib.gif\nno",
            Result = "borfle\nbib.gif\nno",
            Description = @"$ assertion, DotAll and Multiline")]
        [TestCase(@"^.*B", RegexOptions.None, "abc\nB", Result = null,
            Description = @"mode switches, no options")]
        [TestCase(@"(?m)^.*B", RegexOptions.None, "abc\nB", Result = "B",
            Description = @"mode switches, Multiline")]
        [TestCase(@"(?s)^.*B", RegexOptions.None, "abc\nB", Result = "abc\nB",
            Description = @"mode switches, DotAll")]
        [TestCase(@"(?ms)^.*B", RegexOptions.None, "abc\nB", Result = "abc\nB",
            Description = @"mode switches, DotAll and Multiline")]
        public string DotAllVersusMultiline1(string pattern, RegexOptions opts, string text)
        {
            Regex re = new Regex(pattern, opts);
            return re.Match(text).ToString();
        }

        [TestCase(RegexOptions.None, "1234X", "B", null,
            Description = @"Alternation, no options")]
        [TestCase(RegexOptions.Multiline, "1234X", "B", "B",
            Description = @"Alternation, Multiline")]
        [TestCase(RegexOptions.DotAll, "abcde\n1234X", "B", null,
            Description = @"Alternation, DotAll")]
        [TestCase(RegexOptions.DotAll | RegexOptions.Multiline, "abcde\n1234X", "B", "B",
            Description = @"Alternation, DotAll and Multiline")]
        public void DotAllVersusMultiline2(RegexOptions opts, string res1, string res2, string res3)
        {
            Regex re = new Regex(@".*X|^B", opts);
            Assert.That(re.Match("abcde\n1234Xyz").ToString(), Is.EqualTo(res1));
            Assert.That(re.Match("BarFoo").ToString(), Is.EqualTo(res2));
            Assert.That(re.Match("abcde\nBar").ToString(), Is.EqualTo(res3));
        }

        [Test]
        public void DefaultWordBreaks()
        {
            Regex re = new Regex(@"\b");
            Assert.That(re.Split("The 'quick' (\"brown\") fox\r\ncan't ju\xFEFFmp 32.3 feet,\nright?"),
                Is.EqualTo(new string[] { "", "The", " ", "'", "quick", "'", " ", "(", "\"", "brown", "\"", ")", " ",
                    "fox", "\r\n", "can't", " ", "ju\xFEFFmp", " ", "32.3", " ", "feet", ",", "\n", "right", "?", "" }));
        }

        [Test]
        public void DefaultNonWordBreaks()
        {
            Regex re = new Regex(@"\B\p{Upper}");
            Assert.That(re.Match("CamelcaseIdentifier").ToString(), Is.EqualTo("I"));
            Assert.That(re.Match("Glottal'Stop").ToString(), Is.EqualTo("S"));
        }

        [Test]
        public void SimpleWordBreaks()
        {
            Regex re = new Regex(@"\b", RegexOptions.SimpleWordBreak);
            Assert.That(re.Split("The 'quick' (\"brown\") fox\r\ncan't ju\xFEFFmp 32.3 feet,\nright?"),
                Is.EqualTo(new string[] { "", "The", " '", "quick", "' (\"", "brown", "\") ", "fox", "\r\n", "can",
                    "'", "t", " ", "ju", "\xFEFF", "mp", " ", "32", ".", "3", " ", "feet", ",\n", "right", "?", "" }));
        }

        [Test]
        public void SimpleNonWordBreaks()
        {
            Regex re = new Regex(@"\B\p{Upper}", RegexOptions.SimpleWordBreak);
            Assert.That(re.Match("CamelcaseIdentifier").ToString(), Is.EqualTo("I"));
            Assert.False(re.Match("Glottal'Stop").IsSuccess);
        }

        [Test]
        public void SimpleWordBreaksOn()
        {
            Regex re = new Regex(@"(?b).+\b\d+");
            Assert.That(re.Match("1.23").ToString(), Is.EqualTo("1.23"));
        }

        [Test]
        public void SimpleWordBreaksOff()
        {
            Regex re = new Regex(@"(?-b)\Bt", RegexOptions.SimpleWordBreak);
            Assert.That(re.Match("can't").ToString(), Is.EqualTo("t"));
        }
    }
}
