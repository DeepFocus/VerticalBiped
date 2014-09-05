using System.Configuration;
using System.Data.Entity;
using System.Data.Odbc;
using System.IO;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using JumpFocus.Models;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using JumpFocus.DAL;
using JumpFocus.Proxies;

namespace JumpFocus.ViewModels
{
    class WelcomeViewModel : Screen
    {  
        private Player _player;
        private readonly ObjectCache _cache;
        private readonly string _twitterApiKey = ConfigurationManager.AppSettings["TwitterApiKey"];
        private readonly string _twitterApiSecret = ConfigurationManager.AppSettings["TwitterApiSecret"];
        private readonly string _twitterScreenName = ConfigurationManager.AppSettings["TwitterScreenName"];
        private readonly PlayerProxy _playersProxy;
        private readonly System.Timers.Timer _aTimer = new System.Timers.Timer
        {
            Interval = 2000,
            Enabled = true
        };

        private string _guide;
        public string Guide
        {
            get { return _guide; }
            private set
            {
                _guide = value;
                NotifyOfPropertyChange(() => Guide);
            }
        }

        private string _bottomGuide;
        public string BottomGuide
        {
            get { return _bottomGuide; }
            private set
            {
                _bottomGuide = value;
                NotifyOfPropertyChange(() => BottomGuide);
            }
        }

        private string _bgVideo;
        public string BgVideo
        {
            get { return _bgVideo; }
            private set
            {
                _bgVideo = value;
                NotifyOfPropertyChange(() => BgVideo);
            }
        }

        private string _twitterHandle;
        public string TwitterHandle
        {
            get { return _twitterHandle; }
            private set
            {
                _twitterHandle = value;
                NotifyOfPropertyChange(() => TwitterHandle);
            }
        }

        private string _playerName;
        public string PlayerName
        {
            get { return _playerName; }
            private set
            {
                _playerName = value;
                NotifyOfPropertyChange(() => PlayerName);
            }
        }

        private bool _inputTextBoxVisible;
        public bool InputTextBoxVisible
        {
            get { return _inputTextBoxVisible; }
            set
            {
                _inputTextBoxVisible = value;
                NotifyOfPropertyChange(() => InputTextBoxVisible);
            }
        }

        private string _inputTextValue;
        public string InputTextValue
        {
            get { return _inputTextValue; }
            set
            {
                _inputTextValue = value;
                OnInput();
                NotifyOfPropertyChange(() => InputTextValue);
            }
        }

        private BitmapImage _twitterPhoto;
        public BitmapImage TwitterPhoto
        {
            get { return _twitterPhoto; }
            private set
            {
                _twitterPhoto = value;
                NotifyOfPropertyChange(() => TwitterPhoto);
            }
        }

        private readonly IConductor _conductor;
        private readonly KinectSensor _sensor;
        private KinectAudioStream _convertStream;
        private SpeechRecognitionEngine _speechEngine;

        public WelcomeViewModel(IConductor conductor, KinectSensor kinectSensor)
        {
            _conductor = conductor;
            _sensor = kinectSensor;
            _cache = new MemoryCache(GetType().FullName);
            _playersProxy = new PlayerProxy(_cache, _twitterApiKey, _twitterApiSecret, _twitterScreenName);
            _aTimer.Elapsed += (sender, args) =>
            {
                _isTyping = false;
            };
        }

        protected async override void OnActivate()
        {
            TextBox textBox = null;
            var frameworkElement = GetView() as FrameworkElement;
            if (frameworkElement != null)
            {
                var video = frameworkElement.FindName("VideoElement") as MediaElement;
                if (video != null)
                {
                   video.LoadedBehavior = MediaState.Manual;
                   video.Play();
                   video.MediaEnded += (sender, args) =>
                   {
                        video.Position = TimeSpan.Zero;
                        video.Play();
                   };
                }
                textBox = frameworkElement.FindName("TextInputBox") as TextBox;
            }
            
            BottomGuide = "Waiting for Kinect...";
            BgVideo = "Resources\\Videos\\Video.mp4";
            TwitterHandle = string.Empty;
            PlayerName = string.Empty;
            TwitterPhoto = null;
            
            var names = new Choices();
            await InitKinectAsync();
            BottomGuide = "Loading...";
            await Task.Factory.StartNew(t => 
                {
                    try
                    {
                        var players = _playersProxy.GetAllPlayers().Result;
                        foreach (var p in players)
                        {
                            names.Add(new SemanticResultValue(p.Name, p.Id));
                            names.Add(new SemanticResultValue(p.TwitterHandle, p.Id));
                        }
                    }
                    catch (Exception)
                    {
                        this.TryClose();    
                    }
                }, TaskCreationOptions.LongRunning);

            //Initialize the speech recognition engine
            SpeechInitialization(names, NameRecognized);
            BottomGuide = string.Empty;
            BgVideo = "Resources\\Videos\\Video2.mp4"; ;
            Guide = "What's your name?";
            InputTextBoxVisible = true;
            if (textBox != null)
            {
                textBox.Focus();
            }
        }

        readonly TaskCompletionSource<bool> _resultCompletionSource = new TaskCompletionSource<bool>();
        Task<bool> InitKinectAsync()
        {
            _sensor.Open();
            _sensor.IsAvailableChanged += Sensor_IsAvailableChanged;
            return _resultCompletionSource.Task;
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (_sensor != null && _sensor.IsAvailable && !_resultCompletionSource.Task.IsCompleted)
            {
                // grab the audio stream
                IReadOnlyList<AudioBeam> audioBeamList = _sensor.AudioSource.AudioBeams;
                Stream audioStream = audioBeamList[0].OpenInputStream();
                _convertStream = new KinectAudioStream(audioStream);
                _sensor.IsAvailableChanged -= Sensor_IsAvailableChanged;
                _resultCompletionSource.SetResult(true);
            }
        }

        private void SpeechInitialization(Choices names, EventHandler<SpeechRecognizedEventArgs> success)
        {
            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {
                _speechEngine = new SpeechRecognitionEngine(ri.Id);

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(names);

                var g = new Grammar(gb);
                _speechEngine.LoadGrammar(g);

                _speechEngine.SpeechRecognized += success;

                // let the convertStream know speech is going active
                _convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                _speechEngine.SetInputToAudioStream(
                    _convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        private bool _isTyping;
        private async void OnInput()
        {
            if (!_isTyping)
            {
                _isTyping = true;
                _aTimer.Start();
                _player = await PlayerSearch(InputTextValue);
                if (_player != null)
                {
                    UpdatePlayer();
                }

            }
        }

        private void UpdatePlayer()
        {
            TwitterHandle = '@' + _player.TwitterHandle;
            PlayerName = _player.Name;
            TwitterPhoto = new BitmapImage(new Uri(_player.TwitterPhoto));
        }

        private async Task<Player> PlayerSearch(string query)
        {
            Player found = null;
            await Task.Run(() =>
            {
                var players = _playersProxy.GetAllPlayers().Result;
                found = players.FirstOrDefault(x => x.Name.StartsWith(query) || x.TwitterHandle.StartsWith(query));
            });
            return found;
        }

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void NameRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                if ((int) e.Result.Semantics.Value == 0)
                {
                    TwitterHandle = "Guest";
                    PlayerName = "Guest";
                }
                else
                {
                    var dbRepo = new JumpFocusContext();
                    _player = dbRepo.Players.Single(p => p.Id == (int) e.Result.Semantics.Value);
                    if (_player != null)
                    {
                        UpdatePlayer();
                    }
                }

                var handles = new Choices();
                handles.Add(new SemanticResultValue("yes", true));
                handles.Add(new SemanticResultValue("no", false));

                Guide = "Confirm with yes or no";
                //Reinitialize the speech recognition engine for the yes/no
                SpeechInitialization(handles, YesRecognized);
            }
        }

        private void YesRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            const double ConfidenceThresold = 0.3;

            if (e.Result.Confidence >= ConfidenceThresold)
            {
                if ((bool)e.Result.Semantics.Value)
                {
                    _conductor.ActivateItem(new JumpViewModel(_conductor, _sensor, _player));
                }
                else
                {
                    OnActivate();
                }
            }
        }

        /// <summary>
        /// Gets the metadata for the speech recognizer (acoustic model) most suitable to
        /// process audio from Kinect device.
        /// </summary>
        /// <returns>
        /// RecognizerInfo if found, <code>null</code> otherwise.
        /// </returns>
        private static RecognizerInfo GetKinectRecognizer()
        {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers())
            {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return recognizer;
                }
            }

            return null;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        protected override void OnDeactivate(bool close)
        {
            if (null != _convertStream)
            {
                _convertStream.SpeechActive = false;
            }
            if (null != _speechEngine)
            {
                _speechEngine.SpeechRecognized -= NameRecognized;
                _speechEngine.RecognizeAsyncStop();
            }
            if (null != _sensor)
            {
                _sensor.Close();
            }
        }
      
    }
}
