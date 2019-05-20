using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using Microsoft.Identity.Client;
using OAuthNativeFlow;

namespace MyMixes.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            global::Xamarin.Forms.Forms.Init();

            global::Xamarin.Auth.Presenters.XamarinIOS.AuthenticationConfiguration.Init();

            LoadApplication(new App());

            var x = typeof(Xamarin.Forms.Themes.DarkThemeResources);
            if (x == typeof(Xamarin.Forms.Themes.DarkThemeResources))
            {
                x = typeof(Xamarin.Forms.Themes.iOS.UnderlineEffect);
            }


            return base.FinishedLaunching(app, options);
        }
        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (url.AbsoluteString.Contains("google"))
            {
                var uri_netfx = new Uri(url.AbsoluteString);
                AuthenticationState.Authenticator.OnPageLoading(uri_netfx);
            }
            else
            {
                AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(url);
            }

            return true;
        }
    }
}
