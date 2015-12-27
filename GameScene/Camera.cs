using Sandbox.Physics;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sandbox.GameScene
{
    class Camera : Sandbox.Physics.Entity
    {
        public float yaw, pitch;
        private Vector3 eye_offset;
        private Dictionary<Keys, bool> keys;

        public Camera(Vector3 pos)
        {
            this.Position = pos;
            this.Collision = new Box { center = new Vector3(), halfSize = new Vector3(1.0f, 1.0f, 2.0f) };
            this.MaxSize = 1;
            this.Acceloration = new Vector3(0, 0, -30);
            this.eye_offset = new Vector3(0.0f, 0.0f, 1.45f);

            InitStairTest();
        }

        private Vector3 CalcOffset()
        {
            Vector4 offset = new Vector4(-1, 0, 0, 0);
            Vector4 x = new Vector4(0, 1, 0, 0);
            offset = Vector4.Transform(offset, Matrix.RotationZ(yaw));
            x = Vector4.Transform(x, Matrix.RotationZ(yaw));
            pitch = Math.Min((float)Math.PI / 2.001f, pitch);
            pitch = Math.Max((float)Math.PI / -2.001f, pitch);
            offset = Vector4.Transform(offset, Matrix.RotationAxis(new Vector3(x.X, x.Y, x.Z), -pitch));
            return new Vector3(offset.X, offset.Y, offset.Z);
        }

        public Matrix GetViewMatrix()
        {
            return Matrix.LookAtLH(Position + eye_offset, Position + eye_offset + CalcOffset(), Vector3.UnitZ);
        }

        public Vector3 MoveHorizontal(Vector4 b)
        {
            Vector4 move = b;
            move = Vector4.Transform(move, Matrix.RotationZ(yaw));
            return new Vector3(move.X, move.Y, move.Z);
        }

        public Vector3 MoveHorizontal(float x, float y)
        {
            return MoveHorizontal(new Vector4(x, y, 0, 0));
        }

        public void SetForm(Form form)
        {
            keys = new Dictionary<Keys, bool>() {
                        { Keys.W, false }, { Keys.S, false }, { Keys.A, false }, { Keys.D, false },
                        { Keys.Up, false }, { Keys.Down, false }, { Keys.Left, false }, { Keys.Right, false },
                        { Keys.Q, false }, { Keys.E, false },
                        { Keys.Space, false },
                    };
            //TODO sync!
            form.KeyDown += delegate(object obj, KeyEventArgs e)
            {
                if (keys.ContainsKey(e.KeyCode)) keys[e.KeyCode] = true;
            };
            form.KeyUp += delegate(object obj, KeyEventArgs e)
            {
                if (keys.ContainsKey(e.KeyCode)) keys[e.KeyCode] = false;
            };
        }

        public void Step()
        {
            Vector3 acc;
            Vector4 movedir = new Vector4();
            if (keys[Keys.W]) movedir.X -= 1;
            if (keys[Keys.S]) movedir.X += 1;
            if (keys[Keys.A]) movedir.Y += 1;
            if (keys[Keys.D]) movedir.Y -= 1;
            movedir.Normalize();
            acc = MoveHorizontal(movedir) * 30.0f;
            acc.Z = Acceloration.Z;

            if (keys[Keys.Up]) pitch -= 0.03f;
            if (keys[Keys.Down]) pitch += 0.03f;
            if (keys[Keys.Left]) yaw -= 0.03f;
            if (keys[Keys.Right]) yaw += 0.03f;
            Acceloration = acc;

            if (acc.X == 0 && acc.Y == 0)
            {
                //var v = Velocity;
                //v.X *= 0.8f;
                //v.Y *= 0.8f;
                //Velocity = v;
            }
            if (keys[Keys.Space])
            {
                Velocity.Z += 1;
            }

            StepStairTest();
        }

        private AdditionalCollision existTest, nearEmptyTest, upperEmptyTest;

        private void InitStairTest()
        {
            float maxHeight = 0.5f; //ratio to halfsize
            float emptyHeight = 2.0f - maxHeight;

            existTest = new AdditionalCollision()
            {
                Type = AdditionalCollisionType.StaticCollision,
                Box = new Box
                {
                    center = new Vector3(0, 0, this.Collision.halfSize.Z * (maxHeight / 2 - 1.0f)),
                    halfSize = new Vector3(this.Collision.halfSize.X, this.Collision.halfSize.Y, this.Collision.halfSize.Z * maxHeight / 2),
                },
            };
            nearEmptyTest = new AdditionalCollision()
            {
                Type = AdditionalCollisionType.StaticCollision,
                Box = new Box
                {
                    center = new Vector3(0, 0, this.Collision.halfSize.Z * (maxHeight / 2 - 1.0f)),
                    halfSize = new Vector3(this.Collision.halfSize.X, this.Collision.halfSize.Y, this.Collision.halfSize.Z * maxHeight / 2),
                },
            };
            upperEmptyTest = new AdditionalCollision()
            {
                Type = AdditionalCollisionType.StaticCollision,
                Box = new Box
                {
                    center = new Vector3(0, 0, this.Collision.halfSize.Z * (1.0f - emptyHeight / 2)),
                    halfSize = new Vector3(this.Collision.halfSize.X, this.Collision.halfSize.Y, this.Collision.halfSize.Z * emptyHeight / 2),
                },
            };

            this.AdditionalCollisionList.Add(existTest);
            this.AdditionalCollisionList.Add(nearEmptyTest);
            this.AdditionalCollisionList.Add(upperEmptyTest);
        }

        private int jumpCount = 0;
        private void StepStairTest()
        {
            if (this.Acceloration.X != 0)
            {

            }

            float offsetRatioMax = 0.5f, offsetRatioMin = 0.3f;

            Vector3 offsetMax = offsetRatioMax * new Vector3(Acceloration.X, Acceloration.Y, 0) / 70.0f * 2.0f,
                offsetMin = offsetRatioMin * new Vector3(Acceloration.X, Acceloration.Y, 0) / 70.0f * 2.0f;
            existTest.Box.center.X = offsetMax.X;
            existTest.Box.center.Y = offsetMax.Y;
            nearEmptyTest.Box.center.X = offsetMin.X;
            nearEmptyTest.Box.center.Y = offsetMin.Y;
            upperEmptyTest.Box.center.X = offsetMax.X;
            upperEmptyTest.Box.center.Y = offsetMax.Y;

            if (jumpCount == 0)
            {
                if (existTest.Result && !upperEmptyTest.Result)
                {
                    jumpCount = 2;
                }
                else if (existTest.Result)
                {

                }
            }
            else
            {
                if (nearEmptyTest.Result || jumpCount != 2)
                {
                    --jumpCount;
                    this.Velocity.Z = 9.0f;
                }
            }
            existTest.Result = false;
            nearEmptyTest.Result = false;
            upperEmptyTest.Result = false;
        }
    }
}
