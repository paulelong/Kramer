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
using MediaManager;
using MediaManager.Library;

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

        private string currentSel;

        private DateTime LastTime;

        private bool nowPlayingDifferentSong = false;

        private double last_playerpos;


        public TransportViewModel()
        {
            if (!DesignMode.IsDesignModeEnabled)
            {
                PlayCommand = new Command(TransportPlayPressed);
                PrevCommand = new Command(PrevSong);
                NextCommand = new Command(NextSong);
            }

            PlayButtonStateImage = "PlayBt.png";

            CrossMediaManager.Current.MediaItemChanged += Current_MediaItemChanged;            

            ResetPlayer();

            Task.Run(async () => { await UpdateSliderAsync(cancelTok.Token); });
        }

        private void Current_MediaItemChanged(object sender, MediaManager.Media.MediaItemEventArgs e)
        {
            if (isAligned)
            {
                CrossMediaManager.Current.SeekTo(new TimeSpan((long)(last_playerpos * CrossMediaManager.Current.Duration.TotalSeconds * 10000000)));
                Console.WriteLine("Seeking to {0}", last_playerpos);
            }
        }

        public void ResetPlayer()
        {
            //if(player != null)
            //{
            //    player.PlaybackEnded -= Player_PlaybackEnded;
            //    player.Dispose();
            //    player = null;

            //}

            //player = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
            //player.Loop = false;
            //player.PlaybackEnded += Player_PlaybackEnded;

            NowPlaying = "";
            SongPosition = 0;
        }

        #region Properties
        public int SongsQueued
        {
            get
            {
                return playlist.Count;
            }
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

        private ObservableCollection<IMediaItem> mediaPlayList = new ObservableCollection<IMediaItem>();

        private ObservableCollection<QueuedTrack> playlist = null; // new ObservableCollection<QueuedTrack>();
        public ObservableCollection<QueuedTrack> Playlist
        {
            get
            {
                if(playlist == null)
                {
                    playlist = new ObservableCollection<QueuedTrack>();
                    PersistentData.LoadQueuedTracks(playlist);

                    //ReadyPlaylist();
                }
                return playlist;
            }
        }

        public void RemoveTrack(int tracknum)
        {
            Playlist.RemoveAt(tracknum);
            mediaPlayList.RemoveAt(tracknum);
        }

        private QueuedTrack selectedSong = null;
        public QueuedTrack SelectedSong
        {
            get
            {
                if(selectedSong == null && Playlist.Count > 0 && currentTrackNumber >= 0)
                {
                    selectedSong = Playlist[currentTrackNumber];
                }
                return selectedSong;
            }
            set
            {
                if(value != selectedSong)
                {
                    if (value?.FullPath != selectedSong?.FullPath)
                    {
                        currentTrackNumber = Playlist.IndexOf(value);
                        PersistentData.LastPlayedSongIndex = currentTrackNumber;
                        if (playerState == PlayerStates.Playing)
                        {
                            CrossMediaManager.Current.PlayQueueItem(currentTrackNumber);
                            //PlayCurrentSongAsync();
                        }
                        else
                        {
                            nowPlayingDifferentSong = true;
                            //NowPlaying = value.Name;
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
                if(currentTrackNumber != value && value < Playlist.Count)
                {
                    currentTrackNumber = value;
                }
                if(currentTrackNumber >= 0 && Playlist.Count > 0)
                {
                    SelectedSong = Playlist[currentTrackNumber];
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
                if(Playlist.Count > 0 && nowPlaying != null)
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

            }
            set
            {
                if(songPosition != value)
                {
                    songPosition = value;

                    if(CrossMediaManager.Current.IsPlaying())
                    {
                        CrossMediaManager.Current.SeekTo(new TimeSpan((long)(songPosition * CrossMediaManager.Current.Duration.TotalSeconds * 10000000)));
                    }

                    //if (player.IsPlaying)
                    //{
                    //    player.Seek(songPosition * player.Duration);
                    //}

                    OnPropertyChanged();
                }
            }
        }

        // Transport Player control
        public Command PlayCommand { get; set; }
        public Command PrevCommand { get; set; }
        public Command NextCommand { get; set; }

        #endregion Properties

        private async Task UpdateSliderAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (CrossMediaManager.Current.IsPlaying())
                {
                    if(CrossMediaManager.Current.Position.TotalSeconds > 0.001)
                    {
                        // We don't want it to seek, so manually set property
                        //songPosition = player.CurrentPosition / player.Duration ;
                        songPosition = CrossMediaManager.Current.Position.TotalSeconds / CrossMediaManager.Current.Duration.TotalSeconds;
                        OnPropertyChanged("SongPosition");
                    }
                }

                await Task.Delay(200, token);
            }
        }


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
                                StartPlayer();
                                //PlayCurrentSongAsync();
                            }
                            else
                            {
                                StartPlayer();
                            }
                            break;
                        case PlayerStates.Stopped:
                            StartPlayer();
//                            PlayCurrentSongAsync();
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

#pragma warning restore AvoidAsyncVoid

        public void NextSong()
        {
            if (playerState == PlayerStates.Playing)
            {
                last_playerpos = SongPosition;

                //CrossMediaManager.Current.PlayNext();

//                if (isAligned)
//                {
//                    // CrossMediaManager.Current.SeekTo(new TimeSpan(0, 0, 40));
//                    CrossMediaManager.Current.SeekTo(new TimeSpan((long)(playerpos * CrossMediaManager.Current.Duration.TotalSeconds * 10000000)));
////                    CrossMediaManager.Current.SeekTo(new TimeSpan((long)(playerpos * 10000000)));
//                }

                //    PlayCurrentSongAsync();
            }
            else
            {
                playerState = PlayerStates.Stopped;
            }

            if (CurrentTrackNumber + 1 >= SongsQueued)
            {
                CurrentTrackNumber = 0;
            }
            else
            {
                CurrentTrackNumber++;
            }
        }

        public void PrevSong()
        {
            CrossMediaManager.Current.PlayPreviousOrSeekToStart();
            if (!isAligned && playerState != PlayerStates.Stopped && player.CurrentPosition > 3)
            {
                CrossMediaManager.Current.SeekToStart();
//                player.Seek(0);
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

                last_playerpos = SongPosition;
                if (playerState == PlayerStates.Playing)
                {
                    //CrossMediaManager.Current.PlayPrevious();
                }

                //if (isAligned)
                //{
                //    CrossMediaManager.Current.SeekTo(new TimeSpan((long)(playerpos * CrossMediaManager.Current.Duration.TotalSeconds * 10000000)));
                //    //CrossMediaManager.Current.SeekTo(new TimeSpan((long)(playerpos * 10000000)));
                //}
            }
        }

        public void MoveSongUp(QueuedTrack t)
        {
            int i = Playlist.IndexOf(t);

            if (i > 0)
            {             
                Playlist.Move(i, i - 1);

                mediaPlayList.Move(i, i - 1);
            }
            else
            {
                Playlist.Move(i, Playlist.Count - 1);
                mediaPlayList.Move(i, i - 1);
            }
        }

        private void PlayCurrentSongAsync()
        {
            PlaySongAsync(Playlist[CurrentTrackNumber].FullPath);
        }

        private bool playlistReady = false;
        public async Task ReadyPlaylist()
        {
            if(!playlistReady)
            {
                playlistReady = true;

                mediaPlayList.Clear();

                foreach (QueuedTrack track in Playlist)
                {
                    IMediaItem mediaItem = new MediaItem(track.FullPath);
                    mediaItem.Title = track.Name;
                    mediaItem.Album = track.Project;

                    mediaPlayList.Add(mediaItem);
                }

                await CrossMediaManager.Current.Play(mediaPlayList);
                CrossMediaManager.Current.RepeatMode = MediaManager.Playback.RepeatMode.All;

                //await CrossMediaManager.Current.Stop();
            }
        }

        public bool PlaySongAsync(string song)
        {

            double playerpos = CrossMediaManager.Current.Position.TotalSeconds;
            //double playerpos = player.CurrentPosition;

            Debug.Print("playing {0}\n", song);

            Dictionary<String, String> properties = new Dictionary<string, string>();

            var TimeElapsed = DateTime.UtcNow - LastTime;

            properties["TrackCount"] = Playlist.Count.ToString();
            properties["LoopMode"] = isLooping.ToString();
            properties["CompareMode"] = isAligned.ToString();
            properties["PlayMode"] = playerState.ToString();

            LastTime = DateTime.UtcNow;

            Analytics.TrackEvent("PlayTrack", properties);

            //if (playerState != PlayerStates.Stopped && isAligned)
            //{
            //    player.Seek(playerpos);
            //}

            NowPlaying = Path.GetFileNameWithoutExtension(song);

            CrossMediaManager.Current.Play(song);
            //StartPlayer();

            return true;

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
            
            //await ReadyPlaylist();
        }

        private async Task ValidatePlayingTracks()
        {
            await ReadyPlaylist();

            List<QueuedTrack> t_remove = new List<QueuedTrack>();

            foreach(QueuedTrack t in Playlist)
            {
                if(!File.Exists(t.FullPath))
                {
                    t_remove.Add(t);
                }
            }

            foreach(QueuedTrack t in t_remove)
            {
                int i = Playlist.IndexOf(t);
                Playlist.Remove(t);
                mediaPlayList.RemoveAt(i);
            }

            playingListLoaded = false;

            await PersistentData.SaveQueuedTracksAsync(Playlist);
        }

        public void RemoveSong(QueuedTrack t)
        {
            if (t == null)
                return;

            int i = Playlist.IndexOf(t);
            Playlist.Remove(t);
            mediaPlayList.RemoveAt(i);
            playingListLoaded = false;

            if (playerState != PlayerStates.Stopped && CurrentTrackNumber == i)
            {
                StopPlayer();

                if(i >= Playlist.Count)
                {
                    CurrentTrackNumber = 0;
                }

                if (isLooping && playerState != PlayerStates.Stopped)
                {
                    StartPlayer();
                    //PlayCurrentSongAsync();
                }
            }
        }

        public void AddSong(Track t)
        {
            int i = 0;
            for (; i < Playlist.Count; i++)
            {
                if (Playlist[i].Name == t.Name && Playlist[i].Project == t.Project)
                    break;
            }

            if (i >= Playlist.Count)
            {
                Playlist.Add(new QueuedTrack() { Name = t.Name, Project = t.Project, FullPath = t.FullPath, LastModifiedDate = t.LastModifiedDate });
                playingListLoaded = false;

                IMediaItem mediaItem = new MediaItem(t.FullPath);
                mediaItem.Title = t.Name;
                mediaItem.Album = t.Project;

                mediaPlayList.Add(mediaItem);
            }
        }

        private async Task StartPlayer()
        {
            playerState = PlayerStates.Playing;

            await ReadyPlaylist();

            //await CrossMediaManager.Current.PlayQueueItem(mediaPlayList[CurrentTrackNumber]);
            await CrossMediaManager.Current.Play();
            
            //player.Play();

            PlayButtonStateImage = "PauseBt.png";
        }

        private void PausePlayer()
        {
            playerState = PlayerStates.Paused;

            CrossMediaManager.Current.Pause();
            //player.Pause();

            PlayButtonStateImage = "PlayBt.png";
        }

        public void StopPlayer()
        {
            playerState = PlayerStates.Stopped;

            CrossMediaManager.Current.Stop();
            //player.Stop();

            PlayButtonStateImage = "PlayBt.png";
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
