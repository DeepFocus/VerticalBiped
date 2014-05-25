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
        private World _world;

        private float _worldWidth = 150f, _worldHeight = 150f;
        private float _cameraWidth = 50f, _cameraHeight = 50f;

        private Rect _camera;
        private Body _anchor;
        private List<Body> _coins;
        private List<Body> _clouds;
        private List<Body> _cats;

        private Brush _greenBrush = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 122, 0));
        private Brush _brownBrush = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 200, 80, 80));
        private Brush _redBrush = new SolidColorBrush(Color.FromArgb(255, 122, 0, 0));
        private Brush _blueBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
        private Typeface _typeface = new Typeface("Verdana");
        private Uri _cloudUri = new Uri("pack://application:,,,/Resources/Images/cloud.png");
        private Uri _coinUri = new Uri("pack://application:,,,/Resources/Images/coin.png");
        private Uri _catUri = new Uri("pack://application:,,,/Resources/Images/cat.png");

        private BitmapImage _cloudImg;
        private BitmapImage _coinImg;
        private BitmapImage _catImg;
        private TransformedBitmap _catReversedImg;

        private int _score = 0;

        public float WorldWdth { get { return _worldWidth; } }
        public float WorldHeight { get { return _worldHeight; } }

        public int Altitude { get; private set; }

        public GameWorld(World world)
        {
            _world = world;

            _cloudImg = new BitmapImage(_cloudUri);
            _coinImg = new BitmapImage(_coinUri);
            _catImg = new BitmapImage(_catUri);
            _catReversedImg = new TransformedBitmap(_catImg, new ScaleTransform(-1, 1));
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

            //Creates Dogecoins
            _coins = new List<Body>();
            var rand = new Random();
            for (int i = 0; i < 50; i++)
            {
                var position = new Vector2(rand.Next(2, (int)_worldWidth - 2), rand.Next(2, (int)_worldHeight - 25));

                var coin = BodyFactory.CreateCircle(_world, 2f, 2f, position);
                coin.BodyType = BodyType.Static;
                coin.CollisionCategories = Category.Cat2;
                coin.CollidesWith = Category.Cat1;
                coin.UserData = new Coin { Id = i, Value = rand.Next(10, 50) }; //nb of points
                coin.OnCollision += coin_OnCollision;

                _coins.Add(coin);
            }

            //Add clouds

            ////Creates the cloud in the physic engine from the image
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
            for (int i = 0; i < 50; i++)
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
            for (int i = 0; i < 10; i++)
            {
                var position = new Vector2(rand.Next(2, (int)_worldWidth - 2), rand.Next(2, (int)_worldHeight - 25));
                var cat = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_catImg.Width), ConvertUnits.ToSimUnits(_catImg.Height), 1f, position);
                cat.BodyType = BodyType.Dynamic;
                cat.Mass = 10f;
                cat.IgnoreGravity = true;
                cat.CollisionCategories = Category.Cat3;
                cat.CollidesWith = Category.Cat1;

                float speed = rand.Next(0, 2) == 1 ? rand.Next(1000, 10000) : -rand.Next(1000, 10000);
                cat.ApplyLinearImpulse(new Vector2(speed, 0));
                _cats.Add(cat);
            }
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
                            X = ConvertUnits.ToDisplayUnits(bodyCoin.Position.X),
                            Y = ConvertUnits.ToDisplayUnits(bodyCoin.Position.Y),
                            Width = ConvertUnits.ToDisplayUnits(2f),
                            Height = ConvertUnits.ToDisplayUnits(2f)
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
                        dc.DrawImage(_catImg, imgContainer);
                    }
                    else
                    {
                        dc.DrawImage(_catReversedImg, imgContainer);
                    }
                }
            }
        }

        bool coin_OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            var coin = (Coin)fixtureA.Body.UserData;
            _score += coin.Value;

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
                return true;
            }

            if (body.LinearVelocity.Y > 0)
            {
                fixtureB.Body.ApplyLinearImpulse(new Vector2(0, 1f));
            }

            return false;
        }
    }
}
