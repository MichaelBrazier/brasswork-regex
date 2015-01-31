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

namespace Brasswork.Regex
{
    /// <summary>Auxiliary data structure used to annotate syntax trees.</summary>
    internal sealed class AnnotationVisitor
    {
        /// <summary>The string from which a syntax tree was parsed.</summary>
        internal string regexRep;

        /// <summary>Count of atomic groups in a syntax tree.</summary>
        internal int nAtomics;

        /// <summary>Names of capturing groups in a syntax tree.</summary>
        internal List<string> captures;

        /// <summary>Nesting level of the nearest enclosing quantifier.</summary>
        internal int quantLevel;

        /// <summary>Depth of quantifier nesting in a syntax tree.</summary>
        internal int quantDepth;

        /// <summary>Whether the regex contains the start-of-text assertion.</summary>
        internal bool anchor;

        /// <summary>Whether the regex contains backreferences.</summary>
        internal bool backrefs;

        /// <summary>Constructor.</summary>
        /// <param name="expr">The string from which a syntax tree was parsed.</param>
        internal AnnotationVisitor(string expr)
        {
            regexRep = expr;
            nAtomics = 0;
            captures = new List<string>();
            quantLevel = 0;
            anchor = false;
            backrefs = false;
        }

        /// <summary>Reconciles the index and name properties in a capture check or backreference.</summary>
        /// <param name="index">Capture group index.</param>
        /// <param name="name">Capture group name.</param>
        /// <param name="position">Position of the reconciled expression in <see cref="regexRep"/>.</param>
        internal void SetCaptureInfo(ref int index, ref string name, int position)
        {
            string capName = name;
            if (index < 0)
                index = captures.FindIndex(c => c == capName);
            else if (index < captures.Count)
                name = captures[index];
            else
                throw new RegexParseException(regexRep, position,
                    string.Format("regex has {0:d} captures, capture {1:d} does not exist", captures.Count - 1, index));

            if (index < 0)
                throw new RegexParseException(regexRep, position,
                    string.Format("No capturing group is named \"{0}\"", name));
        }
    }

    internal sealed class NFABuilder
    {
        struct ASTTransition
        {
            public RegexAST source;
            public InstructAST accept;
            public RegexAST target;
        }

        /// <summary>NFA transitions that have been seen but not processed.</summary>
        Queue<ASTTransition> pending;

        /// <summary>NFA states that have been tagged with an ID.</summary>
        Dictionary<RegexAST, int> visited;

        /// <summary>Table of instructions for the NFA.</summary>
        internal List<NFA.Instruction> Instructions { get; private set; }

        Func<InstructAST, int> instructIndex;

        /// <summary>The regex syntax tree.</summary>
        RegexAST SyntaxTree;

        bool IgnoreCase;

        /// <summary>Count of possible side effects from evaluating an instruction.</summary>
        internal int MaxUndos;

        /// <summary>Maximum number of saved partial results while evaluating an instruction.</summary>
        internal int TestDepth;

        /// <summary>Initializes and compiles a regular expression, with options that modify the pattern.</summary>
        /// <param name="regexRep">The pattern to be compiled.</param>
        /// <param name="opts">The options desired.</param>
        /// <exception cref="ArgumentException"><paramref name="regexRep"/> is an ill-formed pattern.</exception>
        internal NFABuilder(string regexRep, RegexOptions opts)
        {
            pending = new Queue<ASTTransition>();
            visited = new Dictionary<RegexAST, int>();
            instructIndex = ast =>
            {
                Instructions.Add(ast.ToInstruction(this));
                return Instructions.Count - 1;
            };
            instructIndex = instructIndex.Memoize();
            ParseInfo = new AnnotationVisitor(regexRep);
            SyntaxTree = Parser.Parse(regexRep, opts);
            SyntaxTree.Annotate(ParseInfo);
            IgnoreCase = (opts & RegexOptions.IgnoreCase) != 0;
        }

        /// <summary>Information gathered while annotating the regex syntax tree.</summary>
        internal string CanonicalForm { get { return SyntaxTree.ToString(); } }

        /// <summary>Information gathered while annotating the regex syntax tree.</summary>
        internal AnnotationVisitor ParseInfo { get; private set; }

        /// <summary>Regexes standing for the states of the NFA.</summary>
        internal List<List<NFA.Transition>> States;

        /// <summary>Index of the state corresponding to the empty regular expression.</summary>
        internal int FinalState { get; private set; }

        /// <summary>Index of an instruction that checks whether a position can begin a match.</summary>
        internal int FirstTest { get; private set; }

        /// <summary>Looks for a string of characters that must begin any match.</summary>
        internal BoyerMooreScanner FixedPrefix { get; private set; }

        /// <summary>Collects the information needed to construct an NFA from a regular expression.</summary>
        internal void Traverse()
        {
            States = new List<List<NFA.Transition>>();
            Instructions = new List<NFA.Instruction>();

            string prefix = SyntaxTree.FixedPrefix(IgnoreCase);
            FixedPrefix = String.IsNullOrEmpty(prefix) ? null : new BoyerMooreScanner(prefix, IgnoreCase);
            InstructAST firstChars = SyntaxTree.FirstChars;
            FirstTest = AddInstructions(SyntaxTree.FirstChars);
            MaxUndos = firstChars.SideEffects;
            TestDepth = firstChars.TestDepth;

            visited.Clear();
            visited[SyntaxTree] = 0;
            States.Add(new List<NFA.Transition>());
            if (SyntaxTree == EmptyAST.Instance) FinalState = visited[SyntaxTree];

            pending.Clear();
            foreach (RegexDerivative d in SyntaxTree.Derivatives)
                pending.Enqueue(new ASTTransition { source = SyntaxTree, accept = d.Accept, target = d.Target });

            while (pending.Count > 0)
            {
                ASTTransition tran = pending.Dequeue();
                RegexAST state = tran.target;
                if (!visited.ContainsKey(state))
                {
                    visited[state] = visited.Count;
                    States.Add(new List<NFA.Transition>());
                    if (state == EmptyAST.Instance) FinalState = visited[state];
                    foreach (RegexDerivative d in state.Derivatives)
                        pending.Enqueue(new ASTTransition { source = state, accept = d.Accept, target = d.Target });
                }

                MaxUndos = Math.Max(MaxUndos, tran.accept.SideEffects);
                TestDepth = Math.Max(TestDepth, tran.accept.TestDepth);
                int acceptInstr = AddInstructions(tran.accept);
                States[visited[tran.source]].Add(
                    new NFA.Transition { instruction = acceptInstr, target = visited[tran.target] });
            }
        }

        /// <summary>Adds an instruction to the NFA's table.</summary>
        /// <param name="ast">An AST to translate to an instruction.</param>
        /// <returns>The translation of <paramref name="ast"/>.</returns>
        internal int AddInstructions(InstructAST ast) { return instructIndex(ast); }
    }
}
