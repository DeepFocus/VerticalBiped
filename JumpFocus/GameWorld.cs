using FarseerPhysics;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace JumpFocus
{
    class GameWorld
    {
        private World _world;

        private float _worldWidth = 150f, _worldHeight = 150f;
        private float _cameraWidth = 50f, _cameraHeight = 50f;

        private Rect _camera;
        private Body _anchor;
        private ObservableCollection<Body> _coins;
        private Queue<Body> _ballBodies;

        private Brush _greenBrush = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 122, 0));
        private Brush _brownBrush = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 200, 80, 80));
        private Brush _redBrush = new SolidColorBrush(Color.FromArgb(255, 122, 0, 0));
        private Brush _blueBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
        private Typeface _typeface = new Typeface("Verdana");

        private int _score = 0;

        public float WorldWdth { get { return _worldWidth; } }
        public float WorldHeight { get { return _worldHeight; } }

        public int Altitude { get; private set; }

        public GameWorld(World world)
        {
            _world = world;
        }

        public void MoveCameraTo(double X, double Y)
        {
            _camera.X = X - ConvertUnits.ToDisplayUnits(_cameraWidth / 2);
            _camera.Y = Y - ConvertUnits.ToDisplayUnits(_cameraHeight / 2);
        }

        public void GenerateWorld()
        {
            Vertices borders = new Vertices(4);
            borders.Add(new Vector2(0, 0));
            borders.Add(new Vector2(_worldWidth, 0));
            borders.Add(new Vector2(_worldWidth, _worldHeight));
            borders.Add(new Vector2(0, _worldHeight));

            _camera = new Rect
            {
                X = 0,
                Y = ConvertUnits.ToDisplayUnits(_worldHeight - _cameraHeight),
                Width = ConvertUnits.ToDisplayUnits(_cameraWidth),
                Height = ConvertUnits.ToDisplayUnits(_cameraHeight)
            };

            _anchor = BodyFactory.CreateLoopShape(_world, borders);
            _anchor.Restitution = 1f;

            //Ball stuff
            _ballBodies = new Queue<Body>();
            for (int i = 0; i < 100; i++)
            {
                var position = new Vector2(i);
                var ball = BodyFactory.CreateCircle(_world, 0.5f, 1f, position);
                ball.BodyType = BodyType.Dynamic;
                ball.Mass = 10f;
                ball.CollisionCategories = Category.Cat1;
                ball.CollidesWith = Category.Cat1;
                _ballBodies.Enqueue(ball);
            }

            _coins = new ObservableCollection<Body>();
            var rand = new Random();
            for (int i = 0; i < 100; i++)
            {
                var position = new Vector2(rand.Next(2, 120), rand.Next(2, 120));

                var coin = BodyFactory.CreateCircle(_world, 1f, 1f, position);
                coin.BodyType = BodyType.Static;
                coin.CollisionCategories = Category.Cat2;
                //coin.CollidesWith = FP.Dynamics.Category.Cat2;
                coin.UserData = new Coin { Id = i, Value = rand.Next(10, 50) }; //nb of points
                coin.OnCollision += coin_OnCollision;

                _coins.Add(coin);
            }

            _coins.CollectionChanged += _coins_CollectionChanged;
        }

        public void Draw(DrawingContext dc)
        {
            //Blackground
            var black = Color.FromArgb(255, 0, 0, 0);
            var background = new SolidColorBrush(black);
            var bg = new RectangleGeometry(_camera);
            dc.DrawGeometry(background, null, bg);

            //Floor
            var floorRect = new Rect
            {
                X = _camera.X,
                Y = ConvertUnits.ToDisplayUnits(_worldHeight),
                Width = ConvertUnits.ToDisplayUnits(_cameraWidth) - 1,
                Height = 10
            };
            var floor = new RectangleGeometry(floorRect);
            if (bg.FillContains(floor))
            {
                dc.DrawGeometry(_greenBrush, null, floor);
            }

            //Score display
            var score = string.Format("Score: {0}", _score);
            var fText = new FormattedText(score, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, 150, _blueBrush);
            dc.DrawText(fText, _camera.Location);

            //Altitude
            var alt = ConvertUnits.ToSimUnits(_camera.Location.Y);
            alt = _worldHeight - alt;
            var altitude = string.Format("Altitude: {0}", alt);
            fText = new FormattedText(altitude, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, 150, _blueBrush);
            dc.DrawText(fText, Point.Add(_camera.Location, new Vector(0, 150)));

            //Draw balls
            foreach (var ball in _ballBodies)
            {
                var point = new Point();
                point.Y = ConvertUnits.ToDisplayUnits(ball.Position.Y);
                point.X = ConvertUnits.ToDisplayUnits(ball.Position.X);
                var ballGeo = new EllipseGeometry(point, ConvertUnits.ToDisplayUnits(0.5f), ConvertUnits.ToDisplayUnits(0.5f));

                if (bg.FillContains(ballGeo))
                {
                    dc.DrawGeometry(_greenBrush, null, ballGeo);
                }
            }

            //Draw coins
            foreach (var coin in _coins)
            {
                if (coin.Enabled)
                {
                    var point = new Point();
                    point.Y = ConvertUnits.ToDisplayUnits(coin.Position.Y);
                    point.X = ConvertUnits.ToDisplayUnits(coin.Position.X);

                    var coinGeo = new EllipseGeometry(point, ConvertUnits.ToDisplayUnits(1f), ConvertUnits.ToDisplayUnits(1f));

                    if (bg.FillContains(coinGeo))
                    {
                        dc.DrawGeometry(_brownBrush, null, coinGeo);
                    }
                }
            }
        }

        bool coin_OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            var coin = (Coin)fixtureA.Body.UserData;
            _score += coin.Value;

            //_coins.RemoveAt(coin.Id);
            coin.Value = 0;
            fixtureA.Body.UserData = coin;

            return true;
        }

        void _coins_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var coins = sender as ObservableCollection<Body>;
                foreach (var coin in coins)
                {
                    _world.RemoveBody(coin);
                }
            }
        }
    }
}
