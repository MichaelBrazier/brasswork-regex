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
using NUnit.Framework;

namespace Brasswork.Regex.Test
{
    /// <summary>Unit tests for the regular expression parser.</summary>
    [TestFixture]
    class PropertyTests
    {
        [TestCase(@"\p", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p/ at position 2: bracketed Unicode property name expected",
            Description = @"Missing property name")]
        [TestCase(@"\p{}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{}/ at position 3: Unicode property name must not be empty",
            Description = @"Empty property name")]
        [TestCase(@"\p{Basic", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{Basic/ at position 8: unterminated Unicode property",
            Description = @"Open property name")]
        [TestCase(@"\p{Latter}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{Latter}/ at position 9: unrecognized Unicode property name ""Latter""",
            Description = @"Unrecognized property name")]
        [TestCase(@"\p{Block=Wingdings}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{Block=Wingdings}/ at position 18: unrecognized block name ""Wingdings""",
            Description = @"Empty property name")]
        [TestCase(@"\p{GC=}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{GC=}/ at position 6: Unicode property value must not be empty",
            Description = @"Empty property value")]
        [TestCase(@"\p{GC=Lu?", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{GC=Lu?/ at position 8: unterminated Unicode property",
            Description = @"Open property value")]
        [TestCase(@"\p{GC=Qu}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{GC=Qu}/ at position 8: unrecognized value ""Qu"" for Gc property",
            Description = @"Unrecognized category")]
        [TestCase(@"\p{Sc=Quenya}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{Sc=Quenya}/ at position 12: unrecognized value ""Quenya"" for Sc property",
            Description = @"Unrecognized script")]
        [TestCase(@"\p{Scx=Quenya}", RegexOptions.None,
            ExpectedException = typeof(RegexParseException),
            ExpectedMessage = @"Error in /\p{Scx=Quenya}/ at position 13: unrecognized value ""Quenya"" for Scx property",
            Description = @"Unrecognized extended script")]
        public void PropertyError(string pattern, RegexOptions opts)
        {
            Regex re = new Regex(pattern, opts);
        }

        [TestCase(@"^\p{C}\p{L}\p{M}\p{N}\p{P}\p{S}\p{Z}<", RegexOptions.None,
            "\x7f\xc0\x30f\x0660\x066c\xf01\x1680<", Result = true,
            Description = @"One-letter categories")]
        public bool Properties(string pattern, RegexOptions opts, string text)
        {
            Regex re = new Regex(pattern, opts);
            return re.Match(text).IsSuccess;
        }

        [TestCase(@"^\p{Ascii}", "X", "ÿ", Description = @"ASCII")]
        [TestCase(@"^\p{Block=BasicLatin}", "X", "ÿ", Description = @"Named block")]
        [TestCase(@"^\N", "\x2028", "X", Description = @"Newline")]
        [TestCase(@"^\d", "9", "X", Description = @"Digit")]
        [TestCase(@"^\D", "X", "9", Description = @"Not Digit")]
        [TestCase(@"^\s", "\x85", "X", Description = @"Space")]
        [TestCase(@"^\S", "X", "\x85", Description = @"Not Space")]
        [TestCase(@"^\w", "_", "\"", Description = @"Word")]
        [TestCase(@"^\W", "\"", "_", Description = @"Not Word")]
        [TestCase(@"^\p{Graph}", "\"", "\x9", Description = @"Graph")]
        [TestCase(@"^\p{Alpha}", "\x1bb", "_", Description = @"Alpha")]
        [TestCase(@"^\p{Lc}", "\x1c5", "\x1bb", Description = @"Cased")]
        [TestCase(@"^\p{Upper}", "A", "\x1c5", Description = @"Upper")]
        [TestCase(@"^\p{Lower}", "a", "\x1c5", Description = @"Lower")]
        [TestCase(@"^\p{DI}", "\x34f", "X", Description = @"Ignorable")]
        [TestCase(@"^\p{NChar}", "\U0001fffe", "X", Description = @"NChar")]
        [TestCase(@"^\p{C}", "\x7f", "X", Description = @"GC=Other")]
        [TestCase(@"^\p{L}", "p", "9", Description = @"GC=Letter")]
        [TestCase(@"^\p{M}", "\x30f", "X", Description = @"GC=Mark")]
        [TestCase(@"^\p{N}", "9", "X", Description = @"GC=Number")]
        [TestCase(@"^\p{P}", "\x066c", "X", Description = @"GC=Punctuation")]
        [TestCase(@"^\p{S}", "$", "X", Description = @"GC=Symbol")]
        [TestCase(@"^\p{Z}", " ", "X", Description = @"GC=Separator")]
        [TestCase(@"^\P{C}", "X", "\x7f", Description = @"Not GC=Other")]
        [TestCase(@"^\P{L}", "9", "p", Description = @"Not GC=Letter")]
        [TestCase(@"^\P{M}", "X", "\x30f", Description = @"Not GC=Mark")]
        [TestCase(@"^\P{N}", "X", "9", Description = @"Not GC=Number")]
        [TestCase(@"^\P{P}", "X", "\x066c", Description = @"Not GC=Punctuation")]
        [TestCase(@"^\P{S}", "X", "$", Description = @"Not GC=Symbol")]
        [TestCase(@"^\P{Z}", "X", " ", Description = @"Not GC=Separator")]
        [TestCase(@"^\p{Cc}", "\x17", "\x600", Description = @"GC=Control")]
        [TestCase(@"^\p{Cf}", "\x601", "\x9f", Description = @"GC=Format")]
        [TestCase(@"^\p{Cn}", "\U000e0000", "\x9f", Description = @"GC=OtherNotAssigned")]
        [TestCase(@"^\p{Co}", "\xf8ff", "\x9f", Description = @"GC=PrivateUse")]
        //[TestCase(@"^\p{Cs}", "\xDF02\xD800", "\x9f", Description = @"GC=Surrogate")]
        [TestCase(@"^\p{Lu}", "A", "z", Description = @"GC=UppercaseLetter")]
        [TestCase(@"^\p{Ll}", "a", "Z", Description = @"GC=LowercaseLetter")]
        [TestCase(@"^\p{Lt}", "\x1c5", "a", Description = @"GC=TitlecaseLetter")]
        [TestCase(@"^\p{Lm}", "\x2b0", "a", Description = @"GC=ModifierLetter")]
        [TestCase(@"^\p{Lo}", "\x1bb", "a", Description = @"GC=OtherLetter")]
        [TestCase(@"^\p{Mc}", "\x903", "X", Description = @"GC=SpacingCombiningMark")]
        [TestCase(@"^\p{Mn}", "\x300", "X", Description = @"GC=NonSpacingMark")]
        [TestCase(@"^\p{Me}", "\x488", "X", Description = @"GC=EnclosingMark")]
        [TestCase(@"^\p{Nd}", "0", "X", Description = @"GC=DecimalDigitNumber")]
        [TestCase(@"^\p{Nl}", "\x16ee", "X", Description = @"GC=LetterNumber")]
        [TestCase(@"^\p{No}", "\xb3", "X", Description = @"GC=OtherNumber")]
        [TestCase(@"^\p{Pc}", "_", "X", Description = @"GC=ConnectorPunctuation")]
        [TestCase(@"^\p{Pd}", "-", "X", Description = @"GC=DashPunctuation")]
        [TestCase(@"^\p{Ps}", "(", "X", Description = @"GC=OpenPunctuation")]
        [TestCase(@"^\p{Pe}", "]", "X", Description = @"GC=ClosePunctuation")]
        [TestCase(@"^\p{Pi}", "\xab", "X", Description = @"GC=InitialQuotePunctuation")]
        [TestCase(@"^\p{Pf}", "\xbb", "X", Description = @"GC=FinalQuotePunctuation")]
        [TestCase(@"^\p{Po}", "!", "X", Description = @"GC=OtherPunctuation")]
        [TestCase(@"^\p{Sc}", "$", "X", Description = @"GC=CurrencySymbol")]
        [TestCase(@"^\p{Sm}", "+", "X", Description = @"GC=MathSymbol")]
        [TestCase(@"^\p{Sk}", "\x2c2", "X", Description = @"GC=ModifierSymbol")]
        [TestCase(@"^\p{So}", "\xa6", "X", Description = @"GC=OtherSymbol")]
        [TestCase(@"^\p{Zs}", " ", "X", Description = @"GC=OtherSymbol")]
        [TestCase(@"^\p{Zl}", "\x2028", "X", Description = @"GC=OtherSymbol")]
        [TestCase(@"^\p{Zp}", "\x2029", "X", Description = @"GC=OtherSymbol")]
        [TestCase(@"^\p{gc=Lu}", "A", "z", Description = @"Explicit GC=UppercaseLetter")]
        [TestCase(@"^\p{Han}", "\U0002f804", "X", Description = @"Han characters")]
        [TestCase(@"^\p{sc=Arabic}", "\u06e9", "\u060c", Description = @"SC=Arabic")]
        [TestCase(@"^\p{scx=Arabic}", "\u060c", "X", Description = @"SCX=Arabic")]
        public void SingleProperties(string pattern, string goodtext, string badtext)
        {
            Regex re = new Regex(pattern);
            Assert.True(re.Match(goodtext).IsSuccess);
            Assert.False(re.Match(badtext).IsSuccess);
        }

        [TestCase(@"[\p{Lu}||\p{Upper}]", @"\p{Upper}", RegexOptions.None,
            Description = "A||B = A if A contains B")]
        [TestCase(@"[\p{Upper}||\p{Lu}]", @"\p{Upper}", RegexOptions.None,
            Description = "A||B = B if B contains A")]
        [TestCase(@"[\p{Lu}&&\p{Upper}]", @"\p{Lu}", RegexOptions.None,
            Description = "A&&B = B if A contains B")]
        [TestCase(@"[\p{Upper}&&\p{Lu}]", @"\p{Lu}", RegexOptions.None,
            Description = "A&&B = A if B contains A")]
        [TestCase(@"[\p{Lu}--\p{Upper}]", @"\P{Any}", RegexOptions.None,
            Description = "A--B matches nothing if B contains A")]
        [TestCase(@"[A||\p{Upper}]", @"\p{Upper}", RegexOptions.None,
            Description = "A||B = A if A contains B")]
        [TestCase(@"[\p{Upper}||A]", @"\p{Upper}", RegexOptions.None,
            Description = "A||B = B if B contains A")]
        [TestCase(@"[A&&\p{Upper}]", @"A", RegexOptions.None,
            Description = "A&&B = B if A contains B")]
        [TestCase(@"[\p{Upper}&&A]", @"A", RegexOptions.None,
            Description = "A&&B = A if B contains A")]
        [TestCase(@"[A--\p{Upper}]", @"\P{Any}", RegexOptions.None,
            Description = "A--B matches nothing if B contains A")]
        [TestCase(@"[\p{sc=Latin}||\p{scx=Latin}]", @"\p{scx=Latin}", RegexOptions.None,
            Description = "Scx=X contains Sc=X")]
        [TestCase(@"[\p{sc=Latin}&&\p{sc=Greek}]", @"\P{Any}", RegexOptions.None,
            Description = "Different scripts don't overlap")]
        [TestCase(@"[\p{sc=Latin}||a]", @"\p{sc=Latin}", RegexOptions.None,
            Description = "Script contains codepoint")]
        [TestCase(@"[\p{sc=Latin}&&!]", @"\P{Any}", RegexOptions.None,
            Description = "Script does not contain codepoint")]
        [TestCase(@"[a||\p{scx=Latin}]", @"\p{scx=Latin}", RegexOptions.None,
            Description = "Extended script contains codepoint")]
        [TestCase(@"[!&&\p{scx=Latin}]", @"\P{Any}", RegexOptions.None,
            Description = "Extended script does not contain codepoint")]
        public void EqualExpressions(string left, string right, RegexOptions opts)
        {
            RegexAST leftAST = Parser.Parse(left, opts);
            RegexAST rightAST = Parser.Parse(right, opts);
            Assert.True(leftAST.Equals(rightAST));
        }
    }
}
