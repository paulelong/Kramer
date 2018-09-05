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
        public  static async Task<bool> Authenticate(ICloudStore cs)
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

    public interface ICloudStore
    {
        Task<bool> Authenticate(UIParent parent);

        Task<bool> SaveRiffToCloud(Stream localfile, string RootPath, string ProjectPath, string cloudfile);

        Task<bool> ProjectExists(string path);

        Task<bool> DeleteTake(string path);

        Task<List<string>> GetProjectFoldersAsync();

        Task<bool> UpdateProjectAsync(string path);

        void Reset();
    }
}
