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
        ObservableCollection<MixLocation> MixLocationList = new ObservableCollection<MixLocation>();

        public MainPage()
        {

            InitializeComponent();

            TrasnportVMInstance = (TransportViewModel)this.BindingContext;
            Projects.ItemsSource = TrasnportVMInstance.PlayingTracks;

            PersistentData.LoadMixLocations(MixLocationList);

            NavigationPage.SetHasNavigationBar(this, false);
        }

        private async void Add_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddSongs(MixLocationList));
        }

        private async Task SyncProjects()
        {
            Dictionary<string, List<string>> AllSongs = new Dictionary<string, List<string>>();
            
            foreach(MixLocation ml in MixLocationList)
            {
                BusyStatus.Text = ml.Provider.ToString() + " " + ml.Path;

                ProviderInfo pi = await ProviderInfo.GetCloudProviderAsync(ml.Provider);

                if (await pi.CheckAuthenitcation())
                {
                    List<string> l = await pi.GetFoldersAsync(ml.Path);
                    if (l != null)
                    {
                        foreach (string f in l)
                        {
                            BusyStatus.Text = pi.CloudProvider.ToString() + " " + ml.Path + " " + f;
                            var retList = await pi.UpdateProjectAsync(ml.Path, f);
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

        }

    
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


        private void Notes_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new SongNotes());
        }

        //private void SongNameTapped(object sender, EventArgs e)
        //{
        //    Navigation.PushAsync(new PlayListPicker(TrasnportVMInstance.Tracklist));
        //}
    }
}
