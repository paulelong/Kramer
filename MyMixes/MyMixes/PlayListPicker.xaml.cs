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
	public partial class PlayListPicker : ContentPage
	{
        private List<Track> Tracks;

		public PlayListPicker (List<Track> tracks)
		{

			InitializeComponent ();
            Tracks = tracks;
        }

        private void SongSelected(object sender, SelectedItemChangedEventArgs e)
        {

        }

        private void OnAppearing(object sender, EventArgs e)
        {
            SongList.ItemsSource = Tracks;
        }
    }
}