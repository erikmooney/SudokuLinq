using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SudokuLinq
{
    class Program
    {
        static void Main(string[] args)
        {
            SudokuPuzzleTests.TestPeers();
            SudokuPuzzleTests.ApplyConstraintsTest();
            SudokuPuzzleTests.FindSingularizedCellsTest();
            SudokuPuzzleTests.SimpleTest();
            SudokuPuzzleTests.SimpleTest2();
            SudokuPuzzleTests.MultiSolveTest();
            SudokuPuzzleTests.RandomGridTest();
            SudokuPuzzleTests.Top95TimedTest();
            Console.ReadKey(true);
        }
    }
}