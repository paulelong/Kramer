using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyMixes
{
    public class ProviderInfo
    {
        private static Dictionary<CloudProviders, ICloudStore> providers;

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

        public ICloudStore GetCloudProvider()
        {
            if(!providers.ContainsKey(CloudProvider))
            {
                ICloudStore cs;
                switch (CloudProvider)
                {
                    case CloudProviders.OneDrive:
                        cs = new OneDriveStore();
                        break;
                    case CloudProviders.GoogleDrive:
                        cs = new GoogleDriveStore();
                        break;
                    default:
                        cs = null;
                        break;
                }

                providers[CloudProvider] = cs;
                return cs;
            }
            else
            {
                return providers[CloudProvider];
            }
        }
        
        public async Task< List<string> > GetFoldersAsync()
        {
            try
            {
                ICloudStore cs = GetCloudProvider();

                return await cs.GetProjectFoldersAsync();
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        internal async Task<bool> UpdateProjectAsync(string f)
        {
            ICloudStore cs = GetCloudProvider();

            return await cs.UpdateProjectAsync(f);
        }
    }
}
