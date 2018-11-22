using LightDx.InputAttributes;
using Sandbox.Graphics;
using Sandbox.Physics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    public struct BlockRenderData
    {
        [Position]
        public Vector4 pos;
        [TexCoord(1)]
        public float dir_uv_index;
        [Color(0)]
        public uint col;
        [TexCoord(2)]
        public Vector4 aooffset;
        [Color(1)]
        public Vector4 lightness;
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
            return x + y * w + GetLayerOffset(z);
        }

        public int GetOrCreateBlockIndex(int x, int y, int z)
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
                var coord = new WorldCoord(x, y, z);
                if (
                    IsNormalCubeBeside(coord, new WorldCoord.Direction1(0)) &&
                    IsNormalCubeBeside(coord, new WorldCoord.Direction1(1)) &&
                    IsNormalCubeBeside(coord, new WorldCoord.Direction1(2)) &&
                    IsNormalCubeBeside(coord, new WorldCoord.Direction1(3)) &&
                    IsNormalCubeBeside(coord, new WorldCoord.Direction1(4)) &&
                    IsNormalCubeBeside(coord, new WorldCoord.Direction1(5))
                    )
                {
                    //TODO add to another array?
                    return;
                }

                //add
                collisionList.Add(new Box
                {
                    center = new Vector3(x, y, z),
                    halfSize = new Vector3(0.5f, 0.5f, 0.5f),
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
            base.Position = new Vector3(pos.x, pos.y, pos.z);
            base.CollisionSegments = new int[h / World.ChunkLayerHeight + 1];

            init.OnNewChunk(pos, this);
        }

        public GridStaticEntity GetStaticEntity()
        {
            return this;
        }

        private List<Box> collisionList;

        private BlockData GetBlockDataAt(WorldCoord coord)
        {
            if (coord.x >= 0 && coord.x < w && coord.y >= 0 && coord.y < w && coord.z >= 0 && coord.z < h)
            {
                return GetBlock(coord.x, coord.y, coord.z);
            }
            return world.GetBlock(coord.x + baseCoord.x, coord.y + baseCoord.y, coord.z + baseCoord.z);
        }

        private bool MakeAOInner(WorldCoord coord, WorldCoord.Direction2 dir)
        {
            return GetBlockDataAt(coord.WithOffset(dir.coord)).BlockId != 0 ||
                GetBlockDataAt(coord.WithOffset(dir.Devide(0).coord)).BlockId != 0 ||
                GetBlockDataAt(coord.WithOffset(dir.Devide(1).coord)).BlockId != 0;
        }

        private Vector4 GetAOOffset(WorldCoord coord, int face)
        {
            WorldCoord.Direction1 dir = new WorldCoord.Direction1(face);
            var offsetCoord = coord.WithOffset(dir.coord);
            return AmbientOcclusionTexture.MakeAOOffset(
                MakeAOInner(offsetCoord, dir.UVPN(0)),
                MakeAOInner(offsetCoord, dir.UVPN(1)),
                MakeAOInner(offsetCoord, dir.UVPN(2)),
                MakeAOInner(offsetCoord, dir.UVPN(3)));
        }

        private bool IsNormalCubeBeside(WorldCoord coord, WorldCoord.Direction1 offset)
        {
            return GetBlockDataAt(coord.WithOffset(offset.coord)).BlockId != 0;
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

        //Function not used
        private Vector4 GetColorFromInt(int color)
        {
            int x = color & 255;
            int y = color >> 8 & 255;
            int z = color >> 16 & 255;
            return new Vector4(z / 255.0f, y / 255.0f, x / 255.0f, 1.0f);
        }

        private float[] sunlightOnFace = new float[] { 0.60f, 0.52f, 0.42f, 0.40f, 1.0f, 0.3f, 0.39f };

        private float LightnessByteToFloat(int face, byte b)
        {
            //return sunlightOnFace[face] * 0.8f;
            if (b == 0) return 0.0f;
            float ret = b / 16.0f;
            if (b == 14)
            {
                ret *= sunlightOnFace[face];
            }
            else
            {
                ret *= sunlightOnFace[6];
            }
            return ret;
        }

        private float MakeLightnessForVertex(int face, byte ba, byte bb, byte bc, byte bd)
        {
            float a = LightnessByteToFloat(face, ba), b = LightnessByteToFloat(face, bb),
                c = LightnessByteToFloat(face, bc), d = LightnessByteToFloat(face, bd);
            float ret;
            if (b == 0.0f && c == 0.0f)
            {
                ret = a;
            }
            else
            {
                int count = 1;
                float sum = a;
                if (b != 0) { ++count; sum += b; }
                if (c != 0) { ++count; sum += c; }
                if (d != 0) { ++count; sum += d; }
                ret = sum / count;
            }
            return ret * 1.2f;
        }

        private byte GetLightnessOnFace(WorldCoord coord, int face)
        {
            var block = GetBlockDataAt(coord);
            switch (face)
            {
                case 0:
                    return (byte)(block.LightnessXP);
                case 1:
                    return (byte)(block.LightnessXN);
                case 2:
                    return (byte)(block.LightnessYP);
                case 3:
                    return (byte)(block.LightnessYN);
                case 4:
                    return (byte)(block.LightnessZP);
                case 5:
                    return (byte)(block.LightnessZN);
            }
            return 0;
        }

        private float GetLightnessForVertex(WorldCoord coord, int face, int index)
        {
            WorldCoord.Direction2 vertex = new WorldCoord.Direction1(face).UVPN(index);
            return MakeLightnessForVertex(face,
                GetLightnessOnFace(coord, face),
                GetLightnessOnFace(coord.WithOffset(vertex.Devide(0).coord), face),
                GetLightnessOnFace(coord.WithOffset(vertex.Devide(1).coord), face),
                GetLightnessOnFace(coord.WithOffset(vertex.coord), face)
                );
        }

        private Vector4 GetLightnessVec(WorldCoord coord, int face)
        {
            return new Vector4(
                GetLightnessForVertex(coord, face, 0),
                GetLightnessForVertex(coord, face, 1),
                GetLightnessForVertex(coord, face, 2),
                GetLightnessForVertex(coord, face, 3)
                );
        }

        private void AppendBlockRenderData(IRenderBuffer<BlockRenderData> buffer, int x, int y, int z)
        {
            BlockData data = GetBlock(x, y, z);
            if (data.BlockId == 0) return;

            WorldCoord coord = new WorldCoord(x, y, z);
            for (int face = 0; face < 6; ++face)
            {
                var faceDir = new WorldCoord.Direction1(face);
                if (!IsNormalCubeBeside(coord, faceDir))
                {
                    buffer.Append(new BlockRenderData
                    {
                        pos = basePosition + coord.ToVector4(1.0f) + faceDir.coord.ToVector4(0) * 0.5f,
                        col = data.BlockColor,
                        dir_uv_index = face,
                        aooffset = GetAOOffset(coord, face),
                        lightness = GetLightnessVec(coord, face),
                    });
                }
            }
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
