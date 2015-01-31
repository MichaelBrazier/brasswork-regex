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
namespace Brasswork
{
    internal struct BitSet32
    {
        int Data;

        public BitSet32(int value) { Data = value; }
        //public BitSet32(BitSet32 set) { Data = set.Data; }

        public bool this[byte i]
        {
            get { return 0 != (((int)1 << i) & Data); }
            //set
            //{
            //    if (value)
            //        Data = Data | ((int)1 << i);
            //    else
            //        Data = Data & ~((int)1 << i);
            //}
        }
    }
}
