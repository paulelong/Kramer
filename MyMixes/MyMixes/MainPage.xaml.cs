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
using CloudStorage;
using System.Collections.ObjectModel;

namespace MyMixes
{
    public partial class MainPage : ContentPage
    {
//        private List<View> Takes = new List<View>();
        //private string selectedFolder = "";
        //private ISimpleAudioPlayer player;

        private TransportViewModel TrasnportVMInstance = new TransportViewModel();
        //private SelectedTracksVM SelectedTracks = new SelectedTracksVM();
        //private List<string> tl = new List<string>();
        //private List<Track> SelectedTracks = new List<Track>();
        //private int currentOrder;

        //private Dictionary<string, int> PlayListOrder = new Dictionary<string, int>();

        //private ObservableCollection<Track> Playlist = new ObservableCollection<Track>();

        public MainPage()
        {

            InitializeComponent();

            //this.BindingContext = TrasnportVMInstance;
            //Projects.ItemsSource = TrasnportVMInstance.PlayingTracks;

            // seed with static data for now REMOVE
            ProviderInfo piA = new ProviderInfo();

            NavigationPage.SetHasNavigationBar(this, false);

            //ViewModel.CurrentSel = "Add songs to create playlist";

            TrasnportVMInstance.ConfigureTransport();
        }

        //private async void Player_PlaybackEnded(object sender, EventArgs e)
        //{
        //    Device.BeginInvokeOnMainThread(async () =>
        //    {
        //        if (currentSong > ViewModel.SongsQueued)
        //        {
        //            currentSong = 1;
        //            SetSongIndex(currentSong - 1);
        //            if (ViewModel.isLooping)
        //            {
        //                await PlayCurrentSong();
        //            }
        //            else
        //            {
        //                //PlaySongButton.Image = "PlayBt.png";
        //                isSongPlaying = false;
        //            }
        //        }
        //        else
        //        {
        //            currentSong++;
        //            SetSongIndex(currentSong - 1);
        //            await PlayCurrentSong();
        //        }
        //    });
        //}



        private async void Add_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddSongs());


            //await Navigation.PushAsync(new FolderPicker());

        }

        private async void LocalPlay_Clicked(object sender, EventArgs e)
        {
            //Track t = FindTrack((View)sender);

            //if(PlayListOrder.ContainsKey(t.FullPath))
            //{
            //    currentSong = PlayListOrder[t.FullPath];
            //}
            //else
            //{
            //    if(isSongPlaying)
            //    {
            //        foreach (string key in PlayListOrder.Keys.ToArray())
            //        {
            //            if (PlayListOrder.ContainsKey(key) && PlayListOrder[key] >= currentSong)
            //            {
            //                PlayListOrder[key]++;
            //            }
            //        }

            //        PlayListOrder[t.FullPath] = currentSong;

            //        ViewModel.Tracklist.Insert(currentSong, t);
            //        currentSong++;
            //    }
            //    else
            //    {
            //        foreach (string key in PlayListOrder.Keys.ToArray())
            //        {
            //            if (PlayListOrder.ContainsKey(key) && PlayListOrder[key] > currentSong)
            //            {
            //                PlayListOrder[key]++;
            //            }
            //        }

            //        PlayListOrder[t.FullPath] = currentSong;

            //        if(ViewModel.Tracklist.Count <= 0)
            //        {
            //            ViewModel.Tracklist.Add(t);
            //        }
            //        else
            //        {
            //            ViewModel.Tracklist.Insert(currentSong - 1, t);
            //        }
            //    }
            //}

            //this.BindingContext = null;
            //this.BindingContext = ViewModel;

            //SetSongIndex(currentSong - 1);

            //await PlayCurrentSong();

            //if (playingSong != t.FullPath)
            //if (!player.IsPlaying)
            //{
            //    PlayListOrder.Add(t);
            //    currentSong++;

            //    string path = Path.GetDirectoryName(t.FullPath);
            //    string filename = Path.GetFileName(t.FullPath);

            //    Debug.Print("playing {0} {1}\n", filename, path);

            //    IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(path); ;
            //    IFile source = await folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            //    player.Stop();
            //    using (Stream s = await source.OpenAsync(PCLStorage.FileAccess.Read))
            //    {
            //        if (player.Load(s))
            //        {
            //            playingSong = t.FullPath;
            //            //CurrentSong.Text = filename;
            //            PlayButton.Image = "PauseBt.png";
            //            player.Play();
            //            isSongPlaying = true;
            //        }
            //        else
            //        {
            //            await DisplayAlert("Error openning track ", t.FullPath, "OK");
            //        }
            //    }
            //}
            //else
            //{
            //    //playingSong = "";
            //    isSongPlaying = false;

            //    player.Stop();
            //}
        }

        private async Task SyncProjects()
        {
            Dictionary<string, List<string>> AllSongs = new Dictionary<string, List<string>>();
            
            //List<MixLocation> ml_list = await MixLocation.GetMixLocationsAsync();
            foreach(ProviderInfo pi in ProviderInfo.Providers)
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
                            if(AllSongs.ContainsKey(f))
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
            foreach(string p in Directory.GetDirectories(rootPath))
            {
                if (!AllSongs.ContainsKey(Path.GetFileName(p)))
                {
                    Debug.Print("Remove dir " + p + "\n");
                    BusyStatus.Text = "Removing " + p;
                    Directory.Delete(p, true);
                }
                else
                {
                    foreach(string s in Directory.GetDirectories(p))
                    {
                        if(AllSongs[p].Contains(Path.GetFileName(s)))
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
                foreach(string f in AllSongs[p])
                {
                    BusyStatus.Text = p + " " + f;
                }
            }

            PersistentData.Save();
        }



        //private async Task<bool> WavDirectory(string f)
        //{
        //   // IList<IFile> l = await f.GetFilesAsync();
        //    foreach (string fl in Directory.GetFiles(f))
        //    {
        //        if (MusicUtils.isAudioFormat(fl))
        //            return true;
        //    }

        //    return false;
        //}

        //private async void GlobalPlaySong_Clicked(object sender, EventArgs e)
        //{
        //    if (ViewModel.SongsQueued > 0)
        //    //if (currentSong > = 0 !string.IsNullOrEmpty(playingSong))
        //    {
        //        if(player.CurrentPosition > 0)
        //        {
        //            if (isSongPlaying)
        //            {
        //                isSongPlaying = false;
        //                player.Pause();
        //                //PlaySongButton.Image = "PlayBt.png";
        //            }
        //            else
        //            {
        //                isSongPlaying = true;
        //                player.Play();
        //                //PlaySongButton.Image = "PauseBt.png";
        //            }
        //        }
        //        else
        //        {
        //            isSongPlaying = true;
        //            //PlaySongButton.Image = "PauseBt.png";

        //            await PlayCurrentSong();
        //        }
        //    }
        //}



        //private void DeleteFolder_Clicked(object sender, EventArgs e)
        //{

        //}

        //private async Task Delete_Clicked(object sender, EventArgs e)
        //{
        //    Track t = (Track)Projects.SelectedItem;
        //    ProviderInfo pi = ProviderInfo.FindProvider(t.ProjectPath);

        //    if(pi != null)
        //    {
        //        bool deleted = await pi.DeleteTake(t.Name);
        //        if (deleted)
        //        {
        //            bool localDeleted = await DeleteFile(t.FullPath);
        //            if (!localDeleted)
        //            {
        //                await DisplayAlert("Error Deleting", "Local project file could not be deleted.", "OK");
        //            }
        //        }
        //        else
        //        {
        //            await DisplayAlert("Error Deleting", "Remote project file could not be deleted.  Aborting deletion", "OK");
        //        }
        //    }
        //    else
        //    {
        //        await DisplayAlert("Error Deleting", "No entry for local project, is installation corrupt?", "OK");
        //    }
        //}

        //private async Task<bool> DeleteFile(string fullPath)
        //{
        //    try
        //    {
        //        IFolder folder = FileSystem.Current.LocalStorage;
        //        IFile file = await folder.CreateFileAsync(fullPath, CreationCollisionOption.OpenIfExists);

        //        await file.DeleteAsync();
        //    }
        //    catch(Exception ex)
        //    {
        //        await DisplayAlert("Exception Delete File", ex.Message, "OK");
        //        return false;
        //    }

        //    return true;
        //}

        private void OnAppearing(object sender, EventArgs e)
        {
            BusyOn(true);
            TrasnportVMInstance.LoadProjects();
            BusyOn(false);
        }

        private async void ResyncAllClickedAsync(object sender, EventArgs e)
        {
            // DCR: Maybe we don't sync all the time
            BusyOn(true);
            await SyncProjects();
            TrasnportVMInstance.LoadProjects();
            BusyOn(false);
        }

        Track FindTrack(View v)
        {
            Grid g = (Grid)v.Parent;
            Track t = (Track)g.BindingContext;
            return t;
        }

        private void BusyOn(bool TurnOn)
        {
            BusyGrid.IsVisible = TurnOn;
            BusySignal.IsRunning = TurnOn;
            MainStack.IsEnabled = !TurnOn;
        }

        private void DeleteSong_Clicked(object sender, EventArgs e)
        {

        }

        private async void TrackView_Sel(object sender, SelectedItemChangedEventArgs e)
        {
            //Track t = (Track)e.SelectedItem;

            //if (!t.isProject)
            //{

            //}
            //else
            //{
            //    selectedFolder = t.Name;

            //    double scrollY = TrackScroll.ScrollY;

            //    await LoadProjects();

            //    await TrackScroll.ScrollToAsync(0, scrollY, false);
            //}

        }

        //private async void SongOrderClicked(object sender, EventArgs e)
        //{
        //    bool songStopped = false;

        //    Track t = FindTrack((View)sender);

        //    if (!PlayListOrder.ContainsKey(t.FullPath) || PlayListOrder[t.FullPath] == 0)
        //    {
        //        PlayListOrder[t.FullPath] = ++currentOrder;
        //        t.OrderButtonText = "-";
        //        t.ReadyToAdd = false;
                
        //        //SelectedTracks.Add(t);
        //        TrasnportVMInstance.Tracklist.Add(t);

        //        this.BindingContext = null;
        //        this.BindingContext = TrasnportVMInstance;

        //        //SongList.Items.Add(t.Name);
        //        //t.OrderVal = currentOrder++;
        //    }
        //    else
        //    {
        //        if (isSongPlaying && currentSong == t.OrderVal)
        //        {
        //            player.Stop();
        //            songStopped = true;
        //        }

        //        currentOrder--;

        //        foreach (string key in PlayListOrder.Keys.ToArray())
        //        {
        //            if (PlayListOrder.ContainsKey(key) && PlayListOrder[key] > t.OrderVal)
        //            {
        //                PlayListOrder[key]--;
        //            }
        //        }

        //        foreach (Track ct in (List<Track>)Projects.ItemsSource)
        //        {
        //            if (ct.OrderVal > t.OrderVal)
        //            {
        //                ct.OrderVal--;
        //            }
        //        }
        //        PlayListOrder[t.FullPath] = 0;
        //        //SongList.Items.Remove(t.Name);
        //        //SelectedTracks.Remove(t);
        //        TrasnportVMInstance.Tracklist.Remove(t);
        //        this.BindingContext = null;
        //        this.BindingContext = TrasnportVMInstance;

        //        t.OrderButtonText = "+";
        //        t.ReadyToAdd = true ;

        //        if ((currentSong - 1) == TrasnportVMInstance.CurrentTrackNumer)
        //        {
        //            if (currentSong >= TrasnportVMInstance.SongsQueued)
        //                currentSong--;
        //        }
        //    }

        //    t.OrderVal = PlayListOrder[t.FullPath];

        //    //var kvp = PlayListOrder.FirstOrDefault((x) => x.Value == currentSong);

        //    SetSongIndex(currentSong - 1);

        //    if (isSongPlaying && TrasnportVMInstance.SongsQueued > 0 && songStopped)
        //    {
        //        await PlayCurrentSong();
        //    }

        //    SetCurrentSong();
        //}

        //private async void Prev_Clicked(object sender, EventArgs e)
        //{
        //    if (currentSong <= 1)
        //    {
        //        currentSong = TrasnportVMInstance.SongsQueued;
        //    }
        //    else
        //    {
        //        currentSong--;
        //    }

        //    SetSongIndex(currentSong - 1);
        //    await PlayCurrentSong();

        //}

        //private async void NextSong_Clicked(object sender, EventArgs e)
        //{
        //    if (currentSong >= TrasnportVMInstance.SongsQueued)
        //    {
        //        currentSong = 1;
        //    }
        //    else
        //    {
        //        currentSong++;
        //    }

        //    SetSongIndex(currentSong - 1);
        //    await PlayCurrentSong();
        //}

        private async void RemoveSong_Clicked(object sender, EventArgs e)
        {
            //Track t = TrasnportVMInstance.CurrentTrack;

            //if (t == null)
            //    return;

            //if (player.CurrentPosition > 0 && currentSong == t.OrderVal)
            //{
            //    player.Stop();
            //}

            //foreach (string key in PlayListOrder.Keys.ToArray())
            //{
            //    if (PlayListOrder.ContainsKey(key) && PlayListOrder[key] >= t.OrderVal)
            //    {
            //        PlayListOrder[key]--;
            //    }
            //}

            //foreach (Track ct in (List<Track>)Projects.ItemsSource)
            //{
            //    if (ct.OrderVal >= t.OrderVal && ct.OrderVal > 0)
            //    {
            //        ct.OrderVal--;
            //        if(ct.OrderVal == 0)
            //        {
            //            ct.OrderButtonText = "+";
            //            ct.ReadyToAdd = true;
            //        }
            //    }
            //}
            //PlayListOrder[t.FullPath] = 0;

            //TrasnportVMInstance.Tracklist.Remove(t);
            //this.BindingContext = null;
            //this.BindingContext = TrasnportVMInstance;

            //SetSongIndex(t.OrderVal);
            //currentOrder--;

            //if(isSongPlaying)
            //{
            //    if(TrasnportVMInstance.SongsQueued > 0)
            //    {
            //        await PlayCurrentSong();
            //    }
            //    else
            //    {
            //        isSongPlaying = false;
            //        //PlaySongButton.Image = "PlayBt.png";
            //    }
            //}
        }


        //private async void ResyncSongClickedAsync(object sender, EventArgs e)
        //{
        //    Track t = (Track)SongList.SelectedItem;

        //    ProjectMapping pm = PersistentData.ProjectMappings.Find((x) => x.project == t.Name);

        //    if (pm != null)
        //    {
                
        //        ICloudStore pi = await GetCloudProviderAsync(pm.provider);
        //        if(pi != null)
        //        {
        //            //await pi.UpdateProjectAsync(t.FullPath);
        //        }
        //    }
        //}

        private void Notes_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new SongNotes());
        }

        //private void ResyncProjectClickedAsync(object sender, EventArgs e)
        //{

        //}

        private void SongNameTapped(object sender, EventArgs e)
        {
            Navigation.PushAsync(new PlayListPicker(TrasnportVMInstance.Tracklist));
        }
    }
}
