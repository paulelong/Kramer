using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

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
                        cs = new OneDriveStore();
                        break;
                    case CloudProviders.GoogleDrive:
                        cs = new GoogleDriveStore();
                        break;
                    default:
                        return null;
                        break;
                }

                bool success = await CloudStoreUtils.Authenticate(cs);
                if (success)
                {
                    providers[cp] = cs;
                    return cs;
                }
                else
                {
                    return null;
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

                return await cs.GetProjectFoldersAsync(RootPath);
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

            if(cs != null)
            {
                return await cs.UpdateProjectAsync(f);
            }
            else
            {
                return false;
            }
        }

        internal async Task<bool> UpdateFileAsync(string f)
        {
            ICloudStore cs = await GetCloudProviderAsync();

            if (cs != null)
            {
                return await cs.UpdateFileAsync(f);
            }
            else
            {
                return false;
            }
        }

    }
}
