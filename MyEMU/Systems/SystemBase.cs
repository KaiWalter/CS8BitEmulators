using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using MyEMU.Display;

namespace MyEMU.Systems
{
    public delegate void RenderCharacter(int X,int Y,byte[] Character);
    public delegate void DrawScanLine(int iScanLine);

    public struct VideoInfoStruct
    {
        internal int Rows;
        internal int Cols;
        internal int CharHeight;
        internal int CharWidth;
        internal Color FontColor;
        internal Color BackColor;
    }

    public interface ISystemBase
    {
        string Title { get; }

        void setupBase();
        void setupVideo(RenderCharacter funcRenderCharacter,
                   DrawScanLine funcDrawScanLine);

        void run();
        void stop();

        void OnKeyDown(System.Windows.Forms.KeyEventArgs e);
        void OnKeyUp(System.Windows.Forms.KeyEventArgs e);
        void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e);

        VideoInfoStruct VideoInfo { get; }
    }
}
