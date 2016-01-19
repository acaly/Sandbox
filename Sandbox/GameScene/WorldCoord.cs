using SharpDX;
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

        public WorldCoord WithOffset(WorldCoord coord)
        {
            return new WorldCoord { x = x + coord.x, y = y + coord.y, z = z + coord.z };
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public Vector4 ToVector4(float w)
        {
            return new Vector4(x, y, z, w);
        }

        private WorldCoord Inverse()
        {
            return new WorldCoord(-x, -y, -z);
        }

        public struct Direction2
        {
            public WorldCoord coord;

            public Direction1 Devide(int index) //index can be 0, 1. Devide(0) + Devide(1) = this
            {
                if (coord.x == 0)
                {
                    if (index == 0)
                    {
                        return new Direction1 { coord = new WorldCoord { y = coord.y } };
                    }
                    else if (index == 1)
                    {
                        return new Direction1 { coord = new WorldCoord { z = coord.z } };
                    }
                }
                else if (coord.y == 0)
                {
                    if (index == 0)
                    {
                        return new Direction1 { coord = new WorldCoord { z = coord.z } };
                    }
                    else if (index == 1)
                    {
                        return new Direction1 { coord = new WorldCoord { x = coord.x } };
                    }
                }
                else if (coord.z == 0)
                {
                    if (index == 0)
                    {
                        return new Direction1 { coord = new WorldCoord { x = coord.x } };
                    }
                    else if (index == 1)
                    {
                        return new Direction1 { coord = new WorldCoord { y = coord.y } };
                    }
                }
                return new Direction1();
            }
        }

        public struct Direction1
        {
            public WorldCoord coord;

            public Direction1(int face)
            {
                switch (face)
                {
                    case 0:
                        coord = new WorldCoord(1, 0, 0);
                        break;
                    case 1:
                        coord = new WorldCoord(-1, 0, 0);
                        break;
                    case 2:
                        coord = new WorldCoord(0, 1, 0);
                        break;
                    case 3:
                        coord = new WorldCoord(0, -1, 0);
                        break;
                    case 4:
                        coord = new WorldCoord(0, 0, 1);
                        break;
                    case 5:
                        coord = new WorldCoord(0, 0, -1);
                        break;
                    default:
                        coord = new WorldCoord();
                        break;
                }
            }

            public Direction1 U()
            {
                WorldCoord ret;
                if (coord.x > 0)
                {
                    ret = new WorldCoord { z = 1 };
                }
                else if (coord.x < 0)
                {
                    ret = new WorldCoord { y = 1 };
                }
                else if (coord.y > 0)
                {
                    ret = new WorldCoord { x = 1 };
                }
                else if (coord.y < 0)
                {
                    ret = new WorldCoord { z = 1 };
                }
                else if (coord.z > 0)
                {
                    ret = new WorldCoord { y = 1 };
                }
                else if (coord.z < 0)
                {
                    ret = new WorldCoord { x = 1 };
                }
                else
                {
                    return new Direction1();
                }
                return new Direction1 { coord = ret };
            }
            public Direction1 V()
            {
                WorldCoord ret;
                if (coord.x > 0)
                {
                    ret = new WorldCoord { y = 1 };
                }
                else if (coord.x < 0)
                {
                    ret = new WorldCoord { z = 1 };
                }
                else if (coord.y > 0)
                {
                    ret = new WorldCoord { z = 1 };
                }
                else if (coord.y < 0)
                {
                    ret = new WorldCoord { x = 1 };
                }
                else if (coord.z > 0)
                {
                    ret = new WorldCoord { x = 1 };
                }
                else if (coord.z < 0)
                {
                    ret = new WorldCoord { y = 1 };
                }
                else
                {
                    return new Direction1();
                }
                return new Direction1 { coord = ret };
            }

            public Direction2 UVPN(int index) //index can be 0,1,2,3. pp, pn, np, nn
            {
                var ret_u = U().coord;
                var ret_v = V().coord;
                if (index == 1)
                {
                    ret_v = ret_v.Inverse();
                }
                else if (index == 2)
                {
                    ret_u = ret_u.Inverse();
                }
                else if (index == 3)
                {
                    ret_u = ret_u.Inverse();
                    ret_v = ret_v.Inverse();
                }
                return new Direction2 { coord = ret_u.WithOffset(ret_v) };
            }
        }
    }
}
