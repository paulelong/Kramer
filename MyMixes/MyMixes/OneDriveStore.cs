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
using PCLStorage;

namespace MyMixes
{
    class OneDriveStore : ICloudStoreOrg
    {
        private int DefaultChunkSize = 5 * 1024 * 1024;//5MB
        private int BufferSize = 4096;

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
                string filename = "/" + RootPath + "/" + ProjectPath + "/" + cloudfile;
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

        public async Task<bool> DeleteSong(string path)
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

        public async Task<List<string>> GetProjectFoldersAsync(string path)
        {
            try
            {
                var items = await graphClient.Me.Drive.Root.ItemWithPath(path).Children.Request().GetAsync();
                List<string> ret = new List<string>();

                foreach (var di in items)
                {
                    if(di.Folder != null && await isAudioFolderAsync(path + "/" + di.Name))
                    {
                        ret.Add(path + "/" + di.Name);
                    }
                }

                // TOdo: Remove files that are no longer on the remote storage.
                List<string> projects = PersistentData.GetProjectFoldersData("OneDrive", path);
                if(projects != null)
                {
                    foreach (string p in projects)
                    {
                        if (ret.IndexOf(p) < 0)
                        {
                            // Remove the local version of the project
                            await RemoveLocalProjectAsync(p);
                        }
                    }
                }

                PersistentData.PutProjectFoldersData("OneDrive", path, string.Join(",", ret));

                // string.Join(",", ret.ToArray())

                return ret;
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    throw ex;
                }

                return null;
            }
            catch(Exception ex)
            {
                return null;
                throw ex;
            }
        }

        private async Task RemoveLocalProjectAsync(string p)
        {
            try
            {
                IFolder rootfolder = FileSystem.Current.LocalStorage;

                string name = Path.GetFileName(p);

                IFolder folder = await rootfolder.CreateFolderAsync(name, CreationCollisionOption.OpenIfExists);

                await folder.DeleteAsync();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public async Task<bool> UpdateProjectAsync(string path)
        {
            try
            {
                string name = Path.GetFileName(path);

                Debug.WriteLine("Path is {0} name is {1}", path, name);

                bool result = true;
                IFolder rootfolder = FileSystem.Current.LocalStorage;

                IFolder folder = await rootfolder.CreateFolderAsync(name, CreationCollisionOption.OpenIfExists);

                var items = await graphClient.Me.Drive.Root.ItemWithPath(path).Children.Request().GetAsync();
                foreach (var di in items)
                {
                    if (isAudioFile(di.Name))
                    {
                        if (!await DownloadFileAsync(di, name, folder))
                        {
                            result = false;                        }
                    }
                }

                return result;
            }
            catch (ServiceException ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    throw ex;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<bool> isAudioFolderAsync(string path)
        {
            var items = await graphClient.Me.Drive.Root.ItemWithPath(path).Children.Request().GetAsync();
            foreach (var di in items)
            {
                if (isAudioFile(di.Name))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> SyncFolderAsync(string name, string path)
        {
            bool result = true;
            IFolder rootfolder = FileSystem.Current.LocalStorage;

            IFolder folder = await rootfolder.CreateFolderAsync(name, CreationCollisionOption.OpenIfExists);

            var items = await graphClient.Me.Drive.Root.ItemWithPath(path +"/" + name).Children.Request().GetAsync();
            foreach (var di in items)
            {
                if (isAudioFile(di.Name))
                {
                    if(!await DownloadFileAsync(di, name, folder))
                    {
                        result = false;
                    }
                }
            }

            return result;
        }

        public async Task<bool> UpdateFileAsync(string fullpath)
        {
            bool result = true;
            IFolder rootfolder = FileSystem.Current.LocalStorage;

            string name = Path.GetFileName(fullpath);
            string path = Path.GetFullPath(fullpath);


            IFolder folder = await rootfolder.CreateFolderAsync(name, CreationCollisionOption.OpenIfExists);

            var di = await graphClient.Me.Drive.Root.ItemWithPath(path + "/" + name).Request().GetAsync();

            if (!await DownloadFileAsync(di, name, folder))
            {
                result = false;
            }

            return result;
        }

        private async Task<bool> DownloadFileAsync(DriveItem di, string name, IFolder folder)
        {
            //const long DefaultChunkSize = 50 * 1024; // 50 KB, TODO: change chunk size to make it realistic for a large file.
            //long ChunkSize = DefaultChunkSize;
            //long offset = 0;         // cursor location for updating the Range header.
            //byte[] bytesInStream;                    // bytes in range returned by chunk download.

            //DateTimeOffset dto = (DateTimeOffset)di.LastModifiedDateTime;

            //// We'll use the file metadata to determine size and the name of the downloaded file
            //// and to get the download URL.
            //var driveItemInfo = await graphClient.Me.Drive.Items[di.Id].Request().GetAsync();

            //// Get the download URL. This URL is preauthenticated and has a short TTL.
            //object downloadUrl;
            //driveItemInfo.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out downloadUrl);

            //// Get the number of bytes to download. calculate the number of chunks and determine
            //// the last chunk size.
            //long size = (long)driveItemInfo.Size;
            //int numberOfChunks = Convert.ToInt32(size / DefaultChunkSize);
            //// We are incrementing the offset cursor after writing the response stream to a file after each chunk. 
            //// Subtracting one since the size is 1 based, and the range is 0 base. There should be a better way to do
            //// this but I haven't spent the time on that.
            //int lastChunkSize = Convert.ToInt32(size % DefaultChunkSize) - numberOfChunks - 1;
            //if (lastChunkSize > 0) { numberOfChunks++; }

            //// Need a away to only copy if newer, probalby need platform specific code since PCLStorage doesn't support it.
            ////if(await folder.CheckExistsAsync(di.Name) == ExistenceCheckResult.FileExists)
            ////{
            ////    var fileinfo = await folder.CreateFileAsync(di.Name, CreationCollisionOption.OpenIfExists);
            ////    folder.GetFileAsync()
            ////    fileinfo.
            ////}

            //if (await PersistentData.isRemoteNewer(folder.Path + "/" + di.Name, dto.DateTime))
            //{
            //    Debug.WriteLine("  --Downloading {0}", di.Name);

            //    IFile f = await folder.CreateFileAsync(driveItemInfo.Name, CreationCollisionOption.ReplaceExisting);
            //    using (Stream fileStream = await f.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
            //    {
            //        for (int i = 0; i < numberOfChunks; i++)
            //        {
            //            // Setup the last chunk to request. This will be called at the end of this loop.
            //            if (i == numberOfChunks - 1)
            //            {
            //                ChunkSize = lastChunkSize;
            //            }

            //            // Create the request message with the download URL and Range header.
            //            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, (string)downloadUrl);
            //            req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, ChunkSize + offset);

            //            // We can use the the client library to send this although it does add an authentication cost.
            //            // HttpResponseMessage response = await graphClient.HttpProvider.SendAsync(req);
            //            // Since the download URL is preauthenticated, and we aren't deserializing objects, 
            //            // we'd be better to make the request with HttpClient.
            //            var client = new HttpClient();
            //            HttpResponseMessage response = await client.SendAsync(req);

            //            using (Stream responseStream = await response.Content.ReadAsStreamAsync())
            //            {
            //                bytesInStream = new byte[ChunkSize];
            //                int read;
            //                do
            //                {
            //                    read = responseStream.Read(bytesInStream, 0, (int)bytesInStream.Length);
            //                    Debug.WriteLine("req={0} read={1}", bytesInStream.Length, read);
            //                    if (read > 0)
            //                        fileStream.Write(bytesInStream, 0, bytesInStream.Length);
            //                }
            //                while (read > 0);
            //            }
            //            offset += ChunkSize + 1; // Move the offset cursor to the next chunk.
            //        }
            //    }
            //}

            DateTimeOffset dto = (DateTimeOffset)di.LastModifiedDateTime;
            if (await PersistentData.isRemoteNewer(folder.Path + "/" + di.Name, dto.DateTime))
            {
                Debug.WriteLine("  --Downloading {0}", di.Name);

                int chunkSize = DefaultChunkSize;
                long offset = 0; // cursor location for updating the Range header.                
                byte[] buffer = new byte[BufferSize];
                var driveItemInfo = await graphClient.Me.Drive.Items[di.Id].Request().GetAsync();

                object downloadUrl;
                driveItemInfo.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out downloadUrl);
                long size = (long)driveItemInfo.Size;

                int numberOfChunks = Convert.ToInt32(size / DefaultChunkSize);

                // We are incrementing the offset cursor after writing the response stream to a file after each chunk.                 
                int lastChunkSize = Convert.ToInt32(size % DefaultChunkSize);
                if (lastChunkSize > 0)
                {
                    numberOfChunks++;
                }

                IFile f = await folder.CreateFileAsync(driveItemInfo.Name, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await f.OpenAsync(PCLStorage.FileAccess.ReadAndWrite))
                {

                    for (int i = 0; i < numberOfChunks; i++)
                    {
                        // Setup the last chunk to request. This will be called at the end of this loop.              
                        if (i == numberOfChunks - 1)
                        {
                            chunkSize = lastChunkSize;
                        }
                        //Create the request message with the download URL and Range header.
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, (string)downloadUrl);
                        //-1 because range is zero based
                        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, chunkSize + offset - 1);
                        // We can use the the client library to send this although it does add an authentication cost.
                        // HttpResponseMessage response = await graphClient.HttpProvider.SendAsync(req);
                        // Since the download URL is preauthenticated, and we aren't deserializing objects, 
                        // we'd be better to make the request with HttpClient.
                        var client = new HttpClient();
                        HttpResponseMessage response = await client.SendAsync(request);
                        int totalRead = 0;
                        using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                        {
                            int read;
                            while ((read = await responseStream.ReadAsync(buffer: buffer, offset: 0, count: buffer.Length)) > 0)
                            {
                                fileStream.Write(buffer, 0, read);
                                totalRead += read;
                            }
                        }
                        offset += totalRead; // Move the offset cursor to the next chunk.
                    }
                }
            }

            return true;
        }

        private bool isAudioFile(string name)
        {
            switch(Path.GetExtension(name))
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
