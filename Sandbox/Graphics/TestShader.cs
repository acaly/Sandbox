using Sandbox.HLSLGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Graphics
{
    [ShaderClass]
    class TestShader
    {
        public static TestShader CompiledShader;
        public static string ShaderCode;

        public struct Float4
        {
            public float X;
            public float Y, Z, W;
            public static Float4 operator +(Float4 a, Float4 b)
            {
                return new Float4();
            }
        }

        public struct VS_In
        {
            public Float4 x, y;
        }

        public struct VS_Out
        {
            public Float4 x;
        }

        public VS_Out VS(VS_In input)
        {
            VS_Out output = new VS_Out();
            if (input.x.X == 0)
            return output;
            output.x = input.x + input.y;
            output.x.Y = input.x.Z + 1;
            return output;
        }
    }
}
