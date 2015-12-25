using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Physics
{
    public class CollisionQueue
    {
        public struct Node
        {
            public int GridId;
            public int EntityId;
            public float Value;
        }

        private Node[] nodes;
        private int count;

        public CollisionQueue()
        {
            nodes = new Node[8];
            count = 0;
        }

        public void EnsureCapacity(int count)
        {
            if (nodes.Length < count)
            {
                nodes = new Node[count + count / 2];
            }
        }

        public void Insert(int grid, int entity, float value)
        {
            EnsureCapacity(count + 1);
            nodes[count] = new Node
            {
                GridId = grid,
                EntityId = entity,
                Value = value,
            };
            ++count;
        }

        public void Update(float oldValue, int grid, int entity, float newValue)
        {
            if (newValue >= oldValue) return;

            for (int i = 0; i < count; ++i)
            {
                if (nodes[i].GridId == grid && nodes[i].EntityId == entity)
                {
                    nodes[i].Value = newValue;
                    return;
                }
            }
        }

        public int GetCount()
        {
            return count;
        }

        public Node ExtractMin()
        {
            float value = 2.0f;
            int select_i = 0;
            for (int i = 0; i < count; ++i)
            {
                if (value > nodes[i].Value)
                {
                    value = nodes[i].Value;
                    select_i = i;
                }
            }
            var ret = nodes[select_i];
            if (select_i != count - 1)
            {
                nodes[select_i] = nodes[count - 1];
            }
            --count;
            return ret;
        }

        public void Clear()
        {
            count = 0;
        }
    }
}
