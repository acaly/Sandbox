﻿using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

namespace Sandbox
{
    class MultiCube
    {
        private static readonly string ShaderCode = @"
struct VS_IN
{
	float4 pos : POSITION;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 col : COLOR;
};

float4x4 WorldViewProj;

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	output.pos = mul(input.pos, WorldViewProj);
	output.col = input.col;
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	return input.col;
}";
        internal class Program
        {
            /// <summary>
            /// State used to store testcase values.
            /// </summary>
            enum TestType
            {
                Immediate = 0,
                Deferred = 1,
                FrozenDeferred = 2
            }

            struct State
            {
                public bool Exit;
                public int CountCubes;
                public int ThreadCount;
                public TestType Type;
                public bool SimulateCpuUsage;
                public bool UseMap;
            }

            const int MaxNumberOfCubes = 256;
            const int MaxNumberOfThreads = 16;
            const int BurnCpuFactor = 50;

            public void Run()
            {

                // Initial state
                var currentState = new State
                {
                    // Set the number of cubes to display (horizontally and vertically) 
                    CountCubes = 64,
                    // Number of threads to run concurrently 
                    ThreadCount = 4,
                    // Use deferred by default
                    Type = TestType.Deferred,
                    // BurnCpu by default
                    SimulateCpuUsage = true,
                    // Default is using Map/Unmap
                    UseMap = true,
                };
                var nextState = currentState;

                // --------------------------------------------------------------------------------------
                // Init Direct3D11
                // --------------------------------------------------------------------------------------

                // Create the Rendering form 
                var form = new RenderForm("SharpDX - MiniCube Direct3D11");
                form.ClientSize = new Size(1024, 1024);

                // SwapChain description 
                var desc = new SwapChainDescription()
                {
                    BufferCount = 2,
                    ModeDescription =
                        new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                            new Rational(60, 1), Format.R8G8B8A8_UNorm),
                    IsWindowed = true,
                    OutputHandle = form.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    Usage = Usage.RenderTargetOutput
                };

                // Create Device and SwapChain 
                Device device;
                SwapChain swapChain;
                Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out device, out swapChain);
                var immediateContext = device.ImmediateContext;

                // PreCreate deferred contexts 
                var deferredContexts = new DeviceContext[MaxNumberOfThreads];
                for (int i = 0; i < deferredContexts.Length; i++)
                    deferredContexts[i] = new DeviceContext(device);

                // Allocate rendering context array 
                var contextPerThread = new DeviceContext[MaxNumberOfThreads];
                contextPerThread[0] = immediateContext;
                var commandLists = new CommandList[MaxNumberOfThreads];
                CommandList[] frozenCommandLists = null;

                // Check if driver is supporting natively CommandList
                bool supportConcurentResources;
                bool supportCommandList;
                device.CheckThreadingSupport(out supportConcurentResources, out supportCommandList);

                // Ignore all windows events 
                var factory = swapChain.GetParent<Factory>();
                factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

                // New RenderTargetView from the backbuffer 
                var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
                var renderView = new RenderTargetView(device, backBuffer);

                // Compile Vertex and Pixel shaders 
                var bytecode = ShaderBytecode.Compile(ShaderCode, "VS", "vs_4_0");
                var vertexShader = new VertexShader(device, bytecode);

                // Layout from VertexShader input signature 
                var layout = new InputLayout(device, ShaderSignature.GetInputSignature(bytecode), new[] 
                    { 
                        new InputElement("POSITION", 0, Format.R32G32B32A32_Float, 0, 0), 
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 16, 0) 
                    });
                bytecode.Dispose();

                bytecode = ShaderBytecode.Compile(ShaderCode, "PS", "ps_4_0");
                var pixelShader = new PixelShader(device, bytecode);
                bytecode.Dispose();

                // Instantiate Vertex buiffer from vertex data 
                var vertices = Buffer.Create(device, BindFlags.VertexBuffer, new[] 
                                  { 
                                      new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), // Front 
                                      new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), 
                                      new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), 
                                      new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), 
                                      new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), 
                                      new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f), 
 
                                      new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), // BACK 
                                      new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), 
                                      new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), 
                                      new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), 
                                      new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), 
                                      new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 0.0f, 1.0f), 
 
                                      new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), // Top 
                                      new Vector4(-1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), 
                                      new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), 
                                      new Vector4(-1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), 
                                      new Vector4( 1.0f, 1.0f,  1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), 
                                      new Vector4( 1.0f, 1.0f, -1.0f,  1.0f), new Vector4(0.0f, 0.0f, 1.0f, 1.0f), 
 
                                      new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), // Bottom 
                                      new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), 
                                      new Vector4(-1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), 
                                      new Vector4(-1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), 
                                      new Vector4( 1.0f,-1.0f, -1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), 
                                      new Vector4( 1.0f,-1.0f,  1.0f,  1.0f), new Vector4(1.0f, 1.0f, 0.0f, 1.0f), 
 
                                      new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), // Left 
                                      new Vector4(-1.0f, -1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), 
                                      new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), 
                                      new Vector4(-1.0f, -1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), 
                                      new Vector4(-1.0f,  1.0f,  1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), 
                                      new Vector4(-1.0f,  1.0f, -1.0f, 1.0f), new Vector4(1.0f, 0.0f, 1.0f, 1.0f), 
 
                                      new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), // Right 
                                      new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), 
                                      new Vector4( 1.0f, -1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), 
                                      new Vector4( 1.0f, -1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), 
                                      new Vector4( 1.0f,  1.0f, -1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), 
                                      new Vector4( 1.0f,  1.0f,  1.0f, 1.0f), new Vector4(0.0f, 1.0f, 1.0f, 1.0f), 
                            });

                // Create Constant Buffer 
                var staticContantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                var dynamicConstantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Dynamic, BindFlags.ConstantBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);

                // Create Depth Buffer & View 
                var depthBuffer = new Texture2D(device, new Texture2DDescription()
                {
                    Format = Format.D32_Float_S8X24_UInt,
                    ArraySize = 1,
                    MipLevels = 1,
                    Width = form.ClientSize.Width,
                    Height = form.ClientSize.Height,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.DepthStencil,
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.None
                });

                var depthView = new DepthStencilView(device, depthBuffer);

                // --------------------------------------------------------------------------------------
                // Prepare matrices & clocks
                // --------------------------------------------------------------------------------------

                const float viewZ = 5.0f;

                // Prepare matrices 
                var view = Matrix.LookAtLH(new Vector3(0, 0, -viewZ), new Vector3(0, 0, 0), Vector3.UnitY);
                var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);
                var viewProj = Matrix.Multiply(view, proj);

                // Use clock 
                var clock = new Stopwatch();
                clock.Start();

                var fpsTimer = new Stopwatch();
                fpsTimer.Start();
                int fpsCounter = 0;

                // --------------------------------------------------------------------------------------
                // Register KeyDown event handler on the form
                // --------------------------------------------------------------------------------------
                bool switchToNextState = false;

                // Install keys handlers 
                form.KeyDown += (target, arg) =>
                {
                    if (arg.KeyCode == Keys.Left && nextState.CountCubes > 1)
                        nextState.CountCubes--;
                    if (arg.KeyCode == Keys.Right && nextState.CountCubes < MaxNumberOfCubes)
                        nextState.CountCubes++;

                    if (arg.KeyCode == Keys.F1)
                        nextState.Type = (TestType)((((int)nextState.Type) + 1) % 3);
                    if (arg.KeyCode == Keys.F2)
                        nextState.UseMap = !nextState.UseMap;
                    if (arg.KeyCode == Keys.F3)
                        nextState.SimulateCpuUsage = !nextState.SimulateCpuUsage;

                    if (nextState.Type == TestType.Deferred)
                    {
                        if (arg.KeyCode == Keys.Down && nextState.ThreadCount > 1)
                            nextState.ThreadCount--;
                        if (arg.KeyCode == Keys.Up && nextState.ThreadCount < MaxNumberOfThreads)
                            nextState.ThreadCount++;
                    }
                    if (arg.KeyCode == Keys.Escape)
                        nextState.Exit = true;
                    switchToNextState = true;
                };

                // --------------------------------------------------------------------------------------
                // Function used to setup the pipeline
                // --------------------------------------------------------------------------------------
                Action SetupPipeline = () =>
                {
                    int threadCount = 1;
                    if (currentState.Type != TestType.Immediate)
                    {
                        threadCount = currentState.Type == TestType.Deferred ? currentState.ThreadCount : 1;
                        Array.Copy(deferredContexts, contextPerThread, contextPerThread.Length);
                    }
                    else
                    {
                        contextPerThread[0] = immediateContext;
                    }
                    for (int i = 0; i < threadCount; i++)
                    {
                        var renderingContext = contextPerThread[i];
                        // Prepare All the stages 
                        renderingContext.InputAssembler.InputLayout = layout;
                        renderingContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                        renderingContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertices, Utilities.SizeOf<Vector4>() * 2, 0));
                        renderingContext.VertexShader.SetConstantBuffer(0, currentState.UseMap ? dynamicConstantBuffer : staticContantBuffer);
                        renderingContext.VertexShader.Set(vertexShader);
                        renderingContext.Rasterizer.SetViewport(0, 0, form.ClientSize.Width, form.ClientSize.Height);
                        renderingContext.PixelShader.Set(pixelShader);
                        renderingContext.OutputMerger.SetTargets(depthView, renderView);
                    }
                };

                // --------------------------------------------------------------------------------------
                // Function used to render a row of cubes
                // --------------------------------------------------------------------------------------
                Action<int, int, int> RenderRow = (int contextIndex, int fromY, int toY) =>
                {
                    var renderingContext = contextPerThread[contextIndex];
                    var time = clock.ElapsedMilliseconds / 1000.0f;

                    if (contextIndex == 0)
                    {
                        contextPerThread[0].ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
                        contextPerThread[0].ClearRenderTargetView(renderView, SharpDX.Color.Aqua);
                    }

                    int count = currentState.CountCubes;
                    float divCubes = (float)count / (viewZ - 1);

                    var rotateMatrix = Matrix.Scaling(1.0f / count) * Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f);

                    for (int y = fromY; y < toY; y++)
                    {
                        for (int x = 0; x < count; x++)
                        {
                            rotateMatrix.M41 = (x + .5f - count * .5f) / divCubes;
                            rotateMatrix.M42 = (y + .5f - count * .5f) / divCubes;

                            // Update WorldViewProj Matrix 
                            Matrix worldViewProj;
                            Matrix.Multiply(ref rotateMatrix, ref viewProj, out worldViewProj);
                            worldViewProj.Transpose();
                            // Simulate CPU usage in order to see benefits of worlViewProj

                            if (currentState.SimulateCpuUsage)
                            {
                                for (int i = 0; i < BurnCpuFactor; i++)
                                {
                                    Matrix.Multiply(ref rotateMatrix, ref viewProj, out worldViewProj);
                                    worldViewProj.Transpose();
                                }
                            }

                            if (currentState.UseMap)
                            {
                                var dataBox = renderingContext.MapSubresource(dynamicConstantBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None);
                                Utilities.Write(dataBox.DataPointer, ref worldViewProj);
                                renderingContext.UnmapSubresource(dynamicConstantBuffer, 0);
                            }
                            else
                            {
                                renderingContext.UpdateSubresource(ref worldViewProj, staticContantBuffer);
                            }

                            // Draw the cube 
                            renderingContext.Draw(36, 0);
                        }
                    }

                    if (currentState.Type != TestType.Immediate)
                        commandLists[contextIndex] = renderingContext.FinishCommandList(false);
                };


                Action<int> RenderDeferred = (int threadCount) =>
                {
                    int deltaCube = currentState.CountCubes / threadCount;
                    if (deltaCube == 0) deltaCube = 1;
                    int nextStartingRow = 0;
                    var tasks = new Task[threadCount];
                    for (int i = 0; i < threadCount; i++)
                    {
                        var threadIndex = i;
                        int fromRow = nextStartingRow;
                        int toRow = (i + 1) == threadCount ? currentState.CountCubes : fromRow + deltaCube;
                        if (toRow > currentState.CountCubes)
                            toRow = currentState.CountCubes;
                        nextStartingRow = toRow;

                        tasks[i] = new Task(() => RenderRow(threadIndex, fromRow, toRow));
                        tasks[i].Start();
                    }
                    Task.WaitAll(tasks);
                };


                // --------------------------------------------------------------------------------------
                // Main Loop
                // --------------------------------------------------------------------------------------
                RenderLoop.Run(form, () =>
                {
                    if (currentState.Exit)
                        form.Close();

                    fpsCounter++;
                    if (fpsTimer.ElapsedMilliseconds > 1000)
                    {
                        var typeStr = currentState.Type.ToString();
                        if (currentState.Type != TestType.Immediate && !supportCommandList) typeStr += "*";

                        form.Text = string.Format("SharpDX - MultiCube D3D11 - (F1) {0} - (F2) {1} - (F3) {2} - Threads ↑↓{3} - Count ←{4}→ - FPS: {5:F2} ({6:F2}ms)", typeStr, currentState.UseMap ? "Map/UnMap" : "UpdateSubresource", currentState.SimulateCpuUsage ? "BurnCPU On" : "BurnCpu Off", currentState.Type == TestType.Deferred ? currentState.ThreadCount : 1, currentState.CountCubes * currentState.CountCubes, 1000.0 * fpsCounter / fpsTimer.ElapsedMilliseconds, (float)fpsTimer.ElapsedMilliseconds / fpsCounter);
                        fpsTimer.Reset();
                        fpsTimer.Stop();
                        fpsTimer.Start();
                        fpsCounter = 0;
                    }

                    // Setup the pipeline before any rendering
                    SetupPipeline();

                    // Execute on the rendering thread when ThreadCount == 1 or No deferred rendering is selected
                    if (currentState.Type == TestType.Immediate || (currentState.Type == TestType.Deferred && currentState.ThreadCount == 1))
                    {
                        RenderRow(0, 0, currentState.CountCubes);
                    }

                    // In case of deferred context, use of FinishCommandList / ExecuteCommandList
                    if (currentState.Type != TestType.Immediate)
                    {
                        if (currentState.Type == TestType.FrozenDeferred)
                        {
                            if (commandLists[0] == null)
                                RenderDeferred(1);
                        }
                        else if (currentState.ThreadCount > 1)
                        {
                            RenderDeferred(currentState.ThreadCount);
                        }

                        for (int i = 0; i < currentState.ThreadCount; i++)
                        {
                            var commandList = commandLists[i];
                            // Execute the deferred command list on the immediate context
                            immediateContext.ExecuteCommandList(commandList, false);

                            // For classic deferred we release the command list. Not for frozen
                            if (currentState.Type == TestType.Deferred)
                            {
                                // Release the command list
                                commandList.Dispose();
                                commandLists[i] = null;
                            }
                        }
                    }

                    if (switchToNextState)
                    {
                        currentState = nextState;
                        switchToNextState = false;
                    }

                    // Present! 
                    swapChain.Present(0, PresentFlags.None);
                });
            }

            [STAThread]
            private static void Main()
            {
                var program = new Program();
                program.Run();
            }
        }
    }
}
