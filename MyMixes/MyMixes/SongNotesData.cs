using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace MyMixes
{
    class SongNotesData : INotifyPropertyChanged
    {
        private string songName;
        public string SongName
        {
            get
            {
                return songName;
            }
            set
            {
                if(songName != value)
                {
                    songName = value;
                    OnPropertyChanged();
                }
            }
        }

        private string notes;
        public string Notes
        {
            get
            {
                return notes;
            }
            set
            {
                notes = value;
                OnPropertyChanged();
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
