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

        public string providerName;
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
                }
            }
        }

        public CloudStorage.CloudProviders CurrentProvider
        {
            get
            {
                return (CloudStorage.CloudProviders)Enum.Parse(typeof(CloudStorage.CloudProviders), providerName);
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
        }

        private List<string> providerList = null;
        public List<string> ProviderList
        {
            get
            {
                if(providerList == null)
                {
                    providerList = new List<string>();
                    
                    providerList.Add(CloudStorage.CloudProviders.OneDrive.ToString());
                    providerList.Add(CloudStorage.CloudProviders.GoogleDrive.ToString());
                }

                return providerList;
            }
        }
        
		public ProjectPicker (ObservableCollection<MixLocation> list)
		{
			InitializeComponent ();

            BindingContext = this;

            currentFolder = PersistentData.LastFolder;
            ProviderNameText = PersistentData.LastCloud;

            MixLocationList = list;
        }

        private async void OnAppearing(object sender, EventArgs e)
        {
            //if(!string.IsNullOrEmpty(providerName))
            //{
            //    pi = await ProviderInfo.GetCloudProviderAsync(CurrentProvider);

            //    if (!(await pi.CheckAuthenitcation()))
            //    {
            //        await Navigation.PopAsync();
            //    }
            //}

            MixLocationView.ItemsSource = MixLocationList;

            await UpdateFolderList();
        }

        private async Task UpdateFolderList()
        {
            if(pi != null && await pi.CheckAuthenitcation())
            {
                List<string> folders = await pi.GetFoldersAsync(currentFolder);
                FolderList.ItemsSource = folders;
            }
        }

        private async void SelectPressed(object sender, EventArgs e)
        {
            string p = string.IsNullOrEmpty(currentFolder) ? "" : (currentFolder + "/");
            MixLocationList.Add(new MixLocation() { Path = p + (string)FolderList.SelectedItem, Provider = pi.CloudProvider });
            PersistentData.SaveMixLocations(MixLocationList);
        }

        private async void OpenFolder(object sender, EventArgs e)
        {
            if(FolderList.SelectedItem != null)
            {
                currentFolder += "/" + (string)FolderList.SelectedItem;
                await UpdateFolderList();
            }
        }

        private async void Cancel(object sender, EventArgs e)
        {
        }

        private async void UpPressed(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(currentFolder))
            {
                int idx = currentFolder.LastIndexOf('/');
                if(idx >= 0)
                {
                    currentFolder = currentFolder.Substring(0, idx);
                    await UpdateFolderList();
                }
            }
        }

        private void ChangeProvider(object sender, EventArgs e)
        {

        }

        private async void OnProviderChanged(object sender, EventArgs e)
        {
            currentFolder = "";
            pi = await ProviderInfo.GetCloudProviderAsync(CurrentProvider);
            
            await UpdateFolderList();
        }

        private void OnDisappearing(object sender, EventArgs e)
        {
            PersistentData.LastFolder = currentFolder;
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
    }
}   