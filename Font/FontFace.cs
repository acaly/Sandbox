using Sandbox.Graphics;
using Sandbox.Gui;
using SharpDX;
using SharpDX.Direct3D11;
using SharpFont;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Font
{
    class FontFace
    {
        private static Library library;

        public static FontFace GuiFont
        {
            get;
            private set;
        }

        static FontFace()
        {
            library = new Library();
        }

        public static void CreateFonts(RenderManager rm)
        {
            GuiFont = new FontFace(rm, "1.ttf", 12);
        }

        private int textureSize;

        private Face face;
        private RenderManager rm;
        private List<GuiTexture> textureList = new List<GuiTexture>();
        private GuiTexture currentTexture;

        private byte[] data;
        private int currentDataY, currentDataX, currentLineHeight;

        public FontFace(RenderManager rm, string filename, int size)
        {
            this.rm = rm;
            this.face = new Face(library, filename);

            this.textureSize = Math.Min(256, Texture2D.MaximumTexture2DSize);
            this.data = new byte[this.textureSize * this.textureSize];

            this.face.SetCharSize(0, size, 0, 96);
            this.face.SelectCharmap(SharpFont.Encoding.Unicode);
        }

        private void UpdateCurrentTexture()
        {
            DataStream stream;
            if (this.currentTexture == null)
            {
                this.currentTexture = GuiTexture.CreateDynamic(rm, this.textureSize, this.textureSize);
            }
            rm.Device.ImmediateContext.MapSubresource(currentTexture.Texture, 0, SharpDX.Direct3D11.MapMode.WriteDiscard,
                SharpDX.Direct3D11.MapFlags.None, out stream);
            stream.Write(data, 0, data.Length);
            rm.Device.ImmediateContext.UnmapSubresource(currentTexture.Texture, 0);
        }

        private GuiTexture GetTexture(int index)
        {
            if (index == textureList.Count)
            {
                return currentTexture;
            }
            return textureList[index];
        }

        private Dictionary<char, FontCharInfo> loadedChar = new Dictionary<char, FontCharInfo>();

        private struct CharRenderInfo
        {
            public int TextureIndex;
            public float w, h;
            public float u, v, usize, vsize;
            public float bitmapLeft, bearingY;
        }

        private Dictionary<uint, CharRenderInfo> renderInfo = new Dictionary<uint, CharRenderInfo>();

        public FontCharInfo LoadChar(char c)
        {
            if (loadedChar.ContainsKey(c)) return loadedChar[c];
            return AddChar(c);
        }

        private FontCharInfo AddChar(char c)
        {
            AppendMode();

            uint charIndex = face.GetCharIndex(c);

            face.LoadGlyph(charIndex, LoadFlags.Default, LoadTarget.Normal);

            face.Glyph.RenderGlyph(SharpFont.RenderMode.Normal);
            var bitmap = face.Glyph.Bitmap;

            if (currentDataX + bitmap.Width + 2 >= this.textureSize)
            {
                //new line
                currentDataX = 0;
                currentDataY += currentLineHeight;
                currentLineHeight = 0;
            }
            currentLineHeight = Math.Max(currentLineHeight, bitmap.Rows);
            if (currentDataY + currentLineHeight + 2 > this.textureSize)
            {
                //new texture
                UpdateCurrentTexture();
                textureList.Add(currentTexture);
                currentTexture = null;

                currentDataX = currentDataY = 0;
            }

            int x = currentDataX, y = currentDataY;
            currentDataX += bitmap.Width;

            for (int j = 0; j < bitmap.Rows; j++)
            {
                Marshal.Copy(bitmap.Buffer + j * bitmap.Pitch, data, (j + y) * this.textureSize + x, bitmap.Width);
            }
            CharRenderInfo rinfo = new CharRenderInfo()
            {
                TextureIndex = textureList.Count,
                w = bitmap.Width,
                h = bitmap.Rows,
                u = x / (float)this.textureSize,
                v = y / (float)this.textureSize,
                usize = bitmap.Width / (float)this.textureSize,
                vsize = bitmap.Rows / (float)this.textureSize,
                bitmapLeft = -face.Glyph.BitmapLeft,//(float)SharpFontFace.Glyph.Metrics.HorizontalBearingX,
                bearingY = (float)face.Glyph.Metrics.HorizontalBearingY,
            };
            renderInfo.Add(charIndex, rinfo);

            FontCharInfo ret = new FontCharInfo()
            {
                Index = charIndex,
                AdvanceX = (float)face.Glyph.Advance.X,//.Metrics.HorizontalAdvance,//Advance.X
            };
            loadedChar.Add(c, ret);
            return ret;
        }

        public void Render(GuiEnvironment env, Color color, uint charIndex, float penx, float peny)
        {
            RenderMode();
            if (!renderInfo.ContainsKey(charIndex))
            {
                charIndex = 0;
            }
            var info = renderInfo[charIndex];
            env.DrawTexture(GetTexture(info.TextureIndex), new Vector4(color.ToVector3(), 0),
                (float)(penx - info.bitmapLeft), (float)Math.Round(peny - info.bearingY),
                info.w, info.h, info.u, info.v, info.usize, info.vsize);
        }

        private bool isInRenderMode;

        private void AppendMode()
        {
            if (isInRenderMode)
            {
                isInRenderMode = false;
            }
        }

        private void RenderMode()
        {
            if (!isInRenderMode)
            {
                UpdateCurrentTexture();
                isInRenderMode = true;
            }
        }

        public float Ascent
        {
            get
            {
                return face.Ascender / 64.0f * face.Size.Metrics.ScaleX.ToSingle(); //(ascender >> 6) * scaleX
            }
        }

        public float LineHeight
        {
            get
            {
                return (face.Ascender - face.Descender) / 64.0f * face.Size.Metrics.ScaleX.ToSingle();
            }
        }
        public float GetKerning(uint left, uint right)
        {
            var k = face.FaceFlags;
            var kk = face.HasKerning;
            if (kk && left != 0 && right != 0)
            {
                //due to the bug from SharpFont, we have to calculate the scaled kerning ourselves.
                //use SharpFont 3.1 should fix this.
                var kern = face.GetKerning(left, right, KerningMode.Unscaled);
                var ik = kern.X.Value;
                var iks = ik >> 6;
                var scale = face.Size.Metrics.ScaleX.ToSingle();
                return (scale * (float)kern.X);
            }
            return 0;
        }
    }
}
