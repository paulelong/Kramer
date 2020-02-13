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

        //private bool nowPlayingDifferentSong = false;

        private double last_playerpos;
        private double last_endpos;

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

        //private ObservableCollection<IMediaItem> mediaPlayList = new ObservableCollection<IMediaItem>();

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


        private bool isLoopingLoaded = false;
        private bool isLooping = false;
        public bool IsLooping 
        { 
            get
            {
                if(!isLoopingLoaded)
                {
                    isLoopingLoaded = true;
                    IsLooping = PersistentData.LastLoop;
                }

                return isLooping;
            }
            set
            {
                if (value != isLooping)
                {
                    PersistentData.LastLoop = value;
                    isLooping = value;
                    SetRepeatMode();
                }

            }
        }

        private bool isAlignedLoaded = false;
        private bool isAligned = false;
        public bool IsAligned 
        { 
            get
            {
                if (!isAlignedLoaded)
                {
                    isAlignedLoaded = true;
                    IsAligned = PersistentData.LastAlign;
                }

                return isAligned;
            }
            set
            {
                if (value != isAligned)
                {
                    PersistentData.LastAlign = value;
                    isAligned = value;

                    SetRepeatMode();
                }
            }
        }

        private void SetRepeatMode()
        {
            if (IsLooping)
            {
                if (IsAligned)
                {
                    CrossMediaManager.Current.RepeatMode = MediaManager.Playback.RepeatMode.One;
                }
                else
                {
                    CrossMediaManager.Current.RepeatMode = MediaManager.Playback.RepeatMode.All;
                }
            }
            else
            {
                CrossMediaManager.Current.RepeatMode = MediaManager.Playback.RepeatMode.Off;
            }

            Debug.Print("Looping mode: {0}", CrossMediaManager.Current.RepeatMode.ToString());
        }

        public string nowPlaying;

        public bool RemovedSong { get; private set; }

        public string NowPlaying
        {
            get
            {
                if(Playlist.Count > 0 && !string.IsNullOrEmpty(nowPlaying))
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
                    Debug.Print("SetSongPosition was {0} now {1}\n", songPosition, value);
                    songPosition = value;
                    last_playerpos = songPosition;
                    last_endpos = CrossMediaManager.Current.Duration.TotalSeconds;
                    SeekTo(songPosition);
                    OnPropertyChanged("SongPosition");
                }
            }
        }

        private async Task SetSelectedSong(QueuedTrack track)
        {
            Debug.Print("Current track {0} doesn't match playing song {1}, lastplayerpos={2} \n", CurrentTrackNumber, Playlist.IndexOf(track), SongPosition);

            if (CrossMediaManager.Current.IsPlaying())
            {
                last_playerpos = SongPosition;
                last_endpos = CrossMediaManager.Current.Duration.TotalSeconds;
                //currentTrackNumber = Playlist.IndexOf(track);
                await CrossMediaManager.Current.PlayQueueItem(CurrentTrackNumber); // Awaitable, bug?
                SelectedSong = track;
            }
            else
            {
                currentTrackNumber = Playlist.IndexOf(track);
            }

            PersistentData.LastPlayedSongIndex = CurrentTrackNumber;
            //PersistentData.Save();

            Debug.Print("SelectedSong was {0} now {1}\n", selectedSong?.Name, track?.Name);
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
                    selectedSong = value;
                    currentTrackNumber = Playlist.IndexOf(selectedSong);
                    if(CrossMediaManager.Current.IsPlaying())
                    {
                        last_playerpos = songPosition;
                        if (!CrossMediaManager.Current.PlayQueueItem(CurrentTrackNumber).Result)
                        {
                            Debug.Print("SelectedSong PlayQueueItem failed {0} {1}\n", selectedSong?.Name, value?.Name);
                        }

                    }
                    PersistentData.LastPlayedSongIndex = CurrentTrackNumber;

                    OnPropertyChanged("SelectedSong");
                }
                else
                {
                    Debug.Print("SelectedSong didn't change {0} {1}\n", selectedSong?.Name, value?.Name);
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
                //if (currentTrackNumber >= 0 && Playlist.Count > 0)
                //{
                //    SelectedSong = Playlist[currentTrackNumber];
                //}
            }
        }

        #endregion Properties

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
            CrossMediaManager.Current.MediaItemFailed += Current_MediaItemFailed;

            CrossMediaManager.Current.KeepScreenOn = true;

            ResetPlayer();

            Task.Run(async () => { await UpdateSliderAsync(cancelTok.Token); });
        }

        private void Current_MediaItemFailed(object sender, MediaManager.Media.MediaItemFailedEventArgs e)
        {
            Debug.Print("Failed to load media item {0}\n", e);
        }

        public void ResetPlayer()
        {
            //CrossMediaManager.Current.RepeatMode = MediaManager.Playback.RepeatMode.Off;

            playlistReady = false;
            //NowPlaying = "";
            SongPosition = 0;

            CrossMediaManager.Current.Stop();
        }

        public void UpdateUI()
        {
            Xamarin.Forms.Device.BeginInvokeOnMainThread(() => { 
                Debug.Print("Setting last song played which was {0}", PersistentData.LastPlayedSongIndex); 
                CurrentTrackNumber = PersistentData.LastPlayedSongIndex;
                // Select the last song index
                OnPropertyChanged("SelectedSong");

            });
        }

        private async void Current_StateChanged(object sender, MediaManager.Playback.StateChangedEventArgs e)
        {
            Debug.Print("STATE CHANGE: {0}\n", e.State.ToString());

            switch (e.State)
            {
                case MediaManager.Player.MediaPlayerState.Playing:
                    if (MainPlayMode)
                    {
                        if(CrossMediaManager.Current.Queue.Current != null)
                        {
                            //if (CrossMediaManager.Current.Queue != null && !RemovedSong)
                            //{
                                //if (CurrentTrackNumber != CrossMediaManager.Current.Queue.IndexOf(e.MediaItem))
                                //{
                                //    CurrentTrackNumber = CrossMediaManager.Current.Queue.IndexOf(e.MediaItem);

                                //    await SetSelectedSong(Playlist[CurrentTrackNumber]);
                                //}
                            //}

                            if (Playlist[CurrentTrackNumber].FullPath != CrossMediaManager.Current.Queue.Current.MediaUri)
                            {
                                await SetSelectedSong(Playlist[CurrentTrackNumber]);
                                //await CrossMediaManager.Current.PlayQueueItem(CurrentTrackNumber);
                                //OnPropertyChanged("SelectedSong");
                            }

                            //if (pausePlay)
                            //{
                            //    CrossMediaManager.Current.PlayPause();
                            //    pausePlay = false;
                            //}

                            if (IsAligned)
                            {
                                await SeekTo(last_playerpos);

                            //    double ts;
                            //    if ((ts = CrossMediaManager.Current.Position.TotalSeconds) > 0.2)
                            //    {
                            //        Debug.Print("*** Compare and not at begining *** {0}\n", ts);
                            //        Debug.Print(" Percent done = {0} < {1}", CrossMediaManager.Current.Position.TotalSeconds, last_endpos);
                            //        await SeekTo(last_playerpos);
                            //    }
                            //    else
                            //    {
                            //        Debug.Print("*** Compare but at begining *** {0}\n", ts);
                            //        if (IsLooping)
                            //        {
                            //            last_playerpos = 0;
                            //            SongPosition = 0;
                            //        }
                            //        else
                            //        {
                            //            await StopPlayer();
                            //            SongPosition = 0;
                            //        }
                            //    }
                            }

                            NowPlaying = Playlist[CurrentTrackNumber].Name;
                            last_endpos = CrossMediaManager.Current.Duration.TotalSeconds;
                        }
                    }
                    PlayButtonStateImage = "PauseBt.png";
                    break;
                case MediaManager.Player.MediaPlayerState.Stopped:
                    PlayButtonStateImage = "PlayBt.png";
                    if(RemovedSong)
                    {
                        RemovedSong = false;

                        NowPlaying = "";
                        SongPosition = 0;
                    }
                    break;
                case MediaManager.Player.MediaPlayerState.Paused:
                    PlayButtonStateImage = "PlayBt.png";
                    break;
                default:
                    //Debug.Print("STATE CHANGE: {0}\n", e.State.ToString());
                    break;

            }
        }

        private async void Current_MediaItemChanged(object sender, MediaManager.Media.MediaItemEventArgs e)
        {
            Debug.Print("*** Media Item Changed {0} ***\n", e.MediaItem.FileName);


            //if (IsAligned)
            //{
            //    Debug.Print("*** Media Changed Song ***\n");
            //    await SeekTo(last_playerpos);
            //}

            // Figure out the song index
            if (CrossMediaManager.Current.Queue != null && !RemovedSong)
            {
                if(CurrentTrackNumber != CrossMediaManager.Current.Queue.IndexOf(e.MediaItem))
                {
                    CurrentTrackNumber = CrossMediaManager.Current.Queue.IndexOf(e.MediaItem);

                    await SetSelectedSong(Playlist[CurrentTrackNumber]);
                }
            }



            //NowPlaying = Playlist[CurrentTrackNumber].Name;
            //last_endpos = CrossMediaManager.Current.Duration.TotalSeconds;
        }

        private async Task UpdateSliderAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (CrossMediaManager.Current.IsPlaying())
                {
                    if(CrossMediaManager.Current.Position.TotalSeconds > 0.001 && CrossMediaManager.Current.Duration.TotalSeconds > 0.001)
                    {
                        // We don't want it to seek, so manually set property
                        //songPosition = player.CurrentPosition / player.Duration ;
                        //Debug.Print("Slider changed pos={0} dur={1}\n", CrossMediaManager.Current.Position.TotalSeconds, CrossMediaManager.Current.Duration.TotalSeconds);
                        songPosition = CrossMediaManager.Current.Position.TotalSeconds / CrossMediaManager.Current.Duration.TotalSeconds;
                        //Debug.Print("Slider changed new song pos is {0}\n", songPosition);
                        OnPropertyChanged("SongPosition");
                    }
                }

                await Task.Delay(200, token);
            }
        }


#pragma warning disable AvoidAsyncVoid
        private void Player_PlaybackEnded(object sender, EventArgs e)
        {
            Device.BeginInvokeOnMainThread(async () => 
            {
                if(IsAligned)
                {
                    player.Play();
                }
                else
                {
                    if (CurrentTrackNumber + 1 >= SongsQueued)
                    {
                        if(!IsLooping)
                        {
                            await StopPlayer();
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

        public async void TransportPlayPressed()
        {
            // We use a universal transport that can be used from different pages in the UI
            if(MainPlayMode)
            {
                if (SongsQueued > 0)
                {
                    await ReadyPlaylist();

                    Debug.Print("State is {0} and prepared={1}\n", CrossMediaManager.Current.State, CrossMediaManager.Current.IsPrepared());

                    if(Device.RuntimePlatform == Device.iOS)
                    {
                        if(newPlaylist)
                        {
                            await CrossMediaManager.Current.PlayQueueItem(0);
                        }
                        else
                        {
                            //await CrossMediaManager.Current.PlayQueueItem(0);
                            await CrossMediaManager.Current.Play();
                        }

                    }
                    else
                    {
                        await CrossMediaManager.Current.PlayPause();
                    }
                        
                    newPlaylist = false;
                }
            }
            else
            {
                if(CrossMediaManager.Current.IsPrepared())
                {
                    await CrossMediaManager.Current.PlayPause();
                }
            }
        }

#pragma warning restore AvoidAsyncVoid

        public async void NextSong()
        {
            if (CrossMediaManager.Current.IsPlaying())
            {
                // Remember the last position for isAlign looping comparison
                last_playerpos = SongPosition;
                last_endpos = CrossMediaManager.Current.Duration.TotalSeconds;
                if (CurrentTrackNumber + 1 >= SongsQueued)
                {
                    await CrossMediaManager.Current.PlayQueueItem(0);
                    //if (IsAligned)
                    //{
                    //    Debug.Print("*** Next Song ***\n");
                    //    await SeekTo(last_playerpos);
                    //}
                }
                else
                {
                    await CrossMediaManager.Current.PlayNext();
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

        public async void PrevSong()
        {
            if(CrossMediaManager.Current.IsPlaying())
            {
                if (IsAligned)
                {
                    last_playerpos = SongPosition;
                    last_endpos = CrossMediaManager.Current.Duration.TotalSeconds;

                    if (CurrentTrackNumber <= 0)
                    {
                        CurrentTrackNumber = SongsQueued - 1;
                        await CrossMediaManager.Current.PlayQueueItem(CurrentTrackNumber);
                        //if (IsAligned)
                        //{
                        //    Debug.Print("*** Prev Song ***\n");
                        //    await SeekTo(last_playerpos);
                        //}
                    }
                    else
                    {
                        CurrentTrackNumber--;
                        await CrossMediaManager.Current.PlayPrevious();
                    }

                }
                else
                {
                    await CrossMediaManager.Current.PlayPreviousOrSeekToStart();
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

                if(CrossMediaManager.Current.Queue != null && CrossMediaManager.Current.Queue.Count > 0)
                {
                    CrossMediaManager.Current.Queue.Move(i, i - 1);
                }

                if (i == CurrentTrackNumber)
                {
                    currentTrackNumber--;
                }
            }
            else
            {
                Playlist.Move(i, Playlist.Count - 1);

                if (CrossMediaManager.Current.Queue != null && CrossMediaManager.Current.Queue.Count > 0)
                {
                    CrossMediaManager.Current.Queue.Move(i, Playlist.Count - 1);
                }

                if (i == CurrentTrackNumber)
                {
                    currentTrackNumber = Playlist.Count - 1;
                }
            }
        }

        private void PlayCurrentSongAsync()
        {
            PlaySongAsync(Playlist[CurrentTrackNumber].FullPath);
        }

        //private bool pausePlay;
        private bool playlistReady = false;
        private bool newPlaylist = false;
        private readonly double MAX_AHEAD_SEEK = 0.01;

        public async Task ReadyPlaylist()
        {
            if(!playlistReady)
            {
                //if(CrossMediaManager.Current.IsPlaying())
                //{
                //    await CrossMediaManager.Current.Stop();
                //}

                if (CrossMediaManager.Current.Queue != null)
                {
                    CrossMediaManager.Current.Queue.Clear();
                }

                foreach (QueuedTrack track in Playlist)
                {
                    IMediaItem mediaItem = new MediaItem(track.FullPath) { IsMetadataExtracted = true };
                    mediaItem = await CrossMediaManager.Current.Extractor.UpdateMediaItem(mediaItem).ConfigureAwait(false);
                    
                    mediaItem.Title = track.Name;
                    mediaItem.Album = track.Project;

                    //mediaPlayList.Add(mediaItem);
                    CrossMediaManager.Current.Queue.Add(mediaItem);
                }

                //CrossMediaManager.Current.RepeatMode = MediaManager.Playback.RepeatMode.Off;

                //await CrossMediaManager.Current.Play(mediaPlayList);
                //await CrossMediaManager.Current.PlayPause();

                //pausePlay = true;                
                newPlaylist = true;
                playlistReady = true;
            }

            //CurrentTrackNumber = PersistentData.LastPlayedSongIndex;
            //// Select the last song index
            //OnPropertyChanged("SelectedSong");

            SetRepeatMode();

        }

        public bool PlaySongAsync(string song)
        {

            double playerpos = CrossMediaManager.Current.Position.TotalSeconds;
            //double playerpos = player.CurrentPosition;

            Debug.Print("playing {0}\n", song);

            Dictionary<String, String> properties = new Dictionary<string, string>();

            var TimeElapsed = DateTime.UtcNow - LastTime;

            properties["TrackCount"] = Playlist.Count.ToString();
            properties["LoopMode"] = IsLooping.ToString();
            properties["CompareMode"] = IsAligned.ToString();
            properties["PlayMode"] = playerState.ToString();

            LastTime = DateTime.UtcNow;

            Analytics.TrackEvent("PlayTrack", properties);

            NowPlaying = Path.GetFileNameWithoutExtension(song);

            if(File.Exists(song))
            {
                Debug.Print("Song exists {0}\n", song);
            }
            CrossMediaManager.Current.Play(song);
            //StartPlayer();

            return true;
        }

        public void LoadProjects()
        {
            if(!playingListLoaded)
            {
                ValidatePlayingTracks();

                playingListLoaded = true;

                CurrentTrackNumber = 0;
            }
            
            //await ReadyPlaylist();
        }

        private void ValidatePlayingTracks()
        {

            List<QueuedTrack> t_remove = new List<QueuedTrack>();

            foreach(QueuedTrack t in Playlist)
            {
                if(!File.Exists(t.FullPath))
                {
                    Debug.Print("Song missing {0}\n", t.FullPath);
                    playingListLoaded = false;
                    t_remove.Add(t);
                }
            }

            foreach(QueuedTrack t in t_remove)
            {
                int i = Playlist.IndexOf(t);
                Playlist.Remove(t);
                //mediaPlayList.RemoveAt(i);
            }


            PersistentData.SaveQueuedTracks(Playlist);
        }

        public async Task RemoveSong(QueuedTrack t)
        {
            if (t == null)
                return;

            int i = Playlist.IndexOf(t);
            Playlist.Remove(t);

            playlistReady = false;
            playingListLoaded = false;

            if (CurrentTrackNumber == i)
            {
                if (CrossMediaManager.Current.IsPlaying())
                {
                    await StopPlayer();
                }

                RemovedSong = true;
            }

            if (i >= Playlist.Count)
            {
                CurrentTrackNumber--;
            }
            else
            {
                SelectedSong = Playlist[CurrentTrackNumber];
            }

            Debug.Print("Selected song {0}", CurrentTrackNumber);


            //if (Playlist.Count > 0)
            //{
            //    await ReadyPlaylist();
            //}
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

                CrossMediaManager.Current.Queue.Add(mediaItem);
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

        public async Task StopPlayer()
        {
            await CrossMediaManager.Current.Stop();
        }

        private async Task SeekTo(double songPos)
        {

            if(/*CrossMediaManager.Current.Duration.TotalSeconds > 0 &&*/ CrossMediaManager.Current.Duration.TotalSeconds < 3600 && 
                songPos >= 0 && songPos <= 1 /*&& Math.Abs(songPos - curPos) > MAX_AHEAD_SEEK*/)
            {
                try
                {
                    double curPos = CrossMediaManager.Current.Position.TotalSeconds / CrossMediaManager.Current.Duration.TotalSeconds;

                    Debug.Print("Seeking to: {4} Songpos={0}, curPos={1}, pos={2}, dur={3} --- ", 
                                songPos, 
                                curPos, 
                                CrossMediaManager.Current.Position.TotalSeconds, 
                                CrossMediaManager.Current.Duration.TotalSeconds, 
                                (long)(songPos * CrossMediaManager.Current.Duration.TotalSeconds * 10000000));

                    Debug.Print("Calc span {0}, {1}", songPos, CrossMediaManager.Current.Duration.TotalSeconds);
                    TimeSpan sp = new TimeSpan((long)(songPos * CrossMediaManager.Current.Duration.TotalSeconds * 10000000));
                    //TimeSpan sp = new TimeSpan(3);
                    Debug.Print("Calc span complete {0}", (long)(songPos * CrossMediaManager.Current.Duration.TotalSeconds * 10000000));
                    Debug.Print("Timespan is {0}", sp.TotalSeconds);
                    if(sp.TotalSeconds > 0)
                    {
                        await CrossMediaManager.Current.SeekTo(sp);
                        Debug.Print("Seek completed to {0}\n", (long)(songPos * CrossMediaManager.Current.Duration.TotalSeconds * 10000000));
                    }
                }
                catch(Exception ex)
                {
                    Debug.Print("Exception with timespan {0}\n", ex.ToString());
                }
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
