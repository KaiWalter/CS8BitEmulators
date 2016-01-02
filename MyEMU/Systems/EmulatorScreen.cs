using MyEMU.Display;
using System.Drawing;
using System.Windows.Forms;

namespace MyEMU.Systems
{
    
    public partial class EmulatorScreen : Form
    {
        Canvas m_Canvas;
        ISystemBase m_System;

        public EmulatorScreen(ISystemBase system)
        {
            InitializeComponent();

            m_System = system;
            Text = m_System.Title;
            m_System.setupBase();
            VideoInfoStruct vi = m_System.VideoInfo;

            int iPixelSize = 2;
            if (vi.CharHeight > 8) iPixelSize = 1;

            int iBorder = 2;

            // setup system screen canvas
            m_Canvas = new Canvas(vi.Rows,vi.Cols,
                                    iBorder, iPixelSize,
                                    vi.CharHeight, vi.CharWidth, 
                                    vi.FontColor,vi.BackColor);
            Controls.Add(m_Canvas);

            // setup form with borders
            ClientSize = new Size(m_Canvas.Width + iBorder * 2, m_Canvas.Height + iBorder * 2);
            BackColor = vi.BackColor;

            m_System.setupVideo(m_Canvas.renderCharacter,
                         m_Canvas.drawScanLine);

            Utilities.SetForegroundWindow(this.Handle.ToInt32());
            Show();

            m_System.run();
        }

        #region EVENT HANDLING
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            m_System.stop();
            base.OnFormClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            m_System.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            m_System.OnKeyUp(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            m_System.OnKeyPress(e);
        }
        #endregion
    }
}
