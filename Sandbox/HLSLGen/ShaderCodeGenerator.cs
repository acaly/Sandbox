using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.HLSLGen
{
    class VertexShaderGenerator<TIn, TOut>
        where TIn : struct
        where TOut : struct
    {
        private Expression returnExpr;

        public VertexShaderGenerator()
        {
            returnExpr = Expression.Parameter(typeof(TIn), "input");
        }

        public void Append(Expression assign)
        {

        }

        public Expression ReturnExpression
        {
            get
            {
                return returnExpr;
            }
        }
    }

    struct TypeIn
    {
        public Vector3 pos;
    }
    struct TypeOut
    {
        public Vector3 pos;
    }
    struct ConstantMap
    {
        public Vector3 mat;
    }


    class GenTest
    {
        private static int Block(params object[] obj)
        {
            return 0;
        }

        private static int Assign(object left, object right)
        {
            return 0;
        }

        //private static int Output()

        private static void Main()
        {
            //VFloat3 kk = new VFloat3();
            //Expression expr_const = Expression.Constant(kk);
            //var type = expr_const.NodeType;
            //var expr_assign = Expression.Assign(expr_const, expr_const);
            //Expression<Func<TypeIn, TypeOut>> expr = i => new TypeOut { pos = kk };

            //Expression<Func<int, int>> expr = i => { return i + 1; };
            Expression.Block(Expression.Constant(11), (Expression<Func<int,int>>)(i => i + 1));
            Expression<Func<TypeIn,int>> expr = x => Block(
                0,
                int.Parse(""),
                1 + 1,
                Assign(x.pos.X, 1)
            );

        }
    }
}
