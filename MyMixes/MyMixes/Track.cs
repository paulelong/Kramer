using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MyMixes
{
    public class Track : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }
        public string FullPath { get; set; }
        public string ProjectPath { get; set; }
        public bool isProject { get; set; }
        public int TrackNum { get; set; }

        private bool cloudProviderSet = false;
        private CloudStorage.CloudProviders cloudProvider = CloudStorage.CloudProviders.NULL;
        public CloudStorage.CloudProviders CloudProvider
        {
            get
            {
                if (!cloudProviderSet)
                {
                    cloudProvider = PersistentData.GetProvider(Project, Name, LastModifiedDate.ToString());
                    if(cloudProvider != CloudStorage.CloudProviders.NULL)
                    {
                        cloudProviderSet = true;
                    }
                }

                return cloudProvider;
            }
            set
            {
                if(value != cloudProvider)
                {
                    if (value == CloudStorage.CloudProviders.NULL)
                    {
                        cloudProviderSet = false;
                        PersistentData.ResetProvider(Project, Name, LastModifiedDateString);
                    }
                    else
                    {
                        PersistentData.SetProvider(Project, Name, LastModifiedDateString, value);
                        cloudProviderSet = true;
                    }
                }
            }
        }

        public string cloudRoot = null;
        public string CloudRoot
        {
            get
            {
                if (string.IsNullOrEmpty(cloudRoot))
                {
                    cloudRoot = PersistentData.GetCloudRoot(Project, Name, LastModifiedDate.ToString());
                }

                return cloudRoot;
            }
            set
            {
                if(value == null)
                {
                    cloudRoot = null;
                    PersistentData.ResetCloudRoot(Project, Name, LastModifiedDate.ToString());
                }
            }
        }

        public string LastModifiedDateString
        {
            get
            {
                return LastModifiedDate.ToShortDateString();
            }
        }
        public string LastModifiedTimeString
        {
            get
            {
                return LastModifiedDate.ToShortTimeString();
            }
        }
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

        public DateTime LastWriteDate { get; set; }

        public string Project
        {
            get
            {
                return Path.GetFileName(ProjectPath);
            }
        }

        private string orderButtonText = "+";
        public string OrderButtonText
        {
            get
            {
                return orderButtonText;
            }
            set
            {
                if(orderButtonText != value)
                {
                    orderButtonText = value;
                    OnPropertyChanged("OrderButtonText");
                }
            }
        }

        private string updateListImage = "AddBt2.png";
        public string UpdateListImage
        {
            get
            {
                return updateListImage;
            }
        }

        private bool trackPlaying = false;
        public bool TrackPlaying
        {
            get
            {
                return trackPlaying;
            }
            set
            {
                if(value != trackPlaying)
                {
                    trackPlaying = value;
                    OnPropertyChanged("PlayListImage");

                    if (trackPlaying)
                    {
                        playListImage = "PauseBt.png";
                    }
                    else
                    {
                        playListImage = "PlayBt.png";
                    }
                }
            }
        }

        private string playListImage = "PlayBt.png";
        public string PlayListImage
        {
            get
            {
                return playListImage;
            }
        }

        public bool ReadyToAdd
        {
            set
            {
                if(value)
                {
                    updateListImage = "AddBt2.png";
                }
                else
                {
                    updateListImage = "RemoveBt2.png";
                }
                OnPropertyChanged("UpdateListImage");
            }
        }


        private int order = 0;

        public string Order
        {
            get
            {
                if(order == 0)
                {
                    return "*";
                }
                else
                {
                    return order.ToString();
                }
            }
            set
            {
                int neworder = 0;
                if(int.TryParse(value, out neworder))
                {
                    order = neworder;
                    OnPropertyChanged("Order");
                }
            }

        }

        public int OrderVal
        {
            get
            {
                return order;
            }
            set
            {
                order = value;
                OnPropertyChanged("Order");

            }
        }

        public void Print()
        {
            
            Debug.Print("{2} {0} {1}\n", Name, FullPath, isProject ? "Project" : "Track");
        }

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
