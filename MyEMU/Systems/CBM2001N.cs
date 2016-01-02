using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using MyEMU.CPU;
using MyEMU.I_O;
using System.Windows.Forms;


namespace MyEMU.Systems
{
    // Neutral - no ROM set
    public class CBM2001N : ISystemBase
    {
        private const int RETRACE_RATE = 60; // screen retraces per second (simulating V-BLANK)

        protected MOS6502 m_cpu;
        private MOS6520 m_pia1;     // PIA 1 - Keyboard I/O
        private MOS6520 m_pia2;     // PIA 2 - IEEE-488 I/O
        private MOS6522 m_via;      // VIA   - I/O and timers

        protected byte[][] m_CharacterROM;
        protected byte[][] m_CharacterROMInverted;

        private byte[] m_ScreenBuffer;
        private VideoInfoStruct m_VideoInfo;
        private RenderCharacter m_funcRenderCharacter;
        private DrawScanLine m_funcDrawScanLine;

        byte[,] m_KeyboardMatrix;
        byte m_Key;
        private int m_KeyRow;

        private bool m_bRun;

        #region ISystemBase
        string ISystemBase.Title
        {
            get { return "CBM2001N"; }
        }

        void ISystemBase.setupBase()
        {
            // inititalize system components
            m_cpu = new MOS6502();
            m_cpu.installRAMBank(32);
            RAMextension ram = new RAMextension(m_cpu, 0xE800, 2); // for BASIC v2
            m_pia1 = new MOS6520("PIA1", m_cpu, 0xE810);
            m_pia2 = new MOS6520("PIA2", m_cpu, 0xE820);
            m_via = new MOS6522("VIA", m_cpu, 0xE840);

            // set I/O and interupt lines
            m_pia1.OutputA = receive_PIA1_A;
            m_pia1.InterruptLine = m_cpu.signalInterrupt;
            m_pia2.InterruptLine = m_cpu.signalInterrupt;
            m_via.InterruptLine = m_cpu.signalInterrupt;

            initKeyboard();

            // setup display
            m_VideoInfo = new VideoInfoStruct();
            m_VideoInfo.Rows = 25;
            m_VideoInfo.Cols = 40;
            m_VideoInfo.CharHeight = 8;
            m_VideoInfo.CharWidth = 8;
            m_VideoInfo.FontColor = Color.Green;
            m_VideoInfo.BackColor = Color.Black;


            // setup screen buffer
            m_ScreenBuffer = new byte[0x0800];
            m_cpu.registerMemoryAccess(0x8000, 0x8800, read, write);

            // load ROMs
            loadROMs();

            m_cpu.Reset();
        }

        void ISystemBase.setupVideo(RenderCharacter funcRenderCharacter,
                   DrawScanLine funcDrawScanLine)
        {
            m_funcRenderCharacter = funcRenderCharacter;
            m_funcDrawScanLine = funcDrawScanLine;
        }

        void ISystemBase.run()
        {

            int iRetraceIntervalMS = 1000 / RETRACE_RATE;
            DateTime dtNextRetrace = DateTime.UtcNow.AddMilliseconds(iRetraceIntervalMS);

            m_bRun = true;

            bool bRetraceActive = false;

            int ScanLine = 0;
            int iScanLineFlip = m_VideoInfo.Rows * m_VideoInfo.CharHeight;

            while (m_bRun)
            {

                // screen emulate one scan line
                m_funcDrawScanLine(ScanLine);
                ScanLine++;
                if (ScanLine >= iScanLineFlip) ScanLine = 0;

                if (bRetraceActive)
                {
                    // emulate screen retrace connected to PIA1 CB1 and VIA PB5
                    bRetraceActive = false;
                    m_pia1.CB1 = Signal.Rise;
                    m_via.InputB(5, bRetraceActive);
                }

                if (DateTime.UtcNow >= dtNextRetrace)
                {
                    Application.DoEvents();

                    // emulate screen retrace connected to PIA1 CB1 and VIA PB5
                    bRetraceActive = true;
                    m_pia1.CB1 = Signal.Fall;
                    m_via.InputB(5, bRetraceActive);

                    m_via.emulateCycle();

                    dtNextRetrace = DateTime.UtcNow.AddMilliseconds(iRetraceIntervalMS);
                }

                m_cpu.emulateCycle();
            }
        }

        void ISystemBase.stop()
        {
            m_bRun = false;
        }

        void ISystemBase.OnKeyDown(System.Windows.Forms.KeyEventArgs e) { }
        void ISystemBase.OnKeyUp(System.Windows.Forms.KeyEventArgs e) { }
        void ISystemBase.OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
        {
            // store the key for keyboard I/O
            m_Key = (byte)e.KeyChar;

            if (m_Key >= 0x61 && m_Key <= 0x7A) // to upper case
                m_Key -= 0x20;
        }

        VideoInfoStruct ISystemBase.VideoInfo
        {
            get { return m_VideoInfo; }
        }
        #endregion

        #region Display
        public byte read(UInt16 address)
        {
            int iOffset = address - 0x8000;
            return m_ScreenBuffer[iOffset];
        }

        public void write(UInt16 address, byte b)
        {
            int iOffset = address - 0x8000;
            m_ScreenBuffer[iOffset] = b;

            if (iOffset < (m_VideoInfo.Rows * m_VideoInfo.Cols))
            {
                int iY = iOffset / m_VideoInfo.Cols;
                int iX = iOffset % m_VideoInfo.Cols;
                int iCharIndex = b & 0x7F;
                bool bInvert = (b & 0x80) == 0x80;

                m_funcRenderCharacter(iX,iY,
                    bInvert ?
                        m_CharacterROMInverted[iCharIndex]
                        :
                        m_CharacterROM[iCharIndex]);
            }
        }
        #endregion

        #region I/O keyboard
        private void initKeyboard()
        {
            // N - normal keyboard
            m_KeyboardMatrix = new byte[10, 8] 
            {
                {0x1D, 0x13, 0x5F, 0x28, 0x26, 0x25, 0x23, 0x21}, //]s_(&%#!
                {0x14, 0x11, 0xFF, 0x29, 0x5C, 0x27, 0x24, 0x22}, //tq.)\'0x"
                {0x39, 0x37, 0x5E, 0x4F, 0x55, 0x54, 0x45, 0x51}, //97^outeq
                {0x2F, 0x38, 0xFF, 0x50, 0x49, 0x59, 0x52, 0x57}, ///8.piyrw
                {0x36, 0x34, 0xFF, 0x4C, 0x4A, 0x47, 0x44, 0x41}, //64.ljgda
                {0x2A, 0x35, 0xFF, 0x3A, 0x4B, 0x48, 0x46, 0x53}, //*5.:khfs
                {0x33, 0x31, 0x0D, 0x3B, 0x4D, 0x42, 0x43, 0x5A}, //31m;mbcz
                {0x2B, 0x32, 0xFF, 0x3F, 0x2C, 0x4E, 0x56, 0x58}, //+2.?,nvx
                {0x2D, 0x30, 0x00, 0x3E, 0xFF, 0x5D, 0x40, 0x00}, //-0.>.]@.
                {0x3D, 0x2E, 0xFF, 0x03, 0x3C, 0x20, 0x5B, 0x12}, //=..c< [r
            };

            m_Key = 0;
            m_KeyRow = 0x0F;
            set_PIA1_B();
        }


        private void receive_PIA1_A(byte b)
        {
            m_KeyRow = b & 0x0F;
            set_PIA1_B();
        }

        private void set_PIA1_B()
        {
            if (m_Key == 0)
                m_pia1.InputB(0xFF); // no input from keyboard
            else if (m_KeyRow > 9)
                m_pia1.InputB(0xFF); // no input from keyboard
            else
                m_pia1.InputB(findKey(m_KeyRow));
        }

        private byte findKey(int KeyRow)
        {
            byte bKeyFound = 0xFF;

            byte bKeyMask = 0x80;
            for (int iBit = 0; iBit < 8; iBit++)
            {
                if (m_KeyboardMatrix[KeyRow, iBit] == m_Key)
                {
                    bKeyFound -= bKeyMask;
                    m_Key = 0; // clear
                    break;
                }
                bKeyMask >>= 1;
            }

            return bKeyFound;
        }
        #endregion


        #region HELPERS
        protected virtual void loadROMs()
        {
            throw new NotImplementedException("CBM2001N.loadROMs w/o ROM set specified");
        }
        #endregion
    }

    // BASIC V1 ROM set
    public class CBM2001N_B1 : CBM2001N 
    {
        protected override void loadROMs()
        {
            m_CharacterROM = Utilities.loadCharacterROM("ROMs\\PET_characters-1.901447-08.bin",8);
            m_CharacterROMInverted = Utilities.loadCharacterROM("ROMs\\PET_characters-1.901447-08.bin",8,true);

            ROMmodule rmTemp;
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_rom-1-c000.901447-09.bin", 0xC000);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_rom-1-c800.901447-02.bin", 0xC800);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_rom-1-d000.901447-03.bin", 0xD000);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_rom-1-d800.901447-04.bin", 0xD800);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_rom-1-e000.901447-05.bin", 0xE000);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_rom-1-f000.901447-06.bin", 0xF000);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_rom-1-f800.901447-07.bin", 0xF800);
        }
    }

    // BASIC V2 ROM set
    public class CBM2001N_B2 : CBM2001N
    {
        protected override void loadROMs()
        {
            m_CharacterROM = Utilities.loadCharacterROM("ROMs\\PET_characters-2.901447-10.bin",8);
            m_CharacterROMInverted = Utilities.loadCharacterROM("ROMs\\PET_characters-2.901447-10.bin",8,true);

            ROMmodule rmTemp;
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_basic-2-c000.901465-01.bin", 0xC000);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_basic-2-d000.901465-02.bin", 0xD000);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_edit-2-n.901447-24.bin", 0xE000);
            rmTemp = new ROMmodule(m_cpu, "ROMs\\PET_kernal-2.901465-03.bin", 0xF000);
        }
    }
}
