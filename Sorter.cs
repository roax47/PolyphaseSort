using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SBD_P1
{
    class Sorter
    {
        private static DBManager dbManager;
        private static string filePath = @"../../myData.bin";
        public static bool printingMode = false;
        public static bool beforePrint = false;
        public static bool printValues = false;
        public static int recordSize = Marshal.SizeOf(typeof(Record));
        public static int blockSize = recordSize * 20;

        private enum MenuAction:int
        {
            ChooseFile = 1,
            GenerateRandom = 2,
            GenerateFromInput =3,
            BeforeToggle = 4,
            PrintFilesToggle = 5,
            PrintValuesToggle =6,
            StartSorting = 7,
            Exit = 8
        };

        private static string text;

        private static void UpdateMenuText()
        {
            text = "1. Choose file from path | Currently choosen file:" + filePath +
                    "\n2. Generate random file\n" +
                    "3. Generate file from input\n" +                    
                    "4. Toggle file printing before and after sorting | Current state: " + beforePrint +
                    "\n5. Toggle tapes printing after each phase | Current state: " + printingMode +
                    "\n6. Toggle value printing (works if printing after each phase toggled) | Current state: " + printValues +
                    "\n7. Sort file\n" +
                    "8. Exit";
        }

        static void Main(string[] args)
        {
            UpdateMenuText();
            do
            {
                Console.WriteLine(text);
                var input = GetInput();
                if (input == (int)MenuAction.Exit)
                {
                    break;
                }
                else
                {
                    HandleMenuAction((MenuAction)input);
                }         
            } while (true);

        }

        private static int GetInput()
        {
            return int.Parse(Console.ReadLine());
        }

        private static void HandleMenuAction(MenuAction action)
        {
            if (!Enum.IsDefined(typeof(MenuAction), action))
            {
                Console.Clear();
                return;
            }
            switch (action)
            {
                case MenuAction.ChooseFile:
                    filePath = GetPath();
                    Console.Clear();
                    break;
                case MenuAction.GenerateRandom:
                    filePath = GetPath();
                    Console.WriteLine("Specify number of records:");
                    DBManager.GenerateRandomFile(GetInput(), filePath);
                    Console.Clear();
                    break;
                case MenuAction.GenerateFromInput:
                    filePath = GetPath();
                    Console.WriteLine("Specify number of records:");
                    DBManager.GenerateFileFromInput(GetInput(), filePath);
                    Console.Clear();
                    break;
                case MenuAction.PrintFilesToggle:
                    printingMode = !printingMode;
                    Console.Clear();
                    break;
                case MenuAction.BeforeToggle:
                    beforePrint = !beforePrint;
                    Console.Clear();
                    break;
                case MenuAction.PrintValuesToggle:
                    printValues =! printValues;
                    Console.Clear();
                    break;
                case MenuAction.StartSorting:
                    PoliphaseSorting();
                    break;
                default:
                    Console.Clear();
                    break;
            }
            UpdateMenuText();
        }

        private static void PoliphaseSorting()
        {
            var tapeOne = File.Create("tapeOne.bin");
            var tapeTwo = File.Create("tapeTwo.bin");
            var tapeThree = File.Create("tapeThree.bin");
            tapeOne.Close();
            tapeTwo.Close();
            tapeThree.Close();

            var tapes = new Tape[4];
            tapes[0] = new Tape(filePath);
            tapes[1] = new Tape(tapeOne.Name);
            tapes[2] = new Tape(tapeTwo.Name);
            tapes[3] = new Tape(tapeThree.Name);

            dbManager = new DBManager(tapes);
           

            int tapeWithDummies;
            int phaseNumber = 1;
            int destinationTape = 3;
            int newDestinationTape = 0;

            if (beforePrint)
            {
                dbManager.PrintTapes(new List<int> { 0 }, true);
            }

            var series = Distribution();
            tapeWithDummies = series.Item3;
            if (tapeWithDummies == 1)
            {
                Console.WriteLine(String.Format("\nDistribution: {0}({2}),{1}\n", series.Item1, series.Item2, series.Item4));
            }
            else
            {
                Console.WriteLine(String.Format("\nDistribution: {0},{1}({2})\n", series.Item1, series.Item2, series.Item4));
            }
            
            if (printingMode)
            {
                dbManager.PrintTapes(new List<int> { 1, 2, 3 }, printValues);
            }

            while (true)
            {
               
                if (phaseNumber == 1) newDestinationTape = Merge(destinationTape, new Tuple<int, int>(tapeWithDummies, series.Item4));
                else newDestinationTape = Merge(destinationTape);

                dbManager.CloseWriter(destinationTape);

                if (printingMode)
                {
                    Console.WriteLine(String.Format("\n\n||||||||||||||||||||||||||||||||||||||||||\n\t\tPhase: {0}\n||||||||||||||||||||||||||||||||||||||||||\n", phaseNumber));
                    dbManager.PrintTapes(new List<int> { 1, 2, 3 }, printValues);
                    //Console.WriteLine(String.Format("\n OP:{0}", dbManager.GetOperationsCount()));
                }

                if (dbManager.CheckForAnotherEmpty(newDestinationTape))
                {
                    if(beforePrint) dbManager.PrintTapes(new List<int> { destinationTape }, true);
                    Console.WriteLine(String.Format("\n\n Total phases:{0} ReadOp: {1} WriteOp: {2} Total: {3} Sorted file on tape: {4}", phaseNumber, dbManager.GetReadOperationsCount(),
                        dbManager.GetWriteOperationsCount(), dbManager.GetReadOperationsCount() + dbManager.GetWriteOperationsCount(), destinationTape));
                    dbManager.Delete();
                    return;
                }
                destinationTape = newDestinationTape;
                phaseNumber++;
            }
        }

        private static new Tuple<int, int, int, int> Distribution()
        {           
            var tapeOneSeries = 0;
            var tapeTwoSeries = 0;
            var seriesToWrite = 1;
            var currentTape = 1;
            // 3 instead of 2 for indexing and naming clarity
            double[] previousValue = new double[3];
            previousValue[1] = double.MinValue;
            previousValue[2] = double.MinValue;

            Record record;
            double recordValue;
            while(true)
            {
                record = dbManager.GetRecord(0);
                if (record == null)
                {
                    seriesToWrite--;
                    if (currentTape == 1) tapeOneSeries++;
                    else tapeTwoSeries++;
                    break;
                }
                recordValue = record.Calculate();

                if (recordValue < previousValue[currentTape])
                {
                    seriesToWrite--;
                    if (currentTape == 1)
                    {
                        tapeOneSeries++;
                        if (seriesToWrite == 0)
                        {
                            currentTape = 2;
                            seriesToWrite = tapeOneSeries;
                           if (previousValue[currentTape] <= recordValue && previousValue[currentTape] != double.MinValue)
                            {
                                tapeTwoSeries--;
                                seriesToWrite++;
                            }
                        }
                    }
                    else
                    {
                        tapeTwoSeries++;
                        if (seriesToWrite == 0)
                        {
                            currentTape = 1;
                            seriesToWrite = tapeTwoSeries;
                            if (previousValue[currentTape] <= recordValue && previousValue[currentTape] != double.MinValue)
                            {
                                tapeOneSeries--;
                                seriesToWrite++;
                            }
                        }
                    }                    
                }
                previousValue[currentTape] = recordValue;
                //Console.WriteLine(string.Format("{0} {1}", tapeOneSeries, tapeTwoSeries));
                dbManager.SetRecord(record, currentTape);
            }

            if (seriesToWrite == tapeOneSeries && currentTape!=1) seriesToWrite = 0;
            else if (seriesToWrite == tapeTwoSeries && currentTape != 2) seriesToWrite = 0;
            dbManager.CloseWriter(1);
            dbManager.CloseWriter(2);
            return new Tuple<int, int,int,int>(tapeOneSeries, tapeTwoSeries,currentTape,seriesToWrite);
        }

        private static int Merge(int destinationTape, Tuple<int, int> dummySeries = null)
        {
            dbManager.ClearTape(destinationTape);
            int tapeOne, tapeTwo, dummyOne = 0, dummyTwo =0;
            Record recordOne = null, recordTwo = null;
            double prevOne = double.MinValue, prevTwo = double.MinValue, valueOne = double.MinValue, valueTwo = double.MinValue;

            // Determine tapes to merge
            if (destinationTape == 1) { tapeOne = 2; tapeTwo = 3; }
            else if(destinationTape == 2) { tapeOne = 1; tapeTwo = 3; }
            else { tapeOne = 1; tapeTwo = 2; }

            
            // Determine which tape has dummy series
            if (dummySeries != null)
            {
                if (tapeOne == dummySeries.Item1) dummyOne = dummySeries.Item2;
                else dummyTwo = dummySeries.Item2;
            }

            recordOne = dbManager.GetRecord(tapeOne);
            recordTwo = dbManager.GetRecord(tapeTwo);
            while (true)
            {
                if(recordOne!=null) valueOne = recordOne.Calculate();
                if(recordTwo!=null) valueTwo = recordTwo.Calculate();


                // If the series hasn't end AND the value is lower OR the series in secord tape has ended OR the other tape has remaining dummies AND the tape itself has not dummies
                if (  valueOne > prevOne && ( valueOne < valueTwo || valueTwo < prevTwo  || dummyTwo > 0 ) && dummyOne == 0)
                {                  
                    dbManager.SetRecord(recordOne, destinationTape);
                    recordOne = dbManager.GetRecord(tapeOne);
                    if (recordOne == null) prevOne = double.MaxValue;
                    else prevOne = valueOne;
                }
                else if ( valueTwo > prevTwo && ( valueOne >= valueTwo || valueOne < prevOne ||  dummyOne > 0) && dummyTwo == 0)
                {
                    dbManager.SetRecord(recordTwo, destinationTape);
                    recordTwo = dbManager.GetRecord(tapeTwo);
                    if (recordTwo == null) prevTwo = double.MaxValue;
                    else prevTwo = valueTwo;
                }
                else
                {
                    if (recordOne == null)
                    {
                        dbManager.ClearTape(tapeOne);
                        if (valueTwo < prevTwo)
                        {
                            dbManager.DecrementBlockPosition(tapeTwo);
                        }
                        return tapeOne;
                    }
                    if (recordTwo == null)
                    {
                        dbManager.ClearTape(tapeTwo);
                        if (valueOne<prevOne)
                        {
                            dbManager.DecrementBlockPosition(tapeOne);
                        }
                        return tapeTwo;
                    }
                    if (dummyOne > 0) dummyOne--;
                    if (dummyTwo > 0) dummyTwo--;
                    prevOne = double.MinValue;
                    prevTwo = double.MinValue;
                }            
            }
        }
        private static string GetPath()
        {
            Console.WriteLine("Specify path to file:");
            return Console.ReadLine();
        }

    }
}
