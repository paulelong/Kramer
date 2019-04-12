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

        private bool isSongPlaying;

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

                CurrentTrackNumber = PersistentData.LastPlayedSongIndex;
            }

            PlayButtonStateImage = "PlayBt.png";
        }

        internal object GetSelectedProject()
        {
            return PlayingTracks[PersistentData.LastPlayedSongIndex];
        }

        public void MoveSongDown(QueuedTrack t)
        {
            int i = PlayingTracks.IndexOf(t);

            if (i + 1 < PlayingTracks.Count)
            {
                PlayingTracks.Move(i, i + 1);
            }
            else
            {
                PlayingTracks.Move(i, 0);
            }
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

        public async Task SetCurrentSong(QueuedTrack t)
        {
            CurrentTrackNumber = PlayingTracks.IndexOf(t);
            if(isSongPlaying)
            {
                await PlayCurrentSong();
            }
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
                        await PlayCurrentSong();
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

        public void RemoveSong(QueuedTrack t)
        {
            if (t == null)
                return;

            int i = PlayingTracks.IndexOf(t);

            if (player.CurrentPosition > 0 && CurrentTrackNumber == i)
            {
                player.Stop();
            }

            PlayingTracks.Remove(t);
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
    }
}
