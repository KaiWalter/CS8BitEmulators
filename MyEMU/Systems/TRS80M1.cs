using System;
using System.Collections.Generic;
using System.Linq;
using MyEMU.CPU;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;

namespace MyEMU.Systems
{
    /// <summary>
    /// http://www.classiccmp.org/cpmarchives/trs80/mirrors/kjsl/www.kjsl.com/trs80/mod1intern.html
    /// </summary>
    class TRS80M1 : ISystemBase
    {
        Z80 m_cpu;

        private VideoInfoStruct m_VideoInfo;
        private RenderCharacter m_funcRenderCharacter;
        private DrawScanLine m_funcDrawScanLine;

        private bool m_bRun;

        #region ISystemBase
        string ISystemBase.Title
        {
            get { return "TRS80M1"; }
        }

        /// <summary>
        /// http://www.classiccmp.org/cpmarchives/trs80/mirrors/kjsl/www.kjsl.com/trs80/mod1intern.html
        /// </summary>
        void ISystemBase.setupBase()
        {
            m_cpu = new Z80();
            m_cpu.installRAMBank(0x4000,16); 

            // setup display
            m_VideoInfo = new VideoInfoStruct();
            m_VideoInfo.Rows = 25;
            m_VideoInfo.Cols = 80;
            m_VideoInfo.CharHeight = 8;
            m_VideoInfo.CharWidth = 8;
            m_VideoInfo.FontColor = Color.Green;
            m_VideoInfo.BackColor = Color.Black;

            ROMmodule rmBIOS = new ROMmodule(m_cpu, "ROMs\\TRS80_level1.rom", 0);

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
            m_bRun = true;

            while(m_bRun)
            {
                m_cpu.emulateCycle();
            }
        }

        void ISystemBase.stop()
        {
            m_bRun = false;
        }

        void ISystemBase.OnKeyDown(System.Windows.Forms.KeyEventArgs e) { }
        void ISystemBase.OnKeyUp(System.Windows.Forms.KeyEventArgs e) { }
        void ISystemBase.OnKeyPress(System.Windows.Forms.KeyPressEventArgs e) { }

        VideoInfoStruct ISystemBase.VideoInfo
        {
            get { return m_VideoInfo; }
        }
        #endregion

    }
}
