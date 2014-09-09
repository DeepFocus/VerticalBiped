using Caliburn.Micro;
using JumpFocus.DAL;
using JumpFocus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JumpFocus.ViewModels
{
    class LeaderBoardViewModel : Screen
    {
        private readonly IConductor _conductor;

        private IEnumerable<History> _scores;
        public IEnumerable<History> Scores
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
            Scores = dbRepo.Histories.OrderByDescending(h => h.Altitude + h.Dogecoins).Take(10).ToList();

            base.OnActivate();
            //_conductor.ActivateItem(new MainViewModel());
        }
    }
}
