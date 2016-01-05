using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Font
{
    class TextRenderRegion
    {
        private FontFace face;

        public float X, Y, W, H;
        public float PenX, PenY;

        public uint LastChar;

        private float currentAdvance;

        public TextRenderRegion(FontFace face, int x, int y, int w, int h)
        {
            this.face = face;

            this.X = x;
            this.Y = y;
            this.W = w;
            this.H = h;

            Reset();
        }

        public void Reset()
        {
            PenX = X;
            PenY = Y + face.Ascent;
            LastChar = 0;
            currentAdvance = 0;
        }

        public void Kerning(float advance)
        {
            PenX += advance;
        }

        public void CheckNewLine(float advance)
        {
            currentAdvance = advance;
            if (PenX + advance > X + W)
            {
                PenX = X;
                PenY += face.LineHeight;
            }
        }

        public void FinishChar()
        {
            PenX += currentAdvance;
            currentAdvance = 0;
        }
    }
}
