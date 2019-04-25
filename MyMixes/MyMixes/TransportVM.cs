﻿using Microsoft.AppCenter.Analytics;
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

        private bool playingListLoaded = false;

        private PlayerStates playerState = PlayerStates.Stopped;

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
                if(isAligned)
                {
                    player.Play();
                }
                else
                {
                    if (CurrentTrackNumber + 1 >= SongsQueued)
                    {
                        if (isLooping)
                        {
                            await PlayCurrentSongAsync();
                        }
                        else
                        {
                            StopPlayer();
                        }
                        CurrentTrackNumber = 0;
                    }
                    else
                    {
                        CurrentTrackNumber++;
                        if (isLooping)
                        {
                            await PlayCurrentSongAsync();
                        }
                        else
                        {
                            StopPlayer();
                        }
                    }
                }
            });
        }

        public async void PlaySong()
        {
            if (SongsQueued > 0)
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
                        await PlayCurrentSongAsync();
                        break;
                }

                //if (playerState)
                //{
                //    if (player.IsPlaying)
                //    {
                //        PausePlayer();
                //    }
                //    else
                //    {
                //        StartPlayer();
                //    }
                //}
                //else
                //{
                //    await PlayCurrentSongAsync();
                //}
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

        private async Task PlayCurrentSongAsync()
        {
            double playerpos = player.CurrentPosition;

            Debug.Print("playing {0}\n", PlayingTracks[CurrentTrackNumber].FullPath);

            try
            {
                player.Stop();

                using (Stream s = new FileStream(PlayingTracks[CurrentTrackNumber].FullPath, FileMode.Open))
                {
                    if (player.Load(s))
                    {
                        if (playerState != PlayerStates.Stopped && isAligned)
                        {
                            player.Seek(playerpos);
                        }

                        StartPlayer();
                    }
                    else
                    {
                        Analytics.TrackEvent("PlayCurrent player laod failed");
                    }
                }

            }
            catch(Exception ex)
            {
                Debug.Print(ex.ToString());
            }
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

        private void StopPlayer()
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
