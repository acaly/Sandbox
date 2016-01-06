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
        private static Dictionary<int, int> colorMap;

        static NatsuTerrain()
        {
            colorMap = new Dictionary<int, int>();
            var lines = File.ReadAllLines("minecraft_palette.txt");
            foreach (var line in lines)
            {
                if (line.Length == 0 || line.StartsWith("//"))
                {
                    continue;
                }
                var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var block = fields[0].Split(':');
                colorMap.Add(int.Parse(block[0]) << 8 | int.Parse(block[1]),
                    int.Parse(fields[3]) | int.Parse(fields[2]) << 8 | int.Parse(fields[1]) << 16);
            }
        }

        private static int GetColor(int blockId, int meta)
        {
            int block = blockId << 8 | meta;
            if (colorMap.ContainsKey(block))
            {
                return colorMap[block];
            }
            return 0;
        }

        public static void CreateWorld(World world, string filename, int zbase)
        {
            byte[] blockdata;
            byte[] blockmeta;
            int x, y, z;
            using (FileStream fs = File.OpenRead(filename))
            {
                byte[] intBuffer = new byte[12];
                fs.Read(intBuffer, 0, 12);
                x = BitConverter.ToInt32(intBuffer, 0);
                y = BitConverter.ToInt32(intBuffer, 4);
                z = BitConverter.ToInt32(intBuffer, 8);

                int size = x * y * z;
                blockdata = new byte[size];
                fs.Read(blockdata, 0, blockdata.Length);
                blockmeta = new byte[size];
                if (fs.Read(blockmeta, 0, blockmeta.Length) <= 0)
                {
                    blockmeta = null;
                }
            }
            int blockdataindex = 0;
            HashSet<byte> blocks = new HashSet<byte>();
            for (int i = 0; i < x; ++i)
            {
                for (int j = 0; j < z; ++j)
                {
                    for (int k = 0; k < y; ++k)
                    {
                        var block = blockdata[blockdataindex];
                        var meta = blockmeta != null ? blockmeta[blockdataindex] : 0;
                        blocks.Add(block);
                        if (block != 0 || j == 0)
                        {
                            world.SetBlock(i - x / 2, k - y / 2, j + zbase, new BlockData { BlockId = 1, BlockColor = GetColor(block, meta) });
                        }

                        ++blockdataindex;
                    }
                }
            }
        }
    }
}
