using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Physics
{
    class GridStaticEntity
    {
        public Box[] CollisionArray;
        public int CollisionCount;
        public int[] CollisionSegments;

        public Vector3 Position;
    }
}
