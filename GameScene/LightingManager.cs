using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    class LightingManager
    {
        public const int LightnessMax = 15;

        public LightingManager(World world, int x, int y)
        {
            this.world = world;

            generatedRange.xmin = x - 55;
            generatedRange.xmax = x + 55;
            generatedRange.ymin = y - 55;
            generatedRange.ymax = y + 55;
            generatedRange.zmin = 0;
            generatedRange.zmax = World.ChunkHeight - 1;

            System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            clock.Start();

            //generate the rects
            RectGenerator gen = new RectGenerator(this);
            gen.Generate();

            //calculate the lightness
            LightnessCalculator lc = new LightnessCalculator(this);
            lc.Calculate();

            //apply lightness to world
            var itor = new World.BlockIterator(world,
                generatedRange.xmin, generatedRange.xmax,
                generatedRange.ymin, generatedRange.ymax,
                generatedRange.zmin, generatedRange.zmax, true);
            while (itor.Next())
            {
                var block = itor.GetBlock();
                if (block.BlockId != 0)
                {
                    if (itor.GetBlockOffset(1, 0, 0).BlockId == 0)
                    {
                        block.LightnessXP = lc.GetLightnessOnBlock(itor.X + 1, itor.Y, itor.Z);
                    }
                    if (itor.GetBlockOffset(-1, 0, 0).BlockId == 0)
                    {
                        block.LightnessXN = lc.GetLightnessOnBlock(itor.X - 1, itor.Y, itor.Z);
                    }
                    if (itor.GetBlockOffset(0, 1, 0).BlockId == 0)
                    {
                        block.LightnessYP = lc.GetLightnessOnBlock(itor.X, itor.Y + 1, itor.Z);
                    }
                    if (itor.GetBlockOffset(0, -1, 0).BlockId == 0)
                    {
                        block.LightnessYN = lc.GetLightnessOnBlock(itor.X, itor.Y - 1, itor.Z);
                    }
                    if (itor.GetBlockOffset(0, 0, 1).BlockId == 0)
                    {
                        block.LightnessZP = lc.GetLightnessOnBlock(itor.X, itor.Y, itor.Z + 1);
                    }
                    if (itor.GetBlockOffset(0, 0, -1).BlockId == 0)
                    {
                        block.LightnessZN = lc.GetLightnessOnBlock(itor.X, itor.Y, itor.Z - 1);
                    }
                    itor.SetBlock(block);
                }
            }

            var time = clock.ElapsedMilliseconds;
        }


        private struct Rectangle
        {
            public int xmin, xmax, ymin, ymax, zmin, zmax;
        }

        private World world;
        private Rectangle generatedRange;

        private List<Rectangle> rectangles = new List<Rectangle>();
        private int[] rectOfBlock;

        private class RectGenerator
        {
            private LightingManager manager;
            private int xmin, xmax, ymin, ymax, zmin, zmax;
            private int xsize, ysize, zsize;

            private int[] isEmpty; //-1: is empty, >=0: index in emptyList
            private int[] emptyList;
            private int emptyCount;

            public RectGenerator(LightingManager manager)
            {
                this.manager = manager;
                this.xmin = manager.generatedRange.xmin;
                this.xmax = manager.generatedRange.xmax;
                this.ymin = manager.generatedRange.ymin;
                this.ymax = manager.generatedRange.ymax;
                this.zmin = manager.generatedRange.zmin;
                this.zmax = manager.generatedRange.zmax;
                this.xsize = xmax - xmin + 1;
                this.ysize = ymax - ymin + 1;
                this.zsize = zmax - zmin + 1;
            }

            public void Generate()
            {
                //first find all empty blocks
                isEmpty = new int[xsize * ysize * zsize];
                emptyList = new int[xsize * ysize * zsize];
                manager.rectOfBlock = new int[xsize * ysize * zsize];
                emptyCount = 0;

                var itor = new World.BlockIterator(manager.world, xmin, xmax, ymin, ymax, zmin, zmax, false);
                while (itor.Next())
                {
                    if (itor.GetBlock().BlockId == 0)
                    {
                        int index = itor.Z * xsize * ysize + (itor.Y - ymin) * xsize + (itor.X - xmin);
                        int emptyId = emptyCount++;
                        isEmpty[index] = emptyId;
                        emptyList[emptyId] = index;
                    }
                    else
                    {
                        isEmpty[itor.Z * xsize * ysize + (itor.Y - ymin) * xsize + (itor.X - xmin)] = -1;
                    }
                }

                while (emptyCount > 0)
                {
                    Step();
                }
            }

            private bool FindEmpty(out int x, out int y, out int z)
            {
                if (emptyCount == 0)
                {
                    x = y = z = 0;
                    return false;
                }
                int blockIndex = emptyList[emptyCount - 1];
                x = blockIndex % xsize + xmin;
                blockIndex /= xsize;
                y = blockIndex % ysize + ymin;
                z = blockIndex / ysize + zmin;
                return true;
            }

            private bool IsRectEmpty(Rectangle rect)
            {
                for (int zi = rect.zmin; zi <= rect.zmax; ++zi)
                {
                    for (int yi = rect.ymin; yi <= rect.ymax; ++yi)
                    {
                        for (int xi = rect.xmin; xi <= rect.xmax; ++xi)
                        {
                            if (isEmpty[zi * xsize * ysize + (yi - ymin) * xsize + (xi - xmin)] == -1) return false;
                        }
                    }
                }
                return true;
            }

            private bool ExpandRect(ref Rectangle rect, int side, ref int sideAvailable)
            {
                switch (side)
                {
                    case 0:
                        if ((sideAvailable & 1) == 0)
                        {
                            if (rect.xmin > xmin && IsRectEmpty(new Rectangle
                            {
                                xmin = rect.xmin - 1,
                                xmax = rect.xmin - 1,
                                ymin = rect.ymin,
                                ymax = rect.ymax,
                                zmin = rect.zmin,
                                zmax = rect.zmax,
                            }))
                            {
                                --rect.xmin;
                                return true;
                            }
                            else
                            {
                                sideAvailable |= 1;
                            }
                        }
                        if ((sideAvailable & 2) == 0)
                        {
                            if (rect.xmax < xmax && IsRectEmpty(new Rectangle
                            {
                                xmin = rect.xmax + 1,
                                xmax = rect.xmax + 1,
                                ymin = rect.ymin,
                                ymax = rect.ymax,
                                zmin = rect.zmin,
                                zmax = rect.zmax,
                            }))
                            {
                                ++rect.xmax;
                                return true;
                            }
                            else
                            {
                                sideAvailable |= 2;
                            }
                        }
                        break;
                    case 1:
                        if ((sideAvailable & 4) == 0)
                        {
                            if (rect.ymin > ymin && IsRectEmpty(new Rectangle
                            {
                                xmin = rect.xmin,
                                xmax = rect.xmax,
                                ymin = rect.ymin - 1,
                                ymax = rect.ymin - 1,
                                zmin = rect.zmin,
                                zmax = rect.zmax,
                            }))
                            {
                                --rect.ymin;
                                return true;
                            }
                            else
                            {
                                sideAvailable |= 4;
                            }
                        }
                        if ((sideAvailable & 8) == 0)
                        {
                            if (rect.ymax < ymax && IsRectEmpty(new Rectangle
                            {
                                xmin = rect.xmin,
                                xmax = rect.xmax,
                                ymin = rect.ymax + 1,
                                ymax = rect.ymax + 1,
                                zmin = rect.zmin,
                                zmax = rect.zmax,
                            }))
                            {
                                ++rect.ymax;
                                return true;
                            }
                            else
                            {
                                sideAvailable |= 8;
                            }
                        }
                        break;
                    case 2:
                        if ((sideAvailable & 16) == 0)
                        {
                            if (rect.zmin > zmin && IsRectEmpty(new Rectangle
                            {
                                xmin = rect.xmin,
                                xmax = rect.xmax,
                                ymin = rect.ymin,
                                ymax = rect.ymax,
                                zmin = rect.zmin - 1,
                                zmax = rect.zmin - 1,
                            }))
                            {
                                --rect.zmin;
                                return true;
                            }
                            else
                            {
                                sideAvailable |= 16;
                            }
                        }
                        if ((sideAvailable & 32) == 0)
                        {
                            if (rect.zmax < zmax && IsRectEmpty(new Rectangle
                            {
                                xmin = rect.xmin,
                                xmax = rect.xmax,
                                ymin = rect.ymin,
                                ymax = rect.ymax,
                                zmin = rect.zmax + 1,
                                zmax = rect.zmax + 1,
                            }))
                            {
                                ++rect.zmax;
                                return true;
                            }
                            else
                            {
                                sideAvailable |= 32;
                            }
                        }
                        break;
                }
                return false;
            }

            private bool ExpandRect(ref Rectangle rect, int sideA, int sideB, int sideC, ref int sideAvailable)
            {
                return ExpandRect(ref rect, sideA, ref sideAvailable) ||
                    ExpandRect(ref rect, sideB, ref sideAvailable) ||
                    ExpandRect(ref rect, sideC, ref sideAvailable);
            }

            private void Step()
            {
                //first find an empty place
                Rectangle rect;
                if (!FindEmpty(out rect.xmax, out rect.ymax, out rect.zmax)) return;
                rect.xmin = rect.xmax;
                rect.ymin = rect.ymax;
                rect.zmin = rect.zmax;

                //expand it
                bool result = true;

                //TODO sideAvailable seems to have no help in improving the speed, remove it
                int sideAvailable = 0;
                while (result)
                {
                    //we should decide which side to expand
                    //note that these are not actually size, just to compare
                    int xsize = rect.xmax - rect.xmin,
                        ysize = rect.ymax - rect.ymin,
                        zsize = rect.zmax - rect.zmin;
                    if (xsize <= ysize && xsize <= zsize)
                    {
                        if (ysize <= zsize)
                        {
                            result = ExpandRect(ref rect, 0, 1, 2, ref sideAvailable);
                        }
                        else
                        {
                            result = ExpandRect(ref rect, 0, 2, 1, ref sideAvailable);
                        }
                    }
                    else if (ysize <= xsize && ysize <= zsize)
                    {
                        if (xsize <= zsize)
                        {
                            result = ExpandRect(ref rect, 1, 0, 2, ref sideAvailable);
                        }
                        else
                        {
                            result = ExpandRect(ref rect, 1, 2, 0, ref sideAvailable);
                        }
                    }
                    else
                    {
                        if (xsize <= ysize)
                        {
                            result = ExpandRect(ref rect, 2, 0, 1, ref sideAvailable);
                        }
                        else
                        {
                            result = ExpandRect(ref rect, 2, 1, 0, ref sideAvailable);
                        }
                    } //end of if
                } //end of while(true)

                //update empty array
                int rectId = manager.rectangles.Count;
                for (int zi = rect.zmin; zi <= rect.zmax; ++zi)
                {
                    for (int yi = rect.ymin; yi <= rect.ymax; ++yi)
                    {
                        for (int xi = rect.xmin; xi <= rect.xmax; ++xi)
                        {
                            var blockIndex = zi * xsize * ysize + (yi - ymin) * xsize + (xi - xmin);
                            var emptyId = isEmpty[blockIndex];
                            var exchangeBlockIndex = emptyList[--emptyCount];
                            isEmpty[exchangeBlockIndex] = emptyId;
                            emptyList[emptyId] = exchangeBlockIndex;
                            isEmpty[blockIndex] = -1;
                            manager.rectOfBlock[blockIndex] = rectId;
                        }
                    }
                }

                //add rect to list
                manager.rectangles.Add(rect);
            }
        }

        private class LightnessCalculator
        {
            private LightingManager manager;

            private List<LightnessSpread>[] spreadQueue;
            private List<RectSpreadInfo> spreadBuffer = new List<RectSpreadInfo>() { new RectSpreadInfo() }; //reserve index 0
            private RectInfo[] rectInfo;

            private int xsize, ysize;

            private int currentLightness;
            private int[,] rectLastSpread; //index + 1

            private struct LightnessSpread
            {
                public int rectIndex;
                public Rectangle src;
                public int direction; //source direction
                public int intensity;
                public int reduceZ;
                public int nextSpreadOfRect;
            }

            private struct RectSpreadInfo
            {
                public Rectangle srcOffset;
                public int intensity;
                public int next;
                public int reduceZ;
            }

            private struct RectInfo
            {
                public int spreadInfoRoot;
            }

            public LightnessCalculator(LightingManager manager)
            {
                this.manager = manager;
                this.xsize = manager.generatedRange.xmax - manager.generatedRange.xmin + 1;
                this.ysize = manager.generatedRange.ymax - manager.generatedRange.ymin + 1;
            }

            public void Calculate()
            {
                //init
                this.spreadQueue = new List<LightnessSpread>[LightnessMax];
                this.rectInfo = new RectInfo[manager.rectangles.Count];
                this.rectLastSpread = new int[manager.rectangles.Count, LightnessMax];

                for (int i = 0; i < spreadQueue.Length; ++i)
                {
                    spreadQueue[i] = new List<LightnessSpread>();
                }

                currentLightness = 0;

                //start the top rects
                LightnessSpread ls = new LightnessSpread();
                for (int i = 0; i < manager.rectangles.Count; ++i)
                {
                    if (manager.rectangles[i].zmax == World.ChunkHeight - 1)
                    {
                        ls.intensity = 13;
                        ls.direction = 16; //z+
                        ls.rectIndex = i;
                        ls.src = manager.rectangles[i]; ls.src.zmin = ls.src.zmax;
                        ls.reduceZ = 0;
                        Append(ls);
                    }
                }

                //finish queue
                for (int i = spreadQueue.Length - 1; i >= 0; --i)
                {
                    currentLightness = i;

                    for (int j = 0; j < spreadQueue[i].Count; ++j)
                    {
                        SpreadInRect(spreadQueue[i][j], j);
                    }
                }
            }

            public byte GetLightnessOnBlock(int x, int y, int z)
            {
                if (x < manager.generatedRange.xmin || x > manager.generatedRange.xmax ||
                    y < manager.generatedRange.ymin || y > manager.generatedRange.ymax ||
                    z < manager.generatedRange.zmin || z > manager.generatedRange.zmax)
                {
                    return 3;
                }

                int blockIndex = (x - manager.generatedRange.xmin) +
                    (y - manager.generatedRange.ymin) * xsize +
                    (z - manager.generatedRange.zmin) * xsize * ysize;
                int rectIndex = manager.rectOfBlock[blockIndex];
                int offsetX = x - manager.rectangles[rectIndex].xmin;
                int offsetY = y - manager.rectangles[rectIndex].ymin;
                int offsetZ = z - manager.rectangles[rectIndex].zmin;
                int ret = 0;
                foreach (var s in GetAllSpreadInfoForRect(rectIndex))
                {
                    int value = CalculateIntensityInRect(s.srcOffset, s.intensity, offsetX, offsetY, offsetZ, s.reduceZ);
                    if (ret < value) ret = value;
                }
                return (byte)(ret + 1);
            }

            private IEnumerable<RectSpreadInfo> GetAllSpreadInfoForRect(int index)
            {
                int spreadIndex = rectInfo[index].spreadInfoRoot;
                while (spreadIndex != 0)
                {
                    var ret = spreadBuffer[spreadIndex];
                    spreadIndex = ret.next;
                    yield return ret;
                }
                yield break;
            }

            private void Append(LightnessSpread spread)
            {
                if (spread.intensity <= 0) return;
                var theList = spreadQueue[spread.intensity];
                var index = theList.Count;
                theList.Add(spread);

                var lastIndex = rectLastSpread[spread.rectIndex, spread.intensity] - 1;

                //update chain
                if (lastIndex != -1)
                {
                    var theSpread = theList[lastIndex];
                    theSpread.nextSpreadOfRect = index + 1;
                    theList[lastIndex] = theSpread;
                }

                //update lastIndex
                rectLastSpread[spread.rectIndex, spread.intensity] = index + 1;
            }

            private struct SpreadToRectInfo
            {
                public int direction;
            }

            //TODO customize this one
            private Dictionary<int, SpreadToRectInfo> rectIndexHashSet = new Dictionary<int, SpreadToRectInfo>();

            private void SpreadInRect(LightnessSpread spread, int indexInQueue)
            {
                Rectangle rect = manager.rectangles[spread.rectIndex];
                Rectangle rectSrcOffset = spread.src;
                rectSrcOffset.xmax -= rect.xmin;
                rectSrcOffset.xmin -= rect.xmin;
                rectSrcOffset.ymax -= rect.ymin;
                rectSrcOffset.ymin -= rect.ymin;
                rectSrcOffset.zmax -= rect.zmin;
                rectSrcOffset.zmin -= rect.zmin;
                int reduceZ = spread.reduceZ;

                //see if the input intensity is too low
                //check in already added spread
                {
                    int spreadIndex = rectInfo[spread.rectIndex].spreadInfoRoot;
                    while (spreadIndex != 0)
                    {
                        var spreadInfo = spreadBuffer[spreadIndex];
                        var spreadLightnessAtSrc = CalculateIntensityInRect(spreadInfo.srcOffset, spread.intensity, rectSrcOffset, spreadInfo.reduceZ);
                        if (spreadLightnessAtSrc >= spread.intensity)
                        {
                            return;
                        }
                        spreadIndex = spreadInfo.next;
                    }
                }
                //check queue
                {
                    //higher ones have been applied, lower ones can't have same intensity
                    int index = spread.nextSpreadOfRect;
                    while (index != 0)
                    {
                        var spreadInfo = spreadQueue[spread.intensity][index - 1];
                        var spreadLightnessAtSrc = CalculateIntensityInRect(spread.src, spread.intensity, spreadInfo.src, reduceZ);
                        if (spreadLightnessAtSrc >= spread.intensity)
                        {
                            return;
                        }
                        index = spreadInfo.nextSpreadOfRect;
                    }
                }

                //add this to list
                {
                    int spreadIndex = rectInfo[spread.rectIndex].spreadInfoRoot;
                    int newSpreadIndex = spreadBuffer.Count;
                    spreadBuffer.Add(new RectSpreadInfo
                    {
                        intensity = spread.intensity,
                        next = spreadIndex,
                        srcOffset = rectSrcOffset,
                        reduceZ = reduceZ,
                    });
                    rectInfo[spread.rectIndex].spreadInfoRoot = newSpreadIndex;
                }

                //spead to surrondings
                //first get a list of rectangles to spread into
                rectIndexHashSet.Clear();
                //z
                for (int xi = rect.xmin; xi <= rect.xmax; ++xi)
                {
                    for (int yi = rect.ymin; yi <= rect.ymax; ++yi)
                    {
                        if (rect.zmin > manager.generatedRange.zmin)
                        {
                            var zi = rect.zmin - 1;
                            var rectIndex = manager.rectOfBlock[BlockIndexOf(xi, yi, zi)];
                            var newInfo = new SpreadToRectInfo
                            {
                                direction = 16, //z+
                            };
                            if (!rectIndexHashSet.ContainsKey(rectIndex))
                            {
                                rectIndexHashSet[rectIndex] = newInfo;
                            }
                        }
                        if (rect.zmax < manager.generatedRange.zmax)
                        {
                            var zi = rect.zmax + 1;
                            var rectIndex = manager.rectOfBlock[BlockIndexOf(xi, yi, zi)];
                            var newInfo = new SpreadToRectInfo
                            {
                                direction = 32, //z-
                            };
                            if (!rectIndexHashSet.ContainsKey(rectIndex))
                            {
                                rectIndexHashSet[rectIndex] = newInfo;
                            }
                        }
                    }
                }
                //y
                for (int zi = rect.zmin; zi <= rect.zmax; ++zi)
                {
                    for (int xi = rect.xmin; xi <= rect.xmax; ++xi)
                    {
                        if (rect.ymin > manager.generatedRange.ymin)
                        {
                            var yi = rect.ymin - 1;
                            var rectIndex = manager.rectOfBlock[BlockIndexOf(xi, yi, zi)];
                            var newInfo = new SpreadToRectInfo
                            {
                                direction = 4, //y+
                            };
                            if (!rectIndexHashSet.ContainsKey(rectIndex))
                            {
                                rectIndexHashSet[rectIndex] = newInfo;
                            }
                        }
                        if (rect.ymax < manager.generatedRange.ymax)
                        {
                            var yi = rect.ymax + 1;
                            var rectIndex = manager.rectOfBlock[BlockIndexOf(xi, yi, zi)];
                            var newInfo = new SpreadToRectInfo
                            {
                                direction = 8, //y-
                            };
                            if (!rectIndexHashSet.ContainsKey(rectIndex))
                            {
                                rectIndexHashSet[rectIndex] = newInfo;
                            }
                        }
                    }
                }
                //x
                for (int yi = rect.ymin; yi <= rect.ymax; ++yi)
                {
                    for (int zi = rect.zmin; zi <= rect.zmax; ++zi)
                    {
                        if (rect.xmin > manager.generatedRange.xmin)
                        {
                            var xi = rect.xmin - 1;
                            var rectIndex = manager.rectOfBlock[BlockIndexOf(xi, yi, zi)];
                            var newInfo = new SpreadToRectInfo
                            {
                                direction = 1, //x+
                            };
                            if (!rectIndexHashSet.ContainsKey(rectIndex))
                            {
                                rectIndexHashSet[rectIndex] = newInfo;
                            }
                        }
                        if (rect.xmax < manager.generatedRange.xmax)
                        {
                            var xi = rect.xmax + 1;
                            var rectIndex = manager.rectOfBlock[BlockIndexOf(xi, yi, zi)];
                            var newInfo = new SpreadToRectInfo
                            {
                                direction = 2, //x-
                            };
                            if (!rectIndexHashSet.ContainsKey(rectIndex))
                            {
                                rectIndexHashSet[rectIndex] = newInfo;
                            }
                        }
                    }
                }

                //process all the rectangles
                //if direction is same, use the same value as reduce, otherwise, use 1
                foreach (var spreadRect in rectIndexHashSet)
                {
                    Rectangle destRect = manager.rectangles[spreadRect.Key];
                    var newSpread = new LightnessSpread
                    {
                        direction = spreadRect.Value.direction,
                        rectIndex = spreadRect.Key,
                        reduceZ = spreadRect.Value.direction == 16 ? reduceZ : 1,
                    };
                    CalculateIntensityInRect(spread.src, spread.intensity, spread.reduceZ, destRect, 
                        out newSpread.src, out newSpread.intensity);
                    Append(newSpread);
                }
            }

            private int BlockIndexOf(int x, int y, int z)
            {
                return (x - manager.generatedRange.xmin) +
                    (y - manager.generatedRange.ymin) * xsize +
                    (z - manager.generatedRange.zmin) * xsize * ysize;
            }

            private int CalculateIntensityInRect(int src, int xdiff, int ydiff, int zdiff, int reduce)
            {
                int ret = src - (Math.Abs(xdiff) + Math.Abs(ydiff) + Math.Abs(zdiff)) * reduce;
                return ret >= 0 ? ret : 0;
            }

            private int CalculateIntensityInRect(Rectangle srcRect, int srcIntensity, int x, int y, int z, int reduceZ)
            {
                int dist = 0;
                if (x < srcRect.xmin) dist += srcRect.xmin - x;
                else if (x > srcRect.xmax) dist += x - srcRect.xmax;
                if (y < srcRect.ymin) dist += srcRect.ymin - y;
                else if (y > srcRect.ymax) dist += y - srcRect.ymax;
                if (reduceZ != 0)
                {
                    if (z < srcRect.zmin) dist += (srcRect.zmin - z) * reduceZ;
                    else if (z > srcRect.zmax) dist += (z - srcRect.zmax) * reduceZ;
                }
                return dist >= srcIntensity ? 0 : srcIntensity - dist;
            }

            //calculate the minimal intensity from srcRect to destRect
            private int CalculateIntensityInRect(Rectangle srcRect, int srcIntensity, Rectangle destRect, int reduceZ)
            {
                int dist = 0;
                {
                    int distX = Math.Max(destRect.xmax - srcRect.xmax, srcRect.xmin - destRect.xmin);
                    if (distX > 0) dist += distX;
                }
                {
                    int distY = Math.Max(destRect.ymax - srcRect.ymax, srcRect.ymin - destRect.ymin);
                    if (distY > 0) dist += distY;
                }
                {
                    int distZ = Math.Max(destRect.zmax - srcRect.zmax, srcRect.zmin - destRect.zmin);
                    if (distZ > 0) dist += distZ * reduceZ;
                }
                srcIntensity -= dist;
                return srcIntensity <= 0 ? 0 : srcIntensity;
            }

            //spread light from srcRect to destRect
            //calculate effective output
            private void CalculateIntensityInRect(Rectangle srcRect, int srcIntensity, int reduceZ, Rectangle destRect, 
                out Rectangle outRect, out int outIntensity)
            {
                int dist = 0;
                outRect = new Rectangle();
                //x
                if (destRect.xmax <= srcRect.xmin)
                {
                    outRect.xmax = outRect.xmin = destRect.xmax;
                    dist += srcRect.xmin - destRect.xmax;
                }
                else if (destRect.xmin >= srcRect.xmax)
                {
                    outRect.xmax = outRect.xmin = destRect.xmin;
                    dist += destRect.xmin - srcRect.xmax;
                }
                else
                {
                    outRect.xmin = Math.Max(srcRect.xmin, destRect.xmin);
                    outRect.xmax = Math.Min(srcRect.xmax, destRect.xmax);
                }
                //y
                if (destRect.ymax <= srcRect.ymin)
                {
                    outRect.ymax = outRect.ymin = destRect.ymax;
                    dist += srcRect.ymin - destRect.ymax;
                }
                else if (destRect.ymin >= srcRect.ymax)
                {
                    outRect.ymax = outRect.ymin = destRect.ymin;
                    dist += destRect.ymin - srcRect.ymax;
                }
                else
                {
                    outRect.ymin = Math.Max(srcRect.ymin, destRect.ymin);
                    outRect.ymax = Math.Min(srcRect.ymax, destRect.ymax);
                }
                //z
                if (destRect.zmax <= srcRect.zmin)
                {
                    outRect.zmax = outRect.zmin = destRect.zmax;
                    dist += (srcRect.zmin - destRect.zmax) * reduceZ;
                }
                else if (destRect.zmin >= srcRect.zmax)
                {
                    outRect.zmax = outRect.zmin = destRect.zmin;
                    dist += (destRect.zmin - srcRect.zmax) * reduceZ;
                }
                else
                {
                    outRect.zmin = Math.Max(srcRect.zmin, destRect.zmin);
                    outRect.zmax = Math.Min(srcRect.zmax, destRect.zmax);
                }
                if (dist >= srcIntensity)
                {
                    outIntensity = 0;
                }
                else
                {
                    outIntensity = srcIntensity - dist;
                }
            }
        }
    }
}
