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
        public Command PlayCommand { get; set; }
        public Command PrevCommand { get; set; }
        public Command NextCommand { get; set; }

        public ObservableCollection<QueuedTrack> PlayingTracks = new ObservableCollection<QueuedTrack>();
        public ISimpleAudioPlayer player { get; set; }

        private string playButtonStateImage;
        public string PlayButtonStateImage
        {
            get
            {
                return playButtonStateImage;
            }
            set
            {
                if (playButtonStateImage != value)
                {
                    playButtonStateImage = value;
                    OnPropertyChanged("PlayButtonStateImage");
                }
            }
        }

        private string playingSong;
        private bool isSongPlaying;
        private int currentSong = 0;

        public TransportViewModel()
        {
            if (!DesignMode.IsDesignModeEnabled)
            {
                PlayCommand = new Command(PlaySong);
                PrevCommand = new Command(PrevSong);
                NextCommand = new Command(NextSong);

                player = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
                player.Loop = false;
                player.PlaybackEnded += Player_PlaybackEnded;

                currentSong = PersistentData.LastPlayedSongIndex;
            }

            PlayButtonStateImage = "PlayBt.png";
        }

        private async void Player_PlaybackEnded(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                CurrentTrackNumber++;
                if (CurrentTrackNumber >= SongsQueued)
                {
                    CurrentTrackNumber = 0;
                    if (isLooping)
                    {
                        await PlayCurrentSong();
                    }
                    else
                    {
                        isSongPlaying = false;
                        player.Stop();
                        PlayButtonStateImage = "PlayBt.png";
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
            CurrentProject = Path.GetFileName(t.ProjectPath);

            CurrentSel = t.Name;
        }

        public async void PlaySong()
        {
            if (SongsQueued > 0)
            {
                if (player.CurrentPosition > 0)
                {
                    if (isSongPlaying)
                    {
                        isSongPlaying = false;
                        player.Pause();
                        PlayButtonStateImage = "PlayBt.png";
                    }
                    else
                    {
                        isSongPlaying = true;
                        player.Play();
                        PlayButtonStateImage = "PauseBt.png";
                    }
                }
                else
                {
                    isSongPlaying = true;
                    PlayButtonStateImage = "PauseBt.png";

                    await PlayCurrentSong();
                }
            }
        }

        public async void PlaySongTrack(QueuedTrack t)
        {
            for(CurrentTrackNumber = 0; CurrentTrackNumber < PlayingTracks.Count; CurrentTrackNumber++)
            {
                if(PlayingTracks[CurrentTrackNumber] == t)
                {
                    break;
                }

            }

            await PlayCurrentSong();
        }

        public void NextSong()
        {
            CurrentTrackNumber++;
            if(CurrentTrackNumber >= SongsQueued)
            {
                CurrentTrackNumber = 0;
            }

            if(isSongPlaying)
            {
                PlayCurrentSong();
            }
        }

        public void PrevSong()
        {
            CurrentTrackNumber--;
            if (CurrentTrackNumber < 0)
            {
                CurrentTrackNumber = SongsQueued - 1;
            }

            if (isSongPlaying)
            {
                PlayCurrentSong();
            }
        }

        private async Task PlayCurrentSong()
        {
            //Track t = CurrentTrack;
            //ViewModel.CurrentSel = t.Name;
            //ViewModel.CurrentProject = t.Project;

            string path = Path.GetDirectoryName(PlayingTracks[CurrentTrackNumber].FullPath);
            string filename = Path.GetFileName(PlayingTracks[CurrentTrackNumber].FullPath);

            Debug.Print("playing {0} {1}\n", filename, path);

            IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(path);
            if(folder == null)
            {
                Debug.Print("Error folder is null, why woudl that happen? No sync?");
                return ;
            }

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
        public bool isAligned { get; set; }

        public int SongsQueued
        {
            get
            {
                return PlayingTracks.Count;
            }
        }

        public int CurrentTrackNumber { get; set; }

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

        public void LoadSampleData()
        {
            PlayingTracks.Add(new QueuedTrack() { Name = "Brother hammond", Project = "A Birds Nest" });
        }
    }
}
