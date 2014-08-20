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
        public MainViewModel()
        {
            var sensor = KinectSensor.GetDefault();
            ActivateItem(new WelcomeViewModel(this, sensor));
        }

        public override sealed void ActivateItem(IScreen item)//see http://goo.gl/krdbwl
        {
            base.ActivateItem(item);
        }
    }
}
