using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Graphics
{
    public interface IRenderBuffer<T>
        where T : struct
    {
        void Append(T val);
    }
}
