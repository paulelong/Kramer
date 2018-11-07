using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

using CloudStorage;

namespace MyMixes
{
    public class ProjectMapping
    {
        public CloudProviders provider;
        public string project;
    }

    public static class PersistentData
    {
        public static void Save()
        {
            Application.Current.SavePropertiesAsync();
        }

        private static List<ProjectMapping> projectMappings = new List<ProjectMapping>();
        public static List<ProjectMapping> ProjectMappings
        {
            get
            {
                if(projectMappings == null)
                {
                    projectMappings = new List<ProjectMapping>();
                    LoadProjectMappings();
                }

                return projectMappings;
            }
        }

        public static void SaveProjectMappings()
        {
            Dictionary<CloudProviders, List<string>> ProjectsByProviders = new Dictionary<CloudProviders, List<string>>();

            foreach(CloudProviders pmname in Enum.GetValues(typeof(CloudProviders)))
            {
                ProjectsByProviders[pmname] = new List<string>();
            }

            foreach(ProjectMapping pm in projectMappings)
            {
                ProjectsByProviders[pm.provider].Add(pm.project);
            }

            foreach(KeyValuePair<CloudProviders, List<string>> kvp in ProjectsByProviders)
            {
                Application.Current.Properties["ProjectMap_" + kvp.Key] = string.Join(",", kvp.Value.ToArray());
            }
        }

        public static void LoadProjectMappings()
        {
            foreach (CloudProviders pmname in Enum.GetValues(typeof(CloudProviders)))
            {
                if(Application.Current.Properties.ContainsKey("ProjectMap_" + pmname))
                {
                    string list = (string)Application.Current.Properties["ProjectMap_" + pmname];
                    foreach(string p in list.Split(','))
                    {
                        projectMappings.Add(new ProjectMapping() { project = p, provider = pmname });
                    }
                }
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
            //IFolder rootfolder = FileSystem.Current.LocalStorage;

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

        static List<ProviderInfo> mixLocations = new List<ProviderInfo>();
        public static List<ProviderInfo> MixLocations
        {
            get
            {

                if (Application.Current.Properties.ContainsKey("mixLocCount"))
                {
                    int count;
                    count = (int)Application.Current.Properties["CloudProviderCount"];

                    for (int n = 0; n < count; n++)
                    {
                        string key = "mixloc" + n.ToString();
                        if (Application.Current.Properties.ContainsKey(key))
                        {
                            ProviderInfo pi = new ProviderInfo
                            {
                                RootPath = (string)Application.Current.Properties[key],
                                CloudProvider = (CloudProviders)Application.Current.Properties["cloudprovider" + n.ToString()]
                            };

                            mixLocations.Add(pi);
                        }

                    }
                }

                return mixLocations;
            }
        }

        static void AddLocation(string path, CloudProviders cp)
        {
            string key = "mixloc" + ProviderCount.ToString();

            ProviderInfo pi = new ProviderInfo
            {
                RootPath = (string)Application.Current.Properties[key],
                CloudProvider = (CloudProviders)Application.Current.Properties["cloudprovider" + ProviderCount.ToString()]
            };

            ProviderCount++;

            mixLocations.Add(pi);
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

        static private string LoadPersitedValue(string storedName)
        {
            if (Application.Current.Properties.ContainsKey(storedName))
            {
                string s = (string)Application.Current.Properties[storedName];
                return s;
            }

            return null;
        }
    }
}
