using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    public struct WorldCoord
    {
        public WorldCoord(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public int x, y, z;

        public override string ToString()
        {
            return "{" + x + ", " + y + ", " + z + "}";
        }
    }
}
