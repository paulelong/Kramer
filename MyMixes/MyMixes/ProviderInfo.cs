using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using CloudStorage;
using OAuthNativeFlow;

namespace MyMixes
{
    public class ProviderInfo
    {
        private static Dictionary<CloudProviders, ICloudStore> providers = new Dictionary<CloudProviders, ICloudStore>();

        public CloudProviders CloudProvider
        {
            get; set;
        }

        public string RootPath
        {
            get; set;
        }

        private ICloudStore CloudStore
        {
            get
            {
                return providers[CloudProvider];
            }
        }

        public async static Task<ICloudStore> GetCloudProvider(CloudProviders cp)
        {
            if (!providers.ContainsKey(cp))
            {
                ICloudStore cs;
                switch (cp)
                {
                    case CloudProviders.OneDrive:
                        cs = CloudStoreFactory.CreateCloudStore(CloudStorage.CloudProviders.OneDrive);
                        Dictionary<string, object> onedriveparams = new Dictionary<string, object>();
                        onedriveparams[CloudParams.ClientID.ToString()] = "7ba22c7f-29be-4dc7-a274-4209fe0b8b72";
                        onedriveparams[CloudParams.UIParent.ToString()] = null;

                        if (cs.Initialize(onedriveparams))
                        {
                            bool worked = await cs.AuthenticateAsync();

                            if (!worked)
                            {
                                return null;
                            }

                            return cs;
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    case CloudProviders.GoogleDrive:
                        cs = CloudStoreFactory.CreateCloudStore(CloudStorage.CloudProviders.GoogleDrive);

                        Dictionary<string, object> googledriveparams = new Dictionary<string, object>();
                        googledriveparams[CloudParams.ClientID.ToString()] = "133589155347-gj93njepme6jp96nh1erjmdi4q4c7d9k.apps.googleusercontent.com";
                        googledriveparams[CloudParams.RedirectURL.ToString()] = "com.paulyshotel.mymixes:/oauth2redirect";
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
                                return cs;
                            }
                            else
                            {
                                return null;
                            }
                        }
                        else
                        {
                            return null;
                        }
                        break;
                    default:
                        return null;
                        break;
                }
            }
            else
            {
                return providers[cp];
            }
        }

        public async Task<ICloudStore> GetCloudProviderAsync()
        {
            return await GetCloudProvider(CloudProvider);
        }
        
        public async Task< List<string> > GetFoldersAsync()
        {
            try
            {
                ICloudStore cs = await GetCloudProviderAsync();

                var l = await cs.GetFolderList("/" + RootPath);
                List<string> retl = new List<string>();
                foreach (var i in l)
                {
                    retl.Add(i.name);
                }

                return retl;
            }
            catch(Exception ex)
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

        public async Task<bool> CheckAuthenitcation()
        {
            if (!IsAuthenticated)
            {
                ICloudStore cs = await GetCloudProvider(CloudProvider);
                if(cs != null)
                {
                    isAuthenticated = await cs.AuthenticateAsync();
                }
            }

            return IsAuthenticated;
        }

        public async Task<bool> UpdateProjectAsync(string f)
        {
            ICloudStore cs = await GetCloudProviderAsync();
            bool result = true;

            try
            {
                string projectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), f);
                string remoteFolderName = "/" + RootPath + "/" + f;

                var items = await cs.GetFolderList(remoteFolderName);
                foreach (var di in items)
                {
                    if (isAudioFile(di))
                    {
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
                                    if (!await cs.DownloadFileAsync(remoteFolderName + "/" + di.name, s))
                                    {
                                        Debug.Print("FAILED " + localFileName + "\n");

                                        result = false;
                                    }

                                    s.Close();
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

            return result;
        }

        private bool isAudioFile(CloudFileData di)
        {
            switch (Path.GetExtension(di.name))
            {
                case ".wav":
                case ".mp3":
                case ".wma":
                    return true;
                default:
                    return false;
            }
        }
    }
}
