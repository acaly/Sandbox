using Sandbox.Graphics;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Gui
{
    class GuiTexture
    {
        public ShaderResourceView ResourceView
        {
            get;
            private set;
        }

        public Texture2D Texture
        {
            get;
            private set;
        }

        public int Width
        {
            get
            {
                return Texture.Description.Width;
            }
        }

        public int Height
        {
            get
            {
                return Texture.Description.Height;
            }
        }

        public static GuiTexture FromFile(RenderManager rm, string filename)
        {
            var res = Resource.FromFile(rm.Device, filename);
            if (!(res is Texture2D))
            {
                res.Dispose();
                return null;
            }
            GuiTexture ret = new GuiTexture();
            ret.Texture = (Texture2D)res;
            ret.ResourceView = new ShaderResourceView(rm.Device, res);
            return ret;
        }
        
        public static GuiTexture CreateDynamic(RenderManager rm, int width, int height)
        {
            GuiTexture ret = new GuiTexture();
            var res = new Texture2D(rm.Device, new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                Format = SharpDX.DXGI.Format.A8_UNorm,//R8G8B8A8_UNorm,
                Height = height,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Dynamic,
                Width = width,
            });
            ret.Texture = res;
            ret.ResourceView = new ShaderResourceView(rm.Device, res);
            return ret;
        }
    }
}
