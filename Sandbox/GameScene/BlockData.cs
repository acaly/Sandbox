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
        //the lightness fields stores value of lightness+1. while 0 indicates that the face is not rendered
        public byte LightnessXP, LightnessXN, LightnessYP, LightnessYN, LightnessZP, LightnessZN;
        public uint BlockColor;

        public bool HasCollision()
        {
            return BlockId != 0;
        }
    }
}
