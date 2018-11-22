using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Raytracing
{
    class RaytracingAccelerator
    {
        private BlockRegionBitmapStatic level1, level2;

        public RaytracingAccelerator()
        {
        }

        public void Build(World world, WorldCoord min, WorldCoord max)
        {
            level1 = new BlockRegionBitmapStatic(min, max, 10);
            level2 = new BlockRegionBitmapStatic(min, max, 100);
            var iterator = new World.BlockIterator(world, min.x, max.x, min.y, max.y, min.z, max.z, true);
            while (iterator.Next())
            {
                if (iterator.GetBlock().BlockId == 0)
                {
                    continue;
                    //throw new Exception();
                }
                SetBitmap(iterator.X, iterator.Y, iterator.Z);
            }
        }

        private void SetBitmap(int x, int y, int z)
        {
            var coord = new WorldCoord(x, y, z);
            level1.Data.Set(level1.GetIndex(coord));
            level2.Data.Set(level2.GetIndex(coord));
        }

        public void Cast(WorldCoord coord, Vector3 x)
        {

        }
    }
}
