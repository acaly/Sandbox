using Sandbox.Graphics;
using Sandbox.Physics;
using SharpDX;
using SharpDX.DXGI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    public struct BlockRenderData
    {
        [RenderDataElement(Format.R32G32B32A32_Float, "POSITION", 0)]
        public Vector4 pos;
        [RenderDataElement(Format.R32G32B32A32_Float, "TEXCOORD", 1)]
        public Vector4 dir_u;
        [RenderDataElement(Format.R32G32B32A32_Float, "TEXCOORD", 2)]
        public Vector4 dir_v;
        [RenderDataElement(Format.R32G32B32A32_Float, "COLOR", 0)]
        public Vector4 col;
    }

    class Chunk : GridStaticEntity, DictionaryValueInit<World>
    {
        public Chunk() { }

        private World world;
        private BlockData[] blocks;
        private int[] collisionIndex; //0 is none, 1 is 0, 2 is 1, etc.
        private bool[] dirty;
        private int w, h;
        private Vector4 basePosition;
        private WorldCoord baseCoord;

        public BlockData GetBlock(int x, int y, int z)
        {
            return blocks[x + y * w + z * w * w];
        }

        public void SetBlock(int x, int y, int z, BlockData b)
        {
            var blockIndex = x + y * w + z * w * w;

            blocks[blockIndex] = b;
            dirty[blockIndex] = true;
        }

        private void UpdateBlockCollision(int x, int y, int z)
        {
            var blockIndex = x + y * w + z * w * w;
            bool hasCollision = blocks[blockIndex].HasCollision();
            //TODO internal blocks optimization (move internal blocks into a new array and only test if normal blocks are ignored)
            int currentCollision = collisionIndex[blockIndex];
            if (hasCollision == (currentCollision != 0)) return;

            if (hasCollision)
            {
                //add
                base.CollisionArray[base.CollisionCount++] = new Box
                {
                    center = new SharpDX.Vector3(x, y, z),
                    halfSize = new SharpDX.Vector3(0.5f, 0.5f, 0.5f),
                };
            }
            else
            {
                base.CollisionArray[currentCollision] = base.CollisionArray[--base.CollisionCount];
            }
        }

        public void Init(World init, WorldCoord pos)
        {
            this.world = init;
            this.w = init.ChunkWidth;
            this.h = init.ChunkHeight;
            int blockSize = h * w * w;
            
            blocks = new BlockData[blockSize];
            collisionIndex = new int[blockSize];
            dirty = new bool[blockSize];
            basePosition = new Vector4(pos.x, pos.y, pos.z, 0);
            baseCoord = pos;

            base.CollisionArray = new Box[blockSize];
            base.CollisionCount = 0;
            base.Position = new SharpDX.Vector3(pos.x, pos.y, pos.z);

            init.OnNewChunk(pos, this);

            renderData = RenderData<BlockRenderData>.Create(init.renderManager, 
                SharpDX.Direct3D.PrimitiveTopology.PointList, new BlockRenderData[0]);
        }

        public GridStaticEntity GetStaticEntity()
        {
            return this;
        }

        private List<BlockRenderData> renderList = new List<BlockRenderData>();

        private BlockData GetBlockDataAt(int x, int y, int z)
        {
            if (x >= 0 && x < w && y >= 0 && y < w && z >= 0 && z < h)
            {
                return blocks[x + y * w + z * w * w];
            }
            return world.GetBlock(x + baseCoord.x, y + baseCoord.y, z + baseCoord.z);
        }

        private bool IsNormalCubeBeside(int x, int y, int z, int offsetX, int offsetY, int offsetZ)
        {
            return GetBlockDataAt(x + offsetX, y + offsetY, z + offsetZ).BlockId != 0;
        }

        private void AppendBlockRenderData(int x, int y, int z, BlockData data)
        {
            if (data.BlockId == 0) return;

            if (!IsNormalCubeBeside(x, y, z, 0, 0, -1)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y + 0.0f, z - 0.5f, 1.0f),
                col = new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                dir_u = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                dir_v = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, 0, 1)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y + 0.0f, z + 0.5f, 1.0f),
                col = new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                dir_u = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                dir_v = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, 1, 0)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y + 0.5f, z + 0.0f, 1.0f),
                col = new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                dir_u = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                dir_v = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, -1, 0)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y - 0.5f, z + 0.0f, 1.0f),
                col = new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                dir_u = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                dir_v = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
            });
            if (!IsNormalCubeBeside(x, y, z, -1, 0, 0)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x - 0.5f, y + 0.0f, z + 0.0f, 1.0f),
                col = new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                dir_u = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                dir_v = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
            });
            if (!IsNormalCubeBeside(x, y, z, 1, 0, 0)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.5f, y + 0.0f, z + 0.0f, 1.0f),
                col = new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                dir_u = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                dir_v = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
            });
        }

        public void Flush()
        {
            renderList.Clear();
            int gridId = 0;
            for (int gridZ = 0; gridZ < h; ++gridZ)
            {
                for (int gridY = 0; gridY < w; ++gridY)
                {
                    for (int gridX = 0; gridX < w; ++gridX)
                    {
                        if (dirty[gridId])
                        {
                            UpdateBlockCollision(gridX, gridY, gridZ);
                        }
                        AppendBlockRenderData(gridX, gridY, gridZ, blocks[gridId]);

                        ++gridId;
                    }
                }
            }
            renderData.ResetBuffer(renderList.ToArray(), 0);
        }

        public RenderData<BlockRenderData> renderData
        {
            get;
            private set;
        }
    }
}
