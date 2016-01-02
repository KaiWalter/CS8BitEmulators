using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyEMU.CPU;
using MyEMU.I_O;
using System.Drawing;
using System.Timers;
using System.Windows.Forms;


namespace MyEMU.Systems
{
    class EHBASIC6502 : ISystemBase
    {
        private const int RETRACE_RATE = 60; // screen retraces per second (simulating V-BLANK)

        protected MOS6502 m_cpu;

        protected byte[][] m_CharacterROM;
        protected byte[][] m_CharacterROMInverted;

        private bool m_bNeedRefresh;
        private Queue<byte> m_DisplayQueue;
        private char[][] m_ScreenBuffer;

        private Queue<byte> m_KeyboardQueue;

        private int m_iCursorX;
        private int m_iCursorY;

        private VideoInfoStruct m_VideoInfo;
        private RenderCharacter m_funcRenderCharacter;
        private DrawScanLine m_funcDrawScanLine;

        private bool m_bRun;

        #region ISystemBase
        string ISystemBase.Title
        {
            get { return "EHBASIC-6502"; }
        }

        void ISystemBase.setupBase()
        {
            // inititalize system components
            m_cpu = new MOS6502();

            m_cpu.installRAMBank(64);

            m_cpu.registerMemoryAccess(0xF001, readDsp, writeDsp);
            m_cpu.registerMemoryAccess(0xF004, readKbd, writeKbd);

            // setup display
            m_VideoInfo = new VideoInfoStruct();
            m_VideoInfo.Rows = 25;
            m_VideoInfo.Cols = 40;
            m_VideoInfo.CharHeight = 8;
            m_VideoInfo.CharWidth = 8;
            m_VideoInfo.FontColor = Color.Orange;
            m_VideoInfo.BackColor = Color.Black;

            m_iCursorX = m_iCursorY = 0;
            m_ScreenBuffer = new char[m_VideoInfo.Rows][];
            for (int r = 0; r < m_VideoInfo.Rows; r++)
            {
                m_ScreenBuffer[r] = new char[m_VideoInfo.Cols];
                for (int c = 0; c < m_VideoInfo.Cols; c++) m_ScreenBuffer[r][c] = ' ';
            }
            m_bNeedRefresh = false;

            m_DisplayQueue = new Queue<byte>(10);
            m_KeyboardQueue = new Queue<byte>(5);

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


            int ScanLine = 0;
            int iScanLineFlip = m_VideoInfo.Rows * m_VideoInfo.CharHeight;

            while (m_bRun)
            {
                // screen emulate one scan line
                m_funcDrawScanLine(ScanLine);
                ScanLine++;
                if (ScanLine >= iScanLineFlip) ScanLine = 0;

                if (DateTime.UtcNow >= dtNextRetrace)
                {
                    Application.DoEvents();

                    if (m_bNeedRefresh)
                    {
                        m_bNeedRefresh = false;

                        // check queue for characters to display
                        while (m_DisplayQueue.Count > 0)
                            outputDsp(m_DisplayQueue.Dequeue());
                    }

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
            if (e.KeyChar >= 0x20 || e.KeyChar == 0x0D)
            {
                byte key = (byte)e.KeyChar;

                if (key >= 0x61 && key <= 0x7A) // to upper case
                    key -= 0x20;

                m_KeyboardQueue.Enqueue(key);
            }
        }

        VideoInfoStruct ISystemBase.VideoInfo
        {
            get { return m_VideoInfo; }
        }
        #endregion

        #region Display
        public byte readDsp(UInt16 address)
        {
            return 0;
        }

        public void writeDsp(UInt16 address, byte dsp)
        {
            if (dsp != 0) outputDsp(dsp);
        }

        public byte readKbd(UInt16 address)
        {
            if (m_KeyboardQueue.Count > 0)
                return m_KeyboardQueue.Dequeue();

            return 0;
        }

        public void writeKbd(UInt16 address, byte kbd) { }

        public void outputDsp(byte dsp)
        {
            // clear old cursor
            m_funcRenderCharacter(m_iCursorX, m_iCursorY, m_CharacterROM[m_ScreenBuffer[m_iCursorY][m_iCursorX]]);

            // display new character
            switch (dsp)
            {
                case 0x0D:
                    m_iCursorX = 0;
                    m_iCursorY++;
                    break;
                default:
                    m_ScreenBuffer[m_iCursorY][m_iCursorX] = (char)dsp;
                    m_funcRenderCharacter(m_iCursorX, m_iCursorY, m_CharacterROM[dsp]);
                    m_iCursorX++;
                    break;
            }

            // check cursor position
            if (m_iCursorX == m_VideoInfo.Cols)
            {
                m_iCursorX = 0;
                m_iCursorY++;
            }
            if (m_iCursorY == m_VideoInfo.Rows)
            {
                newLine();
                m_iCursorY--;
            }

            // draw new cursor
            m_funcRenderCharacter(m_iCursorX, m_iCursorY, m_CharacterROMInverted[m_ScreenBuffer[m_iCursorY][m_iCursorX]]);

        }

        void newLine()
        {
            for (int r = 0; r < m_VideoInfo.Rows - 1; r++)
                m_ScreenBuffer[r] = m_ScreenBuffer[r + 1];

            m_ScreenBuffer[m_VideoInfo.Rows - 1] = new char[m_VideoInfo.Cols];
            for (int c = 0; c < m_VideoInfo.Cols; c++) m_ScreenBuffer[m_VideoInfo.Rows - 1][c] = ' ';

            for (int r = 0; r < m_VideoInfo.Rows; r++)
                for (int c = 0; c < m_VideoInfo.Cols; c++)
                    m_funcRenderCharacter(c, r, m_CharacterROM[m_ScreenBuffer[r][c]]);
        }
        #endregion

        #region HELPERS
        protected virtual void loadROMs()
        {
            m_CharacterROM = Utilities.loadCharacterROM_FLIP("ROMs\\Apple1_charmap.rom",8,false);
            m_CharacterROMInverted = Utilities.loadCharacterROM_FLIP("ROMs\\Apple1_charmap.rom",8,true);

            m_cpu.loadProgram("ROMs\\MOS6502_ehbasic.bin", 0xC000);

        }
        #endregion

    }
}
