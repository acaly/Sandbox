using Sandbox.Graphics;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Sandbox.Gui
{
    class GuiEnvironment
    {
        private RenderManager rm;
        private VertexShader vertexShader;
        private PixelShader pixelShader;
        private SamplerState sampler;

        private ShaderSignature sig;

        private BlendState blend;
        private DepthStencilState depth;

        public GuiEnvironment(RenderManager rm)
        {
            this.rm = rm;
            
            sampler = new SamplerState(rm.Device, new SamplerStateDescription
            {
                Filter = Filter.MinMagMipPoint,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
            });

            vertexList = new Vertex[6];
            vertexList[0].tex = new Vector4(0, 0, 0, 0);
            vertexList[2].tex = new Vector4(1, 1, 0, 0);
            vertexList[1].tex = new Vector4(1, 0, 0, 0);
            vertexList[3].tex = new Vector4(0, 0, 0, 0);
            vertexList[5].tex = new Vector4(0, 1, 0, 0);
            vertexList[4].tex = new Vector4(1, 1, 0, 0);

            var device = rm.Device;
            using (var vertexShaderByteCode = ShaderBytecode.Compile(ShaderString, "VS", "vs_4_0"))
            {
                vertexShader = new VertexShader(rm.Device, vertexShaderByteCode);
                sig = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            }
            using (var pixelShaderByteCode = ShaderBytecode.Compile(ShaderString, "PS", "ps_4_0"))
            {
                pixelShader = new PixelShader(rm.Device, pixelShaderByteCode);
            }

            layout = new InputLayout(device, sig, RenderData<Vertex>.CreateLayoutElementsFromType());

            vertexBuffer = SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, vertexList);

            binding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);

            var bsd = new BlendStateDescription();
            bsd.RenderTarget[0].IsBlendEnabled = true;

            bsd.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            bsd.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            bsd.RenderTarget[0].BlendOperation = BlendOperation.Add;
            
            //useless
            bsd.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            bsd.RenderTarget[0].DestinationAlphaBlend = BlendOption.One;
            bsd.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;

            bsd.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            blend = new BlendState(rm.Device, bsd);

            depth = new DepthStencilState(rm.Device, new DepthStencilStateDescription
            {
                IsDepthEnabled = false,
                IsStencilEnabled = false,
            });
        }

        public void BeginEnvironment()
        {
            var context = rm.Device.ImmediateContext;
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, binding);
            context.VertexShader.Set(vertexShader);
            context.GeometryShader.Set(null);
            context.PixelShader.Set(pixelShader);
            context.PixelShader.SetSampler(0, sampler);
            context.OutputMerger.SetBlendState(blend);
            context.OutputMerger.SetDepthStencilState(depth);
        }

        public void EndEnvironment()
        {
            var context = rm.Device.ImmediateContext;
            context.OutputMerger.SetBlendState(null);
            context.OutputMerger.SetDepthStencilState(null);
        }

        //render part

        private struct Vertex
        {
            [RenderDataElement(Format.R32G32B32A32_Float, "POSITION", 0)]
            public Vector4 pos;
            [RenderDataElement(Format.R32G32B32A32_Float, "TEXCOORD", 0)]
            public Vector4 tex;
            [RenderDataElement(Format.R32G32B32A32_Float, "COLOR", 0)]
            public Vector4 col;
        }

        private Vertex[] vertexList;
        private Buffer vertexBuffer;
        private InputLayout layout;
        private VertexBufferBinding binding;

        public void DrawTexture(GuiTexture tex, Vector4 color, float x, float y, float w, float h, float u, float v, float W, float H)
        {
            var context = rm.Device.ImmediateContext;
            context.PixelShader.SetShaderResource(0, tex.ResourceView);

            x = x / 800.0f * 2 - 1;
            y = -(y / 600.0f * 2 - 1);
            w /= 800.0f / 2;
            h /= -600.0f / 2;

            vertexList[0].pos = new Vector4(x,     y,     0, 1);
            vertexList[0].tex = new Vector4(u,     v,     0, 1);
            vertexList[1].pos = new Vector4(x + w, y,     0, 1);
            vertexList[1].tex = new Vector4(u + W, v,     0, 1);
            vertexList[2].pos = new Vector4(x + w, y + h, 0, 1);
            vertexList[2].tex = new Vector4(u + W, v + H, 0, 1);
            vertexList[3].pos = new Vector4(x,     y,     0, 1);
            vertexList[3].tex = new Vector4(u,     v,     0, 1);
            vertexList[4].pos = new Vector4(x + w, y + h, 0, 1);
            vertexList[4].tex = new Vector4(u + W, v + H, 0, 1);
            vertexList[5].pos = new Vector4(x,     y + h, 0, 1);
            vertexList[5].tex = new Vector4(u,     v + H, 0, 1);

            vertexList[0].col = vertexList[1].col = vertexList[2].col = color;
            vertexList[3].col = vertexList[4].col = vertexList[5].col = color;

            rm.Device.ImmediateContext.UpdateSubresource(vertexList, vertexBuffer);

            context.Draw(6, 0);
        }

        //TODO use geometry shader
        private static readonly string ShaderString = @"

struct VS_IN
{
	float4 pos : POSITION;
	float4 tex : TEXCOORD;
	float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	float4 tex : TEXCOORD;
	float4 col : COLOR;
};

Texture2D faceTexture : register(t0);
SamplerState MeshTextureSampler : register(s0);

PS_IN VS( VS_IN input )
{
	PS_IN output = (PS_IN)0;
	
	output.pos = input.pos;
	output.tex = input.tex;
    output.col = input.col;
	
	return output;
}

float4 PS( PS_IN input ) : SV_Target
{
	float4 ret = faceTexture.Sample(MeshTextureSampler, input.tex.xy);
    if (input.col.a > 0)
    {
        ret.rgb *= input.col.rgb;
    }
    else
    {
        ret.rgb += input.col.rgb;
    }
    return ret;
}

";
    }
}
