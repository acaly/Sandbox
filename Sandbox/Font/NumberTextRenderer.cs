using Sandbox.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Font
{
    class NumberTextRenderer
    {
        private GuiEnvironment env;
        private FontFace face;
        private FontCharInfo[] info;
        private static readonly char[] numberChars = new char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                /*10*/'+',
                /*11*/'-',
                /*12*/'.',
                /*13*/'#',
            };
        public NumberTextRenderer(GuiEnvironment env, FontFace face)
        {
            var info = new FontCharInfo[numberChars.Length];
            for (int i = 0; i < numberChars.Length; ++i)
            {
                info[i] = face.LoadChar(numberChars[i]);
            }

            this.env = env;
            this.face = face;
            this.info = info;

            this.buffer = new int[64];
        }

        public void Render(TextRenderRegion region, int number)
        {
            int count = FormatNumber(number);
            for (int i = 0; i < count; ++i)
            {
                var c = info[buffer[i]];
                region.Kerning(face.GetKerning(region.LastChar, c.Index));
                var advance = c.AdvanceX;
                region.CheckNewLine(advance);
                face.Render(env, SharpDX.Color.ForestGreen, c.Index, region.PenX, region.PenY);
                region.FinishChar();
                region.LastChar = c.Index;
            }
        }

        private int[] buffer;

        private int FormatNumber(int value)
        {
            //first fill buffer in reverse order
            bool appendMinus = false;
            if (value < 0)
            {
                value = -value;
                appendMinus = true;
            }
            int index = 0;
            while (value != 0)
            {
                buffer[index++] = value % 10;
                value /= 10;
            }
            if (index == 0)
            {
                buffer[index++] = 0;
            }
            if (appendMinus)
            {
                buffer[index++] = 11;
            }
            int ret = index;
            int exchange = 0;
            while (exchange < --index)
            {
                var tmp = buffer[exchange];
                buffer[exchange] = buffer[index];
                buffer[index] = tmp;
                exchange++;
            }
            return ret;
        }
    }
}
