using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.AppCenter.Analytics;
using Xamarin.Forms;

namespace MyMixes
{
    public static class CloudStoreUtils
    {
        public async static Task<bool> Authenticate(ICloudStoreOrg cs)
        {
            try
            {
                if (cs != null)
                {
                    if (await cs.Authenticate(App.UiParent))
                    {
                        return true;
                    }
                }
            }
            catch (Microsoft.Identity.Client.MsalClientException ex)
            {
                Analytics.TrackEvent("Authentication exception " + ex.Message);
            }

            return false;
        }
    }

    public interface ICloudStoreOrg
    {
        Task<bool> Authenticate(UIParent parent);

        Task<bool> ProjectExists(string path);

        Task<bool> DeleteSong(string path);

        Task<List<string>> GetProjectFoldersAsync(string path);

        Task<bool> UpdateProjectAsync(string path);

        Task<bool> UpdateFileAsync(string fullpath);

        void Reset();
    }
}
