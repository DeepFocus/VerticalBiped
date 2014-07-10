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
        private KinectSensor _sensor;

        private JumpViewModel _jumpViewModel;

        public MainViewModel()
        {
            _sensor = KinectSensor.GetDefault();

            _jumpViewModel = new JumpViewModel(_sensor);

            ShowJump();
        }

        public void ShowJump()
        {
            ActivateItem(_jumpViewModel);
        }
    }
}
