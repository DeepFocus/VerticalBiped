using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MK = Microsoft.Kinect;

namespace JumpFocus
{
    class Avatar
    {
        private World _world;

        private const float TorsoDensity = 20f;
        private const float ArmDensity = 10f;
        private const float LimbAngularDamping = 7f;

        private Body _torso;
        private RectangleGeometry _torsoGeo;
        private Body _leftArm;
        private RectangleGeometry _leftArmGeo;
        private Body _rightArm;
        private RectangleGeometry _rightArmGeo;

        private readonly BitmapImage _torsoImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/Body.png"));
        private readonly BitmapImage _leftArmImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/LeftArm.png"));
        private readonly BitmapImage _rightArmImg = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Avatar/RightArm.png"));
        
        private RevoluteJoint _jRightArm;
        private RevoluteJoint _jLeftArm;
        
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
            _world.RemoveBody(_torso);
            _world.RemoveBody(_leftArm);
            _world.RemoveBody(_rightArm);
        }

        private void CreateBody(Vector2 position)
        {
            //Torso
            _torso = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_torsoImg.Width), ConvertUnits.ToSimUnits(_torsoImg.Height), TorsoDensity);
            _torso.BodyType = BodyType.Static;
            _torso.Mass = 50f;
            _torso.Position = position;
            _torso.CollisionGroup = -1;

            //Left Arm
            _leftArm = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_leftArmImg.Width), ConvertUnits.ToSimUnits(_leftArmImg.Height), ArmDensity);
            _leftArm.BodyType = BodyType.Dynamic;
            _leftArm.AngularDamping = LimbAngularDamping;
            _leftArm.Mass = 10f;
            _leftArm.Rotation = 1.4f;
            _leftArm.Position = position + new Vector2(-0.9f, 0.4f);
            _leftArm.CollisionGroup = -1;

            //Right Arm

            _rightArm = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_rightArmImg.Width), ConvertUnits.ToSimUnits(_rightArmImg.Height), ArmDensity);
            _rightArm.BodyType = BodyType.Dynamic;
            _rightArm.AngularDamping = LimbAngularDamping;
            _rightArm.Mass = 10f;
            _rightArm.Rotation = -1.4f;
            _rightArm.Position = position + new Vector2(0.9f, 0.4f);
            _rightArm.CollisionGroup = -1;
        }

        private void CreateJoints()
        {
            //lowerLeftArm -> upperLeftArm
            _jLeftArm = new RevoluteJoint(_leftArm, _torso,
                                                        new Vector2(0.25f, 0f),
                                                        new Vector2(-0.75f, -1f));
            _jLeftArm.CollideConnected = false;
            _jLeftArm.MotorEnabled = true;
            _world.AddJoint(_jLeftArm);

            //lowerRightArm -> upperRightArm
            _jRightArm = new RevoluteJoint(_rightArm, _torso,
                                                        new Vector2(0.25f, 0f),
                                                        new Vector2(0.75f, -1f));
            _jRightArm.CollideConnected = false;
            _jRightArm.MotorEnabled = true;
            _world.AddJoint(_jRightArm);
        }

        public void Draw(DrawingContext dc)
        {
            if (double.IsNaN(_torso.Position.X) || double.IsNaN(_torso.Position.Y))
            {
                return;
            }
            
            _torsoGeo = new RectangleGeometry(new Rect
            {
                //The center is the center in Geometry, not the top left
                X = ConvertUnits.ToDisplayUnits(_torso.Position.X) - _torsoImg.Width / 2,
                Y = ConvertUnits.ToDisplayUnits(_torso.Position.Y) - _torsoImg.Height / 2,
                Width = _torsoImg.Width,
                Height = _torsoImg.Height
            });
            _torsoGeo.Transform = new RotateTransform(
                MathHelper.ToDegrees(_torso.Rotation),
                ConvertUnits.ToDisplayUnits(_torso.Position.X),
                ConvertUnits.ToDisplayUnits(_torso.Position.Y));

            var transform = (RotateTransform)_torsoGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_torsoImg, _torsoGeo.Rect);
            dc.Pop();

            _leftArmGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_leftArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_leftArm.Position.Y),
                Width = _leftArmImg.Width,
                Height = _leftArmImg.Height
            });
            _leftArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_leftArm.Rotation), _leftArmGeo.Rect.X, _leftArmGeo.Rect.Y);

            transform = (RotateTransform)_leftArmGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_leftArmImg, _leftArmGeo.Rect);
            dc.Pop();
            
            _rightArmGeo = new RectangleGeometry(new Rect
            {
                X = ConvertUnits.ToDisplayUnits(_rightArm.Position.X),
                Y = ConvertUnits.ToDisplayUnits(_rightArm.Position.Y),
                Width = _rightArmImg.Width,
                Height = _rightArmImg.Height
            });

            _rightArmGeo.Transform = new RotateTransform(MathHelper.ToDegrees(_rightArm.Rotation), _rightArmGeo.Rect.X, _rightArmGeo.Rect.Y);

            transform = (RotateTransform)_rightArmGeo.Transform;
            dc.PushTransform(transform);
            dc.DrawImage(_rightArmImg, _rightArmGeo.Rect);
            dc.Pop();

            //Draw join for DEBUG
            //var dJoin = _jRightArm;

            //var jb = new Point
            //{
            //    X = ConvertUnits.ToDisplayUnits(dJoin.WorldAnchorA.X),
            //    Y = ConvertUnits.ToDisplayUnits(dJoin.WorldAnchorA.Y)
            //};
            //var je = new Point
            //{
            //    X = ConvertUnits.ToDisplayUnits(dJoin.WorldAnchorB.X),
            //    Y = ConvertUnits.ToDisplayUnits(dJoin.WorldAnchorB.Y)
            //};

            //dc.DrawEllipse(new SolidColorBrush(Colors.Red), null, jb, 10, 10);
            //dc.DrawEllipse(new SolidColorBrush(Colors.Green), null, je, 10, 10);
        }

        public void Move(IReadOnlyDictionary<MK.JointType, MK.Joint> Joints, float StepSeconds)
        {
            float maxTorque = 2500f;
            float speedFactor = 1f;
            StepSeconds = StepSeconds / speedFactor;

            //Torso
            if (_torso.BodyType == BodyType.Dynamic && Joints[MK.JointType.ShoulderLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ShoulderRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.ShoulderLeft].Position;
                var end = Joints[MK.JointType.ShoulderRight].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _torso.Rotation = -expectedRadian;
            }

            //Right Arm
            if (Joints[MK.JointType.WristRight].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ShoulderRight].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.ShoulderRight].Position;
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
            if (Joints[MK.JointType.WristLeft].TrackingState == MK.TrackingState.Tracked && Joints[MK.JointType.ShoulderLeft].TrackingState == MK.TrackingState.Tracked)
            {
                var start = Joints[MK.JointType.WristLeft].Position;
                var end = Joints[MK.JointType.ShoulderLeft].Position;

                var expectedRadian = (float)(Math.Atan2(end.Y - start.Y, end.X - start.X));

                _jLeftArm.MotorSpeed = (expectedRadian - _jLeftArm.JointAngle) / StepSeconds;
                _jLeftArm.MaxMotorTorque = maxTorque;
            }
            else
            {
                _jLeftArm.MotorSpeed = 0;
            }
        }

        public void Land()
        {
            _jLeftArm.MotorEnabled = false;
            _jRightArm.MotorEnabled = false;
        }

        public bool Jump(IReadOnlyDictionary<MK.JointType, MK.Joint> Joints, float StepSeconds)
        {
            if (IsReadyToJump && Joints[MK.JointType.SpineMid].TrackingState == MK.TrackingState.Tracked)
            {
                float speedFactor = 2f;
                StepSeconds = StepSeconds / speedFactor;

                //Jump
                if (Joints[MK.JointType.SpineMid].TrackingState == MK.TrackingState.Tracked)
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
                                _torso.ApplyLinearImpulse(new Vector2(0, -1000 * VerticalSpeed));

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
