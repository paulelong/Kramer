using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Identity.Client;
using Xamarin.Forms;

namespace MyMixes
{
	public partial class App : Application
	{
        public static UIParent UiParent = null;

        public App ()
		{
			InitializeComponent();

			MainPage = new NavigationPage(new MyMixes.MainPage());
		}

		protected override void OnStart ()
		{
            // Handle when your app starts
            AppCenter.Start("uwp=99bfe487-df4f-427c-8592-287d7f9e8044;" + "android=9669e969-37c6-41a9-a3e4-8f457cf3d4f0;" + "ios=5575289c-8866-43b4-823b-f707cdfca1cd", typeof(Analytics), typeof(Crashes));
        }

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
