using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using CloudStorage;
using System.ComponentModel;

namespace MyMixes
{
    public class MixLocations
    {
        public string Provider { get; set; }
        public string Path { get; set; }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProjectPicker : ContentPage, INotifyPropertyChanged
    {
        ProviderInfo pi;

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
                providerName = value.ToString();
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
        
		public ProjectPicker ()
		{
            currentFolder = PersistentData.LastFolder;
            providerName = PersistentData.LastCloud;

            BindingContext = this;

			InitializeComponent ();
		}

        private async void OnAppearing(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(CurrentFolder) && !string.IsNullOrEmpty(providerName))
            {
                pi = await ProviderInfo.GetCloudProviderAsync((CloudStorage.CloudProviders)Enum.Parse(typeof(CloudStorage.CloudProviders), providerName), currentFolder);

                if (!(await pi.CheckAuthenitcation()))
                {
                    await Navigation.PopAsync();
                }


            }

            LoadMixLocations();

            UpdateFolderList();
        }

        private void LoadMixLocations()
        {
            List<string> mappings = PersistentData.LoadProjectMappings();
            List<MixLocations> ml_list = new List<MixLocations>();

            foreach (string s in mappings)
            {
                string[] parts = s.Split(':');
                CloudProviders cp = (CloudProviders)Enum.Parse(typeof(CloudProviders), parts[0]);
                ml_list.Add(new MixLocations { Path =parts[1], Provider = parts[0] });
            }

            MixLocationView.ItemsSource = ml_list;
        }

        private async Task UpdateFolderList()
        {
            List<string> folders = await pi.GetFoldersAsync(currentFolder);
            FolderList.ItemsSource = folders;
        }

        private async void SelectPressed(object sender, EventArgs e)
        {
            currentFolder = (string)FolderList.SelectedItem;

            pi.UpdatePath(currentFolder);

            ProviderInfo.SaveMappings();
            //
            //base.OnBackButtonPressed();

            await Navigation.PopModalAsync();
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
            await Navigation.PopModalAsync();
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
            currentFolder = "/";
            pi = await ProviderInfo.GetCloudProviderAsync(CurrentProvider, currentFolder);
            
            await UpdateFolderList();
        }
    }
}   