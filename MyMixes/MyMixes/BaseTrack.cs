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
                return simpleDate(LastModifiedDate);
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

        public string LastModifiedDateTimeString
        {
            get
            {
                return LastModifiedTimeSimple + "\n" + LastModifiedDateSimple;
            }
        }

        private DateTime earliest = DateTime.MaxValue;
        public DateTime Earliest
        {
            get
            {
                if(earliest == DateTime.MaxValue)
                {
                    ComputeFirstLastDate();
                }

                return earliest;
            }
        }

        private DateTime latest = DateTime.MinValue;
        public DateTime Latest
        {
            get
            {
                if (latest == DateTime.MinValue)
                {
                    ComputeFirstLastDate();
                }

                return latest;
            }
        }

        private string folderDateTimeString = null;
        public string FolderDateTimeString
        {
            get
            {
                if(folderDateTimeString == null)
                {
                    ComputeFirstLastDate();

                    if (earliest.Date == latest.Date)
                    {
                        folderDateTimeString = simpleDate(earliest);
                    }
                    else
                    {
                        folderDateTimeString = simpleDate(earliest) + "\n" + simpleDate(latest);
                    }
                }

                return folderDateTimeString;
            }
        }

        private void ComputeFirstLastDate()
        {
            foreach (string songFile in Directory.GetFiles(FullPath))
            {
                if (MusicUtils.isAudioFormat(songFile))
                {
                    FileInfo info = new FileInfo(songFile);

                    if (info.LastWriteTime > latest)
                    {
                        latest = info.LastWriteTime;
                    }

                    if (info.LastWriteTime < earliest)
                    {
                        earliest = info.LastWriteTime;
                    }
                }
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

        private string simpleDate(DateTime dt)
        {
            string date = "";

            if (DateTime.Now.DayOfYear == dt.DayOfYear)
            {
                date = "Today";
            }
            else
            {
                var diff = DateTime.Now - dt;
                if (diff.Days > 365)
                {
                    date = dt.ToShortDateString();
                }
                else if (DateTime.Now.DayOfYear - 1 == dt.DayOfYear)
                {
                    date = "Yesterday";
                }
                else
                {
                    date = String.Format("{0:M/d}", dt);
                }
            }
            return date;
        }

        private string simpleTime(DateTime dt)
        {
            return String.Format("{0:h:mm tt}", LastModifiedDate);
        }

        public void Print()
        {

            Debug.Print("{2} {0} {1}\n", Name, FullPath, isProject ? "Project" : "Track");
        }
    }
}
