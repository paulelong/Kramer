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
        public string LastModifiedDateSimple
        {
            get
            {
                string date = "";

                if (DateTime.Now.DayOfYear == LastModifiedDate.DayOfYear)
                {
                    date = "Today";
                }
                else
                {
                    var diff = DateTime.Now - LastModifiedDate;
                    if (diff.Days > 365)
                    {
                        date = LastModifiedDate.ToShortDateString();
                    }
                    else if (DateTime.Now.DayOfYear - 1 == LastModifiedDate.DayOfYear)
                    {
                        date = "Yesterday";
                    }
                    else
                    {
                        date = String.Format("{0:M/d}", LastModifiedDate);
                    }
                }
                return date;
            }
        }
        public string LastModifiedTimeSimple
        {
            get
            {
                return String.Format("{0:h:mm tt}", LastModifiedDate);
            }
        }
        public DateTime LastModifiedDate { get; set; }
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
