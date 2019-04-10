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
        private TransportViewModel TransportVMInstance;
        ObservableCollection<MixLocation> MixLocationList = new ObservableCollection<MixLocation>();

        public MainPage()
        {

            try
            {
                InitializeComponent();
            }
            catch(Exception ex)
            {
                Debug.Print(ex.ToString());
            }

            TransportVMInstance = (TransportViewModel)this.BindingContext;
            Projects.ItemsSource = TransportVMInstance.PlayingTracks;

            if (DesignMode.IsDesignModeEnabled)
            {
                // Previewer only code  
                //TrasnportVMInstance.LoadProjects();
                //TrasnportVMInstance.LoadSampleData();
            }
            else
            {

                PersistentData.LoadMixLocations(MixLocationList);

                NavigationPage.SetHasNavigationBar(this, false);
            }
        }

        private async void Add_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AddSongs(MixLocationList));
        }

        private void OnAppearing(object sender, EventArgs e)
        {
            TransportVMInstance.LoadProjects();
        }


        private void DeleteSong_Clicked(object sender, EventArgs e)
        {

        }

        private async void TrackView_Sel(object sender, SelectedItemChangedEventArgs e)
        {
            TransportVMInstance.PlaySongTrack((QueuedTrack)e.SelectedItem);
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

        private void EditPressed_Clicked(object sender, EventArgs e)
        {

        }

        //private void SongNameTapped(object sender, EventArgs e)
        //{
        //    Navigation.PushAsync(new PlayListPicker(TrasnportVMInstance.Tracklist));
        //}
    }
}
