using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Lighting
{
    class LightingTest
    {
        private static void Main()
        {
            RectProvider<int> rects = new RectProvider<int>();
            rects.AddRectangles();
            var input = rects.GetRectFromRef(14);
            var rect = input.Rect;

            var light = new Geometry.LightInformation(new Geometry.Direction(-1, -1, -1));
            Geometry.Direction conv = new Geometry.Direction(1, 0, -1);
            light.ConvertSide(ref conv, 2);
            //var face = input.Rect.GetFaceRegion(light, 0, true);
            //var lightSpace = face.MakeLight(light);

            var rectinfo = Geometry.RectangleAdditionalInfo.Create();
            rect.CalculateAdditionalInfo(light, rectinfo);

            var srcPlane = rectinfo.Planes[4]; //z+
            var lightSpace = srcPlane.MakeLight(light);
            var lightSpaceInfo = Geometry.LightSpaceAdditionalInfo.Create();
            lightSpace.CalculateAdditionalInfo(lightSpaceInfo);

            Geometry.PlaneRegion outputRegionProj = new Geometry.PlaneRegion();
            lightSpace.MakeProjection(ref rect, 1, 0, false, rectinfo, lightSpaceInfo, ref outputRegionProj);
        }
    }
}
