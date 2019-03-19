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

        private TransportViewModel TrasnportVMInstance;
        //private SelectedTracksVM SelectedTracks = new SelectedTracksVM();
        //private List<string> tl = new List<string>();
        //private List<Track> SelectedTracks = new List<Track>();

        public MainPage()
        {

            InitializeComponent();

            TrasnportVMInstance = (TransportViewModel)this.BindingContext;
            Projects.ItemsSource = TrasnportVMInstance.PlayingTracks;

            // seed with static data for now REMOVE
            //ProviderInfo piA = new ProviderInfo();

            NavigationPage.SetHasNavigationBar(this, false);
        }

        private async void Add_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddSongs());
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
