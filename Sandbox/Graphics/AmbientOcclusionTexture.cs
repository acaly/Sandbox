using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Graphics
{
    class AmbientOcclusionTexture
    {
        private const int cellSize = 8;

        public static Bitmap CreateBitmap()
        {
            var cellBorder = cellSize / 2;
            var cellRealSize = cellSize + cellBorder * 2;
            Bitmap bitmap = new Bitmap(cellRealSize * 4, cellRealSize * 4);

            //fill cells
            for (int y = 0; y < 4; ++y)
            {
                for (int x = 0; x < 4; ++x)
                {
                    int index = x + y * 4;

                    //fill one cell
                    int offset_x = x * cellRealSize, offset_y = y * cellRealSize;
                    for (int pix_x = -cellBorder; pix_x < cellSize + cellBorder; ++pix_x)
                    {
                        for (int pix_y = -cellBorder; pix_y < cellSize + cellBorder; ++pix_y)
                        {
                            int pix_xx = pix_x;
                            if (pix_xx < 0) pix_xx = 0; if (pix_xx >= cellSize) pix_xx = cellSize - 1;
                            int pix_yy = pix_y;
                            if (pix_yy < 0) pix_yy = 0; if (pix_yy >= cellSize) pix_yy = cellSize - 1;
                            float depth = Interpolate(index, (pix_xx + 0.5f) / cellSize, (pix_yy + 0.5f) / cellSize);
                            bitmap.SetPixel(pix_x + cellBorder + offset_x, pix_y + cellBorder + offset_y,
                                Color.FromArgb((int)(depth * 255), 0, 0));
                        }
                    }
                }
            }

            return bitmap;
        }
        
        private static float Interpolate(int index, float x, float y)
        {
            int pp = (index & 1) != 0 ? 1 : 0;
            int pn = (index & 2) != 0 ? 1 : 0;
            int np = (index & 4) != 0 ? 1 : 0;
            int nn = (index & 8) != 0 ? 1 : 0;
            return pp * x * y + pn * x * (1 - y) +
                np * (1 - x) * y + nn * (1 - x) * (1 - y);
        }

        public static Vector4 MakeAOOffset(bool pp, bool pn, bool np, bool nn)
        {
            int index = (pp ? 1 : 0) + (pn ? 2 : 0) + (np ? 4 : 0) + (nn ? 8 : 0);
            int x = index & 3;
            int y = index / 4;
            return new Vector4(x / 4.0f, y / 4.0f, 0, 0);
        }
    }
}
