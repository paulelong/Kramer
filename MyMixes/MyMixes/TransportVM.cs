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
            CrossMediaManager.Current.StateChanged += Current_StateChanged;

            ResetPlayer();

            Task.Run(async () => { await UpdateSliderAsync(cancelTok.Token); });
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

            playlistReady = false;
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


        // Transport Player control
        public Command PlayCommand { get; set; }
        public Command PrevCommand { get; set; }
        public Command NextCommand { get; set; }

        private double songPosition;
        public double SongPosition
        {
            get
            {
                return songPosition;

            }
            set
            {
                if (songPosition != value)
                {
                    songPosition = value;
                    last_playerpos = songPosition;
                    SeekTo(songPosition);
                    OnPropertyChanged();
                }
            }
        }


        private QueuedTrack selectedSong = null;
        public QueuedTrack SelectedSong
        {
            get
            {
                if (selectedSong == null && Playlist.Count > 0 && currentTrackNumber >= 0)
                {
                    selectedSong = Playlist[currentTrackNumber];
                }
                return selectedSong;
            }
            set
            {
                if (value != selectedSong)
                {
                    if (CurrentTrackNumber != Playlist.IndexOf(value) && CrossMediaManager.Current.IsPlaying())
                    {
                        last_playerpos = SongPosition;
                        currentTrackNumber = Playlist.IndexOf(value);
                        CrossMediaManager.Current.PlayQueueItem(CurrentTrackNumber);
                        if (isAligned)
                        {
                            SeekTo(last_playerpos);
                        }
                    }
                    else
                    {
                        currentTrackNumber = Playlist.IndexOf(value);
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
                if (currentTrackNumber != value && value < Playlist.Count)
                {
                    currentTrackNumber = value;
                }

                // Set the selected song so the UI will update.
                if (currentTrackNumber >= 0 && Playlist.Count > 0)
                {
                    SelectedSong = Playlist[currentTrackNumber];
                }
            }
        }

        #endregion Properties

        private void Current_StateChanged(object sender, MediaManager.Playback.StateChangedEventArgs e)
        {
            switch (e.State)
            {
                case MediaManager.Player.MediaPlayerState.Playing:
                    if (MainPlayMode)
                    {
                        if (Playlist[CurrentTrackNumber].FullPath != CrossMediaManager.Current.Queue.Current.MediaUri)
                        {
                            CrossMediaManager.Current.PlayQueueItem(CurrentTrackNumber);
                        }

                        if (pausePlay)
                        {
                            CrossMediaManager.Current.PlayPause();
                            pausePlay = false;
                        }

                        if (isAligned)
                        {
                            SeekTo(last_playerpos);
                        }
                    }
                    PlayButtonStateImage = "PauseBt.png";
                    break;
                case MediaManager.Player.MediaPlayerState.Paused:
                case MediaManager.Player.MediaPlayerState.Stopped:
                    PlayButtonStateImage = "PlayBt.png";
                    break;

            }
        }

        private void Current_MediaItemChanged(object sender, MediaManager.Media.MediaItemEventArgs e)
        {
            if (isAligned)
            {
                SeekTo(last_playerpos);
            }

            // Figure out the song index
            CurrentTrackNumber = mediaPlayList.IndexOf(e.MediaItem);
        }

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
            // We use a universal transport that can be used from different pages in the UI
            if(MainPlayMode)
            {
                if (SongsQueued > 0)
                {
                    ReadyPlaylist();

                    CrossMediaManager.Current.PlayPause();
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
            if (CrossMediaManager.Current.IsPlaying())
            {
                // Remember the last position for isAlign looping comparison
                last_playerpos = SongPosition;
                if (CurrentTrackNumber + 1 >= SongsQueued)
                {
                    CrossMediaManager.Current.PlayQueueItem(0);
                    if (isAligned)
                    {
                        SeekTo(last_playerpos);
                        Console.WriteLine("Seeking to {0}", last_playerpos);
                    }
                }
                else
                {
                    CrossMediaManager.Current.PlayNext();
                }
            }
            else
            {
                if (CurrentTrackNumber + 1 >= SongsQueued)
                {
                    CurrentTrackNumber = 0;

                }
                else
                {
                    CurrentTrackNumber++;
                }
            }
        }

        public void PrevSong()
        {
            if(CrossMediaManager.Current.IsPlaying())
            {
                if (isAligned)
                {
                    last_playerpos = SongPosition;

                    if (CurrentTrackNumber <= 0)
                    {
                        CurrentTrackNumber = SongsQueued - 1;
                    }
                    else
                    {
                        CurrentTrackNumber--;
                    }

                    CrossMediaManager.Current.PlayPrevious();
                }
                else
                {
                    CrossMediaManager.Current.PlayPreviousOrSeekToStart();
                }
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

        private bool pausePlay;
        private bool playlistReady = false;
        private readonly double MAX_AHEAD_SEEK = 0.01;

        public async Task ReadyPlaylist()
        {
            if(!playlistReady)
            {
                mediaPlayList.Clear();

                foreach (QueuedTrack track in Playlist)
                {
                    IMediaItem mediaItem = new MediaItem(track.FullPath) { IsMetadataExtracted = true };
                    mediaItem = await CrossMediaManager.Current.Extractor.UpdateMediaItem(mediaItem).ConfigureAwait(false);
                    
                    mediaItem.Title = track.Name;
                    mediaItem.Album = track.Project;

                    mediaPlayList.Add(mediaItem);
                }

                await CrossMediaManager.Current.Play(mediaPlayList);
                CrossMediaManager.Current.RepeatMode = MediaManager.Playback.RepeatMode.Off;
                await CrossMediaManager.Current.PlayPause();

                pausePlay = true;                

                playlistReady = true;
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

            if(File.Exists(song))
            {
                Console.WriteLine("Song exists " + song);
            }
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

            List<QueuedTrack> t_remove = new List<QueuedTrack>();

            foreach(QueuedTrack t in Playlist)
            {
                if(!File.Exists(t.FullPath))
                {
                    Console.WriteLine("Song missing " + t.FullPath);
                    t_remove.Add(t);
                }
            }

            foreach(QueuedTrack t in t_remove)
            {
                int i = Playlist.IndexOf(t);
                Playlist.Remove(t);
                //mediaPlayList.RemoveAt(i);
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

            playlistReady = false;
            playingListLoaded = false;

            if (CrossMediaManager.Current.IsPlaying() && CurrentTrackNumber == i)
            {
                StopPlayer();
            }

            if (i > Playlist.Count)
            {
                currentTrackNumber--;
            }
            else
            {
                currentTrackNumber = 0;
            }

            if (Playlist.Count > 0)
            {
                ReadyPlaylist();
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
            await CrossMediaManager.Current.PlayPause();
        }

        private void PausePlayer()
        {
            CrossMediaManager.Current.PlayPause();
        }

        public void StopPlayer()
        {
            CrossMediaManager.Current.Stop();
        }

        private void SeekTo(double songPos)
        {
            double curPos = CrossMediaManager.Current.Position.TotalSeconds / CrossMediaManager.Current.Duration.TotalSeconds;

            if(CrossMediaManager.Current.Duration.TotalSeconds > 0 && CrossMediaManager.Current.Duration.TotalSeconds < 3600 && songPos > 0 && songPos < 1 && (songPos - curPos) > MAX_AHEAD_SEEK)
            {
                CrossMediaManager.Current.SeekTo(new TimeSpan((long)(songPos * CrossMediaManager.Current.Duration.TotalSeconds * 10000000)));
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
