using Caliburn.Micro;
using JumpFocus.DAL;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace JumpFocus.ViewModels
{
    class LeaderBoardViewModel : Screen
    {
        private readonly IConductor _conductor;
        private readonly KinectSensor _sensor;

        private IEnumerable<LeaderScoreItem> _scores;
        public IEnumerable<LeaderScoreItem> Scores
        {
            get { return _scores; }
            private set
            {
                _scores = value;
                NotifyOfPropertyChange(() => Scores);
            }
        }

        public LeaderBoardViewModel(IConductor conductor, KinectSensor kinectSensor)
        {
            _conductor = conductor;
            _sensor = kinectSensor;
        }

        protected override void OnActivate()
        {
            var dbRepo = new JumpFocusContext();
            var i = 0;
            Scores = dbRepo.Histories.OrderByDescending(h => h.Altitude + h.Dogecoins)
                .Select(x => new LeaderScoreItem
                {
                    Name = x.Player.TwitterHandle,
                    Score = x.Altitude + x.Dogecoins
                }).Take(10).ToList();

            foreach (var s in Scores)
            {
                s.Rank = ++i;
                switch (s.Rank)
                {
                    case 1:
                        s.RankSuperscript = "ST";
                        break;
                    case 2:
                        s.RankSuperscript = "ND";
                        break;
                    case 3:
                        s.RankSuperscript = "RD";
                        break;
                    default:
                        s.RankSuperscript = "TH";
                        break;
                }
            }

            //var t = new Timer(state => _conductor.ActivateItem(new WelcomeViewModel(_conductor, _sensor)));
            var t = new Timer(state => _conductor.ActivateItem(((MainViewModel)_conductor).WelcomeViewModel));
            t.Change(10000, Timeout.Infinite);//waits 10sec
        }
    }

    class LeaderScoreItem
    {
        public int Rank { get; set; }
        public string RankSuperscript { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
    }
}
