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
    /// <summary>Unit tests for the Regex API.</summary>
    [TestFixture]
    class MatchingTests
    {
        [Test]
        public void OptionCodes()
        {
            Assert.True(new Regex(".*", "b").IsOption(RegexOptions.SimpleWordBreak));
            Assert.True(new Regex(".*", "i").IsOption(RegexOptions.IgnoreCase));
            Assert.True(new Regex(".*", "m").IsOption(RegexOptions.Multiline));
            Assert.True(new Regex(".*", "s").IsOption(RegexOptions.DotAll));
            Assert.True(new Regex(".*", "x").IsOption(RegexOptions.FreeSpacing));
            Assert.True(new Regex(".*", "A").IsOption(RegexOptions.Anchored));
            Assert.True(new Regex(".*", "ims").IsOption(
                RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.DotAll));
            Assert.That(new Regex(".*", "simbAx").Options, Is.EqualTo("bimsxA"));

            Assert.Throws<ArgumentOutOfRangeException>(() => new Regex(".*", "g"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Regex(".*", "imst"));
        }

        [Test]
        public void OutOfBounds()
        {
            Regex re = new Regex(@"abc");
            string text = "abc";
            Assert.Throws<ArgumentOutOfRangeException>(() => re.Match(text, -1),
                "{0} matched at {1:d}", re.ToString(), -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => re.Match(text, 4),
                "{0} matched at {1:d}", re.ToString(), 4);
        }

        [Test]
        public void MatchErrors()
        {
            Regex re1 = new Regex("abc");
            Assert.Throws<ArgumentNullException>(() => re1.Match(null));

            Match m1 = re1.Match("abc");
            Assert.Throws<ArgumentOutOfRangeException>(() => { string str = m1.Groups["group"].ToString(); });

            Match m2 = re1.Match("abcd");
            Assert.Throws<ArgumentException>(() => Match.PosixPriority(m1, m2));
            Assert.Throws<ArgumentException>(() => m1.Copy(m2));

            Regex re2 = new Regex("abcd");
            Match m3 = re2.Match("abcd");
            Assert.Throws<ArgumentException>(() => Match.PosixPriority(m2, m3));
            Assert.Throws<ArgumentException>(() => m2.Copy(m3));
        }

        [Test]
        public void AnchoredRegex()
        {
            Regex re = new Regex(@"fox", RegexOptions.Anchored);
            string text = "The quick brown fox jumped over the lazy dog.";
            Assert.False(re.Match(text).IsSuccess, "{0} matched \"{1}\"", re.ToString(), text);
            Assert.True(re.Match(text, 16).IsSuccess,
                "{0} did not match \"{1}\" at {2:d}", re.ToString(), text, 16);
        }

        [Test]
        public void AnchoredEmpty()
        {
            Regex re = new Regex(@"\w*", RegexOptions.Anchored);
            string text = "The quick brown fox jumped over the lazy dog.";
            Match m = re.Match(text);
            Assert.True(m.IsSuccess, "{0} did not match \"{1}\" on pass 1", re.ToString(), text);
            m = m.FindNext();
            Assert.True(m.IsSuccess, "{0} did not match \"{1}\" on pass 2", re.ToString(), text);
            m = m.FindNext();
            Assert.False(m.IsSuccess, "{0} matched \"{1}\" on pass 3", re.ToString(), text);
            Assert.Throws<InvalidOperationException>(() => m.FindNext());
        }

        [Test]
        public void GroupEnumeration()
        {
            Regex re = new Regex(@"(+\d{4})(+\d{2})(+\d{2})");
            Assert.That(re.Match("20090704").Groups.Select(g => g.ToString()),
                Is.EqualTo(new string[] { "20090704", "2009", "07", "04" }));
        }

        [Test]
        public void AllMatches()
        {
            Regex re = new Regex(@"\w+");
            string text = "The quick brown fox jumped over the lazy dog.";
            Assert.That(re.Matches(text).Select(m => m.ToString()), Is.EqualTo(
                new string[] { "The", "quick", "brown", "fox", "jumped", "over", "the", "lazy", "dog" }));
            Assert.That(re.Matches(text, 19).Select(m => m.ToString()), Is.EqualTo(
                new string[] { "jumped", "over", "the", "lazy", "dog" }));
        }

        [Test]
        public void SpaceSplit()
        {
            Regex re = new Regex(@"\s+");
            string text = "The quick brown fox jumped over the lazy dog.";
            Assert.That(re.Split(text), Is.EqualTo(
                new string[] { "The", "quick", "brown", "fox", "jumped", "over", "the", "lazy", "dog." }));
            Assert.That(re.Split(text, 5), Is.EqualTo(
                new string[] { "The", "quick", "brown", "fox", "jumped over the lazy dog." }));
            Assert.That(re.Split(text, 5, 5), Is.EqualTo(
                new string[] { "The quick", "brown", "fox", "jumped", "over the lazy dog." }));
            Assert.That(re.Split(text, -1, 18), Is.EqualTo(
                new string[] { "The quick brown fox", "jumped", "over", "the", "lazy", "dog." }));
        }

        [Test]
        public void CharSplit()
        {
            Regex re = new Regex(@"");
            Assert.That(re.Split("Brasswork"), Is.EqualTo(
                new string[] { "", "B", "r", "a", "s", "s", "w", "o", "r", "k", "" }));
        }

        [Test]
        public void TrimSpaces()
        {
            Regex re = new Regex(@"\s+");
            string text = "The\tquick \tbrown\t fox\njumped\n\tover   the\flazy dog.";
            Assert.That(re.Replace(text, " "),
                Is.EqualTo("The quick brown fox jumped over the lazy dog."));
            Assert.That(re.Replace(text, " ", 5),
                Is.EqualTo("The quick brown fox jumped over   the\flazy dog."));
            Assert.That(re.Replace(text, " ", -1, 20),
                Is.EqualTo("The\tquick \tbrown\t fox jumped over the lazy dog."));
        }

        [Test]
        public void Titlecase()
        {
            Regex re = new Regex(@"\b\w");
            string text = "The quick brown fox jumped over the lazy dog.";
            MatchEvaluator eval = new MatchEvaluator(m => m.ToString().ToUpper());
            Assert.That(re.Replace(text, eval),
                Is.EqualTo("The Quick Brown Fox Jumped Over The Lazy Dog."));
            Assert.That(re.Replace(text, eval, 4),
                Is.EqualTo("The Quick Brown Fox jumped over the lazy dog."));
            Assert.That(re.Replace(text, eval, -1, 30),
                Is.EqualTo("The quick brown fox jumped over The Lazy Dog."));
            Assert.That(re.Replace(text, eval, 1, 20),
                Is.EqualTo("The quick brown fox Jumped over the lazy dog."));
        }

        [Test, Sequential]
        public void MatchResult()
        {
            Regex re = new Regex(@"jumped");
            Match m = re.Match("The quick brown fox jumped over the lazy dog.");
            Assert.That(m.ReplaceResult(@"$`<i>$&</i>$'"),
                Is.EqualTo("The quick brown fox <i>jumped</i> over the lazy dog."));
        }

        [Test]
        public void FormatDate()
        {
            Regex re = new Regex(@"(+\d{4})(+\d{2})(+\d{2})");
            MatchEvaluator eval = new MatchEvaluator(m =>
            {
                string[] months = { "January", "February", "March", "April",
                                  "May", "June", "July", "August",
                                  "September", "October", "November", "December", };
                int year = Int32.Parse(m.Groups[1].ToString());
                int month = Int32.Parse(m.Groups[2].ToString());
                int day = Int32.Parse(m.Groups[3].ToString());
                return String.Format("{0} {1:d}, {2:d}", months[month - 1], day, year);
            });
            Assert.That(re.Replace("20090704", "$1/$2/$3"), Is.EqualTo("2009/07/04"));
            Assert.That(re.Replace("20090704", eval), Is.EqualTo("July 4, 2009"));
        }

        [Test]
        public void ReplaceWithNamedCaptures()
        {
            Regex re = new Regex(@"({year}\d{4})({month}\d{2})({day}\d{2})");
            Assert.That(re.GroupNames, Is.EqualTo(new string[] { "0", "year", "month", "day" }));
            Assert.That(re.Replace("20090704", "${year}/${month}/${day}"), Is.EqualTo("2009/07/04"));
        }

        [Test]
        public void Doubles()
        {
            Regex re = new Regex(@"\w+");
            Assert.That(re.Replace("hello", "$$$_$$ $$$_$$"), Is.EqualTo("$hello$ $hello$"));
        }
    }
}
