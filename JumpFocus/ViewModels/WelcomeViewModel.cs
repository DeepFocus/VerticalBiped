using System.Configuration;
using System.IO;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using JumpFocus.Models;
using JumpFocus.Models.API;
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
        private readonly TwitterConfig _twitterConfig = new TwitterConfig
        {
            AccessToken = ConfigurationManager.AppSettings["TwitterAccessToken"],
            AccessTokenSecret = ConfigurationManager.AppSettings["TwitterAccessTokenSecret"],
            ConsumerKey = ConfigurationManager.AppSettings["TwitterConsumerKey"],
            ConsumerSecret = ConfigurationManager.AppSettings["TwitterConsumerSecret"]
        };
        private readonly string _twitterScreenName = ConfigurationManager.AppSettings["TwitterScreenName"];

        private readonly PlayerProxy _playersProxy;
        private Choices _names;
        private TextBox _textBox;

        private readonly DispatcherTimer _aTimer = new DispatcherTimer
        {
            Interval = new TimeSpan(0, 0, 0, 2),
            IsEnabled = true
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

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange(() => IsLoading);
            }
        }

        private string _inputTextValue;
        public string InputTextValue
        {
            get { return _inputTextValue; }
            set
            {
                _inputTextValue = value;
                NotifyOfPropertyChange(() => InputTextValue);
            }
        }

        private string _foundTextValue;
        public string FoundTextValue
        {
            get { return _foundTextValue; }
            set
            {
                _foundTextValue = value;
                NotifyOfPropertyChange(() => FoundTextValue);
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
            _playersProxy = new PlayerProxy(_cache, _twitterConfig, _twitterScreenName);
        }

        protected async override void OnViewLoaded(object view)
        {
            _aTimer.Tick += OnInput;
            var frameworkElement = view as FrameworkElement;
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
            }

            BottomGuide = "Waiting for Kinect...";
            BgVideo = "Resources\\Videos\\Video.mp4";
            TwitterHandle = string.Empty;
            PlayerName = string.Empty;
            TwitterPhoto = null;

            await InitKinectAsync();
            BottomGuide = "Loading...";
            //await Task.Factory.StartNew(t => 
            //    {
            //        try
            //        {
            //            var players = _playersProxy.GetAllPlayers().Result;
            //            _names = new Choices();
            //            foreach (var p in players)
            //            {
            //                _names.Add(new SemanticResultValue(p.Name, p.Id));
            //                _names.Add(new SemanticResultValue(p.TwitterHandle, p.Id));
            //            }
            //        }
            //        catch (Exception)
            //        {
            //            this.TryClose();    
            //        }
            //    }, TaskCreationOptions.LongRunning);

            await _playersProxy.CacheWarmup();
            if (null != frameworkElement)
            {
                var stack = frameworkElement.FindName("NameStack") as StackPanel;
                if (null != stack)
                {
                    stack.Visibility = Visibility.Visible;
                }
                _textBox = frameworkElement.FindName("InputTextValue") as TextBox;
                if (null != _textBox)
                {
                    _textBox.Focus();
                }
            }
            InitNames();

            base.OnViewLoaded(view);
        }

        protected override void OnDeactivate(bool close)
        {
            _aTimer.Tick -= OnInput;
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

            base.OnDeactivate(close);
        }

        private void InitNames()
        {
            //Initialize the speech recognition engine with user names
            //Generates a "The calling thread must be STA, because many UI components require this." exception when coming back to this screen

            _player = null;
            UpdatePlayer();
            InputTextBoxVisible = true;

            BottomGuide = string.Empty;
            Guide = "What's your name?";
            BgVideo = "Resources\\Videos\\Video2.mp4";
            //Task.Run( () => SpeechInitialization(_names, NameRecognized));
        }

        readonly TaskCompletionSource<bool> _resultCompletionSource = new TaskCompletionSource<bool>();
        private async Task<bool> InitKinectAsync()
        {
            _sensor.Open();
            _sensor.IsAvailableChanged += Sensor_IsAvailableChanged;
            return await _resultCompletionSource.Task;
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


                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);
                _convertStream.SpeechActive = false;
                _speechEngine.SetInputToAudioStream(
                    _convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                _convertStream.SpeechActive = true;
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }

        #region Input Text Handling
        private bool _isTyping;

        private async void OnInput(object sender, EventArgs e)
        {
            if (null != _textBox && !string.IsNullOrWhiteSpace(_textBox.Text))
            {
                _isTyping = true;
                _aTimer.Start();
                IsLoading = true;
                _player = await PlayerSearch(InputTextValue);
                UpdatePlayer();
                IsLoading = false;
                _isTyping = false;
            }
        }

        private void UpdatePlayer()
        {
            if (_player == null)
            {
                TwitterHandle = string.Empty;
                PlayerName = string.Empty;
                TwitterPhoto = null;
                InputTextValue = string.Empty;
                FoundTextValue = string.Empty;
                IsLoading = false;
                return;
            }
            FoundTextValue = _player.TwitterHandle.Substring(_textBox.Text.Length);
            PlayerName = _player.Name;
            TwitterPhoto = new BitmapImage(new Uri(_player.TwitterPhoto));
            RecognizeConfirmation();
        }

        private async Task<Player> PlayerSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }
            Player found = null;
            if (null != _playersProxy.Players)
            {
                await Task.Run(() =>
                {
                    found =
                        _playersProxy.Players.FirstOrDefault(
                            x => x.Name.StartsWith(query) || x.TwitterHandle.StartsWith(query));
                });
            }
            return found;
        }
        #endregion

        /// <summary>
        /// Handler for recognized speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void NameRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double confidenceThreshold = 0.3;

            if (e.Result.Confidence >= confidenceThreshold)
            {
                //if ((int) e.Result.Semantics.Value == 0)
                //{
                //    TwitterHandle = "Guest";
                //    PlayerName = "Guest";
                //}
                //else
                //{
                //}
                var dbRepo = new JumpFocusContext();
                _player = dbRepo.Players.Single(p => p.Id == (int)e.Result.Semantics.Value);
                UpdatePlayer();
            }
        }

        private bool _isConfirming;
        private void RecognizeConfirmation()
        {
            _isConfirming = true;
            var handles = new Choices();
            handles.Add(new SemanticResultValue("yes", true));
            handles.Add(new SemanticResultValue("no", false));

            Guide = "Confirm with yes or no";
            //Reinitialize the speech recognition engine for the yes/no
            SpeechInitialization(handles, YesRecognized);
        }

        private void YesRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            const double confidenceThresold = 0.3;

            if (e.Result.Confidence >= confidenceThresold)
            {
                if ((bool)e.Result.Semantics.Value)
                {
                    _conductor.ActivateItem(new JumpViewModel(_conductor, _sensor, _player));
                }
                else
                {
                    OnActivate();
                }
                _isConfirming = false;
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

        private ICommand _escapeCommand;
        public ICommand EscapeCommand
        {
            get
            {
                return _escapeCommand
                    ?? (_escapeCommand = new ActionCommand(() =>
                    {
                        InputTextValue = string.Empty;
                        IsLoading = false;
                        if (_isConfirming)
                        {
                            _isConfirming = false;
                            InitNames();
                        }
                    }));
            }
        }

        private ICommand _confirmCommand;
        public ICommand ConfirmCommand
        {
            get
            {
                return _confirmCommand
                    ?? (_confirmCommand = new ActionCommand(() =>
                    {
                        if (_player != null)
                        {
                            _conductor.ActivateItem(new JumpViewModel(_conductor, _sensor, _player));
                        }
                    }));
            }
        }
    }

    public class ActionCommand : ICommand
    {
        private readonly System.Action _action;

        public ActionCommand(System.Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

    }
}
