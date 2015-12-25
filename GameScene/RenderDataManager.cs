using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    class ArrayBuffer<T> where T : struct
    {
        public T[] Data;
        public int Count;

        public ArrayBuffer()
        {
            Data = new T[1024];
            Count = 0;
        }

        private void EnsureCapacity(int increase)
        {
            if (Count + increase > Data.Length)
            {
                int newSize = Data.Length * 2;
                while (newSize < Count + increase)
                {
                    newSize *= 2;
                }
                var newData = new T[newSize];
                Array.Copy(Data, newData, Count);
                Data = newData;
            }
        }

        public void AddRange(List<T> source)
        {
            EnsureCapacity(source.Count);
            foreach (var data in source)
            {
                Data[Count++] = data;
            }
        }

        public void Clear()
        {
            Count = 0;
        }
    }

    class RenderDataManager
    {
        private World world;
        public RenderData<BlockRenderData> renderData
        {
            get;
            private set;
        }
        private ArrayBuffer<BlockRenderData> renderBuffer = new ArrayBuffer<BlockRenderData>();

        public RenderDataManager(World world)
        {
            this.world = world;
            this.renderData = RenderData<BlockRenderData>.Create(world.renderManager, 
                SharpDX.Direct3D.PrimitiveTopology.PointList, new BlockRenderData[0]);
        }

        public void Flush()
        {
            renderBuffer.Clear();
            foreach (var chunk in world.chunkList)
            {
                renderBuffer.AddRange(chunk.RenderDataList);
            }
            if (renderData != null)
            {
                renderData.Dispose();
            }
            renderData = RenderData<BlockRenderData>.Create(world.renderManager,
                SharpDX.Direct3D.PrimitiveTopology.PointList,
                renderBuffer.Data, renderBuffer.Count);
        }
    }
}
