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
        private bool isSongPlaying;

        private readonly ObservableCollection<QueuedTrack> playingTracks = new ObservableCollection<QueuedTrack>();
        public ObservableCollection<QueuedTrack> PlayingTracks
        {
            get
            {
                return playingTracks;
            }
        }

        private QueuedTrack selectedSong = null;
        public QueuedTrack SelectedSong
        {
            get
            {
                if(selectedSong == null && PlayingTracks.Count > 0 && currentTrackNumber >= 0)
                {
                    selectedSong = PlayingTracks[currentTrackNumber];
                }
                return selectedSong;
            }
            set
            {
                if (value != selectedSong)
                {
                    selectedSong = value;
                    currentTrackNumber = PlayingTracks.IndexOf(value);
                    OnPropertyChanged("SelectedSong");
                    PersistentData.LastPlayedSongIndex = currentTrackNumber;
                    if(isSongPlaying)
                    {
                        PlayCurrentSongAsync();
                    }
                }
            }
        }

        private int currentTrackNumber = -1;
        public int CurrentTrackNumber
        {
            get
            {
                return currentTrackNumber;
            }
            set
            {
                if(currentTrackNumber != value)
                {
                    currentTrackNumber = value;
                }
                SelectedSong = PlayingTracks[currentTrackNumber];
            }

        }

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


        public bool isLooping { get; set; }
        public bool isAligned { get; set; }

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
            get { return currentProject; }
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
            }

            PlayButtonStateImage = "PlayBt.png";
        }

        // Transport Player control
        public Command PlayCommand { get; set; }
        public Command PrevCommand { get; set; }
        public Command NextCommand { get; set; }

#pragma warning disable AvoidAsyncVoid
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
                        await PlayCurrentSongAsync();
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
                    await PlayCurrentSongAsync();
                }
            });
        }

        //public async Task SetCurrentSongAsync(QueuedTrack t)
        //{
        //    CurrentTrackNumber = playingTracks.IndexOf(t);
        //    if(isSongPlaying)
        //    {
        //        await PlayCurrentSongAsync();
        //    }
        //}

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
//                        await PlayCurrentSongAsync();
                        PlayButtonStateImage = "PauseBt.png";
                    }
                }
                else
                {
                    isSongPlaying = true;
                    PlayButtonStateImage = "PauseBt.png";

                    await PlayCurrentSongAsync();
                }
            }
        }

        public async void PlaySongTrack(QueuedTrack t)
        {
            for(CurrentTrackNumber = 0; CurrentTrackNumber < playingTracks.Count; CurrentTrackNumber++)
            {
                if(playingTracks[CurrentTrackNumber] == t)
                {
                    break;
                }

            }

            await PlayCurrentSongAsync();
        }
#pragma warning restore AvoidAsyncVoid

        public void NextSong()
        {
            if(CurrentTrackNumber+1 >= SongsQueued)
            {
                CurrentTrackNumber = 0;
            }
            else
            {
                CurrentTrackNumber++;
            }

            if (isSongPlaying)
            {
                PlayCurrentSongAsync();
            }
        }

        public void PrevSong()
        {
            if (CurrentTrackNumber <= 0)
            {
                CurrentTrackNumber = SongsQueued - 1;
            }
            else
            {
                CurrentTrackNumber--;
            }

            if (isSongPlaying)
            {
                PlayCurrentSongAsync();
            }
        }

        public void MoveSongDown(QueuedTrack t)
        {
            int i = playingTracks.IndexOf(t);

            if (i + 1 < playingTracks.Count)
            {
                playingTracks.Move(i, i + 1);
            }
            else
            {
                playingTracks.Move(i, 0);
            }
        }

        private async Task PlayCurrentSongAsync()
        {
            //Track t = CurrentTrack;
            //ViewModel.CurrentSel = t.Name;
            //ViewModel.CurrentProject = t.Project;

            string path = Path.GetDirectoryName(playingTracks[CurrentTrackNumber].FullPath);
            string filename = Path.GetFileName(playingTracks[CurrentTrackNumber].FullPath);

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
            PersistentData.LoadQueuedTracks(playingTracks);
        }

        public void RemoveSong(QueuedTrack t)
        {
            if (t == null)
                return;

            int i = playingTracks.IndexOf(t);

            if (player.CurrentPosition > 0 && CurrentTrackNumber == i)
            {
                player.Stop();
            }

            playingTracks.Remove(t);
        }

        //private void SetSongIndex(int tracknumber)
        //{
        //    TrasnportVMInstance.CurrentTrackNumer = tracknumber;
        //    SetCurrentSong();
        //}

        public int SongsQueued
        {
            get
            {
                return playingTracks.Count;
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
