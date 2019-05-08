using Microsoft.AppCenter.Analytics;
//using PCLStorage;
using Plugin.SimpleAudioPlayer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace MyMixes
{
    public partial class TransportViewModel : INotifyPropertyChanged
    {
        private enum PlayerStates
        {
            Playing,
            Paused,
            Stopped
        }

        public delegate void ErrorCallback(string title, string text, string button);

        private bool playingListLoaded = false;

        private PlayerStates playerState = PlayerStates.Stopped;

        private CancellationTokenSource cancelTok = new CancellationTokenSource();

        public TransportViewModel()
        {
            if (!DesignMode.IsDesignModeEnabled)
            {
                PlayCommand = new Command(TransportPlayPressed);
                PrevCommand = new Command(PrevSong);
                NextCommand = new Command(NextSong);

                player = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
                player.Loop = false;
                player.PlaybackEnded += Player_PlaybackEnded;
            }

            PlayButtonStateImage = "PlayBt.png";

            Task.Run(async () => { await UpdateSliderAsync(cancelTok.Token); });
            //UpdateSliderAsync(cancelTok.Token);
        }


        private bool prevCommandVisible = true;
        public bool PrevCommandVisible
        {
            get
            {
                return prevCommandVisible;
            }
            set
            {
                if(prevCommandVisible != value)
                {
                    prevCommandVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool nextCommandVisible = true;
        public bool NextCommandVisible
        {
            get
            {
                return nextCommandVisible;
            }
            set
            {
                if (nextCommandVisible != value)
                {
                    nextCommandVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool mainPlayMode = true;
        public bool MainPlayMode
        {
            get
            {
                return mainPlayMode;
            }
            set
            {
                if(mainPlayMode != value)
                {
                    mainPlayMode = value;
                    if (mainPlayMode)
                    {
                        PrevCommandVisible = true;
                        NextCommandVisible = true;
                    }
                    else
                    {
                        PrevCommandVisible = false;
                        NextCommandVisible = false;
                    }
                }
            }
        }

        private ErrorCallback errorCallbackRoutine = null;
        public ErrorCallback ErrorCallbackRoutine
        {
            get
            {
                return errorCallbackRoutine;
            }
            set
            {
                if(errorCallbackRoutine != value)
                {
                    errorCallbackRoutine = value;
                }
            }
        }


        private ObservableCollection<QueuedTrack> playingTracks = null; // new ObservableCollection<QueuedTrack>();
        public ObservableCollection<QueuedTrack> PlayingTracks
        {
            get
            {
                if(playingTracks == null)
                {
                    playingTracks = new ObservableCollection<QueuedTrack>();
                    PersistentData.LoadQueuedTracks(PlayingTracks);
                }
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
                if(value != selectedSong)
                {
                    if (value?.FullPath != selectedSong?.FullPath)
                    {
                        currentTrackNumber = PlayingTracks.IndexOf(value);
                        PersistentData.LastPlayedSongIndex = currentTrackNumber;
                        if (playerState == PlayerStates.Playing)
                        {
                            PlayCurrentSongAsync();
                        }
                        else
                        {
                            nowPlayingDifferentSong = true;
                        }
                    }
                    selectedSong = value;
                    OnPropertyChanged("SelectedSong");
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
                if(currentTrackNumber != value && value < PlayingTracks.Count)
                {
                    currentTrackNumber = value;
                }
                if(currentTrackNumber >= 0 && PlayingTracks.Count > 0)
                {
                    SelectedSong = PlayingTracks[currentTrackNumber];
                }
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
        private DateTime LastTime;
        private bool nowPlayingDifferentSong = false;

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

        public string nowPlaying;
        public string NowPlaying
        {
            get
            {
                if(PlayingTracks.Count > 0 && nowPlaying != null)
                {
                    return "Now Playing: " + nowPlaying;
                }
                else
                {
                    return "No track playing";
                }
            }
            set
            {
                if(nowPlaying != value)
                {
                    nowPlaying = value;
                    OnPropertyChanged();
                }
            }
        }

        private double songPosition;
        public double SongPosition
        {
            get
            {
                return songPosition;
                //if(player.IsPlaying)
                //{
                //    double newPos =  player.CurrentPosition / player.Duration;
                //    return newPos;
                //}
                //else
                //{
                //    return 0;
                //}
            }
            set
            {
                //if (player.IsPlaying)
                //{
                //    player.Seek(value * player.Duration);
                //}
                if(songPosition != value)
                {
                    songPosition = value;

                    if (player.IsPlaying)
                    {
                        player.Seek(songPosition * player.Duration);
                    }

                    OnPropertyChanged();
                }
            }
        }

        private async Task UpdateSliderAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (player.IsPlaying)
                {
                    if(player.CurrentPosition > 0.001)
                    {
                        // We don't want it to seek, so manually set property
                        songPosition = player.CurrentPosition / player.Duration ;
                        OnPropertyChanged("SongPosition");
                    }
                }

                await Task.Delay(200, token);
            }
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
                if(isAligned)
                {
                    player.Play();
                }
                else
                {
                    if (CurrentTrackNumber + 1 >= SongsQueued)
                    {
                        if(!isLooping)
                        {
                            StopPlayer();
                        }
                        CurrentTrackNumber = 0;
                    }
                    else
                    {
                        CurrentTrackNumber++;
                    }
                }
            });
        }

        public void TransportPlayPressed()
        {
            if(MainPlayMode)
            {
                if (SongsQueued > 0)
                {
                    switch (playerState)
                    {
                        case PlayerStates.Playing:
                            PausePlayer();
                            break;
                        case PlayerStates.Paused:
                            if (nowPlayingDifferentSong)
                            {
                                nowPlayingDifferentSong = false;
                                PlayCurrentSongAsync();
                            }
                            else
                            {
                                StartPlayer();
                            }
                            break;
                        case PlayerStates.Stopped:
                            PlayCurrentSongAsync();
                            break;
                    }
                }
            }
            else
            {
                switch (playerState)
                {
                    case PlayerStates.Playing:
                        PausePlayer();
                        break;
                    case PlayerStates.Paused:
                        StartPlayer();
                        break;
                    case PlayerStates.Stopped:
//                        PlayCurrentSongAsync();
                        break;
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

            PlayCurrentSongAsync();
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

            if (playerState == PlayerStates.Playing)
            {
                PlayCurrentSongAsync();
            }
            else
            {
                playerState = PlayerStates.Stopped;
            }
        }

        public void PrevSong()
        {
            if(!isAligned && playerState != PlayerStates.Stopped && player.CurrentPosition > 3)
            {
                player.Seek(0);
                //player.Stop();
                //player.Play();
            }
            else
            {
                if (CurrentTrackNumber <= 0)
                {
                    CurrentTrackNumber = SongsQueued - 1;
                }
                else
                {
                    CurrentTrackNumber--;
                }

                if (playerState == PlayerStates.Playing)
                {
                    PlayCurrentSongAsync();
                }
                else
                {
                    playerState = PlayerStates.Stopped;
                }
            }
        }

        public void MoveSongUp(QueuedTrack t)
        {
            int i = PlayingTracks.IndexOf(t);

            if (i > 0)
            {             
                PlayingTracks.Move(i, i - 1);
            }
            else
            {
                PlayingTracks.Move(i, PlayingTracks.Count - 1);
            }
        }

        private void PlayCurrentSongAsync()
        {
            PlaySongAsync(PlayingTracks[CurrentTrackNumber].FullPath);
        }

        public bool PlaySongAsync(string song)
        {
            double playerpos = player.CurrentPosition;

            Debug.Print("playing {0}\n", song);

            Dictionary<String, String> properties = new Dictionary<string, string>();

            var TimeElapsed = DateTime.UtcNow - LastTime;

            properties["TrackCount"] = PlayingTracks.Count.ToString();
            properties["LoopMode"] = isLooping.ToString();
            properties["CompareMode"] = isAligned.ToString();
            properties["PlayMode"] = playerState.ToString();

            LastTime = DateTime.UtcNow;

            Analytics.TrackEvent("PlayTrack", properties);

            try
            {
                player.Stop();

                using (Stream s = new FileStream(song, FileMode.Open))
                {
                    if (player.Load(s))
                    {
                        if (playerState != PlayerStates.Stopped && isAligned)
                        {
                            player.Seek(playerpos);
                        }

                        NowPlaying = Path.GetFileNameWithoutExtension(song);

                        StartPlayer();
                    }
                    else
                    {
                        properties.Clear();
                        properties["Length"] = s.Length.ToString();
                        properties["Type"] = Path.GetExtension(song);

                        Analytics.TrackEvent("PlayCurrent player.Load failed", properties);

                        ErrorMsg(AppResources.SongPlayFailedTitle, AppResources.SongPlayFailed, AppResources.OK);
                        StopPlayer();

                        return false;
                    }
                }
            }
            catch(Exception ex)
            {
                ErrorMsg(AppResources.SongPlayFailedTitle, ex.Message, AppResources.OK);
                Debug.Print(ex.ToString());

                return false;
            }

            return true;
        }

        public async Task LoadProjects()
        {
            if(!playingListLoaded)
            {
                await ValidatePlayingTracks();

                playingListLoaded = true;

                CurrentTrackNumber = 0;
            }
        }

        private async Task ValidatePlayingTracks()
        {
            List<QueuedTrack> t_remove = new List<QueuedTrack>();

            foreach(QueuedTrack t in PlayingTracks)
            {
                if(!File.Exists(t.FullPath))
                {
                    t_remove.Add(t);
                }
            }

            foreach(QueuedTrack t in t_remove)
            {
                PlayingTracks.Remove(t);
            }

            playingListLoaded = false;

            await PersistentData.SaveQueuedTracksAsync(PlayingTracks);
        }

        public void RemoveSong(QueuedTrack t)
        {
            if (t == null)
                return;

            int i = PlayingTracks.IndexOf(t);
            PlayingTracks.Remove(t);
            playingListLoaded = false;

            if (playerState != PlayerStates.Stopped && CurrentTrackNumber == i)
            {
                StopPlayer();

                if(i >= PlayingTracks.Count)
                {
                    CurrentTrackNumber = 0;
                }

                if (isLooping && playerState != PlayerStates.Stopped)
                {
                    PlayCurrentSongAsync();
                }
            }
        }

        public void AddSong(Track t)
        {
            int i = 0;
            for (; i < PlayingTracks.Count; i++)
            {
                if (PlayingTracks[i].Name == t.Name && PlayingTracks[i].Project == t.Project)
                    break;
            }

            if (i >= PlayingTracks.Count)
            {
                PlayingTracks.Add(new QueuedTrack() { Name = t.Name, Project = t.Project, FullPath = t.FullPath });
                playingListLoaded = false;
            }
        }

        private void StartPlayer()
        {
            playerState = PlayerStates.Playing;

            player.Play();

            PlayButtonStateImage = "PauseBt.png";
        }

        private void PausePlayer()
        {
            playerState = PlayerStates.Paused;
            player.Pause();
            PlayButtonStateImage = "PlayBt.png";
        }

        public void StopPlayer()
        {
            playerState = PlayerStates.Stopped;
            player.Stop();
            PlayButtonStateImage = "PlayBt.png";
        }

        public int SongsQueued
        {
            get
            {
                return playingTracks.Count;
            }
        }

        public void ErrorMsg(string title, string text, string button)
        {
            if(ErrorCallbackRoutine != null)
            {
                ErrorCallbackRoutine(title, text, button);
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
