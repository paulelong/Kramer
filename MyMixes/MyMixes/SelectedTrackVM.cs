using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace MyMixes
{
    public class SelectedTracksVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        List<string> selTrackList = new List<string>();
        List<Track> trackList = new List<Track>();
        string currentSel;
        Track currentTrackSel;
        public List<string> tl = new List<string>();

        public List<string> SelTrackList
        {
            get
            {
                return selTrackList;
            }
            set
            {
                if(selTrackList != value)
                {
                    selTrackList = value;
                    OnPropertyChanged("SelTrackList");
                }
            }
        }

        public List<Track> TrackList
        {
            get { return trackList; }
            set
            {
                if(trackList != value)
                {
                    trackList = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentSel
        {
            get
            {
                return currentSel;
            }
            set
            {
                if(currentSel != value)
                {
                    currentSel = value;
                    OnPropertyChanged("CurrentSel");
                }
            }
        }

        public void Add(Track t)
        {
            trackList.Add(t);
            SelTrackList.Add(t.Name);
            OnPropertyChanged("SelTrackList");
        }

        public void Add(string s)
        {
            SelTrackList.Add(s);
            OnPropertyChanged("SelTrackList");
        }

        public void Remove(Track t)
        {
            trackList.Remove(t);
            SelTrackList.Remove(t.Name);
            OnPropertyChanged("SelTrackList");
        }

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            var changed = PropertyChanged;
            if (changed != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
