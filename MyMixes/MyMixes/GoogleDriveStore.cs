using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using System.Diagnostics;

using RestSharp;

using Xamarin.Forms;
using Xamarin.Auth;
using System.Net;

using OAuthNativeFlow;
using Newtonsoft.Json;

using Microsoft.AppCenter.Analytics;


namespace MyMixes
{
    public class AuthToken
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string id_token { get; set; }
        public string refresh_token { get; set; }
    }


    public class ParentData
    {
        //public string kind { get; set; }
        public string kind { get; set; }
        public string id { get; set; }
        public bool isRoot { get; set; }
    }

    public class FileMetaData
    {
        //public string kind { get; set; }
        public string name { get; set; }
        public string title { get; set; }
        public string mimeType { get; set; }
        public List<ParentData> parents { get; set; }
        public string id { get; set; }
    }

    public class FileUploadData
    {
        public string name { get; set; }
        public List<string> parents;
        public string mimeType { get; set; }
    }


    public class ListData
    {
        public string kind { get; set; }
        public List<FileMetaData> items { get; set; }
    }



    class GoogleDriveStore : ContentPage, ICloudStore
    {
        private AuthToken authToken = new AuthToken();

        protected Xamarin.Auth.WebAuthenticator authenticator = null;

        string Scopes = "https://www.googleapis.com/auth/drive" +
            " https://www.googleapis.com/auth/drive.appdata" +
            " https://www.googleapis.com/auth/drive.file" +
            " https://www.googleapis.com/auth/drive.metadata";

        private bool webAuthcodeComplete;
        private bool webAuthSuccess;
        Account account;
        AccountStore store;

        string clientId = null;
        string redirectUri = null;
        string clientSecret = null;

        public static string AppName = "RiffRecorder";
        public static string iOSClientId = "373101837454-4987s059fdup3ssv3fcajsc85goalt85.apps.googleusercontent.com";
        public static string AndroidClientId = "373101837454-sejuskobtc5psl0dks2coljjvq4fi353.apps.googleusercontent.com";
        public static string UWPClientId = "373101837454-gj420hsuo339rkdd1ecd56q93940081n.apps.googleusercontent.com";

        // These values do not need changing
        public static string AuthorizeUrl = "https://accounts.google.com/o/oauth2/auth";
        public static string AccessTokenUrl = "https://www.googleapis.com/oauth2/v4/token";
        public static string UserInfoUrl = "https://www.googleapis.com/oauth2/v2/userinfo";

        // Set these to reversed iOS/Android client ids, with :/oauth2redirect appended
        public static string iOSRedirectUrl = "com.googleusercontent.apps.373101837454-4987s059fdup3ssv3fcajsc85goalt85:/oauth2redirect";
        public static string AndroidRedirectUrl = "com.googleusercontent.apps.373101837454-sejuskobtc5psl0dks2coljjvq4fi353:/oauth2redirect";
        public static string UWPRedirectUrl = "com.paulyshotel.rr:/oauth2redirect";

        private const string UWPclientSecret = "rXlLa_vdA0p8HSPrmVUzI6uO";

        public async Task<bool> Authenticate(UIParent parent)
        {
            store = AccountStore.Create();
            if (store == null)
            {
                Analytics.TrackEvent("Account store NULL");
                return false;
            }

            Analytics.TrackEvent("Google Auth");

            switch (Device.RuntimePlatform)
            {
                case Device.iOS:
                    clientId = iOSClientId;
                    redirectUri = iOSRedirectUrl;
                    clientSecret = null;
                    break;

                case Device.Android:
                    clientId = AndroidClientId;
                    redirectUri = AndroidRedirectUrl;
                    clientSecret = null;
                    break;

                case Device.UWP:
                    clientId = UWPClientId;
                    clientId = AndroidClientId;
                    redirectUri = UWPRedirectUrl;
                    clientSecret = UWPclientSecret;
                    clientSecret = null;
                    break;
            }

            account = store.FindAccountsForService(AppName).FirstOrDefault();
            if (account == null)
            {
                Analytics.TrackEvent("Account information not found and is NULL");
                //                account = new Account();
            }
            else
            {
                if (account.Properties.ContainsKey("access_token"))
                {
                    Analytics.TrackEvent("Google persitent google creds");
                    webAuthcodeComplete = true;
                    webAuthSuccess = true;
                    return true;
                }
            }

            var authenticator = new Xamarin.Auth.OAuth2Authenticator(
                clientId,
                clientSecret,
                Scopes,
                new Uri(AuthorizeUrl),
                new Uri(redirectUri),
                new Uri(AccessTokenUrl),
                null,
                true);

            authenticator.Completed += OnAuthCompleted;
            authenticator.Error += OnAuthError;

            webAuthcodeComplete = false;

            AuthenticationState.Authenticator = authenticator;

            var presenter = new Xamarin.Auth.Presenters.OAuthLoginPresenter();
            if (presenter != null)
            {
                presenter.Login(authenticator);
            }

            return true;
        }

        private async Task WaitForAuthenticationComplete()
        {
            while (!webAuthcodeComplete)
            {
                await Task.Delay(500);
            }
        }

        private async Task<string> GetItemID(string path)
        {
            string[] paths = path.Split('/');
            string parentId = await CreateDirectory(paths[1], "root");

            for (int i = 2; i < paths.Length; i++)
            {
                if (parentId != null)
                {
                    parentId = await GetPathId(paths[i], parentId);
                }
                else
                {
                    return null;
                }
            }

            return parentId;
        }

        public async Task<bool> ProjectExists(string path)
        {
            await WaitForAuthenticationComplete();

            if (!webAuthSuccess)
            {
                return false;
            }

            if (!(await IsTokenFresh()))
            {
                return false;
            }

            if(await GetItemID(path) == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        async void OnAuthCompleted(object sender, AuthenticatorCompletedEventArgs e)
        {
            var authenticator = sender as Xamarin.Auth.OAuth2Authenticator;
            if (authenticator != null)
            {
                authenticator.Completed -= OnAuthCompleted;
                authenticator.Error -= OnAuthError;
            }

            //User user = null;
            if (e.IsAuthenticated)
            {
                // If the user is authenticated, request their basic user data from Google
                if (account != null)
                {
                    store.Delete(account, AppName);
                }

                await store.SaveAsync(account = e.Account, AppName);
                Debug.WriteLine("Authentication success: " + e.Account.Username);
                Analytics.TrackEvent("Google Authentication success");
                webAuthcodeComplete = true;
                webAuthSuccess = true;
                //await DisplayAlert("Email address", user.Email, "OK");
            }
            else
            {
                webAuthcodeComplete = true;
                webAuthSuccess = false;
            }
        }

        void OnAuthError(object sender, AuthenticatorErrorEventArgs e)
        {
            var authenticator = sender as Xamarin.Auth.OAuth2Authenticator;
            if (authenticator != null)
            {
                authenticator.Completed -= OnAuthCompleted;
                authenticator.Error -= OnAuthError;
            }

            Debug.WriteLine("Authentication error: " + e.Message);
            Analytics.TrackEvent("Google Authentication error: " + e.Message);
            webAuthcodeComplete = true;
            webAuthSuccess = false;
        }

        public async Task<bool> RefreshToken()
        {
            string uriString = "https://www.googleapis.com/oauth2/v4/token?";
            uriString += "grant_type=refresh_token";
            uriString += "&refresh_token=" + account.Properties["refresh_token"];
            uriString += "&client_id=" + clientId;
            var request = new GooglePutFileRequest("POST", new Uri(uriString), null, account);

            var response = await request.GetResponseAsync();

            if(response.StatusCode == HttpStatusCode.OK)
            {
                string jsonInfo = await response.GetResponseTextAsync();
                AuthToken at = JsonConvert.DeserializeObject<AuthToken>(jsonInfo);

                account.Properties["access_token"] = at.access_token;
                await store.SaveAsync(account, AppName);
            }

            return true;
        }

        public IRestResponse SendFileResumable(RestClient client, string cloudfile)
        {
            RestRequest request = new RestRequest("https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Content-Type", "application/json; charset=UTF-8");

            FileMetaData mfd = new FileMetaData { name = cloudfile };

            request.AddBody(mfd);

            IRestResponse response = client.Execute(request);

            return response;
        }

        async Task<bool> IsTokenFresh()
        {
            // Check that token is fresh
            bool unauthorized = await CheckToken();
            if (!unauthorized)
            {
                Debug.Print("Invalid Credentials, refreshing\n");
                bool result = await RefreshToken();
                if (!result)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> SaveRiffToCloud(Stream localfile, string RootPath, string ProjectPath, string cloudfile)
        {
            Analytics.TrackEvent("Google save");

            await WaitForAuthenticationComplete();

            if (!webAuthSuccess)
            {
                return false;
            }

            if(!(await IsTokenFresh()))
            {
                return false;
            }

            // Get/Create RiffSess top level directory
            string parentId = await CreateDirectory(RootPath, "root");
            string folderId;
            if(parentId != null)
            {
                folderId = await CreateDirectory(ProjectPath, parentId);
                if(folderId == null)
                {
                    Analytics.TrackEvent("Google couldn't open/create root folder");
                    return false;
                }
            }
            else
            {
                Analytics.TrackEvent("Google couldn't open/create project folder");
                return false;
            }

            var request = new GooglePutFileRequest("POST", new Uri("https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable"), null, account);

            FileUploadData fud = new FileUploadData() { name = cloudfile };
            fud.parents = new List<string>();
            fud.parents.Add(folderId);

            string jsonData = JsonConvert.SerializeObject(fud);
            request.SetRequestBody(jsonData, "application/json; charset=utf-8");

            var response = await request.GetResponseAsync();
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Debug.Print("Invalid Credentials\n");
                bool result = await RefreshToken();

                if (result == true)
                {
                    request = new GooglePutFileRequest("POST", new Uri("https://www.googleapis.com/upload/drive/v3/files?uploadType=resumable"), null, account);
                    request.SetRequestBody(jsonData, "application/json; charset=utf-8");
                }
                else
                {
                    Analytics.TrackEvent("Google refresh token failed saved failed.");
                    return false;
                }
            }

            if (response != null)
            {
                var list = response.Headers.ToList();
                KeyValuePair<string, string> p = list.Find(x => x.Key.ToLower() == "location");
                if (p.Value != null)
                {
                    Debug.Print("File Location URL: {0}\n", p.Value);

                    var writeRequest = new GooglePutFileRequest("PUT", new Uri(p.Value), null, account);

                    byte[] result;
                    using (var streamReader = new MemoryStream())
                    {
                        localfile.CopyTo(streamReader);

                        result = streamReader.ToArray();
                    }

                    writeRequest.SetRequestBody(result);

                    var writeResponse = await writeRequest.GetResponseAsync();
                    if (writeResponse == null)
                    {
                        return false;
                    }
                    else
                    {
                        Debug.WriteLine(writeResponse.ResponseUri);
                        Debug.WriteLine(writeResponse.GetResponseText());
                    }
                }
                else
                {
                    Debug.WriteLine("Locaiton blank: " + response.GetResponseText());
                }
            }
            else
            {
                Debug.WriteLine("Response NULL: " + response.GetResponseText());
            }

            return true;
        }

        private async Task<bool> CheckToken()
        {
            if (account.Properties.ContainsKey("access_token"))
            {
                string act = account.Properties["access_token"];
                var request = new GooglePutFileRequest("GET", new Uri("https://www.googleapis.com/oauth2/v3/tokeninfo=access_token=" + act), null, account);

                var response = await request.GetResponseAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private async Task<string> GetPathId(string name, string id)
        {
            string uriString = "https://www.googleapis.com/drive/v2/files?";
            uriString += "q=title%3D%22" + name + "%22%20and%20trashed%3Dfalse";

            GooglePutFileRequest request = new GooglePutFileRequest("GET", new Uri(uriString), null, account);

            var response = await request.GetResponseAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Analytics.TrackEvent("Google GetPathId error " + response.StatusCode);
                return null;
            }
            else
            {
                string respData = response.GetResponseText();
                try
                {
                    ListData ld = JsonConvert.DeserializeObject<ListData>(respData);
                    if(id == "root")
                    {
                        return ld.items[0].id;
                    }
                    else
                    {
                        var newid = ld.items.Find(p => (p.parents.Find(f => (id == f.id)) != null));
                        return newid.id;
                    }
                }
                catch(Exception ex)
                {
                    Debug.Print(respData);
                    return null;
                }
            }
        }

        private async Task<string> CreateDirectory(string folderName, string parentId)
        {
            string SessRoot = await GetPathId(folderName, parentId);

            if(SessRoot == null)
            {
                GooglePutFileRequest request = new GooglePutFileRequest("POST", new Uri("https://www.googleapis.com/drive/v3/files"), null, account);

                FileUploadData fud = new FileUploadData { name = folderName, mimeType = "application/vnd.google-apps.folder" };
                fud.parents = new List<string>();
                fud.parents.Add(parentId);

                string jsonData = JsonConvert.SerializeObject(fud);
                request.SetRequestBody(jsonData, "application/json; charset=utf-8");

                var response = await request.GetResponseAsync();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Analytics.TrackEvent("Google CreateDir error " + response.StatusCode);
                    return null;
                }

                FileMetaData rmfd = JsonConvert.DeserializeObject<FileMetaData>(response.GetResponseText());

                return rmfd.id;
            }
            else
            {
                return SessRoot;
            }
        }

        private string ExtractCodeFromUrl(string url)
        {
            if (url.Contains("code="))
            {
                var attributes = url.Split('&');

                var code = attributes.FirstOrDefault(s => s.Contains("code=")).Split('=')[1];

                return code;
            }

            return string.Empty;
        }

        public void RevokeToken()
        {
            RestClient client = new RestClient("https://accounts.google.com/o/oauth2/revoke");
            client.FollowRedirects = false;

            var revokeRequest = new RestRequest(Method.GET);
            revokeRequest.AddParameter("token", PersistentData.GoogleToken);

            IRestResponse response = client.Execute(revokeRequest);
            Debug.Print("After Token request status={0}, {2} content={1}\n", response.ResponseStatus.ToString(), response.Content, response.StatusDescription);

        }

        public void Reset()
        {
            RevokeToken();
            PersistentData.GoogleToken = "";
        }

        public async Task<bool> DeleteTake(string path)
        {
            await WaitForAuthenticationComplete();

            if (!webAuthSuccess)
            {
                return false;
            }

            if (!(await IsTokenFresh()))
            {
                return false;
            }

            string fid = await GetItemID(path);
            if (fid == null)
            {
                return false;
            }
            else
            {
                string act = account.Properties["access_token"];
                var request = new GooglePutFileRequest("DELETE", new Uri("https://www.googleapis.com/drive/v3/files/" + fid), null, account);

                var response = await request.GetResponseAsync();

                if (response.StatusCode >= HttpStatusCode.Ambiguous)
                {
                    return false;
                }

                return true;
            }
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
