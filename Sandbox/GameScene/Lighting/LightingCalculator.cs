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
        bool Check(ref Geometry.Rectangle rect, ref Spread oldSpread, ref Spread newSpread);
        void SpreadSpread(ref Geometry.Rectangle rect, ref Spread s, List<Spread> list);
        void GetSpreadInfo(ref Spread spread, out CommonRectRef rect);
        float GetLightnessResult(ref Geometry.Rectangle rect, ref Spread spread, float offsetX, float offsetY, float offsetZ);
    }

    struct RectInfo
    {
        
    }

    class LightnessResultAccess<Spread>
    {
        public readonly LightingHandler<Spread> Handler;

        public LightnessResultAccess(LightingHandler<Spread> handler)
        {
            this.Handler = handler;
        }

        public float GetLightness(ref Geometry.Rectangle rect, CommonRectRef rectref, float offsetX, float offsetY, float offsetZ)
        {
            Spread spread = default(Spread);
            //TODO for each spread, return the max
            return Handler.GetLightnessResult(ref rect, ref spread, offsetX, offsetY, offsetZ);
        }

        //return true if it's a new rect
        public bool AddRect(CommonRectRef rectref)
        {
            return false;
        }

        public void AddRectSpread(CommonRectRef rectref, ref Spread spread)
        {

        }

        public void Reset()
        {

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

            for (int level = spreadQueue.Length - 1; level > 0; --level)
            {
                var theList = spreadQueue[level];
                for (int i = 0; i < theList.Count; ++i)
                {
                    var spread = theList[i];
                    CommonRectRef rectref;
                    handler.GetSpreadInfo(ref spread, out rectref);
                }
            }
        }
    }
}
