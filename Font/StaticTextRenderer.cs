using Sandbox.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Font
{
    //TODO should cache kerning (11%)
    //TODO should cache texture (50%)
    class StaticTextRenderer
    {
        private GuiEnvironment env;
        private FontFace face;
        private FontCharInfo[] info;

        public StaticTextRenderer(GuiEnvironment env, FontFace face, string str)
        {
            var chars = str.ToCharArray();
            var info = new FontCharInfo[chars.Length];
            for (int i = 0; i < chars.Length; ++i)
            {
                info[i] = face.LoadChar(chars[i]);
            }

            this.env = env;
            this.face = face;
            this.info = info;
        }

        public void Render(TextRenderRegion region)
        {
            foreach (var c in info)
            {
                region.Kerning(face.GetKerning(region.LastChar, c.Index));
                var advance = c.AdvanceX;
                region.CheckNewLine(advance);
                face.Render(env, SharpDX.Color.ForestGreen, c.Index, region.PenX, region.PenY);
                region.FinishChar();
                region.LastChar = c.Index;
            }
        }
    }
}
