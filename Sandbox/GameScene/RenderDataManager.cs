using LightDx;
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

    class RenderDataManager : IDisposable
    {
        private World world;
        private VertexDataProcessor<BlockRenderData> pipelineInput;

        private List<VertexBuffer> renderDataList = new List<VertexBuffer>();
        private RenderBuffer myRenderBuffer;

        private const int RenderDataMaxLength = 16384;

        private BlockRenderData[] arrayBuffer;
        private int lastRenderDataLength;
        
        private class RenderBuffer : IRenderBuffer<BlockRenderData>
        {
            private RenderDataManager manager;
            
            public RenderBuffer(RenderDataManager manager)
            {
                this.manager = manager;
            }

            public void Append(BlockRenderData val)
            {
                if (manager.lastRenderDataLength >= RenderDataMaxLength)
                {
                    //create last renderdata
                    var rd = manager.pipelineInput.CreateImmutableBuffer(manager.arrayBuffer);
                    manager.renderDataList.Add(rd);
                    manager.lastRenderDataLength = 0;
                }
                manager.arrayBuffer[manager.lastRenderDataLength++] = val;
            }
        }

        public RenderDataManager(World world, VertexDataProcessor<BlockRenderData> pipelineInput)
        {
            this.world = world;
            this.pipelineInput = pipelineInput;
            this.arrayBuffer = new BlockRenderData[RenderDataMaxLength];
            this.myRenderBuffer = new RenderBuffer(this);
        }

        public void Flush()
        {
            ClearAllRenderData();

            foreach (var chunk in world.chunkList)
            {
                chunk.FlushRenderData(myRenderBuffer);
            }
            //create the last render data object
            var rd = pipelineInput.CreateImmutableBuffer(arrayBuffer, 0, lastRenderDataLength);
            renderDataList.Add(rd);
        }

        private void ClearAllRenderData()
        {
            foreach (var rd in renderDataList)
            {
                rd.Dispose();
            }
            renderDataList.Clear();
            lastRenderDataLength = 0;
        }

        public void Dispose()
        {
            ClearAllRenderData();
        }

        public void Render()
        {
            foreach (var rd in renderDataList)
            {
                rd.DrawAll();
            }
        }
    }
}
