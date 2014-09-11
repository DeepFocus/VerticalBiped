using Caliburn.Micro;
using JumpFocus.DAL;
using System.Collections.Generic;
using System.Linq;

namespace JumpFocus.ViewModels
{
    class LeaderBoardViewModel : Screen
    {
        private readonly IConductor _conductor;

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

        public LeaderBoardViewModel(IConductor conductor)
        {
            _conductor = conductor;
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
            base.OnActivate();
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
