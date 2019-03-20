﻿using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

using CloudStorage;
using System.Collections.ObjectModel;
using System.Diagnostics;

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
            Application.Current.SavePropertiesAsync();
        }

        internal static void LoadMixLocations(ObservableCollection<MixLocation> mixLocationList)
        {
            foreach (CloudProviders cpn in Enum.GetValues(typeof(CloudProviders)))
            {
                if (Application.Current.Properties.ContainsKey("ProjectMap_" + cpn))
                {
                    string list = (string)Application.Current.Properties["ProjectMap_" + cpn];
                    if (!string.IsNullOrEmpty(list))
                    {
                        foreach (string p in list.Split(','))
                        {
                            mixLocationList.Add(new MixLocation() { Provider = cpn, Path = p });
                        }
                    }
                }
            }
        }

        internal static void SaveMixLocations(ObservableCollection<MixLocation> mixLocationList)
        {
            Dictionary<CloudProviders, List<string>> ProjectsByProviders = new Dictionary<CloudProviders, List<string>>();

            foreach (MixLocation ml in mixLocationList)
            {
                ProjectsByProviders[ml.Provider].Add(ml.Path);
            }

            foreach (KeyValuePair<CloudProviders, List<string>> kvp in ProjectsByProviders)
            {
                Application.Current.Properties["ProjectMap_" + kvp.Key] = string.Join(",", kvp.Value.ToArray());
            }
        }

        public static List<string> GetProjectFoldersData(string provider, string root)
        {
            string key = provider + "_" + root;

            if(Application.Current.Properties.ContainsKey(key))
            {
                string projects = (string)Application.Current.Properties[key];
                return new List<string>(projects.Split(','));
            }
            else
            {
                return null;
            }
        }

        public static void PutProjectFoldersData(string provider, string root, string value)
        {
            string key = provider + "_" + root;

            Application.Current.Properties[key] = value;
        }

        public static async Task<bool> isRemoteNewer(string path, DateTime lastModified)
        {
            string filepath = Path.GetDirectoryName(path);
            string name = Path.GetFileName(path);

            IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(filepath);
            ExistenceCheckResult result = await folder.CheckExistsAsync(name);

            if(result != ExistenceCheckResult.FileExists)
            {
                return true;
            }

            if (Application.Current.Properties.ContainsKey(path))
            {
                DateTime localLastModified = (DateTime)Application.Current.Properties[path];
                if (lastModified <= localLastModified)
                {
                    return false;
                }
            }

            Application.Current.Properties[path] = lastModified;
            return true;
        }

        static int ProviderCount
        {
            get
            {
                return (int)Application.Current.Properties["CloudProviderCount"];
            }
            set
            {
                Application.Current.Properties["CloudProviderCount"] = value;
            }
        }

        static string googleToken;
        public static string GoogleToken
        {
            get
            {
                if (googleToken == null)
                {
                    googleToken = LoadPersitedValue("GoogleToken");
                    if (googleToken == null)
                    {
                        return "";
                    }
                }

                return googleToken;
            }
            set
            {
                googleToken = value;
                Application.Current.Properties["GoogleToken"] = googleToken;
            }
        }

        public static int LastPlayedSongIndex
        {
            get
            {
                if (Application.Current.Properties.ContainsKey("CurrentSongIndex"))
                {
                    int s = (int)Application.Current.Properties["CurrentSongIndex"];
                    return s;
                }

                return 0;
            }
            set
            {
                Application.Current.Properties["CurrentSongIndex"] = value;
            }
        }

        static public string LastFolder
        {
            get
            {
                return LoadPersitedValue("LastFolder");
            }
            set
            {
                Application.Current.Properties["LastFolder"] = value;
            }
        }

        static public string LastCloud
        {
            get
            {
                return LoadPersitedValue("LastCloud");
            }
            set
            {
                Application.Current.Properties["LastCloud"] = value;
            }
        }

        static private string LoadPersitedValue(string storedName)
        {
            if (Application.Current.Properties.ContainsKey(storedName))
            {
                string s = (string)Application.Current.Properties[storedName];
                return s;
            }

            return null;
        }

        static private string TrackNumberKey(string project, string trackname)
        {
            string key = project + '_' + trackname;
            return key;
        }

        static public int GetTrackNumber(string project, string trackname)
        {
            string key = TrackNumberKey(project, trackname);

            if (Application.Current.Properties.ContainsKey(key))
            {
                int tracknum = (int)Application.Current.Properties[key];
                return tracknum;
            }

            return 0;
        }

        static public void SetTrackNumber(string project, string trackname, int tracknum)
        {
            string key = TrackNumberKey(project, trackname);
            Application.Current.Properties[key] = tracknum;
        }

        static public void LoadQueuedTracks(ObservableCollection<QueuedTrack> qt)
        {
            qt.Clear();

            try
            {
                if (Application.Current.Properties.ContainsKey(KEY_QUEUED_TRACK_COUNT))
                {
                    for (int n = 0; n < (int)Application.Current.Properties[KEY_QUEUED_TRACK_COUNT]; n++)
                    {
                        string key = KEY_QUEUED_TRACK_ID + n.ToString();

                        if (Application.Current.Properties.ContainsKey(key))
                        {
                            string value = (string)Application.Current.Properties[key];

                            string[] trackParams = value.Split(',');
                            if(trackParams.Length >= 3)
                            {
                                qt.Add(new QueuedTrack() { Name = trackParams[0], Project = trackParams[1], FullPath = trackParams?[2] });
                            }
                        }

                    }
                }
            }
            catch(InvalidCastException ex)
            {
                Debug.Print(ex.Message);
            }

        }

        static public async Task SaveQueuedTracks(ObservableCollection<QueuedTrack> qtlist)
        {
            int i = 0;
            foreach(QueuedTrack qt in qtlist)
            {
                string key = KEY_QUEUED_TRACK_ID + i.ToString();
                Application.Current.Properties[key] = qt.Name + "," + qt.Project + "," + qt.FullPath;
                i++;
            }

            Application.Current.Properties[KEY_QUEUED_TRACK_COUNT] = qtlist.Count;
            await Application.Current.SavePropertiesAsync();
        }
    }
}
