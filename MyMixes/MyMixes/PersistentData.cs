using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MyMixes
{
    public static class PersistentData
    {
        public static void Save()
        {
            Application.Current.SavePropertiesAsync();
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
                                CloudProvider = (ProviderInfo.CloudProviders)Application.Current.Properties["cloudprovider" + n.ToString()]
                            };

                            mixLocations.Add(pi);
                        }

                    }
                }

                return mixLocations;
            }
        }

        static void AddLocation(string path, ProviderInfo.CloudProviders cp)
        {
            string key = "mixloc" + ProviderCount.ToString();

            ProviderInfo pi = new ProviderInfo
            {
                RootPath = (string)Application.Current.Properties[key],
                CloudProvider = (ProviderInfo.CloudProviders)Application.Current.Properties["cloudprovider" + ProviderCount.ToString()]
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
