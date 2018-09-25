using PCLStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
//using Xamarin.Plugin.FilePicker;

using Plugin.SimpleAudioPlayer;
using System.IO;
using Plugin.FilePicker.Abstractions;
using Plugin.FilePicker;
using static MyMixes.ProviderInfo;

namespace MyMixes
{
    public partial class MainPage : ContentPage
    {
        private List<View> Takes = new List<View>();
        private string selectedFolder = "";
        private ISimpleAudioPlayer player;
        private string playingSong;
        private bool isSongPlaying;

        private MainVM ViewModel = new MainVM();

        public MainPage()
        {
            InitializeComponent();

            player = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();

            this.BindingContext = ViewModel;
        }

        private async void TrackView_Sel(object sender, EventArgs e)
        {
            Track t = (Track)Projects.SelectedItem;
            if (t.isProject)
            {
                selectedFolder = t.Name;

                double scrollY = TrackScroll.ScrollY;

                await LoadProjects();

                await TrackScroll.ScrollToAsync(0, scrollY, false);
            }
        }

        private async void Add_Clicked(object sender, EventArgs e)
        {
            var ProviderChoices = Enum.GetNames(typeof(CloudProviders));
        
            var action = await DisplayActionSheet("Which cloud platform?", "Cancel", null, ProviderChoices);

            // BUGBUG: Not finished
            ProviderInfo pi = new ProviderInfo();
            if(action != "Cancel")
            {
                ICloudStore cs = await GetCloudProvider((CloudProviders)Enum.Parse(typeof(CloudProviders), action));

                if (cs != null)
                {
                    if (await CloudStoreUtils.Authenticate(cs))
                    {
                        //CrossFilePicker.Current.PickFile();
                    }
                }
            }
        }

        private async void Play_Clicked(object sender, EventArgs e)
        {
            Track t = FindTrack((View)sender);

            if (playingSong != t.FullPath)
            {
                string path = Path.GetDirectoryName(t.FullPath);
                string filename = Path.GetFileName(t.FullPath);

                Debug.Print("playing {0} {1}\n", filename, path);

                IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(path); ;
                IFile source = await folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                player.Stop();
                using (Stream s = await source.OpenAsync(PCLStorage.FileAccess.Read))
                {
                    if (player.Load(s))
                    {
                        playingSong = t.FullPath;
                        CurrentSong.Text = filename;
                        PlayButton.Image = "PauseBt.png";
                        player.Play();
                    }
                    else
                    {
                        await DisplayAlert("Error openning track ", t.FullPath, "OK");
                    }
                }
            }
            else
            {
                playingSong = "";

                player.Stop();
            }
        }

        private async Task SyncProjects()
        {
            // seed with static data for now REMOVE
            ProviderInfo piA = new ProviderInfo();

            piA.RootPath = "/Mixes";
            piA.CloudProvider = CloudProviders.OneDrive;
            PersistentData.MixLocations.Add(piA);

            foreach(ProviderInfo pi in PersistentData.MixLocations)
            {
                List<string> l = await pi.GetFoldersAsync();
                if(l != null)
                {
                    foreach (string f in l)
                    {
                        await pi.UpdateProjectAsync(f);
                    }
                }
            }

            PersistentData.Save();
        }

        private async Task LoadProjects()
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            IList<IFolder> folderList = await folder.GetFoldersAsync();
            var tracks = new List<Track>();

            Debug.Print("Project local {0}\n", folder.Path);

            try
            {
                foreach (IFolder f in folderList)
                {
                    if (await WavDirectory(f))
                    {
                        var p = new Track { Name = f.Name, FullPath = f.Path, isProject = true };
                        tracks.Add(p);

                        if (f.Name == selectedFolder)
                        {
                            IList<IFile> fileList = await f.GetFilesAsync();
                            foreach (IFile fl in fileList)
                            {
                                var t = new Track { Name = fl.Name, FullPath = fl.Path, isProject = false };
                                tracks.Add(t);
                            }
                        }
                    }
                }

                Projects.ItemsSource = tracks;

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            Debug.Print("Project local {0} DONE\n", folder.Path);

        }

        private async Task<bool> WavDirectory(IFolder f)
        {
            IList<IFile> l = await f.GetFilesAsync();
            foreach (IFile fl in l)
            {
                if (fl.Name.EndsWith("wav") || fl.Name.EndsWith("mp3"))
                    return true;
            }

            return false;
        }

        private async void PlaySong_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(playingSong))
            {
                if(isSongPlaying)
                {
                    isSongPlaying = false;
                    player.Pause();
                    PlayButton.Image = "PauseBt.png";
                }
                else
                {
                    isSongPlaying = true;
                    player.Play();
                    PlayButton.Image = "PlayBt.png";
                }
            }
        }

        private void DeleteFolder_Clicked(object sender, EventArgs e)
        {

        }

        private async Task Delete_Clicked(object sender, EventArgs e)
        {
            Track t = (Track)Projects.SelectedItem;

            ProjectMapping pm = PersistentData.ProjectMappings.Find((x) => x.project == t.Name);

            if(pm != null)
            {
                ICloudStore pi = await GetCloudProvider(pm.provider);
                bool deleted = await pi.DeleteSong(pm.project + "/" + t.Name);
                if (deleted)
                {
                    bool localDeleted = await DeleteFile(t.FullPath);
                    if (!localDeleted)
                    {
                        await DisplayAlert("Error Deleting", "Local project file could not be deleted.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Error Deleting", "Remote project file could not be deleted.  Aborting deletion", "OK");
                }
            }
            else
            {
                await DisplayAlert("Error Deleting", "No entry for local project, is installation corrupt?", "OK");
            }
        }

        private async Task<bool> DeleteFile(string fullPath)
        {
            try
            {
                IFolder folder = FileSystem.Current.LocalStorage;
                IFile file = await folder.CreateFileAsync(fullPath, CreationCollisionOption.OpenIfExists);

                await file.DeleteAsync();
            }
            catch(Exception ex)
            {
                await DisplayAlert("Exception Delete File", ex.Message, "OK");
                return false;
            }

            return true;
        }

        private async void OnAppearing(object sender, EventArgs e)
        {
            await LoadProjects();
        }

        private async void Sync_Clicked(object sender, EventArgs e)
        {
            // DCR: Maybe we don't sync all the time
            await SyncProjects();
        }

        Track FindTrack(View v)
        {
            Grid g = (Grid)v.Parent;

            List<Track> tl = (List<Track>)Projects.ItemsSource;

            Label l;

            if (g.Children[0] is Label)
            {
               l = (Label)(g.Children[0]);
            }
            else
            {
                l = (Label)(g.Children[1]);
            }

            Track t = tl.Find((x) => x.Name == l.Text);
            return t;
        }
    }
}
