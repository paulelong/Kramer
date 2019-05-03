using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xamarin.Forms;


namespace MyMixes
{
    public partial class MainPage : ContentPage
    {
        private TransportViewModel TransportVMInstance;
        private DateTime start = DateTime.UtcNow;
        private bool startRecorded = false;

        public MainPage()
        {
            try
            {
                InitializeComponent();
                Analytics.TrackEvent("Started");
            }
            catch (Exception ex)
            {
                Crashes.TrackError(ex);
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

        private string CohortTime(TimeSpan timeSpan)
        {
            if(timeSpan.Seconds < 1)
            {
                return "OneSecond";
            } if(timeSpan.Seconds < 2)
            {
                return "OneSeconds";
            }
            if (timeSpan.Seconds < 5)
            {
                return "FiveSeconds";
            }
            if (timeSpan.Seconds < 20)
            {
                return "TwentySeconds";
            }
            else
            {
                return "TooLong";
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
            Navigation.PushAsync(new SongNotes(TransportVMInstance, QueuedTrack.FindQueuedTrack((View)sender)));
        }

        private async void OnAppearing(object sender, EventArgs e)
        {
            await TransportVMInstance.LoadProjects();

            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => { TransportVMInstance.CurrentTrackNumber = PersistentData.LastPlayedSongIndex; });

            if(!startRecorded)
            {
                Dictionary<String, String> properties = new Dictionary<string, string>();

                properties["LoadTime"] = CohortTime(DateTime.UtcNow - start);
                properties["OS"] = Device.RuntimePlatform.ToString();

                Analytics.TrackEvent("Started Completed", properties);

                startRecorded = true;
            }

            if (TransportVMInstance.PlayingTracks.Count <= 0)
            {
                await DisplayAlert(AppResources.NoPlaylistTitle, AppResources.NoPlaylist, AppResources.OK);
            }
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
