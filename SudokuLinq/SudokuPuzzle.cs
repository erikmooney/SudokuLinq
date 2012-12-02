using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

namespace SudokuLinq
{
    class SudokuPuzzle : ICloneable
    {
        public int[][] Cells;
        public int Length;
        public int BoxSize { get { return (int)Math.Sqrt(Length); } }

        public SudokuPuzzle(int n)
        {
            Cells = Enumerable.Repeat(Enumerable.Range(1, n).ToArray(), n * n).ToArray();
            Length = n;
        }

        public SudokuPuzzle(string input)
            : this(input.Select(c => Char.IsDigit(c) ? c - '0' : 0).ToArray())
        {

        }

        public SudokuPuzzle(int[] input)
            : this((int)Math.Sqrt(input.Length))
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > 0 && input[i] <= Length)
                {
                    var puzzle = this.PlaceValue(i, input[i]);
                    if (puzzle == null)
                        throw new ArgumentException("This puzzle is unsolvable!");
                    this.Cells = puzzle.Cells;
                }
            }
        }

        public virtual object Clone()
        {
            var clone = new SudokuPuzzle(this.Length);

            clone.Cells = new int[this.Cells.Length][];
            for (int i = 0; i < this.Cells.Length; i++)
            {
                clone.Cells[i] = new int[this.Cells[i].Length];
                Buffer.BlockCopy(this.Cells[i], 0, clone.Cells[i], 0, Buffer.ByteLength(this.Cells[i]));
            }
            return clone;
        }

        public virtual bool IsPeer(int c1, int c2)
        {
            return (c1 / Length == c2 / Length    //Cells in same row
                || c1 % Length == c2 % Length     //Cells in same column
                || (c1 / Length / BoxSize == c2 / Length / BoxSize && c1 % Length / BoxSize == c2 % Length / BoxSize))  //Cells in same box
                && c1 != c2;                      //Cell is not peer to itself
        }

        //Dictionary to memoize lists of peers
        //The key is a tuple of <int, int>, where the first value is the length of the puzzle (typically 9) and the second value is the cell index
        private static Dictionary<Tuple<int, int>, int[]> _savedPeers = new Dictionary<Tuple<int, int>, int[]>();
        public virtual int[] Peers(int cell)
        {
            var key = new Tuple<int, int>(this.Length, cell);
            if (!_savedPeers.ContainsKey(key))
                _savedPeers.Add(key, Enumerable.Range(0, Length * Length).Where(c => IsPeer(cell, c)).ToArray());

            return _savedPeers[key];
        }

        public virtual SudokuPuzzle ApplyConstraints(int cellIndex, int value)
        {
            SudokuPuzzle puzzle = (SudokuPuzzle)this.Clone();

            //Standard Sudoku constraint logic: Set this cell to one and only one candidate, and remove this value from the candidate list of all its peers
            puzzle.Cells[cellIndex] = new int[] { value };

            foreach (int peerIndex in puzzle.Peers(cellIndex))
            {
                var newPeers = puzzle.Cells[peerIndex].Except(new int[] { value }).ToArray();
                if (!newPeers.Any())
                    return null;

                puzzle.Cells[peerIndex] = newPeers;
            }
            return puzzle;
        }

        public static List<int> FindSingularizedCells(SudokuPuzzle puzzle1, SudokuPuzzle puzzle2, int cellIndex)
        {
            Debug.Assert(puzzle1.Length == puzzle2.Length);
            var result = new List<int>();
            foreach (int i in puzzle1.Peers(cellIndex))
            {
                if (puzzle1.Cells[i].Length > 1 && puzzle2.Cells[i].Length == 1)
                    result.Add(i);
            }
            return result;
        }

        public virtual SudokuPuzzle PlaceValue(int cellIndex, int value)
        {
            if (!this.Cells[cellIndex].Contains(value))
                return null;

            var puzzle = ApplyConstraints(cellIndex, value);
            if (puzzle == null)
                return null;

            foreach (int i in FindSingularizedCells(this, puzzle, cellIndex))
            {
                if ((puzzle = puzzle.PlaceValue(i, puzzle.Cells[i].Single())) == null)
                    return null;
            }

            return puzzle;
        }

        public virtual int FindWorkingCell()
        {
            int minCandidates = this.Cells.Where(cands => cands.Length >= 2).Min(cands => cands.Length);
            return Array.FindIndex(this.Cells, c => c.Length == minCandidates);
        }

        public static List<SudokuPuzzle> MultiSolve(SudokuPuzzle input, int MaximumSolutions = -1)
        {
            var Solutions = new List<SudokuPuzzle>();
            input.Solve(p =>
            {
                Solutions.Add(p);
                return Solutions.Count() < MaximumSolutions || MaximumSolutions == -1;
            });
            return Solutions;
        }

        public virtual SudokuPuzzle Solve(Func<SudokuPuzzle, bool> SolutionFunc = null)
        {
            if (this.Cells.All(cell => cell.Length == 1))
                return (SolutionFunc != null && SolutionFunc(this)) ? null : this;

            int ActiveCell = FindWorkingCell();
            foreach (int guess in this.Cells[ActiveCell])
            {
                SudokuPuzzle puzzle;
                if ((puzzle = PlaceValue(ActiveCell, guess)) != null)
                    if ((puzzle = puzzle.Solve(SolutionFunc)) != null)
                        return puzzle;
            }
            return null;
        }

        public static SudokuPuzzle RandomGrid(int size)
        {
            SudokuPuzzle puzzle = new SudokuPuzzle(size);
            var rand = new Random();

            while (true)
            {
                int[] UnsolvedCellIndexes = puzzle.Cells
                    .Select((cands, index) => new { cands, index })     //Project to a new sequence of candidates and index (an anonymous type behaving like a tuple)
                    .Where(t => t.cands.Length >= 2)                    //Filter to cells with at least 2 candidates
                    .Select(u => u.index)                               //Project the tuple to only the index
                    .ToArray();

                int cellIndex = UnsolvedCellIndexes[rand.Next(UnsolvedCellIndexes.Length)];
                int candidateValue = puzzle.Cells[cellIndex][rand.Next(puzzle.Cells[cellIndex].Length)];

                SudokuPuzzle workingPuzzle = puzzle.PlaceValue(cellIndex, candidateValue);
                if (workingPuzzle != null)
                {
                    var Solutions = MultiSolve(workingPuzzle, 2);
                    switch (Solutions.Count)
                    {
                        case 0: continue;
                        case 1: return Solutions.Single();
                        default:
                            puzzle = workingPuzzle;
                            break;
                    }
                }
            }
        }

        public static int[] CreateClues(SudokuPuzzle input, int maxClues = 0)
        {
            //Get a unique solution for the input puzzle, in case it wasn't already
            var puzzle = input.Solve();
            if (puzzle == null) throw new ArgumentException("Can't create clues from an unsolvable puzzle!");

            //This is the list of clues we work on.  It can be reconstituted into a new puzzle object by way of a constructor
            int[] Clues = puzzle.Cells.Select(c => c[0]).ToArray();
            var rand = new Random();
            while (true)
            {
                //Pick a random cell to blank
                int ClueCell = rand.Next(Clues.Length);
                if (Clues[ClueCell] == 0)
                    continue;

                var workingClues = Clues.ToArray();
                workingClues[ClueCell] = 0;
                if (MultiSolve(new SudokuPuzzle(workingClues), 2).Count() > 1)
                {
                    if (maxClues == 0)
                        return Clues;
                    else
                        continue;
                }

                Clues = workingClues;
                if (Clues.Count(c => c != 0) <= maxClues)
                    return Clues;
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            int maxWidth = this.Cells.Max(c => c.Length);
            this.Cells.Select((cands, index) => new { cands, index }).ToList().ForEach(a =>
            {
                result.AppendFormat("{0,-" + maxWidth + "} ", String.Join("", a.cands));

                if (a.index % this.Length == this.Length - 1)
                    result.AppendLine();
                else if (a.index % this.BoxSize == this.BoxSize - 1)
                    result.Append("| ");
                if (a.index % (this.Length * this.BoxSize) == (this.Length * this.BoxSize - 1) && a.index < this.Length * this.Length - 1)
                    result.AppendLine(String.Join("-+-",
                        Enumerable.Repeat(String.Join("-",
                            Enumerable.Repeat(String.Concat(Enumerable.Repeat("-", maxWidth)), this.BoxSize)), this.BoxSize)));
            });

            return result.ToString();
        }

        public static void Output(SudokuPuzzle puzzle)
        {
            System.Console.Write(puzzle.ToString());
        }
    }
}
