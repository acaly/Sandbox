using LightDx;
using Sandbox.GameScene;
using Sandbox.Graphics;
using Sandbox.Gui;
using Sandbox.Terrain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Sandbox.GameScene.WorldCoord;

/* Speed test (device setup, draw the cube)
loop: 68.2

api: 56.8
  event 1.9
  clear 5.8+2.0
  present 24.2
  draw 22.9

other: 11.4
 */

namespace Sandbox
{
    public struct VertexConstData
    {
        public Matrix4x4 transform;
    }

    public struct DirVectorList
    {
        public Vector4 u0, u1, u2, u3, u4, u5, u6, u7;
        public Vector4 v0, v1, v2, v3, v4, v5, v6, v7;
    }

    static class Program
    {
        public static Form MainForm;

        [STAThread]
        static void Main()
        {
            List<long> times = new List<long>(20);
            Stopwatch clock = new Stopwatch();
            clock.Start();

            var form = new Form();
            form.ClientSize = new Size(800, 600);

            using (LightDevice device = LightDevice.Create(form))
            {
                times.Add(clock.ElapsedMilliseconds);

                var target0 = device.GetDefaultTarget();
                target0.ClearColor = Color.AntiqueWhite.WithAlpha(1);
                var target1 = device.CreateDepthStencilTarget();

                var target = new RenderTargetList(target0, target1);
                target.Apply();

                //var shaderFace = Shader<VertexConstData>.CreateFromString(device, BlockFaceShader.Value);
                var pipeline = device.CompilePipeline(InputTopology.Point,
                    ShaderSource.FromString(BlockFaceShader.Value,
                        ShaderType.Vertex | ShaderType.Geometry | ShaderType.Pixel));
                var pipelineInput = pipeline.CreateVertexDataProcessor<BlockRenderData>();
                var pipelineConstant = pipeline.CreateConstantBuffer<VertexConstData>();
                pipeline.SetConstant(ShaderType.Vertex, 0, pipelineConstant);

                var proj = device.CreatePerspectiveFieldOfView((float)Math.PI / 4).Transpose();

                {
                    var pipelineConstantDir = pipeline.CreateConstantBuffer<DirVectorList>();
                    pipelineConstantDir.Value.u0 = new Direction1(0).U().coord.ToVector4(0);
                    pipelineConstantDir.Value.u1 = new Direction1(1).U().coord.ToVector4(0);
                    pipelineConstantDir.Value.u2 = new Direction1(2).U().coord.ToVector4(0);
                    pipelineConstantDir.Value.u3 = new Direction1(3).U().coord.ToVector4(0);
                    pipelineConstantDir.Value.u4 = new Direction1(4).U().coord.ToVector4(0);
                    pipelineConstantDir.Value.u5 = new Direction1(5).U().coord.ToVector4(0);
                    pipelineConstantDir.Value.v0 = new Direction1(0).V().coord.ToVector4(0);
                    pipelineConstantDir.Value.v1 = new Direction1(1).V().coord.ToVector4(0);
                    pipelineConstantDir.Value.v2 = new Direction1(2).V().coord.ToVector4(0);
                    pipelineConstantDir.Value.v3 = new Direction1(3).V().coord.ToVector4(0);
                    pipelineConstantDir.Value.v4 = new Direction1(4).V().coord.ToVector4(0);
                    pipelineConstantDir.Value.v5 = new Direction1(5).V().coord.ToVector4(0);
                    pipeline.SetConstant(ShaderType.Vertex, 1, pipelineConstantDir);
                    pipelineConstantDir.Update();
                }

#warning support sampler
                //shaderFace.CreateSamplerForPixelShader(0, new SamplerStateDescription
                //{
                //    Filter = Filter.MinMagMipLinear,
                //    AddressU = TextureAddressMode.Border,
                //    AddressV = TextureAddressMode.Border,
                //    AddressW = TextureAddressMode.Border,
                //});

                Texture2D aotexture;
                using (var aobitmap = AmbientOcclusionTexture.CreateBitmap())
                {
                    aotexture = device.CreateTexture2D(aobitmap);
                    pipeline.SetResource(0, aotexture);
                }
                
                times.Add(clock.ElapsedMilliseconds);

                World theWorld = new World(device);
                Camera camera = new Camera(new Vector3(0, 0, 50));
                var gameSceneGui = new GameSceneGui(device, camera);
                RenderDataManager rdm = new RenderDataManager(theWorld, pipelineInput);

                MainForm = form;

                times.Add(clock.ElapsedMilliseconds);

                theWorld.SetBlock(-100, -100, 0, new BlockData { BlockId = 1, BlockColor = 0 });
                theWorld.SetBlock(100, -100, 0, new BlockData { BlockId = 1, BlockColor = 0 });
                theWorld.SetBlock(-100, 100, 0, new BlockData { BlockId = 1, BlockColor = 0 });
                theWorld.SetBlock(100, 100, 0, new BlockData { BlockId = 1, BlockColor = 0 });
                //{
                //NatsuTerrain.CreateWorld(theWorld, @"blocks.bin", 5);
                NatsuTerrain.CreateWorld(theWorld, @"E:\1.schematic.natsu", 5);
                //NatsuTerrain.CreateWorld(theWorld, @"E:\1.schematic.natsu", 5);
                //}
                //{
                //    AscTerrain terr = new AscTerrain(@"blocks_asc.asc", 1000, 1000);
                //    terr.Resample(4);
                //    terr.CreateWorld(theWorld, 000, 000, 250, 250);
                //}
                if (true)
                {
                    for (int x = -8; x <= 8; ++x)
                    {
                        for (int y = -8; y <= 8; ++y)
                        {
                            theWorld.SetBlock(x, y, 0, new BlockData { BlockId = 1, BlockColor = 16777215 });
                            if (true && x >= -2 && x <= 2 && y >= -2 && y <= 2)
                            {
                                //for (int z = 0; z < 4; ++z)
                                theWorld.SetBlock(x, y, 1, new BlockData { BlockId = 1, BlockColor = 16777215 });
                            }
                            //if (x == -20 || x == 20 || y == -20 || y == 20)
                            //{
                            //    theWorld.SetBlock(x, y, 1, new BlockData { BlockId = 1 });
                            //}
                            //if (Math.Abs(x) < 5 && Math.Abs(y) < 5)
                            //{
                            //    theWorld.SetBlock(x, y, 7, new BlockData { BlockId = 1 });
                            //}
                            //if (Math.Abs(x) == 20 && Math.Abs(y) == 0)
                            //{
                            //    theWorld.SetBlock(x, y, 2, new BlockData { BlockId = 1 });
                            //    theWorld.SetBlock(x, y, 3, new BlockData { BlockId = 1 });
                            //    theWorld.SetBlock(x, y, 4, new BlockData { BlockId = 1 });
                            //    theWorld.SetBlock(x, y, 5, new BlockData { BlockId = 1 });
                            //}
                        }
                    }
                }

                times.Add(clock.ElapsedMilliseconds);

                LightingManager lighting = new LightingManager(theWorld, 0, 0);

                times.Add(clock.ElapsedMilliseconds);

                foreach (var chunk in theWorld.chunkList)
                {
                    chunk.FlushCollision();
                }
                rdm.Flush();

                times.Add(clock.ElapsedMilliseconds);

                camera.SetForm(form);
                theWorld.AddEntity(camera);
                
                EventWaitHandle physicsStartEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
                EventWaitHandle physicsFinishEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

                times.Add(clock.ElapsedMilliseconds);

                //Thread physicsThread = new Thread(new ThreadStart(delegate
                //{
                //    while (true)
                //    {
                //        physicsStartEvent.WaitOne();
                //        theWorld.StepPhysics(1.0f / 60);
                //        physicsFinishEvent.Set();
                //    }
                //}));
                //physicsThread.Start();

                //GC.Collect();

                //rm.ImmediateContext.SetRenderData(rdm.renderData);

                ///////////////////////////////////////////////
                ///

                form.Show();

                device.RunLoop(delegate ()
                {
                    target.ClearAll();

                    //--- logic---

                    camera.Step(); //can't be paralleled with physics

                    //physicsStartEvent.Set();
                    theWorld.StepPhysics(1.0f / 60);

                    //--- render world ---
                    pipeline.Apply();
                    
                    pipelineConstant.Value.transform = proj * camera.GetViewMatrix();
                    pipelineConstant.Update();

                    rdm.Render();

                    //--- render gui ---
                    
                    gameSceneGui.Render();

                    //--- present ---

                    device.Present(true);

                    //physicsFinishEvent.WaitOne();
                });

                //physicsThread.Abort();
            }
        }
    }
}
