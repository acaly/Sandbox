using Sandbox.GameScene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Terrain
{
    //Natsu-Maboroshi's format. Taken from Minecraft.
    class NatsuTerrain
    {
        public static void CreateWorld(World world, string filename, int x, int y, int z, int zbase)
        {
            byte[] blockdata;
            using (FileStream fs = File.OpenRead(filename))
            {
                blockdata = new byte[fs.Length];
                fs.Read(blockdata, 0, blockdata.Length);
            }
            int blockdataindex = 0;
            for (int i = 0; i < x; ++i)
            {
                for (int j = 0; j < z; ++j)
                {
                    for (int k = 0; k < y; ++k)
                    {
                        if (blockdata[blockdataindex++] != 0)
                        {
                            world.SetBlock(i - x / 2, k - y / 2, j + zbase, new BlockData { BlockId = 1 });
                        }
                    }
                }
            }
        }
    }
}
