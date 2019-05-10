using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using CloudStorage;
//using Microsoft.AppCenter;
using OAuthNativeFlow;
using Xamarin.Forms;

namespace MyMixes
{
    public class ProviderInfo
    {
        public delegate void UpdateStatus(string status);
        private static Dictionary<CloudProviders, ProviderInfo> providers = new Dictionary<CloudProviders, ProviderInfo>();

        public CloudProviders CloudProvider
        {
            get; set;
        }

        private ICloudStore cs = null;
        private ICloudStore CloudStore
        {
            get
            {
                return cs;
            }
        }

        public ProviderInfo(CloudProviders cp, ICloudStore cs)
        {
            this.cs = cs;
            this.CloudProvider = cp;
        }

        public async static Task<ProviderInfo> GetCloudProviderAsync(CloudProviders cp)
        {
            ICloudStore cs = null ;

            if (!providers.ContainsKey(cp))
            {
                ProviderInfo newProvider = null;

                switch (cp)
                {
                    case CloudProviders.OneDrive:
                        cs = CloudStoreFactory.CreateCloudStore(CloudStorage.CloudProviders.OneDrive);
                        Dictionary<string, object> onedriveparams = new Dictionary<string, object>();
                        onedriveparams[CloudParams.ClientID.ToString()] = "7ba22c7f-29be-4dc7-a274-4209fe0b8b72";
                        onedriveparams[CloudParams.UIParent.ToString()] = App.UiParent;

                        if (cs.Initialize(onedriveparams))
                        {
                            bool worked = await cs.AuthenticateAsync();

                            if (worked)
                            {
                                newProvider = new ProviderInfo(cp, cs);
                            }
                        }
                        break;
                    case CloudProviders.GoogleDrive:
                        cs = CloudStoreFactory.CreateCloudStore(CloudStorage.CloudProviders.GoogleDrive);

                        Dictionary<string, object> googledriveparams = new Dictionary<string, object>();
                        googledriveparams[CloudParams.ClientID.ToString()] = GetGoogleClientID();
                        // 133589155347-gj93njepme6jp96nh1erjmdi4q4c7d9k.apps.googleusercontent.com
                        // 133589155347-2he14os3etg7evt97pcu5jil1udh1klk.apps.googleusercontent.com 
                        googledriveparams[CloudParams.RedirectURL.ToString()] = GetGoogleAuthRedirect();
                        googledriveparams[CloudParams.AppName.ToString()] = "MyMixes";
                        googledriveparams[CloudParams.UIParent.ToString()] = App.UiParent;
                        googledriveparams[CloudParams.GoogleToken.ToString()] = null;

                        if (cs.Initialize(googledriveparams))
                        {
                            bool worked = await cs.AuthenticateAsync();
                            AuthenticationState.Authenticator = cs.GetAuthenticator();
                            bool authCompletWorked = await cs.CompleteAuthenticateAsync(AuthenticationState.Authenticator);
                            if (authCompletWorked)
                            {
                                newProvider = new ProviderInfo(cp, cs);
                            }
                        }
                        break;
                    default:
                        return null;
                }

                if(newProvider != null)
                {
                    providers[cp] = newProvider;
                    return newProvider;
                }
            }
            else
            {
                return providers[cp];
            }

            return null;
        }

        private static string GetGoogleClientID()
        {
            if (Device.RuntimePlatform == Device.iOS)
            {
                return "133589155347-2he14os3etg7evt97pcu5jil1udh1klk.apps.googleusercontent.com";
            }
            else
            {
                return "133589155347-gj93njepme6jp96nh1erjmdi4q4c7d9k.apps.googleusercontent.com";
            }
        }

        private static string GetGoogleAuthRedirect()
        {
            if (Device.RuntimePlatform == Device.iOS)
            {
                return "com.googleusercontent.apps.133589155347-2he14os3etg7evt97pcu5jil1udh1klk:/oauth2redirect";
            }
            else if (Device.RuntimePlatform == Device.Android)
            {
                return "com.googleusercontent.apps.133589155347-gj93njepme6jp96nh1erjmdi4q4c7d9k:/oauth2redirect";
            }
            else // UWP
            {
                return "com.paulyshotel.rr:/oauth2redirect";
            }
        }


        public async Task<List<string>> GetFoldersAsync(string folder)
        {
            try
            {
                var l = await CloudStore.GetFolderListAsync(folder);
                List<string> retl = new List<string>();
                foreach (var i in l)
                {
                    if(i.isFolder)
                        retl.Add(i.name);
                }

                return retl;
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());

                return null;
            }
        }

        private bool isAuthenticated = false;
        public bool IsAuthenticated
        {
            get
            {
                return isAuthenticated;
            }
            set
            {
                isAuthenticated = value;
                if (!isAuthenticated)
                {
                    providers.Remove(CloudProvider);
                }
            }
        }

        public async Task<bool> CheckAuthenitcationAsync()
        {
            if(!isAuthenticated)
            {
                if (cs != null)
                {
                    isAuthenticated = await CloudStore.AuthenticateAsync();
                }
            }

            return isAuthenticated;
        }

        public async Task<List<string>> UpdateProjectAsync(string root, string project, UpdateStatus UpdateStatusRoutine)
        {
            List<string> UpdatedSongs = new List<string>();

            try
            {
                string projectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), project);
                string remoteFolderName = "/" + root + "/" + project;

                var items = await cs.GetFolderListAsync(remoteFolderName);
                foreach (var di in items)
                {
                    if (isAudioFile(di))
                    {
                        UpdatedSongs.Add(di.name);

                        // Is local folder created?
                        var d = Directory.CreateDirectory(projectPath);
                        if (d != null)
                        {
                            string localFileName = projectPath + "/" + di.name;
                            DateTime localWriteTime = File.GetLastWriteTime(localFileName);

                            if(localWriteTime < di.modifiedDate)
                            {
                                using (Stream s = new FileStream(localFileName, FileMode.OpenOrCreate))
                                {
                                    Debug.Print("downloading " + localFileName + "\n");
                                    UpdateStatusRoutine("downloading " + project + "/" + di.name);
                                    if (!await CloudStore.DownloadFileAsync(remoteFolderName + "/" + di.name, s))
                                    {
                                        PersistentData.SetTrackNumber(projectPath, di.name, di.track);
                                        Debug.Print("FAILED " + localFileName + "\n");
                                    }

                                    s.Close();

                                    File.SetLastWriteTime(localFileName, di.modifiedDate);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("exception " + ex.Message);
                throw ex;
            }

            return UpdatedSongs;
        }

        private bool isAudioFile(CloudFileData di)
        {
            return MusicUtils.isAudioFormat(di.name);
        }
    }
}
