using Caliburn.Micro;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JumpFocus.ViewModels
{
    class MainViewModel : Conductor<IScreen>
    {
        private readonly JumpViewModel _jumpViewModel;
        private readonly WelcomeViewModel _welcomeViewModel;

        public MainViewModel()
        {
            var sensor = KinectSensor.GetDefault();

            _jumpViewModel = new JumpViewModel(sensor);
            _welcomeViewModel = new WelcomeViewModel(sensor);

            //ShowJump();
            ShowWelcome();
        }

        public void ShowJump()
        {
            ActivateItem(_jumpViewModel);
        }

        public void ShowWelcome()
        {
            ActivateItem(_welcomeViewModel);
        }
    }
}
