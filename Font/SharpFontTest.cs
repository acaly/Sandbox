using SharpFont;
using System;
using System.Collections.Generic;
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
                using (var face = new Face(lib, @"E:\1.ttf"))
                {
                    face.SetCharSize(0, 62, 0, 96);
                    //var index = face.GetCharIndex('A');
                    //face.LoadGlyph(index, LoadFlags.Default, LoadTarget.Normal);
                    face.SelectCharmap(SharpFont.Encoding.Unicode);
                    face.LoadChar('A', LoadFlags.Default, LoadTarget.Normal);
                    //TODO use index, check if it's 0
                    face.Glyph.RenderGlyph(RenderMode.Normal);
                    //face.LoadChar('B', LoadFlags.Default, LoadTarget.Normal);
                    //face.LoadChar('A', LoadFlags.Default, LoadTarget.Normal);
                    var count = face.GlyphCount;
                    var bitmap = face.Glyph.Bitmap;
                    var type = bitmap.PixelMode;
                    using (var saveBitmap = bitmap.ToGdipBitmap())
                    {
                        saveBitmap.Save(@"E:\1.ttf.A." + type.ToString() + ".bmp");
                    }
                }
            }
        }
    }
}
