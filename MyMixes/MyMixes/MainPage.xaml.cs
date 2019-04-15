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

            if (DesignMode.IsDesignModeEnabled)
            {
            }
            else
            {
                PersistentData.LoadMixLocations(MixLocationList);

                NavigationPage.SetHasNavigationBar(this, false);
            }

        }

#pragma warning disable AvoidAsyncVoid
        private async void Add_Clicked(object sender, EventArgs e)
        {
            PersistentData.Save();
            await Navigation.PushAsync(new AddSongs(MixLocationList, TransportVMInstance));
        }
#pragma warning restore AvoidAsyncVoid

        private void Notes_Clicked(object sender, EventArgs e)
        {
            PersistentData.Save();
            Navigation.PushAsync(new SongNotes(QueuedTrack.FindQueuedTrack((View)sender)));
        }

        private void OnAppearing(object sender, EventArgs e)
        {
            TransportVMInstance.LoadProjects();

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => { TransportVMInstance.CurrentTrackNumber = PersistentData.LastPlayedSongIndex; });
        }

        private void DeleteSong_Clicked(object sender, EventArgs e)
        {
            TransportVMInstance.RemoveSong(QueuedTrack.FindQueuedTrack((View)sender));
        }

        private void DownPosition_Clicked(object sender, EventArgs e)
        {
            QueuedTrack t = QueuedTrack.FindQueuedTrack((View)sender);
            
            TransportVMInstance.MoveSongUp(t);
        }
    }
}
