using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Graphics
{
    class DefaultShader
    {
        public static readonly string Value = @"
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

cbuffer VS_CONSTANT_BUFFER // : register(b1)
{
	float4x4 worldViewProj;
}

PS_IN VS(VS_IN input)
{
	PS_IN output = (PS_IN)0;

	output.pos = mul(input.pos, worldViewProj);
	output.col = input.col;

	return output;
}

float4 PS(PS_IN input) : SV_Target
{
	return input.col;
}

[maxvertexcount(3)]
void GS(triangle PS_IN input[3], inout TriangleStream<PS_IN> triStream)
{
	//input[0].col.r = 1;
	triStream.Append(input[0]);
	triStream.Append(input[1]);
	triStream.Append(input[2]);
	
	triStream.RestartStrip();
}
";
    }
}
