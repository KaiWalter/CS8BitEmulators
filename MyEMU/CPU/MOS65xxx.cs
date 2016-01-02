using System;
using System.IO;

namespace MyEMU.CPU
{
    public class MOS65xxx : CPU8Bit
    {
        public enum Addressing
        {
            _not_set_,
            implied, accumulator, immidiate,
            zeropage, zeropageX, zeropageY,
            absolute, absoluteX, absoluteY,
            indirect, indirectX, indirectY,
            relative
        };

        public MOS65xxx() : base() { }

        public void loadCC65BIN(string sFilename)
        {
            byte[] bBIN = Utilities.loadBinaryFile(sFilename);

            int iOffset = 0;
            while (iOffset < bBIN.Length)
            {
                int iAddress = bBIN[iOffset] + (bBIN[iOffset + 1] * 0x100);
                iOffset += 2;
                int iLength = bBIN[iOffset] + (bBIN[iOffset + 1] * 0x100);
                iOffset += 2;

                byte[] bBlock = new byte[iLength];

                Array.Copy(bBIN, iOffset, bBlock, 0, iLength);

                loadMemfromByteArray(ref bBlock, (UInt16)iAddress);

                iOffset += iLength;
            }

        }


    }
}
