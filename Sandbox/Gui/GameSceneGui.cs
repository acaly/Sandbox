using Sandbox.Font;
using Sandbox.GameScene;
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
        private StaticTextRenderer guitext1, guitext2, guitext3;
        private NumberTextRenderer numtext;
        private int counter = 0;

        private Camera camera;

        public GameSceneGui(GuiEnvironment env, Camera camera)
        {
            this.rr = new TextRenderRegion(FontFace.GuiFont, 5, 3, 200, 600);
            guitext1 = new StaticTextRenderer(env, FontFace.GuiFont, "Frame count: ");
            guitext2 = new StaticTextRenderer(env, FontFace.GuiFont, "Position: ");
            guitext3 = new StaticTextRenderer(env, FontFace.GuiFont, ", ");
            numtext = new NumberTextRenderer(env, FontFace.GuiFont);

            this.camera = camera;
        }

        public void Render()
        {
            rr.Reset();

            guitext1.Render(rr);
            numtext.Render(rr, counter);
            if (counter == int.MaxValue)
            {
                counter = 0;
            }
            else
            {
                ++counter;
            }

            rr.NewLine();

            guitext2.Render(rr);
            numtext.Render(rr, (int)camera.Position.X);
            guitext3.Render(rr);
            numtext.Render(rr, (int)camera.Position.Y);
            guitext3.Render(rr);
            numtext.Render(rr, (int)camera.Position.Z);
        }
    }
}
