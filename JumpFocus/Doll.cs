using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MK = Microsoft.Kinect;

namespace JumpFocus
{
    class Doll : IDisposable
    {
        private static float _simMemberHeight = 0.5f;
        private static double _displayMemberHeight = ConvertUnits.ToDisplayUnits(_simMemberHeight);

        private CircleMember _head;
        private Body _torso;
        private Body _leftForearm;
        private Body _leftUpperarm;
        private Body _rightForearm;
        private Body _rightUpperarm;
        private Body _leftHand;
        private Body _rightHand;
        private Body _leftLowerleg;
        private Body _rightLowerleg;
        private Body _leftUpperleg;
        private Body _rightUpperleg;
        private Body _leftFoot;
        private Body _rightFoot;

        private Brush _redBrush = new SolidColorBrush(Color.FromArgb(120, 255, 0, 0));

        private World _world;

        public Doll(World world, Dictionary<MK.JointType, Point> parts)
        {
            _world = world;

            _head = new CircleMember(_world)
                {
                    Start = MK.JointType.Head,
                    End = MK.JointType.Neck
                };
        }

        public void Step(DrawingContext dc, Dictionary<MK.JointType, Point> parts)
        {
            Cleanup();
            AddPhysic(parts);
            Draw(dc, parts);
        }

        private void AddPhysic(Dictionary<MK.JointType, Point> parts)
        {
            if (parts.ContainsKey(_head.Start) && parts.ContainsKey(_head.End))
            {
                _head.CreatePhysic(parts[_head.Start], parts[_head.End]);
                //_head = CreateCircle(parts[MK.JointType.Head], parts[MK.JointType.Neck]);
            }
            if (parts.ContainsKey(MK.JointType.ShoulderLeft) && parts.ContainsKey(MK.JointType.HipRight))
            {
                var startX = ConvertUnits.ToSimUnits(parts[MK.JointType.ShoulderLeft].X);
                var startY = ConvertUnits.ToSimUnits(parts[MK.JointType.ShoulderLeft].Y);
                var endX = ConvertUnits.ToSimUnits(parts[MK.JointType.HipRight].X);
                var endY = ConvertUnits.ToSimUnits(parts[MK.JointType.HipRight].Y);

                _torso = BodyFactory.CreateRectangle(_world, Math.Abs(endX - startX), Math.Abs(endY - startY), 1f);
                _torso.Position = new Vector2
                    {
                        X = startX + (endX - startX) / 2,
                        Y = startY + (endY - startY) / 2
                    };
            }
            //Left arm
            if (parts.ContainsKey(MK.JointType.ElbowLeft) && parts.ContainsKey(MK.JointType.ShoulderLeft))
            {
                _leftUpperarm = CreateRectangle(parts[MK.JointType.ElbowLeft], parts[MK.JointType.ShoulderLeft]);
            }
            if (parts.ContainsKey(MK.JointType.ElbowLeft) && parts.ContainsKey(MK.JointType.WristLeft))
            {
                _leftForearm = CreateRectangle(parts[MK.JointType.ElbowLeft], parts[MK.JointType.WristLeft]);
            }
            if (parts.ContainsKey(MK.JointType.HandLeft) && parts.ContainsKey(MK.JointType.HandTipLeft))
            {
                _leftHand = CreateCircle(parts[MK.JointType.HandLeft], parts[MK.JointType.HandTipLeft]);
            }
            //Right arm
            if (parts.ContainsKey(MK.JointType.ShoulderRight) && parts.ContainsKey(MK.JointType.ElbowRight))
            {
                _rightUpperarm = CreateRectangle(parts[MK.JointType.ShoulderRight], parts[MK.JointType.ElbowRight]);
            }
            if (parts.ContainsKey(MK.JointType.ElbowRight) && parts.ContainsKey(MK.JointType.WristRight))
            {
                _rightForearm = CreateRectangle(parts[MK.JointType.ElbowRight], parts[MK.JointType.WristRight]);
            }
            if (parts.ContainsKey(MK.JointType.HandRight) && parts.ContainsKey(MK.JointType.HandTipRight))
            {
                _rightHand = CreateCircle(parts[MK.JointType.HandRight], parts[MK.JointType.HandTipRight]);
            }
            //Left leg
            if (parts.ContainsKey(MK.JointType.HipLeft) && parts.ContainsKey(MK.JointType.KneeLeft))
            {
                _leftUpperleg = CreateRectangle(parts[MK.JointType.HipLeft], parts[MK.JointType.KneeLeft]);
            }
            if (parts.ContainsKey(MK.JointType.KneeLeft) && parts.ContainsKey(MK.JointType.AnkleLeft))
            {
                _leftLowerleg = CreateRectangle(parts[MK.JointType.KneeLeft], parts[MK.JointType.AnkleLeft]);
            }
            //Right leg
            if (parts.ContainsKey(MK.JointType.HipRight) && parts.ContainsKey(MK.JointType.KneeRight))
            {
                _rightUpperleg = CreateCircle(parts[MK.JointType.HipRight], parts[MK.JointType.KneeRight]);
            }
            if (parts.ContainsKey(MK.JointType.KneeRight) && parts.ContainsKey(MK.JointType.AnkleRight))
            {
                _rightLowerleg = CreateCircle(parts[MK.JointType.KneeRight], parts[MK.JointType.AnkleRight]);
            }
            //feet
            if (parts.ContainsKey(MK.JointType.AnkleLeft) && parts.ContainsKey(MK.JointType.FootLeft))
            {
                _leftFoot = CreateCircle(parts[MK.JointType.AnkleLeft], parts[MK.JointType.FootLeft]);
            }
            if (parts.ContainsKey(MK.JointType.AnkleRight) && parts.ContainsKey(MK.JointType.FootRight))
            {
                _rightFoot = CreateCircle(parts[MK.JointType.AnkleRight], parts[MK.JointType.FootRight]);
            }
        }

        private void Draw(DrawingContext dc, Dictionary<MK.JointType, Point> parts)
        {
            if (null != _head)
            {
                //DrawEllipse(dc, parts[MK.JointType.Head], parts[MK.JointType.Neck]);
                _head.Draw(dc, _redBrush);
            }
            if (null != _torso)
            {
                DrawRectangle(dc, parts[MK.JointType.ShoulderLeft], parts[MK.JointType.HipRight]);
            }
            //Left arm
            if (null != _leftUpperarm)
            {
                DrawLine(dc, parts[MK.JointType.ShoulderLeft], parts[MK.JointType.ElbowLeft]);
            }
            if (null != _leftForearm)
            {
                DrawLine(dc, parts[MK.JointType.ElbowLeft], parts[MK.JointType.WristLeft]);
            }
            if (null != _leftHand)
            {
                DrawEllipse(dc, parts[MK.JointType.HandLeft], parts[MK.JointType.HandTipLeft]);
            }
            //Right arm
            if (null != _rightUpperarm)
            {
                DrawLine(dc, parts[MK.JointType.ShoulderRight], parts[MK.JointType.ElbowRight]);
            }
            if (null != _rightForearm)
            {
                DrawLine(dc, parts[MK.JointType.ElbowRight], parts[MK.JointType.WristRight]);
            }
            if (null != _rightHand)
            {
                DrawEllipse(dc, parts[MK.JointType.HandRight], parts[MK.JointType.HandTipRight]);
            }
            //Left leg
            if (null != _leftUpperleg)
            {
                DrawLine(dc, parts[MK.JointType.HipLeft], parts[MK.JointType.KneeLeft]);
            }
            if (null != _leftLowerleg)
            {
                DrawLine(dc, parts[MK.JointType.KneeLeft], parts[MK.JointType.AnkleLeft]);
            }
            if (null != _leftFoot)
            {
                DrawEllipse(dc, parts[MK.JointType.AnkleLeft], parts[MK.JointType.FootLeft]);
            }
            //Right leg
            if (null != _rightUpperleg)
            {
                DrawLine(dc, parts[MK.JointType.HipRight], parts[MK.JointType.KneeRight]);
            }
            if (null != _rightLowerleg)
            {
                DrawLine(dc, parts[MK.JointType.KneeRight], parts[MK.JointType.AnkleRight]);
            }
            if (null != _rightFoot)
            {
                DrawEllipse(dc, parts[MK.JointType.AnkleRight], parts[MK.JointType.FootRight]);
            }
        }

        private void DrawLine(DrawingContext dc, Point start, Point end)
        {
            var pen = new Pen(_redBrush, _displayMemberHeight);
            dc.DrawLine(pen, start, end);
        }

        private void DrawRectangle(DrawingContext dc, Point start, Point end)
        {
            var rect = new Rect(start, end);
            dc.DrawRectangle(_redBrush, null, rect);
        }

        private void DrawEllipse(DrawingContext dc, Point start, Point end)
        {
            var radius = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            dc.DrawEllipse(_redBrush, null, start, radius, radius);
        }

        private Body CreateRectangle(Point start, Point end)
        {
            var startX = ConvertUnits.ToSimUnits(start.X);
            var startY = ConvertUnits.ToSimUnits(start.Y);
            var endX = ConvertUnits.ToSimUnits(end.X);
            var endY = ConvertUnits.ToSimUnits(end.Y);

            //Position should be the middle of the rectangle
            var centerX = startX + Math.Abs(endX - startX) / 2;
            var centerY = startY + _simMemberHeight / 2;

            var body = BodyFactory.CreateRectangle(_world, Math.Abs(endX - startX), _simMemberHeight, 1f, new Vector2(centerX, centerY));

            body.FixedRotation = true;
            body.Rotation = MathHelper.ToRadians((float)(Math.Atan2(endX - startX, endY - startY) * (180 / Math.PI)));

            return body;
        }

        private Body CreateCircle(Point start, Point end)
        {
            var startX = ConvertUnits.ToSimUnits(start.X);
            var startY = ConvertUnits.ToSimUnits(start.Y);
            var endX = ConvertUnits.ToSimUnits(end.X);
            var endY = ConvertUnits.ToSimUnits(end.Y);
            var radius = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));

            Body body = BodyFactory.CreateCircle(_world, (float)radius, 1f);
            body.Position = new Vector2(startX, startY);

            return body;
        }

        private void Cleanup()
        {
            //if (null != _head)
            //{
            //    _head.Dispose();
            //    _head = null;
            //}
            if (_world.BodyList.Contains(_torso))
            {
                _torso.Dispose();
                _torso = null;
            }
            if (_world.BodyList.Contains(_leftUpperarm))
            {
                _leftUpperarm.Dispose();
                _leftUpperarm = null;
            }
            if (_world.BodyList.Contains(_leftForearm))
            {
                _leftForearm.Dispose();
                _leftForearm = null;
            }
            if (_world.BodyList.Contains(_leftHand))
            {
                _leftHand.Dispose();
                _leftHand = null;
            }
            if (_world.BodyList.Contains(_rightUpperarm))
            {
                _rightUpperarm.Dispose();
                _rightUpperarm = null;
            }
            if (_world.BodyList.Contains(_rightForearm))
            {
                _rightForearm.Dispose();
                _rightForearm = null;
            }
            if (_world.BodyList.Contains(_rightHand))
            {
                _rightHand.Dispose();
                _rightHand = null;
            }
            if (_world.BodyList.Contains(_leftUpperleg))
            {
                _leftUpperleg.Dispose();
                _leftUpperleg = null;
            }
            if (_world.BodyList.Contains(_leftLowerleg))
            {
                _leftLowerleg.Dispose();
                _leftLowerleg = null;
            }
            if (_world.BodyList.Contains(_leftFoot))
            {
                _leftFoot.Dispose();
                _leftFoot = null;
            }
            if (_world.BodyList.Contains(_rightUpperleg))
            {
                _rightUpperleg.Dispose();
                _rightUpperleg = null;
            }
            if (_world.BodyList.Contains(_rightLowerleg))
            {
                _rightLowerleg.Dispose();
                _rightLowerleg = null;
            }
            if (_world.BodyList.Contains(_rightFoot))
            {
                _rightFoot.Dispose();
                _rightFoot = null;
            }
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
