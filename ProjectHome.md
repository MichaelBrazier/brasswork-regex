Brasswork Regex is a Unicode-aware regular expression package for the .NET platform. It recognizes the Unicode 7.0.0 character set. It conforms to the following requirements of Unicode Technical Standard #18: "Unicode Regular Expressions", Version 17:

  * RL1.1 Hex Notation
  * RL1.2 Properties
  * RL1.3 Subtraction and Intersection
  * RL1.4 Simple Word Boundaries
  * RL1.5 Simple Loose Matches
  * RL1.6 Line Boundaries
  * RL1.7 Supplementary Code Points
  * RL2.3 Default Word Boundaries

Brasswork Regex is based on Valentin Antimirov's NFA construction algorithm, detailed in his 1995 paper "Partial Derivatives of Regular Expressions and Finite Automata Constructions". However, it extends that algorithm to intersections and complements of regular expressions, following a 2013 technical report by Rafaela Bastos, Nelma Moreira, and Rogério Reis.