using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sandbox.GameScene.Lighting.Geometry
{
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
            Direction point = new Direction();
            point[side] = Range[side].GetBorder(pn);
            
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
        public Restrict3 Range;

        public LightSpace MakeLight(LightInformation light)
        {
            return new LightSpace { Light = light, Range = Range };
        }
    }

    class LightInformation
    {
        public Direction Dir;
        private Direction xx, yy, zz;

        //used in ConvertSide
        private Restrict3 restrictCoefficient;

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

        public float CalculateMoveRangeOffset(int lightFaceDir, int axisDir, float axisOffset)
        {
            return AxisCross(lightFaceDir)[axisDir] * axisOffset;
        }
    }
}
