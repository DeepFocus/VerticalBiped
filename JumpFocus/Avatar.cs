using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MK = Microsoft.Kinect;

namespace JumpFocus
{
    class Avatar
    {
        private World _world;

        private const float ArmDensity = 10;
        private const float LegDensity = 15;
        private const float LimbAngularDamping = 7;

        private Body _torso;
        private RectangleGeometry _torsoGeo;
        private Body _head;
        private EllipseGeometry _headGeo;

        private Body _lowerLeftArm;
        private RectangleGeometry _lowerLeftArmGeo;
        private Body _lowerLeftLeg;
        private RectangleGeometry _lowerLeftLegGeo;
        private Body _lowerRightArm;
        private RectangleGeometry _lowerRightArmGeo;
        private Body _lowerRightLeg;
        private RectangleGeometry _lowerRightLegGeo;

        private Body _upperLeftArm;
        private RectangleGeometry _upperLeftArmGeo;
        private Body _upperLeftLeg;
        private RectangleGeometry _upperLeftLegGeo;
        private Body _upperRightArm;
        private RectangleGeometry _upperRightArmGeo;
        private Body _upperRightLeg;
        private RectangleGeometry _upperRightLegGeo;

        private RevoluteJoint _jRightArm;
        private RevoluteJoint _jRightArmBody;
        private RevoluteJoint _jLeftArm;
        private RevoluteJoint _jLeftArmBody;
        private RevoluteJoint _jLeftLeg;
        private RevoluteJoint _jLeftLegBody;
        private RevoluteJoint _jRightLeg;
        private RevoluteJoint _jRightLegBody;

        public Rect BodyCenter { get { return _torsoGeo.Rect; } }

        //Jump related
        private MK.CameraSpacePoint _previousPosition;
        public bool hasJumped { get; private set; }
        public float verticalSpeed { get; private set; }

        public Avatar(World World, Vector2 position)
        {
            _world = World;
            CreateBody(position);
            CreateJoints();
        }

        ~Avatar()
        {
            _world.RemoveBody(_head);
            _world.RemoveBody(_torso);
            _world.RemoveBody(_lowerLeftArm);
            _world.RemoveBody(_lowerLeftLeg);
            _world.RemoveBody(_lowerRightArm);
            _world.RemoveBody(_lowerRightLeg);
            _world.RemoveBody(_upperLeftArm);
            _world.RemoveBody(_upperLeftLeg);
            _world.RemoveBody(_upperRightArm);
            _world.RemoveBody(_upperRightLeg);
        }

        private void CreateBody(Vector2 position)
        {
            //Head
            _head = BodyFactory.CreateCircle(_world, 0.9f, 10f);
            _head.BodyType = BodyType.Dynamic;
            _head.AngularDamping = LimbAngularDamping;
            _head.Mass = 2f;
            _head.Position = position;

            _headGeo = new EllipseGeometry(
                new Point
                {
                    X = ConvertUnits.ToDisplayUnits(_head.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_head.Position.Y)
                },
                ConvertUnits.ToDisplayUnits(0.9f),
                ConvertUnits.ToDisplayUnits(0.9f)
            );

            //Torso
            //_torso = BodyFactory.CreateRoundedRectangle(_world, 2f, 4f, 0.5f, 0.7f, 2, 10f);
            _torso = BodyFactory.CreateRectangle(_world, 2f, 4f, 10f);
            //_torso.BodyType = BodyType.Dynamic;
            //_torso.BodyType = BodyType.Kinematic;
            _torso.Mass = 5f;
            _torso.Position = position + new Vector2(0f, 3f);

            _torsoGeo = new RectangleGeometry(
                new Rect
                {
                    //The center is the center in Geometry, not the top left
                    X = ConvertUnits.ToDisplayUnits(_torso.Position.X) + 1f,
                    Y = ConvertUnits.ToDisplayUnits(_torso.Position.Y) + 2f,
                    Width = ConvertUnits.ToDisplayUnits(2f),
                    Height = ConvertUnits.ToDisplayUnits(4f)
                }
            );

            //_torsoGeo = new RectangleGeometry(
            //    new Rect
            //    {
            //        //The center is the center in Geometry, not the top left
            //        X = ConvertUnits.ToDisplayUnits(_torso.Position.X) + 1f,
            //        Y = ConvertUnits.ToDisplayUnits(_torso.Position.Y) + 2f,
            //        Width = ConvertUnits.ToDisplayUnits(2f),
            //        Height = ConvertUnits.ToDisplayUnits(4f)
            //    },
            //    ConvertUnits.ToDisplayUnits(0.5f),
            //    ConvertUnits.ToDisplayUnits(0.7f)
            //);

            //Left Arm
            _lowerLeftArm = BodyFactory.CreateRectangle(_world, 0.45f, 1f, ArmDensity);
            _lowerLeftArm.BodyType = BodyType.Dynamic;
            _lowerLeftArm.AngularDamping = LimbAngularDamping;
            _lowerLeftArm.Mass = 2f;
            _lowerLeftArm.Rotation = 1.4f;
            _lowerLeftArm.Position = position + new Vector2(-4f, 2.2f);

            _lowerLeftArmGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_lowerLeftArm.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_lowerLeftArm.Position.Y),
                    Width = ConvertUnits.ToDisplayUnits(0.45f),
                    Height = ConvertUnits.ToDisplayUnits(1f)
                }
            );

            _upperLeftArm = BodyFactory.CreateRectangle(_world, 0.45f, 1f, ArmDensity);
            _upperLeftArm.BodyType = BodyType.Dynamic;
            _upperLeftArm.AngularDamping = LimbAngularDamping;
            _upperLeftArm.Mass = 2f;
            _upperLeftArm.Rotation = 1.4f;
            _upperLeftArm.Position = position + new Vector2(-2f, 1.8f);

            _upperLeftArmGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_upperLeftArm.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_upperLeftArm.Position.Y),
                    Width = ConvertUnits.ToDisplayUnits(0.45f),
                    Height = ConvertUnits.ToDisplayUnits(1f)
                }
            );

            //Right Arm
            _lowerRightArm = BodyFactory.CreateRectangle(_world, 0.45f, 1f, ArmDensity);
            _lowerRightArm.BodyType = BodyType.Dynamic;
            _lowerRightArm.AngularDamping = LimbAngularDamping;
            _lowerRightArm.Mass = 2f;
            _lowerRightArm.Rotation = -1.4f;
            _lowerRightArm.Position = position + new Vector2(4f, 2.2f);

            _lowerRightArmGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_lowerRightArm.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_lowerRightArm.Position.Y),
                    Width = ConvertUnits.ToDisplayUnits(0.45f),
                    Height = ConvertUnits.ToDisplayUnits(1f)
                }
            );

            _upperRightArm = BodyFactory.CreateRectangle(_world, 0.45f, 1f, ArmDensity);
            _upperRightArm.BodyType = BodyType.Dynamic;
            _upperRightArm.AngularDamping = LimbAngularDamping;
            _upperRightArm.Mass = 2f;
            _upperRightArm.Rotation = -1.4f;
            _upperRightArm.Position = position + new Vector2(2f, 1.8f);

            _upperRightArmGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_upperRightArm.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_upperRightArm.Position.Y),
                    Width = ConvertUnits.ToDisplayUnits(0.45f),
                    Height = ConvertUnits.ToDisplayUnits(1f)
                }
            );

            //Left Leg
            _lowerLeftLeg = BodyFactory.CreateRectangle(_world, 0.5f, 1f, LegDensity);
            _lowerLeftLeg.BodyType = BodyType.Dynamic;
            _lowerLeftLeg.AngularDamping = LimbAngularDamping;
            _lowerLeftLeg.Mass = 2f;
            _lowerLeftLeg.Position = position + new Vector2(-0.6f, 8f);

            _lowerLeftLegGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_lowerLeftLeg.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_lowerLeftLeg.Position.Y),
                    Width = ConvertUnits.ToDisplayUnits(0.5f),
                    Height = ConvertUnits.ToDisplayUnits(1f)
                }
            );

            _upperLeftLeg = BodyFactory.CreateRectangle(_world, 0.5f, 1f, LegDensity);
            _upperLeftLeg.BodyType = BodyType.Dynamic;
            _upperLeftLeg.AngularDamping = LimbAngularDamping;
            _upperLeftLeg.Mass = 2f;
            _upperLeftLeg.Position = position + new Vector2(-0.6f, 6f);

            _upperLeftLegGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_upperLeftLeg.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_upperLeftLeg.Position.Y),
                    Width = ConvertUnits.ToDisplayUnits(0.5f),
                    Height = ConvertUnits.ToDisplayUnits(1f)
                }
            );

            //Right Leg
            _lowerRightLeg = BodyFactory.CreateRectangle(_world, 0.5f, 1f, LegDensity);
            _lowerRightLeg.BodyType = BodyType.Dynamic;
            _lowerRightLeg.AngularDamping = LimbAngularDamping;
            _lowerRightLeg.Mass = 2f;
            _lowerRightLeg.Position = position + new Vector2(0.6f, 8f);

            _lowerRightLegGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_lowerRightLeg.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_lowerRightLeg.Position.Y),
                    Width = ConvertUnits.ToDisplayUnits(0.5f),
                    Height = ConvertUnits.ToDisplayUnits(1f)
                }
            );

            _upperRightLeg = BodyFactory.CreateRectangle(_world, 0.5f, 1f, LegDensity);
            _upperRightLeg.BodyType = BodyType.Dynamic;
            _upperRightLeg.AngularDamping = LimbAngularDamping;
            _upperRightLeg.Mass = 2f;
            _upperRightLeg.Position = position + new Vector2(0.6f, 6f);

            _upperRightLegGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_upperRightLeg.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_upperRightLeg.Position.Y),
                    Width = ConvertUnits.ToDisplayUnits(0.5f),
                    Height = ConvertUnits.ToDisplayUnits(1f)
                }
            );
        }

        private void CreateJoints()
        {
            //head -> body
            RevoluteJoint jHeadBody = new RevoluteJoint(_head, _torso,
                                                        new Vector2(0f, 1f),
                                                        new Vector2(0f, -2f));
            jHeadBody.CollideConnected = false;
            jHeadBody.MotorEnabled = false;
            _world.AddJoint(jHeadBody);

            //lowerLeftArm -> upperLeftArm
            _jLeftArm = new RevoluteJoint(_lowerLeftArm, _upperLeftArm,
                                                       new Vector2(0f, -1f),
                                                       new Vector2(0f, 1f));
            _jLeftArm.CollideConnected = false;
            _jLeftArm.MotorEnabled = true;
            _world.AddJoint(_jLeftArm);

            //upperLeftArm -> body
            _jLeftArmBody = new RevoluteJoint(_upperLeftArm, _torso,
                                                           new Vector2(0f, -1f),
                                                           new Vector2(-1f, -1.5f));
            _jLeftArmBody.CollideConnected = false;
            _jLeftArmBody.MotorEnabled = true;
            _world.AddJoint(_jLeftArmBody);

            //lowerRightArm -> upperRightArm
            _jRightArm = new RevoluteJoint(_lowerRightArm, _upperRightArm,
                                                        new Vector2(0f, -1f),
                                                        new Vector2(0f, 1f));
            _jRightArm.CollideConnected = false;
            _jRightArm.MotorEnabled = true;
            _world.AddJoint(_jRightArm);

            //upperRightArm -> body
            _jRightArmBody = new RevoluteJoint(_upperRightArm, _torso,
                                                            new Vector2(0f, -1f),
                                                            new Vector2(1f, -1.5f));
            _jRightArmBody.CollideConnected = false;
            _jRightArmBody.MotorEnabled = true;
            _world.AddJoint(_jRightArmBody);

            //lowerLeftLeg -> upperLeftLeg
            _jLeftLeg = new RevoluteJoint(_lowerLeftLeg, _upperLeftLeg,
                                                       new Vector2(0f, -1.1f),
                                                       new Vector2(0f, 1f));
            _jLeftLeg.CollideConnected = false;
            _jLeftLeg.MotorEnabled = true;
            _world.AddJoint(_jLeftLeg);

            //upperLeftLeg -> body
            _jLeftLegBody = new RevoluteJoint(_upperLeftLeg, _torso,
                                                           new Vector2(0f, -1.1f),
                                                           new Vector2(-0.8f, 1.9f));
            _jLeftLegBody.CollideConnected = false;
            _jLeftLegBody.MotorEnabled = true;
            _world.AddJoint(_jLeftLegBody);

            //lowerRightleg -> upperRightleg
            _jRightLeg = new RevoluteJoint(_lowerRightLeg, _upperRightLeg,
                                                        new Vector2(0f, -1.1f),
                                                        new Vector2(0f, 1f));
            _jRightLeg.CollideConnected = false;
            _jRightLeg.MotorEnabled = true;
            _world.AddJoint(_jRightLeg);

            //upperRightleg -> body
            _jRightLegBody = new RevoluteJoint(_upperRightLeg, _torso,
                                                            new Vector2(0f, -1.1f),
                                                            new Vector2(0.8f, 1.9f));
            _jRightLegBody.CollideConnected = false;
            _jRightLegBody.MotorEnabled = true;
            _world.AddJoint(_jRightLegBody);
        }

        public void Draw(DrawingContext dc)
        {
            var black = Color.FromArgb(255, 0, 0, 0);
            var red = Color.FromArgb(255, 255, 0, 0);

            var background = new SolidColorBrush(black);
            var brush = new SolidColorBrush(red);

            if (double.IsNaN(_head.Position.X) || double.IsNaN(_head.Position.Y))
            {
                return;
            }

            _headGeo.Center = new Point
            {
                X = ConvertUnits.ToDisplayUnits(_head.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_head.Position.Y)
            };
            dc.DrawGeometry(brush, null, _headGeo);

            _torsoGeo.Rect = new Rect
            {
                //The center is the center in Geometry, not the top left
                X = ConvertUnits.ToDisplayUnits(_torso.Position.X) - _torsoGeo.Rect.Width / 2,
                Y = ConvertUnits.ToDisplayUnits(_torso.Position.Y) - _torsoGeo.Rect.Height / 2,
                Width = _torsoGeo.Rect.Width,
                Height = _torsoGeo.Rect.Height
            };
            _torsoGeo.Transform = new RotateTransform(
                MathHelper.ToDegrees(_torso.Rotation),
                ConvertUnits.ToDisplayUnits(_torso.Position.X),
                ConvertUnits.ToDisplayUnits(_torso.Position.Y));
            dc.DrawGeometry(brush, null, _torsoGeo);

            _upperLeftArmGeo.Rect = new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_upperLeftArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_upperLeftArm.Position.Y),
                Width = _upperLeftArmGeo.Rect.Width,
                Height = _upperLeftArmGeo.Rect.Height
            };
            _upperLeftArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_upperLeftArm.Rotation), _upperLeftArmGeo.Rect.X, _upperLeftArmGeo.Rect.Y);
            dc.DrawGeometry(brush, null, _upperLeftArmGeo);

            _lowerLeftArmGeo.Rect = new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_lowerLeftArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_lowerLeftArm.Position.Y),
                Width = _lowerLeftArmGeo.Rect.Width,
                Height = _lowerLeftArmGeo.Rect.Height
            };
            _lowerLeftArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_lowerLeftArm.Rotation), _lowerLeftArmGeo.Rect.X, _lowerLeftArmGeo.Rect.Y);
            dc.DrawGeometry(brush, null, _lowerLeftArmGeo);

            _upperRightArmGeo.Rect = new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_upperRightArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_upperRightArm.Position.Y),
                Width = _upperRightArmGeo.Rect.Width,
                Height = _upperRightArmGeo.Rect.Height
            };
            _upperRightArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_upperRightArm.Rotation), _upperRightArmGeo.Rect.X, _upperRightArmGeo.Rect.Y);
            dc.DrawGeometry(brush, null, _upperRightArmGeo);

            _lowerRightArmGeo.Rect = new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_lowerRightArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_lowerRightArm.Position.Y),
                Width = _lowerRightArmGeo.Rect.Width,
                Height = _lowerRightArmGeo.Rect.Height
            };
            _lowerRightArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_lowerRightArm.Rotation), _lowerRightArmGeo.Rect.X, _lowerRightArmGeo.Rect.Y);
            dc.DrawGeometry(brush, null, _lowerRightArmGeo);

            _upperLeftLegGeo.Rect = new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_upperLeftLeg.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_upperLeftLeg.Position.Y),
                Width = _upperLeftLegGeo.Rect.Width,
                Height = _upperLeftLegGeo.Rect.Height
            };
            _upperLeftLegGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_upperLeftLeg.Rotation), _upperLeftLegGeo.Rect.X, _upperLeftLegGeo.Rect.Y);
            dc.DrawGeometry(brush, null, _upperLeftLegGeo);

            _lowerLeftLegGeo.Rect = new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_lowerLeftLeg.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_lowerLeftLeg.Position.Y),
                Width = _lowerLeftLegGeo.Rect.Width,
                Height = _lowerLeftLegGeo.Rect.Height
            };
            _lowerLeftLegGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_lowerLeftLeg.Rotation), _lowerLeftLegGeo.Rect.X, _lowerLeftLegGeo.Rect.Y);
            dc.DrawGeometry(brush, null, _lowerLeftLegGeo);

            _upperRightLegGeo.Rect = new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_upperRightLeg.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_upperRightLeg.Position.Y),
                Width = _upperRightLegGeo.Rect.Width,
                Height = _upperRightLegGeo.Rect.Height
            };
            _upperRightLegGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_upperRightLeg.Rotation), _upperRightLegGeo.Rect.X, _upperRightLegGeo.Rect.Y);
            dc.DrawGeometry(brush, null, _upperRightLegGeo);

            _lowerRightLegGeo.Rect = new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_lowerRightLeg.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_lowerRightLeg.Position.Y),
                Width = _lowerRightLegGeo.Rect.Width,
                Height = _lowerRightLegGeo.Rect.Height
            };
            _lowerRightLegGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_lowerRightLeg.Rotation), _lowerRightLegGeo.Rect.X, _lowerRightLegGeo.Rect.Y);
            dc.DrawGeometry(brush, null, _lowerRightLegGeo);
        }

        public void Move(IReadOnlyDictionary<MK.JointType, MK.Joint> Joints, float StepSeconds)
        {
            float maxTorque = 400f;
            float speedFactor = 2f;
            StepSeconds = StepSeconds / speedFactor;

            //Torso
            if (Joints[MK.JointType.ShoulderLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ShoulderRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.ShoulderLeft].Position;
                var end = Joints[MK.JointType.ShoulderRight].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _torso.Rotation = -expectedRadian;
            }

            //Right Arm
            if (Joints[MK.JointType.ElbowRight].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ShoulderRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.ShoulderRight].Position;
                var end = Joints[MK.JointType.ElbowRight].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _jRightArmBody.MotorSpeed = (expectedRadian - _jRightArmBody.JointAngle) / StepSeconds;
                _jRightArmBody.MaxMotorTorque = maxTorque;
            }
            if (Joints[MK.JointType.WristRight].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ElbowRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.ElbowRight].Position;
                var end = Joints[MK.JointType.WristRight].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _jRightArm.MotorSpeed = (expectedRadian - _jRightArm.JointAngle) / StepSeconds;
                _jRightArm.MaxMotorTorque = maxTorque;
            }

            //Left Arm
            if (Joints[MK.JointType.ElbowLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ShoulderLeft].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.ElbowLeft].Position;
                var end = Joints[MK.JointType.ShoulderLeft].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _jLeftArmBody.MotorSpeed = (expectedRadian - _jLeftArmBody.JointAngle) / StepSeconds;
                _jLeftArmBody.MaxMotorTorque = maxTorque;
            }
            if (Joints[MK.JointType.WristLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ElbowLeft].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.WristLeft].Position;
                var end = Joints[MK.JointType.ElbowLeft].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _jLeftArm.MotorSpeed = (expectedRadian - _jLeftArm.JointAngle) / StepSeconds;
                _jLeftArm.MaxMotorTorque = maxTorque;
            }

            //Right Leg
            if (Joints[MK.JointType.KneeRight].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.HipRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.KneeRight].Position;
                var end = Joints[MK.JointType.HipRight].Position;

                var expectedRadian = (float)((Math.Atan2(end.Y - start.Y, end.X - start.X)) - (Math.PI / 2));

                _jRightLegBody.MotorSpeed = expectedRadian - _jRightLegBody.JointAngle;
                _jRightLegBody.MaxMotorTorque = maxTorque;
            }
            if (Joints[MK.JointType.AnkleRight].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.KneeRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.AnkleRight].Position;
                var end = Joints[MK.JointType.KneeRight].Position;

                var expectedRadian = (float)((Math.Atan2(end.Y - start.Y, end.X - start.X)) - (Math.PI / 2));

                _jRightLeg.MotorSpeed = expectedRadian - _jRightLeg.JointAngle;
                _jRightLeg.MaxMotorTorque = maxTorque;
            }

            //Left Leg
            if (Joints[MK.JointType.KneeLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.HipLeft].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.KneeLeft].Position;
                var end = Joints[MK.JointType.HipLeft].Position;

                var expectedRadian = (float)((Math.Atan2(end.Y - start.Y, end.X - start.X)) - (Math.PI / 2));

                _jLeftLegBody.MotorSpeed = expectedRadian - _jLeftLegBody.JointAngle;
                _jLeftLegBody.MaxMotorTorque = maxTorque;
            }
            if (Joints[MK.JointType.AnkleLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.KneeLeft].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.AnkleLeft].Position;
                var end = Joints[MK.JointType.KneeLeft].Position;

                var expectedRadian = (float)((Math.Atan2(end.Y - start.Y, end.X - start.X)) - (Math.PI / 2));

                _jLeftLeg.MotorSpeed = expectedRadian - _jLeftLeg.JointAngle;
                _jLeftLeg.MaxMotorTorque = maxTorque;
            }
        }


        public void Jump(IReadOnlyDictionary<MK.JointType, MK.Joint> Joints, float StepSeconds)
        {
            if (Joints[MK.JointType.SpineMid].TrackingState == MK.TrackingState.Tracked)
            {
                float speedFactor = 2f;
                StepSeconds = StepSeconds / speedFactor;

                //Jump
                if (Joints[MK.JointType.SpineMid].TrackingState == MK.TrackingState.Tracked)
                {
                    var currentPosition = Joints[MK.JointType.SpineMid].Position;
                    if (_previousPosition != default(MK.CameraSpacePoint))
                    {
                        if (hasJumped)
                        {
                            var hSpeed = -(50 * (_previousPosition.X - currentPosition.X) / StepSeconds);
                            _torso.ApplyLinearImpulse(new Vector2(hSpeed, 0));
                        }
                        else if (StepSeconds > 0)
                        {
                            var currentSpeed = (currentPosition.Y - _previousPosition.Y) / StepSeconds;
                            verticalSpeed = verticalSpeed > currentSpeed ? verticalSpeed : currentSpeed;

                            if (verticalSpeed > 4)
                            {
                                _torso.BodyType = BodyType.Dynamic;
                                _torso.ApplyLinearImpulse(new Vector2(0, -100 * verticalSpeed));

                                if (verticalSpeed > currentSpeed + 1)
                                {
                                    hasJumped = true;
                                }
                            }
                        }
                    }
                    _previousPosition = currentPosition;
                }
            }
        }
    }
}
