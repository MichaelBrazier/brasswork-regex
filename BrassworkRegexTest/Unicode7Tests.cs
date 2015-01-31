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
using NUnit.Framework;

namespace Brasswork.Regex.Test
{
    /// <summary>Unit tests for Unicode 7 utility functions.</summary>
    [TestFixture]
    class Unicode7Tests
    {
        [Test]
        public void UTF16ToCodepoints()
        {
            string value = "A\xD800\xDF02";
            Assert.AreEqual(value.ToCodepoint(0), 0x41, "basic char did not convert");
            Assert.AreEqual(value.ToCodepoint(1), 0x10302, "surrogate pair did not convert");
            Assert.AreEqual(value.ToCodepoint(2), 0x10302, "middle of surrogate pair did not convert");
        }

        [Test]
        public void NextChar()
        {
            string value = "a\xD800\xDF02";
            int pos = 0;
            value.NextCodepoint(ref pos);
            Assert.AreEqual(pos, 1, "did not move over basic char");
            value.NextCodepoint(ref pos);
            Assert.AreEqual(pos, 3, "did not move over surrogate pair");
        }

        [Test]
        public void PrevChar()
        {
            string value = "\xD800\xDF02a";
            int pos = 3;
            value.PrevCodepoint(ref pos);
            Assert.AreEqual(pos, 2, "did not move over basic char");
            value.PrevCodepoint(ref pos);
            Assert.AreEqual(pos, 0, "did not move over surrogate pair");
        }

        [Test]
        public void CharToUTF16()
        {
            Assert.AreEqual(Unicode7.ToUTF16String(0x41), "A", "basic char did not convert");
            Assert.AreEqual(Unicode7.ToUTF16String(0x10302), "\xD800\xDF02", "astral char did not convert");
            Assert.AreEqual(Unicode7.ToUTF16String(0x111000), "\xFFFD", "int too large for Unicode was not replaced");
        }

        [Test]
        public void CanonPropertyValue()
        {
            Assert.AreEqual(Unicode7.ToCanonPropertyValue("Unified  Canadian_Aboriginal-Syllabics"),
                "unifiedcanadianaboriginalsyllabics");
        }

        [Test]
        public void FindBlock()
        {
            Unicode7.Range? range = Unicode7.FindBlock("Braille");
            Assert.IsNotNull(range, "did not find named block");
            range = Unicode7.FindBlock("Quenya");
            Assert.IsNull(range, "returned spurious block");
        }

        [Test]
        public void CategoryName()
        {
            UnicodeCategory? cat = Unicode7.CategoryName("Connector Punctuation");
            Assert.IsNotNull(cat, "did not find category name");
            cat = Unicode7.CategoryName("Diacritical Mark");
            Assert.IsNull(cat, "returned spurious category name");
        }

        [Test]
        public void GetCategory()
        {
            int codepoint = 0xA66F;
            Assert.AreEqual(codepoint.GetUnicodeCategory(), UnicodeCategory.NonSpacingMark);
        }

        [Test]
        public void AbbreviateCategory()
        {
            Assert.AreEqual(Unicode7.Abbreviation(UnicodeCategory.ModifierLetter), "Lm");
        }

        [Test]
        public void ScriptName()
        {
            Unicode7.Script? script = Unicode7.ScriptName("Egyptian Hieroglyphs");
            Assert.IsNotNull(script, "did not find script name");
            script = Unicode7.ScriptName("Quenya");
            Assert.IsNull(script, "returned spurious script name");
        }

        [Test]
        public void GetScript()
        {
            int codepoint = 0xA66F;
            Assert.AreEqual(codepoint.GetScript(), Unicode7.Script.Cyrillic);
        }

        [Test]
        public void AbbreviateScript()
        {
            Assert.AreEqual(Unicode7.Abbreviation(Unicode7.Script.Khudawadi), "Sind");
        }

        [Test]
        public void ExtendedScript()
        {
            int latinA = 0x41;
            int arabComma = 0x60C;
            Assert.True(arabComma.IsInExtendedScript(Unicode7.Script.Arabic), "did not find extended script");
            Assert.True(latinA.IsInExtendedScript(Unicode7.Script.Latin),
                "Script Extensions does not fall back to Script");
        }

        [Test]
        public void PropertyName()
        {
            Unicode7.Property? prop = Unicode7.PropertyName("Cased Letter");
            Assert.IsNotNull(prop, "did not find property name");
            prop = Unicode7.PropertyName("Foreign");
            Assert.IsNull(prop, "returned spurious property name");
        }

        [Test]
        public void IsNewline()
        {
            string chars = "a\n\xa\xb\xc\xd\x85\x2028\x2029";
            Assert.False(chars.IsNewline(0), "spurious newline detected");
            Assert.True(chars.IsNewline(1), "newline not detected");
            Assert.True(chars.IsNewline(2), "0xa not detected");
            Assert.True(chars.IsNewline(3), "0xb not detected");
            Assert.True(chars.IsNewline(4), "0xc not detected");
            Assert.True(chars.IsNewline(5), "0xd not detected");
            Assert.True(chars.IsNewline(6), "0x85 not detected");
            Assert.True(chars.IsNewline(7), "0x2028 not detected");
            Assert.True(chars.IsNewline(8), "0x2029 not detected");
        }

        [Test]
        public void IsWhitespace()
        {
            string chars = "a \t\xa\xb\xc\xd\x2028\x2029";
            Assert.False(chars.IsWhitespace(0), "spurious space detected");
            Assert.True(chars.IsWhitespace(1), "space not detected");
            Assert.True(chars.IsWhitespace(2), "tab not detected");
            Assert.True(chars.IsWhitespace(3), "0xa not detected");
            Assert.True(chars.IsWhitespace(4), "0xb not detected");
            Assert.True(chars.IsWhitespace(5), "0xc not detected");
            Assert.True(chars.IsWhitespace(6), "0xd not detected");
            Assert.True(chars.IsWhitespace(7), "0x2028 not detected");
            Assert.True(chars.IsWhitespace(8), "0x2029 not detected");
        }

        [Test]
        public void IsGraphCharacter()
        {
            string chars = "a ";
            Assert.True(chars.IsGraphCharacter(0), "graph char not detected");
            Assert.False(chars.IsGraphCharacter(1), "spurious graph char detected");
        }

        [Test]
        public void IsWordCharacter()
        {
            string chars = "a \x200C\x200D\x5B0\xAA\x2160";
            Assert.True(chars.IsWordCharacter(0), "'a' not detected");
            Assert.False(chars.IsWordCharacter(1), "spurious word char detected");
            Assert.True(chars.IsWordCharacter(2), "zero-width non-joiner not detected");
            Assert.True(chars.IsWordCharacter(3), "zero-width joiner not detected");
            Assert.True(chars.IsWordCharacter(4), "other-alphabetic not detected");
            Assert.True(chars.IsWordCharacter(5), "other-uppercase not detected");
            Assert.True(chars.IsWordCharacter(6), "other-lowercase not detected");
        }

        [Test]
        public void IsAlphabetic()
        {
            string chars = "a \x200C\x5B0\xAA\x2160";
            Assert.True(chars.IsAlphabetic(0), "'a' not detected");
            Assert.False(chars.IsAlphabetic(1), "space wrongly detected");
            Assert.False(chars.IsAlphabetic(2), "zero-width non-joiner wrongly detected");
            Assert.True(chars.IsAlphabetic(3), "other-alphabetic not detected");
            Assert.True(chars.IsAlphabetic(4), "other-lowercase not detected");
            Assert.True(chars.IsAlphabetic(5), "other-uppercase not detected");
        }

        [Test]
        public void IsCased()
        {
            string chars = " Aa\x5B0\xAA\x2160";
            Assert.False(chars.IsCased(0), "space wrongly detected");
            Assert.True(chars.IsCased(1), "'A' not detected");
            Assert.True(chars.IsCased(2), "'a' not detected");
            Assert.False(chars.IsCased(3), "other-alphabetic wrongly detected");
            Assert.True(chars.IsCased(4), "other-lowercase not detected");
            Assert.True(chars.IsCased(5), "other-uppercase not detected");
        }

        [Test]
        public void IsUppercase()
        {
            string chars = " Aa\x5B0\xAA\x2160";
            Assert.False(chars.IsUppercase(0), "space wrongly detected");
            Assert.True(chars.IsUppercase(1), "'A' not detected");
            Assert.False(chars.IsUppercase(2), "'a' wrongly detected");
            Assert.False(chars.IsUppercase(3), "other-alphabetic wrongly detected");
            Assert.False(chars.IsUppercase(4), "other-lowercase wrongly detected");
            Assert.True(chars.IsUppercase(5), "other-uppercase not detected");
        }

        [Test]
        public void IsLowercase()
        {
            string chars = " Aa\x5B0\xAA\x2160";
            Assert.False(chars.IsLowercase(0), "space wrongly detected");
            Assert.False(chars.IsLowercase(1), "'A' wrongly detected");
            Assert.True(chars.IsLowercase(2), "'a' not detected");
            Assert.False(chars.IsLowercase(3), "other-alphabetic wrongly detected");
            Assert.True(chars.IsLowercase(4), "other-lowercase not detected");
            Assert.False(chars.IsLowercase(5), "other-uppercase wrongly detected");
        }

        [Test]
        public void IsIgnorable()
        {
            string chars = "\x3164\x2160";
            Assert.True(chars.IsIgnorable(0), "ignorable character not detected");
            Assert.False(chars.IsIgnorable(1), "non-ignorable character wrongly detected");
        }

        [Test]
        public void IsNoncharacter()
        {
            string chars = "\xFDD0\xFDE0\xFFFE\xFFFF";
            Assert.True(chars.IsNoncharacter(0));
            Assert.True(chars.IsNoncharacter(1));
            Assert.True(chars.IsNoncharacter(2));
            Assert.True(chars.IsNoncharacter(3));
        }

        [Test]
        public void IsLetter()
        {
            string chars = "\x41\x487\x30\x21\x2B\x20\x9";
            Assert.True(chars.IsLetter(0));
            Assert.False(chars.IsLetter(1));
            Assert.False(chars.IsLetter(2));
            Assert.False(chars.IsLetter(3));
            Assert.False(chars.IsLetter(4));
            Assert.False(chars.IsLetter(5));
            Assert.False(chars.IsLetter(6));
        }

        [Test]
        public void IsMark()
        {
            string chars = "\x41\x487\x30\x21\x2B\x20\x9";
            Assert.False(chars.IsMark(0));
            Assert.True(chars.IsMark(1));
            Assert.False(chars.IsMark(2));
            Assert.False(chars.IsMark(3));
            Assert.False(chars.IsMark(4));
            Assert.False(chars.IsMark(5));
            Assert.False(chars.IsMark(6));
        }

        [Test]
        public void IsNumber()
        {
            string chars = "\x41\x487\x30\x21\x2B\x20\x9";
            Assert.False(chars.IsNumber(0));
            Assert.False(chars.IsNumber(1));
            Assert.True(chars.IsNumber(2));
            Assert.False(chars.IsNumber(3));
            Assert.False(chars.IsNumber(4));
            Assert.False(chars.IsNumber(5));
            Assert.False(chars.IsNumber(6));
        }

        [Test]
        public void IsPunctuation()
        {
            string chars = "\x41\x487\x30\x21\x2B\x20\x9";
            Assert.False(chars.IsPunctuation(0));
            Assert.False(chars.IsPunctuation(1));
            Assert.False(chars.IsPunctuation(2));
            Assert.True(chars.IsPunctuation(3));
            Assert.False(chars.IsPunctuation(4));
            Assert.False(chars.IsPunctuation(5));
            Assert.False(chars.IsPunctuation(6));
        }

        [Test]
        public void IsSymbol()
        {
            string chars = "\x41\x487\x30\x21\x2B\x20\x9";
            Assert.False(chars.IsSymbol(0));
            Assert.False(chars.IsSymbol(1));
            Assert.False(chars.IsSymbol(2));
            Assert.False(chars.IsSymbol(3));
            Assert.True(chars.IsSymbol(4));
            Assert.False(chars.IsSymbol(5));
            Assert.False(chars.IsSymbol(6));
        }

        [Test]
        public void IsSeparator()
        {
            string chars = "\x41\x487\x30\x21\x2B\x20\x9";
            Assert.False(chars.IsSeparator(0));
            Assert.False(chars.IsSeparator(1));
            Assert.False(chars.IsSeparator(2));
            Assert.False(chars.IsSeparator(3));
            Assert.False(chars.IsSeparator(4));
            Assert.True(chars.IsSeparator(5));
            Assert.False(chars.IsSeparator(6));
        }

        [Test]
        public void IsOther()
        {
            string chars = "\x41\x487\x30\x21\x2B\x20\x9";
            Assert.False(chars.IsOther(0));
            Assert.False(chars.IsOther(1));
            Assert.False(chars.IsOther(2));
            Assert.False(chars.IsOther(3));
            Assert.False(chars.IsOther(4));
            Assert.False(chars.IsOther(5));
            Assert.True(chars.IsOther(6));
        }

        [Test]
        public void IsLineBegin()
        {
            string chars = "\x2028ab\r\nc";
            Assert.True(chars.IsLineBegin(0), "missed start of text");
            Assert.True(chars.IsLineBegin(1), "missed empty line");
            Assert.False(chars.IsLineBegin(2), "broke between characters");
            Assert.False(chars.IsLineBegin(3), "broke at end of line");
            Assert.False(chars.IsLineBegin(4), "broke Windows newline pair");
            Assert.True(chars.IsLineBegin(5), "missed start of line");
            Assert.False(chars.IsLineBegin(6), "broke at end of text");
        }

        [Test]
        public void IsLineEnd()
        {
            string chars = "\x2028ab\r\n";
            Assert.False(chars.IsLineEnd(-1), "broke at start of text");
            Assert.True(chars.IsLineEnd(0), "missed empty line");
            Assert.False(chars.IsLineEnd(1), "broke at start of line");
            Assert.False(chars.IsLineEnd(2), "broke between characters");
            Assert.True(chars.IsLineEnd(3), "missed end of line");
            Assert.False(chars.IsLineEnd(4), "broke Windows newline pair");
            Assert.True(chars.IsLineEnd(6), "missed end of text");
        }

        [Test]
        public void SimpleWordBreak()
        {
            string chars = "ab c";
            Assert.True(chars.SimpleWordBreak(0), "missed start of text");
            Assert.False(chars.SimpleWordBreak(1), "broke between characters");
            Assert.True(chars.SimpleWordBreak(2), "missed end of word");
            Assert.True(chars.SimpleWordBreak(3), "missed start of word");
            Assert.True(chars.SimpleWordBreak(4), "missed end of text");
        }

        [Test]
        public void DefaultWordBreak()
        {
            string chars = "The 'quick' (\"brown\") fox\r\ncan't ju\xFEFFmp 32.3 feet,\nright?";
            //Break before: ^  ^^^    ^^^ ^      ^^^^  ^   ^    ^^         ^^   ^^   ^^ ^    ^^

            Assert.True(chars.DefaultWordBreak(0), "missed break at start");
            Assert.False(chars.DefaultWordBreak(1), "broke between letters");
            Assert.True(chars.DefaultWordBreak(3), "missed break {The| }");
            Assert.True(chars.DefaultWordBreak(4), "missed break { |'}");
            Assert.True(chars.DefaultWordBreak(5), "missed break {'|quick}");
            Assert.True(chars.DefaultWordBreak(10), "missed break {quick|'}");
            Assert.True(chars.DefaultWordBreak(11), "missed break {'| }");
            Assert.True(chars.DefaultWordBreak(12), "missed break { |(}");
            Assert.True(chars.DefaultWordBreak(13), "missed break {(|\"}");
            Assert.True(chars.DefaultWordBreak(19), "missed break {\"|brown}");
            Assert.True(chars.DefaultWordBreak(20), "missed break {brown|\"}");
            Assert.True(chars.DefaultWordBreak(21), "missed break {\"|)}");
            Assert.True(chars.DefaultWordBreak(22), "missed break {)| }");
            Assert.False(chars.DefaultWordBreak(26), "broke Windows newline pair");
            Assert.False(chars.DefaultWordBreak(30), "broke before apostrophe");
            Assert.False(chars.DefaultWordBreak(31), "broke after apostrophe");
            Assert.False(chars.DefaultWordBreak(35), "broke before format character");
            Assert.False(chars.DefaultWordBreak(36), "broke after format character");
            Assert.True(chars.DefaultWordBreak(39), "missed break { |32.3}");
            Assert.False(chars.DefaultWordBreak(40), "broke between numbers");
            Assert.False(chars.DefaultWordBreak(41), "broke before decimal point");
            Assert.False(chars.DefaultWordBreak(42), "broke after decimal point");
            Assert.True(chars.DefaultWordBreak(43), "missed break {32.3| }");
            Assert.True(chars.DefaultWordBreak(48), "missed break {feet|,}");
            Assert.True(chars.DefaultWordBreak(49), "missed break {,|\x2028}");
            Assert.True(chars.DefaultWordBreak(50), "missed break {\x2028|right}");
            Assert.True(chars.DefaultWordBreak(55), "missed break {right|?}");
            Assert.True(chars.DefaultWordBreak(56), "missed break at end");
        }

        #region Property subsets

        [Test]
        public void AnyCharSubsets()
        {
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.AnyChar), "Any contains Any");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Newline), "Any contains Newline");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Space), "Any contains Space");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Graph), "Any contains Graph");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Word), "Any contains Word");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Alpha), "Any contains Alpha");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Cased), "Any contains Cased");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Upper), "Any contains Upper");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Lower), "Any contains Lower");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Ignorable), "Any contains Ignorable");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.NonChar), "Any contains NonChar");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Letter), "Any contains Letter");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Mark), "Any contains Mark");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Separator), "Any contains Separator");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Symbol), "Any contains Symbol");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Number), "Any contains Number");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Punctuation), "Any contains Punctuation");
            Assert.True(Unicode7.Property.AnyChar.Contains(Unicode7.Property.Other), "Any contains Other");

            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.UppercaseLetter), "Any contains Lu");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.LowercaseLetter), "Any contains Ll");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.TitlecaseLetter), "Any contains Lt");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.ModifierLetter), "Any contains Lm");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.OtherLetter), "Any contains Lo");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.NonSpacingMark), "Any contains Mn");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.SpacingCombiningMark), "Any contains Mc");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.EnclosingMark), "Any contains Me");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.DecimalDigitNumber), "Any contains Nd");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.LetterNumber), "Any contains Nl");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.OtherNumber), "Any contains No");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.SpaceSeparator), "Any contains Zs");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.LineSeparator), "Any contains Zl");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.ParagraphSeparator), "Any contains Zp");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.Control), "Any contains Cc");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.Format), "Any contains Cf");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.Surrogate), "Any contains Cs");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.PrivateUse), "Any contains Co");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.ConnectorPunctuation), "Any contains Pc");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.DashPunctuation), "Any contains Pd");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.OpenPunctuation), "Any contains Ps");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.ClosePunctuation), "Any contains Pe");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.InitialQuotePunctuation), "Any contains Pi");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.FinalQuotePunctuation), "Any contains Pf");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.OtherPunctuation), "Any contains Po");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.MathSymbol), "Any contains Sm");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.CurrencySymbol), "Any contains Sc");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.ModifierSymbol), "Any contains Sk");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.OtherSymbol), "Any contains So");
            Assert.True(Unicode7.Property.AnyChar.Contains(UnicodeCategory.OtherNotAssigned), "Any contains Cn");
        }

        [Test]
        public void NewlineSubsets()
        {
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.AnyChar), "Newline does not contain Any");
            Assert.True(Unicode7.Property.Newline.Contains(Unicode7.Property.Newline), "Newline contains Newline");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Space), "Newline does not contain Space");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Graph), "Newline does not contain Graph");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Word), "Newline does not contain Word");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Alpha), "Newline does not contain Alpha");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Cased), "Newline does not contain Cased");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Upper), "Newline does not contain Upper");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Lower), "Newline does not contain Lower");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Ignorable), "Newline does not contain Ignorable");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.NonChar), "Newline does not contain NonChar");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Letter), "Newline does not contain Letter");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Mark), "Newline does not contain Mark");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Separator), "Newline does not contain Separator");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Symbol), "Newline does not contain Symbol");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Number), "Newline does not contain Number");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Punctuation), "Newline does not contain Punctuation");
            Assert.False(Unicode7.Property.Newline.Contains(Unicode7.Property.Other), "Newline does not contain Other");

            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.UppercaseLetter), "Newline does not contain Lu");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.LowercaseLetter), "Newline does not contain Ll");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.TitlecaseLetter), "Newline does not contain Lt");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.ModifierLetter), "Newline does not contain Lm");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.OtherLetter), "Newline does not contain Lo");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.NonSpacingMark), "Newline does not contain Mn");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.SpacingCombiningMark), "Newline does not contain Mc");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.EnclosingMark), "Newline does not contain Me");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.DecimalDigitNumber), "Newline does not contain Nd");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.LetterNumber), "Newline does not contain Nl");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.OtherNumber), "Newline does not contain No");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.SpaceSeparator), "Newline does not contain Zs");
            Assert.True(Unicode7.Property.Newline.Contains(UnicodeCategory.LineSeparator), "Newline contains Zl");
            Assert.True(Unicode7.Property.Newline.Contains(UnicodeCategory.ParagraphSeparator), "Newline contains Zp");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.Control), "Newline does not contain Cc");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.Format), "Newline does not contain Cf");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.Surrogate), "Newline does not contain Cs");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.PrivateUse), "Newline does not contain Co");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.ConnectorPunctuation), "Newline does not contain Pc");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.DashPunctuation), "Newline does not contain Pd");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.OpenPunctuation), "Newline does not contain Ps");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.ClosePunctuation), "Newline does not contain Pe");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.InitialQuotePunctuation), "Newline does not contain Pi");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.FinalQuotePunctuation), "Newline does not contain Pf");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.OtherPunctuation), "Newline does not contain Po");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.MathSymbol), "Newline does not contain Sm");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.CurrencySymbol), "Newline does not contain Sc");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.ModifierSymbol), "Newline does not contain Sk");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.OtherSymbol), "Newline does not contain So");
            Assert.False(Unicode7.Property.Newline.Contains(UnicodeCategory.OtherNotAssigned), "Newline does not contain Cn");
        }

        [Test]
        public void SpaceSubsets()
        {
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.AnyChar), "Space does not contain Any");
            Assert.True(Unicode7.Property.Space.Contains(Unicode7.Property.Newline), "Space contains Newline");
            Assert.True(Unicode7.Property.Space.Contains(Unicode7.Property.Space), "Space contains Space");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Graph), "Space does not contain Graph");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Word), "Space does not contain Word");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Alpha), "Space does not contain Alpha");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Cased), "Space does not contain Cased");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Upper), "Space does not contain Upper");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Lower), "Space does not contain Lower");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Ignorable), "Space does not contain Ignorable");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.NonChar), "Space does not contain NonChar");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Letter), "Space does not contain Letter");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Mark), "Space does not contain Mark");
            Assert.True(Unicode7.Property.Space.Contains(Unicode7.Property.Separator), "Space contains Separator");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Symbol), "Space does not contain Symbol");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Number), "Space does not contain Number");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Punctuation), "Space does not contain Punctuation");
            Assert.False(Unicode7.Property.Space.Contains(Unicode7.Property.Other), "Space does not contain Other");

            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.UppercaseLetter), "Space does not contain Lu");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.LowercaseLetter), "Space does not contain Ll");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.TitlecaseLetter), "Space does not contain Lt");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.ModifierLetter), "Space does not contain Lm");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.OtherLetter), "Space does not contain Lo");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.NonSpacingMark), "Space does not contain Mn");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.SpacingCombiningMark), "Space does not contain Mc");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.EnclosingMark), "Space does not contain Me");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.DecimalDigitNumber), "Space does not contain Nd");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.LetterNumber), "Space does not contain Nl");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.OtherNumber), "Space does not contain No");
            Assert.True(Unicode7.Property.Space.Contains(UnicodeCategory.SpaceSeparator), "Space contains Zs");
            Assert.True(Unicode7.Property.Space.Contains(UnicodeCategory.LineSeparator), "Space contains Zl");
            Assert.True(Unicode7.Property.Space.Contains(UnicodeCategory.ParagraphSeparator), "Space contains Zp");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.Control), "Space does not contain Cc");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.Format), "Space does not contain Cf");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.Surrogate), "Space does not contain Cs");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.PrivateUse), "Space does not contain Co");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.ConnectorPunctuation), "Space does not contain Pc");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.DashPunctuation), "Space does not contain Pd");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.OpenPunctuation), "Space does not contain Ps");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.ClosePunctuation), "Space does not contain Pe");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.InitialQuotePunctuation), "Space does not contain Pi");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.FinalQuotePunctuation), "Space does not contain Pf");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.OtherPunctuation), "Space does not contain Po");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.MathSymbol), "Space does not contain Sm");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.CurrencySymbol), "Space does not contain Sc");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.ModifierSymbol), "Space does not contain Sk");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.OtherSymbol), "Space does not contain So");
            Assert.False(Unicode7.Property.Space.Contains(UnicodeCategory.OtherNotAssigned), "Space does not contain Cn");
        }

        [Test]
        public void GraphCharSubsets()
        {
            Assert.False(Unicode7.Property.Graph.Contains(Unicode7.Property.AnyChar), "Graph does not contain Any");
            Assert.False(Unicode7.Property.Graph.Contains(Unicode7.Property.Newline), "Graph does not contain Newline");
            Assert.False(Unicode7.Property.Graph.Contains(Unicode7.Property.Space), "Graph does not contain Space");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Graph), "Graph contains Graph");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Word), "Graph contains Word");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Alpha), "Graph contains Alpha");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Cased), "Graph contains Cased");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Upper), "Graph contains Upper");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Lower), "Graph contains Lower");
            Assert.False(Unicode7.Property.Graph.Contains(Unicode7.Property.Ignorable), "Graph does not contain Ignorable");
            Assert.False(Unicode7.Property.Graph.Contains(Unicode7.Property.NonChar), "Graph does not contain NonChar");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Letter), "Graph contains Letter");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Mark), "Graph contains Mark");
            Assert.False(Unicode7.Property.Graph.Contains(Unicode7.Property.Separator), "Graph does not contain Separator");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Symbol), "Graph contains Symbol");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Number), "Graph contains Number");
            Assert.True(Unicode7.Property.Graph.Contains(Unicode7.Property.Punctuation), "Graph contains Punctuation");
            Assert.False(Unicode7.Property.Graph.Contains(Unicode7.Property.Other), "Graph does not contain Other");

            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.UppercaseLetter), "Graph contains Lu");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.LowercaseLetter), "Graph contains Ll");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.TitlecaseLetter), "Graph contains Lt");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.ModifierLetter), "Graph contains Lm");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.OtherLetter), "Graph contains Lo");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.NonSpacingMark), "Graph contains Mn");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.SpacingCombiningMark), "Graph contains Mc");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.EnclosingMark), "Graph contains Me");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.DecimalDigitNumber), "Graph contains Nd");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.LetterNumber), "Graph contains Nl");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.OtherNumber), "Graph contains No");
            Assert.False(Unicode7.Property.Graph.Contains(UnicodeCategory.SpaceSeparator), "Graph does not contain Zs");
            Assert.False(Unicode7.Property.Graph.Contains(UnicodeCategory.LineSeparator), "Graph does not contain Zl");
            Assert.False(Unicode7.Property.Graph.Contains(UnicodeCategory.ParagraphSeparator), "Graph does not contain Zp");
            Assert.False(Unicode7.Property.Graph.Contains(UnicodeCategory.Control), "Graph does not contain Cc");
            Assert.False(Unicode7.Property.Graph.Contains(UnicodeCategory.Format), "Graph does not contain Cf");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.Surrogate), "Graph contains Cs");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.PrivateUse), "Graph contains Co");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.ConnectorPunctuation), "Graph contains Pc");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.DashPunctuation), "Graph contains Pd");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.OpenPunctuation), "Graph contains Ps");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.ClosePunctuation), "Graph contains Pe");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.InitialQuotePunctuation), "Graph contains Pi");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.FinalQuotePunctuation), "Graph contains Pf");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.OtherPunctuation), "Graph contains Po");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.MathSymbol), "Graph contains Sm");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.CurrencySymbol), "Graph contains Sc");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.ModifierSymbol), "Graph contains Sk");
            Assert.True(Unicode7.Property.Graph.Contains(UnicodeCategory.OtherSymbol), "Graph contains So");
            Assert.False(Unicode7.Property.Graph.Contains(UnicodeCategory.OtherNotAssigned), "Graph does not contain Cn");
        }

        [Test]
        public void WordCharSubsets()
        {
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.AnyChar), "Word does not contain Any");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Newline), "Word does not contain Newline");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Space), "Word does not contain Space");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Graph), "Word does not contain Graph");
            Assert.True(Unicode7.Property.Word.Contains(Unicode7.Property.Word), "Word contains Word");
            Assert.True(Unicode7.Property.Word.Contains(Unicode7.Property.Alpha), "Word contains Alpha");
            Assert.True(Unicode7.Property.Word.Contains(Unicode7.Property.Cased), "Word contains Cased");
            Assert.True(Unicode7.Property.Word.Contains(Unicode7.Property.Upper), "Word contains Upper");
            Assert.True(Unicode7.Property.Word.Contains(Unicode7.Property.Lower), "Word contains Lower");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Ignorable), "Word does not contain Ignorable");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.NonChar), "Word does not contain NonChar");
            Assert.True(Unicode7.Property.Word.Contains(Unicode7.Property.Letter), "Word contains Letter");
            Assert.True(Unicode7.Property.Word.Contains(Unicode7.Property.Mark), "Word contains Mark");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Separator), "Word does not contain Separator");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Symbol), "Word does not contain Symbol");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Number), "Word does not contain Number");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Punctuation), "Word does not contain Punctuation");
            Assert.False(Unicode7.Property.Word.Contains(Unicode7.Property.Other), "Word does not contain Other");

            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.UppercaseLetter), "Word contains Lu");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.LowercaseLetter), "Word contains Ll");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.TitlecaseLetter), "Word contains Lt");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.ModifierLetter), "Word contains Lm");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.OtherLetter), "Word contains Lo");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.NonSpacingMark), "Word contains Mn");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.SpacingCombiningMark), "Word contains Mc");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.EnclosingMark), "Word contains Me");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.DecimalDigitNumber), "Word contains Nd");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.LetterNumber), "Word contains Nl");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.OtherNumber), "Word does not contain No");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.SpaceSeparator), "Word does not contain Zs");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.LineSeparator), "Word does not contain Zl");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.ParagraphSeparator), "Word does not contain Zp");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.Control), "Word does not contain Cc");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.Format), "Word does not contain Cf");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.Surrogate), "Word does not contain Cs");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.PrivateUse), "Word does not contain Co");
            Assert.True(Unicode7.Property.Word.Contains(UnicodeCategory.ConnectorPunctuation), "Word contains Pc");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.DashPunctuation), "Word does not contain Pd");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.OpenPunctuation), "Word does not contain Ps");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.ClosePunctuation), "Word does not contain Pe");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.InitialQuotePunctuation), "Word does not contain Pi");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.FinalQuotePunctuation), "Word does not contain Pf");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.OtherPunctuation), "Word does not contain Po");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.MathSymbol), "Word does not contain Sm");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.CurrencySymbol), "Word does not contain Sc");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.ModifierSymbol), "Word does not contain Sk");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.OtherSymbol), "Word does not contain So");
            Assert.False(Unicode7.Property.Word.Contains(UnicodeCategory.OtherNotAssigned), "Word does not contain Cn");
        }

        [Test]
        public void AlphaCharSubsets()
        {
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.AnyChar), "Alpha does not contain Any");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Newline), "Alpha does not contain Newline");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Space), "Alpha does not contain Space");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Graph), "Alpha does not contain Graph");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Word), "Alpha does not contain Word");
            Assert.True(Unicode7.Property.Alpha.Contains(Unicode7.Property.Alpha), "Alpha contains Alpha");
            Assert.True(Unicode7.Property.Alpha.Contains(Unicode7.Property.Cased), "Alpha contains Cased");
            Assert.True(Unicode7.Property.Alpha.Contains(Unicode7.Property.Upper), "Alpha contains Upper");
            Assert.True(Unicode7.Property.Alpha.Contains(Unicode7.Property.Lower), "Alpha contains Lower");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Ignorable), "Alpha does not contain Ignorable");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.NonChar), "Alpha does not contain NonChar");
            Assert.True(Unicode7.Property.Alpha.Contains(Unicode7.Property.Letter), "Alpha contains Letter");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Mark), "Alpha does not contain Mark");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Separator), "Alpha does not contain Separator");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Symbol), "Alpha does not contain Symbol");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Number), "Alpha does not contain Number");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Punctuation), "Alpha does not contain Punctuation");
            Assert.False(Unicode7.Property.Alpha.Contains(Unicode7.Property.Other), "Alpha does not contain Other");

            Assert.True(Unicode7.Property.Alpha.Contains(UnicodeCategory.UppercaseLetter), "Alpha contains Lu");
            Assert.True(Unicode7.Property.Alpha.Contains(UnicodeCategory.LowercaseLetter), "Alpha contains Ll");
            Assert.True(Unicode7.Property.Alpha.Contains(UnicodeCategory.TitlecaseLetter), "Alpha contains Lt");
            Assert.True(Unicode7.Property.Alpha.Contains(UnicodeCategory.ModifierLetter), "Alpha contains Lm");
            Assert.True(Unicode7.Property.Alpha.Contains(UnicodeCategory.OtherLetter), "Alpha contains Lo");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.NonSpacingMark), "Alpha does not contain Mn");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.SpacingCombiningMark), "Alpha does not contain Mc");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.EnclosingMark), "Alpha does not contain Me");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.DecimalDigitNumber), "Alpha does not contain Nd");
            Assert.True(Unicode7.Property.Alpha.Contains(UnicodeCategory.LetterNumber), "Alpha contains Nl");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.OtherNumber), "Alpha does not contain No");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.SpaceSeparator), "Alpha does not contain Zs");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.LineSeparator), "Alpha does not contain Zl");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.ParagraphSeparator), "Alpha does not contain Zp");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.Control), "Alpha does not contain Cc");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.Format), "Alpha does not contain Cf");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.Surrogate), "Alpha does not contain Cs");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.PrivateUse), "Alpha does not contain Co");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.ConnectorPunctuation), "Alpha does not contain Pc");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.DashPunctuation), "Alpha does not contain Pd");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.OpenPunctuation), "Alpha does not contain Ps");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.ClosePunctuation), "Alpha does not contain Pe");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.InitialQuotePunctuation), "Alpha does not contain Pi");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.FinalQuotePunctuation), "Alpha does not contain Pf");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.OtherPunctuation), "Alpha does not contain Po");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.MathSymbol), "Alpha does not contain Sm");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.CurrencySymbol), "Alpha does not contain Sc");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.ModifierSymbol), "Alpha does not contain Sk");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.OtherSymbol), "Alpha does not contain So");
            Assert.False(Unicode7.Property.Alpha.Contains(UnicodeCategory.OtherNotAssigned), "Alpha does not contain Cn");
        }

        [Test]
        public void CasedLetterSubsets()
        {
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.AnyChar), "Cased does not contain Any");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Newline), "Cased does not contain Newline");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Space), "Cased does not contain Space");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Graph), "Cased does not contain Graph");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Word), "Cased does not contain Word");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Alpha), "Cased does not contain Alpha");
            Assert.True(Unicode7.Property.Cased.Contains(Unicode7.Property.Cased), "Cased contains Cased");
            Assert.True(Unicode7.Property.Cased.Contains(Unicode7.Property.Upper), "Cased contains Upper");
            Assert.True(Unicode7.Property.Cased.Contains(Unicode7.Property.Lower), "Cased contains Lower");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Ignorable), "Cased does not contain Ignorable");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.NonChar), "Cased does not contain NonChar");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Letter), "Cased does not contain Letter");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Mark), "Cased does not contain Mark");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Separator), "Cased does not contain Separator");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Symbol), "Cased does not contain Symbol");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Number), "Cased does not contain Number");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Punctuation), "Cased does not contain Punctuation");
            Assert.False(Unicode7.Property.Cased.Contains(Unicode7.Property.Other), "Cased does not contain Other");

            Assert.True(Unicode7.Property.Cased.Contains(UnicodeCategory.UppercaseLetter), "Cased contains Lu");
            Assert.True(Unicode7.Property.Cased.Contains(UnicodeCategory.LowercaseLetter), "Cased contains Ll");
            Assert.True(Unicode7.Property.Cased.Contains(UnicodeCategory.TitlecaseLetter), "Cased contains Lt");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.ModifierLetter), "Cased does not contain Lm");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.OtherLetter), "Cased does not contain Lo");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.NonSpacingMark), "Cased does not contain Mn");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.SpacingCombiningMark), "Cased does not contain Mc");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.EnclosingMark), "Cased does not contain Me");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.DecimalDigitNumber), "Cased does not contain Nd");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.LetterNumber), "Cased does not contain Nl");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.OtherNumber), "Cased does not contain No");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.SpaceSeparator), "Cased does not contain Zs");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.LineSeparator), "Cased does not contain Zl");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.ParagraphSeparator), "Cased does not contain Zp");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.Control), "Cased does not contain Cc");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.Format), "Cased does not contain Cf");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.Surrogate), "Cased does not contain Cs");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.PrivateUse), "Cased does not contain Co");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.ConnectorPunctuation), "Cased does not contain Pc");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.DashPunctuation), "Cased does not contain Pd");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.OpenPunctuation), "Cased does not contain Ps");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.ClosePunctuation), "Cased does not contain Pe");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.InitialQuotePunctuation), "Cased does not contain Pi");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.FinalQuotePunctuation), "Cased does not contain Pf");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.OtherPunctuation), "Cased does not contain Po");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.MathSymbol), "Cased does not contain Sm");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.CurrencySymbol), "Cased does not contain Sc");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.ModifierSymbol), "Cased does not contain Sk");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.OtherSymbol), "Cased does not contain So");
            Assert.False(Unicode7.Property.Cased.Contains(UnicodeCategory.OtherNotAssigned), "Cased does not contain Cn");
        }

        [Test]
        public void UppercaseSubsets()
        {
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.AnyChar), "Upper does not contain Any");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Newline), "Upper does not contain Newline");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Space), "Upper does not contain Space");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Graph), "Upper does not contain Graph");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Word), "Upper does not contain Word");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Alpha), "Upper does not contain Alpha");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Cased), "Upper does not contain Cased");
            Assert.True(Unicode7.Property.Upper.Contains(Unicode7.Property.Upper), "Upper contains Upper");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Lower), "Upper does not contain Lower");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Ignorable), "Upper does not contain Ignorable");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.NonChar), "Upper does not contain NonChar");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Letter), "Upper does not contain Letter");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Mark), "Upper does not contain Mark");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Separator), "Upper does not contain Separator");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Symbol), "Upper does not contain Symbol");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Number), "Upper does not contain Number");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Punctuation), "Upper does not contain Punctuation");
            Assert.False(Unicode7.Property.Upper.Contains(Unicode7.Property.Other), "Upper does not contain Other");

            Assert.True(Unicode7.Property.Upper.Contains(UnicodeCategory.UppercaseLetter), "Upper contains Lu");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.LowercaseLetter), "Upper does not contain Ll");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.TitlecaseLetter), "Upper does not contain Lt");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.ModifierLetter), "Upper does not contain Lm");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.OtherLetter), "Upper does not contain Lo");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.NonSpacingMark), "Upper does not contain Mn");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.SpacingCombiningMark), "Upper does not contain Mc");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.EnclosingMark), "Upper does not contain Me");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.DecimalDigitNumber), "Upper does not contain Nd");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.LetterNumber), "Upper contains Nl");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.OtherNumber), "Upper does not contain No");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.SpaceSeparator), "Upper does not contain Zs");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.LineSeparator), "Upper does not contain Zl");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.ParagraphSeparator), "Upper does not contain Zp");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.Control), "Upper does not contain Cc");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.Format), "Upper does not contain Cf");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.Surrogate), "Upper does not contain Cs");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.PrivateUse), "Upper does not contain Co");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.ConnectorPunctuation), "Upper does not contain Pc");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.DashPunctuation), "Upper does not contain Pd");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.OpenPunctuation), "Upper does not contain Ps");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.ClosePunctuation), "Upper does not contain Pe");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.InitialQuotePunctuation), "Upper does not contain Pi");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.FinalQuotePunctuation), "Upper does not contain Pf");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.OtherPunctuation), "Upper does not contain Po");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.MathSymbol), "Upper does not contain Sm");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.CurrencySymbol), "Upper does not contain Sc");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.ModifierSymbol), "Upper does not contain Sk");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.OtherSymbol), "Upper does not contain So");
            Assert.False(Unicode7.Property.Upper.Contains(UnicodeCategory.OtherNotAssigned), "Upper does not contain Cn");
        }

        [Test]
        public void LowercaseSubsets()
        {
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.AnyChar), "Lower does not contain Any");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Newline), "Lower does not contain Newline");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Space), "Lower does not contain Space");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Graph), "Lower does not contain Graph");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Word), "Lower does not contain Word");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Alpha), "Lower does not contain Alpha");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Cased), "Lower does not contain Cased");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Upper), "Lower does not contain Upper");
            Assert.True(Unicode7.Property.Lower.Contains(Unicode7.Property.Lower), "Lower contains Lower");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Ignorable), "Lower does not contain Ignorable");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.NonChar), "Lower does not contain NonChar");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Letter), "Lower does not contain Letter");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Mark), "Lower does not contain Mark");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Separator), "Lower does not contain Separator");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Symbol), "Lower does not contain Symbol");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Number), "Lower does not contain Number");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Punctuation), "Lower does not contain Punctuation");
            Assert.False(Unicode7.Property.Lower.Contains(Unicode7.Property.Other), "Lower does not contain Other");

            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.UppercaseLetter), "Lower does not contain Lu");
            Assert.True(Unicode7.Property.Lower.Contains(UnicodeCategory.LowercaseLetter), "Lower contains Ll");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.TitlecaseLetter), "Lower does not contain Lt");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.ModifierLetter), "Lower does not contain Lm");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.OtherLetter), "Lower does not contain Lo");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.NonSpacingMark), "Lower does not contain Mn");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.SpacingCombiningMark), "Lower does not contain Mc");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.EnclosingMark), "Lower does not contain Me");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.DecimalDigitNumber), "Lower does not contain Nd");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.LetterNumber), "Lower contains Nl");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.OtherNumber), "Lower does not contain No");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.SpaceSeparator), "Lower does not contain Zs");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.LineSeparator), "Lower does not contain Zl");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.ParagraphSeparator), "Lower does not contain Zp");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.Control), "Lower does not contain Cc");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.Format), "Lower does not contain Cf");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.Surrogate), "Lower does not contain Cs");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.PrivateUse), "Lower does not contain Co");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.ConnectorPunctuation), "Lower does not contain Pc");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.DashPunctuation), "Lower does not contain Pd");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.OpenPunctuation), "Lower does not contain Ps");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.ClosePunctuation), "Lower does not contain Pe");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.InitialQuotePunctuation), "Lower does not contain Pi");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.FinalQuotePunctuation), "Lower does not contain Pf");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.OtherPunctuation), "Lower does not contain Po");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.MathSymbol), "Lower does not contain Sm");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.CurrencySymbol), "Lower does not contain Sc");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.ModifierSymbol), "Lower does not contain Sk");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.OtherSymbol), "Lower does not contain So");
            Assert.False(Unicode7.Property.Lower.Contains(UnicodeCategory.OtherNotAssigned), "Lower does not contain Cn");
        }

        [Test]
        public void IgnorableCharSubsets()
        {
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.AnyChar), "Ignorable does not contain Any");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Newline), "Ignorable does not contain Newline");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Space), "Ignorable does not contain Space");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Graph), "Ignorable does not contain Graph");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Word), "Ignorable does not contain Word");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Alpha), "Ignorable does not contain Alpha");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Cased), "Ignorable does not contain Cased");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Upper), "Ignorable does not contain Upper");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Lower), "Ignorable does not contain Lower");
            Assert.True(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Ignorable), "Ignorable contains Ignorable");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.NonChar), "Ignorable does not contain NonChar");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Letter), "Ignorable does not contain Letter");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Mark), "Ignorable does not contain Mark");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Separator), "Ignorable does not contain Separator");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Symbol), "Ignorable does not contain Symbol");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Number), "Ignorable does not contain Number");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Punctuation), "Ignorable does not contain Punctuation");
            Assert.False(Unicode7.Property.Ignorable.Contains(Unicode7.Property.Other), "Ignorable does not contain Other");

            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.UppercaseLetter), "Ignorable does not contain Lu");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.LowercaseLetter), "Ignorable does not contain Ll");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.TitlecaseLetter), "Ignorable does not contain Lt");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.ModifierLetter), "Ignorable does not contain Lm");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.OtherLetter), "Ignorable does not contain Lo");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.NonSpacingMark), "Ignorable does not contain Mn");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.SpacingCombiningMark), "Ignorable does not contain Mc");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.EnclosingMark), "Ignorable does not contain Me");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.DecimalDigitNumber), "Ignorable does not contain Nd");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.LetterNumber), "Ignorable contains Nl");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.OtherNumber), "Ignorable does not contain No");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.SpaceSeparator), "Ignorable does not contain Zs");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.LineSeparator), "Ignorable does not contain Zl");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.ParagraphSeparator), "Ignorable does not contain Zp");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.Control), "Ignorable does not contain Cc");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.Format), "Ignorable does not contain Cf");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.Surrogate), "Ignorable does not contain Cs");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.PrivateUse), "Ignorable does not contain Co");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.ConnectorPunctuation), "Ignorable does not contain Pc");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.DashPunctuation), "Ignorable does not contain Pd");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.OpenPunctuation), "Ignorable does not contain Ps");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.ClosePunctuation), "Ignorable does not contain Pe");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.InitialQuotePunctuation), "Ignorable does not contain Pi");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.FinalQuotePunctuation), "Ignorable does not contain Pf");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.OtherPunctuation), "Ignorable does not contain Po");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.MathSymbol), "Ignorable does not contain Sm");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.CurrencySymbol), "Ignorable does not contain Sc");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.ModifierSymbol), "Ignorable does not contain Sk");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.OtherSymbol), "Ignorable does not contain So");
            Assert.False(Unicode7.Property.Ignorable.Contains(UnicodeCategory.OtherNotAssigned), "Ignorable does not contain Cn");
        }

        [Test]
        public void NoncharSubsets()
        {
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.AnyChar), "NonChar does not contain Any");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Newline), "NonChar does not contain Newline");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Space), "NonChar does not contain Space");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Graph), "NonChar does not contain Graph");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Word), "NonChar does not contain Word");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Alpha), "NonChar does not contain Alpha");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Cased), "NonChar does not contain Cased");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Upper), "NonChar does not contain Upper");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Lower), "NonChar does not contain Lower");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Ignorable), "NonChar does not contain Ignorable");
            Assert.True(Unicode7.Property.NonChar.Contains(Unicode7.Property.NonChar), "NonChar contains NonChar");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Letter), "NonChar does not contain Letter");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Mark), "NonChar does not contain Mark");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Separator), "NonChar does not contain Separator");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Symbol), "NonChar does not contain Symbol");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Number), "NonChar does not contain Number");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Punctuation), "NonChar does not contain Punctuation");
            Assert.False(Unicode7.Property.NonChar.Contains(Unicode7.Property.Other), "NonChar does not contain Other");

            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.UppercaseLetter), "NonChar does not contain Lu");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.LowercaseLetter), "NonChar does not contain Ll");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.TitlecaseLetter), "NonChar does not contain Lt");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.ModifierLetter), "NonChar does not contain Lm");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.OtherLetter), "NonChar does not contain Lo");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.NonSpacingMark), "NonChar does not contain Mn");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.SpacingCombiningMark), "NonChar does not contain Mc");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.EnclosingMark), "NonChar does not contain Me");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.DecimalDigitNumber), "NonChar does not contain Nd");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.LetterNumber), "NonChar contains Nl");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.OtherNumber), "NonChar does not contain No");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.SpaceSeparator), "NonChar does not contain Zs");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.LineSeparator), "NonChar does not contain Zl");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.ParagraphSeparator), "NonChar does not contain Zp");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.Control), "NonChar does not contain Cc");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.Format), "NonChar does not contain Cf");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.Surrogate), "NonChar does not contain Cs");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.PrivateUse), "NonChar does not contain Co");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.ConnectorPunctuation), "NonChar does not contain Pc");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.DashPunctuation), "NonChar does not contain Pd");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.OpenPunctuation), "NonChar does not contain Ps");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.ClosePunctuation), "NonChar does not contain Pe");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.InitialQuotePunctuation), "NonChar does not contain Pi");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.FinalQuotePunctuation), "NonChar does not contain Pf");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.OtherPunctuation), "NonChar does not contain Po");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.MathSymbol), "NonChar does not contain Sm");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.CurrencySymbol), "NonChar does not contain Sc");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.ModifierSymbol), "NonChar does not contain Sk");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.OtherSymbol), "NonChar does not contain So");
            Assert.False(Unicode7.Property.NonChar.Contains(UnicodeCategory.OtherNotAssigned), "NonChar does not contain Cn");
        }

        [Test]
        public void LetterSubsets()
        {
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.AnyChar), "Letter does not contain Any");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Newline), "Letter does not contain Newline");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Space), "Letter does not contain Space");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Graph), "Letter does not contain Graph");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Word), "Letter does not contain Word");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Alpha), "Letter does not contain Alpha");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Cased), "Letter contains Cased");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Upper), "Letter does not contain Upper");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Lower), "Letter does not contain Lower");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Ignorable), "Letter does not contain Ignorable");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.NonChar), "Letter does not contain NonChar");
            Assert.True(Unicode7.Property.Letter.Contains(Unicode7.Property.Letter), "Letter contains Letter");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Mark), "Letter does not contain Mark");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Separator), "Letter does not contain Separator");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Symbol), "Letter does not contain Symbol");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Number), "Letter does not contain Number");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Punctuation), "Letter does not contain Punctuation");
            Assert.False(Unicode7.Property.Letter.Contains(Unicode7.Property.Other), "Letter does not contain Other");

            Assert.True(Unicode7.Property.Letter.Contains(UnicodeCategory.UppercaseLetter), "Letter contains Lu");
            Assert.True(Unicode7.Property.Letter.Contains(UnicodeCategory.LowercaseLetter), "Letter contains Ll");
            Assert.True(Unicode7.Property.Letter.Contains(UnicodeCategory.TitlecaseLetter), "Letter contains Lt");
            Assert.True(Unicode7.Property.Letter.Contains(UnicodeCategory.ModifierLetter), "Letter contains Lm");
            Assert.True(Unicode7.Property.Letter.Contains(UnicodeCategory.OtherLetter), "Letter contains Lo");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.NonSpacingMark), "Letter does not contain Mn");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.SpacingCombiningMark), "Letter does not contain Mc");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.EnclosingMark), "Letter does not contain Me");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.DecimalDigitNumber), "Letter does not contain Nd");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.LetterNumber), "Letter does not contain Nl");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.OtherNumber), "Letter does not contain No");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.SpaceSeparator), "Letter does not contain Zs");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.LineSeparator), "Letter does not contain Zl");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.ParagraphSeparator), "Letter does not contain Zp");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.Control), "Letter does not contain Cc");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.Format), "Letter does not contain Cf");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.Surrogate), "Letter does not contain Cs");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.PrivateUse), "Letter does not contain Co");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.ConnectorPunctuation), "Letter does not contain Pc");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.DashPunctuation), "Letter does not contain Pd");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.OpenPunctuation), "Letter does not contain Ps");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.ClosePunctuation), "Letter does not contain Pe");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.InitialQuotePunctuation), "Letter does not contain Pi");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.FinalQuotePunctuation), "Letter does not contain Pf");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.OtherPunctuation), "Letter does not contain Po");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.MathSymbol), "Letter does not contain Sm");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.CurrencySymbol), "Letter does not contain Sc");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.ModifierSymbol), "Letter does not contain Sk");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.OtherSymbol), "Letter does not contain So");
            Assert.False(Unicode7.Property.Letter.Contains(UnicodeCategory.OtherNotAssigned), "Letter does not contain Cn");
        }

        [Test]
        public void MarkSubsets()
        {
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.AnyChar), "Mark does not contain Any");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Newline), "Mark does not contain Newline");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Space), "Mark does not contain Space");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Graph), "Mark does not contain Graph");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Word), "Mark does not contain Word");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Alpha), "Mark does not contain Alpha");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Cased), "Mark does not contain Cased");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Upper), "Mark does not contain Upper");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Lower), "Mark does not contain Lower");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Ignorable), "Mark does not contain Ignorable");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.NonChar), "Mark does not contain NonChar");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Letter), "Mark does not contain Letter");
            Assert.True(Unicode7.Property.Mark.Contains(Unicode7.Property.Mark), "Mark contains Mark");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Separator), "Mark does not contain Separator");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Symbol), "Mark does not contain Symbol");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Number), "Mark does not contain Number");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Punctuation), "Mark does not contain Punctuation");
            Assert.False(Unicode7.Property.Mark.Contains(Unicode7.Property.Other), "Mark does not contain Other");

            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.UppercaseLetter), "Mark does not contain Lu");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.LowercaseLetter), "Mark does not contain Ll");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.TitlecaseLetter), "Mark does not contain Lt");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.ModifierLetter), "Mark does not contain Lm");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.OtherLetter), "Mark does not contain Lo");
            Assert.True(Unicode7.Property.Mark.Contains(UnicodeCategory.NonSpacingMark), "Mark contains Mn");
            Assert.True(Unicode7.Property.Mark.Contains(UnicodeCategory.SpacingCombiningMark), "Mark contains Mc");
            Assert.True(Unicode7.Property.Mark.Contains(UnicodeCategory.EnclosingMark), "Mark contains Me");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.DecimalDigitNumber), "Mark does not contain Nd");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.LetterNumber), "Mark contains Nl");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.OtherNumber), "Mark does not contain No");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.SpaceSeparator), "Mark does not contain Zs");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.LineSeparator), "Mark does not contain Zl");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.ParagraphSeparator), "Mark does not contain Zp");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.Control), "Mark does not contain Cc");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.Format), "Mark does not contain Cf");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.Surrogate), "Mark does not contain Cs");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.PrivateUse), "Mark does not contain Co");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.ConnectorPunctuation), "Mark does not contain Pc");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.DashPunctuation), "Mark does not contain Pd");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.OpenPunctuation), "Mark does not contain Ps");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.ClosePunctuation), "Mark does not contain Pe");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.InitialQuotePunctuation), "Mark does not contain Pi");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.FinalQuotePunctuation), "Mark does not contain Pf");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.OtherPunctuation), "Mark does not contain Po");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.MathSymbol), "Mark does not contain Sm");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.CurrencySymbol), "Mark does not contain Sc");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.ModifierSymbol), "Mark does not contain Sk");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.OtherSymbol), "Mark does not contain So");
            Assert.False(Unicode7.Property.Mark.Contains(UnicodeCategory.OtherNotAssigned), "Mark does not contain Cn");
        }

        [Test]
        public void SeparatorSubsets()
        {
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.AnyChar), "Separator does not contain Any");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Newline), "Separator does not contain Newline");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Space), "Separator does not contain Space");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Graph), "Separator does not contain Graph");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Word), "Separator does not contain Word");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Alpha), "Separator does not contain Alpha");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Cased), "Separator does not contain Cased");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Upper), "Separator does not contain Upper");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Lower), "Separator does not contain Lower");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Ignorable), "Separator does not contain Ignorable");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.NonChar), "Separator contains NonChar");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Letter), "Separator does not contain Letter");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Mark), "Separator does not contain Mark");
            Assert.True(Unicode7.Property.Separator.Contains(Unicode7.Property.Separator), "Separator contains Separator");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Symbol), "Separator does not contain Symbol");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Number), "Separator does not contain Number");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Punctuation), "Separator does not contain Punctuation");
            Assert.False(Unicode7.Property.Separator.Contains(Unicode7.Property.Other), "Separator does not contain Other");

            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.UppercaseLetter), "Separator does not contain Lu");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.LowercaseLetter), "Separator does not contain Ll");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.TitlecaseLetter), "Separator does not contain Lt");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.ModifierLetter), "Separator does not contain Lm");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.OtherLetter), "Separator does not contain Lo");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.NonSpacingMark), "Separator does not contain Mn");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.SpacingCombiningMark), "Separator does not contain Mc");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.EnclosingMark), "Separator does not contain Me");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.DecimalDigitNumber), "Separator does not contain Nd");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.LetterNumber), "Separator contains Nl");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.OtherNumber), "Separator does not contain No");
            Assert.True(Unicode7.Property.Separator.Contains(UnicodeCategory.SpaceSeparator), "Separator contains Zs");
            Assert.True(Unicode7.Property.Separator.Contains(UnicodeCategory.LineSeparator), "Separator contains Zl");
            Assert.True(Unicode7.Property.Separator.Contains(UnicodeCategory.ParagraphSeparator), "Separator contains Zp");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.Control), "Separator does not contain Cc");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.Format), "Separator does not contain Cf");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.Surrogate), "Separator does not contain Cs");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.PrivateUse), "Separator does not contain Co");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.ConnectorPunctuation), "Separator does not contain Pc");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.DashPunctuation), "Separator does not contain Pd");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.OpenPunctuation), "Separator does not contain Ps");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.ClosePunctuation), "Separator does not contain Pe");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.InitialQuotePunctuation), "Separator does not contain Pi");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.FinalQuotePunctuation), "Separator does not contain Pf");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.OtherPunctuation), "Separator does not contain Po");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.MathSymbol), "Separator does not contain Sm");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.CurrencySymbol), "Separator does not contain Sc");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.ModifierSymbol), "Separator does not contain Sk");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.OtherSymbol), "Separator does not contain So");
            Assert.False(Unicode7.Property.Separator.Contains(UnicodeCategory.OtherNotAssigned), "Separator does not contain Cn");
        }

        [Test]
        public void SymbolSubsets()
        {
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.AnyChar), "Symbol does not contain Any");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Newline), "Symbol does not contain Newline");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Space), "Symbol does not contain Space");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Graph), "Symbol does not contain Graph");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Word), "Symbol does not contain Word");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Alpha), "Symbol does not contain Alpha");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Cased), "Symbol does not contain Cased");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Upper), "Symbol does not contain Upper");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Lower), "Symbol does not contain Lower");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Ignorable), "Symbol does not contain Ignorable");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.NonChar), "Symbol contains NonChar");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Letter), "Symbol does not contain Letter");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Mark), "Symbol does not contain Mark");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Separator), "Symbol does not contain Separator");
            Assert.True(Unicode7.Property.Symbol.Contains(Unicode7.Property.Symbol), "Symbol contains Symbol");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Number), "Symbol does not contain Number");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Punctuation), "Symbol does not contain Punctuation");
            Assert.False(Unicode7.Property.Symbol.Contains(Unicode7.Property.Other), "Symbol does not contain Other");

            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.UppercaseLetter), "Symbol does not contain Lu");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.LowercaseLetter), "Symbol does not contain Ll");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.TitlecaseLetter), "Symbol does not contain Lt");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.ModifierLetter), "Symbol does not contain Lm");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.OtherLetter), "Symbol does not contain Lo");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.NonSpacingMark), "Symbol does not contain Mn");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.SpacingCombiningMark), "Symbol does not contain Mc");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.EnclosingMark), "Symbol does not contain Me");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.DecimalDigitNumber), "Symbol does not contain Nd");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.LetterNumber), "Symbol contains Nl");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.OtherNumber), "Symbol does not contain No");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.SpaceSeparator), "Symbol does not contain Zs");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.LineSeparator), "Symbol does not contain Zl");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.ParagraphSeparator), "Symbol does not contain Zp");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.Control), "Symbol does not contain Cc");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.Format), "Symbol does not contain Cf");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.Surrogate), "Symbol does not contain Cs");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.PrivateUse), "Symbol does not contain Co");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.ConnectorPunctuation), "Symbol does not contain Pc");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.DashPunctuation), "Symbol does not contain Pd");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.OpenPunctuation), "Symbol does not contain Ps");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.ClosePunctuation), "Symbol does not contain Pe");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.InitialQuotePunctuation), "Symbol does not contain Pi");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.FinalQuotePunctuation), "Symbol does not contain Pf");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.OtherPunctuation), "Symbol does not contain Po");
            Assert.True(Unicode7.Property.Symbol.Contains(UnicodeCategory.MathSymbol), "Symbol contains Sm");
            Assert.True(Unicode7.Property.Symbol.Contains(UnicodeCategory.CurrencySymbol), "Symbol contains Sc");
            Assert.True(Unicode7.Property.Symbol.Contains(UnicodeCategory.ModifierSymbol), "Symbol contains Sk");
            Assert.True(Unicode7.Property.Symbol.Contains(UnicodeCategory.OtherSymbol), "Symbol contains So");
            Assert.False(Unicode7.Property.Symbol.Contains(UnicodeCategory.OtherNotAssigned), "Symbol does not contain Cn");
        }

        [Test]
        public void NumberSubsets()
        {
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.AnyChar), "Number does not contain Any");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Newline), "Number does not contain Newline");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Space), "Number does not contain Space");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Graph), "Number does not contain Graph");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Word), "Number does not contain Word");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Alpha), "Number does not contain Alpha");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Cased), "Number does not contain Cased");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Upper), "Number does not contain Upper");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Lower), "Number does not contain Lower");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Ignorable), "Number does not contain Ignorable");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.NonChar), "Number does not contain NonChar");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Letter), "Number does not contain Letter");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Mark), "Number does not contain Mark");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Separator), "Number does not contain Separator");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Symbol), "Number does not contain Symbol");
            Assert.True(Unicode7.Property.Number.Contains(Unicode7.Property.Number), "Number contains Number");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Punctuation), "Number does not contain Punctuation");
            Assert.False(Unicode7.Property.Number.Contains(Unicode7.Property.Other), "Number does not contain Other");

            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.UppercaseLetter), "Number does not contain Lu");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.LowercaseLetter), "Number does not contain Ll");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.TitlecaseLetter), "Number does not contain Lt");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.ModifierLetter), "Number does not contain Lm");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.OtherLetter), "Number does not contain Lo");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.NonSpacingMark), "Number does not contain Mn");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.SpacingCombiningMark), "Number does not contain Mc");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.EnclosingMark), "Number does not contain Me");
            Assert.True(Unicode7.Property.Number.Contains(UnicodeCategory.DecimalDigitNumber), "Number does not contain Nd");
            Assert.True(Unicode7.Property.Number.Contains(UnicodeCategory.LetterNumber), "Number contains Nl");
            Assert.True(Unicode7.Property.Number.Contains(UnicodeCategory.OtherNumber), "Number does not contain No");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.SpaceSeparator), "Number does not contain Zs");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.LineSeparator), "Number does not contain Zl");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.ParagraphSeparator), "Number does not contain Zp");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.Control), "Number does not contain Cc");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.Format), "Number does not contain Cf");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.Surrogate), "Number does not contain Cs");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.PrivateUse), "Number does not contain Co");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.ConnectorPunctuation), "Number does not contain Pc");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.DashPunctuation), "Number does not contain Pd");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.OpenPunctuation), "Number does not contain Ps");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.ClosePunctuation), "Number does not contain Pe");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.InitialQuotePunctuation), "Number does not contain Pi");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.FinalQuotePunctuation), "Number does not contain Pf");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.OtherPunctuation), "Number does not contain Po");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.MathSymbol), "Number does not contain Sm");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.CurrencySymbol), "Number does not contain Sc");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.ModifierSymbol), "Number does not contain Sk");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.OtherSymbol), "Number does not contain So");
            Assert.False(Unicode7.Property.Number.Contains(UnicodeCategory.OtherNotAssigned), "Number does not contain Cn");
        }

        [Test]
        public void PunctuationSubsets()
        {
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.AnyChar), "Punctuation does not contain Any");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Newline), "Punctuation does not contain Newline");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Space), "Punctuation does not contain Space");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Graph), "Punctuation does not contain Graph");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Word), "Punctuation does not contain Word");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Alpha), "Punctuation does not contain Alpha");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Cased), "Punctuation does not contain Cased");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Upper), "Punctuation does not contain Upper");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Lower), "Punctuation does not contain Lower");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Ignorable), "Punctuation does not contain Ignorable");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.NonChar), "Punctuation contains NonChar");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Letter), "Punctuation does not contain Letter");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Mark), "Punctuation does not contain Mark");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Separator), "Punctuation does not contain Separator");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Symbol), "Punctuation does not contain Symbol");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Number), "Punctuation does not contain Number");
            Assert.True(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Punctuation), "Punctuation contains Punctuation");
            Assert.False(Unicode7.Property.Punctuation.Contains(Unicode7.Property.Other), "Punctuation does not contain Other");

            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.UppercaseLetter), "Punctuation does not contain Lu");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.LowercaseLetter), "Punctuation does not contain Ll");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.TitlecaseLetter), "Punctuation does not contain Lt");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.ModifierLetter), "Punctuation does not contain Lm");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.OtherLetter), "Punctuation does not contain Lo");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.NonSpacingMark), "Punctuation does not contain Mn");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.SpacingCombiningMark), "Punctuation does not contain Mc");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.EnclosingMark), "Punctuation does not contain Me");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.DecimalDigitNumber), "Punctuation does not contain Nd");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.LetterNumber), "Punctuation contains Nl");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.OtherNumber), "Punctuation does not contain No");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.SpaceSeparator), "Punctuation does not contain Zs");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.LineSeparator), "Punctuation does not contain Zl");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.ParagraphSeparator), "Punctuation does not contain Zp");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.Control), "Punctuation does not contain Cc");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.Format), "Punctuation does not contain Cf");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.Surrogate), "Punctuation does not contain Cs");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.PrivateUse), "Punctuation does not contain Co");
            Assert.True(Unicode7.Property.Punctuation.Contains(UnicodeCategory.ConnectorPunctuation), "Punctuation contains Pc");
            Assert.True(Unicode7.Property.Punctuation.Contains(UnicodeCategory.DashPunctuation), "Punctuation contains Pd");
            Assert.True(Unicode7.Property.Punctuation.Contains(UnicodeCategory.OpenPunctuation), "Punctuation contains Ps");
            Assert.True(Unicode7.Property.Punctuation.Contains(UnicodeCategory.ClosePunctuation), "Punctuation contains Pe");
            Assert.True(Unicode7.Property.Punctuation.Contains(UnicodeCategory.InitialQuotePunctuation), "Punctuation contains Pi");
            Assert.True(Unicode7.Property.Punctuation.Contains(UnicodeCategory.FinalQuotePunctuation), "Punctuation contains Pf");
            Assert.True(Unicode7.Property.Punctuation.Contains(UnicodeCategory.OtherPunctuation), "Punctuation contains Po");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.MathSymbol), "Punctuation does not contain Sm");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.CurrencySymbol), "Punctuation does not contain Sc");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.ModifierSymbol), "Punctuation does not contain Sk");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.OtherSymbol), "Punctuation does not contain So");
            Assert.False(Unicode7.Property.Punctuation.Contains(UnicodeCategory.OtherNotAssigned), "Punctuation does not contain Cn");
        }

        [Test]
        public void OtherCharSubsets()
        {
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.AnyChar), "Other does not contain Any");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Newline), "Other does not contain Newline");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Space), "Other does not contain Space");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Graph), "Other does not contain Graph");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Word), "Other does not contain Word");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Alpha), "Other does not contain Alpha");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Cased), "Other does not contain Cased");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Upper), "Other does not contain Upper");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Lower), "Other does not contain Lower");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Ignorable), "Other does not contain Ignorable");
            Assert.True(Unicode7.Property.Other.Contains(Unicode7.Property.NonChar), "Other contains NonChar");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Letter), "Other does not contain Letter");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Mark), "Other does not contain Mark");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Separator), "Other does not contain Separator");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Symbol), "Other does not contain Symbol");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Number), "Other does not contain Number");
            Assert.False(Unicode7.Property.Other.Contains(Unicode7.Property.Punctuation), "Other does not contain Punctuation");
            Assert.True(Unicode7.Property.Other.Contains(Unicode7.Property.Other), "Other contains Other");

            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.UppercaseLetter), "Other does not contain Lu");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.LowercaseLetter), "Other does not contain Ll");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.TitlecaseLetter), "Other does not contain Lt");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.ModifierLetter), "Other does not contain Lm");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.OtherLetter), "Other does not contain Lo");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.NonSpacingMark), "Other does not contain Mn");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.SpacingCombiningMark), "Other does not contain Mc");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.EnclosingMark), "Other does not contain Me");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.DecimalDigitNumber), "Other does not contain Nd");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.LetterNumber), "Other contains Nl");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.OtherNumber), "Other does not contain No");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.SpaceSeparator), "Other does not contain Zs");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.LineSeparator), "Other does not contain Zl");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.ParagraphSeparator), "Other does not contain Zp");
            Assert.True(Unicode7.Property.Other.Contains(UnicodeCategory.Control), "Other contains Cc");
            Assert.True(Unicode7.Property.Other.Contains(UnicodeCategory.Format), "Other contains Cf");
            Assert.True(Unicode7.Property.Other.Contains(UnicodeCategory.Surrogate), "Other contains Cs");
            Assert.True(Unicode7.Property.Other.Contains(UnicodeCategory.PrivateUse), "Other contains Co");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.ConnectorPunctuation), "Other does not contain Pc");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.DashPunctuation), "Other does not contain Pd");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.OpenPunctuation), "Other does not contain Ps");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.ClosePunctuation), "Other does not contain Pe");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.InitialQuotePunctuation), "Other does not contain Pi");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.FinalQuotePunctuation), "Other does not contain Pf");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.OtherPunctuation), "Other does not contain Po");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.MathSymbol), "Other does not contain Sm");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.CurrencySymbol), "Other does not contain Sc");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.ModifierSymbol), "Other does not contain Sk");
            Assert.False(Unicode7.Property.Other.Contains(UnicodeCategory.OtherSymbol), "Other does not contain So");
            Assert.True(Unicode7.Property.Other.Contains(UnicodeCategory.OtherNotAssigned), "Other contains Cn");
        }

        #endregion

        [Test]
        public void ToCaseFold()
        {
            string chars = "Aa!";
            Assert.AreEqual(chars.ToCodepoint(0).ToCaseFold(), 'a', "did not casefold 'A'");
            Assert.AreEqual(chars.ToCodepoint(1).ToCaseFold(), 'a', "casefolded 'a'");
            Assert.AreEqual(chars.ToCodepoint(2).ToCaseFold(), '!', "casefolded '!'");
        }
    }
}
