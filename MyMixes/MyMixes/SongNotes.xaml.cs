using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MyMixes
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SongNotes : ContentPage
    {
        SongNotesData snd;
        QueuedTrack qt;
        TransportViewModel tvm;

        public SongNotes (TransportViewModel tvm, QueuedTrack qt)
        {
            InitializeComponent();

            this.qt = qt;
            this.tvm = tvm;

            snd = (SongNotesData)this.BindingContext;

            SetSongInfo();
        }

        private void OnDisapearing(object sender, EventArgs e)
        {
            PersistentData.SaveNotes(qt, snd.Notes);
            PersistentData.Save();
        }

        private void LeftSongPressed(object sender, EventArgs e)
        {
            PersistentData.SaveNotes(qt, snd.Notes);

            int i = tvm.Playlist.IndexOf(qt) - 1;

            if (i < 0)
            {
                i = tvm.Playlist.Count - 1;
            }

            qt = tvm.Playlist[i];

            SetSongInfo();
        }

        private void RightSongPressed(object sender, EventArgs e)
        {
            PersistentData.SaveNotes(qt, snd.Notes);

            int i = tvm.Playlist.IndexOf(qt) + 1;

            if (i >= tvm.Playlist.Count)
            {
                i = 0;
            }

            qt = tvm.Playlist[i];

            SetSongInfo();

        }

        private void SetSongInfo()
        {
            snd.SongName = qt.Name;
            snd.ProjectName = qt.Project;

            snd.Notes = PersistentData.LoadNotes(qt);
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            //if(e.NewTextValue != null && e.NewTextValue.Last() == '\n' && e.NewTextValue[e.NewTextValue.Length - 2] != '-')
            //{
            //    if(tvm.player.IsPlaying && tvm.SelectedSong == qt)
            //    {
            //        snd.Notes = snd.Notes.Substring(0, snd.Notes.Length-1) + " " + tvm.player.CurrentPosition.ToString() + "-\n";
            //    }
            //}
        }
    }
}