using System.Configuration;
using Caliburn.Micro;
using Microsoft.Kinect;

namespace JumpFocus.ViewModels
{
    class MainViewModel : Conductor<IScreen>
    {
        public MainViewModel()
        {
            var sensor = KinectSensor.GetDefault();
            ActivateItem(new WelcomeViewModel(this, sensor));
            DisplayName = ConfigurationManager.AppSettings["appName"];
        }

        public override sealed void ActivateItem(IScreen item)//see http://goo.gl/krdbwl
        {
            base.ActivateItem(item);
        }
    }
}
