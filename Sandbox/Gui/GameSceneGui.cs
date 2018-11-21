using LightDx;
using Sandbox.GameScene;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Gui
{
    class GameSceneGui
    {
        private Camera camera;
        private TextureFontCache font;
        private Sprite sprite;
        private int frameCounter = 0;

        public GameSceneGui(LightDevice device, Camera camera)
        {
            this.sprite = new Sprite(device);
            this.font = new TextureFontCache(device, SystemFonts.DefaultFont);
            this.camera = camera;
        }

        public void Render()
        {
            sprite.Apply();

            sprite.DrawString(font, $"Frame count: {frameCounter++}", 5, 3, 200);
            sprite.DrawString(font, $"Position: " +
                $"{camera.Position.X.ToString("0.0")}, " +
                $"{camera.Position.Y.ToString("0.0")}, " +
                $"{camera.Position.Z.ToString("0.0")}", 5, 3 + font.Font.Height, 200);
        }
    }
}
