using System.Data.Entity.Migrations;
using Caliburn.Micro;
using JumpFocus.DAL;
using JumpFocus.Models;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FP = FarseerPhysics;
using System.Drawing;

namespace JumpFocus.ViewModels
{
    class JumpViewModel : Screen
    {
        private Stopwatch _stopwatch;

        private readonly IConductor _conductor;
        private KinectSensor _sensor;
        private MultiSourceFrameReader _reader;
        private Body[] _bodies;
        //Screenshot setup
        private byte[] _colorPixels;
        private int _bytesPerPixel;
        private ushort[] _depthFrameData;
        private byte[] _bodyIndexFrameData;
        private ColorSpacePoint[] _colorPoints;
        //Screenshot result
        private string _filePath = string.Empty;

        private DrawingImage _video;
        private DrawingGroup _drawingGroup;

        private float _readyCounter;
        private int _countDown = 4;

        //physiiiiiics duddddde
        private FP.Dynamics.World _world;

        //Game data
        private ulong _currentUserId;
        private Avatar _avatar;
        private GameWorld _gameWorld;
        private readonly Player _player;

        //Gesture detection
        /// <summary> Path to the gesture database that was trained with VGB </summary>
        private const string GestureDatabase = @"Database\jump.gbd";
        /// <summary> Name of the discrete gesture in the database that we want to track </summary>
        private const string JumpDiscreteGestureName = "Jump";
        private const string JumpContinousGestureName = "JumpProgress";
        private const string NavigateLeftContinousGestureName = "NavigateProgress_Left";
        private const string NavigateRightContinousGestureName = "NavigateProgress_Right";
        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource _vgbFrameSource;
        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        private VisualGestureBuilderFrameReader _vgbFrameReader;
        private bool _isJumping = false;
        private bool _hasJumped = false;

        public DrawingImage Video
        {
            get { return _video; }
            private set
            {
                _video = value;
                NotifyOfPropertyChange(() => Video);
            }
        }

        public JumpViewModel(IConductor conductor, Player player)
        {
            _conductor = conductor;
            _sensor = KinectSensor.GetDefault();
            _player = player;
        }

        protected override void OnActivate()
        {
            if (null != _sensor)
            {
                // create a stopwatch for FPS calculation
                _stopwatch = new Stopwatch();
                _readyCounter = 0; //user readyness

                _sensor.Open();

                _bodies = new Body[6];//SDK doesn't provide this number anymore

                _drawingGroup = new DrawingGroup();
                Video = new DrawingImage(_drawingGroup);

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex);

                // create the colorFrameDescription from the ColorFrameSource using Bgra format
                var colorFrameDescription = _sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                // rgba is 4 bytes per pixel
                _bytesPerPixel = (int)colorFrameDescription.BytesPerPixel;
                _colorPixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * _bytesPerPixel];

                FrameDescription depthFrameDescription = _sensor.DepthFrameSource.FrameDescription;
                int depthWidth = depthFrameDescription.Width;
                int depthHeight = depthFrameDescription.Height;
                // allocate space to put the pixels being received and converted
                _depthFrameData = new ushort[depthWidth * depthHeight];
                _bodyIndexFrameData = new byte[depthWidth * depthHeight];
                _colorPoints = new ColorSpacePoint[depthWidth * depthHeight];

                //_world = new FP.Dynamics.World(new Vector2(0, 9.82f));
                _world = new FP.Dynamics.World(new Vector2(0, 5f));
                FP.ConvertUnits.SetDisplayUnitToSimUnitRatio(128f);

                //Create the world
                _gameWorld = new GameWorld(_world, SystemParameters.WorkArea);
                _gameWorld.GenerateWorld();

                //Delete the player
                _avatar = null;
                _currentUserId = 0;

                //Needs to be the last step
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

                //Gesture
                // create the vgb source. The associated body tracking ID will be set when a valid body frame arrives from the sensor.
                _vgbFrameSource = new VisualGestureBuilderFrameSource(_sensor, 0);
                // open the reader for the vgb frames
                _vgbFrameReader = this._vgbFrameSource.OpenReader();
                if (_vgbFrameReader != null)
                {
                    _vgbFrameReader.IsPaused = true;
                    _vgbFrameReader.FrameArrived += Reader_GestureFrameArrived;
                }
                // load the 'Jump' gesture from the gesture database
                using (var database = new VisualGestureBuilderDatabase(GestureDatabase))
                {
                    _vgbFrameSource.AddGestures(database.AvailableGestures);
                }
            }
        }

        /// <summary>
        /// Handles the depth/color/body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var multiSourceFrame = e.FrameReference.AcquireFrame();
            if (multiSourceFrame != null)
            {
                using (var bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                using (var colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                using (var depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                using (var bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame())
                {
                    if (null != bodyFrame && null != colorFrame && null != depthFrame && null != bodyIndexFrame)
                    {
                        float stepSeconds = 0;
                        if (_stopwatch.IsRunning)
                        {
                            _stopwatch.Stop();
                            //Set next step depending on the kinect FPS rate
                            stepSeconds = (float)_stopwatch.Elapsed.TotalSeconds;

                            _world.Step(stepSeconds);
                            _gameWorld.Step();

                            _stopwatch.Reset();
                        }
                        if (!_stopwatch.IsRunning)
                        {
                            _stopwatch.Start();
                        }

                        bodyFrame.GetAndRefreshBodyData(_bodies);
                        using (var dc = _drawingGroup.Open())
                        {
                            _gameWorld.Draw(dc);

                            //We have a player
                            if (_bodies.Any(b => b.TrackingId == _currentUserId && b.IsTracked))
                            {
                                var body = _bodies.First(b => b.TrackingId == _currentUserId);

                                if (!_gameWorld.HasLanded)
                                {
                                    _avatar.Move(body.Joints, stepSeconds);
                                }

                                if (_avatar.HasJumped && !string.IsNullOrWhiteSpace(_gameWorld.Message) && !_gameWorld.HasLanded)
                                {
                                    _gameWorld.Message = string.Empty;
                                }

                                if (_gameWorld.HasLanded)
                                {
                                    _avatar.Land();

                                    //screenshot
                                    if (string.IsNullOrWhiteSpace(_filePath))
                                    {
                                        //get body index
                                        for (byte index = 0; index < _bodies.Length; index++)
                                        {
                                            if (_bodies[index].TrackingId == _currentUserId)
                                            {
                                                //mugshot
                                                var headBitmap = RenderHeadshot(colorFrame, depthFrame, bodyIndexFrame, body, index);
                                                //_filePath = SaveBitmap(headBitmap);
                                                GeneratePostCard(headBitmap);
                                                //var bodyBitmap = RenderBodyshot(colorFrame, depthFrame,
                                                //    bodyIndexFrame, index);
                                                //_filePath = SaveBitmap(bodyBitmap);
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (null != _avatar)
                                {
                                    _avatar.Draw(dc);
                                    _gameWorld.MoveCameraTo(_avatar.BodyCenter.X, _avatar.BodyCenter.Y);
                                    if (_gameWorld.HasLanded)
                                    {
                                        if (_gameWorld.Landed.AddSeconds(2) < DateTime.Now)
                                        {
                                            var history = new History
                                            {
                                                Altitude = _gameWorld.Altitude,
                                                Dogecoins = _gameWorld.Coins,
                                                Played = DateTime.Now,
                                                Player = _player,
                                                Mugshot = _filePath
                                            };

                                            _player.Dogecoins += _gameWorld.Coins;

                                            var db = new JumpFocusContext();
                                            db.Histories.Add(history);
                                            db.Players.AddOrUpdate(_player);
                                            db.SaveChanges();

                                            _conductor.ActivateItem(new LeaderBoardViewModel(_conductor));
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //Set the closest body to be the one we use
                                float distance = 1000f;
                                for (byte index = 0; index < _bodies.Length; index++)
                                {
                                    if (_bodies[index].IsTracked
                                        && _bodies[index].Joints[JointType.SpineMid].TrackingState == TrackingState.Tracked
                                        && distance > _bodies[index].Joints[JointType.SpineMid].Position.Z)
                                    {
                                        _currentUserId = _bodies[index].TrackingId;

                                        //gesture
                                        _vgbFrameSource.TrackingId = _currentUserId;
                                        _vgbFrameReader.IsPaused = false;

                                        if (null == _avatar)
                                        {
                                            _avatar = new Avatar(_world, new Vector2(_gameWorld.WorldWdth / 2, _gameWorld.WorldHeight - 4f));
                                            _world.Step(0);
                                        }
                                        distance = _bodies[index].Joints[JointType.SpineMid].Position.Z;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    // get the discrete gesture results which arrived with the latest frame
                    var discreteResults = frame.DiscreteGestureResults;
                    var continousResults = frame.ContinuousGestureResults;

                    //Manages the jump part
                    if (null != discreteResults && null != continousResults)
                    {
                        var discreteGesture = _vgbFrameSource.Gestures.FirstOrDefault(g => g.GestureType == GestureType.Discrete && g.Name == JumpDiscreteGestureName);
                        var continousGesture = _vgbFrameSource.Gestures.FirstOrDefault(g => g.GestureType == GestureType.Continuous && g.Name == JumpContinousGestureName);
                        ContinuousGestureResult continousResult;
                        DiscreteGestureResult discreteResult;

                        if (null != discreteGesture && null != continousGesture)
                        {
                            discreteResults.TryGetValue(discreteGesture, out discreteResult);
                            continousResults.TryGetValue(continousGesture, out continousResult);
                            if (null != discreteResult && null != continousResult)
                            {
                                if (discreteResult.Detected && !_hasJumped)
                                {
                                    _isJumping = true;
                                    _avatar.Jump(continousResult.Progress);
                                }
                                if (_isJumping && !discreteResult.Detected)
                                {
                                    _hasJumped = true;
                                }
                            }
                        }
                    }
                    //Manages the left/right navigation
                    if (null != continousResults)
                    {
                        var continousLeftGesture = _vgbFrameSource.Gestures.FirstOrDefault(g => g.GestureType == GestureType.Continuous && g.Name == NavigateLeftContinousGestureName);
                        var continousRightGesture = _vgbFrameSource.Gestures.FirstOrDefault(g => g.GestureType == GestureType.Continuous && g.Name == NavigateRightContinousGestureName);
                        ContinuousGestureResult continousResult;

                        if (null != continousLeftGesture)
                        {
                            continousResults.TryGetValue(continousLeftGesture, out continousResult);
                            if (null != continousResult)
                            {
                                _avatar.Navigate(-continousResult.Progress);
                            }
                        }

                        if (null != continousRightGesture)
                        {
                            continousResults.TryGetValue(continousRightGesture, out continousResult);
                            if (null != continousResult)
                            {
                                _avatar.Navigate(continousResult.Progress);
                            }
                        }
                    }
                }
            }
        }

        private WriteableBitmap RenderHeadshot(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame bodyIndexFrame, Body body, byte bodyIndex)
        {
            var mapper = _sensor.CoordinateMapper;
            var head = mapper.MapCameraPointToDepthSpace(body.Joints[JointType.Head].Position);
            var spine = mapper.MapCameraPointToDepthSpace(body.Joints[JointType.SpineShoulder].Position);

            if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                colorFrame.CopyRawFrameDataToArray(_colorPixels);
            }
            else
            {
                colorFrame.CopyConvertedFrameDataToArray(_colorPixels, ColorImageFormat.Bgra);
            }

            var colorFrameDescription = colorFrame.FrameDescription;
            var colorWidth = colorFrameDescription.Width;
            var colorHeight = colorFrameDescription.Height;

            var depthFrameDescription = depthFrame.FrameDescription;
            var depthWidth = depthFrameDescription.Width;
            var depthHeight = depthFrameDescription.Height;

            if ((depthWidth * depthHeight) == _depthFrameData.Length)
            {
                depthFrame.CopyFrameDataToArray(_depthFrameData);
            }
            _sensor.CoordinateMapper.MapDepthFrameToColorSpace(_depthFrameData, _colorPoints);


            if (bodyIndexFrame != null)
            {
                var bodyIndexFrameDescription = bodyIndexFrame.FrameDescription;
                var bodyIndexWidth = bodyIndexFrameDescription.Width;
                var bodyIndexHeight = bodyIndexFrameDescription.Height;

                if ((bodyIndexWidth * bodyIndexHeight) == _bodyIndexFrameData.Length)
                {
                    bodyIndexFrame.CopyFrameDataToArray(_bodyIndexFrameData);
                }
            }

            var displayPixels = new byte[depthWidth * depthHeight * _bytesPerPixel];
            // loop over each row and column of the depth
            for (int y = 0; y < depthHeight; ++y)
            {
                for (int x = 0; x < depthWidth; ++x)
                {
                    // calculate index into depth array
                    int depthIndex = (y * depthWidth) + x;

                    byte player = _bodyIndexFrameData[depthIndex];
                    // retrieve the depth to color mapping for the current depth pixel
                    ColorSpacePoint colorPoint = _colorPoints[depthIndex];

                    // make sure the depth pixel maps to a valid point in color space
                    int colorX = (int)Math.Floor(colorPoint.X + 0.5);
                    int colorY = (int)Math.Floor(colorPoint.Y + 0.5);
                    if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                    {
                        // set source for copy to the color pixel
                        int displayIndex = depthIndex * _bytesPerPixel;
                        // if we're tracking a player for the current pixel, sets its color and alpha to full
                        if (player == bodyIndex)
                        {
                            // calculate index into color array
                            int colorIndex = ((colorY * colorWidth) + colorX) * _bytesPerPixel;
                            displayPixels[displayIndex++] = _colorPixels[colorIndex++];//blue
                            displayPixels[displayIndex++] = _colorPixels[colorIndex++];//green
                            displayPixels[displayIndex++] = _colorPixels[colorIndex];//red
                            displayPixels[displayIndex] = 0xff;//alpha
                        }
                        else
                        {
                            displayPixels[displayIndex + 3] = 0x00;//alpha
                        }
                    }
                }
            }

            var colorBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0,
                PixelFormats.Bgra32, null);
            colorBitmap.WritePixels(
                new Int32Rect(0, 0, depthFrameDescription.Width, depthFrameDescription.Height),
                displayPixels,
                colorBitmap.PixelWidth * _bytesPerPixel, 0);


            var headShot = new Int32Rect
            {
                X = (int)(head.X + (head.Y - spine.Y)),
                Y = (int)(head.Y + (head.Y - spine.Y)),
                Width = (int)((spine.Y - head.Y) * 2),
                Height = (int)((spine.Y - head.Y) * 2)
            };

            var headBitmap = new WriteableBitmap(headShot.Width, headShot.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
            var headPixels = new byte[headShot.Width * headShot.Height * _bytesPerPixel];
            colorBitmap.CopyPixels(headShot, headPixels, headShot.Width * _bytesPerPixel, 0);

            headBitmap.WritePixels(new Int32Rect(0, 0, headShot.Width, headShot.Height), headPixels,
                headBitmap.PixelWidth * _bytesPerPixel, 0);
            return headBitmap;
        }

        private string GeneratePostCard(WriteableBitmap input)
        {
            if (null != input)
            {
                using (var background = Image.FromFile("Resources/Images/Twitter/card.png"))
                using (var grfx = Graphics.FromImage(background))
                {
                    // create a png bitmap encoder which knows how to save a .png file
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    // create frame from the writable bitmap and add to encoder
                    encoder.Frames.Add(BitmapFrame.Create(input));
                    var ms = new MemoryStream();
                    encoder.Save(ms);
                    var rect = new Rectangle(530, 105, (int)input.Width, (int)input.Height);
                    grfx.DrawImage(Image.FromStream(ms), rect);
                    background.Save(@"C:\temp\test_twitter.png");
                }
            }

            return null;
        }

        protected override void OnDeactivate(bool close)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            _sensor.Close();

            base.OnDeactivate(close);
        }
    }
}
