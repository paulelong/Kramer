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

        private ICloudStore cs = null;
        private ICloudStore CloudStore
        {
            get
            {
                return cs;
            }
        }

        private static Dictionary<string, ProviderInfo> pi_list = new Dictionary<string, ProviderInfo>();
        private static List<ProviderInfo> providerList = null;
        public static List<ProviderInfo> Providers
        {
            get
            {
                if(providerList == null)
                {
                    List<string> mappings = PersistentData.LoadProjectMappings();

                    foreach (string s in mappings)
                    {
                        string[] parts = s.Split(':');
                        CloudProviders cp = (CloudProviders)Enum.Parse(typeof(CloudProviders), parts[0]);
                        providerList.Add(new ProviderInfo() { CloudProvider = cp, RootPath = parts[0] });
                    }
                }
                //List<ProviderInfo> ret = new List<ProviderInfo>();

                //foreach(KeyValuePair<string, ProviderInfo> kvp in pi_list)
                //{
                //    if(kvp.Value != null)
                //    {
                //        ret.Add(kvp.Value);
                //    }
                //}

                return providerList;
            }
        }

        //public static ProviderInfo FindProvider(string name)
        //{
        //    foreach (KeyValuePair<string, ProviderInfo> kvp in pi_list)
        //    {
        //        string[] parts = kvp.Key.Split(':');
        //        if(parts[1] == name)
        //        {
        //            return kvp.Value;
        //        }
        //    }

        //    return null;
        //}

        //public static async Task LoadMappings()
        //{
        //    List<string> mappings = PersistentData.LoadProjectMappings();

        //    foreach (string s in mappings)
        //    {
        //        string[] parts = s.Split(':');
        //        CloudProviders cp = (CloudProviders)Enum.Parse(typeof(CloudProviders), parts[0]);
        //        await GetCloudProviderAsync(cp, parts[1]);
        //    }
        //}

        public static void SaveMappings()
        {
            PersistentData.SaveProjectMappings(providerList);
        }

        public async Task<ProviderInfo> GetCloudProviderAsync()
        {
            ProviderInfo pi = await GetCloudProviderAsync(CloudProvider, RootPath);
            return pi;
        }

        public async static Task<ProviderInfo> GetCloudProviderAsync(CloudProviders cp, string rootpath = null)
        {
            string key = cp.ToString() + ":" + rootpath;

            if (!pi_list.ContainsKey(key))
            {
                ICloudStore cs = null ;

                if (!providers.ContainsKey(cp))
                {
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

                                if (!worked)
                                {
                                    return null;
                                }
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
                                if (!authCompletWorked)
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
                    cs = providers[cp];
                }

                ProviderInfo pi = new ProviderInfo() { cs = cs, RootPath = rootpath };
                pi_list[key] = pi;
                return pi;
            }
            else
            {
                return pi_list[key];
            }
        }

        public void UpdatePath(string path)
        {
            string key = CloudProvider.ToString() + ":" + RootPath;

            RootPath = path;
            pi_list[key] = null;

            key = CloudProvider.ToString() + ":" + RootPath;
            pi_list[key] = this;
        }

        public void RemoveProvider()
        {
            string key = CloudProvider.ToString() + ":" + RootPath;

            pi_list[key] = null;
        }

        //public async Task<ICloudStore> GetCloudProviderAsync()
        //{
        //    return await GetCloudProviderAsync(CloudProvider);
        //}

        public async Task< List<string> > GetProjectFoldersAsync(string path)
        {
            try
            {
                var l = await CloudStore.GetFolderList("/" + path);
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

        public async Task<List<string>> GetFoldersAsync(string folder)
        {
            try
            {
                var l = await CloudStore.GetFolderList(folder);
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

        public async Task<bool> CheckAuthenitcation()
        {
            if(CloudStore == null)
            {
                await GetCloudProviderAsync();
            }

            if (!isAuthenticated && CloudStore != null)
            {
                isAuthenticated = await CloudStore.AuthenticateAsync();
            }

            return isAuthenticated;
        }

        public async Task<List<string>> UpdateProjectAsync(string root, string project)
        {
            List<string> UpdatedSongs = new List<string>();

            try
            {
                string projectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), project);
                string remoteFolderName = "/" + root + "/" + project;

                var items = await cs.GetFolderList(remoteFolderName);
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
                                    if (!await CloudStore.DownloadFileAsync(remoteFolderName + "/" + di.name, s))
                                    {
                                        PersistentData.SetTrackNumber(projectPath, di.name, di.track);
                                        Debug.Print("FAILED " + localFileName + "\n");
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

            return UpdatedSongs;
        }

        private bool isAudioFile(CloudFileData di)
        {
            return MusicUtils.isAudioFormat(di.name);
        }

        public async Task<bool> DeleteTake(string name)
        {
            return true;
        }
    }
}
