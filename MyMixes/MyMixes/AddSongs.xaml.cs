using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MyMixes
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AddSongs : ContentPage
	{
        private string selectedFolder = "";
        private Dictionary<string, int> PlayListOrder = new Dictionary<string, int>();

        ObservableCollection<QueuedTrack> SelectedTrackList = new ObservableCollection<QueuedTrack>();
        ObservableCollection<Track> LoadedTracks = new ObservableCollection<Track>();

        public AddSongs ()
		{
			InitializeComponent ();

            SelectedTracks.ItemsSource = SelectedTrackList;
            Projects.ItemsSource = LoadedTracks;

            PersistentData.LoadQueuedTracks(SelectedTrackList);
        }

        private void DeleteFolder_Clicked(object sender, EventArgs e)
        {

        }

        private void ResyncProjectClickedAsync(object sender, EventArgs e)
        {

        }

        private void LocalPlay_Clicked(object sender, EventArgs e)
        {

        }

        private async Task AddFolder_Clicked(object sender, EventArgs e)
        {
            var ProviderChoices = Enum.GetNames(typeof(CloudStorage.CloudProviders));


            var action = await DisplayActionSheet("Which cloud platform?", "Cancel", null, ProviderChoices);
            //            string action = await DisplayActionSheet("Which", "cancel", null, "1", "2", "3");
            //var action = await DisplayActionSheet("ActionSheet: Save Photo?", "Cancel", "Delete", "Photo Roll", "Email");
            //string action = "GoogleDrive";

            // BUGBUG: Not finished
            if (action != "Cancel")
            {
                ProviderInfo pi = await ProviderInfo.GetCloudProviderAsync((CloudStorage.CloudProviders)Enum.Parse(typeof(CloudStorage.CloudProviders), action));

                if (pi != null)
                {
                    ProjectPicker pp = new ProjectPicker(pi);
                    await Navigation.PushModalAsync(pp);

                    // No directory was selected.
                    if (pi.RootPath == null)
                    {
                        pi.RemoveProvider();
                        pi = null;
                    }
                }
            }

        }

        private async void SongOrderClicked(object sender, EventArgs e)
        {

            Track t = FindTrack((View)sender);

            //var list = (List<QueuedTrack>)SelectedTracks.ItemsSource;
            //if (list.Find((item) => item.Project == t.Project && item.Name == t.Name) == null)
            //{
            //    list.Add(new QueuedTrack() { Name = t.Name, Project = t.Project });
            //}

            //SelectedTracks.ItemsSource = SelectedTrackList;
            int i = 0;
            for(;i < SelectedTrackList.Count; i++)
            {
                if (SelectedTrackList[i].Name == t.Name && SelectedTrackList[i].Project == t.Project)
                    break;
            }

            if (i >= SelectedTrackList.Count)
            {
                SelectedTrackList.Add(new QueuedTrack() { Name = t.Name, Project = t.Project });
            }

            await PersistentData.SaveQueuedTracks(SelectedTrackList);

            //SelectedTracks.ItemsSource = SelectedTrackList;
        }

        private async void TrackView_Sel(object sender, SelectedItemChangedEventArgs e)
        {
            Track t = (Track)e.SelectedItem;

            if (!t.isProject)
            {

            }
            else
            {
                selectedFolder = t.Name;

                //double scrollY = TrackScroll.ScrollY;

                await LoadProjects();

                //await TrackScroll.ScrollToAsync(0, scrollY, false);
            }
        }

        private async Task OnAppearing(object sender, EventArgs e)
        {
            BusyOn(true);
            await ProviderInfo.LoadMappings();
            await LoadProjects();
            BusyOn(false);
        }

        private async void ResyncAllClickedAsync(object sender, EventArgs e)
        {
            // DCR: Maybe we don't sync all the time
            BusyOn(true);
            await SyncProjects();
            await LoadProjects();
            BusyOn(false);
        }

        private void BusyOn(bool TurnOn)
        {
            BusyGrid.IsVisible = TurnOn;
            BusySignal.IsRunning = TurnOn;
            MainStack.IsEnabled = !TurnOn;
        }

        Track FindTrack(View v)
        {
            Grid g = (Grid)v.Parent;
            Track t = (Track)g.BindingContext;
            return t;
        }

        private async Task SyncProjects()
        {
            Dictionary<string, List<string>> AllSongs = new Dictionary<string, List<string>>();

            //List<MixLocation> ml_list = await MixLocation.GetMixLocationsAsync();
            foreach (ProviderInfo pi in ProviderInfo.Providers)
            {
                BusyStatus.Text = pi.CloudProvider.ToString() + " " + pi.RootPath;

                if (await pi.CheckAuthenitcation())
                {
                    List<string> l = await pi.GetProjectFoldersAsync(pi.RootPath);
                    if (l != null)
                    {
                        foreach (string f in l)
                        {
                            BusyStatus.Text = pi.CloudProvider.ToString() + " " + pi.RootPath + " " + f;
                            var retList = await pi.UpdateProjectAsync(pi.RootPath, f);
                            if (AllSongs.ContainsKey(f))
                            {
                                AllSongs[f].AddRange(retList);
                            }
                            else
                            {
                                AllSongs[f] = retList;
                            }
                        }
                    }
                }
            }

            string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (string p in Directory.GetDirectories(rootPath))
            {
                if (!AllSongs.ContainsKey(Path.GetFileName(p)))
                {
                    Debug.Print("Remove dir " + p + "\n");
                    BusyStatus.Text = "Removing " + p;
                    Directory.Delete(p, true);
                }
                else
                {
                    foreach (string s in Directory.GetDirectories(p))
                    {
                        if (AllSongs[p].Contains(Path.GetFileName(s)))
                        {
                            BusyStatus.Text = "Removing " + s;
                            Debug.Print("Remove file " + s + "\n");
                            File.Delete(s);
                        }
                    }
                }
            }

            foreach (string p in AllSongs.Keys)
            {
                foreach (string f in AllSongs[p])
                {
                    BusyStatus.Text = p + " " + f;
                }
            }

            PersistentData.Save();
        }

        private async Task LoadProjects()
        {
            //Directory folder = Directory.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); // FileSystem.Current.LocalStorage;
            //IList<IFolder> folderList = await folder.GetFoldersAsync();
            //var tracks = new List<Track>();
            LoadedTracks.Clear();

            Debug.Print("Project local {0}\n", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            try
            {
                foreach (string projFolder in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)))
                //foreach (IFolder f in folderList)
                {
                    if (await WavDirectory(projFolder))
                    {
                        var p = new Track { Name = Path.GetFileName(projFolder), FullPath = projFolder, isProject = true };
                        LoadedTracks.Add(p);

                        if (selectedFolder == Path.GetFileName(projFolder))
                        {
                            //IList<IFile> fileList = await f.GetFilesAsync();
                            foreach (string songFile in Directory.GetFiles(projFolder))
                            {
                                int tracknum = PersistentData.GetTrackNumber(projFolder, Path.GetFileNameWithoutExtension(songFile));

                                var t = new Track
                                {
                                    Name = Path.GetFileNameWithoutExtension(songFile),
                                    FullPath = songFile,
                                    isProject = false,
                                    ProjectPath = projFolder,
                                    OrderVal = PlayListOrder.ContainsKey(songFile) ? PlayListOrder[songFile] : 0,
                                    TrackNum = tracknum,                                    
                                };

                                if (tracknum != 0)
                                {
                                    LoadedTracks.Insert(tracknum - 1, t);
                                }
                                else
                                {
                                    LoadedTracks.Add(t);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

            Debug.Print("Project local {0} DONE\n");
        }

        private async Task<bool> WavDirectory(string f)
        {
            // IList<IFile> l = await f.GetFilesAsync();
            foreach (string fl in Directory.GetFiles(f))
            {
                if (MusicUtils.isAudioFormat(fl))
                    return true;
            }

            return false;
        }

        private void DeleteSong_Clicked(object sender, EventArgs e)
        {

        }
    }
}