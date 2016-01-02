using System;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;

namespace MyEMU.Systems
{
    class MCP : ISystemBase
    {
        private const int RETRACE_RATE = 60; // screen retraces per second (simulating V-BLANK)

        private byte[][] m_ScreenBuffer;
        private Queue<byte> m_DisplayQueue;

        private int m_CursorX;
        private int m_CursorY;
        private bool m_CursorVisible;

        protected byte[][] m_CharacterROM;
        protected byte[][] m_CharacterROMInverted;

        private VideoInfoStruct m_VideoInfo;
        private RenderCharacter m_funcRenderCharacter;
        private DrawScanLine m_funcDrawScanLine;

        // command processor
        private string m_CommandBuffer;
        private string m_NextCommand;
        private ISystemBase m_LaunchSystem;

        private bool m_bRun;

        string ISystemBase.Title
        {
            get { return "MCP"; }
        }
        void ISystemBase.setupBase()
        {

            // setup display
            m_VideoInfo = new VideoInfoStruct();
            m_VideoInfo.Rows = 25;
            m_VideoInfo.Cols = 80;
            m_VideoInfo.CharHeight = 16;
            m_VideoInfo.CharWidth = 8;
            m_VideoInfo.FontColor = Color.Green;
            m_VideoInfo.BackColor = Color.Black;

            m_CursorX = m_CursorY = 0;
            m_CursorVisible = true;

            // blank whole screen
            m_ScreenBuffer = new byte[m_VideoInfo.Rows][];
            for(int r = 0; r<m_VideoInfo.Rows; r++)
            {
                m_ScreenBuffer[r] = new byte[m_VideoInfo.Cols];
                for(int c=0; c<m_VideoInfo.Cols; c++)
                    m_ScreenBuffer[r][c] = 0x20;
            }

            m_DisplayQueue = new Queue<byte>();

            // load character set
            m_CharacterROM = Utilities.loadCharacterROM_2BANK("ROMs\\Herc.bin", 16);
            m_CharacterROMInverted = Utilities.loadCharacterROM_2BANK("ROMs\\Herc.bin", 16, true);
        
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

            int CursorCount = 0;

            startup();

            while (m_bRun)
            {
                // screen emulate one scan line
                m_funcDrawScanLine(ScanLine);
                ScanLine++;
                if (ScanLine >= iScanLineFlip) ScanLine = 0;

                if (DateTime.UtcNow >= dtNextRetrace)
                {
                    Application.DoEvents();

                    CursorCount++;
                    if (CursorCount > 10)
                    {
                        drawCursor();
                        CursorCount = 0;
                    }

                    dtNextRetrace = DateTime.UtcNow.AddMilliseconds(iRetraceIntervalMS);
                }

                if (m_DisplayQueue.Count > 0)
                    outputDisplay(m_DisplayQueue.Dequeue());

                checkCommand();

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
            m_DisplayQueue.Enqueue((byte)e.KeyChar);

            if (e.KeyChar == '\r')
            {
                m_NextCommand = m_CommandBuffer;
                m_CommandBuffer = "";
            }
            else
                m_CommandBuffer += e.KeyChar.ToString();
        }

        VideoInfoStruct ISystemBase.VideoInfo
        {
            get { return m_VideoInfo; }
        }

        void print(string s)
        {
            foreach (char ch in s)
                m_DisplayQueue.Enqueue((byte)ch);
        }

        private void newLine()
        {
            for (int r = 0; r < m_VideoInfo.Rows - 1; r++)
                m_ScreenBuffer[r] = m_ScreenBuffer[r + 1];

            m_ScreenBuffer[m_VideoInfo.Rows - 1] = new byte[m_VideoInfo.Cols];
            for (int c = 0; c < m_VideoInfo.Cols; c++) m_ScreenBuffer[m_VideoInfo.Rows - 1][c] = 0x20;

            for (int r = 0; r < m_VideoInfo.Rows; r++)
                for (int c = 0; c < m_VideoInfo.Cols; c++)
                    m_funcRenderCharacter(c, r, m_CharacterROM[m_ScreenBuffer[r][c]]);
        }

        private void outputDisplay(byte b)
        {
            m_CursorVisible = false;
            drawCursor();

            if (b == 0x0D)
            {
                m_CursorX = 0;
                m_CursorY++;
            }
            else
            {
                m_ScreenBuffer[m_CursorY][m_CursorX] = b;

                m_funcRenderCharacter(m_CursorX, m_CursorY, m_CharacterROM[b]);

                m_CursorX++;
            }

            // check cursor position
            if (m_CursorX == m_VideoInfo.Cols)
            {
                m_CursorX = 0;
                m_CursorY++;
            }
            if (m_CursorY == m_VideoInfo.Rows)
            {
                newLine();
                m_CursorY--;
            }

            m_CursorVisible = true;
            drawCursor();
        }

        private void drawCursor()
        {
            m_funcRenderCharacter(m_CursorX,m_CursorY,
                m_CursorVisible ? m_CharacterROMInverted[(int)m_ScreenBuffer[m_CursorY][m_CursorX]]
                                : m_CharacterROM[(int)m_ScreenBuffer[m_CursorY][m_CursorX]]
                );
            m_CursorVisible = !m_CursorVisible;
        }

        #region COMMAND PROCESSOR
        void startup()
        {
            print("\r");
            print("*** Master Control Program ***\r");
            prompt();

            m_CommandBuffer = "";
            m_NextCommand = null;

        }

        void prompt()
        {
            print("\r");
            print("READY.\r");
        }

        void checkCommand()
        {
            if (!String.IsNullOrEmpty(m_NextCommand))
            {
                processCommand(m_NextCommand);
                m_NextCommand = null;
            }
        }

        void processCommand(string cmd)
        {
            // evaluate command
            string[] CommandTokens = cmd.Split(' ');

            if(CommandTokens.Length > 0)
            {
                switch(CommandTokens[0].ToUpper())
                {
                    case "EXIT":
                    case "QUIT":
                        m_bRun = false;
                        break;

                    case "HELP":
                        print("UNABLE TO COMPLY\r");
                        break;

                    case "LAUNCH":
                        if (CommandTokens.Length < 2)
                        {
                            print("ERROR\rPLEASE ENTER SYSTEM CODE\r");
                        }
                        else
                        {
                            m_LaunchSystem = null;

                            switch (CommandTokens[1].ToUpper())
                            {
                                case "APPLE1":
                                    if (CommandTokens.Length > 2)
                                        m_LaunchSystem = new Apple1(CommandTokens[2].ToUpper());
                                    else
                                        m_LaunchSystem = new Apple1();
                                    break;

                                case "CBM2001N_B1":
                                    m_LaunchSystem = new CBM2001N_B1();
                                    break;

                                case "CBM2001N":
                                case "CBM2001N_B2":
                                    m_LaunchSystem = new CBM2001N_B2();
                                    break;

                                case "EHBASIC":
                                    m_LaunchSystem = new EHBASIC6502();
                                    break;

                                case "TRS80M1":
                                    m_LaunchSystem = new TRS80M1();
                                    break;

                                case "SIMPLEZ80":
                                    m_LaunchSystem = new SimpleZ80();
                                    break;

                                case "ZX80":
                                    m_LaunchSystem = new ZX80();
                                    break;

                                default:
                                    print("ERROR\rSYSTEM CODE UNKNOWN\r");
                                    break;
                            }

                            if(m_LaunchSystem != null)
                            {
                                Thread tSystem = new Thread(new ThreadStart(launchSystem));
                                tSystem.Start();
                            }

                        }

                        break;

                    default:
                        print("ERROR!\r");
                        break;
                }
            }

            prompt();
        }

        void launchSystem()
        {
            EmulatorScreen screen = new EmulatorScreen(m_LaunchSystem);
        }
        #endregion

    }
}
