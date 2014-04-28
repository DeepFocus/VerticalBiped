using Caliburn.Micro;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace JumpFocus.ViewModels
{
    class WelcomeViewModel : Screen
    {
        public int Count { get; set; }

        private KinectSensor _sensor;
        private BodyFrameReader _frame;
        private FrameDescription _colorFrameDescription;

        public WelcomeViewModel(KinectSensor KinectSensor)
        {
            _sensor = KinectSensor;
        }

        private void BodyFrameCaptured(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = _frame.AcquireLatestFrame())
            {
                if (null != bodyFrame)
                {

                }
            }
        }

        protected override void OnActivate()
        {
            if (null != _sensor)
            {
                _sensor.Open();
                _colorFrameDescription = _sensor.ColorFrameSource.FrameDescription;

                _frame = _sensor.BodyFrameSource.OpenReader();
                _frame.FrameArrived += BodyFrameCaptured;
            }
        }

        protected override void OnDeactivate(bool close)
        {
            if (_frame != null)
            {
                _frame.Dispose();
            }
        }
    }
}
