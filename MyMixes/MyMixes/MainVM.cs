using PCLStorage;
using Plugin.SimpleAudioPlayer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace MyMixes
{
    partial class TransportViewModel : INotifyPropertyChanged
    {
        // Transport Player control
        public ICommand PlayCommand { get; private set; }
        public ObservableCollection<QueuedTrack> PlayingTracks = new ObservableCollection<QueuedTrack>();
        public ISimpleAudioPlayer player { get; set; }

        private string playingSong;
        private bool isSongPlaying;
        private int currentSong = 0;


        public void ConfigureTransport()
        {
            player = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            player.PlaybackEnded += Player_PlaybackEnded;

            //Projects.ItemsSource = ViewModel.PlayingTracks;

            PlayCommand = new Command(PlaySong);

            currentSong = PersistentData.LastPlayedSongIndex;
        }

        private async void Player_PlaybackEnded(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                currentSong++;
                if (currentSong > SongsQueued)
                {
                    currentSong = 0;
                    if (isLooping)
                    {
                        await PlayCurrentSong();
                    }
                    else
                    {
                        isSongPlaying = false;
                    }
                }
                else
                {
                    await PlayCurrentSong();
                }
            });
        }

        public void SetCurrentSong(Track t)
        {
            //Track t = (Track)Projects.SelectedItem;
            CurrentProject = Path.GetFileName(t.ProjectPath);

            CurrentSel = t.Name;
        }

        private async void PlaySong()
        {
            if (SongsQueued > 0)
            //if (currentSong > = 0 !string.IsNullOrEmpty(playingSong))
            {
                if (player.CurrentPosition > 0)
                {
                    if (isSongPlaying)
                    {
                        isSongPlaying = false;
                        player.Pause();
                        //PlaySongButton.Image = "PlayBt.png";
                    }
                    else
                    {
                        isSongPlaying = true;
                        player.Play();
                        //PlaySongButton.Image = "PauseBt.png";
                    }
                }
                else
                {
                    isSongPlaying = true;
                    //PlaySongButton.Image = "PauseBt.png";

                    await PlayCurrentSong();
                }
            }
        }

        private async Task PlayCurrentSong()
        {
            //Track t = CurrentTrack;
            //ViewModel.CurrentSel = t.Name;
            //ViewModel.CurrentProject = t.Project;

            string path = Path.GetDirectoryName(PlayingTracks[CurrentTrackNumer].FullPath);
            string filename = Path.GetFileName(PlayingTracks[CurrentTrackNumer].FullPath);

            Debug.Print("playing {0} {1}\n", filename, path);

            IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(path); ;
            IFile source = await folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            player.Stop();
            using (Stream s = await source.OpenAsync(PCLStorage.FileAccess.Read))
            {
                if (player.Load(s))
                {
                    //playingSong = t.FullPath;
                    //CurrentSong.Text = filename;
                    //PlaySongButton.Image = "PauseBt.png";
                    player.Play();
                    isSongPlaying = true;
                }
                else
                {
                    //await DisplayAlert("Error openning track ", t.FullPath, "OK");
                }
            }

            //SetCurrentSong(t);
        }

        public void LoadProjects()
        {
            PersistentData.LoadQueuedTracks(PlayingTracks);

            //var p = new QueuedTrack { Name = "Some Song", Project = "ProjectName" };
            //tracks.Add(p);
            //var q = new QueuedTrack { Name = "Big guts", Project = "Pencil" };
            //tracks.Add(q);

            //Projects.ItemsSource = tracks;
        }

        public void RemoveSong(Track t)
        {
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
            //        if (ct.OrderVal == 0)
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

            //if (isSongPlaying)
            //{
            //    if (TrasnportVMInstance.SongsQueued > 0)
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

        //private void SetSongIndex(int tracknumber)
        //{
        //    TrasnportVMInstance.CurrentTrackNumer = tracknumber;
        //    SetCurrentSong();
        //}


        public bool isLooping { get; set; }

        public int SongsQueued
        {
            get
            {
                return PlayingTracks.Count;
            }
        }

        //public Track CurrentTrack
        //{
        //    get
        //    {
        //        return Tracklist[CurrentTrackNumer];
        //    }
        //}

        public int CurrentTrackNumer { get; set; }

        List<Track> trackList = new List<Track>();
        public List<Track> Tracklist
        {
            get { return trackList; }
            set
            {
                if (trackList != value)
                {
                    trackList = value;
                    OnPropertyChanged();
                }
            }
        }

        string currentProject;
        public string CurrentProject
        {
            get { return currentProject;  }
            set
            {
                if (currentProject != value)
                {
                    currentProject = value;
                    OnPropertyChanged();
                }
            }
        }

        private string currentSel;
        public string CurrentSel
        {
            get
            {
                return currentSel;
            }
            set
            {
                if (currentSel != value)
                {
                    currentSel = value;
                    OnPropertyChanged("CurrentSel");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
