using Sandbox.Graphics;
using Sandbox.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    class World
    {
        GridPhysicWorld physics;
        CoordDictionary<Chunk, World> chunks;
        public List<Chunk> chunkList = new List<Chunk>();
        public RenderManager renderManager;

        //used by chunks to make their collision list
        public List<Box> collisionBuffer = new List<Box>();

        public static int ChunkWidth
        {
            get
            {
                return 8;
            }
        }

        public static int ChunkHeight
        {
            get
            {
                return 256;
            }
        }

        public static int ChunkLayerHeight
        {
            get
            {
                return 4;
            }
        }

        public World(RenderManager renderManager)
        {
            chunks = new CoordDictionary<Chunk, World>(this);
            InitPhysics();
            this.renderManager = renderManager;
        }

        public BlockData GetBlock(int x, int y, int z)
        {
            if (z < 0) return new BlockData();
            if (z >= ChunkHeight) return new BlockData();

            var chunkCoord = new WorldCoord
            {
                x = ChunkWidth * (int)Math.Floor((float)x / ChunkWidth),
                y = ChunkWidth * (int)Math.Floor((float)y / ChunkWidth),
                z = 0,
            };
            var chunk = chunks.Get(chunkCoord);

            if (chunk == null) return new BlockData();

            return chunk.GetBlock(x - chunkCoord.x, y - chunkCoord.y, z);
        }

        public void SetBlock(int x, int y, int z, BlockData b)
        {
            var chunkCoord = new WorldCoord
            {
                x = ChunkWidth * (int)Math.Floor((float)x / ChunkWidth),
                y = ChunkWidth * (int)Math.Floor((float)y / ChunkWidth),
                z = 0,
            };
            var chunk = chunks.GetOrCreate(chunkCoord);
            chunk.SetBlock(x - chunkCoord.x, y - chunkCoord.y, z, b);
        }

        public void StepPhysics(float time)
        {
            physics.Step(time);
        }

        public void AddEntity(Entity entity)
        {
            physics.AddEntity(entity);
        }

        private int x_min, x_max, y_min, y_max;

        private void InitPhysics()
        {
            physics = new GridPhysicWorld(ChunkLayerHeight, ChunkWidth, 0, 0, 1, 1); //TODO
            x_min = x_max = y_min = y_max = 0;
        }

        private void EnlargeChunkForPhysics(int xoffset, int yoffset, int xsize, int ysize)
        {
            xsize = (xsize < 1) ? 1 : xsize;
            ysize = (ysize < 1) ? 1 : ysize;
            physics.ResetGrid(ChunkLayerHeight, ChunkWidth, xoffset, yoffset, xsize, ysize);
        }

        private void EnsureChunkForPhysics(int blockX, int blockY)
        {
            int x = (int)Math.Floor((float)blockX / ChunkWidth);
            int y = (int)Math.Floor((float)blockY / ChunkWidth);
            bool enlarge = false;
            int nx_min = x_min, nx_max = x_max, ny_min = y_min, ny_max = y_max;
            if (x < x_min) { nx_min = x; enlarge = true; }
            if (x > x_max) { nx_max = x; enlarge = true; }
            if (y < y_min) { ny_min = y; enlarge = true; }
            if (y > y_max) { ny_max = y; enlarge = true; }
            if (enlarge)
            {
                EnlargeChunkForPhysics(-nx_min, -ny_min, nx_max - nx_min + 1, ny_max - ny_min + 1);
                x_min = nx_min;
                x_max = nx_max;
                y_min = ny_min;
                y_max = ny_max;
            }
        }

        private void AddChunkToPhysics(int x, int y, Chunk chunk)
        {
            var pos = new SharpDX.Vector3(x, y, 0);
            physics.SetGridEntity(pos, chunk.GetStaticEntity());
        }

        public void OnNewChunk(WorldCoord pos, Chunk chunk)
        {
            chunkList.Add(chunk);

            EnsureChunkForPhysics(pos.x, pos.y);
            AddChunkToPhysics(pos.x, pos.y, chunk);
        }

        //for fast block iteration
        //can not add new block
        public class BlockIterator
        {
            private World world;
            private int minx, maxx, miny, maxy, minz, maxz;
            private bool skipEmpty;

            public BlockIterator(World world, int minx, int maxx, int miny, int maxy, int minz, int maxz, bool skipEmpty = true)
            {
                this.world = world;
                this.minx = minx;
                this.maxx = maxx;
                this.miny = miny;
                this.maxy = maxy;
                this.minz = minz;
                this.maxz = maxz;
                this.skipEmpty = skipEmpty;

                //calculate chunks
                var minChunkX = (int)Math.Floor((float)minx / ChunkWidth);
                var minChunkY = (int)Math.Floor((float)miny / ChunkWidth);
                var maxChunkX = (int)Math.Floor((float)maxx / ChunkWidth);
                var maxChunkY = (int)Math.Floor((float)maxy / ChunkWidth);
                IterateChunk[] chunks = new IterateChunk[(maxChunkX - minChunkX + 1) * (maxChunkY - minChunkY + 1)];
                int index = 0;
                var ic = new IterateChunk();
                for (int yi = minChunkY; yi <= maxChunkY; ++yi)
                {
                    for (int xi = minChunkX; xi <= maxChunkX; ++xi)
                    {
                        ic.chunkx = xi * ChunkWidth;
                        ic.chunky = yi * ChunkWidth;
                        ic.chunk = world.chunks.Get(new WorldCoord(ic.chunkx, ic.chunky, 0));
                        if (!skipEmpty || ic.chunk != null)
                        {
                            //TODO use a fake chunk object to avoid branch
                            ic.minx = xi == minChunkX ? minx - ic.chunkx : 0;
                            ic.miny = yi == minChunkY ? miny - ic.chunky : 0;
                            ic.maxx = xi == maxChunkX ? maxx - ic.chunkx : ChunkWidth - 1;
                            ic.maxy = yi == maxChunkY ? maxy - ic.chunky : ChunkWidth - 1;
                            chunks[index++] = ic;
                        }
                    }
                }
                this.chunks = chunks;
                this.chunksCount = index;

                //init
                currentZ = maxz;
                currentChunk = -1;
            }

            private struct IterateChunk {
                public Chunk chunk;
                public int chunkx, chunky, minx, maxx, miny, maxy;
            }

            //static data
            private IterateChunk[] chunks;
            private int chunksCount;

            //chunk
            private int currentChunk;
            private Chunk currentChunkObject;
            private int currentChunkX, currentChunkY;
            //chunk restriction
            private int internalXMax, internalXMin, internalYMax, internalYMin;

            //z
            private int currentZ, currentLayerOffset, lastLayerOffset, nextLayerOffset;

            //block
            private int currentInternalX, currentInternalY;

            public bool Next()
            {
                //increase X
                if (++currentInternalX > internalXMax)
                {
                    //increase Y
                    if (++currentInternalY > internalYMax)
                    {
                        //increase z
                        do
                        {
                            ++currentZ;
                            if (currentZ > maxz)
                            {
                                //switch chunk
                                if (++currentChunk == chunksCount)
                                {
                                    //iteration finished
                                    return false;
                                }
                                var nextChunkInfo = chunks[currentChunk];
                                currentChunkObject = nextChunkInfo.chunk;
                                currentChunkX = nextChunkInfo.chunkx;
                                currentChunkY = nextChunkInfo.chunky;
                                internalXMin = nextChunkInfo.minx;
                                internalXMax = nextChunkInfo.maxx;
                                internalYMin = nextChunkInfo.miny;
                                internalYMax = nextChunkInfo.maxy;

                                //reset z
                                currentZ = 0;
                                lastLayerOffset = 0;
                                currentLayerOffset = currentChunkObject != null ? currentChunkObject.GetBlockIndex(0, 0, 0) : 0;
                                nextLayerOffset = currentChunkObject != null ? currentChunkObject.GetBlockIndex(0, 0, 1) : 0;
                            }
                            else
                            {
                                //normal increase
                                lastLayerOffset = currentLayerOffset;
                                currentLayerOffset = nextLayerOffset;
                                nextLayerOffset = currentChunkObject != null ? currentChunkObject.GetBlockIndex(0, 0, currentZ + 1) : 0; //overflow is allowed
                            }
                        } while (skipEmpty && currentLayerOffset == 0);

                        currentInternalY = internalYMin;
                    }
                    currentInternalX = internalXMin;
                }
                return true;
            }

            public BlockData GetBlock()
            {
                return currentChunkObject != null ? 
                    currentChunkObject.RawGetBlock(currentLayerOffset + currentInternalY * ChunkWidth + currentInternalX) :
                    new BlockData();
            }

            public BlockData GetBlockOffset(int x, int y, int z)
            {
                if (z == currentZ)
                {
                    int internalX = x + currentInternalX, internalY = y + currentInternalY;
                    if (internalX >= 0 && internalX < ChunkWidth && internalY >= 0 && internalY < ChunkWidth)
                    {
                        //in the same chunk
                        return currentChunkObject.RawGetBlock(currentLayerOffset + internalY * ChunkWidth + internalX);
                    }
                    else
                    {
                        //in another chunk (slow)
                        return world.GetBlock(currentChunkX + internalX, currentChunkY + internalY, z + currentZ);
                    }
                }
                else
                {
                    //different z
                    //only optimize when x==y==0, z=1 or -1
                    if (x == 0 && y == 0)
                    {
                        if (z == 1)
                            return currentChunkObject.RawGetBlock(nextLayerOffset + currentInternalY * ChunkWidth + currentInternalX);
                        else if (z == -1)
                            return currentChunkObject.RawGetBlock(lastLayerOffset + currentInternalY * ChunkWidth + currentInternalX);
                    }
                    //otherwise (slow)
                    return world.GetBlock(currentChunkX + currentInternalX + x, currentChunkY + currentInternalY + y, currentZ + z);
                }
            }

            public void SetBlock(BlockData block)
            {
                currentChunkObject.RawSetBlock(currentLayerOffset + currentInternalY * ChunkWidth + currentInternalX, block);
            }

            public int X
            {
                get
                {
                    return currentChunkX + currentInternalX;
                }
            }

            public int Y
            {
                get
                {
                    return currentChunkY + currentInternalY;
                }
            }

            public int Z
            {
                get
                {
                    return currentZ;
                }
            }
        }
    }
}
