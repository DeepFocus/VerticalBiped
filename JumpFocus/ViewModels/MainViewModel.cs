using System.Configuration;
using Caliburn.Micro;
using JumpFocus.Models;
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
            ActivateItem(new JumpViewModel(this, new Player()));
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
