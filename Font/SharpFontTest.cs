using SharpFont;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Font
{
    class SharpFontTest
    {
        private static void Main()
        {
            using (Library lib = new Library())
            {
                using (var face = new Face(lib, @"1.ttf"))
                {
                    face.SetCharSize(0, 12, 0, 96);
                    //var index = face.GetCharIndex('A');
                    //face.LoadGlyph(index, LoadFlags.Default, LoadTarget.Normal);
                    face.SelectCharmap(SharpFont.Encoding.Unicode);
                    RenderString(lib, face, "AVA").Save(@"E:\example.bmp");
                    //face.LoadChar('A', LoadFlags.Default, LoadTarget.Normal);
                    ////TODO use index, check if it's 0
                    //face.Glyph.RenderGlyph(RenderMode.Normal);
                    ////face.LoadChar('B', LoadFlags.Default, LoadTarget.Normal);
                    ////face.LoadChar('A', LoadFlags.Default, LoadTarget.Normal);
                    //var count = face.GlyphCount;
                    //var bitmap = face.Glyph.Bitmap;
                    //var type = bitmap.PixelMode;
                    //using (var saveBitmap = bitmap.ToGdipBitmap())
                    //{
                    //    saveBitmap.Save(@"E:\1.ttf.A." + type.ToString() + ".bmp");
                    //}
                }
            }
        }
        public static Bitmap RenderString(Library library, Face face, string text)
        {
            float penX = 0, penY = 0;
            float width = 0;
            float height = 0;

            //both bottom and top are positive for simplicity
            float top = 0, bottom = 0;

            //measure the size of the string before rendering it, requirement of Bitmap.
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                uint glyphIndex = face.GetCharIndex(c);
                face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);

                width += (float)face.Glyph.Advance.X;

                if (face.HasKerning && i < text.Length - 1)
                {
                    char cNext = text[i + 1];
                    width += (float)face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Default).X;
                }

                float glyphTop = (float)face.Glyph.Metrics.HorizontalBearingY;
                float glyphBottom = (float)(face.Glyph.Metrics.Height - face.Glyph.Metrics.HorizontalBearingY);

                if (glyphTop > top)
                    top = glyphTop;
                if (glyphBottom > bottom)
                    bottom = glyphBottom;
            }

            height = top + bottom;

            //create a new bitmap that fits the string.
            Bitmap bmp = new Bitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height));
            var g = System.Drawing.Graphics.FromImage(bmp);
            g.Clear(SystemColors.Control);

            //draw the string
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                uint glyphIndex = face.GetCharIndex(c);
                face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);
                face.Glyph.RenderGlyph(RenderMode.Normal);

                if (c == ' ')
                {
                    penX += (float)face.Glyph.Advance.X;

                    if (face.HasKerning && i < text.Length - 1)
                    {
                        char cNext = text[i + 1];
                        width += (float)face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Default).X;
                    }

                    penY += (float)face.Glyph.Advance.Y;
                }
                else
                {
                    //FTBitmap ftbmp = face.Glyph.Bitmap.Copy(library);
                    FTBitmap ftbmp = face.Glyph.Bitmap;
                    Bitmap cBmp = ftbmp.ToGdipBitmap(Color.Black);

                    //Not using g.DrawImage because some characters come out blurry/clipped.
                    g.DrawImageUnscaled(cBmp, (int)Math.Round(penX + face.Glyph.BitmapLeft), (int)Math.Round(penY + (top - (float)face.Glyph.Metrics.HorizontalBearingY)));

                    penX += (float)face.Glyph.Metrics.HorizontalAdvance;
                    penY += (float)face.Glyph.Advance.Y;

                    if (face.HasKerning && i < text.Length - 1)
                    {
                        char cNext = text[i + 1];

                        var kern = face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Unscaled);
                        var ik = kern.X.Value;
                        var iks = ik >> 6;
                        var scale = face.Size.Metrics.ScaleX.ToSingle();
                        penX += (scale * (float)kern.X);
                    }
                }
            }

            g.Dispose();
            return bmp;
        }
    }
}
