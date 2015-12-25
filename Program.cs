using Sandbox.GameScene;
using Sandbox.Graphics;
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
        [STAThread]
        static void Main()
        {
            using (RenderManager rm = new RenderManager())
            {
                rm.InitDevice();

                GameScene.World theWorld = new GameScene.World(rm);
                GameScene.Camera camera = new GameScene.Camera(new Vector3(0, 0, 70));
                RenderDataManager rdm = new RenderDataManager(theWorld);

                {
                    byte[] blockdata;
                    using (FileStream fs = File.OpenRead(@"blocks.bin"))
                    {
                        blockdata = new byte[fs.Length];
                        fs.Read(blockdata, 0, blockdata.Length);
                    }
                    int blockdataindex = 0;
                    for (int i = -50; i <= 50; ++i)
                    {
                        for (int j = -50; j < 50; ++j)
                        {
                            for (int k = -50; k < 50; ++k)
                            {
                                if (blockdata[blockdataindex++] != 0)
                                {
                                    theWorld.SetBlock(i, k, j + 55, new BlockData { BlockId = 1 });
                                }
                            }
                        }
                    }
                }
                //for (int x = -4; x <= 4; ++x)
                //{
                //    for (int y = -4; y <= 4; ++y)
                //    {
                //        theWorld.SetBlock(x, y, 55, new BlockData { BlockId = 1 });
                //        if (x == -4 || x == 4 || y == -4 || y == 4)
                //        {
                //            theWorld.SetBlock(x, y, 56, new BlockData { BlockId = 1 });
                //        }
                //    }
                //}
                //theWorld.SetBlock(0, 0, 56, new BlockData { BlockId = 1 });
                //theWorld.SetBlock(1, 0, 56, new BlockData { BlockId = 1 });
                //theWorld.SetBlock(1, 0, 57, new BlockData { BlockId = 1 });
                //theWorld.SetBlock(1, 1, 57, new BlockData { BlockId = 1 });


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
                    chunk.Flush();
                }
                rdm.Flush();
                rdm.renderData.SetLayoutFromShader(shaderFace); //TODO merge into world
                rdm.renderData.SetResourceForPixelShader(0, aotexture.ResourceView);

                camera.SetForm(rm.Form);
                theWorld.AddEntity(camera);

                var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, 800.0f / 600.0f, 0.1f, 100.0f);

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
                
                GC.Collect();

                RenderLoopHelper.Run(rm, false, delegate(RenderContext frame)
                {
                    camera.Step();

                    //physicsStartEvent.Set();
                    theWorld.StepPhysics(1.0f / 60);

                    shaderFace.buffer.transform = Matrix.Multiply(camera.GetViewMatrix(), proj);
                    shaderFace.buffer.transform.Transpose();
                    frame.UpdateShaderConstant(shaderFace);

                    rm.ImmediateContext.SetRenderData(rdm.renderData);
                    frame.Draw(rdm.renderData);

                    frame.Present(false);

                    //physicsFinishEvent.WaitOne();
                });
            }
        }
    }
}
