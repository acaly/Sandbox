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

        //for temperary usage only
        //currently used in: LightingManager
        public int flags;

        public bool HasCollision()
        {
            return BlockId != 0;
        }
    }
}
