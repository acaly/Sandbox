using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Lighting
{
    interface IRectangleData<T, TRef>
    {
        Geometry.Rectangle Rect { get; set; }
        T Tag { get; set; }
        TRef Reference { get; }
    }

    interface IRectProvider<T, TRef>
    {
        void GetRectBeside(IRectangleData<T, TRef> rect, int side, RectProcessor<T, TRef> output);
        IRectangleData<T, TRef> GetRectFromRef(TRef r);
    }

    interface RectProcessor<T, TRef>
    {
        void Process(IRectangleData<T, TRef> rect);
    }

    class RectProvider<T> : IRectProvider<T, int>
    {
        private class RectDataImpl : IRectangleData<T, int>
        {
            public RectDataImpl(int index)
            {
                Reference = index;
            }
            public Geometry.Rectangle Rect { get; set; }
            public T Tag { get; set; }

            public int Reference
            {
                get;
                private set;
            }
        }

        private readonly List<RectDataImpl> rectangles = new List<RectDataImpl>();

        public void GetRectBeside(IRectangleData<T, int> rect, int side, RectProcessor<T, int> output)
        {
            int dir = side / 2;
            int np = side & 1;
            var rectrect = rect.Rect;
            foreach (var irect in rectangles)
            {
                var irectrect = irect.Rect;
                if (np == 0) //positive side
                {
                    if (irectrect.Range.MinN(dir) == rectrect.Range.MaxN(dir) &&
                        rectrect.Range[dir + 1].Intersect(irectrect.Range[dir + 1]) &&
                        rectrect.Range[dir + 2].Intersect(irectrect.Range[dir + 2]))
                    {
                        output.Process(irect);
                    }
                }
                else //negative side
                {
                    if (irectrect.Range.MaxN(dir) == rectrect.Range.MinN(dir) &&
                        rectrect.Range[dir + 1].Intersect(irectrect.Range[dir + 1]) &&
                        rectrect.Range[dir + 2].Intersect(irectrect.Range[dir + 2]))
                    {
                        output.Process(irect);
                    }
                }
            }
        }

        public IRectangleData<T, int> GetRectFromRef(int r)
        {
            return rectangles[r];
        }

        public void AddRectangles()
        {
            for (int ix = -1; ix <= 1; ++ix)
            {
                for (int iy = -1; iy <= 1; ++iy)
                {
                    for (int iz = -1; iz <= 1; ++iz)
                    {
                        rectangles.Add(new RectDataImpl(rectangles.Count)
                        {
                            Rect = new Geometry.Rectangle
                            {
                                Range = new Geometry.Restrict3
                                {
                                    RestrictA = new Geometry.Restrict1 { Min = ix - 0.5f, Max = ix + 0.5f },
                                    RestrictB = new Geometry.Restrict1 { Min = iy - 0.5f, Max = iy + 0.5f },
                                    RestrictC = new Geometry.Restrict1 { Min = iz - 0.5f, Max = iz + 0.5f },
                                }
                            },
                        });
                    }
                }
            }
        }
    }
}
