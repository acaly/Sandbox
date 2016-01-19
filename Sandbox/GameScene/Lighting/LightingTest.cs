using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Lighting
{
    class LightingTest
    {
        private struct Spread
        {
            CommonRectRef rect;
            int dir;
            bool pn;
            int type; //0 lightspace, 1 direct spread, 2 indirect spread
            Geometry.Restrict3 range;
            Geometry.Direction reduce;
            float reduceZero;
        }

        private class LightingInit : LightingHandler<Spread>
        {
            public int Init(ref Geometry.Rectangle rect, ref Spread output)
            {
                throw new NotImplementedException();
            }

            public bool Check(ref Geometry.Rectangle rect, ref Spread oldSpread, ref Spread newSpread)
            {
                throw new NotImplementedException();
            }

            public void SpreadSpread(ref Geometry.Rectangle rect, ref Spread s, List<Spread> list)
            {
                throw new NotImplementedException();
            }

            public void GetSpreadInfo(ref Spread spread, out CommonRectRef rect)
            {
                throw new NotImplementedException();
            }

            public float GetLightnessResult(ref Geometry.Rectangle rect, ref Spread spread, float offsetX, float offsetY, float offsetZ)
            {
                throw new NotImplementedException();
            }
        }

        private static void Main()
        {
            RectProvider<int> provider = new RectProvider<int>();
            provider.AddRectangles();
            var calc = new LightingCalculator(provider);
            LightingInit li = new LightingInit();
            calc.Calculate(li);
        }

        private static void Main2()
        {
            RectProvider<int> rects = new RectProvider<int>();
            rects.AddRectangles();

            Geometry.Rectangle rect = new Geometry.Rectangle();
            int data = 0;
            rects.GetRectFromRef(new CommonRectRef { N = 14 }, ref rect, ref data);

            var light = new Geometry.LightInformation(new Geometry.Direction(-1, -1, -1));
            Geometry.Direction conv = new Geometry.Direction(1, 0, -1);
            light.ConvertSide(ref conv, 2);
            //var face = input.Rect.GetFaceRegion(light, 0, true);
            //var lightSpace = face.MakeLight(light);

            var rectinfo = Geometry.RectangleAdditionalInfo.Create();
            var lightSpaceInfo = Geometry.LightSpaceAdditionalInfo.Create();

            rect.CalculateAdditionalInfo(light, rectinfo);

            var clock = new System.Diagnostics.Stopwatch();
            clock.Start();
            for (int i = 0; i < 10000; ++i) //1000->10ms, 10000->80ms
            {
                var srcPlane = rectinfo.Planes[4]; //z+
                var lightSpace = srcPlane.MakeLight(light);
                lightSpace.CalculateAdditionalInfo(lightSpaceInfo);

                Geometry.PlaneRegion outputRegionProj = new Geometry.PlaneRegion();
                lightSpace.MakeProjection(ref rect, 1, 0, false, rectinfo, lightSpaceInfo, ref outputRegionProj);
            }
            var time = clock.ElapsedMilliseconds;
        }
    }
}
