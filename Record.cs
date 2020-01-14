using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SBD_P1
{
    [StructLayout(LayoutKind.Sequential)]
    public class Record
    {
        double a;
        double b;
        double c;

        public Record(double a,double b,double c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public double Calculate()
        {
            if ((b * b) - (4 * a * c) < 0)
            {
                throw new Exception("Delta <0 Invalid DATA");
            }
            return -b / a;

        }

        public byte[] ToByteArray()
        {
            byte[] bytes = new byte[Marshal.SizeOf(typeof(Record))];
            GCHandle pinStructure = GCHandle.Alloc(this, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(pinStructure.AddrOfPinnedObject(), bytes, 0, bytes.Length);
                return bytes;
            }
            finally
            {
                pinStructure.Free();
            }
        }
    }
}
