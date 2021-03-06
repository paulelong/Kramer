﻿using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    [DesignTimeVisible(true)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AddSongs : ContentPage
	{
        private Track selectedTrack = null;
        private Track lastPlayingTrack = null;
        private Dictionary<string, int> PlayListOrder = new Dictionary<string, int>();

        private ObservableCollection<Track> LoadedTracks = new ObservableCollection<Track>();

        private BusyBarViewModel ppd;

        private TransportViewModel tvm;
        private bool SongPickerPlaying;

        public AddSongs(TransportViewModel tvm)
        {
            InitializeComponent();

            this.tvm = tvm;
            this.BindingContext = this.tvm;

            SelectedTracks.ItemsSource = tvm.Playlist;

            ppd = new BusyBarViewModel();
            BusyBar.BindingContext = ppd;

            Projects.ItemsSource = LoadedTracks;

            if (DesignMode.IsDesignModeEnabled)
            {
                ppd.BusyText = "Something here";
            }
        }

        private void LocalPlay_Clicked(object sender, EventArgs e)
        {
            Track t = FindTrack((View)sender);

            if (!tvm.PlaySongAsync(t.FullPath))
            {
                DisplayAlert(AppResources.SongPlayFailedTitle, AppResources.SongPlayFailed, AppResources.OK);
            }
            else
            {
                SongPickerPlaying = true;
                lastPlayingTrack = t;
            }
        }

#pragma warning disable AvoidAsyncVoid
        private async void DeleteFolder_Clicked(object sender, EventArgs e)
        {
            bool result = await DisplayAlert(AppResources.RemoveFolderTitle, AppResources.RemoveFolder, AppResources.Continue, AppResources.Cancel);
            if (result)
            {
                Track t = FindTrack((View)sender);

                if(t.CloudProvider != CloudStorage.CloudProviders.NULL)
                {
                    ProviderInfo pi = await ProviderInfo.GetCloudProviderAsync(t.CloudProvider);

                    if (await pi.CheckAuthenitcationAsync())
                    {
                        bool removeWorked = await pi.RemoveFolder(t.CloudRoot, t.Project, UpdateStatus);
                        if(removeWorked)
                        {
                            // Are we goint to remove a song that is playing?
                            if(this.tvm.SelectedSong.Project == t.Project)
                            {
                                this.tvm.StopPlayer();
                                this.tvm.ResetPlayer();
                            }

                            string projectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), t.Project);
                            Directory.Delete(projectPath, true);

                            t.CloudProvider = CloudStorage.CloudProviders.NULL;
                            t.CloudRoot = null;

                            if(selectedTrack?.Project == t.Project)
                            {
                                selectedTrack = null;
                            }

                            // Remove folder from current list
                            for (int i = LoadedTracks.Count - 1; i >= 0; i--)
                            {
                                if (LoadedTracks[i].Project == t.Project)
                                {
                                    LoadedTracks.RemoveAt(i);
                                }
                            }

                            // If part of playlist, remove from there
                            for (int i = this.tvm.Playlist.Count - 1; i >= 0; i--)
                            {
                                if (t.Project == this.tvm.Playlist[i].Project)
                                {
                                    this.tvm.Playlist.RemoveAt(i);
                                }
                            }
                        }
                        else
                        {
                            await DisplayAlert(AppResources.RemoveFolderRemoteFailedTitle, AppResources.RemoveFolderRemoteFailed, AppResources.OK);
                        }
                    }
                }
                else
                {
                    Analytics.TrackEvent("Cloud Provider for folder invalid");

                    await DisplayAlert("Cloud Provider incorrect", "The cloud provider saved in local storage is missing or incorrect.  This is not an expected error, contact the developer.", "OK");
                }
            }
        }

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
            } else if(tvm.Playlist.Count <= 0)
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
            PersistentData.Save();
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
                        LastModifiedDate = File.GetLastWriteTime(songFile),
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
                        //string newProjectPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/" + projFolder;
                        
                        Track t = new Track { Name = Path.GetFileName(projFolder), FullPath = projFolder, isProject = true, ProjectPath = projFolder, LastModifiedDate = Directory.GetLastWriteTime(projFolder) };

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

        private void DeleteSong_Clicked(object sender, EventArgs e)
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

            PersistentData.Save();
        }
    }
}