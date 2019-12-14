using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MyMixes
{
    public class BaseTrack : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string DisplayName
        {
            get
            {
                return Name + Path.GetExtension(FullPath);
            }
        }

        public string FullPath { get; set; }
        public bool isProject { get; set; }

        public DateTime LastModifiedDate { get; set; }
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

        public string LastModifiedDateString
        {
            get
            {
                return LastModifiedDate.ToShortDateString();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var changed = PropertyChanged;
            if (changed != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        public void Print()
        {

            Debug.Print("{2} {0} {1}\n", Name, FullPath, isProject ? "Project" : "Track");
        }
    }
}
