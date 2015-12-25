using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.Physics
{
    //2D grid
    class GridPhysicWorld
    {
        public GridPhysicWorld(float gridSize, int gridXOffset, int gridYOffset, int gridXSize, int gridYSize)
        {
            this.Reset(gridSize, gridXOffset, gridYOffset, gridXSize, gridYSize, null);
        }

        public void ResetGrid(float gridSize, int gridXOffset, int gridYOffset, int gridXSize, int gridYSize)
        {
            Reset(gridSize, gridXOffset, gridYOffset, gridXSize, gridYSize, grids);
        }

        private void Reset(float gridSize, int gridXOffset, int gridYOffset, int gridXSize, int gridYSize, Grid[] oldgrids)
        {
            this.GridSize = gridSize;
            this.grids = new Grid[gridXSize * gridYSize];
            this.gridOffsetX = gridXOffset;
            this.gridOffsetY = gridYOffset;
            this.gridSizeX = gridXSize;
            this.gridSizeY = gridYSize;

            for (int i = 0; i < grids.Length; ++i)
            {
                grids[i].max_entity_size = 1; //minimum is 1
                grids[i].entities = new EntityInfoInGrids[4];
            }
            RecalcGridRanges();

            if (oldgrids != null)
            {
                for (int gridId = 0; gridId < oldgrids.Length; ++gridId)
                {
                    if (oldgrids[gridId].staticEntity != null)
                    {
                        SetGridEntity(oldgrids[gridId].basePosition, oldgrids[gridId].staticEntity);
                    }
                    for (int entityId = 0; entityId < oldgrids[gridId].entityCount; ++entityId)
                    {
                        AddEntity(oldgrids[gridId].entities[entityId].entity);
                    }
                }
            }
        }

        private float GridSize;

        private struct EntityInfoInGrids
        {
            public Entity entity;
            public PerEntityData collisionData;

            public EntityInfoInGrids(Entity entity)
            {
                this.entity = entity;
                this.collisionData = new PerEntityData();
            }
        }

        private struct Grid
        {
            public EntityInfoInGrids[] entities;
            public int entityCount;
            public int max_entity_size;
            public float minX, minY, maxX, maxY;
            public GridStaticEntity staticEntity;
            public int[] adjacents;
            public Vector3 basePosition;
        }

        /*
         * grid position:
         * (0, 0) is the corner of a grid with grid_x = gridOffsetX, grid_y = gridOffsetY
         */
        private int gridSizeX, gridSizeY;
        private int gridOffsetX, gridOffsetY;
        private Grid[] grids;

        public void MoveEntity(Entity e, Vector3 oldPos)
        {
            int oldGrid = GetGridFromEntityPosition(oldPos);
            if (CheckEntityPositionInGrid(e.Position, oldGrid))
            {
                return;
            }
            RemoveEntityFromGrid(oldGrid, e);
            int newGrid = GetGridFromEntityPosition(e.Position);
            if (newGrid == -1)
            {
                OnEntityMoveOut(e);
            }
            else
            {
                AddEntityIntoGrid(newGrid, e);
            }
        }

        public void AddEntity(Entity e)
        {
            int newGrid = GetGridFromEntityPosition(e.Position);
            if (newGrid == -1)
            {
                throw new Exception();
            }
            AddEntityIntoGrid(newGrid, e);
            e.AddToPhysicWorld(this);
        }

        public void RemoveEntity(Entity e)
        {
            int oldGrid = GetGridFromEntityPosition(e.Position);
            RemoveEntityFromGrid(oldGrid, e);
            e.AddToPhysicWorld(null);
        }

        public void SetGridEntity(Vector3 pos, GridStaticEntity entity)
        {
            var gridId = GetGridFromEntityPosition(pos);
            if (grids[gridId].basePosition != pos)
            {
                throw new Exception();
            }
            grids[gridId].staticEntity = entity;
        }

        // basic calculation

        private int GetGridFromEntityPosition(Vector3 pos)
        {
            int gridX = (int)Math.Floor(pos.X / GridSize) + gridOffsetX;
            int gridY = (int)Math.Floor(pos.Y / GridSize) + gridOffsetY;
            return gridX + gridY * gridSizeX;
        }

        private bool CheckEntityPositionInGrid(Vector3 pos, int grid)
        {
            return pos.X >= grids[grid].minX && pos.X <= grids[grid].maxX &&
                pos.Y >= grids[grid].minY && pos.Y <= grids[grid].maxY;
        }

        // list modification

        private void RemoveEntityFromGrid(int grid, Entity entity)
        {
            var entitiesInGrid = grids[grid].entities;
            int maxEntitySize = 1; //minimum is 1
            int theEntityIdInGrid = -1;
            var entityCountInGrid = grids[grid].entityCount;
            for (int i = 0; i < entityCountInGrid; ++i)
            {
                if (entitiesInGrid[i].entity == entity)
                {
                    theEntityIdInGrid = i;
                }
                else
                {
                    maxEntitySize = Math.Max(maxEntitySize, (int)Math.Ceiling(entitiesInGrid[i].entity.MaxSize / GridSize));
                }
            }

            //check if not found
            if (theEntityIdInGrid == -1)
            {
                throw new Exception();
            }
            
            //update maxsize
            grids[grid].max_entity_size = maxEntitySize;

            //exchange/remove
            if (theEntityIdInGrid != entityCountInGrid - 1) //remove this branch?
            {
                entitiesInGrid[theEntityIdInGrid] = entitiesInGrid[entityCountInGrid - 1];
            }

            //update size
            grids[grid].entityCount -= 1;
        }

        private void AddEntityIntoGrid(int grid, Entity entity)
        {
            //check capacity
            var entityCountInGrid = grids[grid].entityCount;
            if (grids[grid].entities.Length == entityCountInGrid)
            {
                EntityInfoInGrids[] newArray = new EntityInfoInGrids[entityCountInGrid * 2];
                Array.Copy(grids[grid].entities, newArray, entityCountInGrid);
                grids[grid].entities = newArray;
            }
            ++grids[grid].entityCount;

            var entitiesInGrid = grids[grid].entities;

            //add to list
            entitiesInGrid[entityCountInGrid] = new EntityInfoInGrids(entity);

            //update max size
            var maxSizeOfEntity = (int)Math.Ceiling(entity.MaxSize / GridSize);
            if (maxSizeOfEntity > 1)
            {
                //large entity is not supported yet
                throw new Exception();
            }
            if (maxSizeOfEntity > grids[grid].max_entity_size)
            {
                grids[grid].max_entity_size = maxSizeOfEntity;
            }
        }

        private void RecalcGridRanges()
        {
            int gridId = 0;
            List<int> adjacent_list = new List<int>();
            for (int j = -gridOffsetY; j < gridSizeY - gridOffsetY; ++j)
            {
                for (int i = -gridOffsetX; i < gridSizeX - gridOffsetX; ++i)
                {
                    {
                        adjacent_list.Clear();
                        bool nx = i > -gridOffsetX, px = i < gridSizeX - gridOffsetX - 1,
                            ny = j > -gridOffsetY, py = j < gridSizeY - gridOffsetY - 1;
                        if (ny) adjacent_list.Add(gridId - gridSizeX);
                        if (py) adjacent_list.Add(gridId + gridSizeX);
                        if (nx) adjacent_list.Add(gridId - 1);
                        if (px) adjacent_list.Add(gridId + 1);
                        if (nx && ny) adjacent_list.Add(gridId - 1 - gridSizeX);
                        if (nx && py) adjacent_list.Add(gridId - 1 + gridSizeX);
                        if (px && ny) adjacent_list.Add(gridId + 1 - gridSizeX);
                        if (px && py) adjacent_list.Add(gridId + 1 + gridSizeX);
                        grids[gridId].adjacents = adjacent_list.ToArray();
                    }

                    grids[gridId].minX = i * GridSize;
                    grids[gridId].maxX = i * GridSize + GridSize;
                    grids[gridId].minY = j * GridSize;
                    grids[gridId].maxY = j * GridSize + GridSize;
                    grids[gridId].basePosition = new Vector3(grids[gridId].minX, grids[gridId].minY, 0);
                    ++gridId;
                }
            }
        }

        //others

        private void OnEntityMoveOut(Entity e)
        {
            throw new Exception();
        }


        // collision part

        private struct VelocityUpdate
        {
            public bool enable;
            public Vector3 factor;

            public void Append(Vector3 factor)
            {
                enable = true;
                this.factor *= factor;
            }

            public void Replace(Vector3 factor)
            {
                enable = true;
                this.factor = factor;
            }

            public void Clear()
            {
                enable = false;
            }
        }

        private struct Movement
        {
            public Vector3 pos;
            public Vector3 v;
        }

        private struct PerEntityData
        {
            public Movement nextMove;
            //public Vector3 nextVelocity;
            public float currentTime;
            //public bool issolving;
            public int adjustment; //1:x,2:y,4:z

            public float nextCollision;
            public Vector3 velocityUpdateFactor;
        }

        private float collision_time;


        private static bool TestDistWithMargin(ref Vector3 dist, ref Vector3 r, float margin)
        {
            return r.X > Math.Abs(dist.X) + margin && r.Y > Math.Abs(dist.Y) + margin && r.Z > Math.Abs(dist.Z) + margin;
        }

        private static Vector2 TestRange(float pos, float offset, float max)
        {
            if (max <= 0)
            {
                return new Vector2(1, 0);
            }

            if (pos < 0)
            {
                pos = -pos;
                offset = -offset;
            }
            //now pos >= 0

            float pos_offset = pos + offset;
            if (offset == 0)
            {
                if (pos >= max)
                {
                    return new Vector2(1, 0);
                }
                else
                {
                    //make a different, so that we can determine the smallest dimention
                    return new Vector2(-1, 2);
                }
            }
            else if (pos > Math.Abs(pos_offset))
            {
                //(-pos)  (<-)  0    (<-)     (pos)
                if (pos < max)
                {
                    //(-max) (-pos)  (<-)  0   (<-)   (pos)  (max)
                    return new Vector2(-1, 2);
                }
                //(-pos)     (<-)     0  (<-)  (max)  (<-)   (pos)
                float range1 = (pos - max) / -offset;
                if (range1 > 1)
                {
                    //(-pos)       0   (max)  (<-)   (pos)
                    // 0 (max)  (<-)     (pos)  
                    return new Vector2(1, 0);
                }
                // (<-)  0  (<-) (max)      (pos)  
                float range2 = (pos + max) / -offset;
                if (range2 > 1) range2 = 1;
                return new Vector2(range1, range2);
            }
            else if (offset > 0)
            {
                //   0      (pos)   (->)  
                if (pos >= max) 
                {
                    //Note that even when pos == max, we don't make it a collision (to minimize the collision number)
                    //   0   (max)   (pos)   (->)  
                    return new Vector2(1, 0);
                }
                //   0    (pos) (->)  (max)  (->)  
                float range1 = -1;
                float range2 = (max - pos) / offset;
                if (range2 > 1) range2 = 1;
                return new Vector2(range1, range2);
            }
            else
            {
                // (<-) (-pos)   0   (pos)
                if (max > Math.Abs(pos_offset))
                {
                    // (-max) (<-) (-pos)   0   (pos)  (max)
                    return new Vector2(-1, 2);
                }
                else if (max <= pos)
                {
                    //(<-) (-pos)  (-max)   0   (max)  (pos) 
                    float range1 = (pos - max) / -offset;
                    float range2 = (pos + max) / -offset;
                    return new Vector2(range1, range2);
                }
                else
                {
                    //(<-)(-max) (-pos)     0   (pos)  (max) 
                    float range1 = -1;
                    float range2 = (pos + max) / -offset;
                    return new Vector2(range1, range2);
                }
            }
        }

        private void TestEntityWithStatic(int gridId, int entityId)
        {
            TestEntityWithStatic(gridId, entityId, gridId);
            foreach (int adjacent in grids[gridId].adjacents)
            {
                TestEntityWithStatic(gridId, entityId, adjacent);
            }
        }

        private void TestEntityWithStatic(int gridIdOfEntity, int entityId, int gridId)
        {
            var theGridOfEntity = grids[gridIdOfEntity];
            var theGrid = grids[gridId];
            if (theGrid.staticEntity == null) return;

            float test_start_time = theGridOfEntity.entities[entityId].collisionData.currentTime;
            float test_end_time = 1.0f;
            float time = (test_end_time - test_start_time) * this.collision_time;
        L_start_from_start:
            //entity data
            var entityBox = theGridOfEntity.entities[entityId].entity.Collision;
            entityBox.center += theGridOfEntity.entities[entityId].collisionData.nextMove.pos;
            var offsetEntity = theGridOfEntity.entities[entityId].collisionData.nextMove.v * time;

            //grid data
            //TODO cache in grids?
            var gridBoxes = theGrid.staticEntity.CollisionArray; //should add grid pos
            var gridBoxCount = theGrid.staticEntity.CollisionCount;
            var gridPos = theGrid.staticEntity.Position;

            var result_time = 1.0f;
            var result_face = 0;

            for (int boxId = 0; boxId < gridBoxCount; ++boxId)
            {
                Box gridBox = gridBoxes[boxId];
                gridBox.center += gridPos;

                Vector3 dist = entityBox.center - gridBox.center;
                Vector3 range = gridBox.halfSize + entityBox.halfSize;

                //test if we can make adjustment
                //TODO only allow adjustment the first time
                if (theGridOfEntity.entities[entityId].collisionData.adjustment != 0)
                {
                    //can not make adjustment
                    if (TestDistWithMargin(ref dist, ref range, 0.0f))
                    {
                        //ignore
                        continue;
                    }
                }
                else
                {
                    if (TestDistWithMargin(ref dist, ref range, 0.0f))
                    {
                        //should try to make adjustment
                        const float max_margin = 0.1f;
                        if (TestDistWithMargin(ref dist, ref range, max_margin))
                        {
                            //too large, ignore this box
                            continue;
                        }

                        //safe to make it abs now
                        dist.X = Math.Abs(dist.X);
                        dist.Y = Math.Abs(dist.Y);
                        dist.Z = Math.Abs(dist.Z);

                        var adjustment = theGridOfEntity.entities[entityId].collisionData.adjustment;

                        Vector3 adjust = new Vector3();
                        if (((adjustment & 1) == 0) && range.X > dist.X && range.X < dist.X + max_margin)
                        {
                            adjust.X = (range.X - dist.X) * 1.01f * Math.Sign(entityBox.center.X - gridBox.center.X);
                            theGridOfEntity.entities[entityId].collisionData.adjustment |= 1;
                        }
                        if (((adjustment & 2) == 0) && range.Y > dist.Y && range.Y < dist.Y + max_margin)
                        {
                            adjust.Y = (range.Y - dist.Y) * 1.01f * Math.Sign(entityBox.center.Y - gridBox.center.Y);
                            theGridOfEntity.entities[entityId].collisionData.adjustment |= 2;
                        }
                        if (((adjustment & 4) == 0) && range.Z > dist.Z && range.Z < dist.Z + max_margin)
                        {
                            adjust.Z = (range.Z - dist.Z) * 1.01f * Math.Sign(entityBox.center.Z - gridBox.center.Z);
                            theGridOfEntity.entities[entityId].collisionData.adjustment |= 4;
                        }

                        theGridOfEntity.entities[entityId].collisionData.nextMove.pos += adjust;
                        entityBox.center += adjust;

                        if (boxId != 0)
                        {
                            goto L_start_from_start;
                        }
                        
                        //recalculate dist
                        dist = entityBox.center - gridBox.center;
                        //still need normal test
                    }
                    //no collision now, normal test
                }

                //normal test
                {
                    var timeX = TestRange(dist.X, offsetEntity.X, range.X);
                    var timeY = TestRange(dist.Y, offsetEntity.Y, range.Y);
                    var timeZ = TestRange(dist.Z, offsetEntity.Z, range.Z);
                    float timeMin = Math.Max(timeX.X, Math.Max(timeY.X, timeZ.X));
                    float timeMax = Math.Min(timeX.Y, Math.Min(timeY.Y, timeZ.Y));

                    if (timeMin >= timeMax)
                    {
                        //no collision on this box
                        continue;
                    }
                    if (timeMin >= result_time)
                    {
                        //not the first
                        continue;
                    }
                    //set result

                    //the algrithm we use here see Z differently.
                    if (timeMin == timeZ.X)
                    {
                        result_face = 2;
                    }
                    else if (timeMin == timeX.X && timeMin == timeY.X)
                    {
                        //neglect this time (by not modifying result_time)
                        timeMin = result_time;
                    }
                    else if (timeMin == timeX.X)
                    {
                        result_face = 0;
                    }
                    else if (timeMin == timeY.X)
                    {
                        result_face = 1;
                    }
                    result_time = timeMin < 0.0f ? 0.0f : timeMin;
                }
            }

            if (result_time < 1.0f)
            {
                var result_time_ratio = result_time * (test_end_time - test_start_time) + test_start_time;
                if (theGridOfEntity.entities[entityId].collisionData.nextCollision < result_time_ratio)
                {
                    //not the first
                    return;
                }

                Vector3 oldVelocity = new Vector3(1, 1, 1);
                if (theGridOfEntity.entities[entityId].collisionData.nextCollision == result_time_ratio)
                {
                    oldVelocity = theGridOfEntity.entities[entityId].collisionData.velocityUpdateFactor;
                }

                //add to queue
                AddCollisionToQueue(gridIdOfEntity, entityId, result_time_ratio);

                //set nextCollision
                theGridOfEntity.entities[entityId].collisionData.nextCollision = result_time_ratio;

                //set velocity update
                switch (result_face)
                {
                case 0:
                    theGridOfEntity.entities[entityId].collisionData.velocityUpdateFactor = oldVelocity * new Vector3(0, 1, 1);
                    break;
                case 1:
                    theGridOfEntity.entities[entityId].collisionData.velocityUpdateFactor = oldVelocity * new Vector3(1, 0, 1);
                    break;
                case 2:
                    theGridOfEntity.entities[entityId].collisionData.velocityUpdateFactor = oldVelocity * new Vector3(1, 1, 0);
                    break;
                }
            }
        }

        private void TestEntityWithEntity(int gridIdA, int entityIdA, int gridIdB, int entityIdB)
        {
            //no entity-entity collision
        }

        public void Step(float time)
        {
            this.collision_time = time;

            //init per entity data
            //TODO kind of optimization to avoid calculation every frame?
            for (int i = 0; i < grids.Length; ++i)
            {
                var entitiesInGrid = grids[i].entities;
                for (int j = 0; j < grids[i].entityCount; ++j)
                {
                    var entity = entitiesInGrid[j].entity;
                    
                    //this is used to check if a collision exist (also needed to update)
                    entitiesInGrid[j].collisionData.nextCollision = 1.0f;

                    var friction = -1.5f; //TODO allow different value for each entity
                    var v = entity.Velocity * (1 + friction * time) + entity.Acceloration * time;

                    entitiesInGrid[j].collisionData.nextMove.pos = entity.Position;
                    entitiesInGrid[j].collisionData.nextMove.v = v;
                    //entitiesInGrid[j].collisionData.nextVelocity = v;

                    entitiesInGrid[j].collisionData.currentTime = 0.0f;

                    //adjustment is only made each step (not each collision), so only clear outside the while loop
                    entitiesInGrid[j].collisionData.adjustment = 0;
                }
            }

            ClearCollisionQueue();

            //first cycel, check all entities
            {
                float current_time_ratio = 1.0f;

                {
                    //check pairs:
                    //  intro-grid: (1) every entity with static, (2)every pair
                    //  inter-grid: (3) inter-grid pairs

                    //(1), (2)
                    for (int gridIndex = 0; gridIndex < grids.Length; ++gridIndex)
                    {
                        for (int entityIndex_1 = 0; entityIndex_1 < grids[gridIndex].entityCount; ++entityIndex_1)
                        {
                            TestEntityWithStatic(gridIndex, entityIndex_1);
                            //for (int entityIndex_2 = entityIndex_1 + 1; entityIndex_2 < grids[gridIndex].entityCount; ++entityIndex_2)
                            //{
                            //    TestEntityWithEntity(gridIndex, entityIndex_1, gridIndex, entityIndex_2, current_time_ratio);
                            //}
                        }
                    }
                    #region (3) (not used)
                    //(3)
                    /* 
                     * collision test pair:
                     *  foreach grid:A where max_entity_range > 0
                     *      foreach EA in A
                     *      	foreach grid:B where 0 < distance(A, B) < max_entity_range + EA.entity_range
                     *      		if B.max_entity_range > A.max_entity_range then continue
                     *      		if B.max_entity_range == A.max_entity_range then
                     *      			if B.grid_id < A.grid_id continue
                     *      		foreach EB in B
                     *      			test on (EA, EB)
                     * 
                     */
                    /*
                    for (int gridXA = 0; gridXA < gridSizeX; ++gridXA)
                    {
                        for (int gridYA = 0; gridYA < gridSizeY; ++gridYA)
                        {
                            int gridIdA = (gridXA) + (gridYA) * gridSizeX;
                            var entitiesInGridA = grids[gridIdA].entities;
                            int maxEntitySizeInGridA = grids[gridIdA].max_entity_size;
                            for (int entityIdA = 0; entityIdA < entitiesInGridA; ++entityIdA)
                            {
                                int entityARange = (int)Math.Ceiling(entitiesInGridA[entityIdA].entity.MaxSize / GridSize);
                                int gridB_checkRange = maxEntitySizeInGridA + entityARange;
                                int gridB_minX = gridXA - gridB_checkRange, gridB_maxX = gridXA + gridB_checkRange;
                                int gridB_minY = gridYA - gridB_checkRange, gridB_maxY = gridYA + gridB_checkRange;
                                if (gridB_minX < 0) gridB_minX = 0;
                                if (gridB_minY < 0) gridB_minY = 0;
                                if (gridB_maxX >= gridSizeX) gridB_maxX = gridSizeX - 1;
                                if (gridB_maxY >= gridSizeY) gridB_maxY = gridSizeY - 1;
                                for (int gridXB = gridB_minX; gridXB <= gridB_maxX; ++gridXB)
                                {
                                    for (int gridYB = gridB_minY; gridYB <= gridB_maxY; ++gridYB)
                                    {
                                        {
                                            int gridABX = Math.Abs(gridXB - gridXA);
                                            int gridABY = Math.Abs(gridYB - gridYA);
                                            if (gridABX * gridABX + gridABY * gridABY > gridB_checkRange * gridB_checkRange)
                                            {
                                                continue;
                                            }
                                        }
                                        int gridIdB = (gridXB) + (gridYB) * gridSizeX;
                                        if (gridIdB == gridIdA)
                                        {
                                            continue;
                                        }
                                        if (grids[gridIdB].max_entity_size > maxEntitySizeInGridA)
                                        {
                                            continue;
                                        }
                                        if (grids[gridIdB].max_entity_size == maxEntitySizeInGridA)
                                        {
                                            if (gridIdB < gridIdA) continue;
                                        }
                                        var entitiesInGridB = grids[gridIdB].entities;
                                        for (int entityIdB = 0; entityIdB < entitiesInGridB; ++entityIdB)
                                        {
                                            //finally test the pair
                                            TestEntityWithEntity(gridIdA, entityIdA, gridIdB, entityIdB, current_time_ratio);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    */
                    #endregion
                }
            }

            CollisionQueue.Node collision = new CollisionQueue.Node();
            while (!CollisionQueueEmpty(ref collision))
            {
                var entitiesInTheGrid = grids[collision.GridId].entities;
                var lastTimeOfEntity = entitiesInTheGrid[collision.EntityId].collisionData.currentTime;

                //time
                entitiesInTheGrid[collision.EntityId].collisionData.currentTime = collision.Value;
                //pos
                entitiesInTheGrid[collision.EntityId].collisionData.nextMove.pos +=
                    entitiesInTheGrid[collision.EntityId].collisionData.nextMove.v * 
                    (collision.Value - lastTimeOfEntity) * this.collision_time;
                //velocity
                entitiesInTheGrid[collision.EntityId].collisionData.nextMove.v *=
                    entitiesInTheGrid[collision.EntityId].collisionData.velocityUpdateFactor;

                entitiesInTheGrid[collision.EntityId].collisionData.velocityUpdateFactor = new Vector3(1, 1, 1);
                entitiesInTheGrid[collision.EntityId].collisionData.nextCollision = 1.0f;

                //OK//TODO apply velocity update (only for those with endTime)
                //OK//clear collision time equals to endtime to 1

                //OK//TODO update movement
                {
                    //float startTime = this.collision_start_ratio;
                    //float movementTime = this.collision_endTime - startTime;
                    //float realtime = movementTime * time;

                    //for (int i = 0; i < entities.Count; ++i)
                    //{
                    //    perEntityData[i].nextMove.pos += perEntityData[i].nextMove.v * realtime;
                    //    perEntityData[i].nextMove.v = perEntityData[i].nextVelocity;
                    //}
                    //TODO don't calculate movement for every entity every time?
                }

                //check for secondary collision
                {
                    //check with static
                    TestEntityWithStatic(collision.GridId, collision.EntityId);
                    //TODO check with entities
                }
                //removed//this.collision_start_ratio = this.collision_endTime;

            } //iterate through each collision

            entityGridUpdates.Clear();
            //update pos and v to entity
            for (int gridId = 0; gridId < grids.Length; ++gridId)
            {
                var entitiesInGrid = grids[gridId].entities;
                for (int entityId = 0; entityId < grids[gridId].entityCount; ++entityId)
                {
                    var v = entitiesInGrid[entityId].collisionData.nextMove.v;
                    var pos = entitiesInGrid[entityId].collisionData.nextMove.pos;
                    entitiesInGrid[entityId].entity.Velocity = v;
                    var newPos = pos + v * (1.0f - entitiesInGrid[entityId].collisionData.currentTime) * this.collision_time;

                    if (!CheckEntityPositionInGrid(newPos, gridId))
                    {
                        //delay update, or it will mess the iteration
                        entityGridUpdates.Add(new UpdateEntityGridInfo
                        {
                            oldGrid = gridId,
                            entity = entitiesInGrid[entityId].entity,
                            newGrid = GetGridFromEntityPosition(newPos),
                        });
                    }
                    entitiesInGrid[entityId].entity.Position = newPos;
                }
            }

            foreach (var entityGridUpdate in entityGridUpdates)
            {
                RemoveEntityFromGrid(entityGridUpdate.oldGrid, entityGridUpdate.entity);
                if (entityGridUpdate.newGrid == -1)
                {
                    OnEntityMoveOut(entityGridUpdate.entity);
                }
                else
                {
                    AddEntityIntoGrid(entityGridUpdate.newGrid, entityGridUpdate.entity);
                }
            }
        }

        private struct UpdateEntityGridInfo
        {
            public int oldGrid;
            public Entity entity;
            public int newGrid;
        }
        private List<UpdateEntityGridInfo> entityGridUpdates = new List<UpdateEntityGridInfo>();

        #region collision queue

        private CollisionQueue collisionQueue = new CollisionQueue();

        private void ClearCollisionQueue()
        {
            collisionQueue.Clear();
        }

        private bool CollisionQueueEmpty(ref CollisionQueue.Node value)
        {
            if (collisionQueue.GetCount() == 0)
            {
                return true;
            }
            value = collisionQueue.ExtractMin();
            return false;
        }

        private void AddCollisionToQueue(int gridId, int entityId, float value)
        {
            var oldValue = grids[gridId].entities[entityId].collisionData.nextCollision;
            if (oldValue != 1.0f)
            {
                collisionQueue.Update(oldValue, gridId, entityId, value);
            }
            else
            {
                collisionQueue.Insert(gridId, entityId, value);
            }
        }

        #endregion

        private static void Main()
        {
            //GridPhysicWorld theWorld = new GridPhysicWorld(4, 0, 0, 1, 1);
            //theWorld.SetGridEntity(new Vector3(0, 0, 0), new GridStaticEntity
            //{
            //    Position = new Vector3(0, 0, 0),
            //    Collisions = new[] { 
            //        new Box { center = new Vector3(2, 2, 1), halfSize = new Vector3(1, 1, 1) },
            //    },
            //});
            //theWorld.AddEntity(new Entity()
            //{
            //    Position = new Vector3(2, 2, 5),
            //    Collision = new Box { center = new Vector3(), halfSize = new Vector3(1, 1, 1) },
            //    MaxSize = 1,
            //    Acceloration = new Vector3(0, 0, -1),
            //});
            //while (true)
            //{
            //    theWorld.Step(5);
            //}
        }
    }
}
