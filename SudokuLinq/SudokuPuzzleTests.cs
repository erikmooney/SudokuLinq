using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace SudokuLinq
{
    class SudokuPuzzleTests
    {
        public static void TestPeers()
        {
            var p = new SudokuPuzzle(9);
            var peers = p.Peers(40);
            var expectedResult = new int[] { 4, 13, 22, 30, 31, 32, 36, 37, 38, 39, 41, 42, 43, 44, 48, 49, 50, 58, 67, 76 };
            Debug.Assert(peers.SequenceEqual(expectedResult));
        }

        public static string TestPuzzle = "..3.2.6..9..3.5..1..18.64....81.29..7.......8..67.82....26.95..8..2.3..9..5.1.3..";
        public static void SimpleTest()
        {
            var p = new SudokuPuzzle(TestPuzzle);
            p = p.Solve();
            Debug.Assert(p.Cells.Select(c => c[0]).SequenceEqual(new int[] {
                4, 8, 3, 9, 2, 1, 6, 5, 7,
                9, 6, 7, 3, 4, 5, 8, 2, 1,
                2, 5, 1, 8, 7, 6, 4, 9, 3,
                5, 4, 8, 1, 3, 2, 9, 7, 6,
                7, 2, 9, 5, 6, 4, 1, 3, 8,
                1, 3, 6, 7, 9, 8, 2, 4, 5,
                3, 7, 2, 6, 8, 9, 5, 1, 4,
                8, 1, 4, 2, 5, 3, 7, 6, 9,
                6, 9, 5, 4, 1, 7, 3, 8, 2
            }));
        }

        public static void SimpleTest2()
        {
            var p = new SudokuPuzzle("53..7....6..195....98....6.8...6...34..8.3..17...2...6.6....28....419..5....8..79");
            p = p.Solve();
            Debug.Assert(p.Cells.Select(c => c[0]).SequenceEqual(new int[] {
                5, 3, 4, 6, 7, 8, 9, 1, 2,
                6, 7, 2, 1, 9, 5, 3, 4, 8,
                1, 9, 8, 3, 4, 2, 5, 6, 7,
                8, 5, 9, 7, 6, 1, 4, 2, 3,
                4, 2, 6, 8, 5, 3, 7, 9, 1,
                7, 1, 3, 9, 2, 4, 8, 5, 6,
                9, 6, 1, 5, 3, 7, 2, 8, 4,
                2, 8, 7, 4, 1, 9, 6, 3, 5,
                3, 4, 5, 2, 8, 6, 1, 7, 9
            }));
        }

        public static void MultiSolveTest()
        {
            var puzzle = new SudokuPuzzle("   456789679813245548927136   594678857361492964782513   648957796135824485279361");
            var solutions = SudokuPuzzle.MultiSolve(puzzle);
            Debug.Assert(solutions.Count() == 12);

            solutions = SudokuPuzzle.MultiSolve(puzzle, 2);
            Debug.Assert(solutions.Count() == 2);
        }

        public static void RandomGridTest()
        {
            var puzzle = SudokuPuzzle.RandomGrid(9);
            Debug.Assert(puzzle.Cells.All(c => c.Length == 1));
            var puzzle2 = new SudokuPuzzle(puzzle.Cells.Select(c => c.Single()).ToArray());
        }

        public static void Top95TimedTest()
        {
            var stream = new StreamReader("top95.txt");
            var puzzles = new List<String>();
            while (!stream.EndOfStream)
                puzzles.Add(stream.ReadLine());
            stream.Close();

            var times = puzzles.Select(p =>
            {
                DateTime start = DateTime.Now;
                var puz = new SudokuPuzzle(p).Solve();
                SudokuPuzzle.Output(puz);
                return (DateTime.Now - start);
            }).ToList();
            Console.WriteLine("Average: " + times.Average(ts => ts.TotalSeconds));
            Console.WriteLine("Worst: " + times.Max(ts => ts.TotalSeconds));
        }

        public static void ApplyConstraintsTest()
        {
            int cell = 0;
            int value = 1;
            var puzzle = new SudokuPuzzle(9).ApplyConstraints(cell, value);

            Debug.Assert(puzzle.Cells[cell].Single() == value);
            foreach (int peerIndex in puzzle.Peers(cell))
                Debug.Assert(!puzzle.Cells[peerIndex].Contains(value));
        }

        public static void FindSingularizedCellsTest()
        {
            var puzzle = new SudokuPuzzle("1234567..........................................................................");
            var puzzle2 = puzzle.PlaceValue(7, 8);
            var ConstrainedCellIndexes = SudokuPuzzle.FindSingularizedCells(puzzle, puzzle2, 7);
            Debug.Assert(ConstrainedCellIndexes.SequenceEqual(new int[] { 8 }));
        }
    }
}
