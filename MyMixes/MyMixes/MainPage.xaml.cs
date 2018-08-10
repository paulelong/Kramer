﻿using PCLStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Plugin.SimpleAudioPlayer;
using System.IO;

namespace MyMixes
{
    public partial class MainPage : ContentPage
    {
        private List<View> Takes = new List<View>();
        private string selectedFolder = "";
        private ISimpleAudioPlayer player;
        private string playingSong;
        private bool isSongPlaying;

        public MainPage()
        {
            InitializeComponent();

            LoadProjects();

            player = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.CreateSimpleAudioPlayer();
        }

        private async void TrackView_Sel(object sender, EventArgs e)
        {
            Track t = (Track)Projects.SelectedItem;
            if (t.isProject)
            {
                selectedFolder = t.Name;
                await LoadProjects();
            }
        }

        private async void Play_Clicked(object sender, EventArgs e)
        {
            Track t = (Track)Projects.SelectedItem;

            if (playingSong != t.FullPath)
            {
                string path = Path.GetDirectoryName(t.FullPath);
                string filename = Path.GetFileName(t.FullPath);

                Debug.Print("playing {0} {1}\n", filename, path);

                IFolder folder = await FileSystem.Current.GetFolderFromPathAsync(path); ;
                IFile source = await folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

                player.Stop();
                using (Stream s = await source.OpenAsync(PCLStorage.FileAccess.Read))
                {
                    if (player.Load(s))
                    {
                        playingSong = t.FullPath;
                        CurrentSong.Text = filename;
                        PlayButton.Image = "PauseBt.png";
                        player.Play();
                    }
                    else
                    {
                        await DisplayAlert("Error openning track ", t.FullPath, "OK");
                    }
                }
            }
            else
            {
                playingSong = "";

                player.Stop();
            }
        }

        private async Task LoadProjects()
        {
            IFolder folder = FileSystem.Current.LocalStorage;
            IList<IFolder> folderList = await folder.GetFoldersAsync();
            var tracks = new List<Track>();

            Debug.Print("data in {0}\n", folder.Path);

            try
            {
                foreach (IFolder f in folderList)
                {
                    if (await WavDirectory(f))
                    {
                        var p = new Track { Name = f.Name, FullPath = f.Path, isProject = true };
                        tracks.Add(p);

                        if (f.Name == selectedFolder)
                        {
                            IList<IFile> fileList = await f.GetFilesAsync();
                            foreach (IFile fl in fileList)
                            {
                                var t = new Track { Name = fl.Name, FullPath = fl.Path, isProject = false };
                                tracks.Add(t);
                            }
                        }
                    }
                }

                Projects.ItemsSource = tracks;

            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }

        }

        private async Task<bool> WavDirectory(IFolder f)
        {
            IList<IFile> l = await f.GetFilesAsync();
            foreach (IFile fl in l)
            {
                if (fl.Name.EndsWith("wav"))
                    return true;
            }

            return false;
        }

        private async void PlaySong_Clicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(playingSong))
            {
                if(isSongPlaying)
                {
                    isSongPlaying = false;
                    player.Pause();
                    PlayButton.Image = "PauseBt.png";
                }
                else
                {
                    isSongPlaying = true;
                    player.Play();
                    PlayButton.Image = "PlayBt.png";
                }
            }
        }

        private void DeleteFolder_Clicked(object sender, EventArgs e)
        {

        }

        private void Delete_Clicked(object sender, EventArgs e)
        {

        }
    }
}
