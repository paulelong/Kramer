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

        public string ProviderNameText
        {
            get
            {
                return pi.CloudProvider.ToString();
            }
        }

        private string currentFolder;
        public string CurrentFolder
        {
            get { return currentFolder; }
        }
        
		public ProjectPicker (ProviderInfo pi)
		{
            this.pi = pi;
            currentFolder = "";

            BindingContext = this;

			InitializeComponent ();

		}

        private async void OnAppearing(object sender, EventArgs e)
        {
            if(!(await pi.CheckAuthenitcation()))
            {
                await Navigation.PopAsync();
            }

            LoadMixLocations();

            UpdateFolderList();
        }

        private void LoadMixLocations()
        {
            List<MixLocations> ml_list = new List<MixLocations>();

            foreach (ProviderInfo pi in ProviderInfo.Providers)
            {
                ml_list.Add(new MixLocations { Path = pi.RootPath, Provider = pi.CloudProvider.ToString() });
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
    }
}   