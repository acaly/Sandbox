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

        private List<RenderData<BlockRenderData>> renderDataList = new List<RenderData<BlockRenderData>>();
        private RenderBuffer myRenderBuffer;

        private const int RenderDataMaxLength = 16384;

        private BlockRenderData[] arrayBuffer;
        private int lastRenderDataLength;

        private Action<RenderData<BlockRenderData>> setupRenderData;

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
                    var rd = RenderData<BlockRenderData>.Create(manager.world.renderManager,
                        SharpDX.Direct3D.PrimitiveTopology.PointList, manager.arrayBuffer);
                    if (manager.setupRenderData != null)
                    {
                        manager.setupRenderData(rd);
                    }
                    manager.renderDataList.Add(rd);
                    manager.lastRenderDataLength = 0;
                }
                manager.arrayBuffer[manager.lastRenderDataLength++] = val;
            }
        }

        public RenderDataManager(World world)
        {
            this.world = world;
            this.arrayBuffer = new BlockRenderData[RenderDataMaxLength];
            this.myRenderBuffer = new RenderBuffer(this);
        }

        public void SetLayoutFromShader<T>(Shader<T> shader) where T : struct
        {
            setupRenderData = delegate(RenderData<BlockRenderData> rd)
            {
                rd.SetLayoutFromShader(shader);
            };
        }

        public void Flush()
        {
            ClearAllRenderData();

            foreach (var chunk in world.chunkList)
            {
                chunk.FlushRenderData(myRenderBuffer);
            }
            //create the last render data object
            var rd = RenderData<BlockRenderData>.Create(world.renderManager, SharpDX.Direct3D.PrimitiveTopology.PointList,
                arrayBuffer, lastRenderDataLength);
            if (setupRenderData != null)
            {
                setupRenderData(rd);
            }
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

        public void Render(RenderContext frame)
        {
            foreach (var rd in renderDataList)
            {
                frame.SetRenderData(rd);
                frame.Draw(rd);
            }
        }
    }
}
