using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Graphics
{
    class BlockFaceShader
    {
        public static readonly string Value = @"
struct VS_IN
{
    float4 pos : POSITION;
    float4 dir_u : TEXCOORD1;
    float4 dir_v : TEXCOORD2;
    float4 col : COLOR;
};

struct GS_IN
{
    float4 pos : SV_POSITION;
    float4 dir_u : TEXCOORD1;
    float4 dir_v : TEXCOORD2;
    float4 col : COLOR;
};

struct PS_IN
{
	float4 pos : SV_POSITION;
	//float4 tex : TEXCOORD0;
    float4 col : COLOR0;
};

cbuffer VS_CONSTANT_BUFFER
{
	float4x4 worldViewProj;
}

//Texture2D faceTexture : register(t0);
//SamplerState MeshTextureSampler : register(s1);

GS_IN VS(VS_IN input)
{
	GS_IN output = (GS_IN)0;

	output.pos = mul(input.pos, worldViewProj);
    output.dir_u = mul(input.dir_u, worldViewProj);
    output.dir_v = mul(input.dir_v, worldViewProj);
	output.col = float4(0.6, 0.8, 1.0, 1.0) * input.col;

	return output;
}

[maxvertexcount(4)]
void GS(point GS_IN input[1], inout TriangleStream<PS_IN> triStream)
{
    PS_IN point_pp = (PS_IN)0;
    PS_IN point_pn = (PS_IN)0;
    PS_IN point_np = (PS_IN)0;
    PS_IN point_nn = (PS_IN)0;

    point_pp.col = input[0].col;
    point_pn.col = input[0].col;
    point_np.col = input[0].col;
    point_nn.col = input[0].col;

    //point_pp.tex = float4(1, 1, 0, 0);
    //point_pn.tex = float4(1, 0, 0, 0);
    //point_np.tex = float4(0, 1, 0, 0);
    //point_nn.tex = float4(0, 0, 0, 0);

    point_pp.pos = input[0].pos + input[0].dir_u + input[0].dir_v;
    point_pn.pos = input[0].pos + input[0].dir_u - input[0].dir_v;
    point_np.pos = input[0].pos - input[0].dir_u + input[0].dir_v;
    point_nn.pos = input[0].pos - input[0].dir_u - input[0].dir_v;
    
	triStream.Append(point_pp);
	triStream.Append(point_pn);
	triStream.Append(point_np);
	triStream.Append(point_nn);
	triStream.RestartStrip();
}

float4 PS(PS_IN input) : SV_Target
{
    //float2 coord = input.tex.xy * 16;
    //coord.x = floor(coord.x) / 16;
    //coord.y = floor(coord.y) / 16;
    
    //float4 col = faceTexture.Sample(MeshTextureSampler, coord);
    //float4 col = faceTexture.Sample(MeshTextureSampler, input.tex.xy);

    return input.col;
}
";
    }
}
