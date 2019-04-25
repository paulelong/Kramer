using System;
using System.Diagnostics;
using Xamarin.Forms;


namespace MyMixes
{
    public partial class MainPage : ContentPage
    {
        private TransportViewModel TransportVMInstance;

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
                NavigationPage.SetHasNavigationBar(this, false);
            }

        }

#pragma warning disable AvoidAsyncVoid
        private async void Add_Clicked(object sender, EventArgs e)
        {
            PersistentData.Save();
            await Navigation.PushAsync(new AddSongs(TransportVMInstance));
        }

        private async void OnDisappearing(object sender, EventArgs e)
        {
            await PersistentData.SaveQueuedTracksAsync(TransportVMInstance.PlayingTracks);
        }

#pragma warning restore AvoidAsyncVoid

        private void Notes_Clicked(object sender, EventArgs e)
        {
            PersistentData.Save();
            Navigation.PushAsync(new SongNotes(QueuedTrack.FindQueuedTrack((View)sender)));
        }

        private async void OnAppearing(object sender, EventArgs e)
        {
            await TransportVMInstance.LoadProjects();

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
