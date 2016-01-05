using Sandbox.Font;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Gui
{
    class GameSceneGui
    {
        private TextRenderRegion rr;
        private StaticTextRenderer guitext;
        private NumberTextRenderer numtext;
        private int counter = 0;

        public GameSceneGui(GuiEnvironment env)
        {
            this.rr = new TextRenderRegion(FontFace.GuiFont, 0, 0, 200, 600);
            guitext = new StaticTextRenderer(env, FontFace.GuiFont, "Frame count: ");
            numtext = new NumberTextRenderer(env, FontFace.GuiFont);

        }

        public void Render()
        {
            rr.Reset();
            guitext.Render(rr);
            numtext.Render(rr, counter);
            if (counter == int.MaxValue)
            {
                counter = 0;
            }
            else
            {
                ++counter;
            }
        }
    }
}
