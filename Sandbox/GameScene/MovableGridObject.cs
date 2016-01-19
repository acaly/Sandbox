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
        private int cacheDistance, preloadDistance, minDistance;
        private int cacheSize;
        private float gridSize;

        public class GridPos
        {
            public int X, Y;
        }

        //cacheDistance >= preloadDistance >= minDistance
        public MovableGridObject(int cacheDistance, int preloadDistance, int minDistance)
        {
            this.cacheDistance = cacheDistance;
            this.preloadDistance = preloadDistance;
            this.minDistance = minDistance;

            this.gridSize = World.ChunkWidth;

            cacheSize = cacheDistance * 2 + 1;
            this.grids = new T[cacheSize * cacheSize];

            loadedXMax = loadedYMax = -1;
            //wait for the first MoveGridCenter call
        }

        protected abstract void OnGridMove();
        protected abstract void LoadGrid(GridPos grid);
        protected abstract void UnloadGrid(GridPos grid);

        //current range in which we don't need to load new grids
        private float minx, maxx, miny, maxy;

        private int loadedXMin, loadedYMin, loadedXMax, loadedYMax;

        private int offsetX, offsetY;

        public void MoveGridCenter(float x, float y)
        {
            if (x >= minx && x <= maxx && y >= miny && y <= maxy)
            {
                return;
            }

            OnGridMove();

            int cx = (int)Math.Floor(x / gridSize), cy = (int)Math.Floor(y / gridSize);
            EnsureRange(cx - preloadDistance, cx + preloadDistance, cy - preloadDistance, cy + preloadDistance);

            //update range
            minx = (cx - preloadDistance + minDistance) * gridSize;
            maxx = (cx + preloadDistance - minDistance + 1) * gridSize;
            miny = (cy - preloadDistance + minDistance) * gridSize;
            maxy = (cy + preloadDistance - minDistance + 1) * gridSize;
        }

        private void EnsureRange(int xmin, int xmax, int ymin, int ymax)
        {
            //unload unused grids
            for (int xi = loadedXMin; xi <= loadedXMax; ++xi)
            {
                for (int yi = loadedYMin; yi <= loadedYMax; ++yi)
                {
                    if (xi < xmin || xi > xmax || yi < ymin || yi > ymax)
                    {
                        UnloadGrid(new GridPos { X = xi, Y = yi });
                    }
                }
            }

            for (int xi = xmin; xi <= xmax; ++xi)
            {
                for (int yi = ymin; yi <= ymax; ++yi)
                {
                    if (xi < loadedXMin || xi > loadedXMax || yi < loadedYMin || yi > loadedYMax)
                    {
                        LoadGrid(new GridPos { X = xi, Y = yi });
                    }
                }
            }

            loadedXMin = xmin;
            loadedXMax = xmax;
            loadedYMin = ymin;
            loadedYMax = ymax;
        }

        protected int GetIndexForGrid(GridPos pos)
        {
            //TODO check whether this works
            int xi = (pos.X + offsetX - cacheSize * loadedXMin) % cacheSize;
            int yi = (pos.Y + offsetY - cacheSize * loadedYMin) % cacheSize;
            return xi + yi * cacheSize;
        }
    }
}
