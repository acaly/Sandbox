using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    struct BlockData
    {
        public int BlockId;

        public bool HasCollision()
        {
            return BlockId != 0;
        }
    }
}
