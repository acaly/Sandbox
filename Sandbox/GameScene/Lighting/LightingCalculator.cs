using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Lighting
{
    interface LightingHandler<Spread>
    {
        int Init(ref Geometry.Rectangle rect, ref Spread output); //return lightness (only for optimization)
        bool Check(ref Geometry.Rectangle rect, ref Spread oldSpread, ref Spread newSpread); //return false if new spread is not needed
        void SpreadSpread(ref Geometry.Rectangle rect, ref Spread s, List<Spread> list);
        void GetSpreadInfo(ref Spread spread, out CommonRectRef rect);
        float GetLightnessResult(ref Geometry.Rectangle rect, ref Spread spread, float offsetX, float offsetY, float offsetZ);
    }

    class LightnessResultAccess<Spread>
    {
        public readonly LightingHandler<Spread> Handler;

        private Dictionary<CommonRectRef, int> firstSpread;
        private List<SpreadInfo> spreadBuffer;

        private class SpreadInfo
        {
            public Spread spread;
            public int nextID;
        }

        public LightnessResultAccess(LightingHandler<Spread> handler)
        {
            this.Handler = handler;
            this.firstSpread = new Dictionary<CommonRectRef, int>();
            this.spreadBuffer = new List<SpreadInfo>() { new SpreadInfo() }; //add an empty spread to reserve id 0
        }

        public void Reset()
        {
            this.firstSpread.Clear();

            this.spreadBuffer.Clear();
            this.spreadBuffer.Add(new SpreadInfo());
        }

        public float GetLightness(ref Geometry.Rectangle rect, CommonRectRef rectref, float offsetX, float offsetY, float offsetZ)
        {
            float max = 0;

            var itor = GetSpreadInRect(rectref).GetEnumerator();
            while (itor.MoveNext())
            {
                var ss = itor.Current;
                var lightness = Handler.GetLightnessResult(ref rect, ref ss, offsetX, offsetY, offsetZ);
                if (lightness > max) max = lightness;
            }
            return max;
        }

        //return true if it's a new rect
        public void AddRect(CommonRectRef rectref)
        {
            firstSpread.Add(rectref, 0);
        }

        public void AddRectSpread(CommonRectRef rectref, ref Spread spread)
        {
            var next = firstSpread[rectref];
            var newNext = spreadBuffer.Count;
            firstSpread[rectref] = newNext;

            spreadBuffer.Add(new SpreadInfo
            {
                nextID = next,
                spread = spread,
            });
        }

        private class SpreadEnumerator : IEnumerator<Spread>, IEnumerable<Spread>
        {
            private int currentID;
            private int start;
            private LightnessResultAccess<Spread> parent;

            public SpreadEnumerator(int start, LightnessResultAccess<Spread> parent)
            {
                this.parent = parent;

                this.start = start;
                this.currentID = -1;
            }

            public Spread Current
            {
                get { return parent.spreadBuffer[currentID].spread; }
            }

            public void Dispose()
            {
                //Do nothing
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (currentID == -1)
                {
                    currentID = start;
                }
                else
                {
                    currentID = parent.spreadBuffer[currentID].nextID;
                }
                return currentID != 0;
            }

            public void Reset()
            {
                currentID = -1;
            }

            public IEnumerator<Spread> GetEnumerator()
            {
                return this;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this;
            }
        }

        public IEnumerable<Spread> GetSpreadInRect(CommonRectRef rectref)
        {
            return new SpreadEnumerator(firstSpread[rectref], this);
        }
    }

    class LightingCalculator
    {
        private struct RectSpreadLinkedList<Spread>
        {
            Spread spread;
            int next;
        }

        private RectProvider<int> provider;
        public const int LightnessMax = 15;

        public LightingCalculator(RectProvider<int> provider)
        {
            this.provider = provider;
        }

        private class InitCaller<Spread> : RectProcessor<int, CommonRectRef>
        {
            private List<Spread>[] output;
            private LightingHandler<Spread> handler;
            private LightnessResultAccess<Spread> result;

            public InitCaller(List<Spread>[] output, LightingHandler<Spread> handler, LightnessResultAccess<Spread> result)
            {
                this.output = output;
                this.handler = handler;
                this.result = result;
            }

            public void Process(CommonRectRef rectref, ref Geometry.Rectangle rect, ref int data)
            {
                result.AddRect(rectref);

                Spread outputSpread = default(Spread);
                var level = handler.Init(ref rect, ref outputSpread);
                if (level > 0)
                {
                    output[level].Add(outputSpread);
                }
            }
        }

        public void Calculate<Spread>(LightnessResultAccess<Spread> result)
        {
            var spreadQueue = new List<Spread>[LightnessMax];
            var handler = result.Handler;

            provider.ForEach(new InitCaller<Spread>(spreadQueue, handler, result));

            Geometry.Rectangle rect = new Geometry.Rectangle();
            int rectdata = 0;
            for (int level = spreadQueue.Length - 1; level > 0; --level)
            {
                var theList = spreadQueue[level];
                for (int i = 0; i < theList.Count; ++i)
                {
                    var spread = theList[i];
                    CommonRectRef rectref;
                    handler.GetSpreadInfo(ref spread, out rectref);

                    if (!provider.GetRectFromRef(rectref, ref rect, ref rectdata))
                    {
                        //TODO this is an error
                        continue;
                    }

                    var itor = result.GetSpreadInRect(rectref).GetEnumerator();
                    while (itor.MoveNext())
                    {
                        var oldSpread = itor.Current;
                        if (!handler.Check(ref rect, ref oldSpread, ref spread))
                        {
                            goto lbl_check_failed;
                        }
                    }
                    //add the spread
                    result.AddRectSpread(rectref, ref spread);

                lbl_check_failed: ;
                    //do nothing here
                }
            }
        }
    }
}
