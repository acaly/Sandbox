using Sandbox.GameScene.Lighting.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Lighting.Sunlight
{
    struct Spread_LightSpace
    {
        public Restrict3 Range;
    }
    struct Spread_LightProjection
    {
        public PlaneRegion Source;
        public int LightSpaceFaceDir;
        public float LightSpaceFacePos;
    }
    struct Spread_Bent
    {
        public ProjectionBentSpread BentData;
    }

    [StructLayout(LayoutKind.Explicit, Size = 4 * 16)] //occupied 4 * 15
    struct Spread_All
    {
        [FieldOffset(0)]
        public int Type;

        [FieldOffset(4)]
        public Spread_LightSpace A;

        [FieldOffset(4)]
        public Spread_LightProjection B;

        [FieldOffset(4)]
        public Spread_Bent C;
    }

    class SunlightHanlder : LightingHandler<Spread_All>
    {
        public SunlightHanlder(Direction lightDir, RectProvider<int> provider)
        {

        }

        public int Init(ref Rectangle rect, ref Spread_All output)
        {
            throw new NotImplementedException();
        }

        public bool Check(ref Rectangle rect, ref Spread_All oldSpread, ref Spread_All newSpread)
        {
            throw new NotImplementedException();
        }

        public void SpreadSpread(ref Rectangle rect, ref Spread_All s, List<Spread_All> list)
        {
            throw new NotImplementedException();
        }

        public void GetSpreadInfo(ref Spread_All spread, out CommonRectRef rect)
        {
            throw new NotImplementedException();
        }

        public float GetLightnessResult(ref Rectangle rect, ref Spread_All spread, float offsetX, float offsetY, float offsetZ)
        {
            throw new NotImplementedException();
        }
    }
}
