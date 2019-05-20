using MyMixes.Droid;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformDirectories_Android))]
namespace MyMixes.Droid
{
    class PlatformDirectories_Android : IPlatformDirectories
    {
        public string GetDownloadDirectory()
        {
            return Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, Android.OS.Environment.DirectoryDownloads);
        }
    }
}