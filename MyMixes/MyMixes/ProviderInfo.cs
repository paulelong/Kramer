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

        public enum CloudProviders
        {
            OneDrive,
            GoogleDrive,
            Dropbox,
        }

        public CloudProviders CloudProvider
        {
            get; set;
        }

        public string RootPath
        {
            get; set;
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
                        googledriveparams[CloudParams.ClientID.ToString()] = "173566192011-5k9r7r9dnf4pvcuq1vjohopel5kmg8b6.apps.googleusercontent.com";
                        googledriveparams[CloudParams.RedirectURL.ToString()] = "com.paulyshotel.testcloud:/oauth2redirect";
                        googledriveparams[CloudParams.AppName.ToString()] = "MyMixes";
                        googledriveparams[CloudParams.UIParent.ToString()] = null;
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

                var l = await cs.GetFolderList(RootPath);
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

        internal async Task<bool> UpdateProjectAsync(string f)
        {
            ICloudStore cs = await GetCloudProviderAsync();

            try
            {
                string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), RootPath + "/" + f);

                bool result = true;

                var items = await cs.GetFolderList(RootPath + "/" + f);
                foreach (var di in items)
                {
                    if (isAudioFile(di))
                    {
                        Stream s = new FileStream(fileName + "/" + di, FileMode.OpenOrCreate);

                        if (!await cs.DownloadFileAsync(di.name, s))
                        {
                            result = false;
                        }

                        s.Close();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private bool isAudioFile(CloudFileData di)
        {
            switch (Path.GetExtension(di.name))
            {
                case ".wav":
                case ".mp3":
                    return true;
                default:
                    return false;
            }
        }
    }
}
