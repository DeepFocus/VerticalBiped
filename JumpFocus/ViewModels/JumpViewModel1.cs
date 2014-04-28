using Caliburn.Micro;
using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FP = FarseerPhysics;

namespace JumpFocus.ViewModels
{
    class JumpViewModel1 : Screen
    {
        private Stopwatch _stopwatch = null;
        private int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private KinectSensor _sensor;
        private CoordinateMapper _mapper;
        private MultiSourceFrameReader _reader;
        private FrameDescription _colorFrameDescription;
        private FrameDescription _depthFrameDescription;

        private byte[] _colorFrameData;
        private byte[] _bodyIndexFrameData;
        private ushort[] _depthFrameData;
        private byte[] _displayPixels;
        private ColorSpacePoint[] _colorSpacePoints;
        private Body[] _bodies;

        private Int32Rect _rect;
        private WriteableBitmap _bitmap;

        public DrawingImage Video
        {
            get { return _video; }
            private set
            {
                _video = value;
                NotifyOfPropertyChange(() => Video);
            }
        }
        private DrawingImage _video;
        private DrawingGroup _drawingGroup;

        //physiiiiiics duddddde
        private FP.Dynamics.World _world;
        private FP.Dynamics.Body _anchor;

        private FP.Dynamics.Body _ballBody;
        private FP.Collision.Shapes.Shape _ballShape;
        private FP.Dynamics.Fixture _ballFixture;

        private FP.Dynamics.Body _bodyBody;

        private RadialGradientBrush _greenBrush = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 122, 0));
        private RadialGradientBrush _redBrush = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 122, 0, 0));

        //Game data
        private CameraSpacePoint _previousPosition;
        private bool _hasJumped = false;

        //private Queue<FP.Dynamics.Body> _ballBody;
        //private Queue<FP.Collision.Shapes.Shape> _ballShape;
        //private Queue<FP.Dynamics.Fixture> _ballFixture;

        public JumpViewModel1(KinectSensor KinectSensor)
        {
            _sensor = KinectSensor;
        }

        protected override void OnActivate()
        {
            if (null != _sensor)
            {
                _hasJumped = false;
                // create a stopwatch for FPS calculation
                _stopwatch = new Stopwatch();

                _sensor.Open();

                _mapper = _sensor.CoordinateMapper;
                _colorFrameDescription = _sensor.ColorFrameSource.FrameDescription;
                _depthFrameDescription = _sensor.DepthFrameSource.FrameDescription;

                int screenWidth = _depthFrameDescription.Width;
                int screenHeight = _depthFrameDescription.Height;

                _rect = new Int32Rect(0, 0, screenWidth, screenHeight);
                _bitmap = new WriteableBitmap(screenWidth, screenHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                _colorFrameData = new byte[_colorFrameDescription.Width * _colorFrameDescription.Height * bytesPerPixel];
                _bodyIndexFrameData = new byte[screenWidth * screenHeight];
                _depthFrameData = new ushort[screenWidth * screenHeight];
                _displayPixels = new byte[screenWidth * screenHeight * bytesPerPixel];
                _colorSpacePoints = new ColorSpacePoint[screenWidth * screenHeight];

                _bodies = new Body[_sensor.BodyFrameSource.BodyCount];

                _drawingGroup = new DrawingGroup();
                Video = new DrawingImage(_drawingGroup);

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Depth | FrameSourceTypes.Body);

                //Recreate the world
                _world = new FP.Dynamics.World(new Vector2(0.0f, 9.82f));
                FP.ConvertUnits.SetDisplayUnitToSimUnitRatio(64f);

                //Defines the world's boundary
                float halfWidth = FP.ConvertUnits.ToSimUnits(screenWidth) / 2f;
                float halfHeight = FP.ConvertUnits.ToSimUnits(screenHeight) / 2f;

                //FP.Common.Vertices borders = new FP.Common.Vertices(2);
                //borders.Add(new Vector2(FP.ConvertUnits.ToSimUnits(0), FP.ConvertUnits.ToSimUnits(screenHeight)));
                //borders.Add(new Vector2(FP.ConvertUnits.ToSimUnits(screenWidth), FP.ConvertUnits.ToSimUnits(screenHeight)));

                FP.Common.Vertices borders = new FP.Common.Vertices(4);
                borders.Add(new Vector2(FP.ConvertUnits.ToSimUnits(0), FP.ConvertUnits.ToSimUnits(0)));
                borders.Add(new Vector2(FP.ConvertUnits.ToSimUnits(screenWidth), FP.ConvertUnits.ToSimUnits(0)));
                borders.Add(new Vector2(FP.ConvertUnits.ToSimUnits(screenWidth), FP.ConvertUnits.ToSimUnits(screenHeight)));
                borders.Add(new Vector2(FP.ConvertUnits.ToSimUnits(0), FP.ConvertUnits.ToSimUnits(screenHeight)));

                _anchor = FP.Factories.BodyFactory.CreateLoopShape(_world, borders);
                _anchor.CollisionCategories = FP.Dynamics.Category.All;
                _anchor.CollidesWith = FP.Dynamics.Category.All;
                _anchor.Restitution = 0.5f;

                //Ball stuff
                var ballVector = new Vector2
                {
                    X = FP.ConvertUnits.ToSimUnits(screenWidth / 2),
                    Y = 0
                };
                _ballBody = FP.Factories.BodyFactory.CreateBody(_world, ballVector);
                _ballBody.BodyType = FP.Dynamics.BodyType.Dynamic;

                _ballShape = new FP.Collision.Shapes.CircleShape(0.5f, 0.5f);
                _ballFixture = _ballBody.CreateFixture(_ballShape);

                //Needs to be the last step
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }

        /// <summary>
        /// Handles the depth/color/body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            using (var multiSourceFrame = e.FrameReference.AcquireFrame())
            {
                if (multiSourceFrame != null)
                {
                    using (var depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    using (var colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    using (var bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame())
                    using (var bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                    {
                        if (_stopwatch.IsRunning)
                        {
                            _stopwatch.Stop();
                            //Set next step depending on the kinect speed
                            _world.Step((float)_stopwatch.Elapsed.TotalSeconds);
                            _stopwatch.Reset();
                        }
                        if (!_stopwatch.IsRunning)
                        {
                            _stopwatch.Start();
                        }

                        if ((depthFrame != null) && (colorFrame != null) && (bodyIndexFrame != null) && (bodyFrame != null))
                        {
                            bodyFrame.GetAndRefreshBodyData(_bodies);

                            int depthWidth = depthFrame.FrameDescription.Width;
                            int depthHeight = depthFrame.FrameDescription.Height;

                            int colorWidth = colorFrame.FrameDescription.Width;
                            int colorHeight = colorFrame.FrameDescription.Height;

                            int bodyIndexWidth = bodyIndexFrame.FrameDescription.Width;
                            int bodyIndexHeight = bodyIndexFrame.FrameDescription.Height;

                            // verify data and write the new registered frame data to the display bitmap
                            if (((depthWidth * depthHeight) == _depthFrameData.Length) &&
                                ((colorWidth * colorHeight * bytesPerPixel) == _colorFrameData.Length) &&
                                ((bodyIndexWidth * bodyIndexHeight) == _bodyIndexFrameData.Length))
                            {
                                depthFrame.CopyFrameDataToArray(_depthFrameData);
                                if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                {
                                    colorFrame.CopyRawFrameDataToArray(_colorFrameData);
                                }
                                else
                                {
                                    colorFrame.CopyConvertedFrameDataToArray(_colorFrameData, ColorImageFormat.Bgra);
                                }

                                bodyIndexFrame.CopyFrameDataToArray(_bodyIndexFrameData);

                                _mapper.MapDepthFrameToColorSpace(_depthFrameData, _colorSpacePoints);

                                if (!_hasJumped)
                                {
                                    Array.Clear(_displayPixels, 0, _displayPixels.Length);
                                }

                                // loop over each row and column of the depth
                                byte player = 0xff;
                                for (int y = 2; y < depthHeight - 2; ++y)
                                {
                                    for (int x = 2; x < depthWidth - 2; ++x)
                                    {
                                        // calculate index into depth array
                                        int depthIndex = (y * depthWidth) + x;

                                        int previousDepthIndex = (y * depthWidth) + x - 1;
                                        int nextDepthIndex = (y * depthWidth) + x + 1;

                                        byte playerPixel = _bodyIndexFrameData[depthIndex];

                                        byte previousPlayer = _bodyIndexFrameData[previousDepthIndex];
                                        byte nextPlayer = _bodyIndexFrameData[nextDepthIndex];

                                        // if we're tracking a player for the current pixel, sets its color and alpha to full
                                        int displayIndex = depthIndex * bytesPerPixel;

                                        if (player == 0xff && playerPixel != 0xff ||
                                            player != 0xff && (playerPixel == player &&
                                            (previousPlayer == player && nextPlayer == player)))
                                        {
                                            player = playerPixel;

                                            if (!_hasJumped)
                                            {
                                                // retrieve the depth to color mapping for the current depth pixel
                                                ColorSpacePoint colorPoint = _colorSpacePoints[depthIndex];

                                                // make sure the depth pixel maps to a valid point in color space
                                                int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                                                int colorY = (int)Math.Floor(colorPoint.Y + 0.5);
                                                if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                                                {
                                                    // calculate index into color array
                                                    int colorIndex = ((colorY * colorWidth) + colorX) * this.bytesPerPixel;

                                                    _displayPixels[displayIndex] = _colorFrameData[colorIndex];
                                                    _displayPixels[displayIndex + 1] = _colorFrameData[colorIndex + 1];
                                                    _displayPixels[displayIndex + 2] = _colorFrameData[colorIndex + 2];
                                                    _displayPixels[displayIndex + 3] = 0xff;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (_hasJumped && null == _bodyBody)
                                {
                                    try
                                    {
                                        //body shape
                                        var texture = new uint[depthWidth * depthHeight * bytesPerPixel];
                                        Buffer.BlockCopy(_displayPixels, 0, texture, 0, _displayPixels.Length);

                                        var textureVertices = FP.Common.PolygonTools.CreatePolygon(texture, depthWidth, false);

                                        //1. To translate the vertices so the polygon is centered around the centroid.
                                        Vector2 centroid = -textureVertices.GetCentroid();
                                        textureVertices.Translate(ref centroid);

                                        //We simplify the vertices found in the texture.
                                        textureVertices = FP.Common.PolygonManipulation.SimplifyTools.ReduceByDistance(textureVertices, 12f);

                                        List<FP.Common.Vertices> list = FP.Common.Decomposition.Triangulate.ConvexPartition(textureVertices, FP.Common.Decomposition.TriangulationAlgorithm.Bayazit);

                                        //scale the vertices from graphics space to sim space
                                        Vector2 vertScale = new Vector2(FP.ConvertUnits.ToSimUnits(1));
                                        foreach (FP.Common.Vertices vertices in list)
                                        {
                                            vertices.Scale(ref vertScale);
                                        }

                                        //Create a single body with multiple fixtures
                                        _bodyBody = FP.Factories.BodyFactory.CreateCompoundPolygon(_world, list, 1f);
                                        _bodyBody.BodyType = FP.Dynamics.BodyType.Dynamic;
                                    }
                                    catch (Exception ex) { }
                                }

                                _bitmap.WritePixels(_rect, _displayPixels, depthWidth * bytesPerPixel, 0);
                                using (var dc = _drawingGroup.Open())
                                {
                                    dc.DrawImage(_bitmap, new Rect(_rect.X, _rect.Y, _rect.Width, _rect.Height));

                                    var _ballPoint = new Point();
                                    _ballPoint.Y = FP.ConvertUnits.ToDisplayUnits(_ballBody.Position.Y);
                                    _ballPoint.X = FP.ConvertUnits.ToDisplayUnits(_ballBody.Position.X);

                                    dc.DrawEllipse(_greenBrush, null, _ballPoint, FP.ConvertUnits.ToDisplayUnits(0.5f), FP.ConvertUnits.ToDisplayUnits(0.5f));

                                    //Draw joints
                                    if (player != 0xff)
                                    {
                                        var body = _bodies[player];

                                        if (body.IsTracked)
                                        {
                                            var currentPosition = body.Joints[JointType.HandRight].Position;

                                            var formattedText = new FormattedText((currentPosition.Y - _previousPosition.Y).ToString(), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Arial"), 24, new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)));
                                            var hposition = _mapper.MapCameraPointToDepthSpace(body.Joints[JointType.HandRight].Position);

                                            dc.DrawText(formattedText, new Point { X = hposition.X, Y = hposition.Y });

                                            if (!_hasJumped && currentPosition.Y - _previousPosition.Y > 0.02f)
                                            {
                                                _hasJumped = true;
                                            }

                                            if (null != _bodyBody)
                                            {
                                                var vec = new Vector2
                                                {
                                                    X = FP.ConvertUnits.ToSimUnits(hposition.X),
                                                    Y = FP.ConvertUnits.ToSimUnits(hposition.Y)
                                                };

                                                _bodyBody.Position = vec;
                                            }

                                            _previousPosition = body.Joints[JointType.HandRight].Position;

                                            foreach (var joint in body.Joints)
                                            {
                                                var position = _mapper.MapCameraPointToDepthSpace(joint.Value.Position);

                                                if (position.X - 10f > 10 && position.X + 10f < _depthFrameDescription.Width - 10
                                                    && position.Y - 10f > 10 && position.Y + 10f < _depthFrameDescription.Height - 10)
                                                {
                                                    dc.DrawEllipse(_redBrush, null, new Point { X = position.X, Y = position.Y }, 10f, 10f);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void OnDeactivate(bool close)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            _sensor.Close();
        }
    }
}
