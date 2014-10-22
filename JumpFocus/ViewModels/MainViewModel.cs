using System.Configuration;
using Caliburn.Micro;
using Microsoft.Kinect;
using JumpFocus.Configurations;

namespace JumpFocus.ViewModels
{
    class MainViewModel : Conductor<IScreen>
    {
        public WelcomeViewModel WelcomeViewModel { get; set; }

        public MainViewModel()
        {
            WelcomeViewModel = new WelcomeViewModel(this);

            ActivateItem(WelcomeViewModel);
            DisplayName = ConfigurationManager.AppSettings["appName"];
        }

        protected override void OnActivate()
        {
            AutoMapperConfiguration.RegisterMappings();
            base.OnActivate();
        }

        public override sealed void ActivateItem(IScreen item)//see http://goo.gl/krdbwl
        {
            base.ActivateItem(item);
        }
    }
}
