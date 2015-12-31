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

        private int w, h;
        private Vector4 basePosition;
        private WorldCoord baseCoord;

        //blocks array no longer store the block data in consequence
        //use GetBlock/SetBlock to access it.
        //store data in layer
        private BlockData[] blocks;
        //offset in blocks array of a layer
        private int[] layerOffset;
        //free offsets
        private int[] freeLayerOffset;
        private int layerCapacity;
        //number of non-empty layers + 1
        private int layerCount;


        // block access part

        public BlockData GetBlock(int x, int y, int z)
        {
            //old method
            //return blocks[x + y * w + z * w * w];
            //new method
            return blocks[x + y * w + GetLayerOffset(z)];
        }

        public void SetBlock(int x, int y, int z, BlockData b)
        {
            //old method
            //blocks[x + y * w + z * w * w] = b;
            //new method
            var index = x + y * w + GetOrCreateLayerOffset(z);
            blocks[index] = b;
        }

        public BlockData RawGetBlock(int index)
        {
            return blocks[index];
        }

        public void RawSetBlock(int index, BlockData b)
        {
            blocks[index] = b;
        }

        public int GetBlockIndex(int x, int y, int z)
        {
            return x + y * w + GetOrCreateLayerOffset(z);
        }

        private int GetLayerOffset(int z)
        {
            return layerOffset[z];
        }

        private int GetOrCreateLayerOffset(int z)
        {
            var ret = layerOffset[z];
            if (ret == 0)
            {
                if (layerCount >= layerCapacity)
                {
                    //create new array
                    var newLayerCapacity = layerCapacity * 2;
                    var newBlockArray = new BlockData[newLayerCapacity * w * w];
                    Array.Copy(blocks, newBlockArray, blocks.Length);
                    //push new free offsets
                    for (int i = 0; i < newLayerCapacity - layerCapacity; ++i)
                    {
                        var theOffset = (layerCapacity + i) * w * w;
                        freeLayerOffset[i] = theOffset;
                        //clear
                        for (int j = 0; j < w * w; ++j)
                        {
                            newBlockArray[theOffset + j] = new BlockData();
                        }
                    }
                    //update fields
                    blocks = newBlockArray;
                    layerCapacity = newLayerCapacity;
                }
                ret = freeLayerOffset[layerCapacity - layerCount - 1];
                layerOffset[z] = ret;
                layerCount++;
            }
            return ret;
        }

        private void InitBlocksArray()
        {
            //old method
            //blocks = new BlockData[h * w * w];

            //new method
            layerCapacity = 10;
            layerCount = 1; //reserve 0 for empty layer
            blocks = new BlockData[layerCapacity * w * w];
            layerOffset = new int[h + 1]; //here we add 1 to allow overflow in World.BlockIterator
            freeLayerOffset = new int[h];
            for (int i = 0; i < layerCapacity - 1; ++i)
            {
                freeLayerOffset[i] = (i + 1) * w * w;
            }
        }

        //end of block access part

        private void AppendBlockCollision(int x, int y, int z)
        {
            bool hasCollision = GetBlock(x, y, z).HasCollision();

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
            this.w = World.ChunkWidth;
            this.h = World.ChunkHeight;

            InitBlocksArray();
            basePosition = new Vector4(pos.x, pos.y, pos.z, 0);
            baseCoord = pos;

            base.CollisionArray = new Box[0];
            base.CollisionCount = 0;
            base.Position = new SharpDX.Vector3(pos.x, pos.y, pos.z);
            base.CollisionSegments = new int[h / World.ChunkLayerHeight + 1];

            init.OnNewChunk(pos, this);
        }

        public GridStaticEntity GetStaticEntity()
        {
            return this;
        }

        private List<Box> collisionList;

        private BlockData GetBlockDataAt(int x, int y, int z)
        {
            if (x >= 0 && x < w && y >= 0 && y < w && z >= 0 && z < h)
            {
                return GetBlock(x, y, z);
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

        private Vector4 GetColorForLightness(int l)
        {
            if (l == 13)
            {
                //TODO really check if it's sun light
                return new Vector4(l / (float)16, 0, 0, 0);
            }
            else
            {
                return new Vector4(0, l / (float)16, 0, 0);
            }
        }

        private void AppendBlockRenderData(IRenderBuffer<BlockRenderData> buffer, int x, int y, int z)
        {
            BlockData data = GetBlock(x, y, z);
            if (data.BlockId == 0) return;

            if (!IsNormalCubeBeside(x, y, z, 0, 0, -1)) buffer.Append(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y + 0.0f, z - 0.5f, 1.0f),
                col = GetColorForLightness(data.LightnessZN),
                dir_u = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                dir_v = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 0),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, 0, 1)) buffer.Append(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y + 0.0f, z + 0.5f, 1.0f),
                col = GetColorForLightness(data.LightnessZP),
                dir_u = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                dir_v = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 1),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, 1, 0)) buffer.Append(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y + 0.5f, z + 0.0f, 1.0f),
                col = GetColorForLightness(data.LightnessYP),
                dir_u = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                dir_v = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 2),
            });
            if (!IsNormalCubeBeside(x, y, z, 0, -1, 0)) buffer.Append(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.0f, y - 0.5f, z + 0.0f, 1.0f),
                col = GetColorForLightness(data.LightnessYN),
                dir_u = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                dir_v = new Vector4(0.5f, 0.0f, 0.0f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 3),
            });
            if (!IsNormalCubeBeside(x, y, z, -1, 0, 0)) buffer.Append(new BlockRenderData
            {
                pos = basePosition + new Vector4(x - 0.5f, y + 0.0f, z + 0.0f, 1.0f),
                col = GetColorForLightness(data.LightnessXN),
                dir_u = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                dir_v = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 4),
            });
            if (!IsNormalCubeBeside(x, y, z, 1, 0, 0)) buffer.Append(new BlockRenderData
            {
                pos = basePosition + new Vector4(x + 0.5f, y + 0.0f, z + 0.0f, 1.0f),
                col = GetColorForLightness(data.LightnessXP),
                dir_u = new Vector4(0.0f, 0.0f, 0.5f, 0.0f),
                dir_v = new Vector4(0.0f, 0.5f, 0.0f, 0.0f),
                aooffset = GetAOOffset(x, y, z, 5),
            });
        }

        public void FlushCollision()
        {
            collisionList = world.collisionBuffer;
            collisionList.Clear();

            int nextLayerId = 1;
            int nextLayerStartZ = World.ChunkLayerHeight;
            base.CollisionSegments[0] = 0;

            for (int gridZ = 0; gridZ < h; ++gridZ)
            {
                if (gridZ >= nextLayerStartZ)
                {
                    base.CollisionSegments[nextLayerId++] = collisionList.Count;
                    nextLayerStartZ += World.ChunkLayerHeight;
                }

                for (int gridY = 0; gridY < w; ++gridY)
                {
                    for (int gridX = 0; gridX < w; ++gridX)
                    {
                        AppendBlockCollision(gridX, gridY, gridZ);
                    }
                }
            }
            base.CollisionSegments[nextLayerId++] = collisionList.Count;

            CollisionCount = collisionList.Count;
            CollisionArray = collisionList.ToArray();
        }

        public void FlushRenderData(IRenderBuffer<BlockRenderData> buffer)
        {
            for (int gridZ = 0; gridZ < h; ++gridZ)
            {
                for (int gridY = 0; gridY < w; ++gridY)
                {
                    for (int gridX = 0; gridX < w; ++gridX)
                    {
                        AppendBlockRenderData(buffer, gridX, gridY, gridZ);
                    }
                }
            }
        }
    }
}
