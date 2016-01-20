using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Lighting.Geometry
{
    struct DirHelper
    {
        public static int GetThirdDirection(int dir1, int dir2)
        {
            dir1 = dir1 % 3;
            dir2 = dir2 % 3;
            if (dir1 == dir2)
            {
                return 0 / (Math.Abs(1) - 1); //make an error
            }
            if (dir1 == dir2 + 1 || dir1 == dir2 - 2)
            {
                return dir1 + 2;
            }
            return dir1 + 1;
        }
    }

    struct Restrict1
    {
        public float Min, Max;

        public bool IsSubRangeOfThis(Restrict1 other)
        {
            return Max > Min && other.Min >= Min && other.Max <= Max;
        }

        public bool Intersect(Restrict1 other)
        {
            return other.Min < Max && other.Max > Min;
        }

        public float GetBorder(bool pn)
        {
            return pn ? Max : Min;
        }

        public void SetBorder(bool pn, float val)
        {
            if (pn) Max = val;
            else Min = val;
        }

        public float Center()
        {
            return (Max + Min) * 0.5f;
        }

        public float Size()
        {
            return Max - Min;
        }

        public Restrict1(Direction normal, Direction p1, Direction p2)
        {
            float value1 = Direction.Dot(normal, p1), value2 = Direction.Dot(normal, p2);
            Min = Math.Min(value1, value2);
            Max = Math.Max(value1, value2);
        }

        public static Restrict1 NoRestrict()
        {
            return new Restrict1 { Min = float.MinValue, Max = float.MaxValue };
        }

        public static Restrict1 Intersection(Restrict1 a, Restrict1 b)
        {
            return new Restrict1 { Min = Math.Max(a.Min, b.Min), Max = Math.Min(a.Max, b.Max) };
        }
    }

    struct Restrict3
    {
        public Restrict1 RestrictA, RestrictB, RestrictC;

        public Restrict1 this[int dir]
        {
            get
            {
                switch (dir % 3)
                {
                    case 0: return RestrictA;
                    case 1: return RestrictB;
                    case 2: return RestrictC;
                }
                return new Restrict1();
            }
            set
            {
                switch (dir % 3)
                {
                    case 0: RestrictA = value; break;
                    case 1: RestrictB = value; break;
                    case 2: RestrictC = value; break;
                }
            }
        }

        public float MaxN(int index)
        {
            switch (index % 3)
            {
                case 0: return RestrictA.Max;
                case 1: return RestrictB.Max;
                case 2: return RestrictC.Max;
            }
            return 0;
        }

        public float MinN(int index)
        {
            switch (index % 3)
            {
                case 0: return RestrictA.Min;
                case 1: return RestrictB.Min;
                case 2: return RestrictC.Min;
            }
            return 0;
        }

        public Direction Max()
        {
            return new Direction(RestrictA.Max, RestrictB.Max, RestrictC.Max);
        }

        public Direction Min()
        {
            return new Direction(RestrictA.Min, RestrictB.Min, RestrictC.Min);
        }

        public static Restrict3 Intersection(ref Restrict3 a, ref Restrict3 b)
        {
            return new Restrict3
            {
                RestrictA = Restrict1.Intersection(a.RestrictA, b.RestrictA),
                RestrictB = Restrict1.Intersection(a.RestrictB, b.RestrictB),
                RestrictC = Restrict1.Intersection(a.RestrictC, b.RestrictC),
            };
        }
    }

    struct Direction
    {
        public float X, Y, Z;

        public Direction(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Direction(int dir, bool pn)
        {
            X = Y = Z = 0;
            switch (dir % 3)
            {
                case 0: X = pn ? 1 : -1; break;
                case 1: Y = pn ? 1 : -1; break;
                case 2: Z = pn ? 1 : -1; break;
            }
        }

        public float this[int dir]
        {
            get
            {
                switch (dir % 3)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                }
                return 0;
            }
            set
            {
                switch (dir % 3)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                }
            }
        }

        public static Direction Cross(Direction left, Direction right)
        {
            return new Direction(
                left.Y * right.Z - right.Y * left.Z,
                left.Z * right.X - right.Z * left.X,
                left.X * right.Y - right.X * left.Y
                );
        }

        public static float Dot(Direction left, Direction right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        public static Direction MakeNormalized(float x, float y, float z)
        {
            var len = (float)Math.Sqrt(x * x + y * y + z * z);
            return new Direction(x / len, y / len, z / len);
        }

        public Direction Normalize()
        {
            var len = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            return new Direction(X / len, Y / len, Z / len);
        }
    }

    struct Plane
    {
        public Direction Normal;
        public int Dir;
        public bool PN;
        public float Position;

        public static void MovePlaneTo(ref Plane pl, Direction point)
        {
            pl.Position = Direction.Dot(pl.Normal, point);
        }
    }

    struct Line
    {
        public Direction Normal;
        public Direction Base;
    }

    //struct Reduce
    //{
    //    public Direction Direction;
    //    public float Speed;
    //
    //    public static void Normalize(ref Reduce reduce)
    //    {
    //
    //    }
    //}

    struct ProjectionBentSpread
    {
        public Direction LinePosition;
        public Direction Range; //3 points, min to max
        public Direction Distance;
        public int LineDir;
    }

    struct LightSpaceAdditionalInfo
    {
        public Restrict3[] RestrictBorder;

        public static LightSpaceAdditionalInfo Create()
        {
            return new LightSpaceAdditionalInfo
            {
                RestrictBorder = new Restrict3[6],
            };
        }
    }

    struct LightSpace
    {
        public LightInformation Light;
        public Restrict3 Range;

        public PlaneRegion MakeIntersection(PlaneRegion face)
        {
            return new PlaneRegion
            {
                Plane = face.Plane,
                Range = Restrict3.Intersection(ref face.Range, ref Range),
            };
        }

        public void MakeProjection(ref Rectangle rect, int lightFaceDir, int destDir, bool destPN,
            RectangleAdditionalInfo rectAdditionalInfo,
            LightSpaceAdditionalInfo spaceAdditionalInfo,
            ref PlaneRegion outRegion)
        {
            bool lightFacePN = Light.DotAxisWithNN(destDir, lightFaceDir);
            if (!destPN) lightFacePN = !lightFacePN; //TODO use a xor

            var border = spaceAdditionalInfo.RestrictBorder[lightFaceDir * 2 + (lightFacePN ? 0 : 1)];

            var planeRegion = rectAdditionalInfo.Planes[destDir * 2 + (destPN ? 0 : 1)];
            outRegion.Plane = planeRegion.Plane;

            outRegion.Range = planeRegion.Range;
            outRegion.Range[destDir] = Restrict1.Intersection(outRegion.Range[destDir], border[destDir]); //main border

            //apply output side restrictions
            //TODO check and remove this
            //if (lightFacePN)
            //{
            //    //use max(the value of the light face) as min
            //    var newMin = Range[lightFaceDir].Max;
            //    //use min on the opposite side as max (this should be moved first)
            //    var newMax = Range[lightFaceDir].Max +
            //        Light.CalculateMoveRangeOffset(lightFaceDir, destDir, rect.GetSizeAtDirection(destDir, destPN));
            //
            //    //make intersection
            //    outRegion.Range[lightFaceDir] = Restrict1.Intersection(outRegion.Range[lightFaceDir],
            //        new Restrict1 { Max = newMax, Min = newMin });
            //}
            //else
            //{
            //    var newMax = Range[lightFaceDir].Min;
            //    var newMin = Range[lightFaceDir].Min +
            //        Light.CalculateMoveRangeOffset(lightFaceDir, destDir, rect.GetSizeAtDirection(destDir, destPN));
            //
            //    outRegion.Range[lightFaceDir] = Restrict1.Intersection(outRegion.Range[lightFaceDir],
            //        new Restrict1 { Max = newMax, Min = newMin });
            //}
            Restrict1 inoutRestrict = new Restrict1();
            var theBorder = Range[lightFaceDir].GetBorder(lightFacePN);
            inoutRestrict.SetBorder(!lightFacePN, theBorder);
            inoutRestrict.SetBorder(lightFacePN, theBorder + 
                Light.CalculateMoveRangeOffset(lightFaceDir, destDir, rect.GetSizeAtDirection(destDir, destPN)));
            outRegion.Range[lightFaceDir] = Restrict1.Intersection(outRegion.Range[lightFaceDir], inoutRestrict);
        }

        public void CalculateAdditionalInfo(LightSpaceAdditionalInfo additionalInfo)
        {
            additionalInfo.RestrictBorder[0] = GetRestrictBorderInfo(0, true);
            additionalInfo.RestrictBorder[1] = GetRestrictBorderInfo(0, false);
            additionalInfo.RestrictBorder[2] = GetRestrictBorderInfo(1, true);
            additionalInfo.RestrictBorder[3] = GetRestrictBorderInfo(1, false);
            additionalInfo.RestrictBorder[4] = GetRestrictBorderInfo(2, true);
            additionalInfo.RestrictBorder[5] = GetRestrictBorderInfo(2, false);
        }

        /// <summary>
        /// Calculate the border of given side restrict in this LightSpace.
        /// The returned Restrict3 contains in other sides the range in that side according to the given side restrict.
        /// The value of the same side should not be used.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="pn"></param>
        /// <returns></returns>
        public Restrict3 GetRestrictBorderInfo(int side, bool pn)
        {
            float theBorder = Range[side].GetBorder(pn);
            Direction point = new Direction();
            point[side] = theBorder;
            
            float dir1MaxForDir2, dir1MinForDir2, dir2MaxForDir1, dir2MinForDir1;
            
            point[side + 1] = Range[side + 1].GetBorder(true);
            Light.ConvertSide(ref point, side + 2);
            dir2MaxForDir1 = point[side + 2];

            point[side + 1] = Range[side + 1].GetBorder(false);
            Light.ConvertSide(ref point, side + 2);
            dir2MinForDir1 = point[side + 2];

            var dir2Max = Math.Max(dir2MaxForDir1, dir2MinForDir1);
            var dir2Min = Math.Min(dir2MaxForDir1, dir2MinForDir1);

            point[side + 2] = Range[side + 2].GetBorder(true);
            Light.ConvertSide(ref point, side + 1);
            dir1MaxForDir2 = point[side + 1];

            point[side + 2] = Range[side + 2].GetBorder(false);
            Light.ConvertSide(ref point, side + 1);
            dir1MinForDir2 = point[side + 1];

            var dir1Max = Math.Max(dir1MaxForDir2, dir1MinForDir2);
            var dir1Min = Math.Min(dir1MaxForDir2, dir1MinForDir2);

            var ret = new Restrict3();
            ret[side + 1] = new Restrict1
            {
                Max = Math.Min(dir1Max, Range[side + 1].Max),
                Min = Math.Max(dir1Min, Range[side + 1].Min),
            };
            ret[side + 2] = new Restrict1
            {
                Max = Math.Min(dir2Max, Range[side + 2].Max),
                Min = Math.Max(dir2Min, Range[side + 2].Min),
            };
            return ret;
        }

        //given a face of LightSpace and a region of its projection, calculate the minimum distance from the LightSpace face to a given point
        public float GetNearestDistance(LightSpaceAdditionalInfo additionalInfo, int lightFaceDir, ref PlaneRegion region, Direction point)
        {
            bool lightFacePN = Light.DotAxisWithNN(region.Plane.Dir, lightFaceDir);
            if (!region.Plane.PN) lightFacePN = !lightFacePN; //TODO use a xor

            float ret = float.MaxValue;
            for (int i = 0; i < 6; ++i)
            {
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(GetBorderPoint(i), region.Plane)));
            }

            var xxyyzz = Light.GetXXYYZZFromXYZ(point);
            {
                Direction position = new Direction();
                position[region.Plane.Dir + 1] = xxyyzz[region.Plane.Dir + 1];

                position[region.Plane.Dir + 2] = Range.MaxN(region.Plane.Dir + 2);
                Light.ConvertSide(ref position, region.Plane.Dir);
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(position, region.Plane)));

                position[region.Plane.Dir + 2] = Range.MinN(region.Plane.Dir + 2);
                Light.ConvertSide(ref position, region.Plane.Dir);
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(position, region.Plane)));

                position[region.Plane.Dir] = Range.MaxN(region.Plane.Dir);
                Light.ConvertSide(ref position, region.Plane.Dir + 2);
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(position, region.Plane)));

                position[region.Plane.Dir] = Range.MinN(region.Plane.Dir);
                Light.ConvertSide(ref position, region.Plane.Dir + 2);
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(position, region.Plane)));
            }
            {
                Direction position = new Direction();
                position[region.Plane.Dir + 2] = xxyyzz[region.Plane.Dir + 2];

                position[region.Plane.Dir + 1] = Range.MaxN(region.Plane.Dir + 1);
                Light.ConvertSide(ref position, region.Plane.Dir);
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(position, region.Plane)));

                position[region.Plane.Dir + 1] = Range.MinN(region.Plane.Dir + 1);
                Light.ConvertSide(ref position, region.Plane.Dir);
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(position, region.Plane)));

                position[region.Plane.Dir] = Range.MaxN(region.Plane.Dir);
                Light.ConvertSide(ref position, region.Plane.Dir + 1);
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(position, region.Plane)));

                position[region.Plane.Dir] = Range.MinN(region.Plane.Dir);
                Light.ConvertSide(ref position, region.Plane.Dir + 1);
                ret = Math.Min(ret, GetDistance(point, Light.GetXYZFromXXYYZZ(position, region.Plane)));
            }
            return 0;
        }

        public static void GetProjectionBentSpread(LightInformation light, ref PlaneRegion projRegion, int projFaceDir, float projPosition,
            ref PlaneRegion faceRegion,
            int bentDir, bool bentPN, ref ProjectionBentSpread result)
        {
            //temp variable
            Direction point = new Direction();

            //the direction in which the line extends
            int thirdDir = DirHelper.GetThirdDirection(faceRegion.Plane.Dir, bentDir);
            result.LineDir = thirdDir;

            //Step 1
            //calculate LinePosition
            point = faceRegion.Range.Max();
            light.ConvertSide(ref point, faceRegion.Plane.Dir);
            point[bentDir] = faceRegion.Range[bentDir].GetBorder(bentPN);
            result.LinePosition = light.GetXYZFromXXYYZZ(point, faceRegion.Plane);

            //Step 2
            //calculate the three points on the line (saved in Direction)
            //two of them are the border point of the border on thirdDir, the other one should be found among borders on face.Normal

            //true: the reduce border is to the Max border on thirdDir(so only Max value of that border is used)
            bool reduceBorder = !bentPN;
            if (!light.DotAxisWithLight(thirdDir)) reduceBorder = !reduceBorder;
            if (!light.DotAxisWithLight(bentDir)) reduceBorder = !reduceBorder;

            //true: use Max border of the faceRegion (in direction face.Normal)
            bool reduceBorderPN = reduceBorder;
            if (!light.DotAxisWithNN(thirdDir, faceRegion.Plane.Dir)) reduceBorderPN = !reduceBorderPN;
            
            //use GetRestrictBorderInfo to help us (although it's not a light space)
            LightSpace lshelper = new LightSpace { Light = light, Range = projRegion.Range };
            var lineRange = lshelper.GetRestrictBorderInfo(faceRegion.Plane.Dir, reduceBorderPN);

            Restrict1 selfRange = projRegion.Range[thirdDir];
            float additionalPoint = lineRange[thirdDir].GetBorder(reduceBorder);

            //convert xxyyzz to xyz
            Direction resultRangeDir = new Direction();
            point = result.LinePosition;
            if (reduceBorder)
            {
                point[thirdDir] = selfRange.Min;
                resultRangeDir.X = light.GetXYZFromXXYYZZ(point, faceRegion.Plane)[thirdDir];
                point[thirdDir] = selfRange.Max;
                resultRangeDir.Y = light.GetXYZFromXXYYZZ(point, faceRegion.Plane)[thirdDir];
                point[thirdDir] = additionalPoint;
                resultRangeDir.Z = light.GetXYZFromXXYYZZ(point, faceRegion.Plane)[thirdDir];
            }
            else
            {
                point[thirdDir] = additionalPoint;
                resultRangeDir.X = light.GetXYZFromXXYYZZ(point, faceRegion.Plane)[thirdDir];
                point[thirdDir] = selfRange.Min;
                resultRangeDir.Y = light.GetXYZFromXXYYZZ(point, faceRegion.Plane)[thirdDir];
                point[thirdDir] = selfRange.Max;
                resultRangeDir.Z = light.GetXYZFromXXYYZZ(point, faceRegion.Plane)[thirdDir];
            }
            result.Range = resultRangeDir;

            //Step 3
            //calculate distance
            point = result.LinePosition;

            //TODO store additional info
            var lsInfo = LightSpaceAdditionalInfo.Create();
            lshelper.CalculateAdditionalInfo(lsInfo);

            point[thirdDir] = resultRangeDir.X;
            result.Distance.X = lshelper.GetNearestDistance(lsInfo, projFaceDir, ref faceRegion, point);
            point[thirdDir] = resultRangeDir.Y;
            result.Distance.Y = lshelper.GetNearestDistance(lsInfo, projFaceDir, ref faceRegion, point);
            point[thirdDir] = resultRangeDir.Z;
            result.Distance.Z = lshelper.GetNearestDistance(lsInfo, projFaceDir, ref faceRegion, point);
        }

        private float GetDistance(Direction p1, Direction p2)
        {
            return Math.Abs(p1.X - p2.X) + Math.Abs(p1.Y - p2.Y) + Math.Abs(p1.Z - p2.Z);
        }

        //get a border point (which is a line in space) of this LightSpace in xxyyzz
        private Direction GetBorderPoint(int index)
        {
            int dir1, dir2, dir3;
            bool pn1, pn2;
            Light.GetBorderPointForLightSpace(index, out dir1, out pn1, out dir2, out pn2, out dir3);
            Direction ret = new Direction();
            ret[dir1] = Range[dir1].GetBorder(pn1);
            ret[dir2] = Range[dir2].GetBorder(pn2);
            Light.ConvertSide(ref ret, dir3);
            return ret;
        }
    }

    struct RectangleAdditionalInfo
    {
        public PlaneRegion[] Planes;

        public static RectangleAdditionalInfo Create()
        {
            return new RectangleAdditionalInfo { Planes = new PlaneRegion[6] };
        }
    }

    struct Rectangle
    {
        public Restrict3 Range; //note that this range is expressed in XYZ, not XXYYZZ

        public Plane GetFace(int dir, bool pn)
        {
            Plane pl = new Plane() { Normal = new Direction(dir, pn), Dir = dir, PN = pn };
            Direction point = new Direction();
            point[dir] = Range[dir].GetBorder(pn);
            Plane.MovePlaneTo(ref pl, point);
            return pl;
        }

        public PlaneRegion GetFaceRegion(LightInformation light, int dir, bool pn)
        {
            Plane pl = GetFace(dir, pn);

            float coordDir = Range[dir].GetBorder(pn);

            Direction minmin = new Direction();
            minmin[dir] = coordDir;
            minmin[dir + 1] = Range[dir + 1].Min;
            minmin[dir + 2] = Range[dir + 2].Min;
            Direction maxmax = new Direction();
            maxmax[dir] = coordDir;
            maxmax[dir + 1] = Range[dir + 1].Max;
            maxmax[dir + 2] = Range[dir + 2].Max;

            Restrict3 range = new Restrict3();
            range[dir] = Restrict1.NoRestrict();
            range[dir + 1] = new Restrict1(light.AxisCross(dir + 1), minmin, maxmax);
            range[dir + 2] = new Restrict1(light.AxisCross(dir + 2), minmin, maxmax);

            return new PlaneRegion { Plane = pl, Range = range };
        }

        public void CalculateAdditionalInfo(LightInformation light, RectangleAdditionalInfo additionalInfo)
        {
            additionalInfo.Planes[0] = GetFaceRegion(light, 0, true);
            additionalInfo.Planes[1] = GetFaceRegion(light, 0, false);
            additionalInfo.Planes[2] = GetFaceRegion(light, 1, true);
            additionalInfo.Planes[3] = GetFaceRegion(light, 1, false);
            additionalInfo.Planes[4] = GetFaceRegion(light, 2, true);
            additionalInfo.Planes[5] = GetFaceRegion(light, 2, false);
        }

        public float GetSizeAtDirection(int dir, bool pn)
        {
            var ret = Range[dir].Size();
            return pn ? ret : -ret;
        }
    }

    struct PlaneRegion
    {
        public Plane Plane;
        public Restrict3 Range; //in XXYYZZ

        public LightSpace MakeLight(LightInformation light)
        {
            return new LightSpace { Light = light, Range = Range };
        }
    }

    class LightInformation
    {
        private struct BorderInfo
        {
            public int dir1, dir2, dir3;
            public bool pn1, pn2;
        }

        //should be normalized
        public Direction Dir;

        //should be normalized
        private Direction xx, yy, zz;

        //used in ConvertSide
        private Restrict3 restrictCoefficient;

        private BorderInfo[] borderInfo;

        public Direction AxisCross(int dir)
        {
            switch (dir % 3)
            {
                case 0: return xx;
                case 1: return yy;
                case 2: return zz;
            }
            return new Direction();
        }

        public LightInformation(Direction dir)
        {
            Dir = dir.Normalize();
            xx = Direction.Cross(dir, new Direction(1, 0, 0)).Normalize();
            yy = Direction.Cross(dir, new Direction(0, 1, 0)).Normalize();
            zz = Direction.Cross(dir, new Direction(0, 0, 1)).Normalize();

            float dotXY = Direction.Dot(xx, yy), dotYZ = Direction.Dot(yy, zz), dotZX = Direction.Dot(zz, xx);
            double angXY = Math.Acos(Math.Abs(dotXY)), angYZ = Math.Acos(Math.Abs(dotYZ)), angZX = Math.Acos(Math.Abs(dotZX));

            restrictCoefficient = new Restrict3();
            {
                //X
                double coXY = Math.Sin(angZX) / Math.Sin(angYZ) * Math.Sign(dotXY);
                double coXZ = Math.Sin(angXY) / Math.Sin(angYZ) * Math.Sign(dotZX);
                restrictCoefficient.RestrictA.Min = (float)coXY;
                restrictCoefficient.RestrictA.Max = (float)coXZ;
            }
            {
                //Y
                double coYZ = Math.Sin(angXY) / Math.Sin(angZX) * Math.Sign(dotYZ);
                double coYX = Math.Sin(angYZ) / Math.Sin(angZX) * Math.Sign(dotXY);
                restrictCoefficient.RestrictB.Min = (float)coYZ;
                restrictCoefficient.RestrictB.Max = (float)coYX;
            }
            {
                //Z
                double coZX = Math.Sin(angYZ) / Math.Sin(angXY) * Math.Sign(dotZX);
                double coZY = Math.Sin(angZX) / Math.Sin(angXY) * Math.Sign(dotYZ);
                restrictCoefficient.RestrictC.Min = (float)coZX;
                restrictCoefficient.RestrictC.Max = (float)coZY;
            }
            borderInfo = new BorderInfo[]
            {
                CalculateBorderInfo(0, true),
                CalculateBorderInfo(0, false),
                CalculateBorderInfo(1, true),
                CalculateBorderInfo(1, false),
                CalculateBorderInfo(2, true),
                CalculateBorderInfo(2, false),
            };
        }

        private BorderInfo CalculateBorderInfo(int dir, bool pn)
        {
            BorderInfo ret = new BorderInfo();
            ret.dir1 = dir;
            ret.pn1 = pn;
            ret.dir2 = dir + 1;
            ret.pn2 = Direction.Dot(AxisCross(dir), AxisCross(dir + 1)) > 0 ? pn : !pn;
            ret.dir3 = dir + 2;
            return ret;
        }

        //modify the destSide of dir, in order to make the following three plans intersect on a single line
        //  r . xx = dir.X
        //  r . yy = dir.Y
        //  r . zz = dir.Z
        public void ConvertSide(ref Direction dir, int destSide)
        {
            Restrict1 co = restrictCoefficient[destSide];
            dir[destSide] = dir[destSide + 1] * co.Min + dir[destSide + 2] * co.Max;
        }

        public bool DotAxisWithNN(int axisDir, int nndir)
        {
            return Direction.Dot(new Direction(axisDir, true), AxisCross(nndir)) > 0; //TODO cache
        }

        public bool DotAxisWithLight(int axisDir)
        {
            return Direction.Dot(new Direction(axisDir, true), Dir) > 0;
        }

        public float CalculateMoveRangeOffset(int lightFaceDir, int axisDir, float axisOffset)
        {
            return AxisCross(lightFaceDir)[axisDir] * axisOffset;
        }

        //get the position of a xxyyzz point (a light line) on a plane, in xyz form
        public Direction GetXYZFromXXYYZZ(Direction xxyyzz, Plane pl)
        {
            //TODO check for faster method
            var pointOnXXYYZZ = GetPointOnXXYYZZ(xxyyzz);
            var pointOnPlane = new Direction(pl.Normal.X * pl.Position, pl.Normal.Y * pl.Position, pl.Normal.Z * pl.Position);

            //  [ Point(return) - Point(pointOnPlane) ] dot Direction(pl.Normal) = 0
            //  [ [ Point(pointOnXXYYZZ) + k * Direction(Light) ] - Point(pointOnPlane) ] dot Direction(pl.Normal) = 0
            //  [ Point(pointOnXXYYZZ) + k * Direction(Light)  - Point(pointOnPlane) ] dot Direction(pl.Normal) = 0
            //  [ Point(pointOnPlane) - Point(pointOnXXYYZZ) ] dot Direction(pl.Normal) = k * [ Direction(Light) dot Direction(pl.Normal) ]
            var pppp = new Direction(pointOnPlane.X - pointOnXXYYZZ.X, pointOnPlane.Y - pointOnXXYYZZ.Y, pointOnPlane.Z - pointOnXXYYZZ.Z);
            var k = Direction.Dot(pppp, pl.Normal) / Direction.Dot(Dir, pl.Normal);

            return new Direction(
                pointOnXXYYZZ.X + Dir.X * k,
                pointOnXXYYZZ.Y + Dir.Y * k,
                pointOnXXYYZZ.Z + Dir.Z * k);
        }

        private Direction GetPointOnXXYYZZ(Direction xxyyzz)
        {
            if (Direction.Dot(xx, yy) < 0.05f)
            {
                //use x, z
                Direction pxx = GetPointOnFace(0, xxyyzz.X);
                Direction pzz = GetPointOnFace(1, xxyyzz.Z);
                Direction diffxxyy = new Direction(pxx.X - pzz.X, pxx.Y - pzz.Y, pxx.Z - pzz.Z);
                var move = Direction.Cross(zz, Direction.Cross(diffxxyy, zz));
                return new Direction(pzz.X + move.X, pzz.Y + move.Y, pzz.Z + move.Z);
            }
            else
            {
                //use x, y
                Direction pxx = GetPointOnFace(0, xxyyzz.X);
                Direction pyy = GetPointOnFace(1, xxyyzz.Y);
                Direction diffxxyy = new Direction(pxx.X - pyy.X, pxx.Y - pyy.Y, pxx.Z - pyy.Z);
                var move = Direction.Cross(yy, Direction.Cross(diffxxyy, yy));
                return new Direction(pyy.X + move.X, pyy.Y + move.Y, pyy.Z + move.Z);
            }
        }

        //get one point on the plane
        private Direction GetPointOnFace(int dir, float nn)
        {
            var ret = AxisCross(dir);
            ret.X *= nn;
            ret.Y *= nn;
            ret.Z *= nn;
            return ret;
        }

        //get the xxyyzz form of a point
        public Direction GetXXYYZZFromXYZ(Direction point)
        {
            Direction ret = new Direction();
            Plane pl = new Plane();

            pl.Normal = xx;
            Plane.MovePlaneTo(ref pl, point);
            ret.X = pl.Position;

            pl.Normal = yy;
            Plane.MovePlaneTo(ref pl, point);
            ret.Y = pl.Position;

            pl.Normal = zz;
            Plane.MovePlaneTo(ref pl, point);
            ret.Z = pl.Position;

            return ret;
        }

        public void GetBorderPointForLightSpace(int index, out int dir1, out bool pn1, out int dir2, out bool pn2, out int dir3)
        {
            var ret = borderInfo[index];
            dir1 = ret.dir1;
            pn1 = ret.pn1;
            dir2 = ret.dir2;
            pn2 = ret.pn2;
            dir3 = ret.dir3;
        }
    }

}
