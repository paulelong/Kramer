﻿using System;

using Android.App;
using Android.OS;
using Android.Runtime;
using Plugin.CurrentActivity;

using Microsoft.Identity.Client;
using Android.Views;

namespace MyMixes.Droid
{
    [Application]
    public class MainApplication : Application, Application.IActivityLifecycleCallbacks
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transer)
            : base(handle, transer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            RegisterActivityLifecycleCallbacks(this);
            //A great place to initialize Xamarin.Insights and Dependency Services!
        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            UnregisterActivityLifecycleCallbacks(this);
        }

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)

        {
            CrossCurrentActivity.Current.Activity = activity;
            App.UiParent = new UIParent(activity, useEmbeddedWebview: true);
        }

        public void OnActivityDestroyed(Activity activity)
        {
        }

        public void OnActivityPaused(Activity activity)
        {
        }

        public void OnActivityResumed(Activity activity)
        {
            CrossCurrentActivity.Current.Activity = activity;
            App.UiParent = new UIParent(activity);
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        public void OnActivityStarted(Activity activity)
        {
            CrossCurrentActivity.Current.Activity = activity;
            App.UiParent = new UIParent(activity);
        }

        public void OnActivityStopped(Activity activity)
        {
        }
    }
}