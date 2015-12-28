using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    class LightingManager
    {
        public LightingManager(World world, int x, int y)
        {
            this.world = world;
            generatedRange.xmin = x - 50;
            generatedRange.xmax = x + 50;
            generatedRange.ymin = y - 50;
            generatedRange.ymax = y + 50;
            generatedRange.zmin = 0;
            generatedRange.zmax = World.ChunkHeight - 1;
            RectGenerator gen = new RectGenerator(this);
            System.Diagnostics.Stopwatch clock = new System.Diagnostics.Stopwatch();
            clock.Start();
            gen.Generate();
            var time = clock.ElapsedMilliseconds;
        }

        private struct Rectangle
        {
            public int xmin, xmax, ymin, ymax, zmin, zmax;
        }

        private Rectangle generatedRange;
        private List<Rectangle> rectangles = new List<Rectangle>();
        private int[] Rect;
        private World world;

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

            //TODO currently the bottleneck is at block iteration, improve that
            public void Generate()
            {
                //first find all empty blocks
                isEmpty = new int[xsize * ysize * zsize];
                emptyList = new int[xsize * ysize * zsize];
                emptyCount = 0;
                for (int zi = 0; zi < World.ChunkHeight; ++zi)
                {
                    for (int yi = ymin; yi <= ymax; ++yi)
                    {
                        for (int xi = xmin; xi <= xmax; ++xi)
                        {
                            if (manager.world.GetBlock(xi, yi, zi).BlockId == 0)
                            {
                                int index = zi * xsize * ysize + (yi - ymin) * xsize + (xi - xmin);
                                int emptyId = emptyCount++;
                                isEmpty[index] = emptyId;
                                emptyList[emptyId] = index;
                            }
                            else
                            {
                                isEmpty[zi * xsize * ysize + (yi - ymin) * xsize + (xi - xmin)] = -1;
                            }
                        }
                    }
                }

                while (emptyCount > 0)
                {
                    Step();
                }
            }

            private bool FindEmpty(out int x, out int y, out int z)
            {
                //TODO allow to search from a random offset
                //for (int zi = 0; zi < World.ChunkHeight; ++zi)
                //{
                //    for (int yi = ymin; yi <= ymax; ++yi)
                //    {
                //        for (int xi = xmin; xi <= xmax; ++xi)
                //        {
                //            if (isEmpty[zi * xsize * ysize + (yi - ymin) * xsize + (xi - xmin)] == -1)
                //            {
                //                x = xi;
                //                y = yi;
                //                z = zi;
                //                return true;
                //            }
                //        }
                //    }
                //}
                //x = y = z = 0;
                //return false;
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
                        }
                    }
                }

                //add rect to list
                manager.rectangles.Add(rect);
            }

        }
    }
}
