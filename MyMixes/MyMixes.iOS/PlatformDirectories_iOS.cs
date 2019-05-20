using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using MyMixes.iOS;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformDirectories_iOS))]
namespace MyMixes.iOS
{
    class PlatformDirectories_iOS : IPlatformDirectories
    {
        public string GetDownloadDirectory()
        {
           return null;
        }
    }
}