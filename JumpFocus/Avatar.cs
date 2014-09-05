using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using JumpFocus.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MK = Microsoft.Kinect;

namespace JumpFocus
{
    class Avatar
    {
        private World _world;

        private const float ArmDensity = 10f;
        private const float LegDensity = 15f;
        private const float LimbAngularDamping = 7f;

        private Body _torso;
        private RectangleGeometry _torsoGeo;
        private Body _head;
        private RectangleGeometry _headGeo;

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

        private readonly BitmapImage _torsoImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/Torso.png"));
        private readonly BitmapImage _headImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/Head.png"));
        private readonly BitmapImage _lowerLeftArmImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/LowerLeftArm.png"));
        private readonly BitmapImage _lowerLeftLegImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/LowerLeftLeg.png"));
        private readonly BitmapImage _lowerRightArmImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/LowerRightArm.png"));
        private readonly BitmapImage _lowerRightLegImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/LowerRightLeg.png"));
        private readonly BitmapImage _upperLeftArmImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/UpperLeftArm.png"));
        private readonly BitmapImage _upperLeftLegImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/UpperLeftLeg.png"));
        private readonly BitmapImage _upperRightArmImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/UpperRightArm.png"));
        private readonly BitmapImage _upperRightLegImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/UpperRightLeg.png"));

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
        public bool HasJumped { get; private set; }
        public bool IsReadyToJump { get; set; }
        public float VerticalSpeed { get; private set; }
        public byte BodyIndex { get; set; }

        public Avatar(World world, Vector2 position)
        {
            _world = world;
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
            var headRadius = _headImg.Width / 3; //minus antenna
            _head = BodyFactory.CreateCircle(_world, ConvertUnits.ToSimUnits(headRadius), 20f);
            _head.BodyType = BodyType.Dynamic;
            _head.AngularDamping = LimbAngularDamping;
            _head.Mass = 2f;
            _head.Position = position;

            //Torso
            _torso = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_torsoImg.Width), ConvertUnits.ToSimUnits(_torsoImg.Height), 20f);
            _torso.Mass = 15f;
            _torso.Position = position + new Vector2(0.15f, 2.5f);

            //Left Arm
            _upperLeftArm = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_upperLeftArmImg.Width), ConvertUnits.ToSimUnits(_upperLeftArmImg.Height), ArmDensity);
            _upperLeftArm.BodyType = BodyType.Dynamic;
            _upperLeftArm.AngularDamping = LimbAngularDamping;
            _upperLeftArm.Mass = 2f;
            _upperLeftArm.Rotation = 1.4f;
            _upperLeftArm.Position = position + new Vector2(0f, 2.5f);

            _lowerLeftArm = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_lowerLeftArmImg.Width), ConvertUnits.ToSimUnits(_lowerLeftArmImg.Height), ArmDensity);
            _lowerLeftArm.BodyType = BodyType.Dynamic;
            _lowerLeftArm.AngularDamping = LimbAngularDamping;
            _lowerLeftArm.Mass = 2f;
            _lowerLeftArm.Rotation = 1.4f;
            _lowerLeftArm.Position = position + new Vector2(-1.2f, 2.7f);

            //Right Arm
            _upperRightArm = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_upperRightArmImg.Width), ConvertUnits.ToSimUnits(_upperRightArmImg.Height), ArmDensity);
            _upperRightArm.BodyType = BodyType.Dynamic;
            _upperRightArm.AngularDamping = LimbAngularDamping;
            _upperRightArm.Mass = 2f;
            _upperRightArm.Rotation = -1.4f;
            _upperRightArm.Position = position + new Vector2(1.6f, 3.0f);

            _lowerRightArm = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_lowerRightArmImg.Width), ConvertUnits.ToSimUnits(_lowerRightArmImg.Height), ArmDensity);
            _lowerRightArm.BodyType = BodyType.Dynamic;
            _lowerRightArm.AngularDamping = LimbAngularDamping;
            _lowerRightArm.Mass = 2f;
            _lowerRightArm.Rotation = -1.4f;
            _lowerRightArm.Position = position + new Vector2(2.8f, 3.2f);

            //Left Leg
            _upperLeftLeg = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_upperLeftLegImg.Width), ConvertUnits.ToSimUnits(_upperLeftLegImg.Height), LegDensity);
            _upperLeftLeg.BodyType = BodyType.Dynamic;
            _upperLeftLeg.AngularDamping = LimbAngularDamping;
            _upperLeftLeg.Mass = 5f;
            _upperLeftLeg.Position = position + new Vector2(0.1f, 5f);

            _lowerLeftLeg = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_lowerLeftLegImg.Width), ConvertUnits.ToSimUnits(_lowerLeftLegImg.Height), LegDensity);
            _lowerLeftLeg.BodyType = BodyType.Dynamic;
            _lowerLeftLeg.AngularDamping = LimbAngularDamping;
            _lowerLeftLeg.Mass = 5f;
            _lowerLeftLeg.Position = position + new Vector2(0.15f, 6.2f);

            //Right Leg
            _upperRightLeg = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_upperRightLegImg.Width), ConvertUnits.ToSimUnits(_upperRightLegImg.Height), LegDensity);
            _upperRightLeg.BodyType = BodyType.Dynamic;
            _upperRightLeg.AngularDamping = LimbAngularDamping;
            _upperRightLeg.Mass = 5f;
            _upperRightLeg.Position = position + new Vector2(1.2f, 5f);

            _lowerRightLeg = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_lowerRightLegImg.Width), ConvertUnits.ToSimUnits(_lowerRightLegImg.Height), LegDensity);
            _lowerRightLeg.BodyType = BodyType.Dynamic;
            _lowerRightLeg.AngularDamping = LimbAngularDamping;
            _lowerRightLeg.Mass = 5f;
            _lowerRightLeg.Position = position + new Vector2(1.25f, 6.2f);
        }

        private void CreateJoints()
        {
            //head -> body
            var jHeadBody = new RevoluteJoint(_head, _torso,
                                                        new Vector2(0f, 1f),
                                                        new Vector2(0f, -2f));
            jHeadBody.CollideConnected = false;
            jHeadBody.MotorEnabled = false;
            _world.AddJoint(jHeadBody);

            //upperLeftArm -> body
            _jLeftArmBody = new RevoluteJoint(_upperLeftArm, _torso,
                                                        new Vector2(0.1f, 0f),
                                                        new Vector2(-0.5f, 0.3f));
            _jLeftArmBody.CollideConnected = false;
            _jLeftArmBody.MotorEnabled = true;
            _world.AddJoint(_jLeftArmBody);

            //lowerLeftArm -> upperLeftArm
            _jLeftArm = new RevoluteJoint(_lowerLeftArm, _upperLeftArm,
                                                        new Vector2(0.25f, 0f),
                                                        new Vector2(0.25f, 1.3f));
            _jLeftArm.CollideConnected = false;
            _jLeftArm.MotorEnabled = true;
            _world.AddJoint(_jLeftArm);

            //upperRightArm -> body
            _jRightArmBody = new RevoluteJoint(_upperRightArm, _torso,
                                                        new Vector2(0.1f, 0f),
                                                        new Vector2(1.6f, 0.3f));
            _jRightArmBody.CollideConnected = false;
            _jRightArmBody.MotorEnabled = true;
            _world.AddJoint(_jRightArmBody);

            //lowerRightArm -> upperRightArm
            _jRightArm = new RevoluteJoint(_lowerRightArm, _upperRightArm,
                                                        new Vector2(0.25f, 0f),
                                                        new Vector2(0.25f, 1.3f));
            _jRightArm.CollideConnected = false;
            _jRightArm.MotorEnabled = true;
            _world.AddJoint(_jRightArm);

            //upperLeftLeg -> body
            _jLeftLegBody = new RevoluteJoint(_upperLeftLeg, _torso,
                                                        new Vector2(0f, -1.1f),
                                                        new Vector2(0.1f, 1.5f));
            _jLeftLegBody.CollideConnected = false;
            _jLeftLegBody.MotorEnabled = false;
            _world.AddJoint(_jLeftLegBody);

            //lowerLeftLeg -> upperLeftLeg
            _jLeftLeg = new RevoluteJoint(_lowerLeftLeg, _upperLeftLeg,
                                                       new Vector2(0f, 0f),
                                                       new Vector2(0f, 1.2f));
            _jLeftLeg.CollideConnected = false;
            _jLeftLeg.MotorEnabled = false;
            _world.AddJoint(_jLeftLeg);

            //upperRightleg -> body
            _jRightLegBody = new RevoluteJoint(_upperRightLeg, _torso,
                                                            new Vector2(0f, 0f),
                                                            new Vector2(1.2f, 2.5f));
            _jRightLegBody.CollideConnected = false;
            _jRightLegBody.MotorEnabled = false;
            _world.AddJoint(_jRightLegBody);

            //lowerRightleg -> upperRightleg
            _jRightLeg = new RevoluteJoint(_lowerRightLeg, _upperRightLeg,
                                                        new Vector2(0f, 0f),
                                                        new Vector2(0f, 1.2f));
            _jRightLeg.CollideConnected = false;
            _jRightLeg.MotorEnabled = false;
            _world.AddJoint(_jRightLeg);
        }

        public void Draw(DrawingContext dc)
        {
            if (double.IsNaN(_head.Position.X) || double.IsNaN(_head.Position.Y))
            {
                return;
            }

            _headGeo = new RectangleGeometry(
                new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(_head.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(_head.Position.Y),
                    Width = _headImg.Width,
                    Height = _headImg.Height
                }
            );

            _headGeo.Transform = new RotateTransform(
                MathHelper.ToDegrees(_head.Rotation),
                ConvertUnits.ToDisplayUnits(_head.Position.X),
                ConvertUnits.ToDisplayUnits(_head.Position.Y));

            var transform = (RotateTransform)_headGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_headImg, _headGeo.Rect);
            dc.Pop();

            _torsoGeo = new RectangleGeometry(new Rect
            {
                //The center is the center in Geometry, not the top left
                X = ConvertUnits.ToDisplayUnits(_torso.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_torso.Position.Y),
                Width = _torsoImg.Width,
                Height = _torsoImg.Height
            });
            _torsoGeo.Transform = new RotateTransform(
                MathHelper.ToDegrees(_torso.Rotation),
                ConvertUnits.ToDisplayUnits(_torso.Position.X),
                ConvertUnits.ToDisplayUnits(_torso.Position.Y));

            transform = (RotateTransform)_torsoGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_torsoImg, _torsoGeo.Rect);
            dc.Pop();

            _upperLeftArmGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_upperLeftArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_upperLeftArm.Position.Y),
                Width = _upperLeftArmImg.Width,
                Height = _upperLeftArmImg.Height
            });
            _upperLeftArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_upperLeftArm.Rotation), _upperLeftArmGeo.Rect.X, _upperLeftArmGeo.Rect.Y);

            transform = (RotateTransform)_upperLeftArmGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_upperLeftArmImg, _upperLeftArmGeo.Rect);
            dc.Pop();

            _lowerLeftArmGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_lowerLeftArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_lowerLeftArm.Position.Y),
                Width = _lowerLeftArmImg.Width,
                Height = _lowerLeftArmImg.Height
            });
            _lowerLeftArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_lowerLeftArm.Rotation), _lowerLeftArmGeo.Rect.X, _lowerLeftArmGeo.Rect.Y);

            transform = (RotateTransform)_lowerLeftArmGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_lowerLeftArmImg, _lowerLeftArmGeo.Rect);
            dc.Pop();

            _upperRightArmGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_upperRightArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_upperRightArm.Position.Y),
                Width = _upperRightArmImg.Width,
                Height = _upperRightArmImg.Height
            });
            _upperRightArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_upperRightArm.Rotation), _upperRightArmGeo.Rect.X, _upperRightArmGeo.Rect.Y);

            transform = (RotateTransform)_upperRightArmGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_upperRightArmImg, _upperRightArmGeo.Rect);
            dc.Pop();


            _lowerRightArmGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_lowerRightArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_lowerRightArm.Position.Y),
                Width = _lowerRightArmImg.Width,
                Height = _lowerRightArmImg.Height
            });

            _lowerRightArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_lowerRightArm.Rotation), _lowerRightArmGeo.Rect.X, _lowerRightArmGeo.Rect.Y);

            transform = (RotateTransform)_lowerRightArmGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_lowerRightArmImg, _lowerRightArmGeo.Rect);
            dc.Pop();

            _upperLeftLegGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_upperLeftLeg.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_upperLeftLeg.Position.Y),
                Width = _upperLeftLegImg.Width,
                Height = _upperLeftLegImg.Height
            });
            _upperLeftLegGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_upperLeftLeg.Rotation), _upperLeftLegGeo.Rect.X, _upperLeftLegGeo.Rect.Y);

            transform = (RotateTransform)_upperLeftLegGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_upperLeftLegImg, _upperLeftLegGeo.Rect);
            dc.Pop();

            _lowerLeftLegGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_lowerLeftLeg.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_lowerLeftLeg.Position.Y),
                Width = _lowerLeftLegImg.Width,
                Height = _lowerLeftLegImg.Height
            });
            _lowerLeftLegGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_lowerLeftLeg.Rotation), _lowerLeftLegGeo.Rect.X, _lowerLeftLegGeo.Rect.Y);

            transform = (RotateTransform)_lowerLeftLegGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_lowerLeftLegImg, _lowerLeftLegGeo.Rect);
            dc.Pop();

            _upperRightLegGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_upperRightLeg.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_upperRightLeg.Position.Y),
                Width = _upperRightLegImg.Width,
                Height = _upperRightLegImg.Height
            });
            _upperRightLegGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_upperRightLeg.Rotation), _upperRightLegGeo.Rect.X, _upperRightLegGeo.Rect.Y);

            transform = (RotateTransform)_upperRightLegGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_upperRightLegImg, _upperRightLegGeo.Rect);
            dc.Pop();

            _lowerRightLegGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_lowerRightLeg.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_lowerRightLeg.Position.Y),
                Width = _lowerRightLegImg.Width,
                Height = _lowerRightLegImg.Height
            });
            _lowerRightLegGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_lowerRightLeg.Rotation), _lowerRightLegGeo.Rect.X, _lowerRightLegGeo.Rect.Y);

            transform = (RotateTransform)_lowerRightLegGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_lowerRightLegImg, _lowerRightLegGeo.Rect);
            dc.Pop();
        }

        public void Move(IReadOnlyDictionary<MK.JointType, MK.Joint> Joints, float StepSeconds)
        {
            float maxTorque = 2500f;
            float speedFactor = 1f;
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
            else
            {
                _jRightArmBody.MotorSpeed = 0;
            }
            if (Joints[MK.JointType.WristRight].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ElbowRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.ElbowRight].Position;
                var end = Joints[MK.JointType.WristRight].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _jRightArm.MotorSpeed = (expectedRadian - _jRightArm.JointAngle) / StepSeconds;
                _jRightArm.MaxMotorTorque = maxTorque;
            }
            else
            {
                _jRightArm.MotorSpeed = 0;
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
            else
            {
                _jLeftArmBody.MotorSpeed = 0;
            }
            if (Joints[MK.JointType.WristLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ElbowLeft].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.WristLeft].Position;
                var end = Joints[MK.JointType.ElbowLeft].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _jLeftArm.MotorSpeed = (expectedRadian - _jLeftArm.JointAngle) / StepSeconds;
                _jLeftArm.MaxMotorTorque = maxTorque;
            }
            else
            {
                _jLeftArm.MotorSpeed = 0;
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
            else
            {
                _jRightLegBody.MotorSpeed = 0;
            }
            if (Joints[MK.JointType.AnkleRight].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.KneeRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.AnkleRight].Position;
                var end = Joints[MK.JointType.KneeRight].Position;

                var expectedRadian = (float)((Math.Atan2(end.Y - start.Y, end.X - start.X)) - (Math.PI / 2));

                _jRightLeg.MotorSpeed = expectedRadian - _jRightLeg.JointAngle;
                _jRightLeg.MaxMotorTorque = maxTorque;
            }
            else
            {
                _jRightLeg.MotorSpeed = 0;
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
            else
            {
                _jLeftLegBody.MotorSpeed = 0;
            }
            if (Joints[MK.JointType.AnkleLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.KneeLeft].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.AnkleLeft].Position;
                var end = Joints[MK.JointType.KneeLeft].Position;

                var expectedRadian = (float)((Math.Atan2(end.Y - start.Y, end.X - start.X)) - (Math.PI / 2));

                _jLeftLeg.MotorSpeed = expectedRadian - _jLeftLeg.JointAngle;
                _jLeftLeg.MaxMotorTorque = maxTorque;
            }
            else
            {
                _jLeftLeg.MotorSpeed = 0;
            }
        }

        public bool Jump(IReadOnlyDictionary<MK.JointType, MK.Joint> Joints, float StepSeconds)
        {
            if (IsReadyToJump && Joints[MK.JointType.SpineMid].TrackingState == MK.TrackingState.Tracked)
            {
                float speedFactor = 2f;
                StepSeconds = StepSeconds / speedFactor;

                //Jump
                if (Joints[MK.JointType.HandRight].TrackingState == MK.TrackingState.Tracked)
                {
                    var currentPosition = Joints[MK.JointType.SpineMid].Position;
                    if (_previousPosition != default(MK.CameraSpacePoint))
                    {
                        if (HasJumped)
                        {
                            var hSpeed = -(50 * (_previousPosition.X - currentPosition.X) / StepSeconds);
                            _torso.ApplyLinearImpulse(new Vector2(hSpeed, 0));
                        }
                        else if (StepSeconds > 0)
                        {
                            var currentSpeed = (currentPosition.Y - _previousPosition.Y) / StepSeconds;
                            VerticalSpeed = VerticalSpeed > currentSpeed ? VerticalSpeed : currentSpeed;

                            if (VerticalSpeed > 4)
                            {
                                _torso.BodyType = BodyType.Dynamic;
                                _torso.ApplyLinearImpulse(new Vector2(0, -100 * VerticalSpeed));

                                if (VerticalSpeed > currentSpeed + 1)
                                {
                                    HasJumped = true;
                                    return true;
                                }
                            }
                        }
                    }
                    _previousPosition = currentPosition;
                }
            }

            return false;
        }
    }
}
