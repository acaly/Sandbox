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

        public int ChunkWidth
        {
            get
            {
                return 8;
            }
        }

        public int ChunkHeight
        {
            get
            {
                return 128;
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
            physics = new GridPhysicWorld(ChunkWidth, 0, 0, 1, 1); //TODO
            x_min = x_max = y_min = y_max = 0;
        }

        private void EnlargeChunkForPhysics(int xoffset, int yoffset, int xsize, int ysize)
        {
            xsize = (xsize < 1) ? 1 : xsize;
            ysize = (ysize < 1) ? 1 : ysize;
            physics.ResetGrid(ChunkWidth, xoffset, yoffset, xsize, ysize);
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

        public static void Main()
        {
            //World world = new World();
            
            //for (int i = -10; i <= 10; ++i)
            //{
            //    for (int j = -10; j <= 10; ++j)
            //    {
            //        world.SetBlock(i, j, 0, new BlockData { BlockId = 1 });
            //        if (i == 10 || i == -10 || j == 10 || j == -10)
            //        {
            //            for (int k = 1; k < 5; ++k)
            //            {
            //                world.SetBlock(i, j, k, new BlockData { BlockId = 1 });
            //            }
            //        }
            //    }
            //}

        }
    }
}
