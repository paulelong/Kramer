using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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
    }
}
