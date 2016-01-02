using System;
using System.Collections.Generic;
using System.Linq;
using MyEMU.CPU;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyEMU.Systems
{
    /// <summary>
    /// 
    /// </summary>
    class ZX80 : ISystemBase
    {
        Z80 m_cpu;

        private const int RETRACE_RATE = 60; // screen retraces per second (simulating V-BLANK)

        private bool m_bNeedRefresh;
        private Queue<byte> m_DisplayQueue;
        private char[][] m_ScreenBuffer;

        private int m_iCursorX;
        private int m_iCursorY;

        protected byte[][] m_CharacterROM;
        protected byte[][] m_CharacterROMInverted;

        private VideoInfoStruct m_VideoInfo;
        private RenderCharacter m_funcRenderCharacter;
        private DrawScanLine m_funcDrawScanLine;

        private Queue<KeyPressEventArgs> m_KeyboardQueue;

        private bool m_bRun;

        #region ISystemBase
        string ISystemBase.Title
        {
            get { return "ZX80"; }
        }

        /// <summary>
        /// http://searle.hostei.com/grant/z80/SimpleZ80.html
        /// </summary>
        void ISystemBase.setupBase()
        {
            m_cpu = new Z80();
            m_cpu.installRAMBank(0x2000, 16);
            m_cpu.registerIOHandler(0x80, ControlIn, ControlOut);
            m_cpu.registerIOHandler(0x81, DataIn, DataOut);

            // setup display
            m_VideoInfo = new VideoInfoStruct();
            m_VideoInfo.Rows = 25;
            m_VideoInfo.Cols = 40;
            m_VideoInfo.CharHeight = 8;
            m_VideoInfo.CharWidth = 8;
            m_VideoInfo.FontColor = Color.LightGreen;
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
            m_KeyboardQueue = new Queue<KeyPressEventArgs>(10);

            // load character set
            m_CharacterROM = Utilities.loadCharacterROM("ROMs\\SPECTRUM_ZX82.bin", 8, false, 0x3d00,0x20);
            m_CharacterROMInverted = Utilities.loadCharacterROM("ROMs\\SPECTRUM_ZX82.bin", 8, true, 0x3d00,0x20);

            // load ROMs
            ROMmodule rmSpectrum = new ROMmodule(m_cpu, "ROMs\\SPECTRUM_ZX82.bin", 0);
            ROMmodule rmZEXALL = new ROMmodule(m_cpu, "ROMs\\Z80_ZEXALL.bin", 0x8000);
            m_cpu.Reset();
            //m_cpu.PC = 0x8000;
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

            int ScanLine = 0;
            int iScanLineFlip = m_VideoInfo.Rows * m_VideoInfo.CharHeight;

            m_bRun = true;

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
            m_KeyboardQueue.Enqueue(e);
            m_cpu.signalInterrupt(InterruptSignal.IRQ);
        }

        VideoInfoStruct ISystemBase.VideoInfo
        {
            get { return m_VideoInfo; }
        }
        #endregion

        #region TERMINAL EVENT I/O
        /// <summary>
        /// http://www.electronics.dit.ie/staff/tscarff/6800/6850acia/6850.htm
        /// </summary>
        /// <returns></returns>
        private byte ControlIn()
        {
            return 3; // // TODO Simple Z80 - CONTROL REGISTER IN - signal READY
        }
        private void ControlOut(byte b)
        {
            byte test = b; // TODO Simple Z80 - CONTROL REGISTER OUT
        }

        private byte DataIn()
        {
            byte b = 0;

            if (m_KeyboardQueue.Count > 0)
            {
                b = (byte)m_KeyboardQueue.Dequeue().KeyChar;
            }

            return b;
        }

        private void DataOut(byte dsp)
        {
            m_DisplayQueue.Enqueue(dsp);
            m_bNeedRefresh = true;
        }
        #endregion

        #region TERMINAL OUTPUT
        private void outputDsp(byte dsp)
        {
            // clear old cursor
            m_funcRenderCharacter(m_iCursorX, m_iCursorY, m_CharacterROM[m_ScreenBuffer[m_iCursorY][m_iCursorX]]);

            // display new character
            switch (dsp)
            {
                case 0x0A:
                    m_iCursorY++;
                    break;
                case 0x0C:
                    for (int r = 0; r < m_VideoInfo.Rows; r++)
                        newLine();
                    break;
                case 0x0D:
                    m_iCursorX = 0;
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

    }
}
