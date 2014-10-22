using System.Configuration;
using System.Runtime.Caching;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using JumpFocus.Models;
using JumpFocus.Models.API;
using Microsoft.Speech.Recognition;
using System;
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
            Interval = new TimeSpan(0, 0, 0, 1),
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
                NotifyOfPropertyChange(() => NotLoading);
            }
        }

        private bool _isError;
        public bool IsError
        {
            get { return _isError; }
            private set
            {
                _isError = value;
                NotifyOfPropertyChange(() => IsError);
            }
        }

        private Brush _borderColor;
        public Brush BorderColor
        {
            get { return _borderColor; }
            private set
            {
                _borderColor = value;
                NotifyOfPropertyChange(() => BorderColor);
                NotifyOfPropertyChange(() => NotLoading);
            }
        }

        public bool NotLoading
        {
           get { return !IsLoading && !IsError;  }
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

        public WelcomeViewModel(IConductor conductor)
        {
            _conductor = conductor;
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
            BottomGuide = "Loading...";
            
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
            base.OnDeactivate(close);
        }

        private void InitNames()
        {
                        //Generates a "The calling thread must be STA, because many UI components require this." exception when coming back to this screen

            _player = null;
            UpdatePlayer();
            InputTextBoxVisible = true;
            IsError = false;
            BorderColor = Brushes.WhiteSmoke;

            BottomGuide = string.Empty;
            Guide = "What's your name?";
            BgVideo = "Resources\\Videos\\Video2.mp4";
        }

        #region Input Text Handling
        private bool _isTyping;
        private string previousValue = "";

        private async void OnInput(object sender, EventArgs e)
        {
            if (!_isTyping && previousValue != InputTextValue)
            {
                _isTyping = true;
                IsError = false;
                BorderColor = Brushes.WhiteSmoke;
                IsLoading = true;
                TwitterPhoto = null;
                TwitterHandle = string.Empty;
                previousValue = InputTextValue;
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
                if (InputTextValue != string.Empty)
                {
                    IsError = true; 
                    BorderColor = Brushes.Red;                    
                }
                IsLoading = false;
                              
                PlayerName = string.Empty;
                TwitterHandle = string.Empty;
                TwitterPhoto = null;
                FoundTextValue = string.Empty;

                return;
            }
            IsError = false;
            BorderColor = Brushes.WhiteSmoke;
            FoundTextValue = _player.TwitterHandle.Length >= _textBox.Text.Length ? _player.TwitterHandle.Substring(_textBox.Text.Length) : "";
            PlayerName = _player.Name;
            TwitterPhoto = new BitmapImage(new Uri(_player.TwitterPhoto));
        }

        private async Task<Player> PlayerSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            return await _playersProxy.FindPlayer(query);
        }
        #endregion

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
                        InitNames();
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
                            _conductor.ActivateItem(new JumpViewModel(_conductor, _player));
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
