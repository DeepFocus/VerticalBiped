using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MK = Microsoft.Kinect;

namespace JumpFocus
{
    class CircleMember : IDisposable
    {
        public MK.JointType Start { get; set; }
        public MK.JointType End { get; set; }

        private World _world;
        private Body _body;
        private Point _start;
        private Point _end;

        public CircleMember(World world)
        {
            _world = world;
        }

        public void CreatePhysic(Point Start, Point End)
        {
            var startX = ConvertUnits.ToSimUnits(Start.X);
            var startY = ConvertUnits.ToSimUnits(Start.Y);
            var endX = ConvertUnits.ToSimUnits(End.X);
            var endY = ConvertUnits.ToSimUnits(End.Y);
            var radius = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));

            _body = BodyFactory.CreateCircle(_world, (float)radius, 1f);
            _body.Position = new Vector2(startX, startY);

            _start = Start;
            _end = End;
        }

        public void Draw(DrawingContext dc, Brush brush)
        {
            var radius = Math.Sqrt(Math.Pow(_end.X - _start.X, 2) + Math.Pow(_end.Y - _start.Y, 2));
            dc.DrawEllipse(brush, null, _start, radius, radius);
        }

        public void Step(Point Start, Point End)
        {
            var startX = ConvertUnits.ToSimUnits(Start.X);
            var startY = ConvertUnits.ToSimUnits(Start.Y);
            var endX = ConvertUnits.ToSimUnits(End.X);
            var endY = ConvertUnits.ToSimUnits(End.Y);
            var radius = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));

            //_body.Position = new Vector2(startX, startY);
            _body.ApplyLinearImpulse(new Vector2(startX, startY));
            
            _start = Start;
            _end = End;
        }

        public void Dispose()
        {
            if (_world.BodyList.Contains(_body))
            {
                _world.RemoveBody(_body);
            }
        }
    }
}
