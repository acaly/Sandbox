using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Physics
{
    class Entity
    {
        private GridPhysicWorld world;

        //---------------------
        //the following variables are declared as field instead of property to allow fast access/+=/-= from collision system
        //---------------------

        //should not directly modify these, except by physical engine
        public Vector3 Position, Velocity, Acceloration;

        //the max size of this entity, used by the physical engine to put it in grid
        public float MaxSize;

        public Box Collision;

        public List<AdditionalCollision> AdditionalCollisionList = new List<AdditionalCollision>();
        //---------------------
        //---------------------

        //called by GridPhysicWorld
        public void AddToPhysicWorld(GridPhysicWorld world)
        {
            this.world = world;
        }

        public void UpdatePosition(Vector3 newPos)
        {
            var oldPosition = Position;
            Position = newPos;
            world.MoveEntity(this, oldPosition);
        }
    }
}
