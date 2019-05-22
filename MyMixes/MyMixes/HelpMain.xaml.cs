using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;


using Version.Plugin;

namespace MyMixes
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HelpMain : ContentPage
    {
        public HelpMain()
        {
            InitializeComponent();

            VersionText.Text = "Version: " + CrossVersion.Current.Version.ToString();
        }

        private void OpenWeb_Clicked(object sender, EventArgs e)
        {
            Uri website = new Uri("https://riffrecorder.wordpress.com/");
            Device.OpenUri(website);
        }

        private void OpenVideo_Clicked(object sender, EventArgs e)
        {
            Uri website = new Uri("https://youtu.be/drOQs1A-c_8");
            Device.OpenUri(website);
        }

        // https://youtu.be/drOQs1A-c_8
    }
}