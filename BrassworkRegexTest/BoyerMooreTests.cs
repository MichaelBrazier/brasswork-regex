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
    /// <summary>Unit tests for the Boyer-Moore scanner.</summary>
    [TestFixture]
    class BoyerMooreTests
    {
        [Test]
        public void BoyerMooreExample()
        {
            BoyerMooreScanner scanner = new BoyerMooreScanner("example");
            Assert.That(scanner.Position, Is.EqualTo(-1));
            scanner.Scan("this is a simple example");
            Assert.That(scanner.Position, Is.EqualTo(17));
            scanner.Scan("the quick brown fox");
            Assert.That(scanner.Position, Is.EqualTo(19));
            scanner.Scan("This is a Simple Example");
            Assert.That(scanner.Position, Is.EqualTo(24));
        }

        [Test]
        public void BoyerMooreCaseFold()
        {
            BoyerMooreScanner scanner = new BoyerMooreScanner("example", true);
            Assert.That(scanner.Position, Is.EqualTo(-1));
            scanner.Scan("this is a simple example");
            Assert.That(scanner.Position, Is.EqualTo(17));
            scanner.Scan("the quick brown fox");
            Assert.That(scanner.Position, Is.EqualTo(19));
            scanner.Scan("This is a Simple Example");
            Assert.That(scanner.Position, Is.EqualTo(17));
            scanner.Scan("THIS IS A SIMPLE EXAMPLE");
            Assert.That(scanner.Position, Is.EqualTo(17));
        }

        const string penzance = @"(Scene.-A rocky seashore on the coast of Cornwall.  In the
distance is a calm sea, on which a schooner is lying at anchor. 
Rock L. sloping down to L.C. of stage.  Under these rocks is a
cavern, the entrance to which is seen at first entrance L.  A
natural arch of rock occupies the R.C. of the stage.  As the
curtain rises groups of pirates are discovered -- some drinking,
some playing cards.  SAMUEL, the Pirate Lieutenant, is going from
one group to another, filling the cups from a flask.  FREDERIC is
seated in a despondent attitude at the back of the scene.  RUTH
kneels at his feet.)

 
                         OPENING CHORUS
 
ALL:      Pour, O pour the pirate sherry;
               Fill, O fill the pirate glass;
          And, to make us more than merry
               Let the pirate bumper pass.
 
SAMUEL:   For today our pirate 'prentice
               Rises from indentures freed;
          Strong his arm, and keen his scent is
               He's a pirate now indeed!
 
ALL:      Here's good luck to Fred'ric's ventures!
          Fred'ric's out of his indentures.
 
SAMUEL:   Two and twenty, now he's rising,
               And alone he's fit to fly,
          Which we're bent on signalizing
               With unusual revelry.
 
ALL:      Here's good luck to Fred'ric's ventures!
               Fred'ric's out of his indentures.
          Pour, O pour the pirate sherry;
               Fill, O fill the pirate glass;
          And, to make us more than merry
               Let the pirate bumper pass.
";

        [Test]
        public void BoyerMooreRepeatScan()
        {
            BoyerMooreScanner scanner = new BoyerMooreScanner("pirate");
            
            int nMatches = 0;
            for (int pos = 0; scanner.Scan(penzance, pos); pos = scanner.Position + 1)
                nMatches++;

            Assert.That(nMatches, Is.EqualTo(9));
        }

        [Test]
        public void BoyerMooreCasefoldRepeatScan()
        {
            BoyerMooreScanner scanner = new BoyerMooreScanner("pirate", true);

            int nMatches = 0;
            for (int pos = 0; scanner.Scan(penzance, pos); pos = scanner.Position + 1)
                nMatches++;

            Assert.That(nMatches, Is.EqualTo(10));
        }
    }
}
