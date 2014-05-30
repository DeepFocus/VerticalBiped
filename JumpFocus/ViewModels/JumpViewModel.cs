using Caliburn.Micro;
using Microsoft.Kinect;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FP = FarseerPhysics;

namespace JumpFocus.ViewModels
{
    class JumpViewModel : Screen
    {
        private Stopwatch _stopwatch = null;
        private int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        private KinectSensor _sensor;
        private MultiSourceFrameReader _reader;
        private Body[] _bodies;

        private DrawingImage _video;
        private DrawingGroup _drawingGroup;

        private float _readyCounter;
        private int _countDown = 5;

        //physiiiiiics duddddde
        private FP.Dynamics.World _world;

        private Brush _greenBrush = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 0, 122, 0));
        private Brush _brownBrush = new RadialGradientBrush(Color.FromArgb(0, 0, 0, 0), Color.FromArgb(255, 200, 80, 80));
        private Brush _redBrush = new SolidColorBrush(Color.FromArgb(255, 122, 0, 0));

        //Game data
        private ulong _currentUserId = 0;
        private Avatar _avatar;
        private GameWorld _gameWorld;

        public DrawingImage Video
        {
            get { return _video; }
            private set
            {
                _video = value;
                NotifyOfPropertyChange(() => Video);
            }
        }

        public JumpViewModel(KinectSensor KinectSensor)
        {
            _sensor = KinectSensor;
        }

        protected override void OnActivate()
        {
            if (null != _sensor)
            {
                // create a stopwatch for FPS calculation
                _stopwatch = new Stopwatch();
                _readyCounter = 0; //user readyness

                _sensor.Open();
                                
                _bodies = new Body[_sensor.BodyFrameSource.BodyCount];

                _drawingGroup = new DrawingGroup();
                Video = new DrawingImage(_drawingGroup);

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body);

                _world = new FP.Dynamics.World(new Vector2(0, 9.82f));
                FP.ConvertUnits.SetDisplayUnitToSimUnitRatio(128f);

                //Create the world
                _gameWorld = new GameWorld(_world);
                _gameWorld.GenerateWorld();

                //Delete the player
                _avatar = null;
                _currentUserId = 0;

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
                    using (var bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                    {
                        if (null != bodyFrame)
                        {
                            float stepSeconds = 0;
                            if (_stopwatch.IsRunning)
                            {
                                _stopwatch.Stop();
                                //Set next step depending on the kinect FPS rate
                                stepSeconds = (float)_stopwatch.Elapsed.TotalSeconds;

                                //Stop the game once the player has landed
                                if (!_gameWorld.HasLanded)
                                {
                                    _world.Step(stepSeconds);
                                }
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

                                //We already have a player
                                if (_currentUserId > 0 && _bodies.Any(b => b.TrackingId == _currentUserId && b.IsTracked == true))
                                {
                                    var body = _bodies.First(b => b.TrackingId == _currentUserId);
                                    
                                    _avatar.Move(body.Joints, stepSeconds);

                                    //player ready
                                    if (!_avatar.HasJumped && !_avatar.IsReadyToJump)
                                    {
                                        if (body.HandLeftState == HandState.Closed && body.HandRightState == HandState.Closed)
                                        {
                                            _readyCounter += stepSeconds;
                                            _gameWorld.Message = (_countDown - _readyCounter).ToString("f");
                                        }
                                        else
                                        {
                                            _gameWorld.Message = string.Format("Keep both hands closed for {0} sec", _countDown);
                                            _readyCounter = 0;
                                        }

                                        if (_readyCounter > _countDown)
                                        {
                                            _gameWorld.Message = "JUMP!!";
                                            _avatar.IsReadyToJump = true;
                                        }
                                    }

                                    if (_avatar.IsReadyToJump && !_avatar.HasJumped)
                                    {
                                        _avatar.Jump(body.Joints, stepSeconds);
                                    }

                                    if (_avatar.HasJumped && !string.IsNullOrWhiteSpace(_gameWorld.Message) && !_gameWorld.HasLanded)
                                    {
                                        _gameWorld.Message = string.Empty;
                                    }
                                }
                                else
                                {
                                    foreach (var body in _bodies)
                                    {
                                        if (body.IsTracked)
                                        {
                                            _currentUserId = body.TrackingId;
                                            _avatar = new Avatar(_world, new Vector2(_gameWorld.WorldWdth / 2, _gameWorld.WorldHeight - 10f));
                                            break;
                                        }
                                    }
                                }

                                if (null != _avatar)
                                {
                                    _avatar.Draw(dc);
                                    _gameWorld.MoveCameraTo(_avatar.BodyCenter.X, _avatar.BodyCenter.Y);
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
