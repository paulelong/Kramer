using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace MyMixes
{
    public class QueuedTrack : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }
        public string Project { get; set; }
        public string FullPath { get; set; }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var changed = PropertyChanged;
            if (changed != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        static public QueuedTrack FindQueuedTrack(View v)
        {
            Grid g = (Grid)v.Parent;
            QueuedTrack t = (QueuedTrack)g.BindingContext;
            return t;
        }
    }
}
