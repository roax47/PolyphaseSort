using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBD_P1
{

    public class DBManager
    {

        private Tape[] tapes;
        private int readOperation = 0;
        private int writeOperation = 0;
        private int tapesCount;

        public DBManager(Tape[] tapes)
        {
            this.tapes = tapes;
            this.tapesCount = tapes.Length;
        }

        public Record GetRecord(int tapeNumber = 0,bool printingMode = false)
        {
            
            if (tapeNumber >= tapesCount)
            {
                throw new Exception("Tape does not exist");
            }
            if (tapes[tapeNumber].blockBuffer == null || tapes[tapeNumber].blockPosition == Sorter.blockSize)
            {
                tapes[tapeNumber].ReadBlock(ref readOperation,printingMode);
            }
            if (tapes[tapeNumber].blockPosition >= tapes[tapeNumber].blockBuffer.Length)
            {
                tapes[tapeNumber].blockBuffer = null;
                return null;
            }
            var returnArray = tapes[tapeNumber].blockBuffer.Skip(tapes[tapeNumber].blockPosition).Take(Sorter.recordSize).ToArray();
            tapes[tapeNumber].blockPosition += Sorter.recordSize;
            return new Record(BitConverter.ToDouble(returnArray, 0), BitConverter.ToDouble(returnArray, 8), BitConverter.ToDouble(returnArray, 16));

        }

        public void SetRecord(Record newRecord, int tapeNumber = 0)
        {
            if (tapeNumber >= tapesCount)
            {
                throw new Exception("Tape does not exist");
            }
            if (this.tapes[tapeNumber].writeBlockPosition == Sorter.blockSize)
            {
                tapes[tapeNumber].WriteBytes(ref writeOperation);
            }
            Array.Copy(newRecord.ToByteArray(),0, tapes[tapeNumber].writeBlockBuffer, tapes[tapeNumber].writeBlockPosition, Sorter.recordSize);
            tapes[tapeNumber].writeBlockPosition += Sorter.recordSize;
        }

        public void ClearTape(int tapeNumber)
        {
            this.tapes[tapeNumber].Clear();
        }

        //return number of empty tapes
        public int PrintTapes(List<int> tapesToPrint,bool printValues)
        {
            int emptyTapes = 0;
            foreach (var tapeNumber in tapesToPrint)
            {
                byte[] savedBlockBuffer = null;
                int savedBlockPosition = 0;
                long previousBinaryPos = 0;
                int series = 1;
                bool records = false;
                bool didExist = this.tapes[tapeNumber].binaryReader != null;
                if (didExist)
                {
                    savedBlockBuffer = this.tapes[tapeNumber].blockBuffer;
                    savedBlockPosition = this.tapes[tapeNumber].blockPosition;
                    previousBinaryPos = this.tapes[tapeNumber].binaryReader.BaseStream.Position;
                }

                Console.WriteLine(String.Format("\n  ---------------\n | Tape number {0} |\n  ---------------", tapeNumber));
                var prevValue = double.MinValue;
                while(true)
                {
                    var record = GetRecord(tapeNumber,true);
                    if (record == null)
                    {
                        break;
                    }
                    records = true;
                    var value = record.Calculate();
                    if (value < prevValue && prevValue != double.MinValue)
                    {
                        series++;
                        if(printValues)Console.Write(" |   ");
                    }
                    if (printValues) Console.Write(string.Format("{0:F3}  ", value));
                    prevValue = value;
                }

                if (!records) series = 0;
                Console.Write(string.Format("\n\n [Series count:{0}]", series));


                if (didExist)
                {
                    this.tapes[tapeNumber].binaryReader.BaseStream.Seek(previousBinaryPos, SeekOrigin.Begin);
                    this.tapes[tapeNumber].blockBuffer = savedBlockBuffer;
                    this.tapes[tapeNumber].blockPosition = savedBlockPosition;
                }
                else this.tapes[tapeNumber].binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
            return emptyTapes;
        }

        public void DecrementBlockPosition(int tapeNumber)
        {
            if (this.tapes[tapeNumber].blockBuffer != null)
            {
                this.tapes[tapeNumber].blockPosition -= Sorter.recordSize;
            }
        }
        static public void GenerateRandomFile(int numberOfRecords, string name)
        {
            var file = File.Create(name);
            file.Close();
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(file.Name, FileMode.Append)))
            {
                Random rand = new Random();
                for (int i = 0; i < numberOfRecords; i++)
                {
                    binaryWriter.Write(GenerateRecord(rand).ToByteArray());
                }
            }          
        }

        static public void GenerateFileFromInput(int numberOfRecords, string name)
        {
            var file = File.Create(name);
            file.Close();
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(file.Name, FileMode.Append)))
            {
                Console.WriteLine(String.Format("Input {0} records by spliting 3 doubles with ',' . Remember that discriminant has to be >= 0",numberOfRecords));
                for (int i = 0; i < numberOfRecords; i++)
                {
                    Console.WriteLine(String.Format("Record {0}", i + 1));
                    var numbers = Console.ReadLine().Split(',');
                    if(numbers.Length != 3)
                    {
                        Console.WriteLine("Invalid input - aborting");
                        binaryWriter.Close();
                        File.Delete(name);
                        break;
                    }
                    var record = new Record(double.Parse(numbers[0]), double.Parse(numbers[1]), double.Parse(numbers[2]));
                    binaryWriter.Write(record.ToByteArray());
                }
            }
        }

        public static Record GenerateRecord(Random rand)
        {
            
            var max = 100.0;
            var min = -100.0;
            var b = rand.NextDouble() * (max - min) + min;

            // ac < B^2/4 | we allow negative number up to -100.0
            var temp = (b * b) / 4;
            var ac = rand.NextDouble() * (temp - min ) + min;

            // split a and c - make a 2,5%-5% of te total ac - calculated numbers -b/a will be scaled to be higher 
            var a = ac / rand.Next(20,40);
            var c = ac/a;

            return new Record(a, b, c);
        }
        public bool CheckForAnotherEmpty(int emptyTape)
        {
            List<int> listToCheck;
            if (emptyTape == 1) listToCheck = new List<int> { 2, 3 };
            else if (emptyTape == 2) listToCheck = new List<int> { 1, 3 };
            else listToCheck = new List<int> { 1, 2 };

            foreach(var tape in listToCheck)
            {
                var savedBlockBuffer = this.tapes[tape].blockBuffer;
                var savedBlockPosition = this.tapes[tape].blockPosition;
                long previousBinaryPos = 0;
                if (this.tapes[tape].binaryReader != null) previousBinaryPos = this.tapes[tape].binaryReader.BaseStream.Position;

                if (GetRecord(tape,true)==null) return true;
                else
                { 
                    if (this.tapes[tape].binaryReader!=null) this.tapes[tape].binaryReader.BaseStream.Seek(previousBinaryPos, SeekOrigin.Begin);
                    this.tapes[tape].blockBuffer = savedBlockBuffer;
                    this.tapes[tape].blockPosition = savedBlockPosition;
                }
            }
            return false;
        }
        public int GetReadOperationsCount()
        {
            return this.readOperation;
        }

        public int GetWriteOperationsCount()
        {
            return this.writeOperation;
        }
        public void CloseWriter(int tape)
        {
            if (tapes[tape].writeBlockPosition != 0) tapes[tape].WriteBytes(ref writeOperation);
            if (tapes[tape].binaryWriter != null) this.tapes[tape].binaryWriter.Close();
            this.tapes[tape].binaryWriter = null;
        }
        public void Delete()
        {
            foreach (var tape in this.tapes)
            {
                if (tape.binaryReader != null) tape.binaryReader.Close();
            }
        }
    }
}
