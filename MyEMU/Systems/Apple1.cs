using System;
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
    class Apple1 : ISystemBase
    {

        private const int RETRACE_RATE = 60; // screen retraces per second (simulating V-BLANK)

        protected MOS6502 m_cpu;
        private MC6820 m_pia; 

        protected byte[][] m_CharacterROM;
        protected byte[][] m_CharacterROMInverted;

        private bool m_bNeedRefresh;
        private Queue<byte> m_DisplayQueue;
        private char[][] m_ScreenBuffer;

        private Dictionary<byte, AppleKeyMap> m_keymap;

        private int m_iCursorX;
        private int m_iCursorY;

        private VideoInfoStruct m_VideoInfo;
        private RenderCharacter m_funcRenderCharacter;
        private DrawScanLine m_funcDrawScanLine;

        private bool m_bLoadBASIC;
        private bool m_bLoadHELLO;
        private bool m_bRun;

        public Apple1()
        {
            m_bLoadBASIC = false;
            m_bLoadHELLO = false;
        }

        public Apple1(string sAdditionalROM)
        {
            m_bLoadBASIC = String.Compare(sAdditionalROM, "BASIC", true) == 0;
            m_bLoadHELLO = String.Compare(sAdditionalROM, "HELLO", true) == 0;
        }

        #region ISystemBase

        string ISystemBase.Title
        {
            get { return "Apple1"; }
        }
        void ISystemBase.setupBase()
        {
            // inititalize system components
            m_cpu = new MOS6502();

            if(m_bLoadHELLO)
                m_cpu.installRAMBank(60); // memory needs to go up to EFFF
            else
                m_cpu.installRAMBank(8);

            m_pia = new MC6820("PIA", m_cpu, 0xD010); 

            // note: Apple 1 has no interrupt lines connected
            m_pia.OutputB = receiveDsp;

            // setup display
            m_VideoInfo = new VideoInfoStruct();
            m_VideoInfo.Rows = 24;
            m_VideoInfo.Cols = 40;
            m_VideoInfo.CharHeight = 8;
            m_VideoInfo.CharWidth = 8;
            m_VideoInfo.FontColor = Color.Green;
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

            // setup keyboard
            m_keymap = Apple1KeyMapFactory.Build("DE");

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

        void ISystemBase.OnKeyDown(System.Windows.Forms.KeyEventArgs e)
        {
            m_pia.CA1 = Signal.Fall; // bring keyboard strobe to low to force active transition

            byte key = (byte)e.KeyValue;

            if (key >= 0x61 && key <= 0x7A)
                key &= 0x5F; // make lower case key upper for mapping

            AppleKeyMap keymap;

            if (m_keymap.TryGetValue(key, out keymap))
            {
                if (e.Shift)
                    key = keymap.key_out_shift;
                else if (e.Control)
                    key = keymap.key_out_ctrl;
                else
                    key = keymap.key_out;
            }

            if (key > 0 && key < 0x60)
            {
                m_pia.InputA((byte)(key | 0x80)); // bit 7 is constantly set (+5V)
                m_pia.CA1 = Signal.Rise; // send only pulse
                m_pia.CA1 = Signal.Fall; // 20 micro secs are not worth emulating
            }
        }

        void ISystemBase.OnKeyUp(System.Windows.Forms.KeyEventArgs e) { }
        void ISystemBase.OnKeyPress(System.Windows.Forms.KeyPressEventArgs e) { }

        VideoInfoStruct ISystemBase.VideoInfo
        {
            get { return m_VideoInfo; }
        }
        #endregion

        #region Display
        private void receiveDsp(byte dsp)
        {
            m_DisplayQueue.Enqueue(dsp);
            m_bNeedRefresh = true;
        }

        public void outputDsp(byte dsp)
        {
            if (dsp >= 0x61 && dsp <= 0x7A)
                dsp &= 0x5F; // make lower case key upper

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
                    if (dsp >= 0x20 && dsp <= 0x5F)
                    {
                        m_ScreenBuffer[m_iCursorY][m_iCursorX] = (char)dsp;

                        m_funcRenderCharacter(m_iCursorX, m_iCursorY, m_CharacterROM[dsp]);

                        m_iCursorX++;
                    }
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
            for (int c = 0; c < m_VideoInfo.Cols; c++) m_ScreenBuffer[m_VideoInfo.Rows-1][c] = ' ';

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

            ROMmodule rmHexMon = new ROMmodule(m_cpu,"ROMs\\Apple1_HexMonitor.rom", 0xFF00);
            ROMmodule rmBASIC;

            if (m_bLoadBASIC) rmBASIC = new ROMmodule(m_cpu, "ROMs\\Apple1_basic.rom", 0xE000);
            if (m_bLoadHELLO) m_cpu.loadCC65BIN("ROMs\\Apple1_hello.bin");

        }
        #endregion

    }
}
