using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemColor = System.Drawing.Color;

namespace Sandbox.Graphics
{
    class AmbientOcculsionTexture : IDisposable
    {
        private Resource texture;
        private ShaderResourceView view;
        private const int cellSize = 8;

        public ShaderResourceView ResourceView
        {
            get
            {
                return view;
            }
        }

        public AmbientOcculsionTexture(RenderManager rm)
        {
            var cellBorder = cellSize / 2;
            var cellRealSize = cellSize + cellBorder * 2;
            using (Bitmap bitmap = new Bitmap(cellRealSize * 4, cellRealSize * 4))
            {
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
                                float depth = functions[index]((pix_xx + 0.5f) / cellSize, (pix_yy + 0.5f) / cellSize);
                                bitmap.SetPixel(pix_x + cellBorder + offset_x, pix_y + cellBorder + offset_y, 
                                    SystemColor.FromArgb((int)(depth * 255), 0, 0));
                            }
                        }
                    }
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    texture = Resource.FromMemory(rm.Device, ms.ToArray());
                }
            }
            view = new ShaderResourceView(rm.Device, texture);
        }

        private static float ValueMap(float d)
        {
            return d;
        }

        //index bits:
        //8 2
        //4 1
        private static Func<float, float, float>[] functions = new Func<float, float, float>[]
        {
            //0 0 
            //0 0
            delegate(float a, float b) {
                return 0.0f;
            },
            //0 0
            //0 1
            delegate(float a, float b) {
                return a + b <= 1 ? 0 : (a + b) - 1;
            },
            //0 2
            //0 0
            delegate(float a, float b) {
                return a + (1 - b) <= 1.0f ? 0 : (a + (1 - b)) - 1;
            },
            //0 2
            //0 1
            delegate(float a, float b) {
                return a;
            },
            //0 0
            //4 0
            delegate(float a, float b) {
                return (1 - a) + b <= 1 ? 0 : ((1 - a) + b) - 1;
            },
            //0 0
            //4 1
            delegate(float a, float b) {
                return b;
            },
            //0 2
            //4 0
            delegate(float a, float b) {
                return a + b < 1 ? (a + b) : 2 - (a + b);
            },
            //0 2
            //4 1
            delegate(float a, float b) {
                return Math.Max(a, b);
            },
            //8 0
            //0 0
            delegate(float a, float b) {
                return (1 - a) + (1 - b) <= 1 ? 0 : ((1 - a) + (1 - b)) - 1;
            },
            //8 0
            //0 1
            delegate(float a, float b) {
                return (1 - a) + b < 1 ? ((1 - a) + b) : 2 - ((1 - a) + b);
            },
            //8 2
            //0 0
            delegate(float a, float b) {
                return (1 - b);
            },
            //8 2
            //0 1
            delegate(float a, float b) {
                return Math.Max(a, 1 - b);
            },
            //8 0
            //4 0
            delegate(float a, float b) {
                return (1 - a);
            },
            //8 0
            //4 1
            delegate(float a, float b) {
                return Math.Max(1 - a, b);
            },
            //8 2
            //4 0
            delegate(float a, float b) {
                return Math.Max(1 - a, 1 - b);
            },
            //8 2
            //4 1
            delegate(float a, float b) {
                return 1.0f;
            },
        };

        public void Dispose()
        {
            Utilities.Dispose(ref texture);
            Utilities.Dispose(ref view);
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
