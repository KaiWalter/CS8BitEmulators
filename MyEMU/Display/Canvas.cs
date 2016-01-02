using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace MyEMU.Display
{
    public class Canvas : Panel
    {
        protected int m_iRows;
        protected int m_iCols;
        protected int m_iPixelSize;
        protected int m_iScanLineWidth;
        protected int m_iCharHeight;
        protected int m_iCharWidth;

        protected Color m_FontColor;
        protected Color m_BackColor;
        protected ColorPalette m_Palette;

        protected byte[][][] m_DrawBuffer;
        protected Bitmap[] m_PixelLineCache;
        protected bool[] m_PixelLineNeedRedraw;
        protected bool[] m_PixelLineIsEmpty;

        public Canvas(int iRows, int iCols, 
                      int iBorder, int iPixelSize, 
                      int iCharHeight, int iCharWidth,
                      Color FontColor, Color BackColor)
            : base()
        {
            // setup dimensions
            m_iRows = iRows;
            m_iCols = iCols;
            m_iPixelSize = iPixelSize;
            m_iScanLineWidth = m_iCols * iCharWidth * m_iPixelSize;

            m_iCharHeight = iCharHeight;
            m_iCharWidth = iCharWidth;

            Width = iCols * iPixelSize * iCharWidth;
            Height = iRows * iPixelSize * iCharHeight;
            Top = iBorder;
            Left = iBorder;

            // setup colors
            m_BackColor = BackColor;
            m_FontColor = FontColor;
            m_Palette = new Bitmap(1, 1, PixelFormat.Format1bppIndexed).Palette;
            m_Palette.Entries[0] = m_BackColor;
            m_Palette.Entries[1] = m_FontColor;

            // setup drawing buffers
            m_DrawBuffer = new byte[iRows][][]; // init rows
            for (int r = 0; r < iRows; r++)
            {
                m_DrawBuffer[r] = new byte[iCharHeight][]; // init (bit) lines per row
                for (int l = 0; l < iCharHeight; l++)
                    m_DrawBuffer[r][l] = new byte[iCols]; // init cols per line (8 bits per col)
            }
            m_PixelLineCache = new Bitmap[iRows * iCharHeight];
            m_PixelLineNeedRedraw = new bool[iRows * iCharHeight];
            m_PixelLineIsEmpty = new bool[iRows * iCharHeight];
            for (int pl = 0; pl < iRows * iCharHeight; pl++)
                m_PixelLineNeedRedraw[pl] = m_PixelLineIsEmpty[pl] = true;

            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);

            Paint += new System.Windows.Forms.PaintEventHandler(CanvasPaint_1bpp);
        }

        public void renderCharacter(int iX, int iY, byte[] b)
        {
            for (int iLine = 0; iLine < m_iCharHeight; iLine++)
            {
                m_DrawBuffer[iY][iLine][iX] = b[iLine];
                m_PixelLineCache[iY * m_iCharHeight + iLine] = null;
                m_PixelLineNeedRedraw[iY * m_iCharHeight + iLine] = true;
            }
        }

        public void drawScanLine(int iScanLine)
        {
            if (m_PixelLineNeedRedraw[iScanLine])
                Invalidate(new Rectangle(0, iScanLine * m_iPixelSize, m_iScanLineWidth, m_iPixelSize));
        }

        protected void CanvasPaint_1bpp(object sender, PaintEventArgs e)
        {

            Graphics dc = e.Graphics;

            dc.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            int iPixelLineFrom = e.ClipRectangle.Top / m_iPixelSize;
            int iPixelLineTo = e.ClipRectangle.Bottom / m_iPixelSize;

            for (int iPixelLine = iPixelLineFrom; iPixelLine < iPixelLineTo; iPixelLine++)
            {
                int iY = iPixelLine / m_iCharHeight;
                int iLine = iPixelLine % m_iCharHeight;

                if (m_PixelLineNeedRedraw[iPixelLine])
                {
                    m_PixelLineNeedRedraw[iPixelLine] = false;

                    int iBitSum = 0;
                    for (int i = 0; i < m_iCols; i++) iBitSum += m_DrawBuffer[iY][iLine][i];
                    m_PixelLineIsEmpty[iPixelLine] = iBitSum == 0;

                    if (!m_PixelLineIsEmpty[iPixelLine])
                    {
                        Bitmap bm = new Bitmap(m_iCols * m_iCharWidth, 1, PixelFormat.Format1bppIndexed);
                        bm.Palette = m_Palette;

                        BitmapData bmData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height),
                                                ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);

                        Marshal.Copy(m_DrawBuffer[iY][iLine], 0, bmData.Scan0, m_iCols);

                        bm.UnlockBits(bmData);

                        m_PixelLineCache[iPixelLine] = new Bitmap(bm, new Size(m_iCols * m_iCharWidth * m_iPixelSize, m_iPixelSize));
                    }
                }

                if (!m_PixelLineIsEmpty[iPixelLine])
                    dc.DrawImageUnscaled(m_PixelLineCache[iPixelLine], 0, iPixelLine * m_iPixelSize);
            }
        }

    }
}