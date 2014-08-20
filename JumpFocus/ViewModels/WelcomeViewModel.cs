using System.Configuration;
using System.IO;
using System.Runtime.Caching;
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
        private readonly ObjectCache _cache;
        private readonly string _twitterApiKey = ConfigurationManager.AppSettings["TwitterApiKey"];
        private readonly string _twitterApiSecret = ConfigurationManager.AppSettings["TwitterApiSecret"];
        private readonly string _twitterScreenName = ConfigurationManager.AppSettings["TwitterScreenName"];

        private Player _player;

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
        }

        protected async override void OnActivate()
        {
            Guide = "Loading...";
            TwitterHandle = string.Empty;
            PlayerName = string.Empty;
            TwitterPhoto = null;

            if (null == _sensor)
            {
                // on failure
                return;
            }

            _sensor.Open();

            // grab the audio stream
            IReadOnlyList<AudioBeam> audioBeamList = _sensor.AudioSource.AudioBeams;
            Stream audioStream = audioBeamList[0].OpenInputStream();

            // create the convert stream
            _convertStream = new KinectAudioStream(audioStream);

            var proxy = new PlayerProxy(_cache, _twitterApiKey, _twitterApiSecret, _twitterScreenName);
            var handles = new List<SemanticResultValue>();
            foreach (var p in await proxy.GetAllPlayers())
            {
                handles.Add(new SemanticResultValue(p.Name, p.Id));
                handles.Add(new SemanticResultValue(p.TwitterHandle, p.Id));
            }

            //Initialize the speech recognition engine
            SpeechInitialization(handles, NameRecognized);
            
            Guide = "What's your name?";
        }

        private void SpeechInitialization(IEnumerable<SemanticResultValue> input, EventHandler<SpeechRecognizedEventArgs> success)
        {
            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {
                _speechEngine = new SpeechRecognitionEngine(ri.Id);

                var handles = new Choices();
                foreach (var i in input)
                {
                    handles.Add(i);
                }

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(handles);

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

                    TwitterHandle = '@' + _player.TwitterHandle;
                    PlayerName = _player.Name;
                    TwitterPhoto = new BitmapImage(new Uri(_player.TwitterPhoto));
                }

                var handles = new List<SemanticResultValue>()
                {
                    new SemanticResultValue("yes", true),
                    new SemanticResultValue("no", false)
                };

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
