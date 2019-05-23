using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using CloudStorage;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Microsoft.AppCenter.Analytics;

namespace MyMixes
{
    public class MixLocation
    {
        public CloudProviders Provider { get; set; }
        public string Path { get; set; }
    }

    public class DirectoryEntry
    {
        public string DirectoryName { get; set; }
    }


    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProjectPicker : ContentPage, INotifyPropertyChanged
    {
        ProviderInfo pi;
        //ObservableCollection<MixLocation> MixLocationList;
        ObservableCollection<DirectoryEntry> DirectoryList = new ObservableCollection<DirectoryEntry>();

        private BusyBarViewModel ppd;

        public string providerName = "nothing";
        public string ProviderNameText
        {
            get
            {
                return providerName;
            }
            set
            {
                if (providerName != value)
                {
                    providerName = value;
                    OnPropertyChanged("ProviderNameText");

                    if(CurrentProvider == CloudProviders.NULL)
                    {
                        providerName = "select provider...";
                    }
                }
            }
        }

        public CloudStorage.CloudProviders CurrentProvider
        {
            get
            {
                CloudStorage.CloudProviders cp;
                if(Enum.TryParse<CloudStorage.CloudProviders>(providerName, out cp))
                {
                    return cp;
                }
                else
                {
                    return CloudProviders.NULL;
                }
                //= (CloudStorage.CloudProviders)Enum.Parse(typeof(CloudStorage.CloudProviders), providerName);
                //return (CloudStorage.CloudProviders)Enum.Parse(typeof(CloudStorage.CloudProviders), providerName);
            }
            set
            {
                ProviderNameText = value.ToString();
            }
        }

        private string currentFolder;
        public string CurrentFolder
        {
            get { return currentFolder; }
            set
            {
                if (currentFolder != value)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        currentFolder = "";
                    }
                    else
                    {
                        currentFolder = value;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged("CurrentDisplayFolder");
                }
            }
        }

        public string CurrentDisplayFolder
        {
            get
            {
                if(string.IsNullOrEmpty(currentFolder))
                {
                    return "/";
                }
                else
                {
                    return currentFolder;
                }
            }
        }


        private bool addEnabled = false;
        public bool AddEnabled
        {
            get
            {
                return addEnabled;
            }
            set
            {
                if (addEnabled != value)
                {
                    OnPropertyChanged();
                    addEnabled = value;
                }
            }
        }

        private string currentMixLocation;
        public string CurrentMixLocation
        {
            get
            {
                //if(!string.IsNullOrEmpty(currentMixLocation))
                //{
                //    AddEnabled = true;
                //    return currentMixLocation + "?";
                //}
                //else
                //{
                //    AddEnabled = false;
                //    return null;
                //}
                return null;
            }
            set
            {
                if (currentMixLocation != value)
                {
                    currentMixLocation = value;
                    OnPropertyChanged();
                }
            }
        }


        private List<string> providerList = null;
        private CloudProviders lastProvider;

        public List<string> ProviderList
        {
            get
            {
                if(providerList == null)
                {
                    providerList = new List<string>();
                    providerList.Add("select provider...");
                    providerList.Add(CloudStorage.CloudProviders.OneDrive.ToString());
                    providerList.Add(CloudStorage.CloudProviders.GoogleDrive.ToString());
                }

                return providerList;
            }
        }
        
		public ProjectPicker ()
		{
			InitializeComponent ();

            CloudProivder.BindingContext = this;
            PathBreadCrumbs.BindingContext = this;
            FolderList.BindingContext = this;
            AddButton.BindingContext = this;

            CloudProivder.ItemsSource = ProviderList;

            CurrentFolder = PersistentData.LastFolder;
            ProviderNameText = PersistentData.LastCloud;
            FolderList.ItemsSource = DirectoryList;

            //MixLocationList = PersistentData.MixLocationList;

            ppd = new BusyBarViewModel();
            busyControl.BindingContext = ppd;
        }

        private async void OnAppearing(object sender, EventArgs e)
        {
            MixLocationView.ItemsSource = PersistentData.MixLocationList;

            await UpdateFolderList();

            Dictionary<String, String> properties = new Dictionary<string, string>();

            if (PersistentData.MixLocationList.Count <= 0)
            {
                await DisplayAlert(AppResources.NoMixLocationsTitle, AppResources.NoMixLocations, AppResources.OK);
            }
            else
            {
                Dictionary<String, int> locations = new Dictionary<string, int>();

                foreach (MixLocation ml in PersistentData.MixLocationList)
                {
                    if(locations.ContainsKey(ml.Provider.ToString()))
                    {
                        locations[ml.Provider.ToString()]++;
                    }
                    else
                    {
                        locations[ml.Provider.ToString()] = 1;
                    }
                }

                foreach (KeyValuePair<String, int> kvp in locations)
                {
                    properties[kvp.Key] = kvp.Value.ToString();
                }
            }

            properties["NumLocations"] = PersistentData.MixLocationList.Count.ToString();

            Analytics.TrackEvent("MixLocations", properties);
        }

        private async Task<bool> UpdateFolderList()
        {
            if(pi != null && await pi.CheckAuthenitcationAsync() && CurrentFolder != null)
            {
                BusyOn(true);
                ppd.BusyText = "Refreshing folder view from " + CurrentProvider.ToString() + " " + CurrentDisplayFolder;
                List<string> folders = await pi.GetFoldersAsync(CurrentFolder);
                //FolderList.ItemsSource = folders;
                DirectoryList.Clear();

                foreach(string d in folders)
                {
                    DirectoryList.Add(new DirectoryEntry() { DirectoryName = d });
                }

                BusyOn(false);

                return true;
            }
            else
            {
                return false;
            }
        }

        private async void SelectPressed(object sender, EventArgs e)
        {
            if (FolderList.SelectedItem != null)
            {
                string p = string.IsNullOrEmpty(CurrentFolder) ? "" : (CurrentFolder + "/");
                DirectoryEntry de = (DirectoryEntry)FolderList.SelectedItem;

                // Only add it if it's not there already
                var previousLoc = PersistentData.MixLocationList.FirstOrDefault((el) => (el.Path == p + de.DirectoryName));
                if(previousLoc == null)
                {
                    PersistentData.MixLocationList.Add(new MixLocation() { Path = p + de.DirectoryName, Provider = pi.CloudProvider });
                    PersistentData.SaveMixLocations();
                }
            }
        }

        private async void OpenFolder(object sender, EventArgs e)
        {
            if(FolderList.SelectedItem != null)
            {
                DirectoryEntry de = (DirectoryEntry)FolderList.SelectedItem;
                CurrentFolder += "/" + de.DirectoryName;
                await UpdateFolderList();
                OnPropertyChanged("CurrentDisplayFolder");
                CurrentMixLocation = null;
                FolderList.SelectedItem = null;
            }
        }

        private async void Cancel(object sender, EventArgs e)
        {
        }

        private async void UpPressed(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(CurrentFolder))
            {
                int idx = CurrentFolder.LastIndexOf('/');
                if(idx >= 0)
                {
                    string lastFolder = CurrentFolder.Substring(idx + 1);
                    CurrentMixLocation = lastFolder;

                    CurrentFolder = CurrentFolder.Substring(0, idx);
                    await UpdateFolderList();
                    for (int i = 0; i < DirectoryList.Count; i++)
                    {
                        if (lastFolder == DirectoryList[i].DirectoryName)
                        {
                            FolderList.SelectedItem = DirectoryList[i];
                        }
                    }
                }
            }
        }

        private void ChangeProvider(object sender, EventArgs e)
        {

        }

        private async void OnProviderChanged(object sender, EventArgs e)
        {

            CurrentFolder = "";
            if(CurrentProvider != CloudProviders.NULL)
            {
                pi = await ProviderInfo.GetCloudProviderAsync(CurrentProvider);

                bool worked = await UpdateFolderList();
                if(!worked)
                {
                    CurrentProvider = lastProvider;
                }
            }

            lastProvider = CurrentProvider;
        }

        private void OnDisappearing(object sender, EventArgs e)
        {
            PersistentData.LastFolder = CurrentFolder;
            PersistentData.LastCloud = ProviderNameText;

            PersistentData.Save();

        }

        private void LocationDeleted(object sender, EventArgs e)
        {
            PersistentData.MixLocationList.Remove(FindMixLocation((View)sender));
            PersistentData.SaveMixLocations();
        }

        MixLocation FindMixLocation(View v)
        {
            Grid g = (Grid)v.Parent;
            MixLocation t = (MixLocation)g.BindingContext;
            return t;
        }
        private void BusyOn(bool TurnOn)
        {
            ppd.IsRunning = TurnOn;
            ppd.IsVisible = TurnOn;

            MainStack.IsEnabled = !TurnOn;
        }

        private void FolderList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (FolderList.SelectedItem != null)
            {
                DirectoryEntry de = (DirectoryEntry)FolderList.SelectedItem;
                CurrentMixLocation = de.DirectoryName;
                AddEnabled = true;
            }
            else
            {
                CurrentMixLocation = null;
                AddEnabled = false;
            }
        }
    }
}   