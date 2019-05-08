using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.Xaml;

namespace MyMixes
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AddSongs : ContentPage
	{
        //private string selectedFolder = "";
        private Track selectedTrack = null;
        private Track lastPlayingTrack = null;
        private Dictionary<string, int> PlayListOrder = new Dictionary<string, int>();

        //private ObservableCollection<QueuedTrack> SelectedTrackList = new ObservableCollection<QueuedTrack>();
        private ObservableCollection<Track> LoadedTracks = new ObservableCollection<Track>();

        //private ObservableCollection<MixLocation> MixLocationList = null;

        private BusyBarViewModel ppd;

        private TransportViewModel tvm;
        private bool SongPickerPlaying;

        public AddSongs(TransportViewModel tvm)
        {
            InitializeComponent();

            this.tvm = tvm;
            this.BindingContext = this.tvm;

            //SelectedTracks.BindingContext = this;
            SelectedTracks.ItemsSource = tvm.PlayingTracks;
            //SelectedTracks.ItemsSource = SelectedTrackList;

            ppd = new BusyBarViewModel();
            BusyBar.BindingContext = ppd;

            //Projects.BindingContext = this;
            Projects.ItemsSource = LoadedTracks;

            //PersistentData.LoadMixLocations(MixLocationList);

            //PersistentData.LoadQueuedTracks(SelectedTrackList);

            if (DesignMode.IsDesignModeEnabled)
            {
                ppd.BusyText = "Something here";
            }

            //Transport.BindingContext = tvm;
        }

        private void DeleteFolder_Clicked(object sender, EventArgs e)
        {

        }

        private void LocalPlay_Clicked(object sender, EventArgs e)
        {
            Track t = FindTrack((View)sender);

            //if(!SongPickerPlaying)
            //{
            if (!tvm.PlaySongAsync(t.FullPath))
            {
                DisplayAlert(AppResources.SongPlayFailedTitle, AppResources.SongPlayFailed, AppResources.OK);
            }
            else
            {
                SongPickerPlaying = true;
                //t.TrackPlaying = true;
                lastPlayingTrack = t;
            }
            //}
            //else
            //{
            //    if(lastPlayingTrack == t)
            //    {
            //        tvm.StopPlayer();
            //        SongPickerPlaying = false;
            //        lastPlayingTrack.TrackPlaying = false;
            //    }
            //    else
            //    {
            //        lastPlayingTrack.TrackPlaying = false;
            //        tvm.StopPlayer();

            //        if (!tvm.PlaySongAsync(t.FullPath))
            //        {
            //            DisplayAlert(AppResources.SongPlayFailedTitle, AppResources.SongPlayFailed, AppResources.OK);
            //            SongPickerPlaying = false;
            //        }
            //        else
            //        {
            //            SongPickerPlaying = true;
            //            t.TrackPlaying = true;
            //            lastPlayingTrack = t;
            //        }
            //    }
            //}
        }

#pragma warning disable AvoidAsyncVoid
        private async void AddFolder_Clicked(object sender, EventArgs e)
        {
            ProjectPicker pp = new ProjectPicker();
            await Navigation.PushAsync(pp);
        }

        private async void SongOrderClicked(object sender, EventArgs e)
        {
            Track t = FindTrack((View)sender);

            tvm.AddSong(t);
        }

        private void TrackView_Sel(object sender, SelectedItemChangedEventArgs e)
        {
            Debug.Print("Track selected\n");
            Track t = (Track)e.SelectedItem;

            if (!t.isProject)
            {

            }
            else
            {
                if(selectedTrack != t)
                {
                    selectedTrack = t;
                }
                else
                {
                    selectedTrack = null;
                }

                LoadProjects();
            }
        }

        private async void OnAppearing(object sender, EventArgs e)
        {
            tvm.MainPlayMode = false;

            if (!BusyOn(true, true))
            {
                if (PersistentData.mixLocationsChanged)
                {
                    await SyncProjectsAsync();
                    LoadedTracks.Clear();
                }
                LoadProjects();
                BusyOn(false, true);
            }

            if(PersistentData.MixLocationList.Count <= 0)
            {
                await DisplayAlert(AppResources.NoProjectsTitle, AppResources.NoProjects, AppResources.OK);
            } else if(tvm.PlayingTracks.Count <= 0)
            {
                await DisplayAlert(AppResources.MixLocationsNoPlaylistTitle, AppResources.MixLocationsNoPlaylist, AppResources.OK);
            }
        }

        private async void ResyncAllClickedAsync(object sender, EventArgs e)
        {
            // DCR: Maybe we don't sync all the time
            BusyOn(true);
            await SyncProjectsAsync();
            LoadProjects();
            BusyOn(false);

            Projects.EndRefresh();
        }
#pragma warning restore AvoidAsync

        private bool BusyOn(bool TurnOn, bool IsRunning = false)
        {
            if (TurnOn && IsBusy)
            {
                return true;
            }

            IsBusy = TurnOn;

            if (IsRunning)
            {
                ppd.IsRunning = TurnOn;
            }

            ppd.IsVisible = TurnOn;

            MainStack.IsEnabled = !TurnOn;

            return false;
        }

        Track FindTrack(View v)
        {
            Grid g = (Grid)v.Parent;
            Track t = (Track)g.BindingContext;
            return t;
        }

        private void UpdateStatus(string status)
        {
            ppd.BusyText = status;
        }

        private async Task SyncProjectsAsync()
        {
            Dictionary<string, List<string>> AllSongs = new Dictionary<string, List<string>>();

            foreach (MixLocation ml in PersistentData.MixLocationList)
            {
                UpdateStatus(ml.Provider.ToString() + " " + ml.Path);

                ProviderInfo pi = await ProviderInfo.GetCloudProviderAsync(ml.Provider);

                if (await pi.CheckAuthenitcationAsync())
                {
                    List<string> l = await pi.GetFoldersAsync(ml.Path);
                    if (l != null)
                    {
                        foreach (string f in l)
                        {
                            UpdateStatus(ml.Provider.ToString() + " " + ml.Path  + " " + f);
                            var retList = await pi.UpdateProjectAsync(ml.Path, f, UpdateStatus);
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
                    UpdateStatus("Removing " + p);
                    Directory.Delete(p, true);
                }
                else
                {
                    foreach (string s in Directory.GetDirectories(p))
                    {
                        if (AllSongs[p].Contains(Path.GetFileName(s)))
                        {
                            UpdateStatus("Removing " + s);
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
                    UpdateStatus(p + " " + f);
                }
            }

            PersistentData.Save();
        }

        private void LoadProjects()
        {
            for (int i = LoadedTracks.Count - 1; i >= 0; i--)
            {
                if (!LoadedTracks[i].isProject)
                {
                    LoadedTracks.RemoveAt(i);
                }
            }

            LoadProjectFolders();

            if (selectedTrack != null)
            {
                if(selectedTrack.FullPath != null)
                {
                    LoadProjectTracks();
                }
                else
                {
                    LoadSpeicalFolder();
                }
            }
        }

        private void LoadProjectTracks()
        {
            try
            {
                string newProjectPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/" + selectedTrack.Name;
                int projectIndex = LoadedTracks.IndexOf(selectedTrack) + 1;
                int insTrackNumber = 0;

                int songcount = 0;

                foreach (string songFile in Directory.GetFiles(newProjectPath))
                {
                    int tracknum = PersistentData.GetTrackNumber(newProjectPath, Path.GetFileNameWithoutExtension(songFile));

                    var t = new Track
                    {
                        Name = Path.GetFileNameWithoutExtension(songFile),
                        FullPath = songFile,
                        isProject = false,
                        ProjectPath = newProjectPath,
                        OrderVal = PlayListOrder.ContainsKey(songFile) ? PlayListOrder[songFile] : 0,
                        TrackNum = tracknum,
                    };

                    if (tracknum != 0)
                    {
                        LoadedTracks.Insert(projectIndex + tracknum - 1, t);
                    }
                    else
                    {
                        LoadedTracks.Insert(projectIndex + insTrackNumber, t);
                    }

                    insTrackNumber++;
                    songcount++;
                }

                Dictionary<String, String> properties = new Dictionary<string, string>();

                properties["songcount"] = songcount.ToString();

                Analytics.TrackEvent("ExpandTracks", properties);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private void LoadProjectFolders()
        {
            try
            {
                int songcount = 0, foldercount = 0, dupfoldercount = 0 ;

                foreach (string projFolder in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)))
                {
                    if (WavDirectory(projFolder))
                    {
                        Track t = new Track { Name = Path.GetFileName(projFolder), FullPath = projFolder, isProject = true };

                        int i;
                        for(i = 0; i < LoadedTracks.Count; i++)
                        {
                            // if it's already inserted don't insert again.  This is typical if we've added them already.
                            if(t.Name == LoadedTracks[i].Name)
                            {
                                i = -1;
                                dupfoldercount++;
                                break;
                            }
                            if (string.Compare(t.Name, LoadedTracks[i].Name) < 0)
                            {
                                break;
                            }
                        }

                        if(i >= 0)
                        {
                            LoadedTracks.Insert(i, t);
                            foldercount++;
                        }
                    }
                }

                Dictionary<String, String> properties = new Dictionary<string, string>();

                properties["songcount"] = songcount.ToString();
                properties["foldercount"] = foldercount.ToString();
                properties["dupfoldercount"] = dupfoldercount.ToString();

                Analytics.TrackEvent("AddSongs", properties);

                // Add special DOWNLOADs foloder
                //Track t2 = new Track { Name = Path.GetFileName("DOWNLOADS"), FullPath = null, isProject = true };
                //LoadedTracks.Add(t2);

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private void LoadSpeicalFolder()
        {
            string DownloadDirectory = DependencyService.Get<IPlatformDirectories>().GetDownloadDirectory();
            //string DownloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DependencyService.Get<IPlatformDirectories>().GetDownloadDirectory());

            if (DownloadDirectory != null)
            {
                try
                {
                    int songcount = 0, foldercount = 0, dupfoldercount = 0;

                    foreach (string projFolder in Directory.GetDirectories(DownloadDirectory))
                    {
                        if (WavDirectory(projFolder))
                        {
                            Track t = new Track { Name = Path.GetFileName(projFolder), FullPath = projFolder, isProject = true };

                            int i;
                            for (i = 0; i < LoadedTracks.Count; i++)
                            {
                                // if it's already inserted don't insert again.  This is typical if we've added them already.
                                if (t.Name == LoadedTracks[i].Name)
                                {
                                    i = -1;
                                    dupfoldercount++;
                                    break;
                                }
                                if (string.Compare(t.Name, LoadedTracks[i].Name) < 0)
                                {
                                    break;
                                }
                            }

                            if (i >= 0)
                            {
                                LoadedTracks.Insert(i, t);
                                foldercount++;
                            }
                        }
                    }

                    Dictionary<String, String> properties = new Dictionary<string, string>();

                    properties["songcount"] = songcount.ToString();
                    properties["foldercount"] = foldercount.ToString();
                    properties["dupfoldercount"] = dupfoldercount.ToString();

                    Analytics.TrackEvent("AddSongs DOWNLOAD", properties);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            }
        }

        private bool WavDirectory(string f)
        {
            foreach (string fl in Directory.GetFiles(f))
            {
                if (MusicUtils.isAudioFormat(fl))
                    return true;
            }

            return false;
        }

        private async void DeleteSong_Clicked(object sender, EventArgs e)
        {
            QueuedTrack t = QueuedTrack.FindQueuedTrack((View)sender);

            tvm.RemoveSong(t);
        }

        private void SongDownPosition_Clicked(object sender, EventArgs e)
        {
            QueuedTrack t = QueuedTrack.FindQueuedTrack((View)sender);

            tvm.MoveSongUp(t);
        }

        private void ResyncProjectClickedAsync(object sender, EventArgs e)
        {

        }

        private void OnDisappearing(object sender, EventArgs e)
        {
            if(SongPickerPlaying)
            {
                tvm.StopPlayer();
                tvm.NowPlaying = null;
                lastPlayingTrack.TrackPlaying = false;
            }
        }
    }
}