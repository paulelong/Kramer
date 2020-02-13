//using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

using CloudStorage;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Xamarin.Essentials;

namespace MyMixes
{
    public class ProjectMapping
    {
        public CloudProviders provider;
        public string project;
    }

    public static class PersistentData
    {
        private const string KEY_QUEUED_TRACK_COUNT = "QueuedTrackCount";
        private const string KEY_QUEUED_TRACK_ID = "QueuedTrack";

        public static void Save()
        {
            //Application.Current.SavePropertiesAsync();
        }

        static internal ObservableCollection<MixLocation> mixLocationList = null;
        static public ObservableCollection<MixLocation> MixLocationList
        {
            get
            {
                if (mixLocationList == null)
                {
                    mixLocationList = new ObservableCollection<MixLocation>();
                    LoadMixLocations();
                }

                return mixLocationList;
            }

        }

        internal static bool mixLocationsChanged = false;
        internal static void LoadMixLocations()
        {
            MixLocationList.Clear();

            foreach (CloudProviders cpn in Enum.GetValues(typeof(CloudProviders)))
            {
                string list = Preferences.Get("ProjectMap_" + cpn, GetOldValue<string>("ProjectMap_" + cpn, null));
                if (!string.IsNullOrEmpty(list)) 
                //if (Application.Current.Properties.ContainsKey("ProjectMap_" + cpn))
                {
                    //string list = (string)Application.Current.Properties["ProjectMap_" + cpn];
                    //if (!string.IsNullOrEmpty(list))
                    //{
                        foreach (string p in list.Split(','))
                        {
                            mixLocationList.Add(new MixLocation() { Provider = cpn, Path = p });
                        }
                    //}
                }
            }

            mixLocationsChanged = false;
        }

        internal static void SaveMixLocations()
        {
            Dictionary<CloudProviders, List<string>> ProjectsByProviders = new Dictionary<CloudProviders, List<string>>();

            foreach (MixLocation ml in MixLocationList)
            {
                if (!ProjectsByProviders.ContainsKey(ml.Provider))
                {
                    ProjectsByProviders[ml.Provider] = new List<string>();
                }

                ProjectsByProviders[ml.Provider].Add(ml.Path);
            }

            foreach (KeyValuePair<CloudProviders, List<string>> kvp in ProjectsByProviders)
            {
                Preferences.Set("ProjectMap_" + kvp.Key, string.Join(",", kvp.Value.ToArray()));
                //Application.Current.Properties["ProjectMap_" + kvp.Key] = string.Join(",", kvp.Value.ToArray());
            }

            mixLocationsChanged = true;
        }

        public static List<string> GetProjectFoldersData(string provider, string root)
        {
            string projects = Preferences.Get(provider + "_" + root, GetOldValue<string>(provider + "_" + root, null));
            if(!string.IsNullOrEmpty(projects))
            {
                return new List<string>(projects.Split(','));
            }

            //string key = provider + "_" + root;
            

            //if (Application.Current.Properties.ContainsKey(key))
            //{
            //    string projects = (string)Application.Current.Properties[key];
            //    return new List<string>(projects.Split(','));
            //}
            else
            {
                return null;
            }
        }

        public static void PutProjectFoldersData(string provider, string root, string value)
        {
            Preferences.Set(provider + "_" + root, value);
            //string key = provider + "_" + root;

            //Application.Current.Properties[key] = value;
        }

        //public static async Task<bool> isRemoteNewerAsync(string path, DateTime lastModified)
        //{
        //    string filepath = Path.GetDirectoryName(path);
        //    string name = Path.GetFileName(path);

        //    //IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(filepath);
        //    //ExistenceCheckResult result = await folder.CheckExistsAsync(name);

        //    if(!File.Exists(path))
        //    {
        //        return true;
        //    }

        //    //if(result != ExistenceCheckResult.FileExists)
        //    //{
        //    //    return true;
        //    //}

        //    if (Application.Current.Properties.ContainsKey(path))
        //    {
        //        DateTime localLastModified = (DateTime)Application.Current.Properties[path];
        //        if (lastModified <= localLastModified)
        //        {
        //            return false;
        //        }
        //    }

        //    Application.Current.Properties[path] = lastModified;
        //    return true;
        //}

        static int ProviderCount
        {

            get => Preferences.Get(nameof(ProviderCount), GetOldValue<int>("CloudProviderCount"));
            set
            {
                Preferences.Set(nameof(ProviderCount), value);
            }

            //get
            //{
            //    return (int)Application.Current.Properties["CloudProviderCount"];
            //}
            //set
            //{
            //    Application.Current.Properties["CloudProviderCount"] = value;
            //}
        }

        static string googleToken;
        public static string GoogleToken
        {

            get => Preferences.Get(nameof(GoogleToken), GetOldValue<string>(nameof(GoogleToken), ""));
            set
            {
                Preferences.Set(nameof(GoogleToken), value);
            }

            //get
            //{
            //    if (googleToken == null)
            //    {
            //        googleToken = LoadPersitedValue("GoogleToken");
            //        if (googleToken == null)
            //        {
            //            return "";
            //        }
            //    }

            //    return googleToken;
            //}
            //set
            //{
            //    googleToken = value;
            //    Application.Current.Properties["GoogleToken"] = googleToken;
            //}
        }

        public static int LastPlayedSongIndex
        {


            get => Preferences.Get(nameof(LastPlayedSongIndex), GetOldValue<int>("CurrentSongIndex", 0));
            set
            {
                Preferences.Set(nameof(LastPlayedSongIndex), value);
            }

            //get
            //{
            //    if (Application.Current.Properties.ContainsKey("CurrentSongIndex"))
            //    {
            //        int s = (int)Application.Current.Properties["CurrentSongIndex"];
            //        return s;
            //    }

            //    return 0;
            //}
            //set
            //{
            //    Application.Current.Properties["CurrentSongIndex"] = value;
            //}
        }

        public static bool LastAlign
        {

            get => Preferences.Get(nameof(LastAlign), GetOldValue<bool>("lastAlign", false));
            set
            {
                Preferences.Set(nameof(LastAlign), value);
            }

            //get 
            //{
            //    if (Application.Current.Properties.ContainsKey("lastAlign"))
            //    {
            //        bool s = (bool)Application.Current.Properties["lastAlign"];
            //        return s;
            //    }

            //    return false;
            //}
            //set
            //{
            //    Application.Current.Properties["lastAlign"] = value;
            //}
        }

        public static bool LastLoop
        {

            get => Preferences.Get(nameof(LastLoop), GetOldValue<bool>("lastLoop", false));
            set
            {
                Preferences.Set(nameof(LastLoop), value);
            }

            //get
            //{
            //    if (Application.Current.Properties.ContainsKey("lastLoop"))
            //    {
            //        bool s = (bool)Application.Current.Properties["lastLoop"];
            //        return s;
            //    }

            //    return false;
            //}
            //set
            //{
            //    Application.Current.Properties["lastLoop"] = value;
            //}
        }
        static public string LastFolder
        {

            get => Preferences.Get(nameof(LastFolder), GetOldValue<string>(nameof(LastFolder)));
            set
            {
                Preferences.Set(nameof(LastFolder), value);
            }

            //get
            //{
            //    return LoadPersitedValue("LastFolder");
            //}
            //set
            //{
            //    Application.Current.Properties["LastFolder"] = value;
            //}
        }

        static public string LastCloud
        {
            get => Preferences.Get(nameof(LastCloud), GetOldValue<string>(nameof(LastCloud)));
            set
            {
                Preferences.Set(nameof(LastCloud), value);
            }

            //get
            //{
            //    return LoadPersitedValue("LastCloud");
            //}
            //set
            //{
            //    Application.Current.Properties["LastCloud"] = value;
            //}
        }

        static private string LoadPersitedValue(string storedName)
        {
            return Preferences.Get(storedName, GetOldValue<string>(storedName, null));
        }

        static private string TrackNumberKey(string project, string trackname)
        {
            string key = project + '_' + trackname;
            return key;
        }

        static public int GetTrackNumber(string project, string trackname)
        {
            return Preferences.Get(TrackNumberKey(project, trackname), GetOldValue<int>(TrackNumberKey(project, trackname), 0));
            //string key = TrackNumberKey(project, trackname);

            //if (Application.Current.Properties.ContainsKey(key))
            //{
            //    int tracknum = (int)Application.Current.Properties[key];
            //    return tracknum;
            //}

            //return 0;
        }

        static public void SetTrackNumber(string project, string trackname, int tracknum)
        {
            Preferences.Set(TrackNumberKey(project, trackname), tracknum);
            //string key = TrackNumberKey(project, trackname);
            //Application.Current.Properties[key] = tracknum;
        }

        private static string ProviderKey(string project, string trackname, string date)
        {
            return "ProviderKey_" + project + "_" + trackname + "_" + date;
        }

        static public void SetProvider(string project, string trackname, string date, CloudProviders provider)
        {
            Preferences.Set(ProviderKey(project, trackname, date), provider.ToString());
            //string key = ProviderKey(project, trackname, date);
            //Application.Current.Properties[key] = provider.ToString();
        }


        static public CloudProviders GetProvider(string project, string trackname, string date)
        {
            string cpstr = Preferences.Get(ProviderKey(project, trackname, date), GetOldValue<string>(ProviderKey(project, trackname, date), null));
            if(!string.IsNullOrEmpty(cpstr))
            {
                CloudProviders cp;

                if (Enum.TryParse<CloudStorage.CloudProviders>(cpstr, out cp))
                {
                    return cp;
                }
            }

            //string key = ProviderKey(project, trackname, date);
            //if(Application.Current.Properties.ContainsKey(key))
            //{
            //    string cpstr = (string)Application.Current.Properties[key];
            //    CloudProviders cp;

            //    if (Enum.TryParse<CloudStorage.CloudProviders>(cpstr, out cp))
            //    {
            //        return cp;
            //    }
            //}

            return CloudProviders.NULL;
        }

        static public void ResetProvider(string project, string trackname, string date)
        {
            Preferences.Remove(ProviderKey(project, trackname, date));
            //string key = ProviderKey(project, trackname, date);
            //Application.Current.Properties.Remove(key);
        }

        private static string CloudRootKey(string project, string trackname, string date)
        {
            return "CloudRootKey_" + project + "_" + trackname + "_" + date;
        }

        static public void SetCloudRoot(string project, string trackname, string date, string root)
        {
            Preferences.Set(CloudRootKey(project, trackname, date), root);
            //string key = CloudRootKey(project, trackname, date);
            //Application.Current.Properties[key] = root;
        }


        static public string GetCloudRoot(string project, string trackname, string date)
        {
            //string key = CloudRootKey(project, trackname, date);
            //if (Application.Current.Properties.ContainsKey(key))
            //{
            //    string cpstr = (string)Application.Current.Properties[key];
                
            //    return cpstr;
            //}

            return Preferences.Get(CloudRootKey(project, trackname, date), GetOldValue<string>(CloudRootKey(project, trackname, date), null));
        }

        static public void ResetCloudRoot(string project, string trackname, string date)
        {
            Preferences.Remove(CloudRootKey(project, trackname, date));
            //string key = CloudRootKey(project, trackname, date);
            //Application.Current.Properties.Remove(key);
        }

        static public void LoadQueuedTracks(ObservableCollection<QueuedTrack> qt)
        {
            qt.Clear();

            try
            {
                for (int n = 0; n < Preferences.Get(KEY_QUEUED_TRACK_COUNT, GetOldValue<int>(KEY_QUEUED_TRACK_COUNT, 0)); n++)
                {
                    string value = Preferences.Get(KEY_QUEUED_TRACK_ID + n.ToString(), GetOldValue<string>(KEY_QUEUED_TRACK_ID + n.ToString(), null));

                    if (!string.IsNullOrEmpty(value))
                    {
                        string[] trackParams = value.Split(',');
                        if (trackParams.Length >= 3)
                        {
                            DateTime d = DateTime.UtcNow;

                            if (trackParams.Length >= 4)
                            {
                                if (!DateTime.TryParse(trackParams[3], out d))
                                {
                                    Debug.Print("Error parsing date for {0}", trackParams[3]);
                                }
                            }
                            qt.Add(new QueuedTrack() { Name = trackParams[0], Project = trackParams[1], FullPath = trackParams?[2], LastModifiedDate = d });
                        }
                    }
                }
                //if (Application.Current.Properties.ContainsKey(KEY_QUEUED_TRACK_COUNT))
                //{
                //    for (int n = 0; n < (int)Application.Current.Properties[KEY_QUEUED_TRACK_COUNT]; n++)
                //    {
                //        string key = KEY_QUEUED_TRACK_ID + n.ToString();

                //        if (Application.Current.Properties.ContainsKey(key))
                //        {
                //            string value = (string)Application.Current.Properties[key];

                //            string[] trackParams = value.Split(',');
                //            if(trackParams.Length >= 3)
                //            {
                //                DateTime d = DateTime.UtcNow;

                //                if(trackParams.Length >= 4)
                //                {
                //                    if(!DateTime.TryParse(trackParams[3], out d))
                //                    {
                //                        Debug.Print("Error parsing date for {0}", trackParams[3]);
                //                    }
                //                }
                //                qt.Add(new QueuedTrack() { Name = trackParams[0], Project = trackParams[1], FullPath = trackParams?[2], LastModifiedDate = d });
                //            }
                //        }
                //    }
                //}
            }
            catch(InvalidCastException ex)
            {
                Debug.Print(ex.Message);
            }

        }

        static public void SaveQueuedTracks(ObservableCollection<QueuedTrack> qtlist)
        {
            int i = 0;
            foreach(QueuedTrack qt in qtlist)
            {
                Preferences.Set(KEY_QUEUED_TRACK_ID + i.ToString(), qt.Name + "," + qt.Project + "," + qt.FullPath + "," + qt.LastModifiedDate.ToString());
                //string key = KEY_QUEUED_TRACK_ID + i.ToString();
                //Application.Current.Properties[key] = qt.Name + "," + qt.Project + "," + qt.FullPath + "," + qt.LastModifiedDate.ToString();
                i++;
            }

            Preferences.Set(KEY_QUEUED_TRACK_COUNT, qtlist.Count);
            //Application.Current.Properties[KEY_QUEUED_TRACK_COUNT] = qtlist.Count;
            //await Application.Current.SavePropertiesAsync();
        }

        static public void SaveNotes(QueuedTrack qt, string notes)
        {
            Preferences.Set(qt.FullPath, notes);
            //Application.Current.Properties[qt.FullPath] = notes;
        }

        static public string LoadNotes(QueuedTrack qt)
        {
            return Preferences.Get(qt.FullPath, GetOldValue<string>(qt.FullPath, null));
            //if(Application.Current.Properties.ContainsKey(qt.FullPath))
            //{
            //    return (string)Application.Current.Properties[qt.FullPath];
            //}
            //else
            //{
            //    return null;
            //}
        }

        private static T GetOldValue<T>(string key)
        {
            if (Application.Current.Properties.ContainsKey(key))
            {
                return (T)Application.Current.Properties[key];
            }

            return default(T);
        }

        private static T GetOldValue<T>(string key, T defaultvalue)
        {
            if (Application.Current.Properties.ContainsKey(key))
            {
                return (T)Application.Current.Properties[key];
            }

            return defaultvalue;
        }
    }
}
