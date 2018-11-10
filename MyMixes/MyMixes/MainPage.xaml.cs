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

namespace MyMixes
{
    public partial class MainPage : ContentPage
    {
//        private List<View> Takes = new List<View>();
        private string selectedFolder = "";
        private ISimpleAudioPlayer player;
        private string playingSong;
        private bool isSongPlaying;

        private MainVM ViewModel = new MainVM();
        private SelectedTracksVM SelectedTracks = new SelectedTracksVM();
        private List<string> tl = new List<string>();
        //private List<Track> SelectedTracks = new List<Track>();
        private int currentOrder;
        private int currentSong = 1;

        private Dictionary<string, int> PlayListOrder = new Dictionary<string, int>();

        public MainPage()
        {
            InitializeComponent();

            player = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            player.PlaybackEnded += Player_PlaybackEnded;

            this.BindingContext = ViewModel;

            // seed with static data for now REMOVE
            ProviderInfo piA = new ProviderInfo();

            piA.RootPath = "Mixes";
            piA.CloudProvider = CloudProviders.OneDrive;
            PersistentData.MixLocations.Add(piA);

            piA = new ProviderInfo();

            piA.RootPath = "Mixes";
            piA.CloudProvider = CloudProviders.GoogleDrive;
            PersistentData.MixLocations.Add(piA);

        }

        private async void Player_PlaybackEnded(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
               {
                   if(currentSong > ViewModel.SongsQueued)
                   {
                       currentSong = 1;
                       SongList.SelectedIndex = currentSong - 1;
                       if (ViewModel.isLooping)
                       {
                           await PlayCurrentSong();
                       }
                       else
                       {
                           PlayButton.Image = "PlayBt.png";
                           isSongPlaying = false;
                       }
                   }
                   else
                   {
                       currentSong++;
                       SongList.SelectedIndex = currentSong - 1;
                       await PlayCurrentSong();
                   }
               });
        }

        private async void Add_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new FolderPicker());

            var ProviderChoices = Enum.GetNames(typeof(CloudStorage.CloudProviders));
        
            var action = await DisplayActionSheet("Which cloud platform?", "Cancel", null, ProviderChoices);

            // BUGBUG: Not finished
            ProviderInfo pi = new ProviderInfo();
            if(action != "Cancel")
            {
                ICloudStore cs = await GetCloudProvider((CloudStorage.CloudProviders)Enum.Parse(typeof(CloudStorage.CloudProviders), action));

                if (cs != null)
                {
                    //if (await CloudStoreUtils.Authenticate(cs))
                    //{
                    //    //CrossFilePicker.Current.PickFile();
                    //}
                }
            }
        }

        private async void LocalPlay_Clicked(object sender, EventArgs e)
        {
            Track t = FindTrack((View)sender);

            if(PlayListOrder.ContainsKey(t.FullPath))
            {
                currentSong = PlayListOrder[t.FullPath];
            }
            else
            {
                if(isSongPlaying)
                {
                    foreach (string key in PlayListOrder.Keys.ToArray())
                    {
                        if (PlayListOrder.ContainsKey(key) && PlayListOrder[key] >= currentSong)
                        {
                            PlayListOrder[key]++;
                        }
                    }

                    PlayListOrder[t.FullPath] = currentSong;

                    ViewModel.Tracklist.Insert(currentSong, t);
                    currentSong++;
                }
                else
                {
                    foreach (string key in PlayListOrder.Keys.ToArray())
                    {
                        if (PlayListOrder.ContainsKey(key) && PlayListOrder[key] > currentSong)
                        {
                            PlayListOrder[key]++;
                        }
                    }

                    PlayListOrder[t.FullPath] = currentSong;

                    if(ViewModel.Tracklist.Count <= 0)
                    {
                        ViewModel.Tracklist.Add(t);
                    }
                    else
                    {
                        ViewModel.Tracklist.Insert(currentSong - 1, t);
                    }
                }
            }

            this.BindingContext = null;
            this.BindingContext = ViewModel;

            SongList.SelectedIndex = currentSong - 1;

            await PlayCurrentSong();

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

            foreach(ProviderInfo pi in PersistentData.MixLocations)
            {
                if (await pi.CheckAuthenitcation())
                {
                    List<string> l = await pi.GetFoldersAsync();
                    if (l != null)
                    {
                        foreach (string f in l)
                        {
                            var retList = await pi.UpdateProjectAsync(f);
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
                    Directory.Delete(p, true);
                }
                else
                {
                    foreach(string s in Directory.GetDirectories(p))
                    {
                        if(AllSongs[p].Contains(Path.GetFileName(s)))
                        {
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
                        var p = new Track { Name = f.Name, FullPath = f.Path, isProject = true};
                        tracks.Add(p);

                        if (f.Name == selectedFolder)
                        {
                            IList<IFile> fileList = await f.GetFilesAsync();
                            foreach (IFile fl in fileList)
                            {
                                var t = new Track
                                {
                                    Name = fl.Name,
                                    FullPath = fl.Path,
                                    isProject = false,
                                    OrderVal = PlayListOrder.ContainsKey(fl.Path) ? PlayListOrder[fl.Path] : 0
                                };
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
                if (MusicUtils.isAudioFormat(fl.Name))
                    return true;
            }

            return false;
        }

        private async void GlobalPlaySong_Clicked(object sender, EventArgs e)
        {
            if (ViewModel.SongsQueued > 0)
            //if (currentSong > = 0 !string.IsNullOrEmpty(playingSong))
            {
                if(player.CurrentPosition > 0)
                {
                    if (isSongPlaying)
                    {
                        isSongPlaying = false;
                        player.Pause();
                        PlayButton.Image = "PlayBt.png";
                    }
                    else
                    {
                        isSongPlaying = true;
                        player.Play();
                        PlayButton.Image = "PauseBt.png";
                    }
                }
                else
                {
                    isSongPlaying = true;
                    PlayButton.Image = "PauseBt.png";

                    await PlayCurrentSong();
                }
            }
        }

        private async Task PlayCurrentSong()
        {
            Track t = (Track)SongList.SelectedItem;

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
                    //CurrentSong.Text = filename;
                    PlayButton.Image = "PauseBt.png";
                    player.Play();
                    isSongPlaying = true;
                }
                else
                {
                    await DisplayAlert("Error openning track ", t.FullPath, "OK");
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
                bool deleted = await pi.DeleteTake(pm.project + "/" + t.Name);
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
            BusyOn(true);
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

        Track FindTrack(View v)
        {
            Grid g = (Grid)v.Parent;

            List<Track> tl = (List<Track>)Projects.ItemsSource;

            Label l;

            if (g.Children[1] is Label)
            {
               l = (Label)(g.Children[1]);
            }
            else
            {
                l = (Label)(g.Children[1]);
            }

            Track t = tl.Find((x) => x.Name == l.Text);
            return t;
        }

        private void BusyOn(bool TurnOn)
        {
            BusySignal.IsVisible = TurnOn;
            BusySignal.IsRunning = TurnOn;
            MainStack.IsEnabled = !TurnOn;
        }

        private void DeleteSong_Clicked(object sender, EventArgs e)
        {

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

                double scrollY = TrackScroll.ScrollY;

                await LoadProjects();

                await TrackScroll.ScrollToAsync(0, scrollY, false);
            }

        }

        private async void SongOrderClicked(object sender, EventArgs e)
        {
            bool songStopped = false;

            Track t = FindTrack((View)sender);

            if (!PlayListOrder.ContainsKey(t.FullPath) || PlayListOrder[t.FullPath] == 0)
            {
                PlayListOrder[t.FullPath] = ++currentOrder;
                t.OrderButtonText = "-";
                t.ReadyToAdd = false;
                
                //SelectedTracks.Add(t);
                ViewModel.Tracklist.Add(t);

                this.BindingContext = null;
                this.BindingContext = ViewModel;

                //SongList.Items.Add(t.Name);
                //t.OrderVal = currentOrder++;
            }
            else
            {
                if (isSongPlaying && currentSong == t.OrderVal)
                {
                    player.Stop();
                    songStopped = true;
                }

                currentOrder--;

                foreach (string key in PlayListOrder.Keys.ToArray())
                {
                    if (PlayListOrder.ContainsKey(key) && PlayListOrder[key] > t.OrderVal)
                    {
                        PlayListOrder[key]--;
                    }
                }

                foreach (Track ct in (List<Track>)Projects.ItemsSource)
                {
                    if (ct.OrderVal > t.OrderVal)
                    {
                        ct.OrderVal--;
                    }
                }
                PlayListOrder[t.FullPath] = 0;
                //SongList.Items.Remove(t.Name);
                //SelectedTracks.Remove(t);
                ViewModel.Tracklist.Remove(t);
                this.BindingContext = null;
                this.BindingContext = ViewModel;

                t.OrderButtonText = "+";
                t.ReadyToAdd = true ;

                if ((currentSong - 1) == SongList.SelectedIndex)
                {
                    if (currentSong >= ViewModel.SongsQueued)
                        currentSong--;
                }
            }

            t.OrderVal = PlayListOrder[t.FullPath];

            //var kvp = PlayListOrder.FirstOrDefault((x) => x.Value == currentSong);

            SongList.SelectedIndex = currentSong - 1;

            if (isSongPlaying && ViewModel.SongsQueued > 0 && songStopped)
            {
                await PlayCurrentSong();
            }
        }

        private async void Prev_Clicked(object sender, EventArgs e)
        {
            if (currentSong <= 1)
            {
                currentSong = ViewModel.SongsQueued;
            }
            else
            {
                currentSong--;
            }

            SongList.SelectedIndex = currentSong - 1;
            await PlayCurrentSong();

        }

        private async void NextSong_Clicked(object sender, EventArgs e)
        {
            if (currentSong >= ViewModel.SongsQueued)
            {
                currentSong = 1;
            }
            else
            {
                currentSong++;
            }

            SongList.SelectedIndex = currentSong - 1;
            await PlayCurrentSong();
        }

        private async void RemoveSong_Clicked(object sender, EventArgs e)
        {
            Track t = (Track)SongList.SelectedItem;

            if (t == null)
                return;

            if (player.CurrentPosition > 0 && currentSong == t.OrderVal)
            {
                player.Stop();
            }

            foreach (string key in PlayListOrder.Keys.ToArray())
            {
                if (PlayListOrder.ContainsKey(key) && PlayListOrder[key] >= t.OrderVal)
                {
                    PlayListOrder[key]--;
                }
            }

            foreach (Track ct in (List<Track>)Projects.ItemsSource)
            {
                if (ct.OrderVal >= t.OrderVal && ct.OrderVal > 0)
                {
                    ct.OrderVal--;
                    if(ct.OrderVal == 0)
                    {
                        ct.OrderButtonText = "+";
                        ct.ReadyToAdd = true;
                    }
                }
            }
            PlayListOrder[t.FullPath] = 0;

            ViewModel.Tracklist.Remove(t);
            this.BindingContext = null;
            this.BindingContext = ViewModel;

            SongList.SelectedIndex = t.OrderVal;
            currentOrder--;

            if(isSongPlaying)
            {
                if(ViewModel.SongsQueued > 0)
                {
                    await PlayCurrentSong();
                }
                else
                {
                    isSongPlaying = false;
                    PlayButton.Image = "PlayBt.png";
                }
            }
        }

        private async void ResyncSongClickedAsync(object sender, EventArgs e)
        {
            Track t = (Track)SongList.SelectedItem;

            ProjectMapping pm = PersistentData.ProjectMappings.Find((x) => x.project == t.Name);

            if (pm != null)
            {
                
                ICloudStore pi = await GetCloudProvider(pm.provider);
                if(pi != null)
                {
                    //await pi.UpdateProjectAsync(t.FullPath);
                }
            }
        }

        private void Notes_Clicked(object sender, EventArgs e)
        {

        }
    }
}
