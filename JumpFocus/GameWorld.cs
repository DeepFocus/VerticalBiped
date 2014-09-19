using FarseerPhysics;
using FarseerPhysics.Common;
using FarseerPhysics.Common.Decomposition;
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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JumpFocus
{
    class GameWorld
    {
        private readonly World _world;

        private float _worldWidth = 150f, _worldHeight = 250f;
        private readonly float _cameraWidth = 40f, _cameraHeight = 40f;

        private Rect _camera;
        private Body _anchor;
        private Body _floor;
        private List<Body> _ballBodies;
        private List<Body> _coins;
        private List<Body> _clouds;
        private List<Body> _cats;

        //decorations
        private List<Rect> _xs;
        private List<Rect> _circles;
        private List<Rect> _diamonds;

        private readonly Brush _floorBrush = new SolidColorBrush(Color.FromRgb(236, 124, 95));
        private readonly Brush _skyBrush = new SolidColorBrush(Color.FromRgb(236, 241, 237));
        private readonly Brush _textBrush = new SolidColorBrush(Color.FromRgb(59, 66, 78));
        private readonly Typeface _typeface = new Typeface("Verdana");
        private readonly Uri _cloudUri = new Uri("pack://application:,,,/Resources/Images/cloud.png");
        private readonly Uri _coinUri = new Uri("pack://application:,,,/Resources/Images/coin.png");
        private readonly Uri _catUri = new Uri("pack://application:,,,/Resources/Images/cat.png");

        private readonly Uri _xUri = new Uri("pack://application:,,,/Resources/Images/x.png");
        private readonly Uri _circleUri = new Uri("pack://application:,,,/Resources/Images/circle.png");
        private readonly Uri _diamondUri = new Uri("pack://application:,,,/Resources/Images/diamond.png");

        private readonly BitmapImage _cloudImg;
        private readonly BitmapImage _coinImg;
        private readonly BitmapImage _catImg;
        private readonly TransformedBitmap _catReversedImg;

        private readonly BitmapImage _xImg;
        private readonly BitmapImage _circleImg;
        private readonly BitmapImage _diamondImg;

        private const float _coinsRadius = 1f;

        public int Coins { get; private set; }

        public float WorldWdth { get { return _worldWidth; } }
        public float WorldHeight { get { return _worldHeight; } }

        public int Altitude { get; private set; }
        public bool HasLanded { get; private set; }

        public string Message { get; set; }

        public GameWorld(World world, Rect workArea)
        {
            //Avoid distortion for full screen
            _cameraWidth = (float)((workArea.Width * _cameraHeight) / workArea.Height);
            _world = world;

            _cloudImg = new BitmapImage(_cloudUri);
            _coinImg = new BitmapImage(_coinUri);
            _catImg = new BitmapImage(_catUri);
            _catReversedImg = new TransformedBitmap(_catImg, new ScaleTransform(-1, 1));

            _xImg = new BitmapImage(_xUri);
            _circleImg = new BitmapImage(_circleUri);
            _diamondImg = new BitmapImage(_diamondUri);
        }

        public void Step()
        {
            foreach (var cat in _cats)
            {
                //check positions with margins to avoid pushing the player out of the bounds
                if (cat.Position.X < ConvertUnits.ToSimUnits(_catImg.Width) || cat.Position.X > _worldWidth - ConvertUnits.ToSimUnits(_catImg.Width))
                {
                    cat.LinearVelocity = -cat.LinearVelocity;
                }
            }
        }

        public void MoveCameraTo(double x, double y)
        {
            _camera.X = x - ConvertUnits.ToDisplayUnits(_cameraWidth / 2);
            _camera.Y = y - ConvertUnits.ToDisplayUnits(_cameraHeight / 2);
        }

        public void GenerateWorld()
        {
            var borders = new Vertices
            {
                new Vector2(0, 0),
                new Vector2(_worldWidth, 0),
                new Vector2(_worldWidth, _worldHeight),
                new Vector2(0, _worldHeight)
            };

            _camera = new Rect
            {
                X = 0,
                Y = ConvertUnits.ToDisplayUnits(_worldHeight - _cameraHeight),
                Width = ConvertUnits.ToDisplayUnits(_cameraWidth),
                Height = ConvertUnits.ToDisplayUnits(_cameraHeight)
            };

            _anchor = BodyFactory.CreateLoopShape(_world, borders);
            //_anchor.OnCollision += _anchor_OnCollision;
            _anchor.Restitution = 1f;

            //Floor needs to be seperated because it triggers the end of the game
            _floor = BodyFactory.CreateEdge(_world, new Vector2(-_worldWidth, _worldHeight), new Vector2(2 * _worldWidth, _worldHeight));
            _floor.Restitution = 1f;
            _floor.OnCollision += _floor_OnCollision;

            //DEBUG Ball stuff
            _ballBodies = new List<Body>();
            //for (int i = 1; i < 100; i++)
            //{
            //    var position = new Vector2(i);
            //    var ball = BodyFactory.CreateCircle(_world, 0.5f, 1f, position);
            //    ball.BodyType = BodyType.Dynamic;
            //    ball.Mass = 10f;
            //    ball.CollisionCategories = Category.Cat4;
            //    ball.CollidesWith = Category.Cat1;
            //    _ballBodies.Add(ball);
            //}

            //Creates Dogecoins
            _coins = new List<Body>();
            var rand = new Random();
            for (int i = 0; i < 100; i++)
            {
                var position = new Vector2(rand.Next(2, (int)_worldWidth - 2), rand.Next(2, (int)_worldHeight - 25));

                var coin = BodyFactory.CreateCircle(_world, _coinsRadius, 2f, position);
                coin.BodyType = BodyType.Static;
                coin.CollisionCategories = Category.Cat2;
                coin.CollidesWith = Category.Cat1;
                coin.UserData = new Coin { Id = i, Value = rand.Next(10, 50) }; //nb of points
                coin.OnCollision += coin_OnCollision;

                _coins.Add(coin);
            }

            //Add clouds
            //Creates the cloud in the physic engine from the image
            int nStride = (_cloudImg.PixelWidth * _cloudImg.Format.BitsPerPixel + 7) / 8;
            uint[] pixels = new uint[_cloudImg.PixelHeight * nStride];
            _cloudImg.CopyPixels(pixels, nStride, 0);
            var vertices = PolygonTools.CreatePolygon(pixels, _cloudImg.PixelWidth);
            //For now we need to scale the vertices (result is in pixels, we use meters)
            Vector2 scale = new Vector2(1 / 128f, 1 / 128f);
            vertices.Scale(ref scale);
            var verticesList = Triangulate.ConvexPartition(vertices, TriangulationAlgorithm.Bayazit);
            ////end

            _clouds = new List<Body>();
            rand = new Random();
            for (int i = 0; i < 150; i++)
            {
                var position = new Vector2(rand.Next(2, (int)_worldWidth - 2), rand.Next(2, (int)_worldHeight - 25));
                //Create a single body with multiple fixtures
                var cloud = BodyFactory.CreateCompoundPolygon(_world, verticesList, .0f, position);
                cloud.BodyType = BodyType.Static;
                cloud.CollisionCategories = Category.Cat3;
                cloud.CollidesWith = Category.Cat1;
                cloud.OnCollision += cloud_OnCollision;

                _clouds.Add(cloud);
            }

            //Add cats
            _cats = new List<Body>();
            rand = new Random();
            for (int i = 0; i < 30; i++)
            {
                var position = new Vector2(rand.Next(2, (int)_worldWidth - 2), rand.Next(2, (int)_worldHeight - 25));
                var cat = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_catImg.Width), ConvertUnits.ToSimUnits(_catImg.Height), 1f, position);
                cat.BodyType = BodyType.Kinematic; // so they don't stop

                float speed = rand.Next(0, 2) == 1 ? rand.Next(1000, 5000) : -rand.Next(1000, 5000);
                cat.LinearVelocity = new Vector2(speed, 0);

                _cats.Add(cat);
            }

            //decorations
            _xs = new List<Rect>();
            _circles = new List<Rect>();
            _diamonds = new List<Rect>();

            var w = ConvertUnits.ToDisplayUnits(_worldWidth);
            var h = ConvertUnits.ToDisplayUnits(_worldHeight);

            for (int i = 0; i < 200; i++)
            {
                _xs.Add(new Rect(rand.Next(2, (int)w - 2), rand.Next(2, (int)h - 25), _xImg.Width, _xImg.Height));
                _circles.Add(new Rect(rand.Next(2, (int)w - 2), rand.Next(2, (int)h - 25), _circleImg.Width, _circleImg.Height));
                //_diamonds.Add(new Rect(rand.Next(2, (int)w - 2), rand.Next(2, (int)h - 25), _diamondImg.Width, _diamondImg.Height));
            }
        }

        public void Draw(DrawingContext dc)
        {
            //Background
            var bg = new RectangleGeometry(_camera);
            dc.DrawGeometry(_skyBrush, null, bg);

            var floorHeight = (ConvertUnits.ToDisplayUnits(_cameraHeight) + _camera.Y) - ConvertUnits.ToDisplayUnits(_worldHeight);
            if (floorHeight > 1)
            {
                //Floor
                var floorRect = new Rect
                {
                    X = _camera.X,
                    Y = ConvertUnits.ToDisplayUnits(_worldHeight),
                    Width = ConvertUnits.ToDisplayUnits(_cameraWidth) - 1,
                    Height = floorHeight - 1
                };
                var floor = new RectangleGeometry(floorRect);
                if (bg.FillContains(floor))
                {
                    dc.DrawGeometry(_floorBrush, null, floor);
                }
            }

            //Score display
            var score = string.Format("Score: {0}", Coins);
            var fText = new FormattedText(score, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, 150, _textBrush);
            dc.DrawText(fText, _camera.Location);

            //Altitude
            var alt = ConvertUnits.ToSimUnits(_camera.Location.Y);
            alt = _worldHeight - alt - _cameraHeight;
            if (alt > Altitude)
            {
                Altitude = (int)alt;
            }
            var altitude = string.Format("Max Altitude: {0}", Altitude);
            fText = new FormattedText(altitude, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, 150, _textBrush);
            dc.DrawText(fText, Point.Add(_camera.Location, new Vector(0, 150)));

            //Message is needed
            if (!string.IsNullOrWhiteSpace(Message))
            {
                var messageText = new FormattedText(Message, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, _typeface, 250, _textBrush);
                messageText.TextAlignment = TextAlignment.Center;
                var location = new Point
                {
                    X = _camera.X + (_camera.Width / 2),
                    Y = _camera.Y + (_camera.Height / 4)
                };
                dc.DrawText(messageText, location);
            }

            //Draw balls
            foreach (var ball in _ballBodies)
            {
                var point = new Point();
                point.Y = ConvertUnits.ToDisplayUnits(ball.Position.Y);
                point.X = ConvertUnits.ToDisplayUnits(ball.Position.X);
                var ballGeo = new EllipseGeometry(point, ConvertUnits.ToDisplayUnits(0.5f), ConvertUnits.ToDisplayUnits(0.5f));

                if (bg.FillContains(ballGeo))
                {
                    dc.DrawGeometry(new SolidColorBrush(Colors.Azure), null, ballGeo);
                }
            }

            //Draw coins
            foreach (var bodyCoin in _coins)
            {
                if (bodyCoin.Enabled)
                {
                    var coin = (Coin)bodyCoin.UserData;
                    if (coin.Value > 0)
                    {
                        var imgContainer = new Rect
                        {
                            X = ConvertUnits.ToDisplayUnits(bodyCoin.Position.X - _coinsRadius),
                            Y = ConvertUnits.ToDisplayUnits(bodyCoin.Position.Y - _coinsRadius),
                            Width = ConvertUnits.ToDisplayUnits(_coinsRadius * 2),
                            Height = ConvertUnits.ToDisplayUnits(_coinsRadius * 2)
                        };

                        if (bg.FillContains(new RectangleGeometry(imgContainer)))
                        {
                            //dc.DrawGeometry(_brownBrush, null, coinGeo);
                            dc.DrawImage(_coinImg, imgContainer);
                        }
                    }
                }
            }

            //Draw clouds
            foreach (var cloud in _clouds)
            {
                var imgContainer = new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(cloud.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(cloud.Position.Y),
                    Width = _cloudImg.PixelWidth,
                    Height = _cloudImg.PixelHeight
                };

                if (bg.FillContains(new RectangleGeometry(imgContainer)))
                {
                    dc.DrawImage(_cloudImg, imgContainer);
                }
            }

            //Draw cats
            foreach (var cat in _cats)
            {
                //Flip the picture
                var imgContainer = new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(cat.Position.X),
                    Y = ConvertUnits.ToDisplayUnits(cat.Position.Y),
                    Width = _catImg.PixelWidth,
                    Height = _catImg.PixelHeight
                };

                if (bg.FillContains(new RectangleGeometry(imgContainer)))
                {
                    if (cat.LinearVelocity.X > 0)
                    {
                        dc.DrawImage(_catReversedImg, imgContainer);
                    }
                    else
                    {
                        dc.DrawImage(_catImg, imgContainer);
                    }
                }
            }

            //Draw decorations
            foreach (var x in _xs)
            {
                if (bg.FillContains(x.Location))
                {
                    dc.DrawImage(_xImg, x);
                }
            }
            foreach (var circle in _circles)
            {
                if (bg.FillContains(circle.Location))
                {
                    dc.DrawImage(_circleImg, circle);
                }
            }
            foreach (var diamond in _diamonds)
            {
                if (bg.FillContains(diamond.Location))
                {
                    dc.DrawImage(_diamondImg, diamond);
                }
            }
        }

        bool coin_OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            var coin = (Coin)fixtureA.Body.UserData;
            Coins += coin.Value;

            coin.Value = 0;
            fixtureA.Body.UserData = coin;

            return false;
        }

        /// <summary>
        /// Slows down the body going up but doesn't go further
        /// </summary>
        bool cloud_OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            var body = fixtureB.Body;

            //Needed for the world boundaries
            if (body.BodyType == BodyType.Static)
            {
                return false;
            }

            if (body.LinearVelocity.Y > 0)
            {
                fixtureB.Body.ApplyLinearImpulse(new Vector2(0, 100f));
            }

            return false;
        }

        bool _floor_OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            if (fixtureB.CollisionCategories == Category.Cat1)
            {
                //end of game
                HasLanded = true;
                Message = string.Format("Your score is {0}", Coins + Altitude);
            }

            return true;
        }
    }
}
