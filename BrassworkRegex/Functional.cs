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
using System.Collections.Generic;

namespace Brasswork
{
    /// <summary>Techniques from functional programming.</summary>
    public static class Functional
    {
        /// <summary>A function that memoizes a function of one argument.</summary>
        /// <typeparam name="T">Type of <paramref name="f"/>'s argument.</typeparam>
        /// <typeparam name="TResult">Type of <paramref name="f"/>'s result.</typeparam>
        /// <param name="f">The function being memoized.</param>
        /// <returns>A memoized version of <paramref name="f"/>.</returns>
        public static Func<T, TResult> Memoize<T, TResult>(this Func<T, TResult> f)
        {
            Dictionary<T, TResult> cache = new Dictionary<T, TResult>();

            return arg =>
            {
                TResult result;
                if (!cache.TryGetValue(arg, out result))
                {
                    result = f(arg);
                    cache[arg] = result;
                }
                return result;
            };
        }

        static Func<T, TResult> CastByExample<T, TResult>(Func<T, TResult> f, T t) { return f; }

        /// <summary>A function that memoizes a function of two arguments.</summary>
        /// <typeparam name="T1">Type of <paramref name="f"/>'s first argument.</typeparam>
        /// <typeparam name="T2">Type of <paramref name="f"/>'s second argument.</typeparam>
        /// <typeparam name="TResult">Type of <paramref name="f"/>'s result.</typeparam>
        /// <param name="f">The function being memoized.</param>
        /// <returns>A memoized version of <paramref name="f"/>.</returns>
        public static Func<T1, T2, TResult> Memoize<T1, T2, TResult>(this Func<T1, T2, TResult> f)
        {
            var exemplar = new { A = default(T1), B = default(T2) };
            var tuplified = CastByExample(t => f(t.A, t.B), exemplar);
            var memoized = tuplified.Memoize();
            return (a, b) => memoized(new { A = a, B = b });
        }

        /// <summary>A function that memoizes a function of three arguments.</summary>
        /// <typeparam name="T1">Type of <paramref name="f"/>'s first argument.</typeparam>
        /// <typeparam name="T2">Type of <paramref name="f"/>'s second argument.</typeparam>
        /// <typeparam name="T3">Type of <paramref name="f"/>'s third argument.</typeparam>
        /// <typeparam name="TResult">Type of <paramref name="f"/>'s result.</typeparam>
        /// <param name="f">The function being memoized.</param>
        /// <returns>A memoized version of <paramref name="f"/>.</returns>
        public static Func<T1, T2, T3, TResult> Memoize<T1, T2, T3, TResult>(this Func<T1, T2, T3, TResult> f)
        {
            var exemplar = new { A = default(T1), B = default(T2), C = default(T3) };
            var tuplified = CastByExample(t => f(t.A, t.B, t.C), exemplar);
            var memoized = tuplified.Memoize();
            return (a, b, c) => memoized(new { A = a, B = b, C = c });
        }
    }
}