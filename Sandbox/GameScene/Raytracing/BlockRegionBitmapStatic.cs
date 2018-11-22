using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Raytracing
{
    class BlockRegionBitmapStatic
    {
        public class BitList
        {
            public byte[] Data;

            public BitList(int size)
            {
                if (size == 0) size = 1;
                Data = new byte[(size - 1) / 8 + 1];
            }

            public void Set(int index)
            {
                int bit = index & 7;
                Data[index / 8] |= (byte)(1 << bit);
            }

            public void Clear(int index)
            {
                int bit = index & 7;
                Data[index / 8] &= (byte)~(1 << bit);
            }

            public bool IsSet(int index)
            {
                int bit = index & 7;
                return (Data[index / 8] & (1 << bit)) != 0;
            }
        }

        private WorldCoord min, max;
        private int zlength, ylength, ratio;
        public BitList Data;

        public BlockRegionBitmapStatic(WorldCoord min, WorldCoord max, int ratio)
        {
            this.min = min;
            this.max = max;
            this.ratio = ratio;
            this.ylength = (max.x - min.x) / ratio + 1;
            this.zlength = this.ylength * ((max.y - min.y) / ratio + 1);
            this.Data = new BitList(this.zlength * ((max.z - min.z) / ratio + 1));
        }

        public int GetIndex(WorldCoord coord)
        {
            var ix = coord.x - min.x;
            var iy = coord.y - min.y;
            var iz = coord.z - min.z;
            return (iz / ratio) * zlength + (iy / ratio) * ylength + (ix / ratio);
        }
    }
}
