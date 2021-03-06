﻿using FarseerPhysics;
using FarseerPhysics.Common;
using FarseerPhysics.Common.Decomposition;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Contacts;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JumpFocus
{
    class GameWorld
    {
        private readonly World _world;

        private const float _worldWidth = 500f,_worldHeight = 800f;
        private readonly float _cameraWidth = 80f, _cameraHeight = 80f;

        private Rect _camera;
        private Body _anchor;
        private Body _floor;
        private List<Body> _coins;
        private List<Body> _clouds;
        private List<Body> _cats;

        //decorations
        private List<Rect> _xs;
        private List<Rect> _circles;
        private List<Rect> _diamonds;
        private List<Rect> _mountains1;
        private List<Rect> _mountains2;
        private List<Rect> _mountains3;

        private readonly Brush _floorBrush = new SolidColorBrush(Color.FromRgb(236, 124, 95));
        private readonly Brush _skyBrush = new SolidColorBrush(Color.FromRgb(236, 241, 237));
        private readonly Brush _textBrush = new SolidColorBrush(Color.FromRgb(59, 66, 78));
        private readonly Typeface _typeface = new Typeface(new FontFamily(new Uri("pack://application:,,,/"), "./Resources/Fonts/#OCR-A"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        private readonly Uri _cloudUri = new Uri("pack://application:,,,/Resources/Images/cloud.png");
        private readonly Uri _coin1Uri = new Uri("pack://application:,,,/Resources/Images/coin1.png");
        private readonly Uri _coin2Uri = new Uri("pack://application:,,,/Resources/Images/coin2.png");
        private readonly Uri _coin3Uri = new Uri("pack://application:,,,/Resources/Images/coin3.png");
        private readonly Uri _catUri = new Uri("pack://application:,,,/Resources/Images/cat.png");

        private readonly Uri _fireUri = new Uri("pack://application:,,,/Resources/Images/fire.png");
        private readonly Uri _xUri = new Uri("pack://application:,,,/Resources/Images/x.png");
        private readonly Uri _circleUri = new Uri("pack://application:,,,/Resources/Images/circle.png");
        private readonly Uri _diamondUri = new Uri("pack://application:,,,/Resources/Images/diamond.png");
        private readonly Uri _mountain1Uri = new Uri("pack://application:,,,/Resources/Images/mountain1.png");
        private readonly Uri _mountain2Uri = new Uri("pack://application:,,,/Resources/Images/mountain2.png");
        private readonly Uri _mountain3Uri = new Uri("pack://application:,,,/Resources/Images/mountain3.png");

        private readonly BitmapSource _cloudImg;
        private readonly BitmapSource _coin1Img;
        private readonly BitmapSource _coin2Img;
        private readonly BitmapSource _coin3Img;
        private readonly BitmapSource _catImg;
        private readonly BitmapSource _catReversedImg;

        private readonly BitmapSource _fireImg;
        private readonly BitmapSource _fireReversedImg;
        private readonly BitmapSource _xImg;
        private readonly BitmapSource _circleImg;
        private readonly BitmapSource _diamondImg;
        private readonly BitmapSource _mountain1Img;
        private readonly BitmapSource _mountain2Img;
        private readonly BitmapSource _mountain3Img;
        
        public int Coins { get; private set; }

        public float WorldWidth { get { return _worldWidth; } }
        public float WorldHeight { get { return _worldHeight; } }

        public int Altitude { get; private set; }
        public bool HasLanded { get; private set; }
        public DateTime Landed { get; private set; }

        public string Message { get; set; }

        public GameWorld(World world, Rect workArea)
        {
            //Avoid distortion for full screen
            _cameraWidth = (float)((workArea.Width * _cameraHeight) / workArea.Height);
            _world = world;

            _cloudImg = new BitmapImage(_cloudUri);
            _coin1Img = new BitmapImage(_coin1Uri);
            _coin2Img = new BitmapImage(_coin2Uri);
            _coin3Img = new BitmapImage(_coin3Uri);
            _catImg = new BitmapImage(_catUri);
            _catReversedImg = new TransformedBitmap(_catImg, new ScaleTransform(-1, 1));

            _fireImg = new BitmapImage(_fireUri);
            _fireReversedImg = new TransformedBitmap(_fireImg, new ScaleTransform(-1, 1));
            _xImg = new BitmapImage(_xUri);
            _circleImg = new BitmapImage(_circleUri);
            _diamondImg = new BitmapImage(_diamondUri);
            _mountain1Img = new BitmapImage(_mountain1Uri);
            _mountain2Img = new BitmapImage(_mountain2Uri);
            _mountain3Img = new BitmapImage(_mountain3Uri);
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
            _anchor.Restitution = 1f;

            //Floor needs to be seperated because it triggers the end of the game
            _floor = BodyFactory.CreateEdge(_world, new Vector2(-_worldWidth, _worldHeight - 0.1f), new Vector2(2 * _worldWidth, _worldHeight - 0.1f));
            _floor.Restitution = 0f;
            _floor.Friction = 1f;
            _floor.OnCollision += _floor_OnCollision;

            //Creates Dogecoins
            _coins = new List<Body>();
            var rand = new Random();
            for (int i = 0; i < 500; i++)
            {
                var position = new Vector2(rand.Next(2, (int)_worldWidth - 2), rand.Next(2, (int)_worldHeight - 25));

                var coin = BodyFactory.CreateCircle(_world, ConvertUnits.ToSimUnits(_coin1Img.Width / 2), 2f, position);
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
            for (int i = 0; i < 600; i++)
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
            for (int i = 0; i < 200; i++)
            {
                var position = new Vector2(rand.Next(2, (int)_worldWidth - 2), rand.Next(2, (int)_worldHeight - 25));
                //var cat = BodyFactory.CreateRectangle(_world, ConvertUnits.ToSimUnits(_catImg.Width), ConvertUnits.ToSimUnits(_catImg.Height), 1f, position);
                var cat = BodyFactory.CreateCircle(_world, ConvertUnits.ToSimUnits(_catImg.Width / 2), 1f, position);
                cat.BodyType = BodyType.Kinematic; // so they don't stop
                cat.OnCollision += cat_OnCollision;

                float speed = rand.Next(0, 2) == 1 ? rand.Next(2000, 10000) : -rand.Next(2000, 10000);
                cat.LinearVelocity = new Vector2(speed, 0);

                _cats.Add(cat);
            }

            //decorations
            _xs = new List<Rect>();
            _circles = new List<Rect>();
            _diamonds = new List<Rect>();
            _mountains1 = new List<Rect>();
            _mountains2 = new List<Rect>();
            _mountains3 = new List<Rect>();

            var w = ConvertUnits.ToDisplayUnits(_worldWidth);
            var h = ConvertUnits.ToDisplayUnits(_worldHeight);

            for (int i = 0; i < 500; i++)
            {
                _xs.Add(new Rect(rand.Next(2, (int)w - 2), rand.Next(2, (int)h - 205), _xImg.Width, _xImg.Height));
                _circles.Add(new Rect(rand.Next(2, (int)w - 2), rand.Next(2, (int)h - 250), _circleImg.Width, _circleImg.Height));
            }
            for (int i = 0; i < 50; i++)
            {
                _diamonds.Add(new Rect(rand.Next(2, (int)w - 2), rand.Next(2, (int)h - 250), _diamondImg.Width, _diamondImg.Height));
            }
            for (int i = 0; i < 5; i++)
            {
                _mountains1.Add(new Rect(rand.Next(2, (int)w / 2 - 10), rand.Next((int)(h - _mountain1Img.Height), (int)h), _mountain1Img.Width, _mountain1Img.Height));
                _mountains1.Add(new Rect(rand.Next((int)w / 2 - 10, (int)w - 2), rand.Next((int)(h - _mountain1Img.Height), (int)h), _mountain1Img.Width, _mountain1Img.Height));
                _mountains2.Add(new Rect(rand.Next(2, (int)w / 2 - 10), rand.Next((int)(h - _mountain2Img.Height), (int)h), _mountain2Img.Width, _mountain2Img.Height));
                _mountains2.Add(new Rect(rand.Next((int)w / 2 - 10, (int)w - 2), rand.Next((int)(h - _mountain2Img.Height), (int)h), _mountain2Img.Width, _mountain2Img.Height));
                _mountains3.Add(new Rect(rand.Next(2, (int)w / 2 - 10), rand.Next((int)(h - _mountain3Img.Height), (int)h), _mountain3Img.Width, _mountain3Img.Height));
                _mountains3.Add(new Rect(rand.Next((int)w / 2 - 10, (int)w - 2), rand.Next((int)(h - _mountain3Img.Height), (int)h), _mountain3Img.Width, _mountain3Img.Height));
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
                    Y = ConvertUnits.ToDisplayUnits(_worldHeight) - 200,
                    Width = ConvertUnits.ToDisplayUnits(_cameraWidth) - 1,
                    Height = floorHeight + 199
                };
                var floor = new RectangleGeometry(floorRect);
                if (bg.FillContains(floor))
                {
                    dc.DrawGeometry(_floorBrush, null, floor);
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
                            X = ConvertUnits.ToDisplayUnits(bodyCoin.Position.X) - _coin1Img.Width / 2,
                            Y = ConvertUnits.ToDisplayUnits(bodyCoin.Position.Y) - _coin1Img.Height / 2,
                            Width = _coin1Img.Width,
                            Height = _coin1Img.Height
                        };

                        if (bg.FillContains(new RectangleGeometry(imgContainer)))
                        {
                            if (coin.Value >= 45)
                            {
                                dc.DrawImage(_coin3Img, imgContainer);
                            }
                            else if (coin.Value >= 30)
                            {
                                dc.DrawImage(_coin2Img, imgContainer);
                            }
                            else
                            {
                                dc.DrawImage(_coin1Img, imgContainer);
                            }
                        }
                    }
                }
            }

            //Draw clouds
            foreach (var cloud in _clouds)
            {
                var imgContainer = new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(cloud.Position.X) - _cloudImg.Width / 2,
                    Y = ConvertUnits.ToDisplayUnits(cloud.Position.Y) - _cloudImg.Height / 2,
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
                var imgContainer = new Rect
                {
                    X = ConvertUnits.ToDisplayUnits(cat.Position.X) - _catImg.Width / 2,
                    Y = ConvertUnits.ToDisplayUnits(cat.Position.Y) - _catImg.Height / 1.3,
                    Width = _catImg.PixelWidth,
                    Height = _catImg.PixelHeight
                };

                if (bg.FillContains(new RectangleGeometry(imgContainer)))
                {
                    //Flip the picture if needed
                    if (cat.LinearVelocity.X > 0)
                    {
                        dc.DrawImage(_catReversedImg, imgContainer);
                        if (DateTime.UtcNow.Ticks % 5 != 0)
                        {
                            var fireContainer = new Rect
                            {
                                X = ConvertUnits.ToDisplayUnits(cat.Position.X) + _catImg.Width / 2,
                                Y = ConvertUnits.ToDisplayUnits(cat.Position.Y) + _catImg.Height / 2.7 - _fireImg.Height,
                                Width = _fireImg.PixelWidth,
                                Height = _fireImg.PixelHeight
                            };
                            if (bg.FillContains(new RectangleGeometry(fireContainer)))
                            {
                                dc.DrawImage(_fireReversedImg, fireContainer);
                            }
                        }
                    }
                    else
                    {
                        dc.DrawImage(_catImg, imgContainer);
                        if (DateTime.UtcNow.Ticks % 5 != 0)
                        {
                            var fireContainer = new Rect
                            {
                                X = ConvertUnits.ToDisplayUnits(cat.Position.X) - 1.25 * _catImg.Width,
                                Y = ConvertUnits.ToDisplayUnits(cat.Position.Y) + _catImg.Height / 2.7 - _fireImg.Height,
                                Width = _fireImg.PixelWidth,
                                Height = _fireImg.PixelHeight
                            };

                            if (bg.FillContains(new RectangleGeometry(fireContainer)))
                            {
                                dc.DrawImage(_fireImg, fireContainer);
                            }
                        }
                    }
                }
            }

            //Draw decorations
            foreach (var x in _xs)
            {
                if (bg.FillContains(new RectangleGeometry(x)))
                {
                    dc.DrawImage(_xImg, x);
                }
            }
            foreach (var circle in _circles)
            {
                if (bg.FillContains(new RectangleGeometry(circle)))
                {
                    dc.DrawImage(_circleImg, circle);
                }
            }
            foreach (var diamond in _diamonds)
            {
                if (bg.FillContains(new RectangleGeometry(diamond)))
                {
                    dc.DrawImage(_diamondImg, diamond);
                }
            }
            foreach (var mountain in _mountains1)
            {
                if (bg.FillContains(new RectangleGeometry(mountain)))
                {
                    dc.DrawImage(_mountain1Img, mountain);
                }
            }
            foreach (var mountain in _mountains2)
            {
                if (bg.FillContains(new RectangleGeometry(mountain)))
                {
                    dc.DrawImage(_mountain2Img, mountain);
                }
            }
            foreach (var mountain in _mountains3)
            {
                if (bg.FillContains(new RectangleGeometry(mountain)))
                {
                    dc.DrawImage(_mountain3Img, mountain);
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
        /// Slows down the avatar while going up
        /// </summary>
        bool cloud_OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            var body = fixtureB.Body;

            //Needed for the world boundaries
            if (body.BodyType == BodyType.Static)
            {
                return false;
            }

            if (body.LinearVelocity.Y < 0f)
            {
                fixtureB.Body.ApplyLinearImpulse(new Vector2(0, 100f));
            }

            return false;
        }
        /// <summary>
        /// Doesn't block the user on the way up
        /// </summary>
        /// <param name="fixtureA"></param>
        /// <param name="fixtureB"></param>
        /// <param name="contact"></param>
        /// <returns></returns>
        bool cat_OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            return fixtureB.Body.LinearVelocity.Y > 0f;
        }

        bool _floor_OnCollision(Fixture fixtureA, Fixture fixtureB, Contact contact)
        {
            if (fixtureB.CollisionCategories == Category.Cat1)
            {
                //end of game
                HasLanded = true;
                Landed = DateTime.Now;
                Message = string.Format("Your score is {0}", Coins + Altitude);
            }

            return true;
        }
    }
}
