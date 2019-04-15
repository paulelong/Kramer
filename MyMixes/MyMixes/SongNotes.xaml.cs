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

        public SongNotes (QueuedTrack qt)
        {
            InitializeComponent();

            this.qt = qt;

            snd = (SongNotesData)this.BindingContext;

            snd.SongName = qt.Name + " - " + qt.Project;

            snd.Notes = PersistentData.LoadNotes(qt);
        }

        private void OnDisapearing(object sender, EventArgs e)
        {
            PersistentData.SaveNotes(qt, snd.Notes);
            PersistentData.Save();
        }
    }
}