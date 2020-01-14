using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SBD_P1
{
    public class Tape
    {
        
        
        private string filePath;
        public BinaryReader binaryReader;
        public BinaryWriter binaryWriter;
        public byte[] blockBuffer;
        public byte[] writeBlockBuffer;
        public int blockPosition;
        public int writeBlockPosition = 0;
        public Tape(string filePath)
        {
            this.filePath = filePath;
            writeBlockBuffer = new byte[Sorter.blockSize]; 
        }

        public void ReadBlock(ref int operationCounter, bool printingMode = false)
        {
            if (this.binaryReader == null) this.binaryReader = new BinaryReader(new FileStream(filePath, FileMode.Open));
            this.blockBuffer = this.binaryReader.ReadBytes(Sorter.blockSize);
            this.blockPosition = 0;
            if (printingMode == false) operationCounter++;
            
        }

        public void WriteBytes(ref int operationCounter)
        {
            if (this.binaryWriter == null) binaryWriter = new BinaryWriter(new FileStream(this.filePath, FileMode.Append));
            binaryWriter.Write(this.writeBlockBuffer.Take(this.writeBlockPosition).ToArray());
            this.writeBlockPosition = 0;
            operationCounter++; 
        }

        public void Clear()
        {
            if (binaryReader != null) this.binaryReader.Close();
            File.WriteAllText(this.filePath, string.Empty);
            this.blockBuffer = null;         
            this.binaryReader = null;
        }
    }
}
