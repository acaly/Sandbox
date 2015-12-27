using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Physics
{
    public enum AdditionalCollisionType
    {
        //only test against static entity, id is not used, callback is not called
        StaticCollision,
    }

    class AdditionalCollision
    {
        public AdditionalCollisionType Type;
        public int Group;
        public Box Box;

        //entity is responsible to clear this result
        public bool Result;

        public virtual void OnCollide() { }
    }
}
