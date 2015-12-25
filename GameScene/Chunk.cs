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
        [RenderDataElement(Format.R32G32B32A32_Float, "TEXCOORD", 3)]
        public Vector4 aooffset;
    }

    class Chunk : GridStaticEntity, DictionaryValueInit<World>
    {
        public Chunk() { }

        private World world;
        private BlockData[] blocks;
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

        private void AppendBlockCollision(int x, int y, int z)
        {
            var blockIndex = x + y * w + z * w * w;
            bool hasCollision = blocks[blockIndex].HasCollision();

            if (hasCollision)
            {
                if (
                    IsNormalCubeBeside(x, y, z, 1, 0, 0) &&
                    IsNormalCubeBeside(x, y, z, -1, 0, 0) &&
                    IsNormalCubeBeside(x, y, z, 0, 1, 0) &&
                    IsNormalCubeBeside(x, y, z, 0, -1, 0) &&
                    IsNormalCubeBeside(x, y, z, 0, 0, 1) &&
                    IsNormalCubeBeside(x, y, z, 0, 0, -1)
                    )
                {
                    //TODO add to another array?
                    return;
                }

                //add
                collisionList.Add(new Box
                {
                    center = new SharpDX.Vector3(x, y, z),
                    halfSize = new SharpDX.Vector3(0.5f, 0.5f, 0.5f),
                });
            }
        }

        public void Init(World init, WorldCoord pos)
        {
            this.world = init;
            this.w = init.ChunkWidth;
            this.h = init.ChunkHeight;
            int blockSize = h * w * w;
            
            blocks = new BlockData[blockSize];
            dirty = new bool[blockSize];
            basePosition = new Vector4(pos.x, pos.y, pos.z, 0);
            baseCoord = pos;

            base.CollisionArray = new Box[0];
            base.CollisionCount = 0;
            base.Position = new SharpDX.Vector3(pos.x, pos.y, pos.z);
            base.CollisionSegments = new int[h / init.ChunkLayerHeight + 1];

            init.OnNewChunk(pos, this);
        }

        public GridStaticEntity GetStaticEntity()
        {
            return this;
        }

        private List<BlockRenderData> renderList = new List<BlockRenderData>();
        private List<Box> collisionList;

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

        private bool MakeAOInner(int x, int y, int z, int dx, int dy, int dz)
        {
            bool ret = false;
            if (dx == 0)
            {
                ret = GetBlockDataAt(x, y + dy, z + dz).BlockId != 0 ||
                    GetBlockDataAt(x, y, z + dz).BlockId != 0 ||
                    GetBlockDataAt(x, y + dy, z).BlockId != 0;
            }
            else if (dy == 0)
            {
                ret = GetBlockDataAt(x + dx, y, z + dz).BlockId != 0 ||
                    GetBlockDataAt(x, y, z + dz).BlockId != 0 ||
                    GetBlockDataAt(x + dx, y, z).BlockId != 0;
            }
            else if (dz == 0)
            {
                ret = GetBlockDataAt(x + dx, y + dy, z).BlockId != 0 ||
                    GetBlockDataAt(x, y + dy, z).BlockId != 0 ||
                    GetBlockDataAt(x + dx, y, z).BlockId != 0;
            }
            return ret;
        }

        private Vector4 GetAOOffset(int x, int y, int z, int face)
        {
            switch (face)
            {
                case 5:
                    return AmbientOcculsionTexture.MakeAOOffset(
                        MakeAOInner(x + 1, y, z, 0, 1, 1),
                        MakeAOInner(x + 1, y, z, 0, -1, 1),
                        MakeAOInner(x + 1, y, z, 0, 1, -1),
                        MakeAOInner(x + 1, y, z, 0, -1, -1));
                case 4:
                    return AmbientOcculsionTexture.MakeAOOffset(
                        MakeAOInner(x - 1, y, z, 0, 1, 1),
                        MakeAOInner(x - 1, y, z, 0, 1, -1),
                        MakeAOInner(x - 1, y, z, 0, -1, 1),
                        MakeAOInner(x - 1, y, z, 0, -1, -1));
                case 2:
                    return AmbientOcculsionTexture.MakeAOOffset(
                        MakeAOInner(x, y + 1, z, 1, 0, 1),
                        MakeAOInner(x, y + 1, z, 1, 0, -1),
                        MakeAOInner(x, y + 1, z, -1, 0, 1),
                        MakeAOInner(x, y + 1, z, -1, 0, -1));
                case 3:
                    return AmbientOcculsionTexture.MakeAOOffset(
                        MakeAOInner(x, y - 1, z, 1, 0, 1),
                        MakeAOInner(x, y - 1, z, -1, 0, 1),
                        MakeAOInner(x, y - 1, z, 1, 0, -1),
                        MakeAOInner(x, y - 1, z, -1, 0, -1));
                case 1:
                    return AmbientOcculsionTexture.MakeAOOffset(
                        MakeAOInner(x, y, z + 1, 1, 1, 0),
                        MakeAOInner(x, y, z + 1, -1, 1, 0),
                        MakeAOInner(x, y, z + 1, 1, -1, 0),
                        MakeAOInner(x, y, z + 1, -1, -1, 0));
                case 0:
                    return AmbientOcculsionTexture.MakeAOOffset(
                        MakeAOInner(x, y, z - 1, 1, 1, 0),
                        MakeAOInner(x, y, z - 1, 1, -1, 0),
                        MakeAOInner(x, y, z - 1, -1, 1, 0),
                        MakeAOInner(x, y, z - 1, -1, -1, 0));
            }
            return new Vector4();
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
                aooffset = GetAOOffset(x, y, z, 0),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, 0, 1)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y + 0.0f, z + 0.5f, 1.0f),
                col = new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                dir_u = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                dir_v = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 1),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, 1, 0)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y + 0.5f, z + 0.0f, 1.0f),
                col = new Vector4(0.0f, 0.0f, 1.0f, 1.0f),
                dir_u = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                dir_v = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 2),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, -1, 0)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y - 0.5f, z + 0.0f, 1.0f),
                col = new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                dir_u = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                dir_v = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 3),
            });
            if (!IsNormalCubeBeside(x, y, z, -1, 0, 0)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x - 0.5f, y + 0.0f, z + 0.0f, 1.0f),
                col = new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                dir_u = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                dir_v = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 4),
            });
            if (!IsNormalCubeBeside(x, y, z, 1, 0, 0)) renderList.Add(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.5f, y + 0.0f, z + 0.0f, 1.0f),
                col = new Vector4(0.0f, 1.0f, 1.0f, 1.0f),
                dir_u = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                dir_v = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 5),
            });
        }

        public void Flush()
        {
            collisionList = world.collisionBuffer;
            collisionList.Clear();
            renderList.Clear();
            int gridId = 0;

            int nextLayerId = 1;
            int nextLayerStartZ = world.ChunkLayerHeight;
            base.CollisionSegments[0] = 0;

            for (int gridZ = 0; gridZ < h; ++gridZ)
            {
                if (gridZ >= nextLayerStartZ)
                {
                    base.CollisionSegments[nextLayerId++] = collisionList.Count;
                    nextLayerStartZ += world.ChunkLayerHeight;
                }

                for (int gridY = 0; gridY < w; ++gridY)
                {
                    for (int gridX = 0; gridX < w; ++gridX)
                    {
                        AppendBlockCollision(gridX, gridY, gridZ);
                        AppendBlockRenderData(gridX, gridY, gridZ, blocks[gridId]);

                        ++gridId;
                    }
                }
            }
            base.CollisionSegments[nextLayerId++] = collisionList.Count;

            CollisionCount = collisionList.Count;
            CollisionArray = collisionList.ToArray();
        }

        public List<BlockRenderData> RenderDataList
        {
            get
            {
                return renderList;
            }
        }
    }
}
