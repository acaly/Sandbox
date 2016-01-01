using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    //TODO avoid copying full struct onto stack when reading/writing
    struct BlockData
    {
        public int BlockId;
        public byte LightnessXP, LightnessXN, LightnessYP, LightnessYN, LightnessZP, LightnessZN;
        public int BlockColor;

        public bool HasCollision()
        {
            return BlockId != 0;
        }
    }
}
