using System.Configuration;
using Caliburn.Micro;
using Microsoft.Kinect;

namespace JumpFocus.ViewModels
{
    class MainViewModel : Conductor<IScreen>
    {
        public WelcomeViewModel WelcomeViewModel { get; set; }

        public MainViewModel()
        {
            var sensor = KinectSensor.GetDefault();
            WelcomeViewModel = new WelcomeViewModel(this, sensor);

            ActivateItem(WelcomeViewModel);
            DisplayName = ConfigurationManager.AppSettings["appName"];
        }

        public override sealed void ActivateItem(IScreen item)//see http://goo.gl/krdbwl
        {
            base.ActivateItem(item);
        }
    }
}
