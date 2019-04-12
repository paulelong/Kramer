﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using CloudStorage;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MyMixes
{
    public class MixLocation
    {
        public CloudProviders Provider { get; set; }
        public string Path { get; set; }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProjectPicker : ContentPage, INotifyPropertyChanged
    {
        ProviderInfo pi;
        ObservableCollection<MixLocation> MixLocationList;

        private ProjectPickerData ppd;

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
                if(currentFolder != value)
                {
                    OnPropertyChanged("CurrentFolder");
                    currentFolder = value;
                }
            }
        }

        private List<string> providerList = null;
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
        
		public ProjectPicker (ObservableCollection<MixLocation> list)
		{
			InitializeComponent ();

            CloudProivder.BindingContext = this;
            PathBreadCrumbs.BindingContext = this;

            CloudProivder.ItemsSource = ProviderList;

            CurrentFolder = PersistentData.LastFolder;
            ProviderNameText = PersistentData.LastCloud;

            MixLocationList = list;

            ppd = (ProjectPickerData)this.BindingContext;
        }

        private async void OnAppearing(object sender, EventArgs e)
        {
            MixLocationView.ItemsSource = MixLocationList;

            await UpdateFolderList();
        }

        private async Task UpdateFolderList()
        {
            if(pi != null && await pi.CheckAuthenitcationAsync())
            {
                BusyOn(true);
                List<string> folders = await pi.GetFoldersAsync(CurrentFolder);
                FolderList.ItemsSource = folders;
                BusyOn(false);
            }
        }

        private async void SelectPressed(object sender, EventArgs e)
        {
            string p = string.IsNullOrEmpty(CurrentFolder) ? "" : (CurrentFolder + "/");
            MixLocationList.Add(new MixLocation() { Path = p + (string)FolderList.SelectedItem, Provider = pi.CloudProvider });
            PersistentData.SaveMixLocations(MixLocationList);
        }

        private async void OpenFolder(object sender, EventArgs e)
        {
            if(FolderList.SelectedItem != null)
            {
                CurrentFolder += "/" + (string)FolderList.SelectedItem;
                await UpdateFolderList();
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
                    CurrentFolder = CurrentFolder.Substring(0, idx);
                    await UpdateFolderList();
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

                await UpdateFolderList();
            }
        }

        private void OnDisappearing(object sender, EventArgs e)
        {
            PersistentData.LastFolder = CurrentFolder;
            PersistentData.LastCloud = ProviderNameText;

            Application.Current.SavePropertiesAsync();
        }

        private void LocationDeleted(object sender, EventArgs e)
        {
            MixLocationList.Remove(FindMixLocation((View)sender));
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
    }
}   