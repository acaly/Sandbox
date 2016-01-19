using Sandbox.GameScene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Terrain
{
    //AscTerrain is taken from asc file contained in MagicaVoxel, but with the header removed.
    class AscTerrain
    {
        byte[] data;
        int xsize, ysize;
        private const byte MaxHeight = 200;

        public AscTerrain(string filename, int x, int y)
        {
            data = new byte[x * y];
            xsize = x;
            ysize = y;
            using (var file = File.OpenText(filename))
            {
                for (int i = 0; i < y; ++i)
                {
                    string str = file.ReadLine();
                    var split = str.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < x; ++j)
                    {
                        var raw = float.Parse(split[j]);
                        if (raw > MaxHeight)
                        {
                            data[i * x + j] = MaxHeight;
                        }
                        else
                        {
                            data[i * x + j] = (byte)(int)float.Parse(split[j]);
                        }
                    }
                }
            }
        }

        public void Resample(int rate)
        {
            int nxsize = xsize / rate, nysize = ysize / rate;
            var newData = new byte[nxsize * nysize];
            for (int y = 0; y < nysize; ++y)
            {
                for (int x = 0; x < nxsize; ++x)
                {
                    //sample
                    int sum = 0;
                    for (int sx = 0; sx < rate; ++sx)
                    {
                        for (int sy = 0; sy < rate; ++sy)
                        {
                            var oldData = data[(y * rate + sy) * xsize + (x * rate + sx)];
                            sum += oldData;
                        }
                    }
                    newData[y * nxsize + x] = (byte)(sum / (rate * rate));
                }
            }
            data = newData;
            xsize = nxsize;
            ysize = nysize;
        }

        public void CreateWorld(World world, int offsetX, int offsetY, int sizeX, int sizeY)
        {
            int worldOffsetX = offsetX + sizeX / 2, worldOffsetY = offsetY + sizeY / 2;
            for (int i = offsetX; i < offsetX + sizeX; ++i)
            {
                for (int j = offsetY; j < offsetY + sizeY; ++j)
                {
                    var height = data[j * xsize + i];
                    var sur = height;
                    if (i > 0) sur = Math.Min(sur, data[(i - 1) + xsize * j]);
                    if (i < xsize - 1) sur = Math.Min(sur, data[(i + 1) + xsize * j]);
                    if (j > 0) sur = Math.Min(sur, data[i + xsize * (j - 1)]);
                    if (j < ysize - 1) sur = Math.Min(sur, data[i + xsize * (j + 1)]);
                    for (int k = sur + 1; k < height; ++k)
                    {
                        world.SetBlock(i - worldOffsetX, j - worldOffsetY, k, new BlockData { BlockId = 1 });
                    }
                    world.SetBlock(i - worldOffsetX, j - worldOffsetY, height, new BlockData { BlockId = 1 });
                }
            }
        }
    }
}
