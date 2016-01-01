using Sandbox.GameScene;
using Sandbox.Graphics;
using Sandbox.Terrain;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Format = SharpDX.DXGI.Format;

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
        public Matrix transform;
    }
    
    static class Program
    {
        public static Form MainForm;

        [STAThread]
        static void Main()
        {
            using (RenderManager rm = new RenderManager())
            {
                rm.InitDevice();

                GameScene.World theWorld = new GameScene.World(rm);
                GameScene.Camera camera = new GameScene.Camera(new Vector3(0, 0, 70));
                RenderDataManager rdm = new RenderDataManager(theWorld);

                MainForm = rm.Form;

                //{
                NatsuTerrain.CreateWorld(theWorld, @"blocks.natsu", 5);
                //NatsuTerrain.CreateWorld(theWorld, @"E:\1.schematic.natsu", 5);
                //NatsuTerrain.CreateWorld(theWorld, @"E:\2.schematic.natsu", 5);
                //}
                //{
                //    AscTerrain terr = new AscTerrain(@"blocks_asc.asc", 1000, 1000);
                //    terr.Resample(4);
                //    terr.CreateWorld(theWorld, 000, 000, 250, 250);
                //}
                if (false)
                {
                    for (int x = -2; x <= 2; ++x)
                    {
                        for (int y = -2; y <= 2; ++y)
                        {
                            theWorld.SetBlock(x, y, x + 2, new BlockData { BlockId = 1 });
                            if (false && x == 0 && y == 0)
                            {
                                for (int z = 0; z < 4; ++z)
                                    theWorld.SetBlock(x, y, z, new BlockData { BlockId = 1 });
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

                LightingManager lighting = new LightingManager(theWorld, 0, 0);
                //return;

                var shaderFace = Shader<VertexConstData>.CreateFromString(rm, BlockFaceShader.Value);
                shaderFace.CreateSamplerForPixelShader(0, new SamplerStateDescription
                {
                    Filter = Filter.MinMagMipLinear,
                    AddressU = TextureAddressMode.Border,
                    AddressV = TextureAddressMode.Border,
                    AddressW = TextureAddressMode.Border,
                });
                var aotexture = new AmbientOcculsionTexture(rm);

                foreach (var chunk in theWorld.chunkList)
                {
                    chunk.FlushCollision();
                }
                rdm.SetLayoutFromShader(shaderFace);
                rdm.Flush();
                shaderFace.SetResourceForPixelShader(0, aotexture.ResourceView);

                camera.SetForm(rm.Form);
                theWorld.AddEntity(camera);

                var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, 800.0f / 600.0f, 0.1f, 1000.0f);

                rm.ImmediateContext.ApplyShader(shaderFace);

                EventWaitHandle physicsStartEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
                EventWaitHandle physicsFinishEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

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
                RenderLoopHelper.Run(rm, false, delegate(RenderContext frame)
                {
                    camera.Step(); //can't be paralleled with physics

                    //physicsStartEvent.Set();
                    theWorld.StepPhysics(1.0f / 60);

                    shaderFace.buffer.transform = Matrix.Multiply(camera.GetViewMatrix(), proj);
                    shaderFace.buffer.transform.Transpose();
                    frame.UpdateShaderConstant(shaderFace);

                    rdm.Render(frame);

                    frame.Present(true);

                    //physicsFinishEvent.WaitOne();
                });
                //physicsThread.Abort();
            }
        }
    }
}
