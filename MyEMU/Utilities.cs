using System;
using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace MyEMU
{
    public class Utilities
    {
        #region WINAPI
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(int hwnd);
        #endregion

        public static byte[] loadBinaryFile(string sFilename)
        {
            return File.ReadAllBytes(sFilename);
        }

        #region FONT HELPERS
        public static byte[][] loadCharacterROM(string sFilename, int iCharHeight = 8, bool bInvert = false, int iFileCharOffset = 0, int iFirstChar = 0)
        {
            byte[] abCharPlain = loadBinaryFile(sFilename);

            int iFileCharEnd = iFileCharOffset + (256 * iCharHeight); // assume 256 chars defined in file
            if (iFileCharEnd > abCharPlain.Length) iFileCharEnd = abCharPlain.Length; // if file is shorter, cut down
            int iCharDataLength = iFileCharEnd - iFileCharOffset; // get length of character data area in file
            int iCharCount = iFirstChar + (iCharDataLength / iCharHeight); // determine character count

            byte[][] abCharacterSet = new byte[iCharCount][]; // create 2-dim array according to character count

            int iCharIndex = 0, iLineIndex = 0;

            // fill up to first character with empty (0) lines
            while(iCharIndex<iFirstChar)
                abCharacterSet[iCharIndex++] = new byte[iCharHeight];

            // transform character lines from file into 2-dim array
            for (int c = iFileCharOffset; c < iFileCharEnd; c++)
            {
                if (abCharacterSet[iCharIndex] == null) abCharacterSet[iCharIndex] = new byte[iCharHeight];

                if(bInvert)
                    abCharacterSet[iCharIndex][iLineIndex] = (byte)~abCharPlain[c];
                else
                    abCharacterSet[iCharIndex][iLineIndex] = abCharPlain[c];

                iLineIndex++;

                if (iLineIndex == iCharHeight)
                {
                    iLineIndex = 0;
                    iCharIndex++;
                }
            }

            return abCharacterSet;
        }

        public static byte[][] loadCharacterROM_FLIP(string sFilename, int iCharHeight = 8, bool bInvert = false)
        {
            byte[] abCharPlain = loadBinaryFile(sFilename);

            int iLength = 256 * iCharHeight;
            if (iLength > abCharPlain.Length) iLength = abCharPlain.Length;

            // flip/reverse bits from right-to-left to left-to-right
            for (int c = 0; c < iLength; c++)
            {
                byte fromMask = 0x80;
                byte toMask = 0x01;
                byte bNew = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((abCharPlain[c] & fromMask) == fromMask) bNew |= toMask;
                    fromMask >>= 1;
                    toMask <<= 1;
                }
                abCharPlain[c] = bNew;
            }

            // transform into 2-dim array
            byte[][] abCharacterSet = new byte[iLength / iCharHeight][];
            int iCharIndex = 0, iLineIndex = 0;
            for (int c = 0; c < iLength; c++)
            {
                if (abCharacterSet[iCharIndex] == null) abCharacterSet[iCharIndex] = new byte[iCharHeight];

                if (bInvert)
                    abCharacterSet[iCharIndex][iLineIndex] = (byte)~abCharPlain[c];
                else
                    abCharacterSet[iCharIndex][iLineIndex] = abCharPlain[c];

                iLineIndex++;

                if (iLineIndex == iCharHeight)
                {
                    iLineIndex = 0;
                    iCharIndex++;
                }
            }

            return abCharacterSet;
        }

        public static byte[][] loadCharacterROM_2BANK(string sFilename, int iCharHeight = 8, bool bInvert = false)
        {
            byte[] abCharPlain = loadBinaryFile(sFilename);

            int iLength = 256 * iCharHeight;
            if (iLength > abCharPlain.Length) iLength = abCharPlain.Length;

            // transform into 2-dim array
            byte[][] abCharacterSet = new byte[iLength / iCharHeight][];
            int iCharIndex = 0, iLineIndex = 0, iLineOffset = 0;
            for (int c = 0; c < iLength; c++)
            {
                if (abCharacterSet[iCharIndex] == null) abCharacterSet[iCharIndex] = new byte[iCharHeight];

                if (bInvert)
                    abCharacterSet[iCharIndex][iLineIndex + iLineOffset] = (byte)~abCharPlain[c];
                else
                    abCharacterSet[iCharIndex][iLineIndex + iLineOffset] = abCharPlain[c];

                iLineIndex++;

                if (iLineIndex == (iCharHeight/2))
                {
                    iLineIndex = 0;
                    iCharIndex++;
                }

                if( iCharIndex > 255 )
                {
                    iCharIndex = 0;
                    iLineOffset = 8;
                }
            }

            return abCharacterSet;
        }

        /// <summary>
        /// Load a font from resource (contained in the executing assembly)
        /// </summary>
        /// <param name="sFontName">Name of font file e.g. Apple ][.ttf</param>
        /// <returns>PrivateFontCollection</returns>
        public static PrivateFontCollection LoadFontFromResource(string sFontName)
        {
            PrivateFontCollection fc = new PrivateFontCollection();

            string sResource = null;

            // find embedded resource name
            foreach (string s in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                if (s.EndsWith(sFontName))
                {
                    sResource = s;
                    break;
                }

            if (string.IsNullOrEmpty(sResource))
                throw new ApplicationException("Font" + sFontName + " not found in resources!");

            // receive resource stream
            Stream fontStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(sResource);
            // create an unsafe memory block for the font data
            System.IntPtr data = Marshal.AllocCoTaskMem((int)fontStream.Length);
            // create a buffer to read in to
            byte[] fontdata = new byte[fontStream.Length];
            // read the font data from the resource
            fontStream.Read(fontdata, 0, (int)fontStream.Length);
            // copy the bytes to the unsafe memory block
            Marshal.Copy(fontdata, 0, data, (int)fontStream.Length);
            // pass the font to the font collection
            fc.AddMemoryFont(data, (int)fontStream.Length);
            // close the resource stream
            fontStream.Close();
            // free the unsafe memory
            Marshal.FreeCoTaskMem(data);

            return fc;
        }

        /// <summary>
        /// create a character set represented as bitmaps from a ROM file character set
        /// </summary>
        /// <param name="sFileName">Name of the ROM file e.g. PET_characters-1.901447-08.bin</param>
        /// <param name="iPixelSize">Pixelsize to be used for generated bitmaps</param>
        /// <param name="iRangeFrom">generate from character index e.g. 0</param>
        /// <param name="iRangeTo">generate to character index e.g. 127 or 255</param>
        /// <returns>Bitmap[] - array of bitmaps representing the character set</returns>
        static public Bitmap[] CreateCharacterSetFromROM(string sFileName, int iPixelSize, int iRangeFrom, int iRangeTo)
        {
            return CreateCharacterSetFromROM(sFileName, iPixelSize, iRangeFrom, iRangeTo, -1, -1);
        }

        /// <summary>
        /// create a character set represented as bitmaps from a ROM file character set
        /// </summary>
        /// <param name="sFileName">Name of the ROM file e.g. PET_characters-1.901447-08.bin</param>
        /// <param name="iPixelSize">Pixelsize to be used for generated bitmaps</param>
        /// <param name="iRangeFrom">generate from character index e.g. 0</param>
        /// <param name="iRangeTo">generate to character index e.g. 127 or 255</param>
        /// <param name="iRangeInvFrom">generate inversed from character index e.g. 0</param>
        /// <param name="iRangeInvTo">generate inversed to character index e.g. 127 or 255</param>
        /// <returns>Bitmap[] - array of bitmaps representing the character set</returns>
        static public Bitmap[] CreateCharacterSetFromROM(string sFileName, int iPixelSize, int iRangeFrom, int iRangeTo, int iRangeInvFrom, int iRangeInvTo)
        {
            Bitmap[] cs;

            byte[] bChars = File.ReadAllBytes(sFileName);

            int iCharSetSize = bChars.Length / 8;

            int iCharCount = (iRangeTo - iRangeFrom) + 1;
            if (iRangeInvFrom != -1 && iRangeInvTo != -1) iCharCount += (iRangeInvTo - iRangeInvFrom) + 1;

            cs = new Bitmap[iCharCount];

            int iCharIndex = 0;
            int iOffset = iRangeFrom * 8;

            for (int ch = iRangeFrom; ch <= iRangeTo; ch++)
            {
                Bitmap bm = new Bitmap(8, 8);
                for (int r = 0; r < 8; r++)
                    for (int c = 0; c < 8; c++)
                        if ((bChars[iOffset + r] & (0x01 << c)) > 0)
                            bm.SetPixel(7 - c, r, Color.Green);

                cs[iCharIndex++] = new Bitmap(bm, new Size(8 * iPixelSize, 8 * iPixelSize));

                iOffset += 8;
            }

            if(iRangeInvFrom != -1 && iRangeInvTo != -1)
            {
                iOffset = iRangeFrom * 8;

                for (int ch = iRangeInvFrom; ch <= iRangeInvTo; ch++)
                {
                    Bitmap bm = new Bitmap(8, 8);
                    for (int r = 0; r < 8; r++)
                        for (int c = 0; c < 8; c++)
                            if ((bChars[iOffset + r] & (0x01 << c)) == 0)
                                bm.SetPixel(7 - c, r, Color.Green);

                    cs[iCharIndex++] = new Bitmap(bm, new Size(8 * iPixelSize, 8 * iPixelSize));

                    iOffset += 8;
                }
            }

            return cs;
        }

        #endregion
    }
}
