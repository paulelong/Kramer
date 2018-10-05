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
	public partial class FolderPicker : ContentPage
	{
        List<string> Files = new List<string>();

		public FolderPicker()
		{
			InitializeComponent ();
            //FileList.ItemsSource = Files;

            //Files.Add("test");
		}
	}
}