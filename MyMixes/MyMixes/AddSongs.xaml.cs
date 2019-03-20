﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MyMixes
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class AddSongs : ContentPage
	{
        //private string selectedFolder = "";
        private Track selectedTrack = null;
        private Dictionary<string, int> PlayListOrder = new Dictionary<string, int>();

        ObservableCollection<QueuedTrack> SelectedTrackList = new ObservableCollection<QueuedTrack>();
        ObservableCollection<Track> LoadedTracks = new ObservableCollection<Track>();

        ObservableCollection<MixLocation> MixLocationList = null;


        public AddSongs (ObservableCollection<MixLocation> list)
		{
			InitializeComponent ();

            SelectedTracks.ItemsSource = SelectedTrackList;
            Projects.ItemsSource = LoadedTracks;

            MixLocationList = list;

            PersistentData.LoadQueuedTracks(SelectedTrackList);            
        }

        private void DeleteFolder_Clicked(object sender, EventArgs e)
        {

        }

        private void ResyncProjectClickedAsync(object sender, EventArgs e)
        {

        }

        private void LocalPlay_Clicked(object sender, EventArgs e)
        {

        }

        private async void AddFolder_Clicked(object sender, EventArgs e)
        {
            ProjectPicker pp = new ProjectPicker(MixLocationList);
            await Navigation.PushAsync(pp);
        }

        private async void SongOrderClicked(object sender, EventArgs e)
        {
            Track t = FindTrack((View)sender);

            int i = 0;
            for(;i < SelectedTrackList.Count; i++)
            {
                if (SelectedTrackList[i].Name == t.Name && SelectedTrackList[i].Project == t.Project)
                    break;
            }

            if (i >= SelectedTrackList.Count)
            {
                SelectedTrackList.Add(new QueuedTrack() { Name = t.Name, Project = t.Project, FullPath = t.FullPath });
            }

            await PersistentData.SaveQueuedTracks(SelectedTrackList);
        }

        private async void TrackView_Sel(object sender, SelectedItemChangedEventArgs e)
        {
            Debug.Print("Track selected\n");
            Track t = (Track)e.SelectedItem;

            if (!t.isProject)
            {

            }
            else
            {
                if(selectedTrack != t)
                {
                    selectedTrack = t;
                }
                else
                {
                    selectedTrack = null;
                }

                await LoadProjects();
            }
        }

        private async void OnAppearing(object sender, EventArgs e)
        {
            BusyOn(true);
            await LoadProjects();
            BusyOn(false);
        }

        private async void ResyncAllClickedAsync(object sender, EventArgs e)
        {
            // DCR: Maybe we don't sync all the time
            BusyOn(true);
            await SyncProjects();
            await LoadProjects();
            BusyOn(false);
        }

        private void BusyOn(bool TurnOn)
        {
            BusyGrid.IsVisible = TurnOn;
            BusySignal.IsRunning = TurnOn;
            MainStack.IsEnabled = !TurnOn;
        }

        Track FindTrack(View v)
        {
            Grid g = (Grid)v.Parent;
            Track t = (Track)g.BindingContext;
            return t;
        }

        QueuedTrack FindQueuedTrack(View v)
        {
            Grid g = (Grid)v.Parent;
            QueuedTrack t = (QueuedTrack)g.BindingContext;
            return t;
        }

        private async Task SyncProjects()
        {
            Dictionary<string, List<string>> AllSongs = new Dictionary<string, List<string>>();

            foreach (MixLocation ml in MixLocationList)
            {
                BusyStatus.Text = ml.Provider.ToString() + " " + ml.Path;

                ProviderInfo pi = await ProviderInfo.GetCloudProviderAsync(ml.Provider);

                if (await pi.CheckAuthenitcation())
                {
                    List<string> l = await pi.GetFoldersAsync(ml.Path);
                    if (l != null)
                    {
                        foreach (string f in l)
                        {
                            BusyStatus.Text = ml.Provider.ToString() + " " + ml.Path  + " " + f;
                            var retList = await pi.UpdateProjectAsync(ml.Path, f);
                            if (AllSongs.ContainsKey(f))
                            {
                                AllSongs[f].AddRange(retList);
                            }
                            else
                            {
                                AllSongs[f] = retList;
                            }
                        }
                    }
                }
            }

            string rootPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            foreach (string p in Directory.GetDirectories(rootPath))
            {
                if (!AllSongs.ContainsKey(Path.GetFileName(p)))
                {
                    Debug.Print("Remove dir " + p + "\n");
                    BusyStatus.Text = "Removing " + p;
                    Directory.Delete(p, true);
                }
                else
                {
                    foreach (string s in Directory.GetDirectories(p))
                    {
                        if (AllSongs[p].Contains(Path.GetFileName(s)))
                        {
                            BusyStatus.Text = "Removing " + s;
                            Debug.Print("Remove file " + s + "\n");
                            File.Delete(s);
                        }
                    }
                }
            }

            foreach (string p in AllSongs.Keys)
            {
                foreach (string f in AllSongs[p])
                {
                    BusyStatus.Text = p + " " + f;
                }
            }

            PersistentData.Save();
        }

        private async Task LoadProjects()
        {
            for (int i = LoadedTracks.Count - 1; i >= 0; i--)
            {
                if (!LoadedTracks[i].isProject)
                {
                    LoadedTracks.RemoveAt(i);
                }
            }

            LoadProjectFolders();

            if (selectedTrack != null)
            {
                LoadProjectTracks();
            }
        }

        private void LoadProjectTracks()
        {
            try
            {
                string newProjectPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/" + selectedTrack.Name;
                int projectIndex = LoadedTracks.IndexOf(selectedTrack) + 1;
                int insTrackNumber = 0;

                foreach (string songFile in Directory.GetFiles(newProjectPath))
                {
                    int tracknum = PersistentData.GetTrackNumber(newProjectPath, Path.GetFileNameWithoutExtension(songFile));

                    var t = new Track
                    {
                        Name = Path.GetFileNameWithoutExtension(songFile),
                        FullPath = songFile,
                        isProject = false,
                        ProjectPath = newProjectPath,
                        OrderVal = PlayListOrder.ContainsKey(songFile) ? PlayListOrder[songFile] : 0,
                        TrackNum = tracknum,
                    };

                    if (tracknum != 0)
                    {
                        LoadedTracks.Insert(projectIndex + tracknum - 1, t);
                    }
                    else
                    {
                        LoadedTracks.Insert(projectIndex + insTrackNumber, t);
                    }

                    insTrackNumber++;
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }


            //Debug.Print("Project local {0}\n", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            //try
            //{
            //    foreach (string projFolder in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)))
            //    //foreach (IFolder f in folderList)
            //    {
            //        if (await WavDirectory(projFolder))
            //        {
            //            var p = new Track { Name = Path.GetFileName(projFolder), FullPath = projFolder, isProject = true };
            //            LoadedTracks.Add(p);

            //            if (selectedFolder == Path.GetFileName(projFolder))
            //            {
            //                //IList<IFile> fileList = await f.GetFilesAsync();
            //                foreach (string songFile in Directory.GetFiles(projFolder))
            //                {
            //                    int tracknum = PersistentData.GetTrackNumber(projFolder, Path.GetFileNameWithoutExtension(songFile));

            //                    var t = new Track
            //                    {
            //                        Name = Path.GetFileNameWithoutExtension(songFile),
            //                        FullPath = songFile,
            //                        isProject = false,
            //                        ProjectPath = projFolder,
            //                        OrderVal = PlayListOrder.ContainsKey(songFile) ? PlayListOrder[songFile] : 0,
            //                        TrackNum = tracknum,                                    
            //                    };

            //                    if (tracknum != 0)
            //                    {
            //                        LoadedTracks.Insert(tracknum - 1, t);
            //                    }
            //                    else
            //                    {
            //                        LoadedTracks.Add(t);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Debug.Print(ex.Message);
            //}
        }

        private void LoadProjectFolders()
        {
            try
            {
                foreach (string projFolder in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)))
                {
                    if (WavDirectory(projFolder))
                    {
                        Track t = new Track { Name = Path.GetFileName(projFolder), FullPath = projFolder, isProject = true };

                        int i;
                        for(i = 0; i < LoadedTracks.Count; i++)
                        {
                            // if it's already inserted don't insert again.  This is typical if we've added them already.
                            if(t.Name == LoadedTracks[i].Name)
                            {
                                i = -1;
                                break;
                            }
                            if (string.Compare(t.Name, LoadedTracks[i].Name) < 0)
                            {
                                break;
                            }
                        }

                        if(i >= 0)
                        {
                            LoadedTracks.Insert(i, t);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
        }

        private bool WavDirectory(string f)
        {
            foreach (string fl in Directory.GetFiles(f))
            {
                if (MusicUtils.isAudioFormat(fl))
                    return true;
            }

            return false;
        }

        private async void DeleteSong_Clicked(object sender, EventArgs e)
        {
            QueuedTrack t = FindQueuedTrack((View)sender);

            for (int i = 0; i < SelectedTrackList.Count; i++)
            {
                if (SelectedTrackList[i].Name == t.Name && SelectedTrackList[i].Project == t.Project)
                {
                    SelectedTrackList.RemoveAt(i);
                    break;
                }
            }

            await PersistentData.SaveQueuedTracks(SelectedTrackList);
        }
    }
}