using Sandbox.GameScene;
using Sandbox.Graphics;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
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
    public struct VertexData
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

    public struct VertexConstData
    {
        public Matrix transform;
    }
    
    static class Program
    {
        [STAThread]
        static void Main()
        {

            //for (int i = -10; i <= 10; ++i)
            //{
            //    for (int j = -10; j <= 10; ++j)
            //    {
            //        theWorld.SetBlock(i, j, 0, new GameScene.BlockData { BlockId = 1 });
            //        if (i == 10 || i == -10 || j == 10 || j == -10)
            //        {
            //            for (int k = 1; k < 5; ++k)
            //            {
            //                theWorld.SetBlock(i, j, k, new GameScene.BlockData { BlockId = 1 });
            //            }
            //        }
            //    }
            //}

            using (RenderManager rm = new RenderManager())
            {
                rm.InitDevice();

                GameScene.World theWorld = new GameScene.World(rm);
                GameScene.Camera camera = new GameScene.Camera(new Vector3(0, 0, 6));

                for (int i = -4; i <= 4; ++i)
                {
                    for (int j = -4; j <= 4; ++j)
                    {
                        theWorld.SetBlock(i, j, 0, new BlockData { BlockId = 1 });
                        if (i == 4 || i == -4 || j == 4 || j == -4)
                        //{
                            theWorld.SetBlock(i, j, 1, new BlockData { BlockId = 1 });
                        //}
                    }
                }


                var shaderFace = Shader<VertexConstData>.CreateFromString(rm, BlockFaceShader.Value);

                foreach (var chunk in theWorld.chunkList)
                {
                    chunk.Flush();
                    chunk.renderData.SetLayoutFromShader(shaderFace);
                }

                camera.SetForm(rm.Form);
                theWorld.AddEntity(camera);

                var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, 800.0f / 600.0f, 0.1f, 100.0f);

                rm.ImmediateContext.ApplyShader(shaderFace);

                var renderThread = new Thread(new ThreadStart(delegate()
                {
                    float last_time = 0;
                    while (true)
                    {
                        var frame = rm.NextFrame(true);
                        if (frame == null) break;

                        float time = frame.TotalTimeMilliseconds / 1000.0f;
                        if (time - last_time >= 2)
                        {
                            var fps = frame.Fps;
                            last_time = time;
                        }

                        theWorld.StepPhysics(1.0f / 60);
                        if (camera.Position.Z < 1.5f)
                        {
                        }
                        camera.Step();

                        var view = camera.GetViewMatrix();
                        var viewProj = Matrix.Multiply(view, proj);
                        //shaderFace.buffer.transform = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f) * viewProj;
                        shaderFace.buffer.transform = viewProj;
                        shaderFace.buffer.transform.Transpose();
                        frame.UpdateShaderConstant(shaderFace);

                        //frame.Draw(renderDataFace);

                        foreach (var chunk in theWorld.chunkList)
                        {
                            rm.ImmediateContext.SetRenderData(chunk.renderData);
                            frame.Draw(chunk.renderData);
                        }

                        frame.Present(false);
                    }
                }));

                rm.ShowWindow();
                renderThread.Start();
                while (!rm.FormIsClosed())
                {
                    Application.DoEvents();
                }
                renderThread.Abort(); //TODO abort is a little bit dangerous?
            }
        }
    }
}
