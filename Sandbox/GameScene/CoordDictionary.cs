using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    public interface DictionaryValueInit<T>
    {
        void Init(T init, WorldCoord pos);
    }

    public class CoordDictionary<TValue, TInit>
        where TValue : class, DictionaryValueInit<TInit>, new()
    {
        private struct Node
        {
            public WorldCoord key;
            public int next; //0: empty, -1: no next, >0: go to next-1
            public TValue value;

            public override string ToString()
            {
                if (next == 0)
                {
                    return "{ Empty Node }";
                }
                else if (next == -1)
                {
                    return "{ Node " + key.ToString() + " }";
                }
                return "{ Node " + key.ToString() + ", Next: " + (next - 1) + " }";
            }
        }

        private static int GetSize(int size)
        {
            if (size <= 37)
                return 37;

            if (size % 2 == 0)
                size--;
            else
                size -= 2;

            while (true)
            {
                size += 2;

                if (size % 3 == 0) continue;
                if (size % 5 == 0) continue;
                if (size % 7 == 0) continue;
                if (size % 11 == 0) continue;
                if (size % 13 == 0) continue;
                if (size % 17 == 0) continue;
                if (size % 19 == 0) continue;
                if (size % 23 == 0) continue;
                if (size % 29 == 0) continue;
                if (size % 31 == 0) continue;

                return size;
            }
        }

        Node[] nodes;
        int count;
        TInit initValue;
        int freeNext;

        public CoordDictionary(TInit init)
            : this(init, 37)
        {
        }

        public CoordDictionary(TInit init, int capacity)
        {
            this.initValue = init;
            this.nodes = new Node[GetSize(capacity)];
            this.freeNext = nodes.Length / 3;
        }

        private int GetStartPosition(WorldCoord pos, uint mod)
        {
            uint hash = (uint)(((pos.x) << 10) + ((pos.y) << 20) + pos.z + 100);
            return (int)(hash % mod);
        }

        public bool Contains(WorldCoord pos)
        {
            int s = GetStartPosition(pos, (uint)nodes.Length);
            if (nodes[s].next == 0) return false;
            while (true)
            {
                if (nodes[s].key.x == pos.x && nodes[s].key.y == pos.y && nodes[s].key.z == pos.z)
                {
                    return true;
                }
                if (nodes[s].next == -1) return false;
                s = nodes[s].next - 1;
            }
        }

        public TValue Get(WorldCoord pos)
        {
            int s = GetStartPosition(pos, (uint)nodes.Length);
            if (nodes[s].next == 0) return null;
            while (true)
            {
                if (nodes[s].key.x == pos.x && nodes[s].key.y == pos.y && nodes[s].key.z == pos.z)
                {
                    return nodes[s].value;
                }
                if (nodes[s].next == -1) return null;
                s = nodes[s].next - 1;
            }
        }

        private TValue AutoCreate(WorldCoord pos)
        {
            ++count;
            TValue ret = new TValue();
            ret.Init(initValue, pos);
            return ret;
        }

        private int GetFree(Node[] nodes)
        {
            int lastFree = freeNext;
            for (int i = lastFree; i < nodes.Length; ++i)
            {
                if (nodes[i].next == 0)
                {
                    freeNext = i;
                    return i;
                }
            }
            for (int i = 0; i < lastFree; ++i)
            {
                if (nodes[i].next == 0)
                {
                    freeNext = i;
                    return i;
                }
            }
            return 0;
        }

        private void EnsureSize()
        {
            if (nodes.Length < count + 3)
            {
                int newSize = GetSize(nodes.Length * 2);
                Node[] newArray = new Node[newSize];
                foreach (Node node in nodes)
                {
                    if (node.next == 0) continue;
                    int s = GetStartPosition(node.key, (uint)newArray.Length);
                    if (newArray[s].next == 0)
                    {
                        newArray[s].key = node.key;
                        newArray[s].value = node.value;
                        newArray[s].next = -1;
                    }
                    else
                    {
                        int free = GetFree(newArray);
                        while (newArray[s].next != -1)
                        {
                            s = newArray[s].next - 1;
                        }
                        newArray[s].next = free + 1;
                        newArray[free].key = node.key;
                        newArray[free].value = node.value;
                        newArray[free].next = -1;
                    }
                }
                nodes = newArray;
            }
        }

        public TValue GetOrCreate(WorldCoord pos)
        {
            EnsureSize();
            int s = GetStartPosition(pos, (uint)nodes.Length);
            if (nodes[s].next == 0)
            {
                var ret = AutoCreate(pos);
                nodes[s].key = pos;
                nodes[s].next = -1;
                nodes[s].value = ret;
                return ret;
            }
            while (true)
            {
                if (nodes[s].key.x == pos.x && nodes[s].key.y == pos.y && nodes[s].key.z == pos.z)
                {
                    return nodes[s].value;
                }
                if (nodes[s].next == -1)
                {
                    int free = GetFree(nodes);
                    var ret = AutoCreate(pos);
                    nodes[s].next = free + 1;
                    nodes[free].key = pos;
                    nodes[free].next = -1;
                    nodes[free].value = ret;
                    return ret;
                }
                s = nodes[s].next - 1;
            }
        }
    }
}
