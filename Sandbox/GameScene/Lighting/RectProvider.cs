using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Lighting
{
    interface IRectProvider<T, TRef>
    {
        void GetRectBeside(TRef rect, int side, RectProcessor<T, TRef> output);
        bool GetRectFromRef(TRef r, ref Geometry.Rectangle rect, ref T data);
        void ForEach(RectProcessor<T, TRef> output);
    }

    interface RectProcessor<T, TRef>
    {
        void Process(TRef rectref, ref Geometry.Rectangle rect, ref T data);
    }

    struct CommonRectRef
    {
        public int X, Y, N;
    }

    class RectProvider<T> : IRectProvider<T, CommonRectRef>
    {
        private struct RectData
        {
            public Geometry.Rectangle rect;
            public T data;
        }

        private readonly List<RectData> rectangles = new List<RectData>();

        public void GetRectBeside(CommonRectRef rect, int side, RectProcessor<T, CommonRectRef> output)
        {
            int dir = side / 2;
            int np = side & 1;
            var rectrect = rectangles[rect.N].rect;
            CommonRectRef rectrefOutput = new CommonRectRef();
            for (int i = 0; i < rectangles.Count; ++i)
            {
                var irectrect = rectangles[i].rect;
                if (np == 0) //positive side
                {
                    if (irectrect.Range.MinN(dir) == rectrect.Range.MaxN(dir) &&
                        rectrect.Range[dir + 1].Intersect(irectrect.Range[dir + 1]) &&
                        rectrect.Range[dir + 2].Intersect(irectrect.Range[dir + 2]))
                    {
                        rectrefOutput.N = i;
                        var rectdata = rectangles[i]; //TODO
                        output.Process(rectrefOutput, ref irectrect, ref rectdata.data);
                        rectangles[i] = rectdata;
                    }
                }
                else //negative side
                {
                    if (irectrect.Range.MaxN(dir) == rectrect.Range.MinN(dir) &&
                        rectrect.Range[dir + 1].Intersect(irectrect.Range[dir + 1]) &&
                        rectrect.Range[dir + 2].Intersect(irectrect.Range[dir + 2]))
                    {
                        rectrefOutput.N = i;
                        var rectdata = rectangles[i]; //TODO
                        output.Process(rectrefOutput, ref irectrect, ref rectdata.data);
                        rectangles[i] = rectdata;
                    }
                }
            }
        }

        public void AddRectangles()
        {
            for (int ix = -1; ix <= 1; ++ix)
            {
                for (int iy = -1; iy <= 1; ++iy)
                {
                    for (int iz = -1; iz <= 1; ++iz)
                    {
                        rectangles.Add(new RectData
                        {
                            rect = new Geometry.Rectangle
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

        public void ForEach(RectProcessor<T, CommonRectRef> output)
        {
            CommonRectRef rectrefOutput = new CommonRectRef();
            for (int i = 0; i < rectangles.Count; ++i)
            {
                var irectrect = rectangles[i].rect;
                rectrefOutput.N = i;
                var rectdata = rectangles[i]; //TODO
                output.Process(rectrefOutput, ref irectrect, ref rectdata.data);
                rectangles[i] = rectdata;
            }
        }

        public bool GetRectFromRef(CommonRectRef r, ref Geometry.Rectangle rect, ref T data)
        {
            if (r.N < rectangles.Count)
            {
                rect = rectangles[r.N].rect;
                data = rectangles[r.N].data;
                return true;
            }
            return false;
        }
    }
}
