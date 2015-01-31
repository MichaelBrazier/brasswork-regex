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
using System.IO;
using System.Text;
using Brasswork.Regex;

namespace RegexBenchmark
{
    /// <summary>Main program for benchmarking the regex engine.</summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: {0} <filename> [-c]", AppDomain.CurrentDomain.FriendlyName);
                return;
            }
            StreamReader reader = new StreamReader(args[0], Encoding.UTF8);
            string text = reader.ReadToEnd();
            Console.WriteLine("Read file {0}.", args[0]);

            string[] regexes =
            {
                @"Twain", @"^Twain", @"Twain$", @"Huck[a-zA-Z]+|Finn[a-zA-Z]+", @"a[^x]{20}b", @"Tom|Sawyer|Huckleberry|Finn",
                @".{0,3}(Tom|Sawyer|Huckleberry|Finn)", @"[a-zA-Z]+ing", @"^[a-zA-Z]{0,4}ing[^a-zA-Z]", @"[a-zA-Z]+ing$",
                @"^[a-zA-Z ]{5,}$", @"^.{16,20}$", @"([a-f](.[d-m].){0,2}[h-n]){2}", @"([A-Za-z]awyer|[A-Za-z]inn)[^a-zA-Z]",
                @"""[^""]{0,30}[?!\.]""", @"Tom.{10,25}river|river.{10,25}Tom"
            };

            if (args.Length > 1 && args[1] == "-c")
            {
                foreach (string regex in regexes)
                {
                    Console.WriteLine("Timing /{0}/:", regex);
                    CompareMatchEngines(text, regex);
                }
            }
            else
            {
                foreach (string regex in regexes)
                {
                    Console.WriteLine("Timing /{0}/:", regex);
                    TimeMatch(text, regex);
                }
            }
        }

        static void CompareMatchEngines(string text, string regex)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            System.Text.RegularExpressions.Regex reNet = new System.Text.RegularExpressions.Regex(regex);
            int nMatches = 0;
            foreach (System.Text.RegularExpressions.Match m in reNet.Matches(text)) nMatches++;
            timer.Stop();
            Console.WriteLine("\t.NET library Regex found {0} matches in {1:F3} ms",
                nMatches, timer.Elapsed.TotalMilliseconds);
            double baseline = timer.Elapsed.TotalMilliseconds;

            timer.Restart();
            Regex re = new Regex(regex);
            nMatches = 0;
            foreach (Match m in re.Matches(text)) nMatches++;
            timer.Stop();
            Console.WriteLine("\tBrasswork Regex found {0} matches in {1:F3} ms ({2:F3} of .NET)",
                nMatches, timer.Elapsed.TotalMilliseconds, timer.Elapsed.TotalMilliseconds / baseline);
        }

        static void TimeMatch(string text, string regex)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Regex re = new Regex(regex);
            int nMatches = 0;
            foreach (Match m in re.Matches(text)) nMatches++;
            timer.Stop();
            Console.WriteLine("\tBrasswork Regex found {0} matches in {1:F3} ms",
                nMatches, timer.Elapsed.TotalMilliseconds);
        }

        static double TimeNetMatch(string text, string regex)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(regex);
            int nMatches = 0;
            foreach (System.Text.RegularExpressions.Match m in re.Matches(text)) nMatches++;
            timer.Stop();
            Console.WriteLine("\t.NET library Regex found {0} matches in {1:F3} ms",
                nMatches, timer.Elapsed.TotalMilliseconds);
            return timer.Elapsed.TotalMilliseconds;
        }
    }
}
