﻿using Caliburn.Micro;
using JumpFocus.DAL;
using JumpFocus.Models;
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

        private History _lastPlayed;
        public History LastPlayed
        {
            get { return _lastPlayed; }
            private set
            {
                _lastPlayed = value;
                NotifyOfPropertyChange(() => Scores);
            }
        }



        private LeaderScoreItem[] _scores;
        public LeaderScoreItem[] Scores
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
            _lastPlayed = dbRepo.Histories.Include("Player").OrderByDescending(h => h.Played).First();
            Scores = dbRepo.Histories.OrderByDescending(h => h.Altitude + h.Dogecoins)
                .Select(x => new LeaderScoreItem
                {
                    Id = x.Player.Id,
                    Name = x.Player.TwitterHandle,
                    Score = x.Altitude + x.Dogecoins
                }).Take(10).ToArray();
            //If not in the first 10 ones, remove the last one and add the current user
            if (Scores.All(s => s.Id != _lastPlayed.Player.Id))
            {
                Scores[Scores.Length - 1] = new LeaderScoreItem
                {
                    Id = _lastPlayed.Player.Id,
                    Name = _lastPlayed.Player.TwitterHandle,
                    Score = _lastPlayed.Altitude + _lastPlayed.Dogecoins,
                    Rank = dbRepo.Histories.Count(p => p.Altitude + p.Dogecoins > _lastPlayed.Altitude + _lastPlayed.Dogecoins)
                };
            }
            for (int index = 0; index < Scores.Length; index++)
            {
                var s = Scores[index];
                if (s.Id == _lastPlayed.Player.Id)
                {
                    s.BackgroundColor = "#EC7C5F";
                }
                s.Rank = s.Rank == 0 ? index + 1 : s.Rank;
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

            var t = new Timer(state => _conductor.ActivateItem(((MainViewModel)_conductor).WelcomeViewModel));
            t.Change(15000, Timeout.Infinite);//waits 10sec
        }
    }
}
