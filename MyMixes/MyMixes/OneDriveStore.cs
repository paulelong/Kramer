using Microsoft.Identity.Client;
using Microsoft.Graph;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.IO;
using System.Diagnostics;

using Xamarin.Forms;

using Microsoft.AppCenter.Analytics;


namespace MyMixes
{
    class OneDriveStore : ICloudStore
    {
        static OneDriveStore _instance;

        public static OneDriveStore Instance
        {
            get { return _instance ?? (_instance = new OneDriveStore());  }
        }

        #region Constants
        const string ClientID = "7ba22c7f-29be-4dc7-a274-4209fe0b8b72";

        #endregion Constants

        #region fields

        PublicClientApplication PCA = null;
        string Username = string.Empty;
        string[] Scopes = { "Files.ReadWrite", "User.ReadWrite" };

        bool signedIn = false;
        AuthenticationResult ar = null;

        private GraphServiceClient graphClient = null;

        public string TokenForUser = null;

        public DateTimeOffset expiration;

        #endregion fields

        public OneDriveStore()
        {
            PCA = new PublicClientApplication(ClientID);
            PCA.RedirectUri = "msal" + ClientID + "://auth";
        }

        public async Task<bool> Authenticate(UIParent parent)
        {
            Analytics.TrackEvent("Onedrive Auth");

            if (signedIn == false)
            {
                //// let's see if we have a user in our belly already
                try
                {
                    IUser user = PCA.Users.FirstOrDefault();
                    if(user != null)
                    {
                        ar = await PCA.AcquireTokenSilentAsync(Scopes, user);

                        signedIn = true;

                    }
                }
                catch (MsalUiRequiredException ex)
                {
                    signedIn = false;
                    return signedIn;
                }
            }

            if (signedIn == false)
            {
                try
                {
                    ar = await PCA.AcquireTokenAsync(Scopes, parent);
                    //RefreshUserData(ar.AccessToken);
                    signedIn = true;
                }
                catch (MsalClientException ee2)
                {
                    signedIn = false;
                    return signedIn;
                }
            }

            graphClient = new GraphServiceClient(
                "https://graph.microsoft.com/v1.0",
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        //var token = await GetTokenForUserAsync(parent);
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", ar.AccessToken);
                                // This header has been added to identify our sample in the Microsoft Graph service.  If extracting this code for your project please remove.
                                //requestMessage.Headers.Add("SampleID", "xamarin-csharp-connect-sample");
                            }));

            return signedIn;
        }

        public async Task<bool> ProjectExists(string path)
        {
            try
            {
                var items = await graphClient.Me.Drive.Root.ItemWithPath(path).Children.Request().GetAsync();
            }
            catch(ServiceException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    throw ex;
                }

                return false;
            }

            return true;
        }

        public async Task<bool> SaveRiffToCloud(Stream localfile, string RootPath, string ProjectPath, string cloudfile)
        {
            Analytics.TrackEvent("Onedrive save");

            if (signedIn == true)
            {
                string filename = RootPath + "/" + ProjectPath + "/" + cloudfile;
                Debug.Print("dest Filename: {0}, pos: {1}, len: {2}\n", filename, localfile.Position, localfile.Length);

                UploadSession us = await graphClient.Me.Drive.Root.ItemWithPath(filename).CreateUploadSession().Request().PostAsync() ;
                Debug.Print("upload URL: {0}\n", us.UploadUrl);

                var maxSizeChunk = 320 * 4 * 1024;
                var provider = new ChunkedUploadProvider(us, graphClient, localfile, maxSizeChunk);
                var chunckRequests = provider.GetUploadChunkRequests();
                var exceptions = new List<Exception>();
                var readBuffer = new byte[maxSizeChunk];
                DriveItem itemResult = null;
                //upload the chunks
                foreach (var request in chunckRequests)
                {
                    // Do your updates here: update progress bar, etc.
                    // ...
                    // Send chunk request
                    var result = await provider.GetChunkRequestResponseAsync(request, readBuffer, exceptions);

                    if (result.UploadSucceeded)
                    {
                        itemResult = result.ItemResponse;
                    }
                }

                // Check that upload succeeded
                if (itemResult == null)
                {
                    //await UploadFilesToOneDrive(fileName, filePath, graphClient);
                    return false;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public async Task<string> GetTokenForUserAsync(UIParent parent)
        {
            if (TokenForUser == null || expiration <= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                AuthenticationResult authResult = await PCA.AcquireTokenAsync(Scopes, parent);

                TokenForUser = authResult.AccessToken;
                expiration = authResult.ExpiresOn;
            }

            return TokenForUser;
        }

        public void Reset()
        {
            IUser user = PCA.Users.FirstOrDefault();
            PCA.Remove(user);
        }

        public async Task<bool> DeleteTake(string path)
        {
            try
            {
                await graphClient.Me.Drive.Root.ItemWithPath(path).Request().DeleteAsync();
            }
            catch(ServiceException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    throw ex;
                }

                return false;
            }
            return true;
        }

        public Task<List<string>> GetProjectFoldersAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateProjectAsync(string path)
        {
            throw new NotImplementedException();
        }
    }
}
