using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene
{
    abstract class MovableGridObject<T>
        where T : struct
    {
        protected T[] grids;

        public class GridPos
        {
            public int X, Y;
        }

        public MovableGridObject(int cacheDistance, int preloadDistance, int minDistance)
        {

        }

        protected abstract void OnGridMove(int loadingGrids, int unloadingGrids);
        protected abstract void LoadGrid(GridPos grid);
        protected abstract void UnloadGrid(GridPos grid);

        public void MoveGridCenter(float x, float y)
        {

        }
    }
}
