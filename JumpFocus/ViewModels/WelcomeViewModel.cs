using System.Configuration;
using System.Data.Entity.Migrations;
using System.Windows.Documents;
using Caliburn.Micro;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using JumpFocus.Repositories;
using JumpFocus.Models.API;
using JumpFocus.DAL;
using JumpFocus.Models;

namespace JumpFocus.ViewModels
{
    class WelcomeViewModel : Screen
    {
        private readonly string _twitterApiKey = ConfigurationManager.AppSettings["TwitterApiKey"];
        private readonly string _twitterApiSecret = ConfigurationManager.AppSettings["TwitterApiSecret"];
        private readonly string _twitterScreenName = ConfigurationManager.AppSettings["TwitterScreenName"];

        private string _twitter;
        public string Twitter
        {
            get { return _twitter; }
            private set
            {
                _twitter = value;
                NotifyOfPropertyChange(() => Twitter);
            }
        }

        private readonly KinectSensor _sensor;
        private KinectAudioStream _convertStream;
        private SpeechRecognitionEngine _speechEngine;

        public WelcomeViewModel(KinectSensor kinectSensor)
        {
            _sensor = kinectSensor;
            Twitter = "test";
        }

        protected async override void OnActivate()
        {
            Twitter = "Loading...";

            var twitterRepo = new TwitterRepository(_twitterApiKey, _twitterApiSecret);

            //initiallize the cursor to the default value
            var ids = new List<long>();
            var followersIds = new TwitterFollowersIds
            {
                next_cursor = -1
            };
            do
            {
                followersIds = await twitterRepo.GetFollowersIds(_twitterScreenName, followersIds.next_cursor);
                if (null != followersIds)
                {
                    ids.AddRange(followersIds.ids);
                }
            } while (null != followersIds && followersIds.next_cursor > 0);

            var dbRepo = new JumpFocusContext();
            var dbUserTwitterIds = from p in dbRepo.Players
                                   where p.Name != null && p.Name.Trim() != string.Empty
                                   select p.TwitterId;
            ids.RemoveAll(dbUserTwitterIds.Contains);

            long[] users;
            while ((users = ids.Take(100).ToArray()).Length > 0) //100 is the twitter API limit
            {
                var players = await twitterRepo.PostUsersLookup(users);
                if (null != players)
                {
                    foreach (var player in players)
                    {
                        var p = new Player
                        {
                            TwitterId = player.id,
                            Name = player.name,
                            TwitterHandle = player.screen_name,
                            TwitterPhoto = player.profile_image_url,
                            Created = DateTime.Now
                        };
                        dbRepo.Players.AddOrUpdate(p);
                    }
                }
                ids.RemoveAll(users.Contains);
                dbRepo.SaveChanges();
            }

            if (null == _sensor)
            {
                // on failure
                return;
            }

            _sensor.Open();

            // grab the audio stream
            IReadOnlyList<AudioBeam> audioBeamList = _sensor.AudioSource.AudioBeams;
            System.IO.Stream audioStream = audioBeamList[0].OpenInputStream();

            // create the convert stream
            _convertStream = new KinectAudioStream(audioStream);

            RecognizerInfo ri = GetKinectRecognizer();

            if (null != ri)
            {
                _speechEngine = new SpeechRecognitionEngine(ri.Id);

                var handles = new Choices();
                foreach (var p in dbRepo.Players)
                {
                    handles.Add(new SemanticResultValue(p.Name, p.Id));
                    handles.Add(new SemanticResultValue(p.TwitterHandle, p.Id));
                }

                var gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(handles);

                var g = new Grammar(gb);
                _speechEngine.LoadGrammar(g);

                _speechEngine.SpeechRecognized += NameRecognized;
                _speechEngine.SpeechRecognitionRejected += SpeechRejected;

                // let the convertStream know speech is going active
                _convertStream.SpeechActive = true;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                ////speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                _speechEngine.SetInputToAudioStream(
                    _convertStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                _speechEngine.RecognizeAsync(RecognizeMode.Multiple);

                Twitter = "Ready";
            }
            else
            {
                //error no speech recognizer
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

            //this.ClearRecognitionHighlights();

            if (e.Result.Confidence >= ConfidenceThreshold)
            {
                Twitter = e.Result.Text;
            }
        }

        /// <summary>
        /// Handler for rejected speech events.
        /// </summary>
        /// <param name="sender">object sending the event.</param>
        /// <param name="e">event arguments.</param>
        private void SpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            //propose to follow on twitter
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
                _speechEngine.SpeechRecognitionRejected -= SpeechRejected;
                _speechEngine.RecognizeAsyncStop();
            }
            if (null != _sensor)
            {
                _sensor.Close();
            }
        }
    }
}
